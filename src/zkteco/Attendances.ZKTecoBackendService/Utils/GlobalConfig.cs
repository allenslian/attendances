using Attendances.BackendService.Configs;
using Attendances.ZKTecoBackendService.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendances.ZKTecoBackendService.Utils
{
    public class GlobalConfig
    {
        private static DeviceInfo[] _devices;
        /// <summary>
        /// Device information
        /// </summary>
        public static DeviceInfo[] Devices
        {
            get
            {
                if (_devices == null)
                {
                    var devices = new List<DeviceInfo>(10);
                    var group = ConfigurationManager.GetSection("deviceGroup") as DeviceConfigurationSectionHandler;
                    if (group != null)
                    {
                        foreach (var device in group.Devices)
                        {
                            var config = device as DeviceConfiguration;
                            if (config == null)
                            {
                                continue;
                            }

                            devices.Add(new DeviceInfo(config.Name, config.IP, config.Port, config.Type));
                        }
                    }
                    _devices = devices.ToArray();
                }
                return _devices;
            }
        }

        private static int _retryTimes = -1;
        /// <summary>
        /// How many times does it retries when some errors occur.
        /// </summary>
        public static int RetryTimes
        {
            get
            {
                if (_retryTimes < 0)
                {
                    _retryTimes = GetValue("RetryTime", 3);
                }
                return _retryTimes;
            }
        }

        private static string _apiRootUrl;
        /// <summary>
        /// Where does sync data go?
        /// </summary>
        public static string ApiRootUrl
        {
            get
            {
                if (_apiRootUrl == null)
                {
                    var api = GetValue("ApiRootUrl");
                    if (!api.EndsWith("/"))
                    {
                        api += "/";
                    }
                    _apiRootUrl = api;
                }
                return _apiRootUrl;
            }
        }

        private static string _apiToken;

        public static string ApiToken
        {
            get
            {
                if (_apiToken == null)
                {
                    _apiToken = GetValue("ApiToken");
                }
                return _apiToken;
            }
        }

        private static string _projectCode;
        /// <summary>
        /// Project code which the devices belong to
        /// </summary>
        public static string ProjectCode
        {
            get
            {
                if (_projectCode == null)
                {
                    _projectCode = GetValue("ProjectCode");
                }
                return _projectCode;
            }
        }


        private static string _appRootFolder;
        /// <summary>
        /// The service program's root folder.
        /// </summary>
        public static string AppRootFolder
        {
            get
            {
                if (_appRootFolder == null)
                {
                    _appRootFolder = GetValue("AppRootFolder", AppDomain.CurrentDomain.BaseDirectory);
                }
                return _appRootFolder;
            }
        }

        #region Private methods
        /// <summary>
        /// Read int value from configuration file.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private static int GetValue(string key, int defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            int result;
            if (int.TryParse(value, out result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Read string value from configuration file.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private static string GetValue(string key, string defaultValue = "")
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                if (string.IsNullOrWhiteSpace(defaultValue))
                {
                    throw new ArgumentException(string.Format("Missing '{0}' setting", key));
                }
                return defaultValue;
            }
            return value;
        }

        #endregion
    }
}
