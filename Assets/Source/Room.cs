using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Play.Protocol;

namespace LeanCloud.Play {
    /// <summary>
    /// 房间类
    /// </summary>
	public class Room {
        enum State {
            Init,
            Joining,
            Game,
            Leaving,
            Disconnected,
            Closed
        }

        internal Client Client {
            get; set;
        }

        internal Dictionary<int, Player> playerDict;

        GameConnection gameConn;

        State state;

        /// <summary>
        /// 房间名称
        /// </summary>
        /// <value>The name.</value>
		public string Name {
            get; internal set;
        }

        /// <summary>
        /// 房间是否开启
        /// </summary>
        /// <value><c>true</c> if opened; otherwise, <c>false</c>.</value>
		public bool Open {
            get; internal set;
		}

        /// <summary>
        /// 房间是否可见
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
		public bool Visible {
            get; internal set;
		}

        /// <summary>
        /// 房间允许的最大玩家数量
        /// </summary>
        /// <value>The max player count.</value>
		public int MaxPlayerCount {
            get; internal set;
		}

        /// <summary>
        /// 房间主机玩家 ID
        /// </summary>
        /// <value>The master actor identifier.</value>
		public int MasterActorId {
            get; internal set;
		}

        /// <summary>
        /// 获取房主
        /// </summary>
        /// <value>The master.</value>
        public Player Master {
            get {
                if (MasterActorId == 0) {
                    return null;
                }
                return GetPlayer(MasterActorId);
            }
        }

        /// <summary>
        /// 邀请的好友 ID 列表
        /// </summary>
        /// <value>The expected user identifiers.</value>
        public List<string> ExpectedUserIds {
            get; internal set;
		}

        /// <summary>
        /// 获取自定义属性
        /// </summary>
        /// <value>The custom properties.</value>
        public PlayObject CustomProperties {
            get; internal set;
        }

        /// <summary>
        /// 获取房间内的玩家列表
        /// </summary>
        /// <value>The player list.</value>
        public List<Player> PlayerList {
            get {
                lock (playerDict) {
                    return playerDict.Values.ToList();
                }
            }
        }

        public Player Player {
            get; private set;
        }

        internal Room(Client client) {
            Client = client;
        }

        internal async Task Create(string roomName, RoomOptions roomOptions, List<string> expectedUserIds) {
            state = State.Joining;
            try {
                LobbyRoomResult lobbyRoom = await Client.lobbyService.CreateRoom(roomName);
                gameConn = new GameConnection();
                LobbyInfo lobbyInfo = await Client.lobbyService.Authorize();
                await gameConn.Connect(Client.AppId, lobbyRoom.Url, Client.GameVersion, Client.UserId, lobbyInfo.SessionToken);
                Room room = await gameConn.CreateRoom(lobbyRoom.RoomId, roomOptions, expectedUserIds);
                Init(room);
                state = State.Game;
            } catch (Exception e) {
                Logger.Error(e.Message);
                state = State.Closed;
                throw e;
            }
        }

        internal async Task Join(string roomName, List<string> expectedUserIds) {
            state = State.Joining;
            try {
                LobbyRoomResult lobbyRoom = await Client.lobbyService.JoinRoom(roomName, expectedUserIds, false, false);
                gameConn = new GameConnection();
                LobbyInfo lobbyInfo = await Client.lobbyService.Authorize();
                await gameConn.Connect(Client.AppId, lobbyRoom.Url, Client.GameVersion, Client.UserId, lobbyInfo.SessionToken);
                Room room = await gameConn.JoinRoom(lobbyRoom.RoomId, expectedUserIds);
                Init(room);
                state = State.Game;
            } catch (Exception e) {
                Logger.Error(e.Message);
                state = State.Closed;
                throw e;
            }
        }

        internal async Task Rejoin(string roomName) {
            state = State.Joining;
            try {
                LobbyInfo lobbyInfo = await Client.lobbyService.Authorize();
                LobbyRoomResult lobbyRoom = await Client.lobbyService.JoinRoom(roomName, null, true, false);
                gameConn = new GameConnection();
                await gameConn.Connect(Client.AppId, lobbyRoom.Url, Client.GameVersion, Client.UserId, lobbyInfo.SessionToken);
                Room room = await gameConn.JoinRoom(lobbyRoom.RoomId, null);
                Init(room);
                state = State.Game;
            } catch (Exception e) {
                state = State.Closed;
                throw e;
            }
        }

