using System;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;
using Google.Protobuf;

namespace LeanCloud.Play {
    internal static class Utils {
        internal static Protocol.RoomOptions ConvertToRoomOptions(string roomName, RoomOptions options, List<string> expectedUserIds) {
            var roomOptions = new Protocol.RoomOptions();
            if (!string.IsNullOrEmpty(roomName)) {
                roomOptions.Cid = roomName;
            }
            if (options != null) {
                roomOptions.Visible = options.Visible;
                roomOptions.Open = options.Open;
                roomOptions.EmptyRoomTtl = options.EmptyRoomTtl;
                roomOptions.PlayerTtl = options.PlayerTtl;
                roomOptions.MaxMembers = options.MaxPlayerCount;
                roomOptions.Flag = options.Flag;
                roomOptions.Attr = CodecUtils.EncodePlayObject(options.CustomRoomProperties);
                roomOptions.LobbyAttrKeys.AddRange(options.CustoRoomPropertyKeysForLobby);
            }
            if (expectedUserIds != null) {
                roomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            return roomOptions;
        }

        internal static Room ConvertToRoom(Protocol.RoomOptions options) {
            var room = new Room {
                Name = options.Cid,
                Open = options.Open.Value,
                Visible = options.Visible.Value,
                MaxPlayerCount = options.MaxMembers,
                MasterActorId = options.MasterActorId
            };
            room.ExpectedUserIds = new List<string>();
            if (options.ExpectMembers != null) {
                room.ExpectedUserIds.AddRange(options.ExpectMembers);
            }
            room.playerDict = new Dictionary<int, Player>();
            foreach (RoomMember member in options.Members) {
                var player = ConvertToPlayer(member);
                room.playerDict.Add(player.ActorId, player);
            }
            // attr
            room.CustomProperties = CodecUtils.DecodePlayObject(options.Attr);
            return room;
        }

        internal static LobbyRoom ConvertToLobbyRoom(Protocol.RoomOptions options) {
            var lobbyRoom = new LobbyRoom {
                RoomName = options.Cid,
                Open = options.Open.Value,
                Visible = options.Visible.Value,
                MaxPlayerCount = options.MaxMembers,
                PlayerCount = options.MemberCount,
                EmptyRoomTtl = options.EmptyRoomTtl,
                PlayerTtl = options.PlayerTtl
            };
            if (options.ExpectMembers != null) {
                lobbyRoom.ExpectedUserIds.AddRange(options.ExpectMembers);
            }
            if (options.Attr != null) {
                lobbyRoom.CustomRoomProperties = CodecUtils.DecodePlayObject(options.Attr);
            }
            return lobbyRoom;
        }

        internal static Player ConvertToPlayer(RoomMember member) {
            var player = new Player { 
                UserId = member.Pid,
                ActorId = member.ActorId,
                IsActive = !member.Inactive
            };
            if (member.Attr != null) {
                player.CustomProperties = CodecUtils.DecodePlayObject(member.Attr);
            }
            return player;
        }

        internal static PlayObject ConvertToPlayObject(RoomSystemProperty property) { 
            if (property == null) {
                return null;
            }
            var obj = new PlayObject();
            if (property.Open != null) {
                obj["open"] = property.Open;
            }
            if (property.Visible != null) {
                obj["visible"] = property.Visible;
            }
            if (property.MaxMembers > 0) {
                obj["maxPlayerCount"] = property.MaxMembers;
            }
            if (property.ExpectMembers != null) {
                obj["expectedUserIds"] = Json.Parse(property.ExpectMembers) as List<string>;
            }
            return obj;
        }
    }
}
