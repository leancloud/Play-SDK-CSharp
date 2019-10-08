using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace LeanCloud.Play {
    internal class LobbyRoomResult {
        [JsonProperty("cid")]
        internal string RoomId {
            get; set;
        }

        [JsonProperty("addr")]
        internal string Url {
            get; set;
        }

        [JsonProperty("roomCreated")]
        internal bool Create {
            get; set;
        }
    }

    internal class LobbyService {
        readonly string appId;
        readonly string appKey;
        readonly string userId;
        bool insecure;
        string feature;

        GameRouter gameRouter;

        internal LobbyService(string appId, string appKey, string userId) {
            this.appId = appId;
            this.appKey = appKey;
            this.userId = userId;
        }

        internal async Task<LobbyInfo> Authorize() {
            gameRouter = new GameRouter(appId, appKey, userId, insecure, feature);
            return await gameRouter.Authorize();
        }

        internal async Task<LobbyRoomResult> CreateRoom(string roomName) {
            LobbyInfo lobbyInfo = await gameRouter.Authorize();
            string path = "/1/multiplayer/lobby/room";
            string fullUrl = $"{lobbyInfo.Url}{path}";
            Dictionary<string, object> body = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(roomName)) {
                body.Add("cid", roomName);
            }
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(fullUrl),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(body))
            };
            HttpResponseMessage response = await client.SendAsync(request);
            client.Dispose();
            request.Dispose();
            string content = await response.Content.ReadAsStringAsync();
            response.Dispose();

            return JsonConvert.DeserializeObject<LobbyRoomResult>(content);
        }

        internal async Task<LobbyRoomResult> JoinRoom(string roomName, List<string> expectedUserIds, bool rejoin, bool createOnNotFound) {
            LobbyInfo lobbyInfo = await gameRouter.Authorize();
            string path = $"/1/multiplayer/lobby/room/{roomName}";
            string fullUrl = $"{lobbyInfo.Url}{path}";
            Dictionary<string, object> body = new Dictionary<string, object> {
                { "cid", roomName },
                { "gameVersion", "0.0.1" },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion }
            };
            if (expectedUserIds != null) {
                body.Add("expectMembers", expectedUserIds);
            }
            if (rejoin) {
                body.Add("rejoin", rejoin);
            }
            if (createOnNotFound) {
                body.Add("createOnNotFound", createOnNotFound);
            }
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(fullUrl),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(body))
            };
            HttpResponseMessage response = await client.SendAsync(request);
            client.Dispose();
            request.Dispose();
            string content = await response.Content.ReadAsStringAsync();
            response.Dispose();

            return JsonConvert.DeserializeObject<LobbyRoomResult>(content);
        }

        internal async Task<LobbyRoomResult> JoinRandomRoom(PlayObject matchProperties, List<string> expectedUserIds) {
            LobbyInfo lobbyInfo = await gameRouter.Authorize();
            string path = $"/1/multiplayer/lobby/room/match";
            string fullUrl = $"{lobbyInfo.Url}{path}";
            Dictionary<string, object> body = new Dictionary<string, object> {
                { "gameVersion", "0.0.1" },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion }
            };
            if (matchProperties != null) {
                body.Add("expectAttr", matchProperties.Data);
            }
            if (expectedUserIds != null) {
                body.Add("expectMembers", expectedUserIds);
            }
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(fullUrl),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(body))
            };
            HttpResponseMessage response = await client.SendAsync(request);
            client.Dispose();
            request.Dispose();
            string content = await response.Content.ReadAsStringAsync();
            response.Dispose();

            return JsonConvert.DeserializeObject<LobbyRoomResult>(content);
        }

        internal async Task<LobbyRoomResult> MatchRandom(string piggybackUserId, PlayObject matchProperties, List<string> expectedUserIds) {
            LobbyInfo lobbyInfo = await gameRouter.Authorize();
            string path = "/1/multiplayer/lobby/room/match";
            string fullUrl = $"{lobbyInfo.Url}{path}";
            Dictionary<string, object> body = new Dictionary<string, object> {
                { "gameVersion", "0.0.1" },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion },
                { "piggybackPeerId", piggybackUserId }
            };
            if (matchProperties != null) {
                body.Add("expectAttr", matchProperties.Data);
            }
            if (expectedUserIds != null) {
                body.Add("expectMembers", expectedUserIds);
            }
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(fullUrl),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(body))
            };
            HttpResponseMessage response = await client.SendAsync(request);
            client.Dispose();
            request.Dispose();
            string content = await response.Content.ReadAsStringAsync();
            response.Dispose();

            return JsonConvert.DeserializeObject<LobbyRoomResult>(content);
        }
    }
}
