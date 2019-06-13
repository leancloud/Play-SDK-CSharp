using System;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;

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
                Open = options.Open,
                Visible = options.Visible,
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
            // TODO attr

            return room;
        }

        internal static Player ConvertToPlayer(RoomMember member) {
            var player = new Player { 
                UserId = member.Pid,
                ActorId = member.ActorId,
                IsActive = !member.Inactive,
                // TODO attr

            };
            return player;
        }
    }
}
