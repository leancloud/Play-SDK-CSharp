using NUnit.Framework;
using System;
using System.Threading;
using System.Collections.Generic;

using LeanCloud.Play;

namespace Test
{
    [TestFixture()]
    public class JoinRoomTest
    {
        [Test()]
        public void TestJoinNameRoom()
        {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName = "jnr1_room";
            var b1 = Utility.NewBehavior("jnr1_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("jnr1_2");
            var p2 = b2.Play;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                p1.CreateRoom(roomName);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRoom(roomName);
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                Console.WriteLine("joined room");
                b1.Stop();
                b2.Stop();
                resetEvent.Set();
            });

            p1.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestJoinRandomRoom() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName = "jnr2_room";
            var b1 = Utility.NewBehavior("jnr2_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("jnr2_2");
            var p2 = b2.Play;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                p1.CreateRoom(roomName);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRandomRoom();
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                Console.WriteLine("joined random room");
                b1.Stop();
                b2.Stop();
                resetEvent.Set();
            });

            p1.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestJoinWithExpectedUserIds() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName = "jnr3_room";
            var b1 = Utility.NewBehavior("jnr3_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("jnr3_2");
            var p2 = b2.Play;
            var b3 = Utility.NewBehavior("jnr3_3");
            var p3 = b3.Play;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                var opts = new RoomOptions() { 
                    MaxPlayerCount = 2
                };
                var expectedUserIds = new List<string>() { 
                    "jnr3_3"
                };
                p1.CreateRoom(roomName, opts, expectedUserIds);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRoom(roomName);
            });
            p2.On(Event.ROOM_JOIN_FAILED, (evtData) =>
            {
                Console.WriteLine("p2 join failed");
                p3.Connect();
            });

            p3.On(Event.CONNECTED, (evtData) =>
            {
                p3.JoinRoom(roomName);
            });
            p3.On(Event.ROOM_JOINED, (evtData) =>
            {
                Console.WriteLine("p3 joined room");
                b1.Stop();
                b2.Stop();
                b3.Stop();
                resetEvent.Set();
            });

            p1.Connect();

            resetEvent.WaitOne();
        }

        [Test()]
        public void TestLeaveRoom() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);

            var roomName = "jnr4_room";
            var b1 = Utility.NewBehavior("jnr4_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("jnr4_2");
            var p2 = b2.Play;
            var flag = false;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                p1.CreateRoom(roomName);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRoom(roomName);
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                p2.LeaveRoom();
            });
            p2.On(Event.ROOM_LEFT, (evtData) =>
            {
                Console.WriteLine("room left");
                if (flag)
                {
                    b1.Stop();
                    b2.Stop();
                    resetEvent.Set();
                }
                else
                {
                    p2.JoinRoom(roomName);
                    flag = true;
                }
            });

            p1.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestRejoinRoom() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName = "jnr5_room";
            var b1 = Utility.NewBehavior("jnr5_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("jnr5_2");
            var p2 = b2.Play;
            var rejoin = false;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                var opts = new RoomOptions() {
                    PlayerTtl = 600,
                };
                p1.CreateRoom(roomName, opts);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });
            p1.On(Event.PLAYER_ACTIVITY_CHANGED, (evtData) => {
                var player = evtData["player"] as Player;
                if (player.IsActive) {
                    Console.WriteLine("{0} : {1}", player.UserId, player.IsActive);
                    b1.Stop();
                    b2.Stop();
                    resetEvent.Set();
                } else {
                    Console.WriteLine("{0} : {1}", player.UserId, player.IsActive);
                }
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                if (rejoin) {
                    p2.RejoinRoom(roomName);
                } else {
                    p2.JoinRoom(roomName);   
                }
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                if (!rejoin) {
                    p2.Disconnect();
                }
            });
            p2.On(Event.DISCONNECTED, (evtData) => {
                if (!rejoin) {
                    p2.Connect();
                    rejoin = true;
                }
            });

            p1.Connect();

            resetEvent.WaitOne();
        }

        [Test()]
        public void TestReconnectAndRejoinRoom() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName = "jnr6_room";
            var b1 = Utility.NewBehavior("jnr6_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("jnr6_2");
            var p2 = b2.Play;
            var rejoin = false;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                var opts = new RoomOptions()
                {
                    PlayerTtl = 600,
                };
                p1.CreateRoom(roomName, opts);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });
            p1.On(Event.PLAYER_ACTIVITY_CHANGED, (evtData) => {
                var player = evtData["player"] as Player;
                if (player.IsActive)
                {
                    Console.WriteLine("{0} : {1}", player.UserId, player.IsActive);
                    b1.Stop();
                    b2.Stop();
                    resetEvent.Set();
                }
                else
                {
                    Console.WriteLine("{0} : {1}", player.UserId, player.IsActive);
                }
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                if (!rejoin)
                {
                    p2.JoinRoom(roomName);
                }
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                if (!rejoin)
                {
                    p2.Disconnect();
                }
            });
            p2.On(Event.DISCONNECTED, (evtData) => {
                if (!rejoin)
                {
                    p2.ReconnectAndRejoin();
                    rejoin = true;
                }
            });

            p1.Connect();

            resetEvent.WaitOne();
        }

        [Test()]
        public void TestJoinNameRoomFailed() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName1 = "jnr7_room";
            var roomName2 = "jnr7_room_";
            var b1 = Utility.NewBehavior("jnr7_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("jnr7_2");
            var p2 = b2.Play;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                p1.CreateRoom(roomName1);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRoom(roomName2);
            });
            p2.On(Event.ROOM_JOIN_FAILED, (evtData) =>
            {
                Console.WriteLine("join room failed");
                b1.Stop();
                b2.Stop();
                resetEvent.Set();
            });

            p1.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestJoinRandomRoomWithMatchProperties() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName = "jnr8_room";
            var b1 = Utility.NewBehavior("jnr8_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("jnr8_2");
            var p2 = b2.Play;
            var b3 = Utility.NewBehavior("jnr8_3");
            var p3 = b3.Play;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                var matchProps = new Dictionary<string, object>();
                matchProps.Add("lv", 2);
                var opts = new RoomOptions()
                {
                    CustomRoomProperties = matchProps,
                    CustoRoomPropertyKeysForLobby = new List<string>() { "lv" },
                };
                p1.CreateRoom(roomName, opts);
            });
            p1.On(Event.ROOM_CREATED, (evtData) => {
                p2.Connect();
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                var matchProps = new Dictionary<string, object>();
                matchProps.Add("lv", 2);
                p2.JoinRandomRoom(matchProps);
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                Console.WriteLine("p2 match success");
                p3.Connect();
            });

            p3.On(Event.CONNECTED, (evtData) =>
            {
                var matchProps = new Dictionary<string, object>();
                matchProps.Add("lv", 3);
                p3.JoinRandomRoom(matchProps);
            });
            p3.On(Event.ROOM_JOIN_FAILED, (evtData) =>
            {
                Console.WriteLine("p3 match failed");
                b1.Stop();
                b2.Stop();
                b3.Stop();
                resetEvent.Set();
            });

            p1.Connect();
            resetEvent.WaitOne();
        }
    }
}
