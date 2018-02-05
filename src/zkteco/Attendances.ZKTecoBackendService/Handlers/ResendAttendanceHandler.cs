using Attendances.ZKTecoBackendService.Connectors;
using Attendances.ZKTecoBackendService.Events;
using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Models;
using Attendances.ZKTecoBackendService.Utils;
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

            Logger.DebugFormat("ResendAttendanceHandler.Execute ends on the thread({id}).", Thread.CurrentThread.ManagedThreadId);
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
                var @event = msg.Event;
                if (@event == null)
                {
                    Logger.Debug("ResendFailedMessages Message type is not EventMessage.");
                    continue;
                }

                Logger.Debug("ResendFailedMessages creates handler instance.");
                var handler = Activator.CreateInstance(Type.GetType(msg.Handler, false, false), new[] { Bundle }) as IEventHandler;
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
                    IncreaseFailedTimes(msg);
                    continue;
                }

                DestroyFailedMessage(msg.Id);
            }
        }

        private List<FailedMessage> GetFailedQueueMessages()
        {
            var reader = Bundle.Database.QuerySet(
                @"SELECT id,refer_id,message,create_at,retry_times,kind,handler FROM failed_queue WHERE retry_times<@times ORDER BY create_at ASC;", 
                new Dictionary<string, object>
                {
                    { "@times", GlobalConfig.MaxRetryTimes }
                });
            var results = new List<FailedMessage>();
            while (reader.Read())
            {
                results.Add(new FailedMessage(
                    reader.GetString(0),
                    (FailedEventType)reader.GetInt32(5),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDateTime(3),
                    reader.GetInt32(4),
                    reader.GetString(6)));
            }
            return results;
        }

        private void IncreaseFailedTimes(FailedMessage msg)
        {            
            if (msg.IncreaseFailedCount())
            {
                Bundle.Database.Execute("UPDATE failed_queue SET retry_times=@retry_times WHERE id=@id;",
                    new Dictionary<string, object>
                    {
                        { "@id", msg.Id },
                        { "@retry_times", msg.RetryTimes }
                    });
            }
            Logger.DebugFormat("Increase failed message({id}) at {times} time.", msg.Id, msg.RetryTimes);
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
