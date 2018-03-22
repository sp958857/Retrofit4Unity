#region License

// Author: Weichao Wang     
// Start Date: 2017-05-22

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Retrofit.Converter;
using Retrofit.HttpImpl;
using Retrofit.Methods;
using Retrofit.Parameters;
using Retrofit.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;
using HeaderAttribute = Retrofit.Parameters.HeaderAttribute;

namespace Retrofit
{
    public abstract class RestAdapter : MonoBehaviour
    {
        public string baseUrl;
        public Converter.Converter convert;
        public HttpImplement httpImpl;
        public RequestInterceptor interceptor;

        public RxSupport rxSupport;
        public Type iRestInterface;

        private Dictionary<string, RestMethodInfo> methodInfoCache = new Dictionary<string, RestMethodInfo>();
        private bool genMethodInfoComplete = false;

        public virtual bool EnableDebug()
        {
            return false;
        }
        public void Awake()
        {
            methodInfoCache.Clear();
            convert = new DefalutConvert();
            httpImpl = SetHttpImpl();
            interceptor = SetIntercepter();
            rxSupport = new RxSupport(convert, httpImpl, interceptor);
            SetRestAPI();
            StartCoroutine(GenMethodCache());
        }

   

        private IEnumerator GenMethodCache()
        {
            RestMethodInfo restMethodInfo = null;
            foreach (MethodInfo methodInfo in iRestInterface.GetMethods())
            {
                if (!methodInfoCache.TryGetValue(methodInfo.ToString(), out restMethodInfo))
                {
                    restMethodInfo = ParseRequestInfoByAttribute(methodInfo);
                    methodInfoCache.Add(methodInfo.ToString(), restMethodInfo);
                }
            }
            genMethodInfoComplete = true;
            yield return null;
        }
        protected abstract RequestInterceptor SetIntercepter();
        protected abstract HttpImplement SetHttpImpl();
        protected abstract void SetRestAPI();
        protected void SendRequest<T>(Callback<T> cb, params object[] arguments)
        {
            var methodInfo = GetRestMethodInfo();
            var url = ParseRestParameters<T>(methodInfo, arguments);
            //request
            StartCoroutine(Request(methodInfo, url, arguments, cb));
        }

        protected object SendRequest<T>(params object[] arguments)
        {
            var methodInfo = GetRestMethodInfo();
            if (methodInfo.IsObservable && (httpImpl is RxHttpImplement))
            {
                var url = ParseRestParameters<T>(methodInfo, arguments);
                var ob = rxSupport.CreateRequestObservable<T>(methodInfo, url, arguments);
                return ob;
            }
            else
            {
                Callback<T> cb = arguments[0] as Callback<T>;
                object[] args = new List<object>(arguments).GetRange(1, arguments.Length - 1).ToArray();
                //request
                var url = ParseRestParameters<T>(methodInfo, args);
                StartCoroutine(Request(methodInfo, url, args, cb));
                return null;
            }
        }

        private RestMethodInfo GetRestMethodInfo(int stackIndex = 2)
        {
            StackTrace stackTrace = new StackTrace();
            MethodBase method = stackTrace.GetFrame(stackIndex).GetMethod();

            RestMethodInfo methodInfo = null;
            if (!genMethodInfoComplete && !methodInfoCache.TryGetValue(method.ToString(), out methodInfo))
            {
                //gen cache has not completed and can't be found in cache, just gen and use, but not add to cache;
                methodInfo = ParseRequestInfoByAttribute(method);
            }
            else if (genMethodInfoComplete && !methodInfoCache.TryGetValue(method.ToString(), out methodInfo))
            {
                //gen cache has completed and can't be found in cache, something was wrong!
                Log("RestAdapter Gen Cache Process Error!!!!");
                methodInfo = ParseRequestInfoByAttribute(method);
                methodInfoCache.Add(method.ToString(), methodInfo);
            }
            return methodInfo;
        }

