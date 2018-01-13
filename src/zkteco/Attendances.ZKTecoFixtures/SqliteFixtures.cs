using Microsoft.VisualStudio.TestTools.UnitTesting;
using Attendances.ZKTecoBackendService.Connectors;
using Attendances.ZKTecoBackendService.Utils;
using Moq;
using Attendances.ZKTecoBackendService.Interfaces;
using System;
using Attendances.ZKTecoBackendService.Models;
using System.Collections.Generic;
using Topshelf.Logging;
using Serilog;

namespace Attendances.ZKTecoFixtures
{
    [TestClass]
    public class SqliteFixtures
    {
        private ILogger _logger;

        [TestInitialize]
        public void InitDatabase()
        {
            DbInstaller.Install();

            _logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        }

        [TestMethod]
        public void TestCurrentWorkerIdFound()
        {
            var bundle = new Bundle(new SqliteConnector(), new WebApiConnector());
            var workerId = bundle.GetCurrentWorkerId("1", GlobalConfig.ProjectCode);
            Assert.IsTrue(workerId.Length > 0);
        }

        [TestMethod]
        public void TestCheckInToCTMSSuccess()
        {
            HostLogger.UseLogger(new SerilogLogWriterFactory.SerilogHostLoggerConfigurator(_logger));

            var mockApi = new Mock<IWebApiConnector>();
            mockApi.Setup(api => api.CheckIn(GlobalConfig.ProjectCode, "123", new DateTime(2018, 1, 13, 8, 11, 30), "gate01")).ReturnsAsync(true);

            var attendance = new AttendanceLog("1", 1, 1, 2018, 1, 13, 8, 11, 30, 1, 1, "gate01", DeviceType.In);
            var bundle = new Bundle(new SqliteConnector(), mockApi.Object);
            SaveAttendanceLog(bundle, attendance);
            bundle.CheckInToCTMS(attendance, "123");
            var sync = bundle.Database.QueryScalar("select sync from attendance_logs where id=@id;",
                new Dictionary<string, object>
                {
                    { "@id", attendance.Id }
                });
            Assert.AreEqual(1, sync);
        }

        [TestMethod]
        public void TestCheckInToCTMSFailed()
        {
            HostLogger.UseLogger(new SerilogLogWriterFactory.SerilogHostLoggerConfigurator(_logger));

            var mockApi = new Mock<IWebApiConnector>();
            mockApi.Setup(api => api.CheckIn(GlobalConfig.ProjectCode, "123", new DateTime(2018, 1, 13, 8, 11, 30), "gate01")).ReturnsAsync(false);

            var attendance = new AttendanceLog("1", 1, 1, 2018, 1, 13, 8, 11, 30, 1, 1, "gate01", DeviceType.In);
            var bundle = new Bundle(new SqliteConnector(), mockApi.Object);
            SaveAttendanceLog(bundle, attendance);
            bundle.CheckInToCTMS(attendance, "123");
            var sync = bundle.Database.QueryScalar("select sync from attendance_logs where id=@id;",
                new Dictionary<string, object>
                {
                    { "@id", attendance.Id }
                });
            Assert.AreEqual(0, sync);
        }

        [TestMethod]
        public void TestCheckOutToCTMSSuccess()
        {
            HostLogger.UseLogger(new SerilogLogWriterFactory.SerilogHostLoggerConfigurator(_logger));

            var mockApi = new Mock<IWebApiConnector>();
            mockApi.Setup(api => api.CheckOut(GlobalConfig.ProjectCode, "123", new DateTime(2018, 1, 13, 10, 11, 30))).ReturnsAsync(true);

            var attendance = new AttendanceLog("1", 1, 1, 2018, 1, 13, 10, 11, 30, 1, 1, "gate01", DeviceType.Out);
            var bundle = new Bundle(new SqliteConnector(), mockApi.Object);
            SaveAttendanceLog(bundle, attendance);
            bundle.CheckOutToCTMS(attendance, "123");
            var sync = bundle.Database.QueryScalar("select sync from attendance_logs where id=@id;",
                new Dictionary<string, object>
                {
                    { "@id", attendance.Id }
                });
            Assert.AreEqual(1, sync);
        }

        [TestMethod]
        public void TestCheckOutToCTMSFailed()
        {
            HostLogger.UseLogger(new SerilogLogWriterFactory.SerilogHostLoggerConfigurator(_logger));

            var mockApi = new Mock<IWebApiConnector>();
            mockApi.Setup(api => api.CheckOut(GlobalConfig.ProjectCode, "123", new DateTime(2018, 1, 13, 10, 11, 30))).ReturnsAsync(false);

            var attendance = new AttendanceLog("1", 1, 1, 2018, 1, 13, 10, 11, 30, 1, 1, "gate01", DeviceType.Out);
            var bundle = new Bundle(new SqliteConnector(), mockApi.Object);
            SaveAttendanceLog(bundle, attendance);
            bundle.CheckOutToCTMS(attendance, "123");
            var sync = bundle.Database.QueryScalar("select sync from attendance_logs where id=@id;",
                new Dictionary<string, object>
                {
                    { "@id", attendance.Id }
                });
            Assert.AreEqual(0, sync);
        }

        private void SaveAttendanceLog(Bundle bundle, AttendanceLog attendance)
        {
            var result = bundle.Database.QueryScalar(
                "select count(*) from attendance_logs where id=@id",
                new Dictionary<string, object>
                {
                    { "@id", attendance.Id }
                });
            var count = Convert.ToInt32(result);
            if (count == 0)
            {
                bundle.Database.Execute(
                    @"INSERT INTO attendance_logs(id,machine_id,enroll_number,project_id,log_date,mode,state,work_code,
                    sync,device_name,device_type,log_status) VALUES(@id,@machine_id,@enroll_number,@project_id, 
                    @log_date,@mode,@state,@work_code,-1,@device_name,@device_type,@log_status);",
                    new Dictionary<string, object>
                    {
                        { "@id", attendance.Id },
                        { "@machine_id", attendance.MachineId },
                        { "@enroll_number", attendance.UserId },
                        { "@project_id", attendance.ProjectId },
                        { "@log_date", attendance.LogDate },
                        { "@mode", attendance.Mode },
                        { "@state", attendance.State },
                        { "@work_code", attendance.WorkCode },
                        { "@device_name", attendance.DeviceName },
                        { "@device_type", (int)attendance.DeviceType },
                        { "@log_status", (int)attendance.LogStatus }
                    });
            }
        }
    }
}
