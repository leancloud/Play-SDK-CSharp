using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Common;

namespace LeanCloud.Play {
	internal class Lobby {
        enum State {
            Init,
            Joining,
            Lobby,
            Leaving,
            Closed
        }

        Client Client {
			get;
        }

        State state;
        LobbyConnection lobbyConn;

        internal List<LobbyRoom> LobbyRoomList {
            get; private set;
        }

		internal Lobby(Client client) {
			Client = client;
            state = State.Init;
        }

        internal async Task Join() {
            if (state == State.Joining || state == State.Lobby) {
                return;
            } 
            state = State.Joining;
            LobbyInfo lobbyInfo;
            try {
                lobbyInfo = await Client.lobbyService.Authorize();
            } catch (Exception e) {
                state = State.Init;
                throw e;
            }
            try {
                lobbyConn = new LobbyConnection();
                await lobbyConn.Connect(Client.AppId, lobbyInfo.Url, Client.GameVersion, Client.UserId, lobbyInfo.SessionToken);
                await lobbyConn.JoinLobby();
                lobbyConn.OnRoomListUpdated = (lobbyRoomList) => {
                    LobbyRoomList = lobbyRoomList;
                    Client.OnLobbyRoomListUpdated?.Invoke(LobbyRoomList);
                };
                state = State.Lobby;
            } catch (Exception e) {
                if (lobbyConn != null) {
                    await lobbyConn.Close();
                }
                state = State.Init;
                throw e;
            }
        }

        internal async Task Leave() {
            try {
                await lobbyConn.LeaveLobby();
            } finally {
                await Close();
            }
        }

        internal async Task Close() {
            try {
                await lobbyConn.Close();
                lobbyConn.OnRoomListUpdated = null;
            } catch (Exception e) {
                Logger.Error(e.Message);
            }
        }
    }
}
