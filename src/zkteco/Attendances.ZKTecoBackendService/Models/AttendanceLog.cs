using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Utils;
using System;

namespace Attendances.ZKTecoBackendService.Models
{
    public class AttendanceLog : IIdentityKey
    {
        private AttendanceLog(string enrollNumber, int state, int mode,
            int workCode, int machineNumber, string deviceName, DeviceType type)
        {
            UserId = enrollNumber;
            State = state;
            Mode = mode;
            WorkCode = workCode;
            MachineId = machineNumber;
            DeviceName = deviceName;
            DeviceType = type;
        }

        public AttendanceLog(string enrollNumber, int state, int mode,
            int year, int month, int day, int hour, int minute, int second,
            int workCode, int machineNumber, string deviceName, DeviceType type)
            : this(enrollNumber, state, mode, workCode, machineNumber, deviceName, type)
        {
            LogDate = new DateTime(year, month, day, hour, minute, second);
            ProjectId = GlobalConfig.ProjectCode;
            Id = NewId(enrollNumber, LogDate, ProjectId);
        }

        /// <summary>
        /// Read instance from database.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="enrollNumber"></param>
        /// <param name="state"></param>
        /// <param name="mode"></param>
        /// <param name="logDate"></param>
        /// <param name="workCode"></param>
        /// <param name="machineNumber"></param>
        /// <param name="projectId"></param>
        /// <param name="deviceName"></param>
        /// <param name="type"></param>
        /// <param name="status"></param>
        public AttendanceLog(string id, string enrollNumber, int state, int mode,
            DateTime logDate, int workCode, int machineNumber, string projectId,
            string deviceName, DeviceType type, AttendanceStatus status) : this(enrollNumber, state, mode, workCode, machineNumber, deviceName, type)
        {
            Id = id;
            LogDate = logDate;
            ProjectId = projectId;
            LogStatus = status;
        }

        public string Id { get; private set; }

        public string UserId { get; private set; }

        public int State { get; private set; }

        public int Mode { get; private set; }

        public int WorkCode { get; private set; }

        public DateTime LogDate { get; private set; }

        public string ProjectId { get; private set; }

        public int MachineId { get; private set; }
        /// <summary>
        /// Fingerprint/Facial recognition device name.
        /// It is one device worker is using.
        /// </summary>
        public string DeviceName { get; private set; }
        /// <summary>
        /// It describes the device is checkin, checkout or checkin&out.
        /// </summary>
        public DeviceType DeviceType { get; private set; }

        public AttendanceStatus LogStatus { get; private set; }

        #region Methods

        public void CheckIn()
        {
            LogStatus = AttendanceStatus.CheckIn;
        }

        public void CheckOut()
        {
            LogStatus = AttendanceStatus.CheckOut;
        }

        public AttendanceStatus CalculateStatus(AttendanceLog lastLog)
        {
            if (lastLog == null)
            {
                return AttendanceStatus.CheckIn;
            }

            var diff = LogDate.Subtract(lastLog.LogDate);
            var totalHours = Math.Round(diff.TotalHours, 2);
            if (totalHours - GlobalConfig.MaxWorkingHours >= 0.00)
            {
                // if new log date is more than 16 hours after last log date.
                // system think it is one check in.
                return AttendanceStatus.CheckIn;
            }

            if (GlobalConfig.MaxWorkingHours - totalHours > 0.00 && totalHours - GlobalConfig.MinWorkingHours >= 0.00)
            {
                if (lastLog.LogStatus == AttendanceStatus.CheckIn)
                {
                    return AttendanceStatus.CheckOut;
                }

                if (lastLog.LogStatus == AttendanceStatus.CheckOut)
                {
                    return AttendanceStatus.CheckIn;
                }
            }

            if (GlobalConfig.MinWorkingHours - totalHours > 0.00)
            {
                // time range is less than 30 minutes, system think it is a duplicated attendance log.
                if (lastLog.LogStatus == AttendanceStatus.CheckIn)
                {
                    return AttendanceStatus.CheckIn;
                }

                if (lastLog.LogStatus == AttendanceStatus.CheckOut)
                {
                    return AttendanceStatus.CheckOut;
                }
            }

            throw new NotSupportedException("Unknown attendance status.");
        }

        private static string NewId(string userId, DateTime logDate, string projectId)
        {
            int key;
            if (!Int32.TryParse(userId, out key))
            {
                key = userId == null ? 0 : userId.GetHashCode();
            }

            return string.Format("{0}{1}", logDate.Ticks, key);
        } 

        #endregion
    }
}
