using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;
using Google.Protobuf;

namespace LeanCloud.Play {
    internal class LobbyConnection : Connection {
        internal Action<List<LobbyRoom>> OnRoomListUpdated;


        internal async Task JoinLobby() {
            var request = NewRequest();
            await SendRequest(CommandType.Lobby, OpType.Add, request);
        }

        internal async Task<LobbyRoomResult> CreateRoom(string roomName, RoomOptions roomOptions, List<string> expectedUserIds) {
            var request = NewRequest();
            var roomOpts = Utils.ConvertToRoomOptions(roomName, roomOptions, expectedUserIds);
            request.CreateRoom = new CreateRoomRequest {
                RoomOptions = roomOpts
            };
            var res = await SendRequest(CommandType.Conv, OpType.Start, request);
            var roomRes = res.Response.CreateRoom;
            return new LobbyRoomResult {
                RoomId = roomRes.RoomOptions.Cid,
                Url = roomRes.Addr
            };
        }

        internal async Task<LobbyRoomResult> JoinRoom(string roomName, List<string> expectedUserIds) {
            var request = NewRequest();
            request.JoinRoom = new JoinRoomRequest {
                RoomOptions = new Protocol.RoomOptions { 
                    Cid = roomName
                }
            };
            if (expectedUserIds != null) {
                request.JoinRoom.RoomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            var res = await SendRequest(CommandType.Conv, OpType.Add, request);
            var roomRes = res.Response.JoinRoom;
            return new LobbyRoomResult { 
                RoomId = roomRes.RoomOptions.Cid,
                Url = roomRes.Addr
            };
        }

        internal async Task<LobbyRoomResult> RejoinRoom(string roomName) {
            var request = NewRequest();
            request.JoinRoom = new JoinRoomRequest {
                Rejoin = true,
                RoomOptions = new Protocol.RoomOptions {
                    Cid = roomName
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.Add, request);
            var roomRes = res.Response.JoinRoom;
            return new LobbyRoomResult { 
                RoomId = roomRes.RoomOptions.Cid,
                Url = roomRes.Addr
            };
        }

        internal async Task<LobbyRoomResult> JoinRandomRoom(PlayObject matchProperties, List<string> expectedUserIds) {
            var request = NewRequest();
            request.JoinRoom = new JoinRoomRequest();
            if (matchProperties != null) {
                request.JoinRoom.ExpectAttr = ByteString.CopyFrom(CodecUtils.SerializePlayObject(matchProperties));
            }
            if (expectedUserIds != null) {
                request.JoinRoom.RoomOptions = new Protocol.RoomOptions();
                request.JoinRoom.RoomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            var res = await SendRequest(CommandType.Conv, OpType.AddRandom, request);
            var roomRes = res.Response.JoinRoom;
            return new LobbyRoomResult { 
                RoomId = roomRes.RoomOptions.Cid,
                Url = roomRes.Addr
            };
        }

        internal async Task<LobbyRoomResult> JoinOrCreateRoom(string roomName, RoomOptions roomOptions, List<string> expectedUserIds)  {
            var request = NewRequest();
            request.JoinRoom = new JoinRoomRequest {
                RoomOptions = Utils.ConvertToRoomOptions(roomName, roomOptions, expectedUserIds),
                CreateOnNotFound = true
            };
            var res = await SendRequest(CommandType.Conv, OpType.Add, request);
            if (res.Op == OpType.Started) {
                return new LobbyRoomResult {
                    Create = true,
                    RoomId = res.Response.CreateRoom.RoomOptions.Cid,
                    Url = res.Response.CreateRoom.Addr
                };
            }
            return new LobbyRoomResult { 
                Create = false,
                RoomId = res.Response.JoinRoom.RoomOptions.Cid,
                Url = res.Response.JoinRoom.Addr
            };
        }

        internal async Task<LobbyRoom> MatchRandom(string piggybackUserId, PlayObject matchProperties, List<string> expectedUserIds) {
            var request = NewRequest();
            request.JoinRoom = new JoinRoomRequest { 
                PiggybackPeerId = piggybackUserId
            };
            if (matchProperties != null) {
                request.JoinRoom.ExpectAttr = ByteString.CopyFrom(CodecUtils.SerializePlayObject(matchProperties));
            }
            if (expectedUserIds != null) {
                request.JoinRoom.RoomOptions = new Protocol.RoomOptions();
                request.JoinRoom.RoomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            var res = await SendRequest(CommandType.Conv, OpType.MatchRandom, request);
            return Utils.ConvertToLobbyRoom(res.Response.JoinRoom.RoomOptions);
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
                var lobbyRoom = Utils.ConvertToLobbyRoom(roomOpts);
                LobbyRoomList.Add(lobbyRoom);
            }
            OnRoomListUpdated?.Invoke(LobbyRoomList);
        }
    }
}
