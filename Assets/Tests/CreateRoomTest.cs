using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Play.Test
{
    public class CreateRoomTest {
        [SetUp]
        public void SetUp() {
            Debug.Log("Set up");
            Logger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            Debug.Log("Tear down");
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        [Order(0)]
        public IEnumerator CreateNullNameRoom() {
            var f = false;
            var c = Utils.NewClient("crt0");
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom();
            }).Unwrap().OnSuccess(_ => {
                var room = _.Result;
                Debug.Log(room.Name);
                c.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        [Order(1)]
        public IEnumerator CreateSimpleRoom() {
            var f = false;
            var roomName = "crt1_r";
            var c = Utils.NewClient("crt1");
            c.Connect().OnSuccess(_ => {
                Debug.Log("connected");
                return c.CreateRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                var room = _.Result;
                Assert.AreEqual(room.Name, roomName);
                c.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        [Order(2)]
        public IEnumerator CreateCustomRoom() {
            var f = false;
            var roomName = $"crt2_r_{Random.Range(0, 1000000)}";
            var roomTitle = "LeanCloud Room";
            var c = Utils.NewClient(roomName);
            c.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    Visible = false,
                    EmptyRoomTtl = 60,
                    MaxPlayerCount = 2,
                    PlayerTtl = 60,
                    CustomRoomProperties = new PlayObject {
                        { "title", roomTitle },
                        { "level", 2 },
                    },
                    CustoRoomPropertyKeysForLobby = new List<string> { "level" }
                };
                var expectedUserIds = new List<string> { "world" };
                return c.JoinOrCreateRoom(roomName, roomOptions, expectedUserIds);
            }).Unwrap().OnSuccess(_ => {
                var room = _.Result;
                Assert.AreEqual(room.Name, roomName);
                var props = room.CustomProperties;
                Debug.Log($"title: {props["title"]}");
                Debug.Log($"level: {props["level"]}");
                Assert.AreEqual(props["title"], roomTitle);
                Assert.AreEqual(props["level"], 2);
                c.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        [Order(3)]
        public IEnumerator MasterAndLocal() {
            var flag = false;
            var roomName = "crt3_r";
            var c0 = Utils.NewClient("crt3_0");
            var c1 = Utils.NewClient("crt3_1");
            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c0.OnPlayerRoomJoined += (newPlayer) => {
                    Assert.AreEqual(c0.Player.IsMaster, true);
                    Assert.AreEqual(c0.Player.IsLocal, true);
                    Debug.Log($"new player joined at {Thread.CurrentThread.ManagedThreadId}");
                    flag = true;
                };
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                Assert.AreEqual(c1.Player.IsLocal, true);
                c0.Close();
                c1.Close();
            });

            while (!flag) {
                yield return null;
            }
        }

        [UnityTest]
        [Order(4)]
        public IEnumerator CreateRoomFailed() {
            var f = false;
            var roomName = "crt5_ r";
            var c = Utils.NewClient("crt5");
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom(roomName);
            }).Unwrap().ContinueWith(_ => {
                Assert.AreEqual(_.IsFaulted, true);
                var e = _.Exception.InnerException as PlayException;
                Assert.AreEqual(e.Code, 4316);
                c.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
        }
    }
}
