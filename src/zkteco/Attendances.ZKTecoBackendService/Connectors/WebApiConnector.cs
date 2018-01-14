using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Models;
using Attendances.ZKTecoBackendService.Utils;
using RestSharp;
using System;
using System.Net;
using System.Threading.Tasks;
using Topshelf.Logging;

namespace Attendances.ZKTecoBackendService.Connectors
{
    public class WebApiConnector : IWebApiConnector
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

            Logger.Debug("Invoking FindProjectWorkerByFaceId starts...");
            var response = await Client.ExecuteGetTaskAsync(request);
            if (!response.IsSuccessful)
            {
                if (response.ErrorException != null)
                {
                    Logger.ErrorFormat("FindProjectWorkerByFaceId error: {0}", response.ErrorMessage);
                    throw response.ErrorException;
                }
                else
                {
                    Logger.ErrorFormat("FindProjectWorkerByFaceId error: {0}", response.Content);
                    throw new WebException(response.StatusDescription);
                }                
            }

            Logger.Debug("Invoking FindProjectWorkerByFaceId ends.");
            var worker = Newtonsoft.Json.JsonConvert.DeserializeObject<WorkerDTO>(response.Content);
            if (worker != null)
            {
                return worker.UserId;
            }
            return string.Empty;
        }

        public async Task<bool> CheckIn(string projectId, string workerId, DateTime logDate, string location)
        {
            var request = new RestRequest("attendances/in");
            request.JsonSerializer = JsonSerializer.Default;
            request.AddJsonBody(new CheckInDTO(projectId, workerId, location, logDate));

            Logger.Debug("Invoking CheckIn starts...");
            var response = await Client.ExecutePostTaskAsync(request);
            Logger.Debug("Invoking CheckIn ends.");
            if (!response.IsSuccessful)
            {
                if (response.ErrorException != null)
                {
                    Logger.ErrorFormat("CheckIn error: {0}", response.ErrorMessage);
                }
                else
                {
                    Logger.ErrorFormat("CheckIn error: {0}", response.Content);
                }                    
                return false;
            }
            return true;
        }

        public async Task<bool> CheckOut(string projectId, string workerId, DateTime logDate)
        {
            var request = new RestRequest("attendances/out");
            request.JsonSerializer = JsonSerializer.Default;
            request.AddJsonBody(new CheckOutDTO(projectId, workerId, logDate));

            Logger.Debug("Invoking CheckOut starts...");
            var response = await Client.ExecutePostTaskAsync(request);
            Logger.Debug("Invoking CheckOut ends.");
            if (!response.IsSuccessful)
            {
                if (response.ErrorException != null)
                {
                    Logger.ErrorFormat("CheckOut error: {0}", response.ErrorMessage);
                }
                else
                {
                    Logger.ErrorFormat("CheckOut error: {0}", response.Content);
                }                    
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
