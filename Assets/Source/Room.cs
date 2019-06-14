using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Play {
    /// <summary>
    /// 房间类
    /// </summary>
	public class Room {
        internal Client Client {
            get; set;
        }

        internal Dictionary<int, Player> playerDict;

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

        /// <summary>
        /// 设置房间的自定义属性
        /// </summary>
        /// <param name="properties">自定义属性</param>
        /// <param name="expectedValues">期望属性，用于 CAS 检测</param>
        public Task SetCustomProperties(PlayObject properties, PlayObject expectedValues = null) {
            return Client.SetRoomCustomProperties(properties, expectedValues);
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

        public Task<bool> SetOpened(bool opened) {
            return Client.SetRoomOpen(opened);
        }

        public Task<bool> SetVisible(bool visible) {
            return Client.SetRoomVisible(visible);
        }

        public Task<int> SetMaxPlayerCount(int count) {
            return Client.SetRoomMaxPlayerCount(count);
        }

        public Task<List<string>> SetExpectedUserIds(List<string> expectedUserIds) {
            return Client.SetRoomExpectedUserIds(expectedUserIds);
        }

        public Task ClearExpectedUserIds() {
            return Client.ClearRoomExpectedUserIds();
        }

        public Task<List<string>> AddExpectedUserIds(List<string> expectedUserIds) {
            return Client.AddRoomExpectedUserIds(expectedUserIds);
        }

        public Task<List<string>> RemoveExpectedUserIds(List<string> expectedUserIds) {
            return Client.RemoveRoomExpectedUserIds(expectedUserIds);
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

        internal Dictionary<string, object> MergeSystemProps(Dictionary<string, object> changedProps) {
            var props = new Dictionary<string, object>();
            if (changedProps.TryGetValue("open", out object openObj)) {
                Open = bool.Parse(openObj.ToString());
                props["open"] = Open;
            }
            if (changedProps.TryGetValue("visible", out object visibleObj)) {
                Visible = bool.Parse(visibleObj.ToString());
                props["visible"] = Visible;
            }
            if (changedProps.TryGetValue("maxMembers", out object maxPlayerCountObj)) {
                MaxPlayerCount = int.Parse(maxPlayerCountObj.ToString());
                props["maxPlayerCount"] = MaxPlayerCount;
            }
            if (changedProps.TryGetValue("expectMembers", out object expectedUserIdsObj)) {
                ExpectedUserIds = (expectedUserIdsObj as List<object>).Cast<string>().ToList();
                props["expectedUserIds"] = ExpectedUserIds;
            }
            return props;
        }
    }
}
