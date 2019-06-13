using System;
using System.Threading.Tasks;
using System.Threading;
using WebSocketSharp;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;
using Google.Protobuf;

namespace LeanCloud.Play {
    internal abstract class Connection {
        static readonly string PING = "{}";

        protected WebSocket ws;
        readonly Dictionary<int, TaskCompletionSource<Message>> requests;
        readonly Dictionary<int, TaskCompletionSource<ResponseMessage>> responses;

        internal event Action<Message> OnMessage;
        internal event Action<int, string> OnClose;

        readonly Queue<Message> messageQueue;
        bool isMessageQueueRunning;

        CancellationTokenSource pingTokenSource;
        CancellationTokenSource pongTokenSource;

        string userId;

        internal Connection() {
            requests = new Dictionary<int, TaskCompletionSource<Message>>();
            responses = new Dictionary<int, TaskCompletionSource<ResponseMessage>>();
            messageQueue = new Queue<Message>();
            pingTokenSource = new CancellationTokenSource();
            pongTokenSource = new CancellationTokenSource();
        }

        protected Task Connect(string server, string userId) {
            this.userId = userId;
            Logger.Debug("connect at {0}", Thread.CurrentThread.ManagedThreadId);
            var tcs = new TaskCompletionSource<bool>();
            ws = new WebSocket(server, "protobuf.1");
            void onOpen(object sender, EventArgs args) {
                Logger.Debug("wss on open at {0}", Thread.CurrentThread.ManagedThreadId);
                Connected();
                ws.OnOpen -= onOpen;
                ws.OnClose -= onClose;
                ws.OnError -= onError;
                tcs.SetResult(true);
            }
            void onClose(object sender, CloseEventArgs args) {
                Logger.Debug("wss on close at {0}", Thread.CurrentThread.ManagedThreadId);
                ws.OnOpen -= onOpen;
                ws.OnClose -= onClose;
                ws.OnError -= onError;
                tcs.SetException(new Exception());
            }
            void onError(object sender, ErrorEventArgs args) {
                Logger.Debug("wss on error at {0}", Thread.CurrentThread.ManagedThreadId);
                ws.OnOpen -= onOpen;
                ws.OnClose -= onClose;
                ws.OnError -= onError;
                tcs.SetException(new Exception());
            }
            ws.OnOpen += onOpen;
            ws.OnClose += onClose;
            ws.OnError += onError;
            ws.Connect();
            ws.ConnectAsync();
            return tcs.Task;
        }

        void Connected() {
            isMessageQueueRunning = true;
            ws.OnMessage += OnWebSocketMessage;
            ws.OnClose += OnWebSocketClose;
            ws.OnError += OnWebSocketError;
            Ping();
        }

        protected Task OpenSession(string appId, string userId, string gameVersion) {
            var request = NewRequest();
            request.SessionOpen = new SessionOpenRequest {
                AppId = appId,
                PeerId = userId,
                SdkVersion = Config.PlayVersion,
                GameVersion = gameVersion
            };
            return Send(CommandType.Session, OpType.Open, request);
        }

        protected Task<ResponseMessage> Send(CommandType cmd, OpType op, RequestMessage request) {
            var tcs = new TaskCompletionSource<ResponseMessage>();
            responses.Add(request.I, tcs);
            Send(cmd, op, new Body {
                Request = request
            });
            return tcs.Task;
        }

        protected void SendDirectCommand(DirectCommand directCommand) {
            Send(CommandType.Direct, OpType.None, new Body {
                Direct = directCommand
            });
            Ping();
        }

        protected void Send(CommandType cmd, OpType op, Body body) {
            Logger.Debug("=> {0}/{1}: {2}", cmd, op, body.ToString());
            var command = new Command { 
                Cmd = cmd,
                Op = op,
                Body = body.ToByteString()
            };
            ws.Send(command.ToByteArray());
            Ping();
        }

        protected Task<Message> Send(Message msg) {
            return Task.FromResult<Message>(null);
        }

        void Send(string msg) {
            Logger.Debug("=> {0} at {1}", msg, Thread.CurrentThread.ManagedThreadId);
            ws.Send(msg);
            Ping();
        }

        internal void Close() {
            StopKeepAlive();
            ws.OnMessage -= OnWebSocketMessage;
            ws.OnClose -= OnWebSocketClose;
            ws.OnError -= OnWebSocketError;
            ws.CloseAsync();
        }

