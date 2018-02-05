using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Models;
using System.Collections.Generic;
using Topshelf.Logging;

namespace Attendances.ZKTecoBackendService.Connectors
{
    public class Bundle
    {
        public Bundle(SqliteConnector db, IWebApiConnector api)
        {
            Database = db;
            WebApi = api;

            Logger = HostLogger.Get<Bundle>();
        }

        private LogWriter Logger { get; set; }

        public SqliteConnector Database { get; private set; }

        public IWebApiConnector WebApi { get; private set; }

        public string GetCurrentWorkerId(string enrollNumber, string projectId)
        {
            var workId = GetLocalWorkerId(enrollNumber, projectId);
            if (!string.IsNullOrWhiteSpace(workId))
            {
                return workId;
            }

            workId = WebApi.FindProjectWorkerByFaceId(enrollNumber, projectId).GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(workId))
            {
                SaveLocalWorker(new WorkerDTO(enrollNumber, workId, projectId));
                return workId;
            }
            return string.Empty;
        }

        public async void CheckInToCTMS(AttendanceLog attendance, string workerId)
        {
            var ok = await WebApi.CheckIn(attendance.ProjectId, workerId, attendance.LogDate, attendance.DeviceName);
            if (ok)
            {
                UploadAttendanceLogSuccess(attendance.Id);
            }
            else
            {
                UploadAttendanceLogFailed(attendance.Id);
            }
        }

        public async void CheckOutToCTMS(AttendanceLog attendance, string workerId)
        {
            var ok = await WebApi.CheckOut(attendance.ProjectId, workerId, attendance.LogDate);
            if (ok)
            {
                UploadAttendanceLogSuccess(attendance.Id);
            }
            else
            {
                UploadAttendanceLogFailed(attendance.Id);
            }
        }

        private string GetLocalWorkerId(string enrollNumber, string projectId)
        {
            var result =Database.QueryScalar(
                "SELECT ifnull(user_id,'') AS user FROM user_maps WHERE enroll_number=@enroll_number AND project_id=@project_id;",
                new Dictionary<string, object>()
                {
                    { "@enroll_number", enrollNumber },
                    { "@project_id", projectId }
                });
            if (result == null)
            {
                return string.Empty;
            }
            return (string)result;
        }

        private void SaveLocalWorker(WorkerDTO worker)
        {
            Database.Execute(
                "INSERT INTO user_maps(user_id,enroll_number,project_id) VALUES(@worker_id, @enroll_number, @project_id);",
                new Dictionary<string, object>
                {
                    {"@worker_id", worker.UserId },
                    {"@enroll_number", worker.EnrollNumber },
                    {"@project_id", worker.ProjectId }
                });
        }

        private void UploadAttendanceLogSuccess(string id)
        {
            ChangeAttendanceLogStatus(id, true);
        }

        private void UploadAttendanceLogFailed(string id)
        {
            ChangeAttendanceLogStatus(id, false);
        }

        private void ChangeAttendanceLogStatus(string id, bool ok)
        {
            Database.Execute(
                "UPDATE attendance_logs SET sync=@sync,change_at=datetime('now') WHERE id=@id;",
                new Dictionary<string, object>
                {
                    { "@id", id },
                    { "@sync", ok ? 1 : 0 }
                });
        }

        public void Dispose()
        {
            Logger.Debug("Bundle is disposing...");

            if (Database != null)
            {                
                Database.Dispose();
                Logger.Debug("Database disposes completely.");
            }

            if (WebApi != null)
            {                
                WebApi.Dispose();
                Logger.Debug("WebApi disposes completely.");
            }

            Logger.Debug("Bundle disposes completely.");
        }
    }
}
