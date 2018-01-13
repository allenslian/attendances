using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Models;
using Newtonsoft.Json;
using System;

namespace Attendances.ZKTecoBackendService.Events
{
    public class EventMessage
    {     
        public EventMessage(EventType kind, IIdentityKey data)
        {
            Id = Guid.NewGuid().ToString();
            Kind = kind;
            ReferenceId = data == null ? "" : data.Id;
            Data = data;
            OccurredOn = DateTime.UtcNow;
        }

        public EventMessage(string id, EventType kind, string referId, string json, DateTime occurredOn)
        {
            Id = id;
            Kind = kind;
            ReferenceId = referId;
            Data = JsonConvert.DeserializeObject<IIdentityKey>(json);
            OccurredOn = occurredOn;
        }

        public EventType Kind { get; private set; }

        public string Id { get; private set; }
        /// <summary>
        /// If its Kind is Failed, this ReferenceId will be handler type full name.
        /// </summary>
        public string ReferenceId { get; private set; }

        public IIdentityKey Data { get; private set; }

        public DateTime OccurredOn { get; private set; }

        public override string ToString()
        {
            if (null == Data)
            {
                return string.Empty;
            }
            return JsonConvert.SerializeObject(Data);
        }
    }
}