        protected RestMethodInfo ParseRequestInfoByAttribute(MethodBase method)
        {
            RestMethodInfo methodInfo = new RestMethodInfo(method);
            string methodName = method.Name;
            MethodInfo mi = iRestInterface.GetMethod(methodName);
            ParseMethodHeaders(mi, methodInfo);
            ParseMethodAttributes(mi, methodInfo);
            ParseParameters(mi, methodInfo);
            return methodInfo;
        }


        private Dictionary<string, string> ParseHeaders(MethodInfo methodInfo)
        {
            var ret = new Dictionary<string, string>();

            var declaringTypeAttributes = methodInfo.DeclaringType != null
                ? methodInfo.DeclaringType.GetCustomAttributes(true)
                : new Attribute[0];

            // Headers set on the declaring type have to come first, 
            // so headers set on the method can replace them. Switching
            // the order here will break stuff.
            var headers = declaringTypeAttributes.Concat(methodInfo.GetCustomAttributes(true))
                .OfType<HeadersAttribute>()
                .SelectMany(ha => ha.Headers);

            foreach (var header in headers)
            {
                if (StringUtils.IsNullOrWhiteSpace(header)) continue;

                // NB: Silverlight doesn't have an overload for String.Split()
                // with a count parameter, but header values can contain
                // ':' so we have to re-join all but the first part to get the
                // value.
                var parts = header.Split(':');
                ret[parts[0].Trim()] = parts.Length > 1 ?
                    StringUtils.Join(":", parts.Skip(1)).Trim() : null;
            }

            return ret;
        }

        private void ParseMethodHeaders(MethodInfo mi, RestMethodInfo methodInfo)
        {
            methodInfo.Headers = ParseHeaders(mi);
        }

        private Dictionary<string, string> BuildHeaderParameterMap(List<string> headerParamNames, List<string> headerParamArgs)
        {
            var ret = new Dictionary<string, string>();

            for (int i = 0; i < headerParamNames.Count; i++)
            {
                ret.Add(headerParamNames[i], headerParamArgs[i]);
            }
            return ret;
        }

        private Dictionary<string, object> BuildFieldParameterMap(List<string> fieldParamNames, List<object> fieldParamArgs)
        {
            var ret = new Dictionary<string, object>();

            for (int i = 0; i < fieldParamNames.Count; i++)
            {
                ret.Add(fieldParamNames[i], fieldParamArgs[i]);
            }
            return ret;
        }

        protected IEnumerator Request<T>(RestMethodInfo methodInfo, string url, object[] arguments, Callback<T> cb)
        {
            object request = httpImpl.BuildRequest(methodInfo, url);
            InterceptRequest(request);
            CoroutineWithData cd = new CoroutineWithData(this, httpImpl.SendRequest(this, request));
            yield return cd.coroutine;
            string errorMessage;
            if (httpImpl.IsRequestError(cd.result, out errorMessage))
            {
                cb.errorCB(errorMessage);
                yield break;
            }
            string result = httpImpl.GetSuccessResponse(cd.result);
            //                        result = "[]";
            //                        result = "[asd..s]";
            Log("Response:" + result);

            //Parse Json By Type
            if (typeof (T) == typeof (string))
            {
                var response = (T) (object) result;
                cb.successCB(response);
                yield break;
            }
            T data = default(T);
            bool formatError = false;
            try
            {
                data = convert.FromBody<T>(result);
            }
            catch (ConversionException e)
            {
                formatError = true;
                cb.errorCB(e.Message);
            }
            if (!formatError)
            {
                cb.successCB(data);
            }
        }

