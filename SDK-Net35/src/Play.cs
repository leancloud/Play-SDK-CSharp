using System;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

using WebSocketSharp;

namespace LeanCloud.Play {
    /// <summary>
    /// Play 客户端类
    /// </summary>
	public class Play {
		private enum SslProtocolsHack {
			Tls = 192,
			Tls11 = 768,
			Tls12 = 3072
		}

		private const string MsgTypeKey = "MsgTypeKey";
		private const int LobbyMsgType = 1;
		private const int GameMsgType = 2;
        private const int EventMsgType = 3;

        private const int LobbyKeepAliveDuration = 120000;
        private const int GameKeepAliveDuration = 10000;
        private const int MaxNoPongTimes = 3;

		private string appId = null;
		private string appKey = null;
		private Region region = Region.EastChina;
        private string primaryServer = null;
        private string secondaryServer = null;
        private string masterServer = null;
        private int connectFailedCount = 0;
        private long nextConnectTimestamp = 0;
        private System.Timers.Timer connectTimer = null;
        private System.Timers.Timer ping = null;
        private System.Timers.Timer pong = null;
        private long serverValidTimestamp = 0;

		private string gameVersion = null;

		private WebSocket webSocket = null;

        private Queue<Dictionary<string, object>> waitingMessageQueue = null;
        private Queue<Dictionary<string, object>> handingMessageQueue = null;

		private Dictionary<Event, List<Action<Dictionary<string, object>>>> eventListeners = null;

		private int msgId = 0;

        /// <summary>
        /// 玩家 ID
        /// </summary>
        /// <value>The user identifier.</value>
		public string UserId {
			get; set;
		}

        /// <summary>
        /// 是否自动加入大厅
        /// </summary>
        /// <value><c>true</c> if auto join lobby; otherwise, <c>false</c>.</value>
        public bool AutoJoinLobby {
            get; set;
        }

        internal string SessionToken {
            get; set;
        }

        internal bool InLobby {
            get; set;
        }

        internal bool GameToLobby {
            get; set;
        }

        internal Dictionary<string, object> CachedRoomMsg {
            get; set;
        }

        internal string GameServer {
            get; set;
        }

        /// <summary>
        /// 获取房间列表
        /// </summary>
        /// <value>The lobby room list.</value>
        public List<LobbyRoom> LobbyRoomList
        {
            get; internal set;
        }

        /// <summary>
        /// 当前房间
        /// </summary>
        /// <value>The room.</value>
        public Room Room {
            get; internal set;
        }

        /// <summary>
        /// 当前玩家
        /// </summary>
        /// <value>The player.</value>
        public Player Player {
            get; internal set;
        }

        internal PlayState PlayState {
            get; set;
        }

		public Play() {
			this.eventListeners = new Dictionary<Event, List<Action<Dictionary<string, object>>>>();
            this.waitingMessageQueue = new Queue<Dictionary<string, object>>();
            this.handingMessageQueue = new Queue<Dictionary<string, object>>();
            this.AutoJoinLobby = false;
            this.CachedRoomMsg = null;
            this.PlayState = PlayState.CLOSED;
		}

        /// <summary>
        /// 初始化客户端
        /// </summary>
        /// <param name="appId">APP ID</param>
        /// <param name="appKey">APP KEY</param>
        /// <param name="region">节点地区</param>
		public void Init(string appId, string appKey, Region region) {
			this.appId = appId;
			this.appKey = appKey;
			this.region = region;
		}

