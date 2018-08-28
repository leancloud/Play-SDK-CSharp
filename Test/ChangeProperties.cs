using NUnit.Framework;
using System;
using System.Threading;
using System.Collections.Generic;

using LeanCloud.Play;

namespace Test
{
    [TestFixture()]
    public class ChangeProperties
    {
        [Test()]
        public void TestChangeRoomProperties()
        {
            ManualResetEvent resetEvent = new ManualResetEvent(false);

            var roomName = "cp1_room";
            var b1 = Utility.NewBehavior("cp1_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("cp1_2");
            var p2 = b2.Play;
            var f1 = false;
            var f2 = false;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                p1.CreateRoom(roomName);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });
            p1.On(Event.ROOM_CUSTOM_PROPERTIES_CHANGED, (evtData) =>
            {
                var props = p1.Room.CustomProperties;
                var title = props["title"] as string;
                var gold = (long)props["gold"];
                var pokerList = props["pokers"] as List<object>;
                var poker = pokerList[2] as Dictionary<string, object>;
                Console.WriteLine("{0} : {1}, {2}, {3}", p1.UserId, title, gold, poker["number"]);
                f1 = true;
                if (f1 && f2) {
                    b1.Stop();
                    b2.Stop();
                    resetEvent.Set();
                }
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRoom(roomName);
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                var props = new Dictionary<string, object>();
                props.Add("title", "room_cp");
                props.Add("gold", 1000);
                var pokerList = new List<object>();
                for (int i = 0; i < 3; i++)
                {
                    var poker = new Dictionary<string, object>();
                    poker.Add("number", i);
                    pokerList.Add(poker);
                }
                props.Add("pokers", pokerList);
                p2.Room.SetCustomProperties(props);
            });
            p2.On(Event.ROOM_CUSTOM_PROPERTIES_CHANGED, (evtData) =>
            {
                var props = p2.Room.CustomProperties;
                var title = props["title"] as string;
                var gold = (long)props["gold"];
                var pokerList = props["pokers"] as List<object>;
                var poker = pokerList[2] as Dictionary<string, object>;
                Console.WriteLine("{0} : {1}, {2}, {3}", p2.UserId, title, gold, poker["number"]);
                f2 = true;
                if (f1 && f2)
                {
                    b1.Stop();
                    b2.Stop();
                    resetEvent.Set();
                }
            });

            p1.Connect();

            resetEvent.WaitOne();
        }

