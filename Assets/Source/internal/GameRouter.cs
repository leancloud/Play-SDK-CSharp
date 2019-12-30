using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Newtonsoft.Json;
using LeanCloud.Common;

namespace LeanCloud.Play {
    internal class LobbyInfo {
        [JsonProperty("lobbyAddr")]
        internal string Url {
            get; set;
        }

        [JsonProperty("sessionToken")]
        internal string SessionToken {
            get; set;
        }

        [JsonProperty("ttl")]
        internal long Ttl {
            get; set;
        }

        DateTimeOffset createAt;

        internal LobbyInfo() {
            createAt = DateTimeOffset.Now;
        }

        internal bool IsValid {
            get {
                return DateTimeOffset.Now < createAt + TimeSpan.FromSeconds(Ttl);
            }
        }
    }

    internal class GameRouter {
        readonly AppRouterController appRouterController;

        LobbyInfo lobbyInfo;

        readonly HttpClient httpClient;

        internal GameRouter(Client client) {
            string appId = client.AppId;
            string appKey = client.AppKey;
            string server = client.PlayServer;
            string userId = client.UserId;

            appRouterController = new AppRouterController(appId, server);

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-LC-ID", appId);
            httpClient.DefaultRequestHeaders.Add("X-LC-KEY", appKey);
            httpClient.DefaultRequestHeaders.Add("X-LC-PLAY-USER-ID", userId);
        }

        internal async Task<LobbyInfo> Authorize() {
            if (lobbyInfo != null && lobbyInfo.IsValid) {
                return lobbyInfo;
            }
            return await AuthorizeFromServer();
        }

        async Task<LobbyInfo> AuthorizeFromServer() {
            AppRouter appRouter = await appRouterController.Get();
            HttpRequestMessage request = null;
            HttpResponseMessage response = null;
            try {
                Dictionary<string, object> data = new Dictionary<string, object>();
                string dataContent = JsonConvert.SerializeObject(data);
                string url = $"{appRouter.PlayServer}/1/multiplayer/router/authorize";
                if (!Uri.IsWellFormedUriString(appRouter.PlayServer, UriKind.Absolute)) {
                    url = $"https://{appRouter.PlayServer}/1/multiplayer/router/authorize";
                }
                request = new HttpRequestMessage {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Post,
                    Content = new StringContent(dataContent)
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpUtils.PrintRequest(httpClient, request, dataContent);
                response = await httpClient.SendAsync(request);
                
                string content = await response.Content.ReadAsStringAsync();
                HttpUtils.PrintResponse(response, content);
                if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.Ambiguous) {
                    lobbyInfo = await JsonUtils.DeserializeObjectAsync<LobbyInfo>(content);
                    return lobbyInfo;
                }
                PlayException exception = await JsonUtils.DeserializeObjectAsync<PlayException>(content);
                throw exception;
            } finally {
                if (request != null) {
                    request.Dispose();
                }
                if (response != null) {
                    response.Dispose();
                }
            }
        }
    }
}