        private string ParseRestParameters<T>(RestMethodInfo methodInfo, object[] arguments)
        {
            string url = baseUrl + methodInfo.Path;
            //if param has QUERY attribute, add the fileds(POST) or URL parameteters(GET)
            //if param has PATH attribute, parse the URL
            List<string> pathParamNames = new List<string>();
            List<object> pathParamArgs = new List<object>();
            List<string> queryParamNames = new List<string>();
            List<object> queryParamArgs = new List<object>();
            List<string> fieldParamNames = new List<string>();
            List<object> fieldParamArgs = new List<object>();
            List<string> headerParamNames = new List<string>();
            List<string> headerParamArgs = new List<string>();
            object body = null;
            object part = null;
            Dictionary<string, string> queryMap = new Dictionary<string, string>();
            int i = 0;
            foreach (var paramType in methodInfo.ParameterUsage)
            {
                if (paramType == RestMethodInfo.ParamUsage.Path)
                {
                    pathParamNames.Add(methodInfo.ParameterNames[i]);
                    pathParamArgs.Add(arguments[i]);
                }
                if (paramType == RestMethodInfo.ParamUsage.Query)
                {
                    queryParamNames.Add(methodInfo.ParameterNames[i]);
                    queryParamArgs.Add(arguments[i]);
                }
                if (paramType == RestMethodInfo.ParamUsage.Field)
                {
                    fieldParamNames.Add(methodInfo.ParameterNames[i]);
                    fieldParamArgs.Add(arguments[i]);
                }
                if (paramType == RestMethodInfo.ParamUsage.Body)
                {
                    body = arguments[i];
                }
                if (paramType == RestMethodInfo.ParamUsage.QueryMap)
                {
                    queryMap = arguments[i] as Dictionary<string, string>;
                }
                if (paramType == RestMethodInfo.ParamUsage.Header)
                {
                    headerParamNames.Add(methodInfo.ParameterNames[i]);
                    headerParamArgs.Add(arguments[i] as string);
                }
                if (methodInfo.IsMultipart && paramType == RestMethodInfo.ParamUsage.Part)
                {
                    part = arguments[i];
                }
                i++;
            }
            //replace PATH in URL
            if (pathParamNames.Count > 0)
            {
                string tmpUrl = url;
                int j = 0;
                foreach (var paramName in pathParamNames)
                {
                    tmpUrl = tmpUrl.Replace("{" + paramName + "}", "{" + j + "}");
                    j++;
                }
                tmpUrl = string.Format(tmpUrl, pathParamArgs.ToArray());
                url = tmpUrl;
                Log("parse PATH url:" + url);
            }
            //add QUERY in URL
            bool hasQuery = false;
            if (queryParamNames.Count > 0)
            {
                hasQuery = true;
                StringBuilder sb = new StringBuilder(url);
                sb.Append("?");
                int l = 0;
                foreach (var queryParamName in queryParamNames)
                {
                    if (l > 0)
                    {
                        sb.Append("&");
                    }
                    sb.Append(queryParamName);
                    sb.Append("=");
                    sb.Append(queryParamArgs[l]);
                    l++;
                }
                url = sb.ToString();
                Log("parse QUERY　url:" + url);
            }
            //add QUERYMAP in URL
            if (queryMap != null && (methodInfo.Method == Method.Get && queryMap.Count > 0))
            {
                StringBuilder sb = new StringBuilder(url);
                if (!hasQuery)
                {
                    sb.Append("?");
                }
                int l = 0;
                foreach (KeyValuePair<string, string> keyValuePair in queryMap)
                {
                    if (l > 0 || hasQuery)
                    {
                        sb.Append("&");
                    }
                    sb.Append(keyValuePair.Key);
                    sb.Append("=");
                    sb.Append(keyValuePair.Value);
                    l++;
                }
                url = sb.ToString();
                Log("parse QUERY-MAP　url:" + url);
            }
            //add field to RestMethodInfo
            methodInfo.FieldParameterMap = BuildFieldParameterMap(fieldParamNames, fieldParamArgs);
            //convert body to string, and add to RestMethodInfo
            string bodyString = body != null ? convert.ToBody(body) : string.Empty;
            methodInfo.bodyString = bodyString;
            //add part
            methodInfo.Part = MultipartBody.Convert(part);
            //add patamter header to RestMethodInfo
            methodInfo.HeaderParameterMap = BuildHeaderParameterMap(headerParamNames, headerParamArgs);
            return url;
        }

