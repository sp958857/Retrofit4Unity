#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
using System.Collections;
using UnityEngine;

namespace Retrofit.HttpImpl
{
    public class WWWHttpImpl: HttpImplement
    {
        public object BuildRequest(RestMethodInfo methodInfo, string url)
        {
            WWW www = new WWW(url);
            return www;
        }

        public IEnumerator SendRequest(MonoBehaviour owner, object request)
        {
            WWW www = request as WWW;
            yield return www;
        }

        public bool IsRequestError(object result, out string errorMessage)
        {
            bool isError = true;
            errorMessage = string.Empty;
            WWW www = result as WWW;
            if (string.IsNullOrEmpty(www.error))
            {
                isError = false;
            }
            else
            {
                errorMessage = www.error;
            }
            return isError;
        }

        public string GetSuccessResponse(object result)
        {
            WWW www = result as WWW;
            return www.text;
        }
    }
}