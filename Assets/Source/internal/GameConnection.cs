using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using LeanCloud.Play.Protocol;

namespace LeanCloud.Play {
    internal class GameConnection : Connection {
        internal Room Room {
            get; private set;
        }

        internal GameConnection() {
        
        }

        internal static async Task<GameConnection> Connect(string appId, string server, string userId, string gameVersion) {
            var tcs = new TaskCompletionSource<GameConnection>();
            var connection = new GameConnection();
            await connection.Connect(server, userId);
            await connection.OpenSession(appId, userId, gameVersion);
            return connection;
        }

        internal async Task<Room> CreateRoom(string roomId, RoomOptions roomOptions, List<string> expectedUserIds) {
            var request = NewRequest();
            var roomOpts = Utils.ConvertToRoomOptions(roomId, roomOptions, expectedUserIds);
            request.CreateRoom = new CreateRoomRequest { 
                RoomOptions = roomOpts
            };
            var res = await SendRequest(CommandType.Conv, OpType.Start, request);
            return Utils.ConvertToRoom(res.CreateRoom.RoomOptions);
        }

        internal async Task<Room> JoinRoom(string roomId, List<string> expectedUserIds) {
            var request = NewRequest();
            request.JoinRoom = new JoinRoomRequest {
                Rejoin = false,
                RoomOptions = new Protocol.RoomOptions {
                    Cid = roomId
                },
            };
            if (expectedUserIds != null) {
                request.JoinRoom.RoomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            var res = await SendRequest(CommandType.Conv, OpType.Add, request);
            return Utils.ConvertToRoom(res.JoinRoom.RoomOptions);
        }

        internal async Task LeaveRoom() {
            await SendRequest(CommandType.Conv, OpType.Remove, null);
        }

        internal async Task<PlayObject> SetRoomOpen(bool open) {
            var request = NewRequest();
            request.UpdateSysProperty = new UpdateSysPropertyRequest { 
                SysAttr = new RoomSystemProperty { 
                    Open = open
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> SetRoomVisible(bool visible) {
            var request = NewRequest();
            request.UpdateSysProperty = new UpdateSysPropertyRequest { 
                SysAttr = new RoomSystemProperty { 
                    Visible = visible
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> SetRoomMaxPlayerCount(int count) {
            var request = NewRequest();
            request.UpdateSysProperty = new UpdateSysPropertyRequest { 
                SysAttr = new RoomSystemProperty { 
                    MaxMembers = count
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> SetRoomExpectedUserIds(List<string> expectedUserIds) {
            var request = NewRequest();
            var args = new Dictionary<string, object> {
                { "$set", expectedUserIds.ToList<object>() }
            };
            request.UpdateSysProperty = new UpdateSysPropertyRequest {
                SysAttr = new RoomSystemProperty { 
                    ExpectMembers = Json.Encode(args)
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> ClearRoomExpectedUserIds() {
            var request = NewRequest();
            var args = new Dictionary<string, object> {
                { "$drop", true }
            };
            request.UpdateSysProperty = new UpdateSysPropertyRequest {
                SysAttr = new RoomSystemProperty { 
                    ExpectMembers = Json.Encode(args)
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> AddRoomExpectedUserIds(List<string> expectedUserIds) {
            var request = NewRequest();
            var args = new Dictionary<string, object> {
                { "$add", expectedUserIds.ToList<object>() }
            };
            request.UpdateSysProperty = new UpdateSysPropertyRequest {
                SysAttr = new RoomSystemProperty {
                    ExpectMembers = Json.Encode(args)
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> RemoveRoomExpectedUserIds(List<string> expectedUserIds) {
            var request = NewRequest();
            var args = new Dictionary<string, object> {
                { "$remove", expectedUserIds.ToList<object>() }
            };
            request.UpdateSysProperty = new UpdateSysPropertyRequest {
                SysAttr = new RoomSystemProperty { 
                    ExpectMembers = Json.Encode(args)
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.UpdateSysProperty.SysAttr);
        }

        internal async Task<int> SetMaster(int newMasterId) {
            var request = NewRequest();
            request.UpdateMasterClient = new UpdateMasterClientRequest { 
                MasterActorId = newMasterId
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateMasterClient, request);
            return res.UpdateMasterClient.MasterActorId;
        }

        internal async Task<int> KickPlayer(int actorId, int code, string reason) {
            var request = NewRequest();
            request.KickMember = new KickMemberRequest { 
                TargetActorId = actorId,
                AppInfo = new AppInfo { 
                    AppCode = code,
                    AppMsg = reason
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.Kick, request);
            return res.KickMember.TargetActorId;
        }

        internal Task SendEvent(byte eventId, PlayObject eventData, SendEventOptions options) {
            var request = NewRequest();
            var direct = new DirectCommand { 
                EventId = eventId
            };
            if (eventData != null) {
                direct.Msg = CodecUtils.EncodePlayObject(eventData);
            }
            direct.ReceiverGroup = (int) options.ReceiverGroup;
            if (options.TargetActorIds != null) {
                direct.ToActorIds.AddRange(options.TargetActorIds);
            }
            Send(CommandType.Direct, OpType.None, new Body { 
                Direct = direct
            });
            return Task.FromResult(true);
        }

        internal async Task<PlayObject> SetRoomCustomProperties(PlayObject properties, PlayObject expectedValues) {
            var request = NewRequest();
            request.UpdateProperty = new UpdatePropertyRequest {
                Attr = CodecUtils.EncodePlayObject(properties)
            };
            if (expectedValues != null) {
                request.UpdateProperty.ExpectAttr = CodecUtils.EncodePlayObject(expectedValues);
            }
            var res = await SendRequest(CommandType.Conv, OpType.Update, request);
            var props = CodecUtils.DecodePlayObject(res.UpdateProperty.Attr);
            return props;
        }

        internal async Task<Tuple<int, PlayObject>> SetPlayerCustomProperties(int playerId, PlayObject properties, PlayObject expectedValues) {
            var request = NewRequest();
            request.UpdateProperty = new UpdatePropertyRequest {
                TargetActorId = playerId,
                Attr = CodecUtils.EncodePlayObject(properties)
            };
            if (expectedValues != null) {
                request.UpdateProperty.ExpectAttr = CodecUtils.EncodePlayObject(expectedValues);
            }
            var res = await SendRequest(CommandType.Conv, OpType.UpdatePlayerProp, request);
            var actorId = res.UpdateProperty.ActorId;
            var props = CodecUtils.DecodePlayObject(res.UpdateProperty.Attr);
            return new Tuple<int, PlayObject>(actorId, props);
        }

        protected override int GetPingDuration() {
            return 7;
        }
    }
}
