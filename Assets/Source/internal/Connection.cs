using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;
using Google.Protobuf;

namespace LeanCloud.Play {
    internal abstract class Connection {
        const int RECV_BUFFER_SIZE = 1024;
        static readonly string PING = "{}";

        protected WebSocket ws;
        protected ClientWebSocket client;
        readonly Dictionary<int, TaskCompletionSource<ResponseWrapper>> responses;

        internal event Action<CommandType, OpType, Body> OnMessage;
        internal event Action<int, string> OnClose;
        
        string userId;


        public bool IsOpen {
            get {
                return client != null && client.State == WebSocketState.Open;
            }
        }

        public async Task Close() {
            if (IsOpen) {
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "1", CancellationToken.None);
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

        async Task StartReceive() {
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
                        var command = Command.Parser.ParseFrom(data);
                        var cmd = command.Cmd;
                        var op = command.Op;
                        var body = Body.Parser.ParseFrom(command.Body);
                        Logger.Debug("{0} <= {1}/{2}: {3}", userId, cmd, op, body);
                        HandleCommand(cmd, op, body);
                    } catch (Exception e) {

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




        internal Connection() {
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
                OnMessage?.Invoke(cmd, op, body);
            }
        }

        protected abstract int GetPingDuration();

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
    }
}
