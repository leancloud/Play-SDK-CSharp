using NUnit.Framework;
using System;
using System.Threading;
using System.Collections.Generic;

using LeanCloud.Play;

namespace Test
{
    [TestFixture()]
    public class ConnectTest
    {
        [Test()]
        public void TestConnect()
        {
            var resetEvent = new ManualResetEvent(false);
            var beh = Utility.NewBehavior("tc1");
            var play = beh.Play;
            Behavior behavior = new Behavior(play);
            play.On(Event.CONNECTED, (eventData) => {
                Console.WriteLine("connected..");
                beh.Stop();
                resetEvent.Set();
            });
            play.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestConnectWithSameId() {
            var resetEvent = new ManualResetEvent(false);
            var b1 = Utility.NewBehavior("tc2");
            var b2 = Utility.NewBehavior("tc2");
            var p1 = b1.Play;
            var p2 = b2.Play;
            var f1 = false;
            var f2 = false;

            p1.On(Event.CONNECTED, (eventData) => {
                Console.WriteLine("play1 connected");
                p2.Connect();
            });
            p1.On(Event.ERROR, (eventData) =>
            {
                Console.WriteLine("error event");
                int code = (int)eventData["code"];
                if (code == 4102)
                {
                    Console.WriteLine("connect error");
                    f1 = true;
                    if (f1 && f2) {
                        b2.Stop();
                        resetEvent.Set();
                    }
                }
            });

            p2.On(Event.CONNECTED, (eventData) => {
                Console.WriteLine("play2 connected");
                f2 = true;
                if (f1 && f2)
                {
                    b2.Stop();
                    resetEvent.Set();
                }
            });

            p1.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestDiconnectFromMaster() {
            var resetEvent = new ManualResetEvent(false);
            var beh = Utility.NewBehavior("tc3");
            var play = beh.Play;
            bool reconnectFlag = false;
            play.On(Event.CONNECTED, (eventData) => {
                play.Disconnect();
            });
            play.On(Event.DISCONNECTED, (eventData) => {
                Console.WriteLine("disconnected");
                if (!reconnectFlag) {
                    play.Reconnect();
                    reconnectFlag = true;
                } else {
                    beh.Stop(false);
                    resetEvent.Set();
                }
            });
            play.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestDisconnectFromGame() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName = "tc4_room";
            var beh = Utility.NewBehavior("tc4");
            Play play = beh.Play;
            play.On(Event.CONNECTED, (evtData) =>
            {
                play.CreateRoom(roomName);
            });
            play.On(Event.ROOM_CREATED, (evtData) =>
            {
                play.Disconnect();
            });
            play.On(Event.DISCONNECTED, (evtData) => {
                Console.WriteLine("disconnected");
                beh.Stop(false);
                resetEvent.Set();
            });

            play.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestKeepAlive() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);

            var roomName = "tc5_room";
            var beh = Utility.NewBehavior("tc5");
            Play play = beh.Play;
            play.On(Event.CONNECTED, (evtData) =>
            {
                Console.WriteLine("connected");
                play.CreateRoom(roomName);
            });
            play.On(Event.ROOM_JOINED, (evtData) =>
            {
                Console.WriteLine("joined");
            });

            System.Timers.Timer timer = new System.Timers.Timer(30000);
            timer.Elapsed += (sender, e) => {
                Console.WriteLine("timer over");
                beh.Stop();
                resetEvent.Set();
            };
            timer.Start();

            play.Connect();

            resetEvent.WaitOne();
        }
    }
}
