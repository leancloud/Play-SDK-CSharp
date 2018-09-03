using System;
using System.Collections.Generic;

namespace LeanCloud.Play 
{
    internal static class GameHandler
    {
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
                    case "conv":
                        switch (op)
                        {
                            case "started":
                                HandleCreatedRoom(play, msg);
                                break;
                            case "added":
                                HandleJoinedRoom(play, msg);
                                break;
                            case "members-joined":
                                HandleNewPlayerJoinedRoom(play, msg);
                                break;
                            case "members-left":
                                HandlePlayerLeftRoom(play, msg);
                                break;
                            case "master-client-updated":
                                HandleMasterUpdated(play, msg);
                                break;
                            case "master-client-changed":
                                HandleMasterChanged(play, msg);
                                break;
                            case "opened-notify":
                                HandleRoomOpenedChanged(play, msg);
                                break;
                            case "visible-notify":
                                HandleRoomVisibleChanged(play, msg);
                                break;
                            case "updated":
                                HandleRoomCustomPropertiesChangedResponse(play, msg);
                                break;
                            case "updated-notify":
                                HandleRoomCustomPropertiesChanged(play, msg);
                                break;
                            case "player-prop-updated":
                                HandlePlayerCustomPropertiesChangedResponse(play, msg);
                                break;
                            case "player-props":
                                HandlePlayerCustomPropertiesChanged(play, msg);
                                break;
                            case "members-offline":
                                HandlePlayerOffline(play, msg);
                                break;
                            case "members-online":
                                HandlePlayerOnline(play, msg);
                                break;
                            case "removed":
                                HandleLeaveRoom(play, msg);
                                break;
                            default:
                                Logger.Warn("no handler for {0}", Json.Encode(msg));
                                break;
                        }
                        break;
                    case "direct":
                        HandleEvent(play, msg);
                        break;
                    case "ack":
                        break;
                    case "events":
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
            play.PlayState = PlayState.GAME_OPEN;
            play.CachedRoomMsg["i"] = play.GetMsgId();
            play.SendGameMessage(play.CachedRoomMsg);
        }

