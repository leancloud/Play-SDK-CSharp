using NUnit.Framework;
using System;
using System.Threading;

using LeanCloud.Play;

namespace Test
{
    [TestFixture()]
    public class MasterTest
    {
        [Test()]
        public void TestMaster()
        {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName = "mt1_room";
            var b1 = Utility.NewBehavior("mt1_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("mt1_2");
            var p2 = b2.Play;
            int newMasterId = -1;

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
                newMasterId = newPlayer.ActorId;
                p1.SetMaster(newPlayer.ActorId);
            });
            p1.On(Event.MASTER_SWITCHED, (evtData) =>
            {
                Player newMaster = evtData["newMaster"] as Player;
                Assert.AreEqual(newMasterId, newMaster.ActorId);

                b1.Stop();
                b2.Stop();
                resetEvent.Set();
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRoom(roomName);
            });

            p1.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestMasterLeave() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName = "mt2_room";
            var b1 = Utility.NewBehavior("mt2_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("mt2_2");
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
                p1.LeaveRoom();
            });
            p2.On(Event.MASTER_SWITCHED, (evtData) =>
            {
                Player newMaster = evtData["newMaster"] as Player;
                Assert.AreEqual(p2.Room.MasterActorId, newMaster.ActorId);
                b1.Stop(false);
                b2.Stop();
                resetEvent.Set();
            });

            p1.Connect();
            resetEvent.WaitOne();
        }
    }
}
