using Attendances.ZKTecoBackendService.Connectors;
using Attendances.ZKTecoBackendService.Models;
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

            Interlocked.CompareExchange(ref _running, 0, 1);
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
    }
}
