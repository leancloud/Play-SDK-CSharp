using System.Linq;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;
using Newtonsoft.Json;
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
                if (options.CustomRoomProperties != null) {
                    roomOptions.Attr = ByteString.CopyFrom(CodecUtils.SerializePlayObject(options.CustomRoomProperties));
                }
                if (options.CustoRoomPropertyKeysForLobby != null) {
                    roomOptions.LobbyAttrKeys.AddRange(options.CustoRoomPropertyKeysForLobby);
                }
                if (options.PluginName != null) {
                    roomOptions.PluginName = options.PluginName;
                }
            }
            if (expectedUserIds != null) {
                roomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            return roomOptions;
        }

        internal static Room ConvertToRoom(Protocol.RoomOptions options) {
            var room = new Room(null) {
                Name = options.Cid,
                Open = options.Open == null || options.Open.Value,
                Visible = options.Visible == null || options.Visible.Value,
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
            room.CustomProperties = CodecUtils.DeserializePlayObject(options.Attr);
            return room;
        }

        internal static LobbyRoom ConvertToLobbyRoom(Protocol.RoomOptions options) {
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

        internal static Player ConvertToPlayer(RoomMember member) {
            var player = new Player { 
                UserId = member.Pid,
                ActorId = member.ActorId,
                IsActive = !member.Inactive
            };
            if (member.Attr != null) {
                player.CustomProperties = CodecUtils.DeserializePlayObject(member.Attr);
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
            if (!string.IsNullOrEmpty(property.ExpectMembers)) {
                obj["expectedUserIds"] = JsonConvert.DeserializeObject<List<string>>(property.ExpectMembers);
            }
            return obj;
        }
    }
}
