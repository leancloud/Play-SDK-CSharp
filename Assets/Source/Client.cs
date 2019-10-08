using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using LeanCloud.Play.Protocol;

namespace LeanCloud.Play {
    public class Client {
        // 事件
        /// <summary>
        /// 大厅房间列表更新事件
        /// </summary>
        public event Action<List<LobbyRoom>> OnLobbyRoomListUpdated;
        /// <summary>
        /// 有玩家加入房间事件
        /// </summary>
        public event Action<Player> OnPlayerRoomJoined;
        /// <summary>
        /// 有玩家离开房间事件
        /// </summary>
        public event Action<Player> OnPlayerRoomLeft;
        /// <summary>
        /// 房主切换事件
        /// </summary>
        public event Action<Player> OnMasterSwitched;
        /// <summary>
        /// 房间自定义属性更新事件
        /// </summary>
        public event Action<PlayObject> OnRoomCustomPropertiesChanged;
        /// <summary>
        /// 房间系统属性更新事件，目前包括：房间开关，可见性，最大玩家数量，预留玩家 Id 列表
        /// </summary>
        public event Action<PlayObject> OnRoomSystemPropertiesChanged;
        /// <summary>
        /// 玩家自定义属性更新事件
        /// </summary>
        public event Action<Player, PlayObject> OnPlayerCustomPropertiesChanged;
        /// <summary>
        /// 玩家在线 / 离线变化事件
        /// </summary>
        public event Action<Player> OnPlayerActivityChanged;
        /// <summary>
        /// 用户自定义事件
        /// </summary>
        public event Action<byte, PlayObject, int> OnCustomEvent;
        /// <summary>
        /// 被踢出房间事件
        /// </summary>
        public event Action<int?, string> OnRoomKicked;
        /// <summary>
        /// 断线事件
        /// </summary>
        public event Action OnDisconnected;
        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<int, string> OnError;

        readonly PlayContext context;

        LobbyService lobbyService;

        /// <summary>
        /// LeanCloud App Id
        /// </summary>
        /// <value>The app identifier.</value>
        public string AppId {
            get; private set;
        }

        /// <summary>
        /// LeanCloud App Key
        /// </summary>
        /// <value>The app key.</value>
        public string AppKey {
            get; private set;
        }

        /// <summary>
        /// 用户唯一 Id
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId {
            get; private set;
        }

        /// <summary>
        /// 是否启用 SSL
        /// </summary>
        /// <value><c>true</c> if ssl; otherwise, <c>false</c>.</value>
        public bool Ssl {
            get; private set;
        }

        /// <summary>
        /// 客户端版本号，不同的版本号的玩家不会匹配到相同的房间
        /// </summary>
        /// <value>The game version.</value>
        public string GameVersion {
            get; private set;
        }

        AppRouter playRouter;
        GameRouter lobbyRouter;
        LobbyConnection lobbyConn;
        GameConnection gameConn;

        PlayState state;

        /// <summary>
        /// 大厅房间列表
        /// </summary>
        public List<LobbyRoom> LobbyRoomList;

        /// <summary>
        /// 当前房间对象
        /// </summary>
        /// <value>The room.</value>
        public Room Room {
            get; private set;
        }

        /// <summary>
        /// 当前玩家对象
        /// </summary>
        /// <value>The player.</value>
        public Player Player {
            get; internal set;
        }

        /// <summary>
        /// Client 构造方法
        /// </summary>
        /// <param name="appId">LeanCloud App Id</param>
        /// <param name="appKey">LeanCloud App Key</param>
        /// <param name="userId">用户唯一 Id</param>
        /// <param name="ssl">是否启用 SSL</param>
        /// <param name="gameVersion">游戏版本号</param>
        /// <param name="playServer">游戏服务器地址</param>
        public Client(string appId, string appKey, string userId, bool ssl = true, string gameVersion = "0.0.1", string playServer = null) {
            AppId = appId;
            AppKey = appKey;
            UserId = userId;
            Ssl = ssl;
            GameVersion = gameVersion;

            state = PlayState.INIT;
            Logger.Debug("start at {0}", Thread.CurrentThread.ManagedThreadId);

            var playGO = new GameObject("LeanCloud.Play");
            UnityEngine.Object.DontDestroyOnLoad(playGO);
            context = playGO.AddComponent<PlayContext>();

            playRouter = new AppRouter(appId, playServer);
            lobbyRouter = new GameRouter(appId, appKey, userId, false, null);
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns>The connect.</returns>
        public async Task<Client> Connect() {
            if (state == PlayState.CONNECTING) {
                // 
                Logger.Debug("it is connecting...");
                return null;
            }
            if (state != PlayState.INIT && state != PlayState.DISCONNECT) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call Connect() on {0} state", state.ToString()));
            }
            lobbyService = new LobbyService(AppId, AppKey, UserId);
            await lobbyService.Authorize();
            Logger.Debug("connected at: {0}", Thread.CurrentThread.ManagedThreadId);
            return this;
        }