        [Test()]
        public void TestChangeRoomPropertiesWithCAS() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);

            var roomName = "cp2_room";
            var b1 = Utility.NewBehavior("cp2_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("cp2_2");
            var p2 = b2.Play;
            var f1 = false;
            var f2 = false;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                var props = new Dictionary<string, object>();
                props.Add("title", "cp_room");
                props.Add("gold", 200);
                var opts = new RoomOptions()
                {
                    CustomRoomProperties = props
                };
                p1.CreateRoom(roomName, opts);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });
            p1.On(Event.ROOM_CUSTOM_PROPERTIES_CHANGED, (evtData) =>
            {
                var props = p1.Room.CustomProperties;
                var title = props["title"] as string;
                var gold = (long)props["gold"];
                Console.WriteLine("{0} current thread: {1}", p1.UserId, Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine("{0} : {1}, {2}", p1.UserId, title, gold);
                Assert.AreEqual(gold, 2000);
                f1 = true;
                if (f1 && f2)
                {
                    b1.Stop();
                    b2.Stop();
                    resetEvent.Set();
                }
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRoom(roomName);
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                // 设置带有 CAS 错误的属性
                var props = new Dictionary<string, object>();
                props.Add("title", "room_cp");
                props.Add("gold", 1000);
                var expectedValues = new Dictionary<string, object>();
                expectedValues.Add("gold", 100);
                p2.Room.SetCustomProperties(props, expectedValues);

                // 设置带有 CAS 正确的属性
                props = new Dictionary<string, object>();
                props.Add("gold", 2000);
                expectedValues = new Dictionary<string, object>();
                expectedValues.Add("gold", 200);
                p2.Room.SetCustomProperties(props, expectedValues);
            });
            p2.On(Event.ROOM_CUSTOM_PROPERTIES_CHANGED, (evtData) =>
            {
                var props = p2.Room.CustomProperties;
                var title = props["title"] as string;
                var gold = (long)props["gold"];
                Console.WriteLine("{0} current thread: {1}", p2.UserId, Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine("{0} : {1}, {2}", p2.UserId, title, gold);
                Assert.AreEqual(gold, 2000);
                f2 = true;
                if (f1 && f2)
                {
                    b1.Stop();
                    b2.Stop();
                    resetEvent.Set();
                }
            });

            p1.Connect();

            resetEvent.WaitOne();
        }

        [Test()]
        public void TestChangePlayerProperties() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);

            var roomName = "cp3_room";
            var b1 = Utility.NewBehavior("cp3_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("cp3_2");
            var p2 = b2.Play;
            var f1 = false;
            var f2 = false;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                p1.CreateRoom(roomName);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });
            p1.On(Event.PLAYER_CUSTOM_PROPERTIES_CHANGED, (evtData) =>
            {
                var player = evtData["player"] as Player;
                var props = player.CustomProperties;
                var nickname = props["nickname"] as string;
                var gold = (long)props["gold"];
                var pokers = props["pokers"] as List<object>;
                var poker = pokers[2] as Dictionary<string, object>;
                Console.WriteLine("{0} : {1}, {2}, {3}", player.UserId, nickname, gold, poker["number"]);
                f1 = true;
                if (f1 && f2)
                {
                    b1.Stop();
                    b2.Stop();
                    resetEvent.Set();
                }
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRoom(roomName);
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                var props = new Dictionary<string, object>();
                props.Add("nickname", "Li Lei");
                props.Add("gold", 100);
                var pokerList = new List<object>();
                for (int i = 0; i < 3; i++) {
                    var poker = new Dictionary<string, object>();
                    poker.Add("number", i);
                    pokerList.Add(poker);
                }
                props.Add("pokers", pokerList);
                p2.Player.SetCustomProperties(props);
            });
            p2.On(Event.PLAYER_CUSTOM_PROPERTIES_CHANGED, (evtData) =>
            {
                var player = evtData["player"] as Player;
                var props = player.CustomProperties;
                var nickname = props["nickname"] as string;
                var gold = (long)props["gold"];
                var pokers = props["pokers"] as List<object>;
                var poker = pokers[2] as Dictionary<string, object>;
                Console.WriteLine("{0} : {1}, {2}, {3}", player.UserId, nickname, gold, poker["number"]);
                f2 = true;
                if (f1 && f2) {
                    b1.Stop();
                    b2.Stop();
                    resetEvent.Set();
                }
            });

            p1.Connect();

            resetEvent.WaitOne();
        }

        [Test()]
        public void TestChangePlayerPropertiesWithCAS() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);

            var roomName = "cp4_room";
            var b1 = Utility.NewBehavior("cp4_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("cp4_2");
            var p2 = b2.Play;
            var f1 = false;
            var f2 = false;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                p1.CreateRoom(roomName);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });
            p1.On(Event.PLAYER_CUSTOM_PROPERTIES_CHANGED, (evtData) =>
            {
                var player = evtData["player"] as Player;
                var nickname = player.CustomProperties["nickname"] as string;
                Console.WriteLine("{0} : {1}", player.UserId, nickname);
                if (nickname == "Lily") {
                    f1 = true;
                    if (f1 && f2) {
                        b1.Stop();
                        b2.Stop();
                        resetEvent.Set();
                    }
                }
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRoom(roomName);
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                var props = new Dictionary<string, object>();
                props.Add("id", 1);
                props.Add("nickname", "Jim");
                p2.Player.SetCustomProperties(props);

                // 设置错误 CAS
                props = new Dictionary<string, object>();
                props.Add("nickname", "Lucy");
                var expected = new Dictionary<string, object>();
                expected.Add("id", 2);
                p2.Player.SetCustomProperties(props, expected);

                // 设置正确 CAS
                props = new Dictionary<string, object>();
                props.Add("nickname", "Lily");
                expected = new Dictionary<string, object>();
                expected.Add("id", 1);
                p2.Player.SetCustomProperties(props, expected);
            });
            p2.On(Event.PLAYER_CUSTOM_PROPERTIES_CHANGED, (evtData) =>
            {
                var player = evtData["player"] as Player;
                var nickname = player.CustomProperties["nickname"] as string;
                Console.WriteLine("{0} : {1}", player.UserId, nickname);
                if (nickname == "Lily")
                {
                    f2 = true;
                    if (f1 && f2)
                    {
                        b1.Stop();
                        b2.Stop();
                        resetEvent.Set();
                    }
                }
            });

            p1.Connect();

            resetEvent.WaitOne();
        }

        [Test()]
        public void ChangeGetPlayerPropertiesWhenJoinRoom() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);

            var roomName = "cp5_room";
            var b1 = Utility.NewBehavior("cp5_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("cp5_2");
            var p2 = b2.Play;
            p1.On(Event.CONNECTED, (evtData) =>
            {
                p1.CreateRoom(roomName);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                var props = new Dictionary<string, object>();
                props.Add("ready", true);
                p1.Player.SetCustomProperties(props);
            });
            p1.On(Event.PLAYER_CUSTOM_PROPERTIES_CHANGED, (evtData) =>
            {
                p2.Connect();
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRoom(roomName);
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                var master = p2.Room.Master;
                var props = master.CustomProperties;
                bool ready = (bool)props["ready"];
                Assert.AreEqual(ready, true);

                b1.Stop();
                b2.Stop();
                resetEvent.Set();
            });

            p1.Connect();

            resetEvent.WaitOne();
        }
    }
}
