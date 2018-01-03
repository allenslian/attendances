using Attendances.BackendService.Models;
using System.Configuration;

namespace Attendances.BackendService.Configs
{
    public class DeviceConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("name",
            IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("ip",
            IsRequired = true,
            IsKey = true)]
        public string IP
        {
            get { return (string)this["ip"]; }
            set { this["ip"] = value; }
        }

        [ConfigurationProperty("port",
            DefaultValue = 4370,
            IsRequired = false)]
        public int Port
        {
            get { return (int)this["port"]; }
            set { this["port"] = value; }
        }

        [ConfigurationProperty("type",
            IsRequired = true)]
        public DeviceType Type
        {
            get { return (DeviceType)this["type"]; }
            set { this["type"] = value; }
        }
    }
}
