#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
namespace Retrofit.Methods
{
    public class HeadersAttribute:ValueAttribute
    {
        public string[] Headers { get; private set; }

        public HeadersAttribute(params string[] headers)
        {
            Headers = headers ?? new string[0];
        }
    }
}