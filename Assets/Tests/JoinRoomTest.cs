using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;

namespace LeanCloud.Play.Test
{
    public class JoinRoomTest
    {
        [UnityTest]
        public IEnumerator JoinRoomByName() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var roomName = "jrt0_r";
            var c0 = Utils.NewClient("jrt0_0");
            var c1 = Utils.NewClient("jrt0_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                var room = _.Result;
                Assert.AreEqual(room.Name, roomName);
                c0.Close();
                c1.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator JoinRandomRoom() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var c0 = Utils.NewClient("jrt1_0");
            var c1 = Utils.NewClient("jrt1_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom();
            }).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c1.JoinRandomRoom();
            }).Unwrap().OnSuccess(_ => {
                var room = _.Result;
                Debug.Log($"join random: {room.Name}");
                c0.Close();
                c1.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator JoinWithExpectedUserIds() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var roomName = "jrt2_r";
            var c0 = Utils.NewClient("jrt2_0");
            var c1 = Utils.NewClient("jrt2_1");
            var c2 = Utils.NewClient("jrt2_2");
            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    MaxPlayerCount = 2
                };
                return c0.CreateRoom(roomName, roomOptions, new List<string> { "jrt2_2" });
            }).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }).Unwrap().ContinueWith(_ => {
                Assert.AreEqual(_.IsFaulted, true);
                var e = _.Exception.InnerException as PlayException;
                Assert.AreEqual(e.Code, 4302);
                return c2.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c2.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c0.Close();
                c1.Close();
                c2.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator LeaveRoom() {
            Logger.LogDelegate += Utils.Log;

            var f0 = false;
            var f1 = false;
            var roomName = "jrt3_r";
            var c0 = Utils.NewClient("jrt3_0");
            var c1 = Utils.NewClient("jrt3_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c0.OnPlayerRoomLeft += leftPlayer => {
                    f0 = true;
                };
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                Debug.Log($"{c1.UserId} joined room");
                return c1.LeaveRoom();
            }).Unwrap().OnSuccess(_ => {
                f1 = true;
            });

            while (!f0 || !f1) {
                yield return null;
            }
            c0.Close();
            c1.Close();
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator RejoinRoom() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var roomName = $"jrt4_r_{Random.Range(0, 1000000)}";
            var c0 = Utils.NewClient("jrt4_0");
            var c1 = Utils.NewClient("jrt4_1");

            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    PlayerTtl = 600
                };
                return c0.CreateRoom(roomName, roomOptions);
            }).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c1.OnDisconnected += () => {
                    Debug.Log("------------- disconnected");
                    c1.Connect().OnSuccess(__ => {
                        return c1.RejoinRoom(roomName);
                    }).Unwrap().OnSuccess(__ => {
                        f = true;
                    });
                };
                c1._Disconnect();
            });

            while (!f) {
                yield return null;
            }
            c0.Close();
            c1.Close();
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator ReconnectAndRejoin() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var roomName = "jrt5_r";
            var c0 = Utils.NewClient("jrt5_0");
            var c1 = Utils.NewClient("jrt5_1");

            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    PlayerTtl = 600
                };
                return c0.CreateRoom(roomName, roomOptions);
            }).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c1.OnDisconnected += () => {
                    c1.ReconnectAndRejoin().OnSuccess(__ => {
                        f = true;
                    });
                };
                c1._Disconnect();
            });

            while (!f) {
                yield return null;
            }
            c0.Close();
            c1.Close();
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator JoinRoomFailed() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var roomName = "jrt6_r";
            var c = Utils.NewClient("jrt6");

            c.Connect().OnSuccess(_ => {
                return c.JoinRoom(roomName);
            }).Unwrap().ContinueWith(_ => {
                Assert.AreEqual(_.IsFaulted, true);
                var e = _.Exception.InnerException as PlayException;
                Assert.AreEqual(e.Code, 4301);
                Debug.Log(e.Detail);
                c.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator JoinRandomWithMatchProperties() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var roomName = "jrt7_r";
            var c0 = Utils.NewClient("jrt7_0");
            var c1 = Utils.NewClient("jrt7_1");
            var c2 = Utils.NewClient("jrt7_2");
            var c3 = Utils.NewClient("jrt7_3");
            var c4 = Utils.NewClient("jrt7_2");

            var props = new PlayObject {
                    { "lv", 2 }
                };
            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    MaxPlayerCount = 3,
                    CustomRoomProperties = props,
                    CustoRoomPropertyKeysForLobby = new List<string> { "lv" }
                };
                return c0.CreateRoom(roomName, roomOptions);
            }).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c1.JoinRandomRoom(props, new List<string> { "jrt7_2" });
            }).Unwrap().OnSuccess(_ => {
                return c2.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c2.JoinRandomRoom(new PlayObject {
                    { "lv", 3 }
                });
            }).Unwrap().ContinueWith(t => {
                PlayException e = (PlayException)t.Exception.InnerException;
                Assert.AreEqual(e.Code, 4301);
                c2.Close();
                return c3.Connect();
            }).Unwrap().OnSuccess(t => {
                return c3.JoinRandomRoom(props);
            }).Unwrap().ContinueWith(t => {
                PlayException e = (PlayException)t.Exception.InnerException;
                Assert.AreEqual(e.Code, 4301);
                c3.Close();
                return c4.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c4.JoinRandomRoom(props);
            }).Unwrap().OnSuccess(_ => {
                c0.Close();
                c1.Close();
                c4.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator MatchRandom() {
            Logger.LogDelegate += Utils.Log;

            var f = false;

            var roomName = "jr8_r";
            var c0 = Utils.NewClient("jr8_0");
            var c1 = Utils.NewClient("jr8_1");
            var c2 = Utils.NewClient("jr8_2");
            var c3 = Utils.NewClient("jr8_xxx");

            var props = new PlayObject {
                    { "lv", 5 }
                };
            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    MaxPlayerCount = 3,
                    CustomRoomProperties = props,
                    CustoRoomPropertyKeysForLobby = new List<string> { "lv" }
                };
                return c0.CreateRoom(roomName, roomOptions);
            }).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                Debug.Log("c1 connected");
                return c1.MatchRandom("jr8_1", new PlayObject {
                    { "lv", 5 }
                }, new List<string> { "jr8_xxx" });
            }).Unwrap().OnSuccess(t => {
                var lobbyRoom = t.Result;
                Assert.AreEqual(lobbyRoom.RoomName, roomName);
                return c1.JoinRoom(lobbyRoom.RoomName);
            }).Unwrap().OnSuccess(_ => {
                return c2.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c2.JoinRandomRoom(props);
            }).Unwrap().ContinueWith(t => {
                PlayException e = (PlayException)t.Exception.InnerException;
                Assert.AreEqual(e.Code, 4301);
                c2.Close();
                return c3.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c3.JoinRandomRoom(props);
            }).Unwrap().OnSuccess(_ => {
                c0.Close();
                c1.Close();
                c3.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator JoinWithExpectedUserIdsFixBug() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var roomName = "jr9_r0";
            var c0 = Utils.NewClient("jr9_0");
            var c1 = Utils.NewClient("jr9_1");
            var c2 = Utils.NewClient("jr9_2");
            var c3 = Utils.NewClient("jr9_3");

            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    MaxPlayerCount = 4
                };
                return c0.CreateRoom(roomName, roomOptions, new List<string> { "jr9_1" });
            }).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                return c2.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c2.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => { 
                return c3.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c3.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c0.Close();
                c1.Close();
                c2.Close();
                c3.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
            Logger.LogDelegate -= Utils.Log;
        }
    }
}
