#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion

namespace Retrofit.Converter
{
    public interface Converter
    {

        T FromBody<T>(string body);
        string ToBody(object data);
    }
}