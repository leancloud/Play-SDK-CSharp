using System;
using System.Collections.Generic;
using System.Linq;

namespace LeanCloud.Play
{
    /// <summary>
    /// 创建房间标识
    /// </summary>
    public static class CreateRoomFlag {
        /// <summary>
        /// Master 掉线后固定 Master
        /// </summary>
        public const int FixedMaster = 1;
        /// <summary>
        /// 只允许 Master 设置房间属性
        /// </summary>
        public const int MasterUpdateRoomProperties = 2;
        /// <summary>
        /// 只允许 Master 设置 Master
        /// </summary>
        public const int MasterSetMaster = 3;
    }

    /// <summary>
    /// 创建房间选项
    /// </summary>
    public class RoomOptions
    {
        private const int MAX_PLAYER_COUNT = 10;

        /// <summary>
        /// 房间是否打开
        /// </summary>
        /// <value><c>true</c> if opened; otherwise, <c>false</c>.</value>
        public bool Opened {
            get; set;
        }

        /// <summary>
        /// 房间是否可见，只有「可见」的房间会出现在房间列表里
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        public bool Visible {
            get; set;
        }

        /// <summary>
        /// 房间为空后，延迟销毁的时间
        /// </summary>
        /// <value>The empty room ttl.</value>
        public int EmptyRoomTtl {
            get; set;
        }

        /// <summary>
        /// 玩家掉线后，延迟销毁的时间
        /// </summary>
        /// <value>The player ttl.</value>
        public int PlayerTtl {
            get; set;
        }

        /// <summary>
        /// 最大玩家数量
        /// </summary>
        /// <value>The max player count.</value>
        public int MaxPlayerCount {
            get; set;
        }

        /// <summary>
        /// 自定义房间属性
        /// </summary>
        /// <value>The custom room properties.</value>
        public Dictionary<string, object> CustomRoomProperties {
            get; set;
        }

        /// <summary>
        /// 在大厅中可获得的房间属性「键」数组
        /// </summary>
        /// <value>The custo room property keys for lobby.</value>
        public List<string> CustoRoomPropertyKeysForLobby {
            get; set;
        }

        /// <summary>
        /// 创建房间标记，可多选
        /// </summary>
        /// <value>The flag.</value>
        public int Flag {
            get; set;
        }

        public RoomOptions() {
            this.Opened = true;
            this.Visible = true;
            this.EmptyRoomTtl = 0;
            this.PlayerTtl = 0;
            this.MaxPlayerCount = MAX_PLAYER_COUNT;
            this.CustomRoomProperties = null;
            this.CustoRoomPropertyKeysForLobby = null;
            this.Flag = 0;
        }

        internal Dictionary<string, object> ToMsgObj() {
            if (this.EmptyRoomTtl < 0) {
                throw new ArgumentException("EmptyRoomTtl MUST NOT be less than 0");
            }
            if (this.PlayerTtl < 0) {
                throw new ArgumentException("PlayerTtl MUST NOT be less than 0");
            }
            if (this.MaxPlayerCount < 0 || this.MaxPlayerCount > MAX_PLAYER_COUNT) {
                throw new ArgumentException("MaxPlayerCount MUST be [1, 10]");
            }

            Dictionary<string, object> msg = new Dictionary<string, object>();
            msg.Add("open", this.Opened);
            msg.Add("visible", this.Visible);
            if (this.EmptyRoomTtl > 0) {
                msg.Add("emptyRoomTtl", this.EmptyRoomTtl);
            }
            if (this.PlayerTtl > 0) {
                msg.Add("playerTtl", this.PlayerTtl);
            }
            if (this.MaxPlayerCount < MAX_PLAYER_COUNT) {
                msg.Add("maxMembers", this.MaxPlayerCount);
            }
            if (this.CustomRoomProperties != null) {
                msg.Add("attr", this.CustomRoomProperties);
            }
            if (this.CustoRoomPropertyKeysForLobby != null) {
                List<object> keys = this.CustoRoomPropertyKeysForLobby.Cast<object>().ToList();
                msg.Add("lobbyAttrKeys", keys);
            }
            if (this.Flag > 0) {
                msg.Add("flag", this.Flag);
            }
            return msg;
        }
    }
}
