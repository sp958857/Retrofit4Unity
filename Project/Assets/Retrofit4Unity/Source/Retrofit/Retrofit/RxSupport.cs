#region License

// Author: Weichao Wang     
// Start Date: 2017-05-24

#endregion

using System;
using System.Threading;
using Retrofit.HttpImpl;
using UniRx;
using UnityEngine;

namespace Retrofit
{
    public class RxSupport
    {
        private Converter.Converter convert;
        private RxHttpImplement rxHttpImpl;
        private RequestInterceptor interceptor;

        public RxSupport(Converter.Converter convert, HttpImplement httpImpl, RequestInterceptor interceptor)
        {
            this.convert = convert;
            this.rxHttpImpl = httpImpl as RxHttpImplement;
            this.interceptor = interceptor;
        }

        public static bool IsObservable(Type rawType)
        {
            return rawType == typeof (IObservable<>);
        }
        public IObservable<T> CreateRequestObservable<T>(RestMethodInfo methodInfo, string url,object[] arguments )
        {
            var ob = Observable.Create<T>(o =>
            {
                object request = rxHttpImpl.RxBuildRequest(o, convert, methodInfo, url);
                if (interceptor != null)
                {
                    interceptor.Intercept(request);
                }
                rxHttpImpl.RxSendRequest(o, convert, request);
                return Disposable.Create((() => rxHttpImpl.Cancel(request)));
            });
            return ob;
        }
    }
}