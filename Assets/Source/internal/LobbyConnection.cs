using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;

namespace LeanCloud.Play {
    internal class LobbyConnection : Connection {
        internal Action<List<LobbyRoom>> OnRoomListUpdated;

        internal async Task JoinLobby() {
            var request = NewRequest();
            await SendRequest(CommandType.Lobby, OpType.Add, request);
        }

        internal async Task LeaveLobby() {
            RequestMessage request = NewRequest();
            await SendRequest(CommandType.Lobby, OpType.Remove, request);
        }

        protected override int GetPingDuration() {
            return 20;
        }

        protected override string GetFastOpenUrl(string server, string appId, string gameVersion, string userId, string sessionToken) {
            return $"{server}/1/multiplayer/lobby/websocket?appId={appId}&sdkVersion={Config.SDKVersion}&protocolVersion={Config.ProtocolVersion}&gameVersion={gameVersion}&userId={userId}&sessionToken={sessionToken}";
        }

        protected override void HandleNotification(CommandType cmd, OpType op, Body body) {
            switch (cmd) {
                case CommandType.Lobby:
                    switch (op) {
                        case OpType.RoomList:
                            HandleRoomListMsg(body);
                            break;
                        default:
                            HandleUnknownMsg(cmd, op, body);
                            break;
                    }
                    break;
                case CommandType.Statistic:
                    break;
                case CommandType.Error:
                    HandleErrorMsg(body);
                    break;
                default:
                    HandleUnknownMsg(cmd, op, body);
                    break;
            }
        }

        void HandleRoomListMsg(Body body) {
            List<LobbyRoom> LobbyRoomList = new List<LobbyRoom>();
            foreach (var roomOpts in body.RoomList.List) {
                var lobbyRoom = ConvertToLobbyRoom(roomOpts);
                LobbyRoomList.Add(lobbyRoom);
            }
            OnRoomListUpdated?.Invoke(LobbyRoomList);
        }

        LobbyRoom ConvertToLobbyRoom(Protocol.RoomOptions options) {
            var lobbyRoom = new LobbyRoom {
                RoomName = options.Cid,
                Open = options.Open == null || options.Open.Value,
                Visible = options.Visible == null || options.Visible.Value,
                MaxPlayerCount = options.MaxMembers,
                PlayerCount = options.MemberCount,
                EmptyRoomTtl = options.EmptyRoomTtl,
                PlayerTtl = options.PlayerTtl
            };
            if (options.ExpectMembers != null) {
                lobbyRoom.ExpectedUserIds = options.ExpectMembers.ToList<string>();
            }
            if (options.Attr != null) {
                lobbyRoom.CustomRoomProperties = CodecUtils.DeserializePlayObject(options.Attr);
            }
            return lobbyRoom;
        }
    }
}