        internal async Task JoinOrCreate(string roomName, RoomOptions roomOptions, List<string> expectedUserIds) {
            state = State.Joining;
            try {
                LobbyInfo lobbyInfo = await Client.lobbyService.Authorize();
                LobbyRoomResult lobbyRoom = await Client.lobbyService.JoinRoom(roomName, null, false, true);
                gameConn = new GameConnection();
                await gameConn.Connect(Client.AppId, lobbyRoom.Url, Client.GameVersion, Client.UserId, lobbyInfo.SessionToken);
                Room room;
                if (lobbyRoom.Create) {
                    room = await gameConn.CreateRoom(lobbyRoom.RoomId, roomOptions, expectedUserIds);
                } else {
                    room = await gameConn.JoinRoom(lobbyRoom.RoomId, null);
                }
                Init(room);
                state = State.Game;
            } catch (Exception e) {
                state = State.Closed;
                throw e;
            }
        }

        internal async Task JoinRandom(PlayObject matchProperties, List<string> expectedUserIds) {
            state = State.Joining;
            try {
                LobbyInfo lobbyInfo = await Client.lobbyService.Authorize();
                LobbyRoomResult lobbyRoom = await Client.lobbyService.JoinRandomRoom(matchProperties, expectedUserIds);
                gameConn = new GameConnection();
                await gameConn.Connect(Client.AppId, lobbyRoom.Url, Client.GameVersion, Client.UserId, lobbyInfo.SessionToken);
                Room room = await gameConn.JoinRoom(lobbyRoom.RoomId, expectedUserIds);
                Init(room);
                state = State.Game;
            } catch (Exception e) {
                state = State.Closed;
                throw e;
            }
        }

        internal async Task Leave() {
            Client.Room = null;
            await gameConn.LeaveRoom();
            await Close();
        }

        /// <summary>
        /// 设置房间的自定义属性
        /// </summary>
        /// <param name="properties">自定义属性</param>
        /// <param name="expectedValues">期望属性，用于 CAS 检测</param>
        public async Task SetCustomProperties(PlayObject properties, PlayObject expectedValues = null) {
            if (state != State.Game) {
                throw new PlayException(PlayExceptionCode.StateError, $"Error state: {state}");
            }
            var changedProps = await gameConn.SetRoomCustomProperties(properties, expectedValues);
            if (!changedProps.IsEmpty) {
                MergeCustomProperties(changedProps);
            }
        }

        public async Task SetPlayerCustomProperties(int actorId, PlayObject properties, PlayObject expectedValues) {
            if (state != State.Game) {
                throw new PlayException(PlayExceptionCode.StateError, $"Error state: {state}");
            }
            var res = await gameConn.SetPlayerCustomProperties(actorId, properties, expectedValues);
            if (!res.Item2.IsEmpty) {
                var playerId = res.Item1;
                var player = GetPlayer(playerId);
                var changedProps = res.Item2;
                player.MergeCustomProperties(changedProps);
            }
        }

        /// <summary>
        /// 根据 actorId 获取 Player 对象
        /// </summary>
        /// <returns>玩家对象</returns>
        /// <param name="actorId">玩家在房间中的 Id</param>
        public Player GetPlayer(int actorId) {
            lock (playerDict) {
                if (!playerDict.TryGetValue(actorId, out Player player)) {
                    throw new Exception(string.Format("no player: {0}", actorId));
                }
                return player;
            }
        }

        /// <summary>
        /// 设置开启 / 关闭
        /// </summary>
        /// <returns>The open.</returns>
        /// <param name="open">是否开启</param>
        public async Task<bool> SetOpen(bool open) {
            var sysProps = await gameConn.SetRoomOpen(open);
            MergeSystemProperties(sysProps);
            return Open;
        }

        /// <summary>
        /// 设置可见性
        /// </summary>
        /// <returns>The visible.</returns>
        /// <param name="visible">是否可见</param>
        public async Task<bool> SetVisible(bool visible) {
            var sysProps = await gameConn.SetRoomVisible(visible);
            MergeSystemProperties(sysProps);
            return Visible;
        }

        /// <summary>
        /// 设置最大玩家数量
        /// </summary>
        /// <returns>The max player count.</returns>
        /// <param name="count">数量</param>
        public async Task<int> SetMaxPlayerCount(int count) {
            var sysProps = await gameConn.SetRoomMaxPlayerCount(count);
            MergeSystemProperties(sysProps);
            return MaxPlayerCount;
        }

        /// <summary>
        /// 设置期望玩家
        /// </summary>
        /// <returns>The expected user identifiers.</returns>
        /// <param name="expectedUserIds">玩家 Id 列表</param>
        public async Task<List<string>> SetExpectedUserIds(List<string> expectedUserIds) {
            var sysProps = await gameConn.SetRoomExpectedUserIds(expectedUserIds);
            MergeSystemProperties(sysProps);
            return ExpectedUserIds;
        }

        /// <summary>
        /// 清空期望玩家
        /// </summary>
        /// <returns>The expected user identifiers.</returns>
        public async Task ClearExpectedUserIds() {
            var sysProps = await gameConn.ClearRoomExpectedUserIds();
            MergeSystemProperties(sysProps);
        }