        /// <summary>
        /// 建立连接
        /// </summary>
        /// <param name="gameVersion">游戏版本号，不同的游戏版本号将路由到不同的服务端，默认值为 0.0.1</param>
		public void Connect(string gameVersion = "0.0.1")
        {
            if (this.UserId.IsNullOrEmpty())
            {
                throw new Exception("UserId is null");
            }
            if (this.PlayState != PlayState.CLOSED) {
                throw new Exception(string.Format("play state error: {0}", this.PlayState));
            }
            if (this.connectTimer != null) {
                Logger.Debug("Wating for connect");
                return;
            }

            long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            if (now < this.nextConnectTimestamp) {
                long waitTime = this.nextConnectTimestamp - now;
                this.connectTimer = new System.Timers.Timer(waitTime);
                this.connectTimer.Elapsed += (sender, args) => {
                    this._Connect(gameVersion);
                    this.connectTimer.Stop();
                    this.connectTimer = null;
                };
                this.connectTimer.Start();
            } else {
                this._Connect(gameVersion);
            }
		}

        void _Connect(string gameVer) { 
            if (string.IsNullOrEmpty(gameVer)) {
                throw new ArgumentException(string.Format("{0} is not a string", gameVer));
            }
            this.gameVersion = gameVer;
            var masterURL = Config.EastCNServerURL;
            switch (this.region)
            {
                case Region.EastChina:
                    masterURL = Config.EastCNServerURL;
                    break;
                case Region.NorthChina:
                    masterURL = Config.NorthCNServerURL;
                    break;
                case Region.NorthAmerica:
                    masterURL = Config.USServerURL;
                    break;
                default:
                    break;
            }
            this.PlayState = PlayState.CONNECTING;
            new Thread(() =>
            {
                var client = new WebClient();
                client.QueryString.Add("appId", this.appId);
                client.QueryString.Add("sdkVersion", Config.PlayVersion);
                try {
                    var content = client.DownloadString(masterURL);
                    Dictionary<string, object> response = Json.Parse(content) as Dictionary<string, object>;
                    this.connectFailedCount = 0;
                    this.nextConnectTimestamp = 0;
                    if (this.connectTimer != null) {
                        this.connectTimer.Stop();
                        this.connectTimer = null;
                    }
                    this.primaryServer = response["server"] as string;
                    this.secondaryServer = response["secondary"] as string;
                    this.masterServer = this.primaryServer;
                    long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                    long ttl = (long)response["ttl"];
                    this.serverValidTimestamp = now + ttl * 1000;
                    this.ConnectToMaster();
                } catch (WebException) {
                    this.connectFailedCount++;
                    long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                    this.nextConnectTimestamp = now + (int)Math.Pow(2, this.connectFailedCount) * 1000;
                    var error = new Dictionary<string, object>();
                    error.Add("code", -1);
                    error.Add("detail", "Game router connect failed");
                    this.Emit(Event.CONNECT_FAILED, error);
                }
            }).Start();
        }

        /// <summary>
        /// 重新连接
        /// </summary>
        public void Reconnect() {
            long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            if (now > this.serverValidTimestamp) {
                this.Connect(this.gameVersion);
            } else {
                // 直接连接 Master
                this.ConnectToMaster();
            }
        }

