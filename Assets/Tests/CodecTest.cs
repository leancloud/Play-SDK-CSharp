using NUnit.Framework;
using UnityEngine;

namespace LeanCloud.Play.Test 
{
    public class CodecTest
    {
        [Test]
        public void CheckType() {
            object s = (short)10;
            object a = 10;
            object l = 10L;
            object f = 10f;
            Assert.AreEqual(s is short, true);
            Assert.AreEqual(a is int, true);
            Assert.AreEqual(l is long, true);
            Assert.AreEqual(f is float, true);
            Assert.AreEqual(f is double, false);
        }

        [Test]
        public void PlayObject() {
            var playObj = new PlayObject {
                ["i"] = 123,
                ["b"] = true,
                ["str"] = "hello, world"
            };
            var subPlayObj = new PlayObject {
                ["si"] = 345,
                ["sb"] = true,
                ["sstr"] = "code"
            };
            playObj.Add("obj", subPlayObj);
            var subPlayArr = new PlayArray { 
                666, true, "engineer"
            };
            playObj.Add("arr", subPlayArr);
            var genericValue = CodecUtils.Encode(playObj);
            Debug.Log(genericValue);
            var newPlayObj = CodecUtils.Decode(genericValue) as PlayObject;
            Assert.AreEqual(playObj["i"], 123);
            Assert.AreEqual(playObj["b"], true);
            Assert.AreEqual(playObj["str"], "hello, world");
            var newSubPlayObj = playObj["obj"] as PlayObject;
            Assert.AreEqual(newSubPlayObj["si"], 345);
            Assert.AreEqual(newSubPlayObj["sb"], true);
            Assert.AreEqual(newSubPlayObj["sstr"], "code");
            var newSubPlayArr = playObj["arr"] as PlayArray;
            Assert.AreEqual(newSubPlayArr[0], 666);
            Assert.AreEqual(newSubPlayArr[1], true);
            Assert.AreEqual(newSubPlayArr[2], "engineer");
        }

        [Test]
        public void PlayArray() {
            var playArr = new PlayArray { 
                123, true, "hello, world",
                new PlayObject {
                    ["i"] = 23,
                    ["b"] = true,
                    ["str"] = "hello"
                }
            };
            var genericValue = CodecUtils.Encode(playArr);
            Debug.Log(genericValue);
            var newPlayArr = CodecUtils.Decode(genericValue) as PlayArray;
            Assert.AreEqual(playArr[0], 123);
            Assert.AreEqual(playArr[1], true);
            Assert.AreEqual(playArr[2], "hello, world");
            var subPlayObj = playArr[3] as PlayObject;
            Assert.AreEqual(subPlayObj["i"], 23);
            Assert.AreEqual(subPlayObj["b"], true);
            Assert.AreEqual(subPlayObj["str"], "hello");
        }
    }
}
