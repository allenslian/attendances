using RestSharp.Deserializers;
using RestSharp.Serializers;
using System;

namespace Attendances.ZKTecoBackendService.Models
{
    public class WorkerDTO
    {
        public WorkerDTO() { }

        /// <summary>
        /// A worker dto.
        /// </summary>
        /// <param name="enrollNumber">An id on the device</param>
        /// <param name="userId">An id on the web site</param>
        /// <param name="projectId"></param>
        public WorkerDTO(string enrollNumber, string userId, string projectId)
        {
            EnrollNumber = enrollNumber;
            UserId = userId;
            ProjectId = projectId;
        }

        public string EnrollNumber { get; private set; }

        [DeserializeAs(Name = "_id")]
        public string UserId { get; private set; }

        public string ProjectId { get; private set; }
    }

    public class CheckInDTO
    {
        public CheckInDTO(string projectId, string workerId, string location, DateTime logDate)
        {
            ProjectId = projectId;
            WorkerId = workerId;
            Location = location;
            CheckInDate = logDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        [SerializeAs(Name = "pid")]
        public string ProjectId { get; private set; }

        [SerializeAs(Name = "wid")]
        public string WorkerId { get; private set; }

        [SerializeAs(Name = "loc")]
        public string Location { get; private set; }

        [SerializeAs(Name = "in")]
        public string CheckInDate { get; private set; }
    }

    public class CheckOutDTO
    {
        public CheckOutDTO(string projectId, string workerId, DateTime logDate)
        {
            ProjectId = projectId;
            WorkerId = workerId;
            CheckOutDate = logDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        [SerializeAs(Name = "pid")]
        public string ProjectId { get; private set; }

        [SerializeAs(Name = "wid")]
        public string WorkerId { get; private set; }

        [SerializeAs(Name = "out")]
        public string CheckOutDate { get; private set; }
    }


}
