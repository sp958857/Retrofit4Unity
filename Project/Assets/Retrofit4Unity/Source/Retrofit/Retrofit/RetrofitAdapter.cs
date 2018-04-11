#region License

// Author: Weichao Wang     
// Start Date: 2018-04-09

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Castle.Core.Internal;
using Castle.DynamicProxy;
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
    public class RetrofitAdapter : MonoBehaviour, IInterceptor
    {
        private string baseUrl;
        private Converter.Converter convert;
        private ErrorHandler errorHandler;
        private bool genMethodInfoComplete;
        private HttpImplement httpImpl;
        private RequestInterceptor interceptor;
        private Type iRestInterface;

        private readonly Dictionary<string, RestMethodInfo> methodInfoCache = new Dictionary<string, RestMethodInfo>();

        private RxSupport rxSupport;
        private bool enableDebug;
        public void Intercept(IInvocation invocation)
        {
            var arguments = invocation.Arguments;
            var methodInfo = GetRestMethodInfo(invocation.Method);
            Type t = null;
            if (methodInfo.IsObservable && httpImpl is RxHttpImplement)
            {
                var url = ParseRestParameters(methodInfo, arguments);
                var crb = typeof(RxSupport).GetMethod("CreateRequestObservable");
                if (crb != null)
                {
                    var ob = crb.MakeGenericMethod(invocation.Method.ReturnType.GetGenericArguments()[0])
                        .Invoke(rxSupport, new object[] { methodInfo, url, arguments });
                    invocation.ReturnValue = ob;
                }
               
            }
            else
            {
                var cb = arguments[0];
                var args = new List<object>(arguments).GetRange(1, arguments.Length - 1).ToArray();
                //request
                var url = ParseRestParameters(methodInfo, args);
                var genericArg = cb.GetType().GetGenericArguments().Single();
                var cr = typeof(RetrofitAdapter).GetMethod("CoroutineRequest", BindingFlags.Instance|BindingFlags.NonPublic);
                if (cr != null)
                {
                    cr.MakeGenericMethod(genericArg)
                        .Invoke(this, new object[] { methodInfo, url, args, cb });
                    invocation.ReturnValue = null;
                }
            }
        }

        public void Init(bool enableLog,string baseUrl, HttpImplement httpImpl, RequestInterceptor requestInterceptor, Converter.Converter converter, ErrorHandler errorHandler)
        {
            methodInfoCache.Clear();
            this.enableDebug = enableLog;
            this.baseUrl = baseUrl;
            this.httpImpl = httpImpl;
            this.interceptor = requestInterceptor;
            this.convert = converter;
            this.errorHandler = errorHandler;
            this.rxSupport = new RxSupport(convert, httpImpl, interceptor);
        }

        public T Create<T>()
        {
            RetrofitUtils.ValidateServiceClass(typeof(T));
            iRestInterface = typeof(T);
            StartCoroutine(GenMethodCache());
            var generator = new ProxyGenerator();
            var service = (T) generator.CreateInterfaceProxyWithoutTarget(typeof(T), this);
            return service;
        }

        private void CoroutineRequest<T>(RestMethodInfo methodInfo, string url, object[] args, Callback<T> cb)
        {
            StartCoroutine(Request(methodInfo, url, args, cb));
        }

        private IEnumerator GenMethodCache()
        {
            RestMethodInfo restMethodInfo = null;
            foreach (var methodInfo in iRestInterface.GetMethods())
                if (!methodInfoCache.TryGetValue(methodInfo.ToString(), out restMethodInfo))
                {
                    restMethodInfo = ParseRequestInfoByAttribute(methodInfo);
                    methodInfoCache.Add(methodInfo.ToString(), restMethodInfo);
                }
            genMethodInfoComplete = true;
            yield return null;
        }

        private RestMethodInfo GetRestMethodInfo(MethodBase method)
        {
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
            var methodInfo = new RestMethodInfo(method);
            var methodName = method.Name;
            var mi = iRestInterface.GetMethod(methodName);
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
                ret[parts[0].Trim()] = parts.Length > 1 ? StringUtils.Join(":", parts.Skip(1)).Trim() : null;
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
            for (var i = 0; i < headerParamNames.Count; i++)
                ret.Add(headerParamNames[i], headerParamArgs[i]);
            return ret;
        }

        private Dictionary<string, object> BuildFieldParameterMap(List<string> fieldParamNames, List<object> fieldParamArgs)
        {
            var ret = new Dictionary<string, object>();

            for (var i = 0; i < fieldParamNames.Count; i++)
                ret.Add(fieldParamNames[i], fieldParamArgs[i]);
            return ret;
        }

        protected IEnumerator Request<T>(RestMethodInfo methodInfo, string url, object[] arguments, Callback<T> cb)
        {
            var request = httpImpl.BuildRequest(methodInfo, url);
            InterceptRequest(request);
            var cd = new CoroutineWithData(this, httpImpl.SendRequest(this, request));
            yield return cd.coroutine;
            string errorMessage;
            if (httpImpl.IsRequestError(cd.result, out errorMessage))
            {
                cb.errorCB(errorMessage);
                yield break;
            }
            var result = httpImpl.GetSuccessResponse(cd.result);
            //                        result = "[]";
            //                        result = "[asd..s]";
            Log("Response:" + result);

            //Parse Json By Type
            if (typeof(T) == typeof(string))
            {
                var response = (T) (object) result;
                cb.successCB(response);
                yield break;
            }
            var data = default(T);
            var formatError = false;
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
                cb.successCB(data);
        }

        private string ParseRestParameters(RestMethodInfo methodInfo, object[] arguments)
        {
            var url = baseUrl + methodInfo.Path;
            //if param has QUERY attribute, add the fileds(POST) or URL parameteters(GET)
            //if param has PATH attribute, parse the URL
            var pathParamNames = new List<string>();
            var pathParamArgs = new List<object>();
            var queryParamNames = new List<string>();
            var queryParamArgs = new List<object>();
            var fieldParamNames = new List<string>();
            var fieldParamArgs = new List<object>();
            var headerParamNames = new List<string>();
            var headerParamArgs = new List<string>();
            object body = null;
            object part = null;
            var queryMap = new Dictionary<string, string>();
            var i = 0;
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
                    body = arguments[i];
                if (paramType == RestMethodInfo.ParamUsage.QueryMap)
                    queryMap = arguments[i] as Dictionary<string, string>;
                if (paramType == RestMethodInfo.ParamUsage.Header)
                {
                    headerParamNames.Add(methodInfo.ParameterNames[i]);
                    headerParamArgs.Add(arguments[i] as string);
                }
                if (methodInfo.IsMultipart && paramType == RestMethodInfo.ParamUsage.Part)
                    part = arguments[i];
                i++;
            }
            //replace PATH in URL
            if (pathParamNames.Count > 0)
            {
                var tmpUrl = url;
                var j = 0;
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
            var hasQuery = false;
            if (queryParamNames.Count > 0)
            {
                hasQuery = true;
                var sb = new StringBuilder(url);
                sb.Append("?");
                var l = 0;
                foreach (var queryParamName in queryParamNames)
                {
                    if (l > 0)
                        sb.Append("&");
                    sb.Append(queryParamName);
                    sb.Append("=");
                    sb.Append(queryParamArgs[l]);
                    l++;
                }
                url = sb.ToString();
                Log("parse QUERY　url:" + url);
            }
            //add QUERYMAP in URL
            if (queryMap != null && methodInfo.Method == Method.Get && queryMap.Count > 0)
            {
                var sb = new StringBuilder(url);
                if (!hasQuery)
                    sb.Append("?");
                var l = 0;
                foreach (var keyValuePair in queryMap)
                {
                    if (l > 0 || hasQuery)
                        sb.Append("&");
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
            var bodyString = body != null ? convert.ToBody(body) : string.Empty;
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
                interceptor.Intercept(request);
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
                            theAttribute => theAttribute.GetType() == typeof(RestMethodAttribute)) as RestMethodAttribute;
                    if (methodAttribute != null)
                    {
                        info.Method = methodAttribute.Method;
                        var valueAttribute = attribute as ValueAttribute;
                        info.Path = valueAttribute.Value;
                    }
                }

                if (attribute is MultipartAttribute)
                    info.IsMultipart = true;
            }
        }

        protected void ParseParameters(MethodBase methodInfo, RestMethodInfo info)
        {
            foreach (var parameter in methodInfo.GetParameters())
            {
                var attribute = (ValueAttribute) parameter.GetCustomAttributes(false).FirstOrDefault();


                if (attribute == null)
                {
                    var isCallback = parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(Callback<>);
                    if (!isCallback)
                        Log("No annotation found on parameter " + parameter.Name + " of " + methodInfo.Name);
                    continue;
                }
                var type = attribute.GetType();
                if (type == typeof(PathAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Path);
                    info.ParameterNames.Add(attribute.Value);
                }
                else if (type == typeof(BodyAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Body);
                    info.ParameterNames.Add(null);
                    info.HasBody = true;
                }
                else if (type == typeof(QueryAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Query);
                    info.ParameterNames.Add(attribute.Value);
                }
                else if (type == typeof(QueryMapAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.QueryMap);
                    info.ParameterNames.Add(attribute.Value);
                }
                else if (type == typeof(FieldAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Field);
                    info.ParameterNames.Add(attribute.Value);
                }
                else if (type == typeof(HeaderAttribute))
                {
                    info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Header);
                    info.ParameterNames.Add(attribute.Value);
                }
                else if (type == typeof(PartAttribute))
                {
                    if (!info.IsMultipart)
                        throw new ArgumentException("[Part] parameters can only be used with multipart encoding.");
                    if (parameter.ParameterType == typeof(FileInfo) ||
                        parameter.ParameterType == typeof(MultipartBody))
                    {
                        info.ParameterUsage.Add(RestMethodInfo.ParamUsage.Part);
                        info.ParameterNames.Add(null);
                    }
                    else
                    {
                        //rule2:[Part] parameter type can only be FileInfo or string or MultipartBody
                        throw new ArgumentException("[Part] parameter type can only be FileInfo or string or MultipartBody.");
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
                    throw new ArgumentException("Multipart requests can only contain One Part parameter at most");
                if (partParams.Count < 1)
                    throw new ArgumentException("Multipart must contain Part parameter");
                if (info.HasBody)
                    throw new ArgumentException("Multipart requests may not contain a Body parameter");
                info.GotPart = true;
            }
        }

        protected void Log(string log)
        {
            if (enableDebug)
                Debug.Log(log);
        }

        public class Builder
        {
            private bool enableLog;
            private string goAlias;
            private string baseUrl;
            private Converter.Converter converter;
            private ErrorHandler errorHandler;
            private HttpImplement httpImpl;
            private RequestInterceptor requestInterceptor;
            public Builder EnableLog(bool enable)
            {
                this.enableLog = enable;
                return this;
            }
            /// <summary>
            /// set the GameObject alias name
            /// </summary>
            /// <param name="goAlias"></param>
            /// <returns></returns>
            public Builder SetGoAlias(string goAlias)
            {
                this.goAlias = goAlias;
                return this;
            }
            /// <summary>
            /// set API endpoint URL
            /// </summary>
            /// <param name="baseUrl"></param>
            /// <returns></returns>
            public Builder SetEndpoint(string baseUrl)
            {
                if (string.IsNullOrEmpty(baseUrl))
                    throw new ArgumentException("BaseUrl may not be blank.");
                this.baseUrl = baseUrl;
                return this;
            }

            /// <summary>
            /// The HTTP client used for requests.
            /// </summary>
            /// <param name="client"></param>
            /// <returns></returns>
            public Builder SetClient(HttpImplement client)
            {
                if (client == null)
                    throw new ArgumentException("Client may not be null.");
                httpImpl = client;
                return this;
            }

            /// <summary>
            /// A request interceptor for adding data to every request.
            /// </summary>
            /// <param name="requestInterceptor"></param>
            /// <returns></returns>
            public Builder SetRequestInterceptor(RequestInterceptor requestInterceptor)
            {
                if (requestInterceptor == null)
                    throw new ArgumentException("Request interceptor may not be null.");
                this.requestInterceptor = requestInterceptor;
                return this;
            }

            /// <summary>
            /// The converter used for serialization and deserialization of objects. 
            /// </summary>
            /// <param name="converter"></param>
            /// <returns></returns>
            public Builder SetConverter(Converter.Converter converter)
            {
                if (converter == null)
                    throw new ArgumentException("Converter may not be null.");
                this.converter = converter;
                return this;
            }

            /// <summary>
            /// The error handler allows you to customize the type of exception thrown for errors on
            /// synchronous requests.
            /// </summary>
            /// <param name="errorHandler"></param>
            /// <returns></returns>
            public Builder SetErrorHandler(ErrorHandler errorHandler)
            {
                if (errorHandler == null)
                    throw new ArgumentException("Error handler may not be null.");
                this.errorHandler = errorHandler;
                return this;
            }


            /// <summary>
            /// Create the RetrofitAdapter instances.
            /// </summary>
            /// <returns></returns>
            public RetrofitAdapter Build()
            {
                if (baseUrl == null)
                    throw new ArgumentException("BaseUrl may not be null.");
                EnsureSaneDefaults();
                var go = new GameObject(goAlias.IsNullOrEmpty()?baseUrl:goAlias);
                var restAdapter = go.AddComponent<RetrofitAdapter>();
                restAdapter.Init(enableLog,baseUrl, httpImpl, requestInterceptor, converter, errorHandler);
                return restAdapter;
            }

            private void EnsureSaneDefaults()
            {
                if (converter == null)
                    converter = new DefalutConvert();
                if (httpImpl == null)
                    httpImpl = new HttpClientImpl();
                if (errorHandler == null)
                    errorHandler = new DefaultErrorHandler();
                if (requestInterceptor == null)
                    requestInterceptor = new DefaultRequestInterceptor();
            }
        }
    }
}