using Attendances.ZKTecoBackendService.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Attendances.ZKTecoBackendService.Models
{
    public class ArgumentItem : IIdentityKey
    {
        /// <summary>
        /// For serializer
        /// </summary>
        public ArgumentItem() { }

        public ArgumentItem(string id, params ArgumentValuePair[] pairs)
        {
            Id = id;
            Pairs = new List<ArgumentValuePair>(10);
            Pairs.AddRange(pairs);
        }

        public string Id { get; set; }

        public List<ArgumentValuePair> Pairs { get; set; }

        public string this[string name]
        {
            get
            {
                var found = Pairs.FirstOrDefault(p => p.Key == name);
                if (found == null)
                {
                    return null;
                }
                return found.Value;
            }
        }        

        public class ArgumentValuePair
        {
            public ArgumentValuePair(string key, object value)
            {
                if (value == null)
                {
                    value = "";
                }

                Key = key;
                switch (value.GetType().Name)
                {
                    case "String":
                        Value = (string)value;
                        break;
                    case "Int32":
                        Value = ((int)value).ToString("D1");
                        break;
                    default:
                        Value = JsonConvert.SerializeObject(value);
                        break;
                }
            }

            public string Key { get; set; }

            public string Value { get; set; }
        }
    }
}
