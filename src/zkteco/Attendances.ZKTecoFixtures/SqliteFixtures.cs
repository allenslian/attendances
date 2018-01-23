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

        private Mock<IWebApiConnector> mock;

        [TestInitialize]
        public void InitDatabase()
        {
            DbInstaller.Install();

            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("./log.txt").CreateLogger();
            HostLogger.UseLogger(new SerilogLogWriterFactory.SerilogHostLoggerConfigurator(_logger));

            mock = new Mock<IWebApiConnector>();
            mock.Setup(api => api.FindProjectWorkerByFaceId("2", GlobalConfig.ProjectCode)).ReturnsAsync("2222222");
            mock.Setup(api => api.CheckIn(GlobalConfig.ProjectCode, "2222222", new DateTime(2018, 1, 13, 8, 11, 30), "gate01")).ReturnsAsync(true);
            mock.Setup(api => api.CheckIn(GlobalConfig.ProjectCode, "2222222", new DateTime(2018, 1, 13, 12, 11, 30), "gate01")).ReturnsAsync(false);
            mock.Setup(api => api.CheckOut(GlobalConfig.ProjectCode, "2222222", new DateTime(2018, 1, 13, 10, 11, 30))).ReturnsAsync(true);
            mock.Setup(api => api.CheckOut(GlobalConfig.ProjectCode, "2222222", new DateTime(2018, 1, 13, 14, 11, 30))).ReturnsAsync(false);
        }

        [TestMethod]
        public void TestCurrentWorkerIdFound()
        {
            var bundle = new Bundle(new SqliteConnector(), mock.Object);
            var workerId = bundle.GetCurrentWorkerId("2", GlobalConfig.ProjectCode);
            Assert.IsTrue(workerId.Length > 0);
            Assert.IsTrue(workerId == "2222222");
        }

        [TestMethod]
        public void TestCheckInToCTMSSuccess()
        {           
            var attendance = new AttendanceLog("2", 1, 1, 2018, 1, 13, 8, 11, 30, 1, 1, "gate01", DeviceType.In);
            var bundle = new Bundle(new SqliteConnector(), mock.Object);
            SaveAttendanceLog(bundle, attendance);
            bundle.CheckInToCTMS(attendance, "2222222");
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
            var attendance = new AttendanceLog("2", 1, 1, 2018, 1, 13, 12, 11, 30, 1, 1, "gate01", DeviceType.In);
            var bundle = new Bundle(new SqliteConnector(), mock.Object);
            SaveAttendanceLog(bundle, attendance);
            bundle.CheckInToCTMS(attendance, "2222222");
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
            var attendance = new AttendanceLog("2", 1, 1, 2018, 1, 13, 10, 11, 30, 1, 1, "gate01", DeviceType.Out);
            var bundle = new Bundle(new SqliteConnector(), mock.Object);
            SaveAttendanceLog(bundle, attendance);
            bundle.CheckOutToCTMS(attendance, "2222222");
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
            var attendance = new AttendanceLog("2", 1, 1, 2018, 1, 13, 14, 11, 30, 1, 1, "gate01", DeviceType.Out);
            var bundle = new Bundle(new SqliteConnector(), mock.Object);
            SaveAttendanceLog(bundle, attendance);
            bundle.CheckOutToCTMS(attendance, "2222222");
            var sync = bundle.Database.QueryScalar("select sync from attendance_logs where id=@id;",
                new Dictionary<string, object>
                {
                    { "@id", attendance.Id }
                });
            Assert.AreEqual(0, sync);
        }

        [TestMethod]
        public void TestGetLastAttendanceLog()
        {
            var attendance1 = new AttendanceLog("12", 15, 15, 2018, 1, 13, 14, 11, 30, 1, 1, "gate01", DeviceType.InOut);
            var bundle = new Bundle(new SqliteConnector(), mock.Object);
            SaveAttendanceLog(bundle, attendance1);
            var attendance2 = GetLastAttendanceLog(bundle, "12", attendance1.LogDate);
            Assert.IsNull(attendance2);

            var attendance3 = new AttendanceLog("3", 15, 15, 2018, 1, 12, 14, 11, 30, 1, 1, "gate01", DeviceType.InOut);
            SaveAttendanceLog(bundle, attendance3);

            var attendance4 = new AttendanceLog("3", 15, 15, 2018, 1, 16, 14, 11, 30, 1, 1, "gate01", DeviceType.InOut);
            SaveAttendanceLog(bundle, attendance4);

            var attendance5 = new AttendanceLog("3", 15, 15, 2018, 1, 15, 14, 11, 30, 1, 1, "gate01", DeviceType.InOut);
            SaveAttendanceLog(bundle, attendance5);

            var attendance6 = new AttendanceLog("3", 15, 15, 2018, 1, 13, 14, 11, 30, 1, 1, "gate01", DeviceType.InOut);
            SaveAttendanceLog(bundle, attendance6);

            var attendance = GetLastAttendanceLog(bundle, "3", attendance5.LogDate);
            Assert.IsNotNull(attendance);
            Assert.AreEqual(attendance.LogDate, attendance6.LogDate);
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

        private AttendanceLog GetLastAttendanceLog(Bundle bundle, string enrollNumber, DateTime logDate)
        {
            var reader = bundle.Database.QuerySet(
                @"SELECT id, enroll_number, state, mode, log_date, work_code,
                machine_id, project_id, ifnull(device_name,''), ifnull(device_type,0), log_status FROM attendance_logs 
                WHERE enroll_number=@enroll_number AND log_date<@cur_date
                ORDER BY log_date DESC LIMIT 1;",
                new Dictionary<string, object>
                {
                    { "@enroll_number", enrollNumber },
                    { "@cur_date", logDate}
                });

            AttendanceLog log = null;
            while (reader.Read())
            {
                log = new AttendanceLog(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    reader.GetDateTime(4),
                    reader.GetInt32(5),
                    reader.GetInt32(6),
                    reader.GetString(7),
                    reader.GetString(8),
                    (DeviceType)reader.GetInt32(9),
                    (AttendanceStatus)reader.GetInt32(10));
                break;
            }
            return log;
        }
    }
}
