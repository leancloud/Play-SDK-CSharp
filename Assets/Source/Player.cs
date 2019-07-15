﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

namespace LeanCloud.Play {
	/// <summary>
    /// 玩家类
    /// </summary>
	public class Player {
		internal Client Client {
            get; set;
        }

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
        public PlayObject CustomProperties {
            get; internal set;
        }

        /// <summary>
        /// 判断是不是当前客户端玩家
        /// </summary>
        /// <value><c>true</c> if is local; otherwise, <c>false</c>.</value>
        public bool IsLocal {
            get {
                return ActorId != -1 && ActorId == Client.Player.ActorId;
            }
        }

        /// <summary>
        /// 判断是不是主机玩家
        /// </summary>
        /// <value><c>true</c> if is master; otherwise, <c>false</c>.</value>
        public bool IsMaster {
            get {
                return ActorId != -1 && ActorId == Client.Room.MasterActorId;
            }
        }

        /// <summary>
        /// 设置玩家的自定义属性
        /// </summary>
        /// <param name="properties">Properties.</param>
        /// <param name="expectedValues">Expected values.</param>
        public Task SetCustomProperties(PlayObject properties, PlayObject expectedValues = null) {
            return Client.SetPlayerCustomProperties(ActorId, properties, expectedValues);
        }

        internal Player() {
            ActorId = -1;
            UserId = null;
		}

        internal void MergeCustomProperties(PlayObject changedProps) {
            if (changedProps == null)
                return;

            lock (CustomProperties) { 
                foreach (var entry in changedProps) {
                    CustomProperties[entry.Key] = entry.Value;
                }
            }
        }
	}
}
