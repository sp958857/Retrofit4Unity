#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
namespace Retrofit
{
    public class Callback<T>
    {
        public delegate void OnRequestSuccessDelegate(T response);
        public delegate void OnRequestErrorDelegate(string error);

        public OnRequestErrorDelegate errorCB { get; set; }
        public OnRequestSuccessDelegate successCB { get; set; }
       
    }
}