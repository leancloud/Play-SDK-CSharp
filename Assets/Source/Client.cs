using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using LeanCloud.Play.Protocol;

namespace LeanCloud.Play {
    public class Client {
        // 事件
        public event Action<List<LobbyRoom>> OnLobbyRoomListUpdated;
        public event Action<Player> OnPlayerRoomJoined;
        public event Action<Player> OnPlayerRoomLeft;
        public event Action<Player> OnMasterSwitched;
        public event Action<PlayObject> OnRoomCustomPropertiesChanged;
        public event Action<PlayObject> OnRoomSystemPropertiesChanged;
        public event Action<Player, PlayObject> OnPlayerCustomPropertiesChanged;
        public event Action<Player> OnPlayerActivityChanged;
        public event Action<byte, PlayObject, int> OnCustomEvent;
        public event Action<int?, string> OnRoomKicked;
        public event Action OnDisconnected;
        public event Action<int, string> OnError;

        readonly PlayContext context;

        public string AppId {
            get; private set;
        }

        public string AppKey {
            get; private set;
        }

        public string UserId {
            get; private set;
        }

        public bool Ssl {
            get; private set;
        }

        public string GameVersion {
            get; private set;
        }

        PlayRouter playRouter;
        LobbyRouter lobbyRouter;
        LobbyConnection lobbyConn;
        GameConnection gameConn;

        PlayState state;

        public List<LobbyRoom> LobbyRoomList;

        public Room Room {
            get; private set;
        }

        public Player Player {
            get; internal set;
        }

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

