using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using LeanCloud.Play.Protocol;

namespace LeanCloud.Play {
    internal class LobbyConnection : Connection {
        internal LobbyConnection() {

        }

        internal static async Task<LobbyConnection> Connect(string appId, string server, string userId, string gameVersion) {
            LobbyConnection connection = new LobbyConnection();
            await connection.Connect(server, userId);
            await connection.OpenSession(appId, userId, gameVersion);
            return connection;
        }

        internal async Task JoinLobby() {
            var msg = Message.NewRequest("lobby", "add");
            await Send(msg);
        }

        internal async Task<LobbyRoomResult> CreateRoom(string roomName, RoomOptions roomOptions, List<string> expectedUserIds) {
            var request = NewRequest();
            var roomOpts = Utils.ConvertToRoomOptions(roomName, roomOptions, expectedUserIds);
            request.CreateRoom = new CreateRoomRequest {
                RoomOptions = roomOpts
            };
            var res = await SendRequest(CommandType.Conv, OpType.Start, request);
            var roomRes = res.CreateRoom;
            return new LobbyRoomResult {
                RoomId = roomRes.RoomOptions.Cid,
                PrimaryUrl = roomRes.Addr,
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
            var roomRes = res.JoinRoom;
            return new LobbyRoomResult { 
                RoomId = roomRes.RoomOptions.Cid,
                PrimaryUrl = roomRes.Addr
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
            var roomRes = res.JoinRoom;
            return new LobbyRoomResult { 
                RoomId = roomRes.RoomOptions.Cid,
                PrimaryUrl = roomRes.Addr
            };
        }

        internal async Task<LobbyRoomResult> JoinRandomRoom(PlayObject matchProperties, List<string> expectedUserIds) {
            var request = NewRequest();
            request.JoinRoom = new JoinRoomRequest();
            if (matchProperties != null) {
                request.JoinRoom.ExpectAttr = CodecUtils.EncodePlayObject(matchProperties);
            }
            if (expectedUserIds != null) {
                request.JoinRoom.RoomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            var res = await SendRequest(CommandType.Conv, OpType.AddRandom, request);
            var roomRes = res.JoinRoom;
            return new LobbyRoomResult { 
                RoomId = roomRes.RoomOptions.Cid,
                PrimaryUrl = roomRes.Addr
            };
        }

        internal async Task<LobbyRoomResult> JoinOrCreateRoom(string roomName, RoomOptions roomOptions, List<string> expectedUserIds)  {
            var msg = Message.NewRequest("conv", "add");
            msg["cid"] = roomName;
            msg["createOnNotFound"] = true;
            if (roomOptions != null) {
                var roomOptionsDict = roomOptions.ToDictionary();
                foreach (var entry in roomOptionsDict) {
                    msg[entry.Key] = entry.Value;
                }
            }
            if (expectedUserIds != null) {
                List<object> expecteds = expectedUserIds.Cast<object>().ToList();
                msg["expectMembers"] = expecteds;
            }
            var res = await Send(msg);
            return new LobbyRoomResult {
                Create = res.Op == "started",
                RoomId = res["cid"].ToString(),
                PrimaryUrl = res["addr"].ToString()
            };
        }

        internal async Task<LobbyRoom> MatchRandom(PlayObject matchProperties, List<string> expectedUserIds) {
            var request = NewRequest();
            if (matchProperties != null) {
                request.JoinRoom.ExpectAttr = CodecUtils.EncodePlayObject(matchProperties);
            }
            if (expectedUserIds != null) {
                request.JoinRoom.RoomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            var res = await SendRequest(CommandType.Conv, OpType.MatchRandom, request);
            // TODO 返回 LobbyRoom

            return null;
        }

        protected override int GetPingDuration() {
            return 20;
        }
    }
}