        protected void InterceptRequest(object request)
        {
            if (interceptor != null)
            {
                interceptor.Intercept(request);
            }
        }

        protected void ParseMethodAttributes(MethodBase method, RestMethodInfo info)
        {
            foreach (Attribute attribute in method.GetCustomAttributes(true))
            {
                if (attribute is ValueAttribute)
                {
                var innerAttributes = attribute.GetType().GetCustomAttributes(true);

                // Find the request method attribute, if present.
                var methodAttribute =
                    innerAttributes.FirstOrDefault(
                        theAttribute => theAttribute.GetType() == typeof (RestMethodAttribute)) as RestMethodAttribute;
                    if (methodAttribute != null)
                    {
                        info.Method = methodAttribute.
                            Method;
                        var valueAttribute = attribute as ValueAttribute;
                        info.Path = valueAttribute.Value;
                    }
                }

                if (attribute is MultipartAttribute)
                {
                    info.IsMultipart = true;
                }
            }
        }

        protected void ParseParameters(MethodBase methodInfo, RestMethodInfo info)
        {
            foreach (ParameterInfo parameter in methodInfo.GetParameters())
            {
                var attribute = (ValueAttribute) parameter.GetCustomAttributes(false).FirstOrDefault();


                if (attribute == null)
                {
                    bool isCallback = parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof (Callback<>);
                    if (!isCallback)
                    {
                        Log("No annotation found on parameter " + parameter.Name + " of " + methodInfo.Name);
                    }
                    continue;
                }
                var type = attribute.GetType();
                if (type == typeof (PathAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Path);
                    info.ParameterNames.Add(attribute.Value);
                }
                else if (type == typeof (BodyAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Body);
                    info.ParameterNames.Add(null);
                    info.HasBody = true;
                }
                else if (type == typeof (QueryAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Query);
                    info.ParameterNames.Add(attribute.Value);
                }
                else if (type == typeof (QueryMapAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.QueryMap);
                    info.ParameterNames.Add(attribute.Value);
                }else if (type == typeof (FieldAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Field);
                    info.ParameterNames.Add(attribute.Value);
                }
                else if (type == typeof (HeaderAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Header);
                    info.ParameterNames.Add(attribute.Value);
                }else if (type == typeof (PartAttribute))
                {
                    if (!info.IsMultipart)
                    {
                        //rule1:[Part] parameters can only be used with multipart encoding
                        throw new ArgumentException("[Part] parameters can only be used with multipart encoding.");
                    }
                    if (parameter.ParameterType == typeof (FileInfo) ||
                        parameter.ParameterType == typeof (MultipartBody))
                    {
                        info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Part);
                        info.ParameterNames.Add(null);
                    }
                    else
                    {
                        //rule2:[Part] parameter type can only be FileInfo or string or MultipartBody
                        throw new Exception("[Part] parameter type can only be FileInfo or string or MultipartBody.");
                    }
                }
            }
            if (info.IsMultipart)
            {
                var partParams = methodInfo.GetParameters()
                    .Select(x => new {Parameter = x, PartAttribute = x.GetCustomAttributes(true).OfType<PartAttribute>().FirstOrDefault()})
                    .Where(x => x.PartAttribute != null)
                    .ToList();
                if (partParams.Count > 1)
                {
                    //rule3:Multipart requests can only contain One Part parameter
                    throw new ArgumentException("Multipart requests can only contain One Part parameter at most");
                }
                if (partParams.Count < 1)
                {
                    //rule4:Multipart must contain Part parameter
                    throw new ArgumentException("Multipart must contain Part parameter");
                }
                if (info.HasBody)
                {
                    //rule5:Multipart requests may not contain a Body parameter
                    throw new ArgumentException("Multipart requests may not contain a Body parameter");
                }
                info.GotPart = true;
            }
        }

        protected void Log(string log)
        {
            if (EnableDebug())
            {
                Debug.Log(log);
            }
        }
    }
}