        static void HandleCreatedRoom(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("reasonCode", out object reasonCodeObj)) {
                int code = (int)(long)reasonCodeObj;
                string detail = msg["detail"] as string;
                Dictionary<string, object> eventData = new Dictionary<string, object>();
                eventData.Add("code", code);
                eventData.Add("detail", detail);
                play.Emit(Event.ROOM_CREATE_FAILED, eventData);
            } else {
                play.Room = Room.NewFromDictionary(play, msg);
                play.Emit(Event.ROOM_CREATED);
                play.Emit(Event.ROOM_JOINED);
            }
        }

        static void HandleJoinedRoom(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("reasonCode", out object reasonCodeObj)) {
                int code = (int)(long)reasonCodeObj;
                string detail = msg["detail"] as string;
                Dictionary<string, object> eventData = new Dictionary<string, object>();
                eventData.Add("code", code);
                eventData.Add("detail", detail);
                play.Emit(Event.ROOM_JOIN_FAILED, eventData);
            } else {
                play.Room = Room.NewFromDictionary(play, msg);
                play.Emit(Event.ROOM_JOINED);
            }
        }

        static void HandleNewPlayerJoinedRoom(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("member", out object playerObj)) {
                Dictionary<string, object> playerDict = playerObj as Dictionary<string, object>;
                Player player = new Player(play);
                player.InitWithDictionary(playerDict);
                play.Room.AddPlayer(player);
                Dictionary<string, object> eventData = new Dictionary<string, object>();
                eventData.Add("newPlayer", player);
                play.Emit(Event.PLAYER_ROOM_JOINED, eventData);
            }
            else {
                Logger.Error("Handle new player joined room error : {0}", Json.Encode(msg));
            }
        }

        static void HandlePlayerLeftRoom(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("initByActor", out object playerIdObj)) {
                int playerId = (int)(long)playerIdObj;
                var leftPlayer = play.Room.GetPlayer(playerId);
                play.Room.RemovePlayer(playerId);
                Dictionary<string, object> eventData = new Dictionary<string, object>();
                eventData.Add("leftPlayer", leftPlayer);
                play.Emit(Event.PLAYER_ROOM_LEFT, eventData);
            } else {
                Logger.Error("Handle player left room error : {0}", Json.Encode(msg));
            }
        }

        static void HandleMasterUpdated(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("reasonCode", out object reasonCodeObj)) {
                int reasonCode = (int)(long)reasonCodeObj;
                string detail = msg["detail"] as string;
                Logger.Error("Handle master updated error : {0}", Json.Encode(msg));
            }
        }

        static void HandleMasterChanged(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("masterActorId", out object newMasterIdObj)) {
                int newMasterId = (int)(long)newMasterIdObj;
                play.Room.MasterActorId = newMasterId;
                var newMaster = play.Room.GetPlayer(newMasterId);
                Dictionary<string, object> eventData = new Dictionary<string, object>();
                eventData.Add("newMaster", newMaster);
                play.Emit(Event.MASTER_SWITCHED, eventData);
            } else {
                Logger.Error("Handle master changed error : {0}", Json.Encode(msg));
            }
        }

        static void HandleRoomOpenedChanged(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("toggle", out object openedObj)) {
                bool opened = (bool)openedObj;
                play.Room.Opened = opened;
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("opened", opened);
                play.Emit(Event.ROOM_OPEN_CHANGED, data);
            } else {
                Logger.Error("Handle room opened changed error : {0}", Json.Encode(msg));
            }
        }

        static void HandleRoomVisibleChanged(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("toggle", out object visibleObj)) {
                bool visible = (bool)visibleObj;
                play.Room.Visible = visible;
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("visible", visible);
                play.Emit(Event.ROOM_VISIBLE_CHANGED, data);
            } else {
                Logger.Error("Handle room visible changed error : {0}", Json.Encode(msg));
            }
        }

        static void HandleRoomCustomPropertiesChangedResponse(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("reasonCode", out object reasonCodeObj))
            {
                int reasonCode = (int)(long)reasonCodeObj;
                string detail = msg["detail"] as string;
                Logger.Error("Handle room custom properties changed response error : {0}", Json.Encode(msg));
            }
        }

        static void HandleRoomCustomPropertiesChanged(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("attr", out object attrObj)) {
                var changedProps = attrObj as Dictionary<string, object>;
                play.Room.MergeProperties(changedProps);
                play.Emit(Event.ROOM_CUSTOM_PROPERTIES_CHANGED, changedProps);
            } else {
                Logger.Error("Handle room custom properties changed error : {0}", Json.Encode(msg));
            }
        }

        static void HandlePlayerCustomPropertiesChangedResponse(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("reasonCode", out object reasonCodeObj))
            {
                int reasonCode = (int)(long)reasonCodeObj;
                string detail = msg["detail"] as string;
                Logger.Error("Handle player custom properties changed response error : {0}", Json.Encode(msg));
            }
        }

        static void HandlePlayerCustomPropertiesChanged(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("actorId", out object playerIdObj)) {
                int playerId = (int)(long)playerIdObj;
                Dictionary<string, object> changedProps = msg["attr"] as Dictionary<string, object>;
                var player = play.Room.GetPlayer(playerId);
                player.MergeProperties(changedProps);
                var eventData = new Dictionary<string, object>() {
                    { "player", player },
                    { "changedProps", changedProps },
                };
                play.Emit(Event.PLAYER_CUSTOM_PROPERTIES_CHANGED, eventData);
            } else {
                Logger.Error("Handle player custom properties changed error : {0}", Json.Encode(msg));
            }
        }

        static void HandlePlayerOffline(Play play, Dictionary<string, object> msg)
        {
            if (msg.TryGetValue("initByActor", out object playerIdObj)) {
                int playerId = (int)(long)playerIdObj;
                var player = play.Room.GetPlayer(playerId);
                player.IsActive = false;
                Dictionary<string, object> eventData = new Dictionary<string, object>();
                eventData.Add("player", player);
                play.Emit(Event.PLAYER_ACTIVITY_CHANGED, eventData);
            } else {
                Logger.Error("Handle player offline error : {0}", Json.Encode(msg));
            }
        }

        static void HandlePlayerOnline(Play play, Dictionary<string, object> msg) {
            if (msg.TryGetValue("member", out object memberObj)) {
                var member = memberObj as Dictionary<string, object>;
                int playerId = (int)(long)member["actorId"];
                var player = play.Room.GetPlayer(playerId);
                player.IsActive = true;
                Dictionary<string, object> eventData = new Dictionary<string, object>();
                eventData.Add("player", player);
                play.Emit(Event.PLAYER_ACTIVITY_CHANGED, eventData);
            } else {
                Logger.Error("Handle player onlien error : {0}", Json.Encode(msg));
            }
        }

        static void HandleLeaveRoom(Play play, Dictionary<string, object> msg) {
            play.Room = null;
            play.Player = null;
            play.ConnectToMaster(true);
        }

        static void HandleEvent(Play play, Dictionary<string, object> msg) {
            if (msg.TryGetValue("eventId", out object eventIdObj)) {
                string eventId = eventIdObj as string;
                Dictionary<string, object> eventData = msg["msg"] as Dictionary<string, object>;
                int senderId = (int)(long)msg["fromActorId"];
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("eventId", eventId);
                data.Add("eventData", eventData);
                data.Add("senderId", senderId);
                play.Emit(Event.CUSTOM_EVENT, data);
            } else {
                Logger.Error("Handle event error : {0}", Json.Encode(msg));
            }
        }
    }
}
