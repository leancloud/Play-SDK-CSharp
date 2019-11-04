using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using LeanCloud.Common;

namespace LeanCloud.Play {
    /// <summary>
    /// 创建 / 加入房间结果
    /// </summary>
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
        const string USER_SESSION_TOKEN_KEY = "X-LC-PLAY-MULTIPLAYER-SESSION-TOKEN";
        const string APPLICATION_JSON = "application/json";

        readonly Client client;

        GameRouter gameRouter;

        internal LobbyService(Client client) {
            this.client = client;
        }

        internal async Task<LobbyInfo> Authorize() {
            gameRouter = new GameRouter(client.PlayServer, client.AppId, client.AppKey, client.UserId, !client.Ssl, null);
            return await gameRouter.Authorize();
        }

        internal async Task<LobbyRoomResult> CreateRoom(string roomName) {
            LobbyInfo lobbyInfo = await gameRouter.Authorize();
            string path = "/1/multiplayer/lobby/room";
            string fullUrl = $"{lobbyInfo.Url}{path}";
            Logger.Debug(fullUrl);
            Dictionary<string, object> body = new Dictionary<string, object> {
                { "gameVersion", client.GameVersion },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion },
                { "useInsecureAddr", !client.Ssl }
            };
            if (!string.IsNullOrEmpty(roomName)) {
                body.Add("cid", roomName);
            }
            return await Request(fullUrl, lobbyInfo.SessionToken, body);
        }

        internal async Task<LobbyRoomResult> JoinRoom(string roomName, List<string> expectedUserIds, bool rejoin, bool createOnNotFound) {
            LobbyInfo lobbyInfo = await gameRouter.Authorize();
            string path = $"/1/multiplayer/lobby/room/{roomName}";
            string fullUrl = $"{lobbyInfo.Url}{path}";
            Dictionary<string, object> body = new Dictionary<string, object> {
                { "cid", roomName },
                { "gameVersion", client.GameVersion },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion },
                { "useInsecureAddr", !client.Ssl }
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
            return await Request(fullUrl, lobbyInfo.SessionToken, body);
        }

        internal async Task<LobbyRoomResult> JoinRandomRoom(PlayObject matchProperties, List<string> expectedUserIds) {
            LobbyInfo lobbyInfo = await gameRouter.Authorize();
            string path = $"/1/multiplayer/lobby/match/room";
            string fullUrl = $"{lobbyInfo.Url}{path}";
            Dictionary<string, object> body = new Dictionary<string, object> {
                { "gameVersion", client.GameVersion },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion },
                { "useInsecureAddr", !client.Ssl }
            };
            if (matchProperties != null) {
                body.Add("expectAttr", matchProperties.Data);
            }
            if (expectedUserIds != null) {
                body.Add("expectMembers", expectedUserIds);
            }
            return await Request(fullUrl, lobbyInfo.SessionToken, body);
        }

        internal async Task<LobbyRoomResult> MatchRandom(string piggybackUserId, PlayObject matchProperties, List<string> expectedUserIds) {
            LobbyInfo lobbyInfo = await gameRouter.Authorize();
            string path = "/1/multiplayer/lobby/match/room";
            string fullUrl = $"{lobbyInfo.Url}{path}";
            Dictionary<string, object> body = new Dictionary<string, object> {
                { "gameVersion", client.GameVersion },
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
            return await Request(fullUrl, lobbyInfo.SessionToken, body);
        }

        async Task<LobbyRoomResult> Request(string url, string sessionToken, Dictionary<string, object> body) {
            HttpClient httpClient = null;
            HttpRequestMessage request = null;
            HttpResponseMessage response = null;
            try {
                httpClient = new HttpClient();
                string data = JsonConvert.SerializeObject(body);
                request = new HttpRequestMessage {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Post,
                    Content = new StringContent(data)
                };
                AddHeaders(request.Content.Headers);
                request.Content.Headers.Add(USER_SESSION_TOKEN_KEY, sessionToken);
                HttpUtils.PrintRequest(httpClient, request, data);
                response = await httpClient.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();
                HttpUtils.PrintResponse(response, content);
                if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.Ambiguous) {
                    return JsonConvert.DeserializeObject<LobbyRoomResult>(content);
                }
                PlayException exception = JsonConvert.DeserializeObject<PlayException>(content);
                throw exception;
            } finally {
                if (httpClient != null) {
                    httpClient.Dispose();
                }
                if (request != null) {
                    request.Dispose();
                }
                if (response != null) {
                    response.Dispose();
                }
            }
        }

        void AddHeaders(HttpContentHeaders headers) {
            headers.Add("X-LC-ID", client.AppId);
            headers.Add("X-LC-KEY", client.AppKey);
            headers.Add("X-LC-PLAY-USER-ID", client.UserId);
            headers.ContentType = new MediaTypeHeaderValue(APPLICATION_JSON);
        }
    }
}
