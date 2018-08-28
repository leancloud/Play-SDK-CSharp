using NUnit.Framework;
using System;
using System.Threading;
using System.Collections.Generic;

using LeanCloud.Play;

namespace Test
{
    [TestFixture()]
    public class CreateRoom
    {
        [Test()]
        public void TestCreateDefaultRoom()
        {
            var beh = Utility.NewBehavior("cr1");
            var play = beh.Play;
            var resetEvent = new ManualResetEvent(false);
            Behavior behavior = new Behavior(play);
            play.On(Event.CONNECTED, (evtData) => {
                play.CreateRoom();
            });
            play.On(Event.ROOM_CREATED, (evtData) => {
                Console.WriteLine("room created");
                behavior.Stop();
                resetEvent.Set();
            });
            play.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestCreateSimpleRoom() {
            var beh = Utility.NewBehavior("cr2");
            var play = beh.Play;
            var resetEvent = new ManualResetEvent(false);
            Behavior behavior = new Behavior(play);
            play.On(Event.CONNECTED, (evtData) => {
                play.CreateRoom("cr2_room");
            });
            play.On(Event.ROOM_CREATED, (evtData) => {
                Console.WriteLine("room created");
                behavior.Stop();
                resetEvent.Set();
            });
            play.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestCreateCustomRoom() {
            var random = new Random();
            var roomName = string.Format("room_{0}", random.Next(10000000));
            var beh = Utility.NewBehavior("cr3");
            var play = beh.Play;
            var resetEvent = new ManualResetEvent(false);
            Behavior behavior = new Behavior(play);
            play.On(Event.CONNECTED, (evtData) => {
                var props = new Dictionary<string, object>();
                props.Add("title", "room title");
                props.Add("level", 2);
                var options = new RoomOptions()
                {
                    Visible = false,
                    EmptyRoomTtl = 10000,
                    PlayerTtl = 600,
                    MaxPlayerCount = 2,
                    CustomRoomProperties = props,
                    CustoRoomPropertyKeysForLobby = new List<string>() { "level" },
                };
                var expectedUserIds = new List<string>() { "cr3_2" };
                play.CreateRoom(roomName, options, expectedUserIds);
            });
            play.On(Event.ROOM_CREATED, (evtData) => {
                Console.WriteLine("room created");
                Assert.AreEqual(play.Room.Visible, false);
                beh.Stop();
                resetEvent.Set();
            });
            play.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestCreateFailed() {
            var resetEvent = new ManualResetEvent(false);
            var roomName = "cr4_room";
            var b1 = Utility.NewBehavior("cr4_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("cr4_2");
            var p2 = b2.Play;
            p1.On(Event.CONNECTED, (evtData) =>
            {
                p1.CreateRoom(roomName);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });

            p2.On(Event.CONNECTED, (evtData) => {
                p2.CreateRoom(roomName);
            });
            p2.On(Event.ROOM_CREATE_FAILED, (evtData) => {
                b1.Stop();
                b2.Stop();
                resetEvent.Set();
            });
            p1.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestMasterAndLocal() {
            var resetEvent = new ManualResetEvent(false);
            var roomName = "cr5_room";
            var b1 = Utility.NewBehavior("cr5_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("cr5_2");
            var p2 = b2.Play;
            p1.On(Event.CONNECTED, (evtData) =>
            {
                p1.CreateRoom(roomName);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });
            p1.On(Event.PLAYER_ROOM_JOINED, (evtData) =>
            {
                Player newPlayer = evtData["newPlayer"] as Player;
                Assert.AreEqual(p1.Player.IsMaster, true);
                Assert.AreEqual(newPlayer.IsMaster, false);
                Assert.AreEqual(p1.Player.IsLocal, true);
                Assert.AreEqual(newPlayer.IsLocal, false);
                Assert.AreEqual(p1.Room.PlayerList.Count, 2);

                b1.Stop();
                b2.Stop();
                resetEvent.Set();
            });

            p2.On(Event.CONNECTED, (evtData) => {
                p2.JoinRoom(roomName);
            });

            p1.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestSetRoomOpened() {
            var resetEvent = new ManualResetEvent(false);
            var roomName = "cr6_room";
            var beh = Utility.NewBehavior("cr6");
            var play = beh.Play;

            play.On(Event.CONNECTED, (evtData) =>
            {
                play.CreateRoom(roomName);
            });
            play.On(Event.ROOM_CREATED, (evtData) =>
            {
                play.SetRoomOpened(false);
                play.SetRoomVisible(false);
            });
            play.On(Event.ROOM_OPEN_CHANGED, (evtData) =>
            {
                bool opened = (bool)evtData["opened"];
                Assert.AreEqual(opened, false);
            });
            play.On(Event.ROOM_VISIBLE_CHANGED, (evtData) =>
            {
                bool visible = (bool)evtData["visible"];
                Assert.AreEqual(visible, false);

                beh.Stop();
                resetEvent.Set();
            });

            play.Connect();

            resetEvent.WaitOne();
        }
    }
}
