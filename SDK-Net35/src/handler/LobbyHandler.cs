using System;
using System.Collections.Generic;
using System.Linq;

namespace LeanCloud.Play {
    internal static class LobbyHandler
    {
        private const string SESSION = "session";

        internal static void HandleMessage(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("cmd", out object cmdObj)) {
                string cmd = cmdObj as string;
                string op = null;
                if (msg.TryGetValue("op", out object opObj))
                {
                    op = opObj as string;
                }
                switch (cmd)
                {
                    case "session":
                        switch (op)
                        {
                            case "opened":
                                HandleSessionOpen(play, msg);
                                break;
                            default:
                                Logger.Warn("no handler for {0}", Json.Encode(msg));
                                break;
                        }
                        break;
                    case "lobby":
                        switch (op)
                        {
                            case "added":
                                HandleJoinedLobby(play, msg);
                                break;
                            case "room-list":
                                HandleRoomList(play, msg);
                                break;
                            case "remove":
                                HandleLeftLobby(play);
                                break;
                            default:
                                Logger.Warn("no handler for {0}", Json.Encode(msg));
                                break;
                        }
                        break;
                    case "statistic":
                        HandleStatistic();
                        break;
                    case "conv":
                        switch (op)
                        {
                            case "results":
                                HandleRoomList(play, msg);
                                break;
                            case "started":
                                HandleCreateRoom(play, msg);
                                break;
                            case "added":
                                HandleJoinRoom(play, msg);
                                break;
                            case "random-added":
                                HandleJoinRoom(play, msg);
                                break;
                            default:
                                Logger.Warn("no handler for {0}", Json.Encode(msg));
                                break;
                        }
                        break;
                    case "event":
                        break;
                    case "error":
                        ErrorHandler.HandleError(play, msg);
                        break;
                    default:
                        Logger.Warn("no handler for {0}", Json.Encode(msg));
                        break;
                }
            } else {
                Logger.Error(string.Format("Error message : {0}", Json.Encode(msg)));
            }

        }

        static void HandleSessionOpen(Play play, Dictionary<string, object> msg)
        {
            var player = new Player(play);
            player.UserId = play.UserId;
            play.Player = player;
            if (play.AutoJoinLobby)
            {
                play.JoinLobby();
            }
            if (play.GameToLobby) {
                play.Emit(Event.ROOM_LEFT);
                play.GameToLobby = false;
            } else {
                play.Emit(Event.CONNECTED);
            }
        }

        static void HandleJoinedLobby(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("reasonCode", out object reasonCodeObj)) {
                int code = (int)(long)reasonCodeObj;
                string detail = msg["detail"] as string;
                Logger.Error("Handle joined lobby error : {0}", Json.Encode(msg));
            }
            else
            {
                play.InLobby = true;
                play.Emit(Event.LOBBY_JOINED);
            }
        }

        static void HandleLeftLobby(Play play)
        {
            play.InLobby = false;
            play.Emit(Event.LOBBY_LEFT);
        }

        static void HandleRoomList(Play play, Dictionary<string, object> msg)
        {
            play.LobbyRoomList = new List<LobbyRoom>();
            if (msg.TryGetValue("list", out object roomsObj)) {
                List<object> rooms = roomsObj as List<object>;
                foreach (Dictionary<string, object> room in rooms) {
                    var lobbyRoom = new LobbyRoom(room);
                    play.LobbyRoomList.Add(lobbyRoom);
                }
            }
            play.Emit(Event.LOBBY_ROOM_LIST_UPDATED);
        }

        static void HandleStatistic() {

        }

        static void HandleCreateRoom(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("reasonCode", out object reasonCodeObj)) {
                int code = (int)(long)reasonCodeObj;
                string detail = msg["detail"] as string;
                Dictionary<string, object> eventData = new Dictionary<string, object>() {
                    { "code", (int)code },
                    { "detail", detail },
                };
                play.Emit(Event.ROOM_CREATE_FAILED, eventData);
            } else {
                play.CachedRoomMsg["op"] = "start";
                HandleGameServer(play, msg);
            }
        }

        static void HandleJoinRoom(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("reasonCode", out object reasonCodeObj)) {
                int code = (int)(long)reasonCodeObj;
                string detail = msg["detail"] as string;
                Dictionary<string, object> eventData = new Dictionary<string, object>() {
                    { "code", (int)code },
                    { "detail", detail },
                };
                play.Emit(Event.ROOM_JOIN_FAILED, eventData);
            } else {
                play.CachedRoomMsg["op"] = "add";
                HandleGameServer(play, msg);
            }
        }

        static void HandleGameServer(Play play, Dictionary<string, object> msg) {
            if (play.InLobby) {
                play.InLobby = false;
                play.Emit(Event.LOBBY_LEFT);
            }
            play.GameServer = msg["secureAddr"] as string;
            if (msg.ContainsKey("cid")) {
                play.CachedRoomMsg["cid"] = msg["cid"] as string;
            }
            play.ConnectToGame();
        }
	}
}
