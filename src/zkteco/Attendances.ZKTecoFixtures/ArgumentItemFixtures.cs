using Microsoft.VisualStudio.TestTools.UnitTesting;
using Attendances.ZKTecoBackendService.Models;
using Attendances.ZKTecoBackendService.Events;
using Newtonsoft.Json;

namespace Attendances.ZKTecoFixtures
{
    [TestClass]
    public class ArgumentItemFixtures
    {
        [TestMethod]
        public void TestIncreaseFailedCount()
        {
            var msg = new EventMessage(EventType.Failed,
                new AttendanceLog("1", 1, 1, 2018, 1, 13, 8, 0, 0, 1, 1, "gate01", DeviceType.In));
            var arg = new ArgumentItem("123", new ArgumentItem.ArgumentValuePair("Message", msg));
            Assert.IsTrue(arg.IncreaseFailedCount());
            Assert.IsTrue(arg.IncreaseFailedCount());

            var json = JsonConvert.SerializeObject(arg);
            var newArg = JsonConvert.DeserializeObject<ArgumentItem>(json);

            Assert.IsTrue(newArg.Count == 2);
            Assert.IsTrue(newArg.IncreaseFailedCount());
            Assert.IsTrue(newArg.IncreaseFailedCount());
            Assert.IsTrue(newArg.Count == 4);
            Assert.IsFalse(newArg.IncreaseFailedCount());
        }


        [TestMethod]
        public void TestArgumentValuePair()
        {
            var subArg = new ArgumentItem("456123");
            var arg = new ArgumentItem("1",
                new ArgumentItem.ArgumentValuePair("EnrollNumber", "1"),
                new ArgumentItem.ArgumentValuePair("FingerIndex", 100),
                new ArgumentItem.ArgumentValuePair("ActionResult", 1),
                new ArgumentItem.ArgumentValuePair("TemplateLength", 520),
                new ArgumentItem.ArgumentValuePair("SubItem", subArg));
            Assert.IsTrue(arg["EnrollNumber"] == "1");
            Assert.IsTrue(arg["FingerIndex"] == "100");
            Assert.IsTrue(arg["ActionResult"] == "1");
            Assert.IsTrue(arg["TemplateLength"] == "520");

            var newSubArg = JsonConvert.DeserializeObject<ArgumentItem>(arg["SubItem"]);
            Assert.IsTrue(newSubArg.Id == "456123");
        }
    }
}