        /// <summary>
        /// 加入大厅，会接收到大厅房间列表更新的事件
        /// </summary>
        /// <returns>The lobby.</returns>
        public async Task JoinLobby() {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call JoinLobby() on {0} state", state.ToString()));
            }
            await lobbyService.Authorize();
            lobbyConn = new LobbyConnection();
            await lobbyConn.Connect(AppId, null, GameVersion, UserId, null);
            await lobbyConn.JoinLobby();
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        /// <returns>The room.</returns>
        /// <param name="roomName">房间唯一 Id</param>
        /// <param name="roomOptions">创建房间选项</param>
        /// <param name="expectedUserIds">期望用户 Id 列表</param>
        public async Task<Room> CreateRoom(string roomName = null, RoomOptions roomOptions = null, List<string> expectedUserIds = null) {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call CreateRoom() on {0} state", state.ToString()));
            }
            try {
                state = PlayState.LOBBY_TO_GAME;
                var lobbyRoom = await lobbyConn.CreateRoom(roomName, roomOptions, expectedUserIds);
                var roomId = lobbyRoom.RoomId;
                var server = lobbyRoom.Url;
                gameConn = new GameConnection();
                await lobbyService.Authorize();
                await gameConn.Connect(AppId, server, GameVersion, UserId, null);
                var room = await gameConn.CreateRoom(roomId, roomOptions, expectedUserIds);
                LobbyToGame(gameConn, room);
                return room;
            } catch (Exception e) {
                state = PlayState.LOBBY;
                throw e;
            }
        }

