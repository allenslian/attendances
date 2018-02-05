using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Models;
using Attendances.ZKTecoBackendService.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Attendances.ZKTecoBackendService.Events
{
    public class FailedMessage : MessageBase
    {
        /// <summary>
        /// For deserialize
        /// </summary>
        public FailedMessage() { }

        /// <summary>
        /// Create a new Failed message instance
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="handler"></param>
        /// <param name="msg"></param>
        public FailedMessage(FailedEventType kind, string handler, EventMessage msg)
        {
            if (msg == null)
            {
                throw new ArgumentNullException("msg");
            }

            Id = Guid.NewGuid().ToString();
            Kind = kind;
            Handler = handler;
            ReferenceId = msg.Data.Id;
            Data = new ArgumentItem(msg.Id, new ArgumentItem.ArgumentValuePair("message", msg));
            OccurredOn = DateTime.UtcNow;
            RetryTimes = 0;
        }
        
        /// <summary>
        /// read from database
        /// </summary>
        /// <param name="id"></param>
        /// <param name="kind"></param>
        /// <param name="referId"></param>
        /// <param name="json"></param>
        /// <param name="occurredOn"></param>
        /// <param name="retryTimes"></param>
        /// <param name="handler"></param>
        public FailedMessage(string id, FailedEventType kind, string referId, string json, 
            DateTime occurredOn, int retryTimes, string handler)
        {
            Id = id;
            Kind = kind;
            ReferenceId = referId;
            Json = json;
            OccurredOn = occurredOn;
            RetryTimes = retryTimes;
            Handler = handler;
        }

        /// <summary>
        /// Failed kind
        /// </summary>
        public FailedEventType Kind { get; set; }        

        /// <summary>
        /// Which handler may handle this message?
        /// </summary>
        public string Handler { get; set; }        

        /// <summary>
        /// retry times
        /// </summary>
        public int RetryTimes { get; set; }

        /// <summary>
        /// Wrap event message.
        /// </summary>
        public EventMessage Event
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Json))
                {
                    return null;
                }

                try
                {
                    var arg = JsonConvert.DeserializeObject<ArgumentItem>(Json);
                    if (arg == null)
                    {
                        return null;
                    }
                    var token = JToken.Parse(arg["message"]);
                    var id = token.Value<string>("Id");
                    var kind = token.Value<int>("Kind");
                    var referId = token.Value<string>("ReferenceId");
                    var data = token.Value<JObject>("Data").ToString();
                    var date = token.Value<DateTime>("OccurredOn");
                    return new EventMessage(id, (EventType)kind, referId, data, date);
                }
                catch(Exception ex)
                {
                    return null;
                }
            }            
        }

        /// <summary>
        /// increase try times, when reaching max value, it will return false.
        /// </summary>
        /// <returns></returns>
        public bool IncreaseFailedCount()
        {
            ++RetryTimes;

            if (GlobalConfig.MaxRetryTimes <= RetryTimes)
            {
                return false;
            }
            return true;
        }
    }
}
