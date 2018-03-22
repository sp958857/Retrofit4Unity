#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
namespace Retrofit.Methods
{
    [RestMethod(Method.Get)]
    public class GetAttribute : ValueAttribute
    {
        public GetAttribute(string path)
        {
            this.Value = path;
        }
    }
}