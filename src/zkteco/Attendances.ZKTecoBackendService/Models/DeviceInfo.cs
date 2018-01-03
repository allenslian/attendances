using Attendances.BackendService.Models;

namespace Attendances.ZKTecoBackendService.Models
{
    /// <summary>
    /// Device's information from configuration file.
    /// </summary>
    public class DeviceInfo
    {
        public DeviceInfo(string deviceName, string ip, 
            int port, DeviceType type)
        {
            DeviceName = deviceName;
            IP = ip;
            Port = port;
            Type = type;
        }

        /// <summary>
        /// Device name
        /// </summary>
        public string DeviceName { get; private set; }

        /// <summary>
        /// Device IP
        /// </summary>
        public string IP { get; private set; }

        /// <summary>
        /// Device port
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Device type has three kinds: in, out, and in&out.
        /// </summary>
        public DeviceType Type { get; private set; }
    }
}
