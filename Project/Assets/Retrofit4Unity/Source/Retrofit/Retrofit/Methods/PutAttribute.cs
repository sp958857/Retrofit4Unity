#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
namespace Retrofit.Methods
{
    [RestMethod(Method.Put)]
    public class PutAttribute : ValueAttribute
    {
        public PutAttribute(string path)
        {
            this.Value = path;
        }
    }
}