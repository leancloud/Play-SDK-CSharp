using System;

using LeanCloud.Play;

namespace Test
{
    public class Utility
    {
        static string APP_ID = "1yzaPvxYPs2DLQXIccBzb0k1-gzGzoHsz";
        static string APP_KEY = "Nlt1SIVxxFrMPut6SvfEJiYT";
        static Region APP_REGION = Region.NorthChina;

        private static Play NewPlay(string userId) {
            var play = new Play();
            play.Init(APP_ID, APP_KEY, APP_REGION);
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
