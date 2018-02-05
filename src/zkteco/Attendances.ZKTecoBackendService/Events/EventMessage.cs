using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Models;
using Newtonsoft.Json;
using System;

namespace Attendances.ZKTecoBackendService.Events
{
    public class EventMessage : MessageBase
    {
        /// <summary>
        /// For deserialize
        /// </summary>
        public EventMessage() { }

        public EventMessage(EventType kind, IIdentityKey data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            Id = Guid.NewGuid().ToString();
            Kind = kind;
            ReferenceId = data.Id;
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
            Json = json;
            OccurredOn = occurredOn;
        }        

        public EventType Kind { get; set; }

        /// <summary>
        /// Convert Data from json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ConvertFromJSON<T>() where T : IIdentityKey
        {
            if (string.IsNullOrWhiteSpace(Json))
            {
                return default(T);                
            }

            var instance = JsonConvert.DeserializeObject<T>(Json);
            if (instance != null)
            {
                Data = instance;
            }
            return instance;
        }
    }
}
