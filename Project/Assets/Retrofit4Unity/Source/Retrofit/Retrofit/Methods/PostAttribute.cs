#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
namespace Retrofit.Methods
{
    [RestMethod(Method.Post)]
    public class PostAttribute : ValueAttribute
    {
        public PostAttribute(string path)
        {
            this.Value = path;
        }
    }
}