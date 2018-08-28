using System;
using System.Threading;

using LeanCloud.Play;

namespace Test
{
    public class Behavior
    {
        public bool Flag {
            get; set;
        }

        public Play Play {
            get; set;
        }

        public Behavior(Play play)
        {
            this.Play = play;
            this.Flag = true;
            Thread thread = new Thread(() => {
                while (this.Flag)
                {
                    play.HandleMessage();
                    Thread.Sleep(30);
                }
            });
            thread.Start();
        }

        public void Stop() {
            this.Play.Disconnect();
            this.Flag = false;
        }
    }
}
