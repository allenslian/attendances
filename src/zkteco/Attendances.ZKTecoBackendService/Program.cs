using Attendances.ZKTecoBackendService.Handlers;
using Attendances.ZKTecoBackendService.Utils;
using Quartz;
using Serilog;
using System.IO;
using Topshelf;
using Topshelf.Ninject;
using Topshelf.Quartz;
using Topshelf.Quartz.Ninject;

namespace Attendances.ZKTecoBackendService
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new LoggerConfiguration().ReadFrom.AppSettings()
                .WriteTo.RollingFile(Path.Combine(GlobalConfig.AppRootFolder, "logs", "debug{Date}.log"), retainedFileCountLimit: 10)
                .CreateLogger();            

            HostFactory.Run(cfg =>
            {
                cfg.UseSerilog(logger);

                cfg.UseNinject(new BackendServiceModule());

                cfg.BeforeInstall(_ => BeforeInstall(logger));

                cfg.AfterUninstall(() => AfterUninstall(logger));

                cfg.Service<BackendRootService>(x =>
                {
                    x.ConstructUsingNinject();
                    x.WhenStarted((s, h) => s.Start(h));
                    x.WhenStopped((s, h) => s.Stop(h));

                    //Retry to send failed attendance logs.
                    x.UseQuartzNinject();

                    x.ScheduleQuartzJob(q =>
                    {
                        q.WithJob(() =>
                        {
                            return JobBuilder.Create<ResendAttendanceHandler>().WithIdentity("retryJob", "syncbackend").Build();
                        }).AddTrigger(() =>
                        {
                            return TriggerBuilder.Create().WithIdentity("retryTrigger", "syncbackend")
                            .StartNow().WithSimpleSchedule(b => b.WithIntervalInMinutes(5).RepeatForever()).Build();
                        });                        
                    });                    
                });

                cfg.RunAsLocalService();
                cfg.StartAutomatically();

                cfg.SetServiceName("AttendanceBackendService");
                cfg.SetDisplayName("ZKTeco Attendance Backend Service");
                cfg.SetDescription("ZKTeco Synchronize attendance log to CTMS.");

                cfg.OnException(ex =>
                {
                    logger.Error("OnException: {@ex}", ex);
                });
            });
        }

        private static void BeforeInstall(ILogger logger)
        {
            // When building this project, copy libs/*.dll to output directory.
            logger.Debug("Dll installs.");
            new DLLInstaller(logger).Install();

            logger.Debug("Database installs.");
            DbInstaller.Install();
        }

        private static void AfterUninstall(ILogger logger)
        {
            logger.Debug("Dll uninstalls.");
            new DLLInstaller(logger).Uninstall();
        }        
    }
}
