using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Attendances.ZKTecoBackendService.Utils;
using Attendances.ZKTecoBackendService.Events;
using Attendances.ZKTecoBackendService.Connectors;
using Serilog;
using Attendances.ZKTecoBackendService.Models;
using Topshelf.Logging;
using System.Collections.Generic;
using Attendances.ZKTecoBackendService.Handlers;
using Attendances.ZKTecoBackendService.Interfaces;
using Moq;
using System.Threading;
using System.Net;

namespace Attendances.ZKTecoFixtures
{
    [TestClass]
    public class HandlerFixtures
    {
        private ILogger _logger;
        private Mock<IWebApiConnector> _mock;

        [TestInitialize]
        public void InitializeVars()
        {
            DbInstaller.Install();

            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("./log.txt").CreateLogger();
            HostLogger.UseLogger(new SerilogLogWriterFactory.SerilogHostLoggerConfigurator(_logger));

            _mock = new Mock<IWebApiConnector>();
            _mock.Setup(api => api.FindProjectWorkerByFaceId("4", GlobalConfig.ProjectCode)).ReturnsAsync("4444444");
            _mock.Setup(api => api.FindProjectWorkerByFaceId("2", GlobalConfig.ProjectCode)).ReturnsAsync("2222222");
            _mock.Setup(api => api.FindProjectWorkerByFaceId("3", GlobalConfig.ProjectCode)).Throws(new WebException("Bad Request"));

            _mock.Setup(api => api.CheckIn(GlobalConfig.ProjectCode, "2222222", new DateTime(2018, 1, 13, 12, 11, 30), "gate01")).ReturnsAsync(true);
            _mock.Setup(api => api.CheckOut(GlobalConfig.ProjectCode, "4444444", new DateTime(2018, 1, 13, 14, 11, 30))).ReturnsAsync(true);
            _mock.Setup(api => api.CheckIn(GlobalConfig.ProjectCode, "4444444", new DateTime(2018, 1, 13, 16, 11, 30), "gate01")).ReturnsAsync(false);
            _mock.Setup(api => api.CheckOut(GlobalConfig.ProjectCode, "2222222", new DateTime(2018, 1, 13, 18, 11, 30))).ReturnsAsync(false);
        }

        [TestMethod]
        public void TestPublishMessageToQueue()
        {
            // No any subscriber to handle the message.
            var db = new SqliteConnector();
            var hub = new EventHub(db);
            var attendance = new AttendanceLog("2", 1, 1, 2018, 1, 13, 12, 11, 30, 1, 1, "gate01", DeviceType.OnlyIn);
            hub.PublishAsync(new EventMessage(EventType.AttTransactionEx, attendance)).GetAwaiter().GetResult();

            var results = db.QueryScalar(
                "select count(*) from queue where refer_id=@refer_id",
                new Dictionary<string, object>
                {
                    { "@refer_id", attendance.Id }
                });

            var count = Convert.ToInt32(results);
            Assert.IsTrue(count > 0);
        }

        [TestMethod]
        public void TestUploadAttendanceHandlerOk()
        {
            var bundle = new Bundle(new SqliteConnector(), _mock.Object);
            var hub = new EventHub(bundle.Database);
            hub.Subscribe(EventType.AttTransactionEx, new UploadAttendanceHandler(bundle));

            var attendance = new AttendanceLog("2", 1, 1, 2018, 1, 13, 12, 11, 30, 1, 1, "gate01", DeviceType.OnlyIn);
            hub.PublishAsync(new EventMessage(EventType.AttTransactionEx, attendance)).GetAwaiter().GetResult();

            Thread.Sleep(30000);

            var results = bundle.Database.QueryScalar(
                "select count(*) from queue where refer_id=@refer_id",
                new Dictionary<string, object>
                {
                    { "@refer_id", attendance.Id }
                });

            var count = Convert.ToInt32(results);
            Assert.IsTrue(count == 0);

            results = bundle.Database.QueryScalar(
                "select sync from attendance_logs where id=@id",
                new Dictionary<string, object>
                {
                    { "@id", attendance.Id }
                });
            var sync = Convert.ToInt32(results);
            Assert.IsTrue(sync == 1);
        }

        [TestMethod]
        public void TestUploadAttendanceHandlerFailedToAttendance()
        {
            var bundle = new Bundle(new SqliteConnector(), _mock.Object);
            var hub = new EventHub(bundle.Database);
            hub.Subscribe(EventType.AttTransactionEx, new UploadAttendanceHandler(bundle));

            var attendance = new AttendanceLog("4", 1, 1, 2018, 1, 13, 16, 11, 30, 1, 1, "gate01", DeviceType.OnlyIn);
            hub.PublishAsync(new EventMessage(EventType.AttTransactionEx, attendance)).GetAwaiter().GetResult();

            Thread.Sleep(30000);

            var result = bundle.Database.QueryScalar(
                "select count(*) from queue where refer_id=@refer_id",
                new Dictionary<string, object>
                {
                    { "@refer_id", attendance.Id }
                });
            var count = Convert.ToInt32(result);
            Assert.IsTrue(count == 0);

            result = bundle.Database.QueryScalar(
                "select sync from attendance_logs where id=@id",
                new Dictionary<string, object>
                {
                    { "@id", attendance.Id }
                });
            var sync = Convert.ToInt32(result);
            Assert.IsTrue(sync == 0);
        }

        [TestMethod]
        public void TestUploadAttendanceHandlerFailedToMessage()
        {           
            var bundle = new Bundle(new SqliteConnector(), _mock.Object);
            var hub = new EventHub(bundle.Database);
            hub.Subscribe(EventType.AttTransactionEx, new UploadAttendanceHandler(bundle));

            var attendance = new AttendanceLog("3", 1, 1, 2018, 1, 14, 12, 11, 30, 1, 1, "gate01", DeviceType.OnlyIn);
            hub.PublishAsync(new EventMessage(EventType.AttTransactionEx, attendance)).GetAwaiter().GetResult();

            Thread.Sleep(15000);

            var result = bundle.Database.QueryScalar(
                "select count(*) from failed_queue;", null);
            var count = (long)result;
            Assert.IsTrue(count > 0);

            Thread.Sleep(1000);
            result = bundle.Database.QueryScalar(
                "select count(*) from attendance_logs where id=@id",
                new Dictionary<string, object>
                {
                    { "@id", attendance.Id }
                });
            count = (long)result;
            Assert.IsTrue(count == 0);
        }

        [TestMethod]
        public void TestResendFailedMessages()
        {
            var bundle = new Bundle(new SqliteConnector(), _mock.Object);
            var handler = new ResendAttendanceHandler(bundle);
            handler.ResendFailedMessages();

            var result = bundle.Database.QueryScalar(
                "select count(*) from failed_queue where retry_times > 0;", null);
            var count = (long)result;
            Assert.IsTrue(count > 0);
        }
    }
}
