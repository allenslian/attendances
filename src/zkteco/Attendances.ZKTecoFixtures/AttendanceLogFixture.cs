using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Attendances.ZKTecoBackendService.Models;

namespace Attendances.ZKTecoFixtures
{
    [TestClass]
    public class AttendanceLogFixture
    {
        [TestMethod]
        public void TestCheckInAndOut()
        {
            var prev = new AttendanceLog("6365298588900000001", "1", 15, 15, new DateTime(2018, 1, 31, 8, 58, 9), 1, 1, "592e2531b2ddc226f0df2b24", "gate02", DeviceType.InOut, AttendanceStatus.CheckIn);
            var next = new AttendanceLog("1", 15, 15, 2018, 1, 31, 18, 15, 16, 1, 1, "gate02", DeviceType.InOut);
            var status = next.CalculateStatus(prev);
            Assert.AreEqual(AttendanceStatus.CheckOut, status);
        }
    }
}
