using Serilog;
using System;
using System.Diagnostics;
using System.IO;

namespace Attendances.ZKTecoBackendService.Utils
{
    internal class DLLInstaller
    {
        private ILogger Logger { get; set; }

        public DLLInstaller(ILogger logger)
        {
            Logger = logger;
        }

        public void Install()
        {
            Logger.Debug("Install dll files");
#if x64
            RegisterX64DLLs();
#else
            Registerx86DLLs();
#endif
        }

        public void Uninstall()
        {
            Logger.Debug("Uninstall dll files");
#if x64
            UnregisterX64DLLs();
#else
            UnregisterX86DLLs();            
#endif
        }

        private void RegisterX86DLLs()
        {
            RegisterDLLs("sdk_install", "x86");
        }

        private void UnregisterX86DLLs()
        {
            RegisterDLLs("sdk_uninstall", "x86");
        }

        private void RegisterX64DLLs()
        {
            RegisterDLLs("sdk_install", "x64");
        }

        private void UnregisterX64DLLs()
        {
            RegisterDLLs("sdk_uninstall", "x64");
        }

        private void RegisterDLLs(string command, string platform)
        {
            Logger.Debug("{command}.bat {platform}", command, platform);

            var pi = new ProcessStartInfo("cmd.exe", String.Format("/c {0}.bat {1}", 
                Path.Combine(GlobalConfig.AppRootFolder, command), platform))
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var p = Process.Start(pi);
            p.OutputDataReceived += (sender, e) => Logger.Debug("{command} Output:{data}.", command, e.Data);
            p.BeginOutputReadLine();
            p.ErrorDataReceived += (sender, e) => Logger.Error("{command} Error:{error}", command, e.Data);
            p.BeginErrorReadLine();

            p.WaitForExit();
            var exitCode = p.ExitCode;
            p.Close();

            Logger.Debug("{command} ExitCode: {code}.", command, exitCode);
        }
    }
}
