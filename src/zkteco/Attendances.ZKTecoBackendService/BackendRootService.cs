using Attendances.ZKTecoBackendService.Connectors;
using Attendances.ZKTecoBackendService.Devices;
using Attendances.ZKTecoBackendService.Events;
using Attendances.ZKTecoBackendService.Handlers;
using Attendances.ZKTecoBackendService.Utils;
using System;
using System.Collections.Generic;
using Topshelf;
using Topshelf.Logging;

namespace Attendances.ZKTecoBackendService
{
    class BackendRootService : ServiceControl
    {
        private LogWriter Logger { get; set; }

        private Bundle Bundle { get; set; }

        private EventHub Hub { get; set; }

        private List<ZktecoDevice> Devices { get; set; }

        public BackendRootService(Bundle bundle, EventHub hub)
        {
            Bundle = bundle;
            Hub = hub;

            Logger = HostLogger.Get<BackendRootService>();
        }

        public bool Start(HostControl hostControl)
        {
            Logger.Debug("BackendRootService is starting...");

            try
            {
                SubscribeEventHandlers();

                RegisterAndStartDevices();                
            }
            catch (Exception ex)
            {
                Logger.DebugFormat("BackendRootService start error: {@ex}.", ex);
                return false;
            }           

            Logger.Debug("BackendRootService started.");
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            Logger.Debug("BackendRootService is stoping...");
            foreach (var device in Devices)
            {
                device.Stop();
            }

            Hub.Dispose();
            Bundle.Dispose();
            Logger.Debug("BackendRootService stops.");
            return true;
        }
        
        public bool Shutdown(HostControl hostControl)
        {
            Logger.Debug("BackendRootService is shutting down...");
            Logger.Debug("BackendRootService stops.");
            //Stop the root service
            hostControl.Stop();
            return true;
        }

        private void RegisterAndStartDevices()
        {
            var count = GlobalConfig.Devices.Length;
            Devices = new List<ZktecoDevice>(count);
            for (var i = 0; i < count; i++)
            {
                var device = new ZktecoDevice(new zkemkeeper.CZKEM(), GlobalConfig.Devices[i], Hub);
                device.StartAsync();
                Devices.Add(device);
                Logger.DebugFormat("RegisterDevices: device name({name}), ip({ip}), port({port}), kind({kind}) on the thread({id}).", 
                    GlobalConfig.Devices[i].DeviceName, GlobalConfig.Devices[i].IP, GlobalConfig.Devices[i].Port, GlobalConfig.Devices[i].Type, 
                    System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void SubscribeEventHandlers()
        {
            Hub.Subscribe(Models.EventType.AttTransactionEx, new UploadAttendanceHandler(Bundle));
            //Hub.Subscribe(Models.EventType.Failed, new FailedMessageHandler(Bundle));
        }
    }
}
