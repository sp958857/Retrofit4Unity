using Newtonsoft.Json;

namespace Demo.Scripts
{
    public class PostBody
    {
        [JsonProperty("author")]
        public string  author;
        [JsonProperty("country")]
        public string country;

        public PostBody(string author, string country)
        {
            this.author = author;
            this.country = country;
        }
    }
}