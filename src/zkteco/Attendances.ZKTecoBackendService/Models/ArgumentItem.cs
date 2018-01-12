using Attendances.ZKTecoBackendService.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Attendances.ZKTecoBackendService.Models
{
    public class ArgumentItem : IIdentityKey
    {
        private List<ArgumentValuePair> _pairs;

        public ArgumentItem(string id, params ArgumentValuePair[] pairs)
        {
            Id = id;

            _pairs = new List<ArgumentValuePair>(10);
            _pairs.AddRange(pairs);
        }

        public string Id { get; private set; }        

        public object this[string name]
        {
            get
            {
                var found = _pairs.FirstOrDefault(p => p.Key == name);
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
