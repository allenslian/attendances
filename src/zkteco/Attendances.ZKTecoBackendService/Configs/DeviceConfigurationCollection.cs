using System.Configuration;

namespace Attendances.ZKTecoBackendService.Configs
{
    public class DeviceConfigurationCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new DeviceConfiguration();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var config = (DeviceConfiguration)element;
            return config.Name;
        }

        public DeviceConfiguration this[int index]
        {
            get { return (DeviceConfiguration)BaseGet(index); }
        }

        protected override string ElementName
        {
            get { return "device"; }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMapAlternate; }
        }
    }
}
