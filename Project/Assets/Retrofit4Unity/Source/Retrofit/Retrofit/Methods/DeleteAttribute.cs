#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
namespace Retrofit.Methods
{
    [RestMethod(Method.Delete)]
    public class DeleteAttribute : ValueAttribute
    {
        public DeleteAttribute(string path)
        {
            this.Value = path;
        }
    }
}