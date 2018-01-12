using Attendances.ZKTecoBackendService.Models;
using Attendances.ZKTecoBackendService.Utils;
using RestSharp;
using System;
using System.Net;
using System.Threading.Tasks;
using Topshelf.Logging;

namespace Attendances.ZKTecoBackendService.Connectors
{
    public class WebApiConnector
    {
        private RestClient Client { get; set; }

        private LogWriter Logger { get; set; }

        public WebApiConnector()
        {
            Logger = HostLogger.Get<WebApiConnector>();

            // It fixes the following error:
            // The underlying connection was closed: An unexpected error occurred on a send.
            // https://stackoverflow.com/questions/22627977/the-underlying-connection-was-closed-an-unexpected-error-occurred-on-a-send
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Client = new RestClient(new Uri(GlobalConfig.ApiRootUrl));
            Client.AddDefaultHeader("Accept", "application/json");
            Client.AddDefaultHeader("Authorization", string.Format("Bearer {0}", GlobalConfig.ApiToken));
        }

        public async Task<string> FindProjectWorkerByFaceId(string faceId, string projectId)
        {
            var request = new RestRequest(string.Format("projects/{0}/workers/{1}", projectId, faceId));
            try
            {
                Logger.Debug("Invoking FindProjectWorkerByFaceId starts...");
                var worker = await Client.GetTaskAsync<WorkerDTO>(request);
                Logger.Debug("Invoking FindProjectWorkerByFaceId ends.");
                if (worker != null)
                {
                    return worker.UserId;
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("FindProjectWorkerByFaceId error: {@ex}", ex);
                throw;
            }
            return await Task.FromResult("");
        }

        public async Task<bool> CheckIn(string projectId, string workerId, DateTime logDate, string location)
        {
            var request = new RestRequest("attendances/in");
            request.AddObject(new CheckInDTO(projectId, workerId, location, logDate));
            try
            {
                Logger.Debug("Invoking CheckIn starts...");
                await Client.ExecutePostTaskAsync(request);
                Logger.Debug("Invoking CheckIn ends.");
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("CheckIn error: {@ex}", ex);
                return false;
            }
            return true;
        }

        public async Task<bool> CheckOut(string projectId, string workerId, DateTime logDate)
        {
            var request = new RestRequest("attendances/out");
            request.AddObject(new CheckOutDTO(projectId, workerId, logDate));
            try
            {
                Logger.Debug("Invoking CheckOut starts...");
                await Client.ExecutePostTaskAsync(request);
                Logger.Debug("Invoking CheckOut ends.");
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("CheckOut error: {@ex}", ex);
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            Client = null;
        }
    }
}
