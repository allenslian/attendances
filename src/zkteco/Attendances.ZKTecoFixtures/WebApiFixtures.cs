using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Attendances.ZKTecoBackendService.Utils;
using System.Net;
using Attendances.ZKTecoBackendService.Interfaces;
using Moq;

namespace Attendances.ZKTecoFixtures
{
    [TestClass]
    public class WebApiFixtures
    {
        private Mock<IWebApiConnector> _mock;

        [TestInitialize]
        public void InitMock()
        {
            _mock = new Mock<IWebApiConnector>();
        }

        [TestMethod]
        public void TestWorkerFound()
        {
            _mock.Setup(api => api.FindProjectWorkerByFaceId("1", GlobalConfig.ProjectCode))
                .ReturnsAsync("5a42f4890407c8536a7b1776");
            var id = _mock.Object.FindProjectWorkerByFaceId("1", GlobalConfig.ProjectCode).GetAwaiter().GetResult();
            Assert.IsFalse(string.IsNullOrWhiteSpace(id));
            Assert.IsTrue(id.Length == 24);
            Assert.IsTrue(id == "5a42f4890407c8536a7b1776");
        }

        [TestMethod]
        [ExpectedException(typeof(WebException))]
        public void TestWorkerNotFound()
        {
            _mock.Setup(api => api.FindProjectWorkerByFaceId("120", GlobalConfig.ProjectCode))
                .Throws(new WebException("Bad Request"));
            _mock.Object.FindProjectWorkerByFaceId("120", GlobalConfig.ProjectCode);
        }

        [TestMethod]
        public void TestCheckInSuccess()
        {
            _mock.Setup(api => api.CheckIn(GlobalConfig.ProjectCode, "5a42f4890407c8536a7b1776", DateTime.UtcNow.Date, "gate01")).ReturnsAsync(true);
            var result = _mock.Object.CheckIn(GlobalConfig.ProjectCode, "5a42f4890407c8536a7b1776", DateTime.UtcNow.Date, "gate01").GetAwaiter().GetResult();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCheckInFailed()
        {
            _mock.Setup(api => api.CheckIn("", "", DateTime.UtcNow, "gate01")).ReturnsAsync(false);
            var result = _mock.Object.CheckIn("", "", DateTime.UtcNow, "gate01").
                GetAwaiter().GetResult();
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestCheckOutSuccess()
        {
            _mock.Setup(api => api.CheckOut(GlobalConfig.ProjectCode, "5a42f4890407c8536a7b1776", DateTime.UtcNow.Date)).ReturnsAsync(true);
            var result = _mock.Object.CheckOut(GlobalConfig.ProjectCode, "5a42f4890407c8536a7b1776", DateTime.UtcNow.Date)
                .GetAwaiter().GetResult();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCheckOutFailed()
        {
            _mock.Setup(api => api.CheckOut("", "", DateTime.UtcNow.Date)).ReturnsAsync(false);
            var result = _mock.Object.CheckOut("", "", DateTime.UtcNow.Date).
                GetAwaiter().GetResult();
            Assert.IsFalse(result);
        }

        [TestMethod]
        [ExpectedException(typeof(WebException))]
        public void TestRealGetFaceIdAPIFailed()
        {
            var api = new Attendances.ZKTecoBackendService.Connectors.WebApiConnector();
            var worker = api.FindProjectWorkerByFaceId("10", "592e2531b2ddc226f0df2b24").GetAwaiter().GetResult();
            Assert.AreEqual("", worker);
        }
    }
}
