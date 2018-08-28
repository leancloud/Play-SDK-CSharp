using NUnit.Framework;
using System;
using System.Threading;

using LeanCloud.Play;

namespace Test
{
    [TestFixture()]
    public class LobbyTest
    {
        [Test()]
        public void TestJoinLobbyManually()
        {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            Behavior behavior = Utility.NewBehavior("lt1");
            Play play = behavior.Play;
            play.On(Event.CONNECTED, (evtData) => {
                play.JoinLobby();
            });
            play.On(Event.LOBBY_JOINED, (evtData) =>
            {
                Console.WriteLine("joined lobby");
                behavior.Stop();
                resetEvent.Set();
            });
            play.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestRoomListUpdate() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            Behavior behavior1 = Utility.NewBehavior("lt2_1");
            Play play1 = behavior1.Play;
            Behavior behavior2 = Utility.NewBehavior("lt2_2");
            Play play2 = behavior2.Play;
            Behavior behavior3 = Utility.NewBehavior("lt2_3");
            Play play3 = behavior3.Play;
            Behavior behavior4 = Utility.NewBehavior("lt2_4");
            Play play4 = behavior4.Play;
            play1.On(Event.CONNECTED, (evtData) =>
            {
                play1.CreateRoom("lt2_room1");
            });
            play1.On(Event.ROOM_CREATED, (evtData) =>
            {
                play2.Connect();
            });

            play2.On(Event.CONNECTED, (evtData) =>
            {
                play2.CreateRoom("lt2_room2");
            });
            play2.On(Event.ROOM_CREATED, (evtData) =>
            {
                play3.Connect();
            });

            play3.On(Event.CONNECTED, (evtData) =>
            {
                play3.CreateRoom("lt2_room3");
            });
            play3.On(Event.ROOM_CREATED, (evtData) =>
            {
                play4.Connect();
            });

            play4.On(Event.CONNECTED, (evtData) =>
            {
                play4.JoinLobby();
            });
            play4.On(Event.LOBBY_ROOM_LIST_UPDATED, (evtData) =>
            {
                Console.WriteLine("room list updated");
                Assert.GreaterOrEqual(play4.LobbyRoomList.Count, 3);
                behavior1.Stop();
                behavior2.Stop();
                behavior3.Stop();
                behavior4.Stop();
                resetEvent.Set();
            });

            play1.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestAutoJoinLobby() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);

            var beh = Utility.NewBehavior("lt3");
            var play = beh.Play;
            play.AutoJoinLobby = true;

            play.On(Event.LOBBY_JOINED, (evtData) => {
                play.JoinOrCreateRoom("lt3_room");
            });
            play.On(Event.ROOM_CREATED, (evtData) => {
                Console.WriteLine("room created");
            });
            play.On(Event.ROOM_CREATE_FAILED, (evtData) => {
                Console.WriteLine("room create failed");
            });
            play.On(Event.ROOM_JOINED, (evtData) =>
            {
                Console.WriteLine("room joined");
                beh.Stop();
                resetEvent.Set();
            });
            play.On(Event.ROOM_JOIN_FAILED, (evtData) =>
            {
                Console.WriteLine("room join failed");
            });

            play.Connect();

            resetEvent.WaitOne();
        }
    }
}
