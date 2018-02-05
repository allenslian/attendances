using Attendances.ZKTecoBackendService.Events;
using Attendances.ZKTecoBackendService.Models;
using System;

namespace Attendances.ZKTecoBackendService.Errors
{
    public class FailedHandleException : Exception
    {
        public FailedHandleException(string exception, FailedEventType kind, string handler, EventMessage msg) : base(exception)
        {
            if (msg == null)
                throw new ArgumentNullException("msg");

            FailedMessage = new FailedMessage(kind, handler, msg);
        }

        public FailedHandleException(Exception exception, FailedEventType kind, string handler, EventMessage msg) : base(exception.Message, exception)
        {
            if (msg == null)
                throw new ArgumentNullException("msg");

            FailedMessage = new FailedMessage(kind, handler, msg);
        }

        public FailedMessage FailedMessage { get; private set; }        
    }
}
