using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Serializers;
using System.IO;

namespace Attendances.ZKTecoBackendService.Utils
{
    /// <summary>
    /// A json serializer/deserializer for restsharp.
    /// </summary>
    public class JsonSerializer : ISerializer, IDeserializer
    {
        private Newtonsoft.Json.JsonSerializer _serializer;

        public JsonSerializer(Newtonsoft.Json.JsonSerializer serializer)
        {
            _serializer = serializer;
        }

        public string ContentType
        {
            get { return "application/json"; }
            set { }
        }

        public string DateFormat { get; set; }

        public string Namespace { get; set; }

        public string RootElement { get; set; }

        public string Serialize(object obj)
        {
            using (var stringWriter = new StringWriter())
            {
                using (var jsonTextWriter = new Newtonsoft.Json.JsonTextWriter(stringWriter))
                {
                    _serializer.Serialize(jsonTextWriter, obj);
                    return stringWriter.ToString();
                }
            }
        }

        public T Deserialize<T>(IRestResponse response)
        {
            var content = response.Content;
            using (var stringReader = new StringReader(content))
            {
                using (var jsonTextReader = new Newtonsoft.Json.JsonTextReader(stringReader))
                {
                    return _serializer.Deserialize<T>(jsonTextReader);
                }
            }
        }

        public static JsonSerializer Default
        {
            get
            {
                return new JsonSerializer(new Newtonsoft.Json.JsonSerializer()
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                });
            }
        }
    }
}
