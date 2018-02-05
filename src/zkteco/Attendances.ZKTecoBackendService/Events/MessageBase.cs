using Attendances.ZKTecoBackendService.Interfaces;
using Newtonsoft.Json;
using System;

namespace Attendances.ZKTecoBackendService.Events
{
    public class MessageBase
    {
        /// <summary>
        /// Failed message id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// It is Data's identity.
        /// </summary>
        public string ReferenceId { get; set; }

        /// <summary>
        /// Data is one type implemented IIdentityKey interface.
        /// </summary>
        public IIdentityKey Data { get; set; }

        /// <summary>
        /// A date which Failed message occurred on
        /// </summary>
        public DateTime OccurredOn { get; set; }

        /// <summary>
        /// Inner json format.
        /// </summary>
        protected string Json { get; set; }

        public string DataToJSON()
        {
            if (Data == null)
            {
                return string.Empty;
            }

            return JsonConvert.SerializeObject(Data);
        }

    }
}
