using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Models;
using Newtonsoft.Json;
using System;

namespace Attendances.ZKTecoBackendService.Events
{
    public class EventMessage
    {
        /// <summary>
        /// For deserialize
        /// </summary>
        public EventMessage() { }

        public EventMessage(EventType kind, IIdentityKey data)
        {
            Id = Guid.NewGuid().ToString();
            Kind = kind;
            ReferenceId = data == null ? "" : data.Id;
            Data = data;
            OccurredOn = DateTime.UtcNow;
        }

        /// <summary>
        /// It is used for initializing event message.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="kind"></param>
        /// <param name="referId"></param>
        /// <param name="json"></param>
        /// <param name="occurredOn"></param>
        public EventMessage(string id, EventType kind, string referId, string json, DateTime occurredOn)
        {
            Id = id;
            Kind = kind;
            ReferenceId = referId;
            JsonData = json;
            OccurredOn = occurredOn;
        }

        /// <summary>
        /// It is used for initializing failed message.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="referId"></param>
        /// <param name="json"></param>
        /// <param name="occurredOn"></param>
        /// <param name="retryTimes"></param>
        public EventMessage(string id, string referId, string json, DateTime occurredOn, int retryTimes)
        {
            Id = id;
            Kind = EventType.Failed;
            ReferenceId = referId;
            JsonData = json;
            OccurredOn = occurredOn;
            RetryTimes = retryTimes;
        }

        public EventType Kind { get; set; }

        public string Id { get; set; }
        /// <summary>
        /// If its Kind is Failed, this ReferenceId will be handler type full name.
        /// </summary>
        public string ReferenceId { get; set; }

        public IIdentityKey Data { get; set; }

        private string _json;
        /// <summary>
        /// Data property's json format
        /// </summary>
        public string JsonData
        {
            get
            {
                if (_json == null)
                {
                    if (Data == null)
                    {
                        _json = string.Empty;
                    }
                    else
                    {
                        _json = JsonConvert.SerializeObject(Data);
                    }
                }
                return _json;
            }

            set { _json = value; }
        }

        public DateTime OccurredOn { get; set; }

        public int RetryTimes { get; set; }
    }
}
