#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
namespace Retrofit.Methods
{
    [RestMethod(Method.Head)]
    public class HeadAttribute : ValueAttribute
    {
        public HeadAttribute(string path)
        {
            this.Value = path;
        }
    }
}