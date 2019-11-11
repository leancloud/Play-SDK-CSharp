using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;

namespace LeanCloud.Play {
    internal class LobbyConnection : Connection {
        internal async Task JoinLobby() {
            var request = NewRequest();
            await SendRequest(CommandType.Lobby, OpType.Add, request);
        }

        internal async Task LeaveLobby() {
            RequestMessage request = NewRequest();
            await SendRequest(CommandType.Lobby, OpType.Remove, request);
        }

        protected override int GetPingDuration() {
            return 20;
        }

        protected override string GetFastOpenUrl(string server, string appId, string gameVersion, string userId, string sessionToken) {
            return $"{server}/1/multiplayer/lobby/websocket?appId={appId}&sdkVersion={Config.SDKVersion}&protocolVersion={Config.ProtocolVersion}&gameVersion={gameVersion}&userId={userId}&sessionToken={sessionToken}";
        }

        protected override void HandleNotification(CommandType cmd, OpType op, Body body) {
            OnMessage?.Invoke(cmd, op, body);
        }
    }
}
