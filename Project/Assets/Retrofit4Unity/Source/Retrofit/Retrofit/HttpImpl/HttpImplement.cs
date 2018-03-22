#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
using System.Collections;
using UnityEngine;

namespace Retrofit.HttpImpl
{
    public interface HttpImplement
    {

        object BuildRequest(RestMethodInfo methodInfo, string url);
        IEnumerator SendRequest(MonoBehaviour owner, object request);
        bool IsRequestError(object result, out string errorMessage);
        string GetSuccessResponse(object result);

    }
}