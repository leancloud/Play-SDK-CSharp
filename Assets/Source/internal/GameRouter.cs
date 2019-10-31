using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Newtonsoft.Json;

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
        readonly string appId;
        readonly string appKey;
        readonly string userId;
        readonly bool insecure;
        readonly string feature;

        AppRouter appRouter;
        LobbyInfo lobbyInfo;

        readonly HttpClient client;

        internal GameRouter(string appId, string appKey, string userId, bool insecure, string feature) {
            this.appId = appId;
            this.appKey = appKey;
            this.userId = userId;
            this.insecure = insecure;
            this.feature = feature;

            appRouter = new AppRouter(appId, null);

            client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-LC-ID", appId);
            client.DefaultRequestHeaders.Add("X-LC-KEY", appKey);
            client.DefaultRequestHeaders.Add("X-LC-PLAY-USER-ID", userId);
        }

        internal async Task<LobbyInfo> Authorize() {
            if (lobbyInfo != null && lobbyInfo.IsValid) {
                return lobbyInfo;
            }
            return await AuthorizeFromServer();
        }

        async Task<LobbyInfo> AuthorizeFromServer() {
            string url = await appRouter.Fetch();
            HttpRequestMessage request = null;
            HttpResponseMessage response = null;
            try {
                Dictionary<string, object> data = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(feature)) {
                    data.Add("feature", feature);
                }
                string dataContent = JsonConvert.SerializeObject(data);
                request = new HttpRequestMessage {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Post,
                    Content = new StringContent(dataContent)
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                Utils.PrintRequest(client, request, dataContent);
                response = await client.SendAsync(request);
                
                string content = await response.Content.ReadAsStringAsync();
                Utils.PrintResponse(response, content);
                if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.Ambiguous) {
                    return JsonConvert.DeserializeObject<LobbyInfo>(content);
                }
                PlayException exception = JsonConvert.DeserializeObject<PlayException>(content);
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
