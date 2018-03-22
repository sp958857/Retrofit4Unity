#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
namespace Retrofit.Methods
{
    [RestMethod(Method.Patch)]
    public class PatchAttribute : ValueAttribute
    {
        public PatchAttribute(string path)
        {
            this.Value = path;
        }
    }
}