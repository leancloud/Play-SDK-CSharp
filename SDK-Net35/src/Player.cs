using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace LeanCloud.Play {
	/// <summary>
    /// 玩家类
    /// </summary>
	public class Player {
		private Play play = null;

        /// <summary>
        /// 玩家 ID
        /// </summary>
        /// <value>The user identifier.</value>
		public string UserId {
            get; internal set;
		}

        /// <summary>
        /// 房间玩家 ID
        /// </summary>
        /// <value>The actor identifier.</value>
		public int ActorId {
            get; internal set;
		}

        /// <summary>
        /// 判断是不是活跃状态
        /// </summary>
        /// <value><c>true</c> if is active; otherwise, <c>false</c>.</value>
        public bool IsActive {
            get; internal set;
        }

        /// <summary>
        /// 获取自定义属性
        /// </summary>
        /// <value>The custom properties.</value>
        public Dictionary<string, object> CustomProperties {
            get; internal set;
        }

        /// <summary>
        /// 判断是不是当前客户端玩家
        /// </summary>
        /// <value><c>true</c> if is local; otherwise, <c>false</c>.</value>
        public bool IsLocal
        {
            get
            {
                return this.ActorId != -1 && this.ActorId == this.play.Player.ActorId;
            }
        }

        /// <summary>
        /// 判断是不是主机玩家
        /// </summary>
        /// <value><c>true</c> if is master; otherwise, <c>false</c>.</value>
        public bool IsMaster
        {
            get
            {
                return this.ActorId != -1 && this.ActorId == this.play.Room.MasterActorId;
            }
        }

        /// <summary>
        /// 设置玩家的自定义属性
        /// </summary>
        /// <param name="properties">Properties.</param>
        /// <param name="expectedValues">Expected values.</param>
        public void SetCustomProperties(Dictionary<string, object> properties, Dictionary<string, object> expectedValues = null)
        {
            this.play.SetPlayerCustomProperties(this.ActorId, properties, expectedValues);
        }

        internal Player (Play play) {
			this.play = play;
            this.ActorId = -1;
            this.UserId = null;
		}

        internal static Player NewFromDictionary(Play play, Dictionary<string, object> playerDict) {
            Player player = new Player(play);
            player.InitWithDictionary(playerDict);
            return player;
        }

        internal void InitWithDictionary(Dictionary<string, object> playerDict) {
            if (playerDict == null) {
                throw new ArgumentException("Player data is null");
            }
            this.UserId = playerDict["pid"] as string;
            this.ActorId = (int)(long)playerDict["actorId"];
            if (playerDict.TryGetValue("attr", out object propsObj)) {
                var props = propsObj as Dictionary<string, object>;
                this.CustomProperties = props;
            } else {
                this.CustomProperties = new Dictionary<string, object>();
            }
        }

        internal void MergeProperties(Dictionary<string, object> changedProps) {
            if (changedProps == null)
                return;

            foreach (KeyValuePair<string, object> entry in changedProps) {
                this.CustomProperties[entry.Key] = entry.Value;
            }
        }
	}
}