        internal void Disconnect() {
            ws.CloseAsync();
        }

        // Websocket 事件
        void OnWebSocketMessage(object sender, MessageEventArgs eventArgs) {
            Pong();
            if (PING.Equals(eventArgs.Data)) {
                Logger.Debug("<= {}");
                return;
            }
            var command = Command.Parser.ParseFrom(eventArgs.RawData);
            var cmd = command.Cmd;
            var op = command.Op;
            var body = Body.Parser.ParseFrom(command.Body);
            Logger.Debug("<= {0}/{1}: {2}", cmd, op, body);
            HandleCommand(cmd, op, body);


            //var message = Message.FromJson(eventArgs.Data);
            //if (isMessageQueueRunning) {
            //    HandleMessage(message);
            //} else {
            //    Logger.Debug($"delay: {message.ToJson()}");
            //    lock (messageQueue) {
            //        messageQueue.Enqueue(message);
            //    }
            //}
        }

        void HandleCommand(CommandType cmd, OpType op, Body body) {
            if (body.Response != null) {
                var res = body.Response;
                if (responses.TryGetValue(res.I, out var tcs)) {
                    if (res.ErrorInfo != null) {
                        var errorInfo = res.ErrorInfo;
                        tcs.SetException(new PlayException(errorInfo.ReasonCode, errorInfo.Detail));
                    } else {
                        tcs.SetResult(res);
                    }
                }
            } else if (body.Direct != null) { 
            
            } else if (body.RoomNotification != null) { 
            
            } else if (body.Events != null) { 
            
            } else if (body.Statistic != null) { 
            
            } else if (body.RoomList != null) { 
            
            } else { 
                // NOT support

            }
        }

        void HandleResponse(CommandType command, OpType op, ResponseMessage response) {
            Logger.Debug("<= {0}/{1}: {2}", command, op, response);

        }

        void HandleMessage(Message message) {
            Logger.Debug($"handle: {message.ToJson()}");
            if (message.HasI) {
                TaskCompletionSource<Message> tcs = null;
                lock (requests) {
                    if (!requests.TryGetValue(message.I, out tcs)) {
                        Logger.Error("no requests for {0}", message.I);
                    }
                }
                if (message.IsError) {
                    tcs.SetException(new PlayException(message.ReasonCode, message.Detail));
                } else {
                    tcs.SetResult(message);
                }
            } else {
                // 推送消息
                OnMessage?.Invoke(message);
            }
        }

        void OnWebSocketClose(object sender, CloseEventArgs eventArgs) {
            StopKeepAlive();
            OnClose?.Invoke(eventArgs.Code, eventArgs.Reason);
        }

        void OnWebSocketError(object sender, ErrorEventArgs e) {
            Logger.Error(e.Message);
            ws.CloseAsync();
        }

        internal void PauseMessageQueue() {
            isMessageQueueRunning = false;
        }

        internal void ResumeMessageQueue() {
            if (messageQueue.Count > 0) {
                lock (messageQueue) { 
                    while (messageQueue.Count > 0) {
                        var msg = messageQueue.Dequeue();
                        HandleMessage(msg);
                    }
                }
            }
            isMessageQueueRunning = true;
        }

       void Ping() {
            lock (pingTokenSource) {
                if (pingTokenSource != null) {
                    pingTokenSource.Cancel();
                }
                pingTokenSource = new CancellationTokenSource();
                Task.Delay(TimeSpan.FromSeconds(GetPingDuration())).ContinueWith(t => {
                    Logger.Debug("------------- {0} ping", userId);
                    ws.Send(PING);
                    Ping();
                }, pingTokenSource.Token);
            }
        }

        void Pong() { 
            lock (pongTokenSource) { 
                if (pongTokenSource != null) {
                    pongTokenSource.Cancel();
                }
                Task.Delay(TimeSpan.FromSeconds(GetPingDuration() * 3)).ContinueWith(t => {
                    Logger.Debug("It's time for closing ws.");
                    lock (ws) {
                        try {
                            ws.Close();
                        } catch (Exception e) {
                            Logger.Error(e.Message);
                        }
                    }
                }, pongTokenSource.Token);
            }
        }

        void StopKeepAlive() { 
            if (pingTokenSource != null) {
                pingTokenSource.Cancel();
            }
            if (pongTokenSource != null) {
                pongTokenSource.Cancel();
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
