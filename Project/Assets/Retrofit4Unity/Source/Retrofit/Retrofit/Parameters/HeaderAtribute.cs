#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
namespace Retrofit.Parameters
{
    public class HeaderAttribute : ValueAttribute
    {
        public HeaderAttribute(string value)
        {
            this.Value = value;
        }
    }
}