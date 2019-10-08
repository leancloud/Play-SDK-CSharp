using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LeanCloud.Play {
    internal class LobbyInfo {
        [JsonProperty("url")]
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

        internal GameRouter(string appId, string appKey, string userId, bool insecure, string feature) {
            this.appId = appId;
            this.appKey = appKey;
            this.userId = userId;
            this.insecure = insecure;
            this.feature = feature;

            appRouter = new AppRouter(appId, null);
        }

        internal async Task<LobbyInfo> Authorize() {
            if (lobbyInfo != null && lobbyInfo.IsValid) {
                return lobbyInfo;
            }
            string url = await appRouter.Fetch();
            Dictionary<string, object> data = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(feature)) {
                data.Add("feature", feature);
            }
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get,
                Content = new StringContent(JsonConvert.SerializeObject(data))
            };
            request.Headers.Add("X-LC-ID", appId);
            request.Headers.Add("X-LC-KEY", appKey);
            request.Headers.Add("X-LC-PLAY-USER-ID", userId);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            try {
                HttpResponseMessage response = await client.SendAsync(request);
                client.Dispose();
                request.Dispose();
                string content = await response.Content.ReadAsStringAsync();
                Logger.Debug(content);
                response.Dispose();

                return JsonConvert.DeserializeObject<LobbyInfo>(content);
            } catch (Exception e) {
                Logger.Error(e.Message);
            }
            return null;
        }
    }
}
