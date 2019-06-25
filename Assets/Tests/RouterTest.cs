using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LeanCloud.Play;
using System.Threading.Tasks;

namespace LeanCloud.Play.Test
{
    public class RouterTest
    {
        [Test]
        public async void PlayServer() {
            Logger.LogDelegate += Utils.Log;

            var appId = "pyon3kvufmleg773ahop2i7zy0tz2rfjx5bh82n7h5jzuwjg";
            var appKey = "MJSm46Uu6LjF5eNmqfbuUmt6";
            var userId = "rt0";
            var playServer = "https://api2.ziting.wang";
            var c = new Client(appId, appKey, userId, playServer: playServer);
            await c.Connect();
            await c.CreateRoom();
            c.Close();

            Logger.LogDelegate -= Utils.Log;
        }
    }
}
