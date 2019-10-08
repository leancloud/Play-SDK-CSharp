using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace LeanCloud.Play {
    internal class AppRouterInfo {
        [JsonProperty("multiplayer_router_server")]
        internal string PrimaryUrl {
            get; set;
        }

        [JsonProperty("play_server")]
        internal string SecondaryUrl {
            get; set;
        }

        [JsonProperty("ttl")]
        internal long Ttl {
            get; set;
        }

        DateTimeOffset createAt;

        internal AppRouterInfo() {
            createAt = DateTimeOffset.Now;
        }

        internal string Url {
            get {
                return $"https://{PrimaryUrl ?? SecondaryUrl}/1/multiplayer/router/route";
            }
        }

        internal bool IsValid {
            get {
                return DateTimeOffset.Now < createAt + TimeSpan.FromSeconds(Ttl);
            }
        }
    }

    internal class AppRouter {
        readonly string appId;
        readonly string playServer;

        AppRouterInfo appInfo;

        readonly SemaphoreSlim locker = new SemaphoreSlim(1);

        internal AppRouter(string appId, string playServer) {
            this.appId = appId;
            this.playServer = playServer;
        }

        internal async Task<string> Fetch() {
            if (playServer != null) {
                return $"{playServer}/1/multiplayer/router/route";
            }
            if (appInfo != null && appInfo.IsValid) {
                Logger.Debug("Get server from cache");
                return appInfo.Url;
            }

            await locker.WaitAsync();
            try {
                if (appInfo == null) {
                    appInfo = await FetchFromServer();
                }
                return appInfo.Url;
            } finally {
                locker.Release();
            }
        }

        async Task<AppRouterInfo> FetchFromServer() {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri($"https://app-router.leancloud.cn/2/route?appId={appId}"),
                Method = HttpMethod.Get
            };
            HttpResponseMessage response = await client.SendAsync(request);
            client.Dispose();
            request.Dispose();

            string content = await response.Content.ReadAsStringAsync();
            response.Dispose();

            return JsonConvert.DeserializeObject<AppRouterInfo>(content);
        }
    }
}
