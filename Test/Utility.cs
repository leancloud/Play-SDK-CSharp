using System;

using LeanCloud.Play;

namespace Test
{
    public class Utility
    {
        private static Play NewPlay(string userId) {
            var play = new Play();
            play.Init("315XFAYyIGPbd98vHPCBnLre-9Nh9j0Va", "Y04sM6TzhMSBmCMkwfI3FpHc", Region.EastChina);
            play.UserId = userId;
            return play;
        }

        public static Behavior NewBehavior(string userId) {
            var play = NewPlay(userId);
            Behavior behavior = new Behavior(play);
            return behavior;
        }
    }
}
