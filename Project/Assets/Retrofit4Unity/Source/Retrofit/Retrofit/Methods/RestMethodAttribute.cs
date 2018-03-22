#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
using System;
namespace Retrofit.Methods
{
    public class RestMethodAttribute : Attribute
    {
        public Method Method { get; private set; }

        public RestMethodAttribute(Method method)
        {
            this.Method = method;
        }
    }
}