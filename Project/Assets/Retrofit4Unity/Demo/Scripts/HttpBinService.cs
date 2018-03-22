using Retrofit;
using Retrofit.HttpImpl;
using UniRx;
using UnityEngine;

namespace Demo.Scripts
{
    public class HttpBinService:RestAdapter,IHttpBinInterface
    {
        private static HttpBinService _instance;

        public static HttpBinService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("HttpBinService");
                    _instance = go.AddComponent<HttpBinService>();
                }
                return _instance;
            }
        }

        protected override RequestInterceptor SetIntercepter()
        {
            return null;
        }

        protected override HttpImplement SetHttpImpl()
        {
            var httpImpl = new HttpClientImpl();
            httpImpl.EnableDebug = true;
            return httpImpl;
        }

        protected override void SetRestAPI()
        {
            baseUrl = "http://httpbin.org";
            iRestInterface = typeof (IHttpBinInterface);
        }


        public IObservable<HttpBinResponse> Get(string arg1, string arg2)
        {
            return SendRequest<HttpBinResponse>(arg1,arg2) as IObservable<HttpBinResponse>;
        }

        public IObservable<HttpBinResponse> Post(float fieldArg1, string fieldArg2)
        {
            return SendRequest<HttpBinResponse>(fieldArg1, fieldArg2) as IObservable<HttpBinResponse>;
        }

        public IObservable<HttpBinResponse> PostBody(PostBody postBody, string client)
        {
            return SendRequest<HttpBinResponse>(postBody, client) as IObservable<HttpBinResponse>;
        }

        public IObservable<HttpBinResponse> MultipartFileUpload(MultipartBody body, float fieldArg1, string fieldArg2)
        {
            return SendRequest<HttpBinResponse>(body, fieldArg1, fieldArg2) as IObservable<HttpBinResponse>;
        }

        public IObservable<HttpBinResponse> Patch(float fieldArg1)
        {
            return SendRequest<HttpBinResponse>(fieldArg1) as IObservable<HttpBinResponse>;
        }

        public IObservable<HttpBinResponse> Put(float fieldArg1, string fieldArg2)
        {
            return SendRequest<HttpBinResponse>(fieldArg1, fieldArg2) as IObservable<HttpBinResponse>;
        }

        public IObservable<HttpBinResponse> Delete()
        {
            return SendRequest<HttpBinResponse>() as IObservable<HttpBinResponse>;
        }

        public IObservable<HttpBinResponse> PathTest(int seconds)
        {
            return SendRequest<HttpBinResponse>(seconds) as IObservable<HttpBinResponse>;

        }
    }
}