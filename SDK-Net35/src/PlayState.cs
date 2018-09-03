using System;

namespace LeanCloud.Play
{
    /// <summary>
    /// 连接状态
    /// </summary>
    internal enum PlayState
    {
        /// <summary>
        /// 断开
        /// </summary>
        CLOSED = 0,
        /// <summary>
        /// 连接中
        /// </summary>
        CONNECTING = 1,
        /// <summary>
        /// 大厅连接成功
        /// </summary>
        LOBBY_OPEN = 2,
        /// <summary>
        /// 房间连接成功
        /// </summary>
        GAME_OPEN = 3,
        /// <summary>
        /// 断开中
        /// </summary>
        CLOSING = 4,
    }
}