        /// <summary>
        /// 增加期望玩家
        /// </summary>
        /// <returns>The expected user identifiers.</returns>
        /// <param name="expectedUserIds">玩家 Id 列表</param>
        public async Task<List<string>> AddExpectedUserIds(List<string> expectedUserIds) {
            var sysProps = await gameConn.AddRoomExpectedUserIds(expectedUserIds);
            MergeSystemProperties(sysProps);
            return ExpectedUserIds;
        }

        /// <summary>
        /// 删除期望玩家
        /// </summary>
        /// <returns>The expected user identifiers.</returns>
        /// <param name="expectedUserIds">玩家 Id 列表</param>
        public async Task<List<string>> RemoveExpectedUserIds(List<string> expectedUserIds) {
            var sysProps = await gameConn.RemoveRoomExpectedUserIds(expectedUserIds);
            MergeSystemProperties(sysProps);
            return ExpectedUserIds;
        }

        public async Task<Player> SetMaster(int newMasterId) {
            MasterActorId = await gameConn.SetMaster(newMasterId);
            return Master;
        }

        public async Task KickPlayer(int actorId, int code, string reason) {
            var playerId = await gameConn.KickPlayer(actorId, code, reason);
            RemovePlayer(playerId);
        }

        public Task SendEvent(byte eventId, PlayObject eventData, SendEventOptions options) {
            var opts = options;
            if (opts == null) {
                opts = new SendEventOptions {
                    ReceiverGroup = ReceiverGroup.All
                };
            }
           return gameConn.SendEvent(eventId, eventData, opts);
        }

        public async Task Close() {
            try {
                await gameConn.Close();
            } catch (Exception e) {
                Logger.Error(e.Message);
            } finally {
                Logger.Debug("Room closed.");
            }
        }

        public void _Disconnect() {
            gameConn._Disconnect();
        }

        internal void AddPlayer(Player player) {
			if (player == null) {
				throw new Exception(string.Format("player is null"));
			}
            lock (playerDict) {
                playerDict.Add(player.ActorId, player);
            }
		}

        internal void RemovePlayer(int actorId) {
            lock (playerDict) {
                if (!playerDict.Remove(actorId)) {
                    throw new Exception(string.Format("no player: {0}", actorId));
                }
            }
		}

        internal void MergeProperties(Dictionary<string, object> changedProps) {
            if (changedProps == null)
                return;

            lock (CustomProperties) {
                foreach (KeyValuePair<string, object> entry in changedProps) {
                    CustomProperties[entry.Key] = entry.Value;
                }
            }
        }

        internal void MergeCustomProperties(PlayObject changedProps) { 
            if (changedProps == null) {
                return;
            }
            lock (CustomProperties) { 
                foreach (var entry in changedProps) {
                    CustomProperties[entry.Key] = entry.Value;
                }
            }
        }

        internal void MergeSystemProperties(PlayObject changedProps) { 
            if (changedProps == null) {
                return;
            }
            if (changedProps.TryGetBool("open", out var open)) {
                Open = open;
            }
            if (changedProps.TryGetBool("visible", out var visible)) {
                Visible = visible;
            }
            if (changedProps.TryGetInt("maxPlayerCount", out var maxPlayerCount)) {
                MaxPlayerCount = maxPlayerCount;
            }
            if (changedProps.TryGetValue("expectedUserIds", out object expectedUserIds)) {
                ExpectedUserIds = expectedUserIds as List<string>;
            }
        }

        void Init(Room room) {
            if (room == null) {
                return;
            }
            Name = room.Name;
            Open = room.Open;
            Visible = room.Visible;
            MaxPlayerCount = room.MaxPlayerCount;
            MasterActorId = room.MasterActorId;
            ExpectedUserIds = room.ExpectedUserIds;
            playerDict = room.playerDict;
            foreach (Player player in playerDict.Values) {
                if (player.UserId == Client.UserId) {
                    Player = player;
                    break;
                }
            }
            CustomProperties = room.CustomProperties;
            gameConn.OnMessage += (cmd, op, body) => {
                switch (cmd) {
                    case CommandType.Conv:
                        switch (op) {
                            case OpType.MembersJoined:
                                HandlePlayerJoinedRoom(body.RoomNotification.JoinRoom);
                                break;
                            case OpType.MembersLeft:
                                HandlePlayerLeftRoom(body.RoomNotification.LeftRoom);
                                break;
                            case OpType.MasterClientChanged:
                                HandleMasterChanged(body.RoomNotification.UpdateMasterClient);
                                break;
                            case OpType.SystemPropertyUpdatedNotify:
                                HandleRoomSystemPropertiesChanged(body.RoomNotification.UpdateSysProperty);
                                break;
                            case OpType.UpdatedNotify:
                                HandleRoomCustomPropertiesChanged(body.RoomNotification.UpdateProperty);
                                break;
                            case OpType.PlayerProps:
                                HandlePlayerCustomPropertiesChanged(body.RoomNotification.UpdateProperty);
                                break;
                            case OpType.MembersOffline:
                                HandlePlayerOffline(body.RoomNotification);
                                break;
                            case OpType.MembersOnline:
                                HandlePlayerOnline(body.RoomNotification);
                                break;
                            case OpType.KickedNotice:
                                HandleRoomKicked(body.RoomNotification);
                                break;
                            default:
                                //HandleUnknownMsg(cmd, op, body);
                                break;
                        }
                        break;
                    case CommandType.Events:
                        break;
                    case CommandType.Direct:
                        HandleSendEvent(body.Direct);
                        break;
                    case CommandType.Error:
                        //HandleErrorMsg(body);
                        break;
                    default:
                        //HandleUnknownMsg(cmd, op, body);
                        break;
                }
            };
            gameConn.OnClose += (code, message) => {
                Client.OnDisconnected?.Invoke();
            };
        }

