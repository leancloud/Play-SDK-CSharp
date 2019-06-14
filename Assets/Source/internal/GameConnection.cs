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

        internal async Task<Dictionary<string, object>> SetRoomOpen(bool open) {
            var msg = Message.NewRequest("conv", "update-system-property");
            msg["sysAttr"] = new Dictionary<string, object> {
                { "open", open }
            };
            var res = await Send(msg);
            return res["sysAttr"] as Dictionary<string, object>;
        }

        internal async Task<Dictionary<string, object>> SetRoomVisible(bool visible) {
            var msg = Message.NewRequest("conv", "update-system-property");
            msg["sysAttr"] = new Dictionary<string, object> {
                { "visible", visible }
            };
            var res = await Send(msg);
            return res["sysAttr"] as Dictionary<string, object>;
        }

        internal async Task<Dictionary<string, object>> SetRoomMaxPlayerCount(int count) {
            var msg = Message.NewRequest("conv", "update-system-property");
            msg["sysAttr"] = new Dictionary<string, object> {
                { "maxMembers", count }
            };
            var res = await Send(msg);
            return res["sysAttr"] as Dictionary<string, object>;
        }

        internal async Task<Dictionary<string, object>> SetRoomExpectedUserIds(List<string> expectedUserIds) {
            var msg = Message.NewRequest("conv", "update-system-property");
            msg["sysAttr"] = new Dictionary<string, object> {
                { "expectMembers", new Dictionary<string, object> {
                    { "$set", expectedUserIds.ToList<object>() }
                } }
            };
            var res = await Send(msg);
            return res["sysAttr"] as Dictionary<string, object>;
        }

        internal async Task<Dictionary<string, object>> ClearRoomExpectedUserIds() {
            var msg = Message.NewRequest("conv", "update-system-property");
            msg["sysAttr"] = new Dictionary<string, object> {
                { "expectMembers", new Dictionary<string, object> {
                    { "$drop", true }
                } }
            };
            var res = await Send(msg);
            return res["sysAttr"] as Dictionary<string, object>;
        }

        internal async Task<Dictionary<string, object>> AddRoomExpectedUserIds(List<string> expectedUserIds) {
            var msg = Message.NewRequest("conv", "update-system-property");
            msg["sysAttr"] = new Dictionary<string, object> {
                { "expectMembers", new Dictionary<string, object> {
                    { "$add", expectedUserIds.ToList<object>() }
                } }
            };
            var res = await Send(msg);
            return res["sysAttr"] as Dictionary<string, object>;
        }

        internal async Task<Dictionary<string, object>> RemoveRoomExpectedUserIds(List<string> expectedUserIds) {
            var msg = Message.NewRequest("conv", "update-system-property");
            msg["sysAttr"] = new Dictionary<string, object> {
                { "expectMembers", new Dictionary<string, object> {
                    { "$remove", expectedUserIds.ToList<object>() }
                } }
            };
            var res = await Send(msg);
            return res["sysAttr"] as Dictionary<string, object>;
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

        internal async Task SendEvent(byte eventId, Dictionary<string, object> eventData, SendEventOptions options) {
            var msg = Message.NewRequest("direct", null);
            msg["eventId"] = eventId;
            msg["msg"] = eventData;
            msg["receiverGroup"] = (int) options.ReceiverGroup;
            if (options.TargetActorIds != null) {
                msg["toActorIds"] = options.TargetActorIds.Cast<object>().ToList();
            }
            await Send(msg);
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

        internal async Task<Dictionary<string, object>> SetPlayerCustomProperties(int playerId, PlayObject properties, PlayObject expectedValues) {
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
            return new Dictionary<string, object> {
                    { "actorId", actorId },
                    { "changedProps", props },
                };
        }

        protected override int GetPingDuration() {
            return 7;
        }
    }
}
