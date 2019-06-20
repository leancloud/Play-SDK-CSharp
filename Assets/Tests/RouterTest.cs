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

            var appId = "2ke9qjLSGeamYyU7dT6eqvng-9Nh9j0Va";
            var appKey = "MJSm46Uu6LjF5eNmqfbuUmt6";
            var userId = "rt0";
            var playServer = "https://2ke9qjLS.play.lncldapi.com/1/multiplayer/router";
            var c = new Client(appId, appKey, userId, playServer: playServer);
            await c.Connect();
            await c.CreateRoom();
            c.Close();

            Logger.LogDelegate -= Utils.Log;
        }
    }
}
