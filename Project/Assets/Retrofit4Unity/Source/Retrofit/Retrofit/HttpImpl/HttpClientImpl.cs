using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using CI.HttpClient;
using Retrofit.Converter;
using UniRx;
using UnityEngine;

namespace Retrofit.HttpImpl
{
    public class HttpClientImpl : RxHttpImplement
    {
        public bool EnableDebug = false;
        [Obsolete]
        public object BuildRequest(RestMethodInfo methodInfo, string url)
        {
            throw new System.NotImplementedException();
        }

        [Obsolete]
        public IEnumerator SendRequest(MonoBehaviour owner, object request)
        {
            throw new System.NotImplementedException();
        }

        public void RxSendRequest<T>(IObserver<T> o, Converter.Converter convert, RestMethodInfo methodInfo, string url, ErrorHandler errorHandler, object request)
        {
            HttpClientRequest httpClientRequest = request as HttpClientRequest;
            if (httpClientRequest != null)
            {
                if (EnableDebug)
                {
                    Debug.LogFormat("Send on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
                }
                httpClientRequest.Send();
            }
        }

        private Exception HandleError(ErrorHandler errorHandler,RetrofitError retrofitError)
        {
            if (errorHandler != null)
            {
               return errorHandler.handleError(retrofitError);
            }
            return retrofitError;
        }
        public object RxBuildRequest<T>(IObserver<T> o, Converter.Converter convert, RestMethodInfo methodInfo, string url, ErrorHandler errorHandler)
        {
            Action<HttpResponseMessage<string>> responseMessage = message =>
            {
                string errorMessage = "";
                if (IsRequestError(message, out errorMessage))
                {
                    o.OnError(HandleError(errorHandler,RetrofitError.HttpError(url,errorMessage,convert,typeof(T))));
                    return;
                }
                string result = GetSuccessResponse(message);
//                                        result = "[]";
//                                        result = "[asd..s]";
                if (EnableDebug)
                {
                    Debug.LogFormat("Raw Response:{0}", result);
                }
                //Parse Json By Type
                if (typeof(T) == typeof(string))
                {
                    var resultData = (T) (object) result;
                    o.OnNext(resultData);
                    o.OnCompleted();
                    return;
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
                    o.OnError(HandleError(errorHandler, RetrofitError.ConversionError(url, e.Message, convert, typeof(T),e)));
                }
                if (!formatError)
                {
                    o.OnNext(data);
                    o.OnCompleted();
                }
            };
            HttpClientRequest httpClientRequest = new HttpClientRequest(new Uri(url), responseMessage);
            ConfigureRESTfulApi(methodInfo, httpClientRequest);
            return httpClientRequest;
        }

        public void Cancel(object request)
        {
            HttpClientRequest httpClientRequest = request as HttpClientRequest;
            if (httpClientRequest != null)
            {
                httpClientRequest.Abort();
            }
        }
        public string GetSuccessResponse(object message)
        {
            HttpResponseMessage<string> responseMessage = message as HttpResponseMessage<string>;
            return responseMessage.Data;
        }
        private static void ConfigureRESTfulApi(RestMethodInfo methodInfo, HttpClientRequest client)
        {
            //add headers
            if (methodInfo.Headers.Count > 0)
            {
                foreach (var keyValuePair in methodInfo.Headers)
                {
                    client.Request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }
            if (methodInfo.HeaderParameterMap.Count > 0)
            {
                foreach (var keyValuePair in methodInfo.HeaderParameterMap)
                {
                    client.Request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }
            HttpAction httpAction = (HttpAction) Enum.Parse(typeof(HttpAction), methodInfo.Method.ToString());
            client.SetMethod(httpAction);
            switch (httpAction)
            {
                case HttpAction.Delete:
                    break;
                case HttpAction.Get:
                    break;
                case HttpAction.Patch:
                case HttpAction.Post:
                case HttpAction.Put:
                    MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
                    FormUrlEncodedContent formUrlEncodedContent = null;
                    ByteArrayContent byteArrayContent = null;
                    if (methodInfo.IsMultipart && methodInfo.GotPart && methodInfo.Part != null)
                    {
                        //multipart bytes
                        byteArrayContent = new ByteArrayContent(methodInfo.Part.GetBinaryData(), methodInfo.Part.Mimetype);
                    }
                    if (methodInfo.FieldParameterMap.Count > 0)
                    {
                        //field
                        var fields = new Dictionary<string, string>();
                        foreach (var keyValuePair in methodInfo.FieldParameterMap)
                        {
                            fields.Add(keyValuePair.Key, keyValuePair.Value.ToString());
                        }
                        formUrlEncodedContent = new FormUrlEncodedContent(fields);
                    }

                    if (byteArrayContent != null)
                    {
                        //multipart/form-data; boundary=***
                        multipartFormDataContent.Add(byteArrayContent, methodInfo.Part.Field, methodInfo.Part.FileName);
                        if (formUrlEncodedContent != null)
                        {
                            foreach (var keyValuePair in methodInfo.FieldParameterMap)
                            {
                                StringContent stringContent = new StringContent(keyValuePair.Value.ToString());
                                multipartFormDataContent.Add(stringContent,keyValuePair.Key);
                            }
                            client.SetHttpContent(multipartFormDataContent);
                        }
                    }
                    else if(formUrlEncodedContent!=null)
                    {
                        client.SetHttpContent(formUrlEncodedContent);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (!string.IsNullOrEmpty(methodInfo.bodyString))
            {
                //raw body application/json
                StringContent content = new StringContent(methodInfo.bodyString, Encoding.UTF8, "application/json");
                client.SetHttpContent(content);
            }
//          client.SetProxy(new WebProxy(new Uri("http://127.0.0.1:8888")));
        }

        public bool IsRequestError(object message, out string errorMessage)
        {
            bool isError = true;
            errorMessage = string.Empty;
            HttpResponseMessage<string> responseMessage = message as HttpResponseMessage<string>;
            if (responseMessage.Data == null)
            {
                if (responseMessage.Exception != null)
                    errorMessage = responseMessage.Exception.Message + " url:" + responseMessage.OriginalRequest.RequestUri;
                else
                    errorMessage = "service error, url:" + responseMessage.OriginalRequest.RequestUri;
            }
            else if (!responseMessage.IsSuccessStatusCode)
            {
                errorMessage = "error code :" + responseMessage.StatusCode + " message : " + responseMessage.ReasonPhrase + " url:" + responseMessage.OriginalRequest.RequestUri;
            }
            else if (responseMessage.IsSuccessStatusCode)
            {
                isError = false;
            }
            return isError;
        }
    }
}