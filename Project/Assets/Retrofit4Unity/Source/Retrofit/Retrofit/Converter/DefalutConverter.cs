#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Retrofit.Converter
{
    public class DefalutConvert : Converter
    {
        public T FromBody<T>(string body)
        {
            bool isError = false;
            string errorMessage = "";
            T data = JsonConvert.DeserializeObject<T>(body,
                new JsonSerializerSettings
                {
                    Error = delegate (object sender, ErrorEventArgs args)
                    {
                        args.ErrorContext.Handled = true;
                        errorMessage = args.ErrorContext.Error.Message;
                        isError = true;
                    }
                });
            if (isError)
            {
               throw new ConversionException(errorMessage);
            }
            return data;
        }

        public string ToBody(object data)
        {
            string dataString = "";
            dataString = JsonConvert.SerializeObject(data);
            return dataString;
        }
    }
}