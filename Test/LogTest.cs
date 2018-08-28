using NUnit.Framework;
using System;
using System.Threading;

using LeanCloud.Play;

namespace Test
{
    [TestFixture()]
    public class LogTest
    {
        [Test]
        public void TestLog() {
            Logger.LogDelegate = (logLevel, info) => {
                Console.WriteLine(string.Format("[{0}] {1}", logLevel, info));
            };
            var resetEvent = new ManualResetEvent(false);
            var beh = Utility.NewBehavior("tl1");
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
    }
}
