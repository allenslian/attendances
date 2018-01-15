using Attendances.ZKTecoBackendService.Configs;
using Attendances.ZKTecoBackendService.Models;
using System;
using System.Collections.Generic;
using System.Configuration;

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

                            devices.Add(new DeviceInfo(config.Name, config.IP, config.Port, config.Type, config.Password));
                        }
                    }
                    _devices = devices.ToArray();
                }
                return _devices;
            }
        }

        private static int _maxRetryTimes = -1;
        /// <summary>
        /// How many times does it retries when some errors occur.
        /// </summary>
        public static int MaxRetryTimes
        {
            get
            {
                if (_maxRetryTimes < 0)
                {
                    _maxRetryTimes = ReadValueFromConfig("MaxRetryTimes", 3);
                }
                return _maxRetryTimes;
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
                    var api = ReadValueFromConfig("ApiRootUrl");
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
                    _apiToken = ReadValueFromConfig("ApiToken");
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
                    _projectCode = ReadValueFromConfig("ProjectCode");
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
                    _appRootFolder = ReadValueFromConfig("AppRootFolder", AppDomain.CurrentDomain.BaseDirectory);
                }
                return _appRootFolder;
            }
        }

        private static double _minWorkingHours = -1;

        public static double MinWorkingHours
        {
            get
            {
                if (_minWorkingHours < 0)
                {
                    _minWorkingHours = ReadValueFromConfig("MinWorkingHours", 0.5);
                }
                return _minWorkingHours;
            }
        }

        private static double _maxWorkingHours = -1;

        public static double MaxWorkingHours
        {
            get
            {
                if (_maxWorkingHours < 0)
                {
                    _maxWorkingHours = ReadValueFromConfig("MaxWorkingHours", 16.0);
                }
                return _maxWorkingHours;
            }
        }


        private static int _resendIntervalMinutes = -1;

        public static int ResendIntervalMinutes
        {
            get
            {
                if (_resendIntervalMinutes < 0)
                {
                    _resendIntervalMinutes = ReadValueFromConfig("ResendIntervalMinutes", 60);
                }
                return _resendIntervalMinutes;
            }
        }

        #region Private methods
        /// <summary>
        /// Read int value from configuration file.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private static int ReadValueFromConfig(string key, int defaultValue)
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
        private static string ReadValueFromConfig(string key, string defaultValue = "")
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

        private static double ReadValueFromConfig(string key, double defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            double result;
            if (double.TryParse(value, out result))
            {
                return Math.Round(result, 2);
            }
            return Math.Round(defaultValue, 2);
        }
        #endregion
    }
}
