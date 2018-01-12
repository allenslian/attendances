namespace Attendances.ZKTecoBackendService.Models
{
    /// <summary>
    /// It describes one device is checkin, checkout or checkin&checkout.
    /// </summary>
    public enum DeviceType
    {
        /// <summary>
        /// Out type means, workers only go out from this door, not go in.
        /// </summary>
        Out = -1,
        /// <summary>
        /// InOut type means, workers may go in / out from this door.
        /// </summary>
        InOut = 0,
        /// <summary>
        /// In type means, workers only go in from this door, not go out.
        /// </summary>
        In = 1
    }

    public enum AttendanceStatus
    {
        /// <summary>
        /// Unknown status
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Check in to door
        /// </summary>
        CheckIn = 1,

        /// <summary>
        /// Check out from door
        /// </summary>
        CheckOut = -1,
    }

    public enum EventType
    {
        AttTransactionEx = 1,
        Finger,
        NewUser,
        EnrollFingerEx,
        Verify,
        FingerFeature,
        Door,
        Alarm,
        HIDNum,
        WriteCard,
        EmptyCard,
        DeleteTemplate,
        Failed
    }
}
