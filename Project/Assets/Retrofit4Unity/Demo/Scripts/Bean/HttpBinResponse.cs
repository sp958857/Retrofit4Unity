using Newtonsoft.Json;

namespace Demo.Scripts
{
    public class HttpBinResponse
    {

        [JsonProperty("args")]
        public QueryArgs queryArgs;
        [JsonProperty("data")]
        public string data;
        [JsonProperty("files")]
        public Files files;
        [JsonProperty("form")]
        public FormData formData;
        [JsonProperty("headers")]
        public Headers headers;
        [JsonProperty("origin")]
        public string originIp;
        [JsonProperty("url")]
        public string url;
    }

    public class Files
    {
        [JsonProperty("file")]
        public string file;
    }

    public class QueryArgs
    {
        [JsonProperty("query1")]
        public string arg1;
        [JsonProperty("query2")]
        public string arg2;
    }

    public class FormData
    {
        [JsonProperty("form-data-field1")]
        public string arg1;
        [JsonProperty("form-data-field2")]
        public string arg2;

        public override string ToString()
        {
            return @"{""form-data-field1"":""" + arg1+ @""",""form - data - field2"":"""+arg2+@"""}";
        }
    }

    public class Headers
    {
        [JsonProperty("Host")]
        public string host;
        [JsonProperty("User-Agent")]
        public string userAgent;
    }
}