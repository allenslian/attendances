using Attendances.ZKTecoBackendService.Events;

namespace Attendances.ZKTecoBackendService.Interfaces
{
    public interface IEventHandler
    {
        /// <summary>
        /// This is one unique key in the same event's handlers.
        /// </summary>
        string HandlerKey { get; }

        void Handle(EventMessage msg);
    }
}
