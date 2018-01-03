using System.Configuration;

namespace Attendances.BackendService.Configs
{
    public class DeviceConfigurationSectionHandler : ConfigurationSection
    {
        [ConfigurationProperty("devices")]
        [ConfigurationCollection(typeof(DeviceConfigurationCollection))]
        public DeviceConfigurationCollection Devices
        {
            get { return (DeviceConfigurationCollection)base["devices"]; }
            set { base["devices"] = value; }
        }
    }
}
