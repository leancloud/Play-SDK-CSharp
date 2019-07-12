using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;
using System;

namespace LeanCloud.Play.Test
{
    public class ChangeProperties
    {
        [UnityTest]
        public IEnumerator ChangeRoomProperties() {
            Logger.LogDelegate += Utils.Log;

            var roomName = "cp0_r";
            var f0 = false;
            var f1 = false;
            var c0 = Utils.NewClient("cp0_0");
            var c1 = Utils.NewClient("cp0_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c0.OnRoomCustomPropertiesChanged += changedProps => {
                    var props = c0.Room.CustomProperties;
                    Assert.AreEqual(props.GetString("name"), "leancloud");
                    Assert.AreEqual(props.GetInt("gold"), 1000);
                    f0 = true;
                };
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c1.OnRoomCustomPropertiesChanged += changedProps => {
                    var props = c1.Room.CustomProperties;
                    Assert.AreEqual(props.GetString("name"), "leancloud");
                    Assert.AreEqual(props.GetInt("gold"), 1000);
                    f1 = true;
                };
                var newProps = new PlayObject {
                    { "name", "leancloud" },
                    { "gold", 1000 },
                };
                return c0.SetRoomCustomProperties(newProps);
            });

            while (!f0 || !f1) {
                yield return null;
            }
            c0.Close();
            c1.Close();
            Logger.LogDelegate -= Utils.Log;
        }

        [Test]
        public async void ChangeRoomPropertiesWithCAS() {
            Logger.LogDelegate += Utils.Log;

            Debug.Log("hello");
            var roomName = "cp1_r";
            var c = Utils.NewClient("cp1");
            await c.Connect();
            var options = new RoomOptions { 
                CustomRoomProperties = new PlayObject {
                    { "id", 1 },
                    { "gold", 100 }
                }
            };
            var room = await c.CreateRoom(roomName, options);

            var newProps = new PlayObject {
                    { "gold", 200 },
                };
            var expectedValues = new PlayObject {
                    { "id", 2 }
                };

            await c.SetRoomCustomProperties(newProps, expectedValues);
            Assert.AreEqual(c.Room.CustomProperties["gold"], 100);

            c.Close();
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator ChangePlayerProperties() {
            Logger.LogDelegate += Utils.Log;

            var f0 = false;
            var f1 = false;
            var roomName = "cp2_r";
            var c0 = Utils.NewClient("cp2_0");
            var c1 = Utils.NewClient("cp2_1");
            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c0.OnPlayerCustomPropertiesChanged += (player, changedProps) => {
                    var props = player.CustomProperties;
                    Assert.AreEqual(props.GetString("nickname"), "LeanCloud");
                    Assert.AreEqual(props.GetInt("gold"), 100);
                    var attr = props["attr"] as PlayObject;
                    Assert.AreEqual(attr.GetInt("hp"), 10);
                    Assert.AreEqual(attr.GetInt("mp"), 20);
                    Debug.Log("c0 check done");
                    f0 = true;
                };
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c1.OnPlayerCustomPropertiesChanged += (player, changedProps) => {
                    var p = player.CustomProperties;
                    Assert.AreEqual(p.GetString("nickname"), "LeanCloud");
                    Assert.AreEqual(p.GetInt("gold"), 100);
                    var attr = p["attr"] as PlayObject;
                    Assert.AreEqual(attr.GetInt("hp"), 10);
                    Assert.AreEqual(attr.GetInt("mp"), 20);
                    Debug.Log("c1 check done");
                    f1 = true;
                };
                var props = new PlayObject {
                    { "nickname", "LeanCloud" },
                    { "gold", 100 },
                    { "attr", new PlayObject {
                            { "hp", 10 },
                            { "mp", 20 }
                        } 
                    }
                };
                return c1.Player.SetCustomProperties(props);
                //return c0.SetPlayerCustomProperties(c1.Player.ActorId, props);
            });

            while (!f0 || !f1) {
                yield return null;
            }
            c0.Close();
            c1.Close();
            Logger.LogDelegate -= Utils.Log;
        }

        [Test]
        public async void ChangePlayerPropertiesWithCAS() {
            Logger.LogDelegate += Utils.Log;

            var roomName = "cp3_r";
            var c = Utils.NewClient("cp3");

            await c.Connect();
            await c.CreateRoom(roomName);
            var props = new PlayObject {
                { "id", 1 },
                { "nickname", "lean" }
            };
            await c.Player.SetCustomProperties(props);

            var newProps = new PlayObject {
                { "nickname", "cloud" }
            };
            var expectedValues = new PlayObject {
                { "id", 2 }
            };
            await c.Player.SetCustomProperties(newProps, expectedValues);
            Assert.AreEqual(c.Player.CustomProperties["nickname"], "lean");
            c.Close();

            Logger.LogDelegate -= Utils.Log;
        }

        [Test]
        public async void GetPlayerPropertiesWhenJoinRoom() {
            var roomName = "cp4_r";
            var c0 = Utils.NewClient("cp4_0");
            var c1 = Utils.NewClient("cp4_1");

            await c0.Connect();
            await c0.CreateRoom(roomName);
            var props = new PlayObject {
                { "ready", true }
            };
            await c0.Player.SetCustomProperties(props);

            await c1.Connect();
            await c1.JoinRoom(roomName);
            var master = c1.Room.Master;

            Assert.AreEqual(bool.Parse(master.CustomProperties["ready"].ToString()), true);

            c0.Close();
            c1.Close();
        }

        [Test]
        public async void ChangePropertiesWithSameValue() {
            Logger.LogDelegate += Utils.Log;

            var roomName = "cp5_r";
            var c = Utils.NewClient("cp5");

            await c.Connect();
            await c.CreateRoom(roomName);
            var props = new PlayObject {
                { "ready", true }
            };
            await c.Room.SetCustomProperties(props);
            await c.Room.SetCustomProperties(props);
            await c.Player.SetCustomProperties(props);
            await c.Player.SetCustomProperties(props);
            c.Close();
            Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator SetNullProperty() {
            Logger.LogDelegate += Utils.Log;

            var f0 = false;
            var f1 = false;
            var roomName = "cp6_r";
            var c = Utils.NewClient("cp6");

            c.Connect().OnSuccess(_ => {
                return c.CreateRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c.OnRoomCustomPropertiesChanged += (__) => { 
                    if (c.Room.CustomProperties.GetString("name") == "leancloud") {
                        f0 = true;
                        Debug.Log("============== f0 is true");
                    }
                    if (c.Room.CustomProperties.IsNull("name")) {
                        f1 = true;
                        Debug.Log("============== f1 is true");
                    }
                };
                var props = new PlayObject {
                    { "name", "leancloud" }
                };
                return c.Room.SetCustomProperties(props);
            }).Unwrap().OnSuccess(_ => {
                var props = new PlayObject {
                    { "name", null }
                };
                return c.Room.SetCustomProperties(props);
            });

            while (!f0 || !f1) {
                yield return null;
            }

            c.Close();

            Logger.LogDelegate -= Utils.Log;
        }
    }
}