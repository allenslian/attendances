using Attendances.ZKTecoBackendService.Models;
using System.Configuration;

namespace Attendances.ZKTecoBackendService.Configs
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
            get { return (string)base["ip"]; }
            set { base["ip"] = value; }
        }

        [ConfigurationProperty("port",
            DefaultValue = 4370,
            IsRequired = false)]
        public int Port
        {
            get { return (int)base["port"]; }
            set { base["port"] = value; }
        }

        [ConfigurationProperty("type",
            IsRequired = true)]
        public DeviceType Type
        {
            get { return (DeviceType)base["type"]; }
            set { base["type"] = value; }
        }
    }
}