            playRouter = new PlayRouter(appId, playServer);
            lobbyRouter = new LobbyRouter(appId, false, null);
            lobbyConn = new LobbyConnection();
            gameConn = new GameConnection();
        }

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
            try {
                state = PlayState.CONNECTING;
                lobbyConn = await ConnectLobby();
                state = PlayState.LOBBY;
                Logger.Debug("connected at: {0}", Thread.CurrentThread.ManagedThreadId);
                lobbyConn.OnMessage += OnLobbyConnMessage;
                lobbyConn.OnClose += OnLobbyConnClose;
                return this;
            } catch (Exception e) {
                state = PlayState.INIT;
                throw e;
            }
        }

        public async Task JoinLobby() {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call JoinLobby() on {0} state", state.ToString()));
            }
            await lobbyConn.JoinLobby();
        }

        public async Task<Room> CreateRoom(string roomName = null, RoomOptions roomOptions = null, List<string> expectedUserIds = null) {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call CreateRoom() on {0} state", state.ToString()));
            }
            try {
                state = PlayState.LOBBY_TO_GAME;
                var lobbyRoom = await lobbyConn.CreateRoom(roomName, roomOptions, expectedUserIds);
                var roomId = lobbyRoom.RoomId;
                var server = lobbyRoom.PrimaryUrl;
                gameConn = await GameConnection.Connect(AppId, server, UserId, GameVersion);
                var room = await gameConn.CreateRoom(roomId, roomOptions, expectedUserIds);
                LobbyToGame(gameConn, room);
                return room;
            } catch (Exception e) {
                state = PlayState.LOBBY;
                throw e;
            }
        }

        public async Task<Room> JoinRoom(string roomName, List<string> expectedUserIds = null) {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call JoinRoom() on {0} state", state.ToString()));
            }
            try {
                state = PlayState.LOBBY_TO_GAME;
                var lobbyRoom = await lobbyConn.JoinRoom(roomName, expectedUserIds);
                var roomId = lobbyRoom.RoomId;
                var server = lobbyRoom.PrimaryUrl;
                gameConn = await GameConnection.Connect(AppId, server, UserId, GameVersion);
                Room = await gameConn.JoinRoom(roomId, expectedUserIds);
                LobbyToGame(gameConn, Room);
                return Room;
            } catch (Exception e) {
                state = PlayState.LOBBY;
                throw e;
            }
        }

        public async Task<Room> RejoinRoom(string roomName) {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call RejoinRoom() on {0} state", state.ToString()));
            }
            try {
                state = PlayState.LOBBY_TO_GAME;
                var lobbyRoom = await lobbyConn.RejoinRoom(roomName);
                var roomId = lobbyRoom.RoomId;
                var server = lobbyRoom.PrimaryUrl;
                gameConn = await GameConnection.Connect(AppId, server, UserId, GameVersion);
                Room = await gameConn.JoinRoom(roomId, null);
                LobbyToGame(gameConn, Room);
                return Room;
            } catch (Exception e) {
                state = PlayState.LOBBY;
                throw e;
            }
        }

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
                var server = lobbyRoom.PrimaryUrl;
                gameConn = await GameConnection.Connect(AppId, server, UserId, GameVersion);
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

        public async Task<Room> JoinRandomRoom(PlayObject matchProperties = null, List<string> expectedUserIds = null) {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call JoinRandomRoom() on {0} state", state.ToString()));
            }
            try {
                state = PlayState.LOBBY_TO_GAME;
                var lobbyRoom = await lobbyConn.JoinRandomRoom(matchProperties, expectedUserIds);
                var roomId = lobbyRoom.RoomId;
                var server = lobbyRoom.PrimaryUrl;
                gameConn = await GameConnection.Connect(AppId, server, UserId, GameVersion);
                Room = await gameConn.JoinRoom(roomId, expectedUserIds);
                LobbyToGame(gameConn, Room);
                return Room;
            } catch (Exception e) {
                state = PlayState.LOBBY;
                throw e;
            }
        }

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

        public async Task<LobbyRoom> MatchRandom(string piggybackUserId, PlayObject matchProperties = null) {
            if (state != PlayState.LOBBY) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call MatchRandom() on {0} state", state.ToString()));
            }
            var lobbyRoom = await lobbyConn.MatchRandom(piggybackUserId, matchProperties);
            return lobbyRoom;
        }

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

        public async Task<bool> SetRoomOpen(bool opened) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetRoomOpened() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.SetRoomOpen(opened);
            Room.MergeSystemProperties(sysProps);
            return Room.Open;
        }

        public async Task<bool> SetRoomVisible(bool visible) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetRoomVisible() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.SetRoomVisible(visible);
            Room.MergeSystemProperties(sysProps);
            return Room.Visible;
        }

        public async Task<int> SetRoomMaxPlayerCount(int count) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetRoomMaxPlayerCount() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.SetRoomMaxPlayerCount(count);
            Room.MergeSystemProperties(sysProps);
            return Room.MaxPlayerCount;
        }

        public async Task<List<string>> SetRoomExpectedUserIds(List<string> expectedUserIds) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetRoomExpectedUserIds() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.SetRoomExpectedUserIds(expectedUserIds);
            Room.MergeSystemProperties(sysProps);
            return Room.ExpectedUserIds;
        }

        public async Task ClearRoomExpectedUserIds() {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call ClearRoomExpectedUserIds() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.ClearRoomExpectedUserIds();
            Room.MergeSystemProperties(sysProps);
        }

        public async Task<List<string>> AddRoomExpectedUserIds(List<string> expectedUserIds) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call AddRoomExpectedUserIds() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.AddRoomExpectedUserIds(expectedUserIds);
            Room.MergeSystemProperties(sysProps);
            return Room.ExpectedUserIds;
        }

        public async Task<List<string>> RemoveRoomExpectedUserIds(List<string> expectedUserIds) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call RemoveRoomExpectedUserIds() on {0} state", state.ToString()));
            }
            var sysProps = await gameConn.RemoveRoomExpectedUserIds(expectedUserIds);
            Room.MergeSystemProperties(sysProps);
            return Room.ExpectedUserIds;
        }

        public async Task<Player> SetMaster(int newMasterId) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call SetMaster() on {0} state", state.ToString()));
            }
            Room.MasterActorId = await gameConn.SetMaster(newMasterId);
            return Room.Master;
        }

        public async Task KickPlayer(int actorId, int code = 0, string reason = null) {
            if (state != PlayState.GAME) {
                throw new PlayException(PlayExceptionCode.StateError,
                    string.Format("You cannot call KickPlayer() on {0} state", state.ToString()));
            }
            var playerId = await gameConn.KickPlayer(actorId, code, reason);
            Room.RemovePlayer(playerId);
        }

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

        public void PauseMessageQueue() { 
            if (state == PlayState.LOBBY) {
                lobbyConn.PauseMessageQueue();
            } else if (state == PlayState.GAME) {
                gameConn.PauseMessageQueue();
            }
        }

        public void ResumeMessageQueue() {
            if (state == PlayState.LOBBY) {
                lobbyConn.ResumeMessageQueue();
            } else if (state == PlayState.GAME) {
                gameConn.ResumeMessageQueue();
            }
        }

        public void Close() {
            if (state == PlayState.LOBBY) {
                lobbyConn.Close();
            } else if (state == PlayState.GAME) {
                gameConn.Close();
            }
            state = PlayState.CLOSE;
        }

        void OnLobbyConnMessage(CommandType cmd, OpType op, Body body) {
            context.Post(() => { 
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
            });
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
            context.Post(() => {
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
                                //HandlePlayerOffline(msg);
                                break;
                            case OpType.MembersOnline:
                                //HandlePlayerOnline(msg);
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
            });
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
            var changedProps = CodecUtils.DecodePlayObject(updatePropertyNotification.Attr);
            // 房间属性变化
            Room.MergeCustomProperties(changedProps);
            OnRoomCustomPropertiesChanged?.Invoke(changedProps);
        }

        void HandlePlayerCustomPropertiesChanged(UpdatePropertyNotification updatePropertyNotification) {
            var changedProps = CodecUtils.DecodePlayObject(updatePropertyNotification.Attr);
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
            var playerId = roomNotification.InitByActor;
            var player = Room.GetPlayer(playerId);
            if (player == null) {
                Logger.Error("No player id: {0} when player is offline");
                return;
            }
            player.IsActive = true;
            OnPlayerActivityChanged?.Invoke(player);
        }

        void HandleSendEvent(DirectCommand directCommand) {
            var eventId = (byte) directCommand.EventId;
            var eventData = CodecUtils.DecodePlayObject(directCommand.Msg);
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
            return playRouter.Fetch().OnSuccess(t => {
                var serverUrl = t.Result;
                Logger.Debug("play server: {0} at {1}", serverUrl, Thread.CurrentThread.ManagedThreadId);
                return lobbyRouter.Fetch(serverUrl);
            }).Unwrap().OnSuccess(t => {
                var lobbyUrl = t.Result;
                Logger.Debug("wss server: {0} at {1}", lobbyUrl, Thread.CurrentThread.ManagedThreadId);
                return LobbyConnection.Connect(AppId, lobbyUrl, UserId, GameVersion);
            }).Unwrap();
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
