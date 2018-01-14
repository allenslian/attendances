using Attendances.ZKTecoBackendService.Connectors;
using Attendances.ZKTecoBackendService.Events;
using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Models;
using Attendances.ZKTecoBackendService.Utils;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading;
using Topshelf.Logging;

namespace Attendances.ZKTecoBackendService.Handlers
{
    public class ResendAttendanceHandler : IJob
    {
        private Bundle Bundle { get; set; }

        private LogWriter Logger { get; set; }

        private int _running = 0;

        public ResendAttendanceHandler(Bundle bundle)
        {
            Bundle = bundle;

            Logger = HostLogger.Get<ResendAttendanceHandler>();
        }

        public void Execute(IJobExecutionContext context)
        {
            Logger.DebugFormat("ResendAttendanceHandler.Execute starts on the thread({id}).", Thread.CurrentThread.ManagedThreadId);

            if (Interlocked.CompareExchange(ref _running, 1, 0) == 1)
            {
                return;
            }

            try
            {
                ResendFailedAttendances();
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("ResendFailedAttendances error: {@ex}", ex);
            }

            try
            {
                ResendFailedMessages();
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("ResendFailedMessages error: {@ex}", ex);
            }            

            Interlocked.CompareExchange(ref _running, 0, 1);
        }

        /// <summary>
        /// public is used by unit test.
        /// </summary>
        public void ResendFailedAttendances()
        {
            var attendances = GetFailedAttendances();
            foreach (var attendance in attendances)
            {
                var workerId = Bundle.GetCurrentWorkerId(attendance.UserId, attendance.ProjectId);
                if (workerId == string.Empty)
                {
                    Logger.DebugFormat("Not found the worker id({id}).", attendance.UserId);
                    continue;
                }

                switch (attendance.LogStatus)
                {
                    case AttendanceStatus.CheckIn:
                        Bundle.CheckInToCTMS(attendance, workerId);
                        break;
                    case AttendanceStatus.CheckOut:
                        Bundle.CheckOutToCTMS(attendance, workerId);
                        break;
                    default:
                        Logger.ErrorFormat("Not support device type:{@attendance}", attendance);
                        break;
                }
            }
        }

        private List<AttendanceLog> GetFailedAttendances()
        {
            var reader = Bundle.Database.QuerySet(
                @"SELECT id,enroll_number,state,mode,log_date,work_code,machine_id,project_id,ifnull(device_name,''), 
                    ifnull(device_type,0), log_status FROM attendance_logs WHERE sync=0;", null);
            var results = new List<AttendanceLog>();
            while (reader.Read())
            {
                results.Add(new AttendanceLog(
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
                    (AttendanceStatus)reader.GetInt32(10)));
            }
            return results;
        }

        /// <summary>
        /// public is used by unit test.
        /// </summary>
        public void ResendFailedMessages()
        {
            var messages = GetFailedQueueMessages();
            foreach (var msg in messages)
            {
                var data = JsonConvert.DeserializeObject<ArgumentItem>(msg.JsonData);
                if (data == null)
                {
                    Logger.Debug("ResendFailedMessages Data type is not ArgumentItem.");
                    continue;
                }

                EventMessage @event;
                try
                {
                    @event = JsonConvert.DeserializeObject<EventMessage>(data["Message"]);
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("ResendFailedMessages DeserializeObject error:{@ex}", ex);
                    @event = null;
                }

                if (@event == null)
                {
                    Logger.Debug("ResendFailedMessages Message type is not EventMessage.");
                    continue;
                }

                Logger.Debug("ResendFailedMessages creates handler instance.");
                var handler = Activator.CreateInstance(Type.GetType(msg.ReferenceId, false, false), new[] { Bundle }) as IEventHandler;
                if (handler == null)
                {
                    Logger.Debug("ResendFailedMessages fails to create Handler instance.");
                    continue;
                }

                Logger.Debug("ResendFailedMessages invokes failed handler's Handle method.");

                try
                {
                    handler.Handle(@event);
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("ResendFailedMessages error: {@ex}, try {num} times.", ex, msg.RetryTimes);
                    IncreaseFailedTimes(msg.Id);
                    continue;
                }

                DestroyFailedMessage(msg.Id);
            }
        }

        private List<EventMessage> GetFailedQueueMessages()
        {
            var reader = Bundle.Database.QuerySet(
                @"SELECT id,refer_id,message,create_at,retry_times FROM failed_queue WHERE retry_times<@times;", 
                new Dictionary<string, object>
                {
                    { "@times", GlobalConfig.MaxRetryTimes }
                });
            var results = new List<EventMessage>();
            while (reader.Read())
            {
                results.Add(new EventMessage(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDateTime(3),
                    reader.GetInt32(4)));
            }
            return results;
        }

        private void IncreaseFailedTimes(string id)
        {
            Logger.DebugFormat("Increase failed message({id}) at one time.", id);
            Bundle.Database.Execute("UPDATE failed_queue SET retry_times=retry_times+1 WHERE id=@id;",
                new Dictionary<string, object>
                {
                    { "@id", id }
                });
        }

        private void DestroyFailedMessage(string id)
        {
            Logger.DebugFormat("Destroy failed message({id}).", id);
            Bundle.Database.Execute("DELETE FROM failed_queue WHERE id=@id;",
                new Dictionary<string, object>
                {
                    { "@id", id }
                });
        }
    }
}
