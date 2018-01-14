using Attendances.ZKTecoBackendService.Models;
using System;

namespace Attendances.ZKTecoBackendService.Errors
{
    [Obsolete]
    public class FailedHandleException : Exception
    {
        public FailedHandleException(Exception exception, ArgumentItem attached) : base(exception.Message, exception)
        {
            Argument = attached;
        }

        public ArgumentItem Argument { get; private set; }
    }
}
