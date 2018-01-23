using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Events;
using Attendances.ZKTecoBackendService.Connectors;
using Topshelf.Logging;
using Attendances.ZKTecoBackendService.Models;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Attendances.ZKTecoBackendService.Handlers
{
    public class UploadAttendanceHandler : IEventHandler
    {
        private Bundle Bundle { get; set; }

        private LogWriter Logger { get; set; }

        public UploadAttendanceHandler(Bundle bundle)
        {
            Bundle = bundle;
            Logger = HostLogger.Get<UploadAttendanceHandler>();
        }

        public string HandlerKey
        {
            get { return GetType().FullName; }
        }

        public void Handle(EventMessage msg)
        {
            AttendanceLog attendance = null;
            try
            {
                attendance = JsonConvert.DeserializeObject<AttendanceLog>(msg.JsonData);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("DeserializeObject error: {@ex}, EventMessage.Data type[{type}] is not supported.", ex, msg.JsonData);
                return;
            }

            if (IsPendingAttendance(attendance.Id))
            {
                Logger.DebugFormat("The attendance({id}) is handled.", attendance.Id);
                return;
            }

            var workerId = Bundle.GetCurrentWorkerId(attendance.UserId, attendance.ProjectId);
            if (workerId == string.Empty)
            {
                Logger.DebugFormat("Not found the worker id({id}).", attendance.UserId);
                return;
            }

            switch (attendance.DeviceType)
            {
                case DeviceType.In:
                    CheckIn(attendance, workerId);
                    break;
                case DeviceType.Out:
                    CheckOut(attendance, workerId);
                    break;
                case DeviceType.InOut:
                    CheckInOrCheckOut(attendance, workerId);
                    break;
                default:
                    Logger.ErrorFormat("Not support device type:{@attendance}", attendance);
                    break;
            }

        }

        private bool IsPendingAttendance(string id)
        {
            var result = Bundle.Database.QueryScalar(
                "SELECT IFNULL(COUNT(*), 0) AS num FROM attendance_logs WHERE id=@id;",
                new Dictionary<string, object>
                {
                    { "@id", id }
                });
            if (result == null)
            {
                return false;
            }
            return Convert.ToInt32(result) > 0;
        }        

        private void CheckIn(AttendanceLog attendance, string workerId)
        {
            Logger.DebugFormat("Attendance({id}) CheckIn executes.", attendance.Id);
            attendance.CheckIn();

            Logger.Debug("SaveAttendanceLog executes.");
            SaveAttendanceLog(attendance);

            Logger.Debug("Bundle.CheckInToCTMS executes.");
            Bundle.CheckInToCTMS(attendance, workerId);
        }

        private void CheckOut(AttendanceLog attendance, string workerId)
        {
            Logger.DebugFormat("Attendance({id}) CheckOut executes.", attendance.Id);
            attendance.CheckOut();

            Logger.Debug("SaveAttendanceLog executes.");
            SaveAttendanceLog(attendance);

            Logger.Debug("Bundle.CheckOutToCTMS executes.");
            Bundle.CheckOutToCTMS(attendance, workerId);
        }

        private void CheckInOrCheckOut(AttendanceLog attendance, string workerId)
        {
            var lastAttendance = GetLastAttendanceLog(attendance.UserId, attendance.LogDate);
            AttendanceStatus status = AttendanceStatus.Unknown;
            try
            {
                status = attendance.CalculateStatus(lastAttendance);
                Logger.DebugFormat("Attendance({id}) status: {status}.", attendance.Id, status);
            }
            catch (NotSupportedException ex)
            {
                Logger.ErrorFormat("CheckInOrCheckOut error:{exception}, attendance:{@log}, worker:{id}",
                    ex.Message, attendance, workerId);
                return;
            }

            if (status == AttendanceStatus.CheckIn)
            {
                CheckIn(attendance, workerId);
                return;
            }

            if (status == AttendanceStatus.CheckOut)
            {
                CheckOut(attendance, workerId);
                return;
            }

            Logger.ErrorFormat("CheckInOrCheckOut: Unknown status, attendance:{@log}, worker:{id}", attendance, workerId);
        }        

        private void SaveAttendanceLog(AttendanceLog attendance)
        {
            Bundle.Database.Execute(
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

        private AttendanceLog GetLastAttendanceLog(string enrollNumber, DateTime logDate)
        {
            var reader = Bundle.Database.QuerySet(
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
