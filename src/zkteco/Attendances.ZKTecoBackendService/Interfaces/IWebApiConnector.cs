using System;
using System.Threading.Tasks;

namespace Attendances.ZKTecoBackendService.Interfaces
{
    public interface IWebApiConnector : IDisposable
    {
        Task<string> FindProjectWorkerByFaceId(string faceId, string projectId);

        Task<bool> CheckIn(string projectId, string workerId, DateTime logDate, string location);

        Task<bool> CheckOut(string projectId, string workerId, DateTime logDate);
    }
}
