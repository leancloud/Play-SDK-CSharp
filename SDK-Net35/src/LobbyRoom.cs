using System;
using System.Collections.Generic;
using System.Linq;

namespace LeanCloud.Play 
{
    /// <summary>
    /// 大厅房间数据类
    /// </summary>
    public class LobbyRoom
    {
        /// <summary>
        /// 房间名称
        /// </summary>
        /// <value>The name of the room.</value>
        public string RoomName
        {
            get; internal set;
        }

        /// <summary>
        /// 房间最大玩家数
        /// </summary>
        /// <value>The max player count.</value>
        public int MaxPlayerCount
        {
            get; internal set;
        }

        /// <summary>
        /// 邀请好友 ID 数组
        /// </summary>
        /// <value>The expected user identifiers.</value>
        public List<string> ExpectedUserIds
        {
            get; internal set;
        }

        /// <summary>
        /// 房间置空后销毁时间（秒）
        /// </summary>
        /// <value>The empty room ttl.</value>
        public int EmptyRoomTtl
        {
            get; internal set;
        }

        /// <summary>
        /// 玩家离线后踢出房间时间（秒）
        /// </summary>
        /// <value>The player ttl.</value>
        public int PlayerTtl {
            get; internal set;
        }

        /// <summary>
        /// 当前房间玩家数量
        /// </summary>
        /// <value>The player count.</value>
        public int PlayerCount {
            get; internal set;
        }

        /// <summary>
        /// 房间属性
        /// </summary>
        /// <value>The custom room properties.</value>
        public Dictionary<string, object> CustomRoomProperties {
            get; internal set;
        }

        internal LobbyRoom(Dictionary<string, object> roomDict) {
            this.RoomName = roomDict["cid"] as string;
            this.MaxPlayerCount = (int)(long)roomDict["maxMembers"];
            if (roomDict.TryGetValue("expectMembers", out object expectedsObj)) {
                this.ExpectedUserIds = (expectedsObj as List<object>).Cast<string>().ToList();   
            }
            if (roomDict.ContainsKey("emptyRoomTtl")) {
                this.EmptyRoomTtl = (int)(long)roomDict["emptyRoomTtl"];
            }
            if (roomDict.ContainsKey("playerTtl")) {
                this.PlayerTtl = (int)(long)roomDict["playerTtl"];
            }
            if (roomDict.ContainsKey("playerCount")) {
                this.PlayerCount = (int)(long)roomDict["playerCount"];
            }
            if (roomDict.TryGetValue("attr", out object propsObj)) {
                this.CustomRoomProperties = propsObj as Dictionary<string, object>;
            }
        }
    }
}
