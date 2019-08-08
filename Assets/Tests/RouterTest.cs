using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LeanCloud.Play;
using System.Threading.Tasks;

namespace LeanCloud.Play.Test {
    public class RouterTest {
        [UnityTest]
        public IEnumerator PlayServer() {
            Logger.LogDelegate += Utils.Log;

            var f = false;
            var appId = "pyon3kvufmleg773ahop2i7zy0tz2rfjx5bh82n7h5jzuwjg";
            var appKey = "MJSm46Uu6LjF5eNmqfbuUmt6";
            var userId = "rt0";
            var playServer = "https://api2.ziting.wang";
            var c = new Client(appId, appKey, userId, playServer: playServer);
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom();
            }).Unwrap().OnSuccess(_ => {
                c.Close();
                f = true;
            });

            while (!f) {
                yield return null;
            }
            Logger.LogDelegate -= Utils.Log;
        }

        [Test]
        public void FallbackRouter() {
            Debug.Log(PlayRouter.GetFallbackRouter("FQr8l8LLvdxIwhMHN77sNluX-9Nh9j0Va"));
            Assert.AreEqual(PlayRouter.GetFallbackRouter("FQr8l8LLvdxIwhMHN77sNluX-9Nh9j0Va"), "https://fqr8l8ll.play.lncldapi.com/1/multiplayer/router/route");
            Debug.Log(PlayRouter.GetFallbackRouter("FQr8l8LLvdxIwhMHN77sNluX-MdYXbMMI"));
            Assert.AreEqual(PlayRouter.GetFallbackRouter("FQr8l8LLvdxIwhMHN77sNluX-MdYXbMMI"), "https://fqr8l8ll.play.lncldglobal.com/1/multiplayer/router/route");
            Debug.Log(PlayRouter.GetFallbackRouter("BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz"));
            Assert.AreEqual(PlayRouter.GetFallbackRouter("BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz"), "https://bmyv4rks.play.lncld.com/1/multiplayer/router/route");
        }
    }
}
