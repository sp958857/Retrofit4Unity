#region License

// Author: Weichao Wang     
// Start Date: 2017-05-22

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retrofit.Methods;

namespace Retrofit
{
    public class RestMethodInfo
    {
        public MethodBase methodBase;
        public bool IsObservable;
        public ResponseType responseType;

        public Method Method { get; set; }
        //reletive url
        public string Path { get; set; }
        public bool IsMultipart { get; set; }
        public bool GotPart { get; set; }
        public bool HasBody { get; set; }
        public MultipartBody Part { get; set; }

        public List<ParamUsage> ParameterUsage = new List<ParamUsage>();
        public List<string> ParameterNames = new List<string>();
        public List<object> Arguments = new List<object>();
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
        public Dictionary<string, string> HeaderParameterMap = new Dictionary<string, string>();
        public Dictionary<string, object> FieldParameterMap = new Dictionary<string, object>();
        public string bodyString = string.Empty;


        public RestMethodInfo(MethodBase method)
        {
            methodBase = method;
            MethodInfo methodInfo = method as MethodInfo;
            Type returnType = methodInfo.ReturnType;
            Type firstParamType = methodInfo.GetParameters() !=null && methodInfo.GetParameters().Length >=1 ? methodInfo.GetParameters()[0].ParameterType:null;
            bool hasReturnType = returnType != typeof (void);
            bool firstParamIsCallback = firstParamType != null && firstParamType.IsGenericType && firstParamType.GetGenericTypeDefinition() == typeof(Callback<>);
            IsObservable = returnType.IsGenericType && RxSupport.IsObservable(returnType.GetGenericTypeDefinition());
            if (hasReturnType)
            {
                if (IsObservable)
                {
                    responseType = ResponseType.OBSERVABLE;
                }
                else
                {
                    throw new ArgumentException("Retrofit return type must be IObserable<>!");
                }
            }
            else
            {
                if (firstParamIsCallback)
                {
                    responseType = ResponseType.VOID;
                }
                else
                {
                    throw new ArgumentException("Sync Retrofit first parameter type must be Callback<>!");
                }
            }
        }

        public enum ParamUsage
        {
            Query,
            Path,
            Body,
            Field,
            QueryMap,
            Header,
            Part
        }

        public enum ResponseType
        {
            VOID,
            OBSERVABLE,
        }
    }
}