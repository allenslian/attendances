using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Events;
using Attendances.ZKTecoBackendService.Models;
using Attendances.ZKTecoBackendService.Connectors;
using System;
using Topshelf.Logging;

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
            if (msg.Kind != EventType.Failed)
            {
                return;
            }

            var data = msg.Data as ArgumentItem;
            if (data == null)
            {
                return;
            }

            var @event = data["Message"] as EventMessage;
            if (@event == null)
            {
                return;
            }

            var handler = Activator.CreateInstance(Type.GetType(msg.ReferenceId, false, false), new[] {Bundle}) as IEventHandler;
            if (handler == null)
            {
                return;
            }

            handler.Handle(@event);
        }
    }
}