        /// <summary>
        /// 重新连接并自动加入房间
        /// </summary>
        public void ReconnectAndRejoin() {
            // 判断 cid 是否为空
            string cid = this.CachedRoomMsg["cid"] as string;
            if (string.IsNullOrEmpty(cid)) {
                throw new ArgumentException("cid id null");
            }
            this.CachedRoomMsg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "add" },
                { "i", this.GetMsgId() },
                { "cid", cid },
                { "rejoin", true }
            };
            this.ConnectToGame();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect() {
            if (this.PlayState != PlayState.LOBBY_OPEN && this.PlayState != PlayState.GAME_OPEN) {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            this.PlayState = PlayState.CLOSING;
            this.StopPing();
            if (this.webSocket != null) {
                this.webSocket.Close();
                this.webSocket = null;
            }
        }

        /// <summary>
        /// 加入大厅
        /// </summary>
		public void JoinLobby() {
            if (this.PlayState != PlayState.LOBBY_OPEN) {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            Dictionary<string, object> msg = new Dictionary<string, object>() {
                { "cmd", "lobby" },
                { "op", "add" },
                { "i", this.GetMsgId() },
            };
            this.SendLobbyMessage(msg);
		}

        /// <summary>
        /// 离开大厅
        /// </summary>
        public void LeaveLobby() {
            if (this.PlayState != PlayState.LOBBY_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            Dictionary<string, object> msg = new Dictionary<string, object>() { 
                { "cmd", "lobby" },
                { "op", "remove" },
                { "i", this.GetMsgId() },
            };
            this.SendLobbyMessage(msg);
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        /// <param name="roomName">房间名称，在整个游戏中唯一，默认值为 null，则由服务端分配一个唯一 Id</param>
        /// <param name="roomOptions">创建房间选项，默认值为 null</param>
        /// <param name="expectedUserIds">邀请好友 ID 数组，默认值为 null</param>
        public void CreateRoom(string roomName = null, RoomOptions roomOptions = null, List<string> expectedUserIds = null) {
            if (this.PlayState != PlayState.LOBBY_OPEN) {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            this.CachedRoomMsg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "start" },
                { "i", this.GetMsgId() },
            };
            if (!roomName.IsNullOrEmpty()) {
                this.CachedRoomMsg.Add("cid", roomName);
            }
            // 房间参数
            if (roomOptions != null) {
                var roomOptionsDict = roomOptions.ToMsgObj();
                foreach (var entry in roomOptionsDict) {
                    this.CachedRoomMsg.Add(entry.Key, entry.Value);
                }
            }
            if (expectedUserIds != null) {
                List<object> expecteds = expectedUserIds.Cast<object>().ToList();
                this.CachedRoomMsg.Add("expectMembers", expecteds);
            }
            this.SendLobbyMessage(this.CachedRoomMsg);
        }

        /// <summary>
        /// 加入房间
        /// </summary>
        /// <param name="roomName">房间名称</param>
        /// <param name="expectedUserIds">邀请好友 ID 数组，默认值为 null</param>
        public void JoinRoom(string roomName, List<string> expectedUserIds = null) {
            if (string.IsNullOrEmpty(roomName)) {
                throw new ArgumentException("roomName is null");
            }
            if (this.PlayState != PlayState.LOBBY_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            this.CachedRoomMsg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "add" },
                { "i", this.GetMsgId() },
                { "cid", roomName }
            };
            if (expectedUserIds != null)
            {
                List<object> expecteds = expectedUserIds.Cast<object>().ToList();
                this.CachedRoomMsg.Add("expectMembers", expecteds);
            }
            this.SendLobbyMessage(this.CachedRoomMsg);
        }

        /// <summary>
        /// 重新加入房间
        /// </summary>
        /// <param name="roomName">房间名称</param>
        public void RejoinRoom(string roomName) {
            if (string.IsNullOrEmpty(roomName)) {
                throw new Exception("roomName is null");
            }
            if (this.PlayState != PlayState.LOBBY_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            this.CachedRoomMsg = new Dictionary<string, object>() { 
                { "cmd", "conv" },
                { "op", "add" },
                { "i", this.GetMsgId() },
                { "cid", roomName },
                { "rejoin", true },
            };
            this.SendGameMessage(this.CachedRoomMsg);
        }

        /// <summary>
        /// 随机加入或创建房间
        /// </summary>
        /// <param name="roomName">房间名称</param>
        /// <param name="roomOptions">创建房间选项，默认值为 null</param>
        /// <param name="expectedUserIds">邀请好友 ID 数组，默认值为 null</param>
        public void JoinOrCreateRoom(string roomName, RoomOptions roomOptions = null, List<string> expectedUserIds = null) {
            if (string.IsNullOrEmpty(roomName)) {
                throw new Exception("roomName is null");
            }
            if (this.PlayState != PlayState.LOBBY_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            this.CachedRoomMsg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "add" },
                { "i", this.GetMsgId() },
                { "cid", roomName },
            };
            if (roomOptions != null) {
                var roomOptionsDict = roomOptions.ToMsgObj();
                foreach (var entry in roomOptionsDict) {
                    this.CachedRoomMsg.Add(entry.Key, entry.Value);
                }
            }
            if (expectedUserIds != null) {
                List<object> expecteds = expectedUserIds.Cast<object>().ToList();
                this.CachedRoomMsg.Add("expectMembers", expecteds);
            }

            var msg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "add" },
                { "i", this.GetMsgId() },
                { "cid", roomName },
                { "createOnNotFound", true },
            };
            if (expectedUserIds != null) {
                List<object> expecteds = expectedUserIds.Cast<object>().ToList();
                msg.Add("expectMembers", expecteds);
            }
            this.SendLobbyMessage(msg);
        }

