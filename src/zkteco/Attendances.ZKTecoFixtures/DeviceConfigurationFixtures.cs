using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using Attendances.ZKTecoBackendService.Configs;

namespace Attendances.ZKTecoFixtures
{
    [TestClass]
    public class DeviceConfigurationFixtures
    {
        [TestMethod]
        public void TestReadAttributeFromConfigFile()
        {
            var group = ConfigurationManager.GetSection("deviceGroup") as DeviceConfigurationSectionHandler;
            Assert.IsNotNull(group);
            Assert.IsTrue(group.Devices.Count > 0);
            var config = group.Devices[0] as DeviceConfiguration;
            Assert.IsNotNull(config);
            Assert.AreEqual("127.0.0.1", config.IP);
            Assert.IsTrue(config.Name == "gate01");
            Assert.AreEqual(-1, config.Password);
        }
    }
}
