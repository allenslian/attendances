using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Events;
using Attendances.ZKTecoBackendService.Models;
using Attendances.ZKTecoBackendService.Connectors;
using System;
using Topshelf.Logging;
using System.Threading;
using Attendances.ZKTecoBackendService.Errors;
using Newtonsoft.Json;

namespace Attendances.ZKTecoBackendService.Handlers
{
    public class FailedMessageHandler : IEventHandler
    {
        private Bundle Bundle { get; set; }

        private LogWriter Logger { get; set; }

        public FailedMessageHandler(Bundle bundle)
        {
            Bundle = bundle;

            Logger = HostLogger.Get<FailedMessageHandler>();
        }

        public string HandlerKey
        {
            get { return GetType().FullName; }
        }

        public void Handle(EventMessage msg)
        {
            Logger.DebugFormat("FailedMessageHandler.Handle starts on the thread({id}).", Thread.CurrentThread.ManagedThreadId);

            if (msg.Kind != EventType.Failed)
            {
                Logger.DebugFormat("FailedMessageHandler.Handle Kind({kind}) is not Failed.", msg.Kind);
                return;
            }

            var data = msg.Data as ArgumentItem;
            if (data == null)
            {
                Logger.Debug("FailedMessageHandler.Handle Data type is not ArgumentItem.");
                return;
            }

            EventMessage @event;
            try
            {
                @event = JsonConvert.DeserializeObject<EventMessage>(data["Message"]);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("FailedMessageHandler.Handle DeserializeObject error:{@ex}", ex);
                @event = null;
            }
                        
            if (@event == null)
            {
                Logger.Debug("FailedMessageHandler.Handle Message type is not EventMessage.");
                return;
            }

            var handler = Activator.CreateInstance(Type.GetType(msg.ReferenceId, false, false), new[] {Bundle}) as IEventHandler;
            if (handler == null)
            {
                Logger.Debug("FailedMessageHandler.Handle fails to create Handler instance.");
                return;
            }

            Logger.Debug("FailedMessageHandler.Handle invokes failed handler's Handle method.");

            try
            {
                handler.Handle(@event);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("FailedMessageHandler.Handle error: {@ex}, try {num} times.", ex, data.Count);
                if (!data.IncreaseFailedCount())
                {
                    // It avoids to retry the same handler many times.                   
                    return;
                }

                throw new FailedHandleException(ex, data);
            }

            Logger.Debug("FailedMessageHandler.Handle ends.");
        }
    }
}