        /// <summary>
        /// 随机加入房间
        /// </summary>
        /// <param name="matchProperties">匹配属性，默认值为 null</param>
        /// <param name="expectedUserIds">邀请好友 ID 数组，默认值为 null</param>
        public void JoinRandomRoom(Dictionary<string, object> matchProperties = null, List<string> expectedUserIds = null) {
            this.CachedRoomMsg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "add" },
                { "i", this.GetMsgId() }
            };
            if (this.PlayState != PlayState.LOBBY_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            if (matchProperties != null) {
                this.CachedRoomMsg.Add("expectAttr", matchProperties);
            }
            if (expectedUserIds != null) {
                List<object> expecteds = expectedUserIds.Cast<object>().ToList();
                this.CachedRoomMsg.Add("expectMembers", expecteds);
            }

            var msg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "add-random" },
            };
            if (matchProperties != null)
            {
                msg.Add("expectAttr", matchProperties);
            }
            if (expectedUserIds != null)
            {
                List<object> expecteds = expectedUserIds.Cast<object>().ToList();
                msg.Add("expectMembers", expecteds);
            }
            this.SendLobbyMessage(msg);
        }

        /// <summary>
        /// 设置房间开启 / 关闭
        /// </summary>
        /// <param name="opened">是否开启</param>
        public void SetRoomOpened(bool opened) {
            if (this.Room == null) {
                throw new Exception("room is null");
            }
            if (this.PlayState != PlayState.GAME_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            var msg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "open" },
                { "i", this.GetMsgId() },
                { "toggle", opened },
            };
            this.SendGameMessage(msg);
        }

        /// <summary>
        /// 设置房间可见 / 不可见
        /// </summary>
        /// <param name="visible">是否可见</param>
        public void SetRoomVisible(bool visible) {
            if (this.Room == null) {
                throw new Exception("room is null");
            }
            if (this.PlayState != PlayState.GAME_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            var msg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "visible" },
                { "i", this.GetMsgId() },
                { "toggle", visible },
            };
            this.SendGameMessage(msg);
        }

        /// <summary>
        /// 设置房主
        /// </summary>
        /// <param name="newMasterId">新房主 ID</param>
        public void SetMaster(int newMasterId) {
            if (this.Room == null) {
                throw new ArgumentException("room is null");
            }
            if (this.PlayState != PlayState.GAME_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            var msg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "update-master-client" },
                { "i", this.GetMsgId() },
                { "masterActorId", newMasterId }
            };
            this.SendGameMessage(msg);
        }

        /// <summary>
        /// 发送自定义消息
        /// </summary>
        /// <param name="eventId">事件 ID</param>
        /// <param name="eventData">事件参数</param>
        /// <param name="options">发送事件选项</param>
        public void SendEvent(string eventId, Dictionary<string, object> eventData, SendEventOptions options) {
            if (this.Room == null) {
                throw new Exception("room is null");
            }
            if (this.Player == null) {
                throw new Exception("player is null");
            }
            if (this.PlayState != PlayState.GAME_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            var msg = new Dictionary<string, object>() { 
                { "cmd", "direct" },
                { "i", this.GetMsgId() },
                { "eventId", eventId },
                { "msg", eventData },
                { "receiverGroup", (int)options.ReceiverGroup },
            };
            if (options.targetActorIds != null) {
                List<object> targets = options.targetActorIds.Cast<object>().ToList();
                msg.Add("toActorIds", targets);
            }
            this.SendGameMessage(msg);
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        public void LeaveRoom() {
            if (this.PlayState != PlayState.GAME_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            var msg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "remove" },
                { "i", this.GetMsgId() },
                { "cid", this.Room.Name },
            };
            this.SendGameMessage(msg);
        }

        internal void SetRoomCustomProperties(Dictionary<string, object> properties, Dictionary<string, object> expectedValues) {
            if (properties == null) {
                throw new Exception("room props is null");
            }
            if (this.PlayState != PlayState.GAME_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            Dictionary<string, object> msg = new Dictionary<string, object>() {
                { "cmd", "conv" },
                { "op", "update" },
                { "i", this.GetMsgId() },
                { "attr", properties },
            };
            if (expectedValues != null) {
                msg.Add("expectAttr", expectedValues);
            }
            this.SendGameMessage(msg);
        }

        internal void SetPlayerCustomProperties(int playerId, Dictionary<string, object> properties, Dictionary<string, object> expectedValues) {
            if (properties == null) {
                throw new Exception("player props is null");
            }
            if (this.PlayState != PlayState.GAME_OPEN)
            {
                throw new Exception(string.Format("error play state: {0}", this.PlayState));
            }
            Dictionary<string, object> msg = new Dictionary<string, object>() { 
                { "cmd", "conv" },
                { "op", "update-player-prop" },
                { "i", this.GetMsgId() },
                { "targetActorId", playerId },
                { "attr", properties },
            };
            if (expectedValues != null) {
                msg.Add("expectAttr", expectedValues);
            }
            this.SendGameMessage(msg);
        }

		/// <summary>
        /// 由主线程调用
        /// </summary>
		public void HandleMessage() {
			if (this.waitingMessageQueue.Count > 0) {
				lock (this.waitingMessageQueue) {
                    Queue<Dictionary<string, object>> temp = this.handingMessageQueue;
					this.handingMessageQueue = this.waitingMessageQueue;
					this.waitingMessageQueue = temp;
				}
				while (this.handingMessageQueue.Count > 0) {
                    Dictionary<string, object> msgObj = this.handingMessageQueue.Dequeue();
                    // 根据 msgType 区分大厅消息还是房间消息
                    int msgType = (int)msgObj[MsgTypeKey];
					if (msgType == LobbyMsgType) {
						LobbyHandler.HandleMessage(this, msgObj);
					} else if (msgType == GameMsgType) {
						GameHandler.HandleMessage(this, msgObj);	
                    } else if (msgType == EventMsgType) {
                        EventHandler.HandleMessage(this, msgObj);
                    }
				}
			}
		}

        /// <summary>
        /// 注册事件回调
        /// </summary>
        /// <param name="evt">事件</param>
        /// <param name="action">回调</param>
		public void On(Event evt, Action<Dictionary<string, object>> action) {
			if (action == null) {
				throw new Exception("action is null");
			}
            List<Action<Dictionary<string, object>>> actions = null;
            if (!this.eventListeners.TryGetValue(evt, out actions)) {
				actions = new List<Action<Dictionary<string, object>>>();
				this.eventListeners.Add(evt, actions);
			}
			actions.Add(action);
		}

        /// <summary>
        /// 解注册事件回调
        /// </summary>
        /// <param name="evt">事件</param>
        /// <param name="action">回调</param>
		public void Off(Event evt, Action<Dictionary<string, object>> action) {
			if (action == null) {
				throw new Exception("action is null");
			}
			if (this.eventListeners.TryGetValue(evt, out var actions)) {
                if (!actions.Remove(action))
                {
                    throw new Exception("no action");
                }
            }
            else {
                throw new Exception("no action");
            }
		}

        internal void Emit(Event evt, Dictionary<string, object> eventData = null) {
			if (this.eventListeners.TryGetValue(evt, out var actions)) {
				for (int i = 0; i < actions.Count; i++) {
					var action = actions [i];
					action.Invoke(eventData);
				}
			}
		}

        internal void EmitInMainThread(Event evt, Dictionary<string, object> evtData = null) {
            lock (waitingMessageQueue) {
                var msgObj = new Dictionary<string, object>();
                msgObj.Add(MsgTypeKey, EventMsgType);
                msgObj.Add("eventId", evt);
                if (evtData != null) {
                    msgObj.Add("eventData", evtData);
                }
                this.waitingMessageQueue.Enqueue(msgObj);
            }
        }

        internal void ConnectToMaster(bool fromGame = false) {
            this.PlayState = PlayState.CONNECTING;
            this.CleanUp();
            this.GameToLobby = fromGame;
            this.webSocket = new WebSocket(this.masterServer);
            this.webSocket.SslConfiguration.EnabledSslProtocols = (System.Security.Authentication.SslProtocols)(SslProtocolsHack.Tls12 | SslProtocolsHack.Tls11 | SslProtocolsHack.Tls);
            this.webSocket.OnOpen += this.OnLobbyWebSocketOpen;
            this.webSocket.OnMessage += this.OnLobbyWebSocketMessage;
            this.webSocket.OnClose += this.OnLobbyWebSocketClose;
            this.webSocket.Connect();
        }

        void OnLobbyWebSocketOpen(object sender, EventArgs args) {
            Logger.Debug("Lobby websocket opened");
            this.LobbySessionOpen();
        }

        void OnLobbyWebSocketMessage(object sender, MessageEventArgs args) {
            // 加锁处理，websocket 库使用了「线程池」
            lock (waitingMessageQueue)
            {
                Logger.Debug("{0} <= {1}", this.UserId, args.Data);
                // 1. 反序列化
                Dictionary<string, object> msgDict = Json.Parse(args.Data) as Dictionary<string, object>;
                msgDict.Add(MsgTypeKey, LobbyMsgType);
                // 2. 插入待处理队列
                this.waitingMessageQueue.Enqueue(msgDict);
                this.StopPong();
                this.StartPongListener(LobbyKeepAliveDuration);
            }
        }

        void OnLobbyWebSocketClose(object sender, CloseEventArgs args) {
            Logger.Debug("Lobby websocket close");
            this.PlayState = PlayState.CLOSED;
            this.webSocket.OnOpen -= OnLobbyWebSocketOpen;
            this.webSocket.OnMessage -= OnLobbyWebSocketMessage;
            this.webSocket.OnClose -= OnLobbyWebSocketClose;
            this.webSocket = null;
            if (this.PlayState == PlayState.CONNECTING) {
                // 连接失败
                if (this.masterServer == this.secondaryServer)
                {
                    // 连接失败
                    this.EmitInMainThread(Event.CONNECT_FAILED);
                }
                else
                {
                    // 内部重连
                    this.masterServer = this.secondaryServer;
                    this.ConnectToMaster();
                }
            }
            else {
                // 断开连接
                this.EmitInMainThread(Event.DISCONNECTED);
            }
        }

        internal void ConnectToGame() {
            this.PlayState = PlayState.CONNECTING;
            this.CleanUp();
            this.webSocket = new WebSocket(this.GameServer);
            this.webSocket.OnOpen += OnGameWebSocketOpen;
            this.webSocket.OnMessage += OnGameWebSocketMessage;
            this.webSocket.OnClose += OnGameWebSocketClose;
            this.webSocket.Connect();
        }

        void OnGameWebSocketOpen(object sender, EventArgs args) {
            Logger.Debug("Game websocket opened");
            this.GameSessionOpen();
        }

        void OnGameWebSocketMessage(object sender, MessageEventArgs args) {
            // 加锁处理，websocket 库使用了「线程池」
            lock (waitingMessageQueue)
            {
                Logger.Debug("{0} <= {1}", this.UserId, args.Data);
                // 1. 反序列化
                Dictionary<string, object> msgObj = Json.Parse(args.Data) as Dictionary<string, object>;
                msgObj.Add(MsgTypeKey, GameMsgType);
                // 2. 插入待处理队列
                this.waitingMessageQueue.Enqueue(msgObj);
                this.StopPong();
                this.StartPongListener(GameKeepAliveDuration);
            }
        }

        void OnGameWebSocketClose(object sender, CloseEventArgs args) {
            Logger.Debug("Game websocket close");
            this.PlayState = PlayState.CLOSED;
            if (args.Code == 1006)
            {
                if (this.masterServer == this.secondaryServer)
                {
                    // 连接失败
                    this.EmitInMainThread(Event.CONNECT_FAILED);
                }
                else
                {
                    // 内部重连
                    this.masterServer = this.secondaryServer;
                    this.ConnectToMaster();
                }
            }
            else
            {
                this.EmitInMainThread(Event.DISCONNECTED);
            }
            StopPing();
        }

        void CleanUp() {
            if (this.webSocket != null) {
                // 解注册事件
                this.webSocket.OnOpen -= OnLobbyWebSocketOpen;
                this.webSocket.OnMessage -= OnLobbyWebSocketMessage;
                this.webSocket.OnClose -= OnLobbyWebSocketClose;
                this.webSocket.OnOpen -= OnGameWebSocketOpen;
                this.webSocket.OnMessage -= OnGameWebSocketMessage;
                this.webSocket.OnClose -= OnGameWebSocketClose;
                this.webSocket.Close();
                this.webSocket = null;
            }
        }

        void LobbySessionOpen() {
            var msg = new Dictionary<string, object>() {
                { "cmd", "session" },
                { "op", "open" },
                { "i", this.GetMsgId() },
                { "appId", this.appId },
                { "peerId", this.UserId },
                { "sdkVersion", Config.PlayVersion },
                { "gameVersion", this.gameVersion },
            };
            this.SendLobbyMessage(msg);
        }

        void GameSessionOpen() {
            var msg = new Dictionary<string, object>() {
                { "cmd", "session" },
                { "op", "open" },
                { "i", this.GetMsgId() },
                { "appId", this.appId },
                { "peerId", this.UserId },
                { "sdkVersion", Config.PlayVersion },
                { "gameVersion", this.gameVersion },
            };
            this.SendGameMessage(msg);
        }

        internal void SendLobbyMessage(Dictionary<string, object> msg) {
            Send(msg, LobbyKeepAliveDuration);
        }

        internal void SendGameMessage(Dictionary<string, object> msg) {
            Send(msg, GameKeepAliveDuration);
        }

        void Send(Dictionary<string, object> msg, int duration) {
            if (msg == null) {
                throw new Exception("msg is null");
            }
            if (this.webSocket.ReadyState == WebSocketState.Open) {
                string msgStr = Json.Encode(msg);
                Logger.Debug("{0} => {1}", this.UserId, msgStr);
                this.webSocket.Send(msgStr);
                // 心跳包
                this.StopPing();
                this.ping = new System.Timers.Timer(duration);
                this.ping.Elapsed += (sender, e) => {
                    this.webSocket.Ping();
                };
                this.ping.Start();
            }
            else {
                this.StopPing();
                this.StopPong();
            }
        }

        void StopPing() {
            if (this.ping != null) {
                this.ping.Stop();
                this.ping = null;
            }
        }

        void StopPong() {
            if (this.pong != null) {
                this.pong.Stop();
                this.pong = null;
            }
        }

        void StartPongListener(int duration) {
            this.pong = new System.Timers.Timer(duration * MaxNoPongTimes);
            this.pong.Elapsed += (sender, e) =>
            {
                this.webSocket.Close();
            };
            this.pong.Start();
        }

		internal int GetMsgId() {
			this.msgId++;
			return this.msgId;
		}

        private static Play instance = null;

        /// <summary>
        /// 获取 Play 客户端单例
        /// </summary>
        /// <value>The instantce.</value>
        public static Play Instance {
            get {
                if (instance == null) {
                    instance = new Play();
                }
                return instance;
            }
        }
	}
}