        void HandlePlayerJoinedRoom(JoinRoomNotification joinRoomNotification) {
            var player = Utils.ConvertToPlayer(joinRoomNotification.Member);
            player.Client = Client;
            AddPlayer(player);
            Client.OnPlayerRoomJoined?.Invoke(player);
        }

        void HandlePlayerLeftRoom(LeftRoomNotification leftRoomNotification) {
            var playerId = leftRoomNotification.ActorId;
            var leftPlayer = GetPlayer(playerId);
            RemovePlayer(playerId);
            Client.OnPlayerRoomLeft?.Invoke(leftPlayer);
        }

        void HandleMasterChanged(UpdateMasterClientNotification updateMasterClientNotification) {
            var newMasterId = updateMasterClientNotification.MasterActorId;
            MasterActorId = newMasterId;
            if (newMasterId == 0) {
                Client.OnMasterSwitched?.Invoke(null);
            } else {
                var newMaster = GetPlayer(newMasterId);
                Client.OnMasterSwitched?.Invoke(newMaster);
            }
        }

        void HandleRoomCustomPropertiesChanged(UpdatePropertyNotification updatePropertyNotification) {
            var changedProps = CodecUtils.DeserializePlayObject(updatePropertyNotification.Attr);
            // 房间属性变化
            MergeCustomProperties(changedProps);
            Client.OnRoomCustomPropertiesChanged?.Invoke(changedProps);
        }

        void HandlePlayerCustomPropertiesChanged(UpdatePropertyNotification updatePropertyNotification) {
            var changedProps = CodecUtils.DeserializePlayObject(updatePropertyNotification.Attr);
            // 玩家属性变化
            var player = GetPlayer(updatePropertyNotification.ActorId);
            if (player == null) {
                Logger.Error("No player id: {0} when player properties changed", updatePropertyNotification);
                return;
            }
            player.MergeCustomProperties(changedProps);
            Client.OnPlayerCustomPropertiesChanged?.Invoke(player, changedProps);
        }

        void HandleRoomSystemPropertiesChanged(UpdateSysPropertyNotification updateSysPropertyNotification) {
            var changedProps = Utils.ConvertToPlayObject(updateSysPropertyNotification.SysAttr);
            MergeSystemProperties(changedProps);
            Client.OnRoomSystemPropertiesChanged?.Invoke(changedProps);
        }

        void HandlePlayerOffline(RoomNotification roomNotification) {
            var playerId = roomNotification.InitByActor;
            var player = GetPlayer(playerId);
            if (player == null) {
                Logger.Error("No player id: {0} when player is offline");
                return;
            }
            player.IsActive = false;
            Client.OnPlayerActivityChanged?.Invoke(player);
        }

        void HandlePlayerOnline(RoomNotification roomNotification) {
            var playerId = roomNotification.JoinRoom.Member.ActorId;
            var player = GetPlayer(playerId);
            if (player == null) {
                Logger.Error("No player id: {0} when player is offline");
                return;
            }
            player.IsActive = true;
            Client.OnPlayerActivityChanged?.Invoke(player);
        }

        void HandleSendEvent(DirectCommand directCommand) {
            var eventId = (byte)directCommand.EventId;
            var eventData = CodecUtils.DeserializePlayObject(directCommand.Msg);
            var senderId = directCommand.FromActorId;
            Client.OnCustomEvent?.Invoke(eventId, eventData, senderId);
        }

        void HandleRoomKicked(RoomNotification roomNotification) {
            var appInfo = roomNotification.AppInfo;
            if (appInfo != null) {
                var code = appInfo.AppCode;
                var reason = appInfo.AppMsg;
                Client.OnRoomKicked?.Invoke(code, reason);
            } else {
                Client.OnRoomKicked?.Invoke(null, null);
            }
        }
    }
}
