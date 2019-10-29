using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;
using System.Threading;

namespace LeanCloud.Play.Test
{
    public class ConnectTest
    {
        [SetUp]
        public void SetUp() {
            Logger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator FastOpen() {
            bool f = false;

            GameConnection gameConn = new GameConnection();
            string appId = "FQr8l8LLvdxIwhMHN77sNluX-9Nh9j0Va";
            string server = "wss://cn-e1-cell2.leancloud.cn:5769/";
            string gameVersion = "0.0.1";
            string userId = "lean";
            string sessionToken = "be5090bd3d471ecb41ac71bcede88a2d";
            gameConn.Connect(appId, server, gameVersion, userId, sessionToken).ContinueWith(t => {
                if (t.IsFaulted) {
                    Debug.Log($"failed: {t.Exception.InnerException.Message}");
                } else {
                    Debug.Log("success");
                    f = true;
                }
            });

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Connect() {
            var f = false;
            var c = Utils.NewClient("ct0");
            c.Connect().OnSuccess(_ => {
                Debug.Log($"{c.UserId} connected.");
                c.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ConnectWithSameId() {
            Logger.LogDelegate += Utils.Log;

            var f0 = false;
            var f1 = false;
            var c0 = Utils.NewClient("ct1");
            var c1 = Utils.NewClient("ct1");
            c0.OnError += (code, detail) => {
                Debug.Log($"on error at {Thread.CurrentThread.ManagedThreadId}");
                Assert.AreEqual(code, 4102);
                Debug.Log(detail);
            };
            c0.OnDisconnected += () => {
                Debug.Log("c0 is disconnected.");
                f0 = true;
            };
            c0.Connect().OnSuccess(_ => {
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                Debug.Log($"{c1.UserId} connected at {Thread.CurrentThread.ManagedThreadId}");
                c1.Close();
                f1 = true;
            });

            while (!f0 || !f1) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CloseFromLobby() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var c = Utils.NewClient("ct2");
            c.Connect().ContinueWith(_ => {
                Assert.AreEqual(_.IsFaulted, false);
                c.Close();
                c = Utils.NewClient("ct2");
                return c.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CloseFromGame() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var c = Utils.NewClient("ct3");
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom();
            }).ContinueWith(_ => {
                c.Close();
                Assert.AreEqual(_.IsFaulted, false);
                c = Utils.NewClient("ct3");
                return c.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c.CreateRoom();
            }).Unwrap().OnSuccess(_ => {
                c.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ConnectFailed() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var c = Utils.NewClient("ct4 ");
            c.Connect().ContinueWith(_ => { 
                Assert.AreEqual(_.IsFaulted, true);
                var e = _.Exception.InnerException as PlayException;
                Assert.AreEqual(e.Code, 4104);
                f = true;
            });

            while (!f) {
                yield return null;
            }
        }

        [UnityTest, Timeout(40000)]
        public IEnumerator KeepAlive() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var roomName = "ct5_r";
            var c = Utils.NewClient("ct5");

            c.Connect().OnSuccess(_ => {
                return c.CreateRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                Task.Delay(30000).OnSuccess(__ => {
                    Debug.Log("delay 30s done");
                    f = true;
                });
            });

            while (!f) {
                yield return null;
            }
            c.Close();
        }

        [UnityTest, Timeout(40000)]
        public IEnumerator SendOnly() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var c = Utils.NewClient("ct6");
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom();
            }).Unwrap().OnSuccess(_ => {
                Task.Run(async () => {
                    var count = 6;
                    while (count > 0 && !f) {
                        var options = new SendEventOptions { 
                            ReceiverGroup = ReceiverGroup.Others
                        };
                        await c.SendEvent(5, null, options);
                        Thread.Sleep(5000);
                    }
                });
                Task.Delay(30000).OnSuccess(__ => {
                    Debug.Log("delay 30s done");
                    f = true;
                });
            });

            while (!f) {
                yield return null;
            }
            c.Close();
        }

        [UnityTest]
        public IEnumerator ConnectRepeatedly() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var c = Utils.NewClient("ct7");

            c.Connect().OnSuccess(_ => {
                f = true;
            });
            c.Connect().ContinueWith(t => { 
                
            });

            while (!f) {
                yield return null;
            }
            _ = c.Close();
        }
    }
}
