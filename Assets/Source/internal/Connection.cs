using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;
using Google.Protobuf;

namespace LeanCloud.Play {
    public abstract class Connection {
        enum State {
            Init,
            Connecting,
            Connected,
            Disconnected,
            Closed,
        }

        const int RECV_BUFFER_SIZE = 1024;
        static readonly string PING = "{}";

        protected WebSocket ws;
        protected ClientWebSocket client;
        readonly Dictionary<int, TaskCompletionSource<ResponseWrapper>> responses;

        internal event Action<CommandType, OpType, Body> OnMessage;
        internal event Action<int, string> OnClose;
        internal event Action<int, string> OnError;
        
        string userId;


        public bool IsOpen {
            get {
                return client != null && client.State == WebSocketState.Open;
            }
        }

        public async Task Close() {
            try {
                if (IsOpen) {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "1", CancellationToken.None);
                }
            } catch (Exception e) {
                Logger.Error(e.Message);
            }
        }

        public void Disconnect() {
            OnClose?.Invoke(0, string.Empty);
            _ = Close();
        }

        protected async Task Connnect(string server, string userId) {
            this.userId = userId;
            client = new ClientWebSocket();
            client.Options.AddSubProtocol("protobuf.1");
            client.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            await client.ConnectAsync(new Uri(server), default);
            _ = StartReceive();
        }

        internal async Task<Task<ResponseWrapper>> Connect(string appId, string server, string gameVersion, string userId, string sessionToken) {
            this.userId = userId;
            TaskCompletionSource<ResponseWrapper> tcs = new TaskCompletionSource<ResponseWrapper>();
            client = new ClientWebSocket();
            client.Options.AddSubProtocol("protobuf.1");
            client.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            string newServer = server.Replace("https://", "wss://");
            string url = GetFastOpenUrl(newServer, appId, gameVersion, userId, sessionToken);
            await client.ConnectAsync(new Uri(url), default);
            _ = StartReceive();
            responses.Add(0, tcs);
            return tcs.Task;
        }

        protected Task<ResponseWrapper> SendRequest(CommandType cmd, OpType op, RequestMessage request) {
            var tcs = new TaskCompletionSource<ResponseWrapper>();
            responses.Add(request.I, tcs);
            _ = Send(cmd, op, new Body {
                Request = request
            });
            return tcs.Task;
        }

        protected void SendDirectCommand(DirectCommand directCommand) {
            _ = Send(CommandType.Direct, OpType.None, new Body {
                Direct = directCommand
            });
        }

        protected async Task Send(CommandType cmd, OpType op, Body body) {
            if (!IsOpen) {
                throw new Exception("WebSocket is not open when send data");
            }
            Logger.Debug("{0} => {1}/{2}: {3}", userId, cmd, op, body.ToString());
            var command = new Command {
                Cmd = cmd,
                Op = op,
                Body = body.ToByteString()
            };
            ArraySegment<byte> bytes = new ArraySegment<byte>(command.ToByteArray());
            try {
                await client.SendAsync(bytes, WebSocketMessageType.Binary, true, default);
            } catch (InvalidOperationException e) {
                OnClose?.Invoke(-2, e.Message);
                _ = Close();
            }
        }

        protected async Task StartReceive() {
            byte[] buffer = new byte[RECV_BUFFER_SIZE];
            try {
                while (client.State == WebSocketState.Open) {
                    byte[] data = new byte[0];
                    WebSocketReceiveResult result;
                    do {
                        result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close) {
                            OnClose?.Invoke((int)result.CloseStatus, result.CloseStatusDescription);
                            return;
                        }
                        data = await MergeData(data, buffer, result.Count);
                    } while (!result.EndOfMessage);
                    try {
                        Command command = Command.Parser.ParseFrom(data);
                        CommandType cmd = command.Cmd;
                        OpType op = command.Op;
                        Body body = Body.Parser.ParseFrom(command.Body);
                        Logger.Debug("{0} <= {1}/{2}: {3}", userId, cmd, op, body);
                        HandleCommand(cmd, op, body);
                    } catch (Exception e) {
                        Logger.Error(e.Message);
                        throw e;
                    }
                }
            } catch (Exception e) {
                OnClose?.Invoke(-1, e.Message);
            }
        }

        static async Task<byte[]> MergeData(byte[] oldData, byte[] newData, int newDataLength) {
            return await Task.Run(() => {
                var data = new byte[oldData.Length + newDataLength];
                Array.Copy(oldData, data, oldData.Length);
                Array.Copy(newData, 0, data, oldData.Length, newDataLength);
                return data;
            });
        }




        public Connection() {
            responses = new Dictionary<int, TaskCompletionSource<ResponseWrapper>>();
        }

        protected Task OpenSession(string appId, string userId, string gameVersion) {
            var request = NewRequest();
            request.SessionOpen = new SessionOpenRequest {
                AppId = appId,
                PeerId = userId,
                SdkVersion = Config.SDKVersion,
                GameVersion = gameVersion
            };
            return SendRequest(CommandType.Session, OpType.Open, request);
        }

        protected Task<Message> Send(Message msg) {
            return Task.FromResult<Message>(null);
        }

        void HandleCommand(CommandType cmd, OpType op, Body body) {
            if (body.Response != null) {
                var res = body.Response;
                if (responses.TryGetValue(res.I, out var tcs)) {
                    if (res.ErrorInfo != null) {
                        var errorInfo = res.ErrorInfo;
                        tcs.SetException(new PlayException(errorInfo.ReasonCode, errorInfo.Detail));
                    } else {
                        tcs.SetResult(new ResponseWrapper { 
                            Cmd = cmd,
                            Op = op,
                            Response = res
                        });
                    }
                }
            } else {
                HandleNotification(cmd, op, body);
            }
        }

        protected abstract string GetFastOpenUrl(string server, string appId, string gameVersion, string userId, string sessionToken);

        protected abstract int GetPingDuration();

        protected abstract void HandleNotification(CommandType cmd, OpType op, Body body);

        static volatile int requestI = 1;
        static readonly object requestILock = new object();

        static int RequestI {
            get {
                lock (requestILock) {
                    return requestI++;
                }
            }
        }

        protected static RequestMessage NewRequest() {
            var request = new RequestMessage {
                I = RequestI
            };
            return request;
        }

        protected void HandleErrorMsg(Body body) {
            Logger.Error("error msg: {0}", body);
            var errorInfo = body.Error.ErrorInfo;
            OnError?.Invoke(errorInfo.ReasonCode, errorInfo.Detail);
        }

        protected void HandleUnknownMsg(CommandType cmd, OpType op, Body body) {
            try {
                Logger.Error("unknown msg: {0}/{1} {2}", cmd, op, body);
            } catch (Exception e) {
                Logger.Error(e.Message);
            }
        }
    }
}
