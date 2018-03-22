using Retrofit;
using Retrofit.Methods;
using Retrofit.Parameters;
using UniRx;

namespace Demo.Scripts
{
    public interface IHttpBinInterface
    {
        [Get("/get")]
        IObservable<HttpBinResponse> Get(
            [Query("query1")]string arg1,
            [Query("query2")]string arg2
            );

        [Post("/post")]
        IObservable<HttpBinResponse> Post(
            [Field("form-data-field1")]float fieldArg1,
            [Field("form-data-field2")]string fieldArg2
            );

        [Post("/post")]
        [Headers("time:2018-3-21")]
        IObservable<HttpBinResponse> PostBody(
            [Body]PostBody postBody,
            [Header("client")]string client
        );

        [Multipart]
        [Post("/post")]
        IObservable<HttpBinResponse> MultipartFileUpload(
            [Part]MultipartBody body,
            [Field("form-data-field1")]float fieldArg1,
            [Field("form-data-field2")]string fieldArg2
        );

        [Patch("/patch")]
        IObservable<HttpBinResponse> Patch(
            [Field("form-data-field1")]float fieldArg1
        );

        [Put("/put")]
        IObservable<HttpBinResponse> Put(
            [Field("form-data-field1")]float fieldArg1,
            [Field("form-data-field2")]string fieldArg2
        );

        [Delete("/delete")]
        IObservable<HttpBinResponse> Delete();

        //Delays responding for min(n, 10) seconds.
        [Get("/delay/{seconds}")]
        IObservable<HttpBinResponse> PathTest(
            [Path("seconds")]int seconds);

    }
}