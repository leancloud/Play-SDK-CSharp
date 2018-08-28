using NUnit.Framework;
using System;
using System.Threading;
using System.Collections.Generic;

using LeanCloud.Play;

namespace Test
{
    [TestFixture()]
    public class CustomEventTest
    {
        [Test()]
        public void TestCustomEventWithReceiverGroup()
        {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName = "ce1_room";
            var b1 = Utility.NewBehavior("ce1_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("ce1_2");
            var p2 = b2.Play;

            p1.On(Event.CONNECTED, (evtData) =>
            {
                p1.CreateRoom(roomName);
            });
            p1.On(Event.ROOM_CREATED, (evtData) =>
            {
                p2.Connect();
            });
            p1.On(Event.CUSTOM_EVENT, (evtData) =>
            {
                var eventId = evtData["eventId"] as string;
                var eventData = evtData["eventData"] as Dictionary<string, object>;
                var name = eventData["name"] as string;
                var body = eventData["body"] as string;
                Console.WriteLine("{0} : {1} => {2}, {3}", p1.UserId, eventId, name, body);

                b1.Stop();
                b2.Stop();
                resetEvent.Set();
            });

            p2.On(Event.CONNECTED, (evtData) =>
            {
                p2.JoinRoom(roomName);
            });
            p2.On(Event.ROOM_JOINED, (evtData) =>
            {
                Dictionary<string, object> data = new Dictionary<string, object>() { 
                    { "name", "aa" },
                    { "body", "bb" },
                };
                var opts = new SendEventOptions()
                {
                    ReceiverGroup = ReceiverGroup.MasterClient,
                };
                p2.SendEvent("hi", data, opts);
            });
            p2.On(Event.CUSTOM_EVENT, (evtData) =>
            {
                var eventId = evtData["eventId"] as string;
                var eventData = evtData["eventData"] as Dictionary<string, object>;
                var name = eventData["name"] as string;
                var body = eventData["body"] as string;
                Console.WriteLine("{0} : {1} => {2}, {3}", p2.UserId, eventId, name, body);
            });

            p1.Connect();
            resetEvent.WaitOne();
        }

        [Test()]
        public void TestCustomEventWithTargetIds() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var roomName = "ce2_room";
            var b1 = Utility.NewBehavior("ce2_1");
            var p1 = b1.Play;
            var b2 = Utility.NewBehavior("ce2_2");
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
            p1.On(Event.CUSTOM_EVENT, (evtData) =>
            {
                var eventId = evtData["eventId"] as string;
                var eventData = evtData["eventData"] as Dictionary<string, object>;
                var name = eventData["name"] as string;
                var body = eventData["body"] as string;
                Console.WriteLine("{0} : {1} => {2}, {3}", p1.UserId, eventId, name, body);
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
                Dictionary<string, object> data = new Dictionary<string, object>() {
                    { "name", "aa" },
                    { "body", "bb" },
                };
                var opts = new SendEventOptions()
                {
                    targetActorIds = new List<int>() { 1, 2 },
                };
                p2.SendEvent("hi", data, opts);
            });
            p2.On(Event.CUSTOM_EVENT, (evtData) => {
                var eventId = evtData["eventId"] as string;
                var eventData = evtData["eventData"] as Dictionary<string, object>;
                var name = eventData["name"] as string;
                var body = eventData["body"] as string;
                Console.WriteLine("{0} : {1} => {2}, {3}", p2.UserId, eventId, name, body);
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
    }
}