        /// <summary>
        /// 加入房间
        /// </summary>
        /// <returns>The room.</returns>
        /// <param name="roomName">房间 Id</param>
        /// <param name="expectedUserIds">期望用户 Id 列表</param>
        public async Task<Room> JoinRoom(string roomName, List<string> expectedUserIds = null) {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call JoinRoom() on {0} state", state.ToString()));
            }
            try {
                state = PlayState.LOBBY_TO_GAME;
                var lobbyRoom = await lobbyConn.JoinRoom(roomName, expectedUserIds);
                var roomId = lobbyRoom.RoomId;
                var server = lobbyRoom.Url;
                gameConn = new GameConnection();
                Room = await gameConn.JoinRoom(roomId, expectedUserIds);
                LobbyToGame(gameConn, Room);
                return Room;
            } catch (Exception e) {
                state = PlayState.LOBBY;
                throw e;
            }
        }

        /// <summary>
        /// 返回房间
        /// </summary>
        /// <returns>The room.</returns>
        /// <param name="roomName">房间 Id</param>
        public async Task<Room> RejoinRoom(string roomName) {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call RejoinRoom() on {0} state", state.ToString()));
            }
            try {
                state = PlayState.LOBBY_TO_GAME;
                var lobbyRoom = await lobbyConn.RejoinRoom(roomName);
                var roomId = lobbyRoom.RoomId;
                var server = lobbyRoom.Url;
                gameConn = new GameConnection();
                Room = await gameConn.JoinRoom(roomId, null);
                LobbyToGame(gameConn, Room);
                return Room;
            } catch (Exception e) {
                state = PlayState.LOBBY;
                throw e;
            }
        }

        /// <summary>
        /// 加入或创建房间，如果房间 Id 存在，则加入；否则根据 roomOptions 和 expectedUserIds 创建新的房间
        /// </summary>
        /// <returns>The or create room.</returns>
        /// <param name="roomName">房间 Id</param>
        /// <param name="roomOptions">创建房间选项</param>
        /// <param name="expectedUserIds">期望用户 Id 列表</param>
        public async Task<Room> JoinOrCreateRoom(string roomName, RoomOptions roomOptions = null, List<string> expectedUserIds = null) {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call JoinOrCreateRoom() on {0} state", state.ToString()));
            }
            try {
                state = PlayState.LOBBY_TO_GAME;
                var lobbyRoom = await lobbyConn.JoinOrCreateRoom(roomName, roomOptions, expectedUserIds);
                var create = lobbyRoom.Create;
                var roomId = lobbyRoom.RoomId;
                var server = lobbyRoom.Url;
                gameConn = new GameConnection();
                if (create) {
                    Room = await gameConn.CreateRoom(roomId, roomOptions, expectedUserIds);
                } else {
                    Room = await gameConn.JoinRoom(roomId, expectedUserIds);
                }
                LobbyToGame(gameConn, Room);
                return Room;
            } catch (Exception e) {
                state = PlayState.LOBBY;
                throw e;
            }
        }

        /// <summary>
        /// 随机加入房间
        /// </summary>
        /// <returns>The random room.</returns>
        /// <param name="matchProperties">匹配属性</param>
        /// <param name="expectedUserIds">期望用户 Id 列表</param>
        public async Task<Room> JoinRandomRoom(PlayObject matchProperties = null, List<string> expectedUserIds = null) {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call JoinRandomRoom() on {0} state", state.ToString()));
            }
            try {
                state = PlayState.LOBBY_TO_GAME;
                var lobbyRoom = await lobbyConn.JoinRandomRoom(matchProperties, expectedUserIds);
                var roomId = lobbyRoom.RoomId;
                var server = lobbyRoom.Url;
                gameConn = new GameConnection();
                Room = await gameConn.JoinRoom(roomId, expectedUserIds);
                LobbyToGame(gameConn, Room);
                return Room;
            } catch (Exception e) {
                state = PlayState.LOBBY;
                throw e;
            }
        }

        /// <summary>
        /// 重连并返回上一个加入的房间
        /// </summary>
        /// <returns>The and rejoin.</returns>
        public async Task<Room> ReconnectAndRejoin() {
            if (state != PlayState.DISCONNECT) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call ReconnectAndRejoin() on {0} state", state.ToString()));
            }
            if (Room == null) {
                throw new ArgumentNullException(nameof(Room));
            }
            await Connect();
            Logger.Debug("Connect at {0}", Thread.CurrentThread.ManagedThreadId);
            var room = await RejoinRoom(Room.Name);
            Logger.Debug("Rejoin at {0}", Thread.CurrentThread.ManagedThreadId);
            return room;
        }

        /// <summary>
        /// 匹配房间（不加入）
        /// </summary>
        /// <returns>The random.</returns>
        /// <param name="piggybackUserId">占位用户 Id</param>
        /// <param name="matchProperties">匹配属性</param>
        public async Task<LobbyRoom> MatchRandom(string piggybackUserId, PlayObject matchProperties = null, List<string> expectedUserIds = null) {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call MatchRandom() on {0} state", state.ToString()));
            }
            var lobbyRoom = await lobbyConn.MatchRandom(piggybackUserId, matchProperties, expectedUserIds);
            return lobbyRoom;
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        /// <returns>The room.</returns>
        public async Task LeaveRoom() {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call LeaveRoom() on {0} state", state.ToString()));
            }
            state = PlayState.GAME_TO_LOBBY;
            try {
                await gameConn.LeaveRoom();
            } catch (Exception e) {
                state = PlayState.GAME;
                throw e;
            }
            try {
                lobbyConn = await ConnectLobby();
                GameToLobby(lobbyConn);
            } catch (Exception e) {
                state = PlayState.INIT;
                throw e;
            }
        }

        /// <summary>
        /// 设置房间开启 / 关闭
        /// </summary>
        /// <returns>The room open.</returns>
        /// <param name="open">是否开启</param>
        public async Task<bool> SetRoomOpen(bool open) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetRoomOpened() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.SetRoomOpen(open);
            Room.MergeSystemProperties(sysProps);
            return Room.Open;
        }

        /// <summary>
        /// 设置房间可见性
        /// </summary>
        /// <returns>The room visible.</returns>
        /// <param name="visible">是否可见</param>
        public async Task<bool> SetRoomVisible(bool visible) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetRoomVisible() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.SetRoomVisible(visible);
            Room.MergeSystemProperties(sysProps);
            return Room.Visible;
        }

        /// <summary>
        /// 设置房间最大玩家数量
        /// </summary>
        /// <returns>The room max player count.</returns>
        /// <param name="count">数量</param>
        public async Task<int> SetRoomMaxPlayerCount(int count) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetRoomMaxPlayerCount() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.SetRoomMaxPlayerCount(count);
            Room.MergeSystemProperties(sysProps);
            return Room.MaxPlayerCount;
        }

        /// <summary>
        /// 设置期望用户
        /// </summary>
        /// <returns>The room expected user identifiers.</returns>
        /// <param name="expectedUserIds">期望用户 Id 列表</param>
        public async Task<List<string>> SetRoomExpectedUserIds(List<string> expectedUserIds) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetRoomExpectedUserIds() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.SetRoomExpectedUserIds(expectedUserIds);
            Room.MergeSystemProperties(sysProps);
            return Room.ExpectedUserIds;
        }

        /// <summary>
        /// 清空期望用户 Id 列表
        /// </summary>
        /// <returns>The room expected user identifiers.</returns>
        public async Task ClearRoomExpectedUserIds() {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call ClearRoomExpectedUserIds() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.ClearRoomExpectedUserIds();
            Room.MergeSystemProperties(sysProps);
        }

        /// <summary>
        /// 增加期望用户
        /// </summary>
        /// <returns>The room expected user identifiers.</returns>
        /// <param name="expectedUserIds">增加的期望用户 Id 列表</param>
        public async Task<List<string>> AddRoomExpectedUserIds(List<string> expectedUserIds) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call AddRoomExpectedUserIds() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.AddRoomExpectedUserIds(expectedUserIds);
            Room.MergeSystemProperties(sysProps);
            return Room.ExpectedUserIds;
        }

        /// <summary>
        /// 删除期望用户
        /// </summary>
        /// <returns>The room expected user identifiers.</returns>
        /// <param name="expectedUserIds">删除的期望用户 Id 列表</param>
        public async Task<List<string>> RemoveRoomExpectedUserIds(List<string> expectedUserIds) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call RemoveRoomExpectedUserIds() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.RemoveRoomExpectedUserIds(expectedUserIds);
            Room.MergeSystemProperties(sysProps);
            return Room.ExpectedUserIds;
        }

        /// <summary>
        /// 设置房主
        /// </summary>
        /// <returns>The master.</returns>
        /// <param name="newMasterId">新房主的 Actor Id</param>
        public async Task<Player> SetMaster(int newMasterId) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetMaster() on {0} state", state.ToString()));
            }
            Room.MasterActorId = await gameConn.SetMaster(newMasterId);
            return Room.Master;
        }

        /// <summary>
        /// 将玩家踢出房间
        /// </summary>
        /// <returns>The player.</returns>
        /// <param name="actorId">玩家的 Actor Id</param>
        /// <param name="code">附加码</param>
        /// <param name="reason">附加消息</param>
        public async Task KickPlayer(int actorId, int code = 0, string reason = null) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call KickPlayer() on {0} state", state.ToString()));
            }
            var playerId = await gameConn.KickPlayer(actorId, code, reason);
            Room.RemovePlayer(playerId);
        }

        /// <summary>
        /// 发送自定义事件
        /// </summary>
        /// <returns>The event.</returns>
        /// <param name="eventId">事件 Id</param>
        /// <param name="eventData">事件参数</param>
        /// <param name="options">事件选项</param>
        public Task SendEvent(byte eventId, PlayObject eventData = null, SendEventOptions options = null) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SendEvent() on {0} state", state.ToString()));
            }
            var opts = options;
            if (opts == null) {
                opts = new SendEventOptions {
                    ReceiverGroup = ReceiverGroup.All
                };
            }
            return gameConn.SendEvent(eventId, eventData, opts);
        }

        /// <summary>
        /// 设置房间自定义属性
        /// </summary>
        /// <returns>The room custom properties.</returns>
        /// <param name="properties">自定义属性</param>
        /// <param name="expectedValues">用于 CAS 的期望属性</param>
        public async Task SetRoomCustomProperties(PlayObject properties, PlayObject expectedValues = null) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetRoomCustomProperties() on {0} state", state.ToString()));
            }
            var changedProps = await gameConn.SetRoomCustomProperties(properties, expectedValues);
            if (!changedProps.IsEmpty) {
                Room.MergeCustomProperties(changedProps);
            }
        }

        /// <summary>
        /// 设置玩家自定义属性
        /// </summary>
        /// <returns>The player custom properties.</returns>
        /// <param name="actorId">玩家 Actor Id</param>
        /// <param name="properties">自定义属性</param>
        /// <param name="expectedValues">用于 CAS 的期望属性</param>
        public async Task SetPlayerCustomProperties(int actorId, PlayObject properties, PlayObject expectedValues = null) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetPlayerCustomProperties() on {0} state", state.ToString()));
            }
            var res = await gameConn.SetPlayerCustomProperties(actorId, properties, expectedValues);
            if (!res.Item2.IsEmpty) {
                var playerId = res.Item1;
                var player = Room.GetPlayer(playerId);
                var changedProps = res.Item2;
                player.MergeCustomProperties(changedProps);
            }
        }

        /// <summary>
        /// 暂停消息队列
        /// </summary>
        public void PauseMessageQueue() {
            context.IsMessageQueueRunning = false;
        }

        /// <summary>
        /// 恢复消息队列
        /// </summary>
        public void ResumeMessageQueue() {
            context.IsMessageQueueRunning = true;
        }

        /// <summary>
        /// 关闭服务
        /// </summary>
        public void Close() {
            if (state == PlayState.LOBBY) {
                lobbyConn.Close();
            } else if (state == PlayState.GAME) {
                gameConn.Close();
            }
            state = PlayState.CLOSE;
        }

        void OnLobbyConnMessage(CommandType cmd, OpType op, Body body) {
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
            LobbyRoomList = new List<LobbyRoom>();
            foreach (var roomOpts in body.RoomList.List) {
                var lobbyRoom = Utils.ConvertToLobbyRoom(roomOpts);
                LobbyRoomList.Add(lobbyRoom);
            }
            OnLobbyRoomListUpdated?.Invoke(LobbyRoomList);
        }

        void HandleErrorMsg(Body body) {
            Logger.Error("error msg: {0}", body);
            var errorInfo = body.Error.ErrorInfo;
            OnError?.Invoke(errorInfo.ReasonCode, errorInfo.Detail);
        }

        void HandleUnknownMsg(CommandType cmd, OpType op, Body body) {
            try {
                Logger.Error("unknown msg: {0}/{1} {2}", cmd, op, body);
            } catch (Exception e) {
                Logger.Error(e.Message);
            }
        }

        void OnLobbyConnClose(int code, string reason) {
            context.Post(() => {
                state = PlayState.DISCONNECT;
                OnDisconnected?.Invoke();
            });
        }

        void OnGameConnMessage(CommandType cmd, OpType op, Body body) {
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
                            HandleUnknownMsg(cmd, op, body);
                            break;
                    }
                    break;
                case CommandType.Events:
                    break;
                case CommandType.Direct:
                    HandleSendEvent(body.Direct);
                    break;
                case CommandType.Error:
                    HandleErrorMsg(body);
                    break;
                default:
                    HandleUnknownMsg(cmd, op, body);
                    break;
            }
        }

        void OnGameConnClose(int code, string reason) {
            context.Post(() => {
                state = PlayState.DISCONNECT;
                OnDisconnected?.Invoke();
            });
        }

        void HandlePlayerJoinedRoom(JoinRoomNotification joinRoomNotification) {
            var player = Utils.ConvertToPlayer(joinRoomNotification.Member);
            player.Client = this;
            Room.AddPlayer(player);
            OnPlayerRoomJoined?.Invoke(player);
        }

        void HandlePlayerLeftRoom(LeftRoomNotification leftRoomNotification) {
            var playerId = leftRoomNotification.ActorId;
            var leftPlayer = Room.GetPlayer(playerId);
            Room.RemovePlayer(playerId);
            OnPlayerRoomLeft?.Invoke(leftPlayer);
        }

        void HandleMasterChanged(UpdateMasterClientNotification updateMasterClientNotification) {
            var newMasterId = updateMasterClientNotification.MasterActorId;
            Room.MasterActorId = newMasterId;
            if (newMasterId == 0) {
                OnMasterSwitched?.Invoke(null);
            } else {
                var newMaster = Room.GetPlayer(newMasterId);
                OnMasterSwitched?.Invoke(newMaster);
            }
        }

        void HandleRoomCustomPropertiesChanged(UpdatePropertyNotification updatePropertyNotification) {
            var changedProps = CodecUtils.DeserializePlayObject(updatePropertyNotification.Attr);
            // 房间属性变化
            Room.MergeCustomProperties(changedProps);
            OnRoomCustomPropertiesChanged?.Invoke(changedProps);
        }

        void HandlePlayerCustomPropertiesChanged(UpdatePropertyNotification updatePropertyNotification) {
            var changedProps = CodecUtils.DeserializePlayObject(updatePropertyNotification.Attr);
            // 玩家属性变化
            var player = Room.GetPlayer(updatePropertyNotification.ActorId);
            if (player == null) {
                Logger.Error("No player id: {0} when player properties changed", updatePropertyNotification);
                return;
            }
            player.MergeCustomProperties(changedProps);
            OnPlayerCustomPropertiesChanged?.Invoke(player, changedProps);
        }

        void HandleRoomSystemPropertiesChanged(UpdateSysPropertyNotification updateSysPropertyNotification) {
            var changedProps = Utils.ConvertToPlayObject(updateSysPropertyNotification.SysAttr);
            Room.MergeSystemProperties(changedProps);
            OnRoomSystemPropertiesChanged?.Invoke(changedProps);
        }

        void HandlePlayerOffline(RoomNotification roomNotification) {
            var playerId = roomNotification.InitByActor;
            var player = Room.GetPlayer(playerId);
            if (player == null) {
                Logger.Error("No player id: {0} when player is offline");
                return;
            }
            player.IsActive = false;
            OnPlayerActivityChanged?.Invoke(player);
        }

        void HandlePlayerOnline(RoomNotification roomNotification) {
            var playerId = roomNotification.JoinRoom.Member.ActorId;
            var player = Room.GetPlayer(playerId);
            if (player == null) {
                Logger.Error("No player id: {0} when player is offline");
                return;
            }
            player.IsActive = true;
            OnPlayerActivityChanged?.Invoke(player);
        }

        void HandleSendEvent(DirectCommand directCommand) {
            var eventId = (byte)directCommand.EventId;
            var eventData = CodecUtils.DeserializePlayObject(directCommand.Msg);
            var senderId = directCommand.FromActorId;
            OnCustomEvent?.Invoke(eventId, eventData, senderId);
        }

        void HandleRoomKicked(RoomNotification roomNotification) {
            state = PlayState.GAME_TO_LOBBY;
            // 建立连接
            ConnectLobby().ContinueWith(t => {
                context.Post(() => {
                    if (t.IsFaulted) {
                        state = PlayState.INIT;
                        throw t.Exception.InnerException;
                    }
                    GameToLobby(t.Result);
                    var appInfo = roomNotification.AppInfo;
                    if (appInfo != null) {
                        var code = appInfo.AppCode;
                        var reason = appInfo.AppMsg;
                        OnRoomKicked?.Invoke(code, reason);
                    } else {
                        OnRoomKicked?.Invoke(null, null);
                    }
                });
            });
        }

        Task<LobbyConnection> ConnectLobby() {
            return null;
            //return playRouter.Fetch().OnSuccess(t => {
            //    var serverUrl = t.Result;
            //    Logger.Debug("play server: {0} at {1}", serverUrl, Thread.CurrentThread.ManagedThreadId);
                
            //    return lobbyRouter.Fetch(serverUrl);
            //}).Unwrap().OnSuccess(t => {
            //    var lobbyUrl = t.Result;
            //    Logger.Debug("wss server: {0} at {1}", lobbyUrl, Thread.CurrentThread.ManagedThreadId);
            //    return LobbyConnection.Connect(context, AppId, lobbyUrl, UserId, GameVersion);
            //}).Unwrap();
        }

        void LobbyToGame(GameConnection gc, Room room) {
            state = PlayState.GAME;
            lobbyConn.OnMessage -= OnLobbyConnMessage;
            lobbyConn.OnClose -= OnLobbyConnClose;
            lobbyConn.Close();
            gameConn = gc;
            gameConn.OnMessage += OnGameConnMessage;
            gameConn.OnClose += OnGameConnClose;
            Room = room;
            Room.Client = this;
            foreach (var player in Room.PlayerList) {
                if (player.UserId == UserId) {
                    Player = player;
                }
                player.Client = this;
            }
        }

        void GameToLobby(LobbyConnection lc) {
            state = PlayState.LOBBY;
            gameConn.OnMessage -= OnGameConnMessage;
            gameConn.OnClose -= OnGameConnClose;
            gameConn.Close();
            Logger.Debug("connected at: {0}", Thread.CurrentThread.ManagedThreadId);
            lobbyConn = lc;
            lobbyConn.OnMessage += OnLobbyConnMessage;
            lobbyConn.OnClose += OnLobbyConnClose;
        }

        // 调试时模拟断线
        public void _Disconnect() {
            if (state == PlayState.LOBBY) {
                lobbyConn.Disconnect();
            } else if (state == PlayState.GAME) {
                gameConn.Disconnect();
            }
        }
    }
}
