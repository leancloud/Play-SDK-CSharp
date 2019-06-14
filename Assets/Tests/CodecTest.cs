using NUnit.Framework;
using UnityEngine;
using Google.Protobuf;
using LeanCloud.Play.Protocol;
using System.Collections.Generic;

namespace LeanCloud.Play
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

        [Test]
        public void Protocol() {
            // 构造请求
            var request = new RequestMessage() {
                I = 1,
            };
            var roomOptions = new RoomOptions {
                Visible = false,
                EmptyRoomTtl = 60,
                MaxPlayerCount = 2,
                PlayerTtl = 60,
                CustomRoomProperties = new PlayObject {
                    { "title", "room title" },
                    { "level", 2 },
                },
                CustoRoomPropertyKeysForLobby = new List<string> { "level" }
            };
            var expectedUserIds = new List<string> { "world" };
            var roomOpts = ConvertToRoomOptions("abc", roomOptions, expectedUserIds);
            request.CreateRoom = new CreateRoomRequest {
                RoomOptions = roomOpts
            };
            var command = new Command {
                Cmd = CommandType.Conv,
                Op = OpType.Start,
                Body = new Body {
                    Request = request
                }.ToByteString()
            };
            // 序列化请求
            var bytes = command.ToByteArray();
            // 反序列化请求
            var reCommand = Command.Parser.ParseFrom(bytes);
            Assert.AreEqual(reCommand.Cmd, CommandType.Conv);
            Assert.AreEqual(reCommand.Op, OpType.Start);
            var reBody = Body.Parser.ParseFrom(reCommand.Body);
            var reRequest = reBody.Request;
            Assert.AreEqual(reRequest.I, 1);
            var reRoomOptions = request.CreateRoom.RoomOptions;
            Assert.AreEqual(reRoomOptions.Visible, false);
            Assert.AreEqual(reRoomOptions.EmptyRoomTtl, 60);
            Assert.AreEqual(reRoomOptions.MaxMembers, 2);
            Assert.AreEqual(reRoomOptions.PlayerTtl, 60);
            var attrBytes = reRoomOptions.Attr;
            var reAttr = CodecUtils.Decode(GenericCollectionValue.Parser.ParseFrom(attrBytes)) as PlayObject;
            Debug.Log(reAttr["title"]);
            Debug.Log(reAttr["level"]);
            Assert.AreEqual(reAttr["title"], "room title");
            Assert.AreEqual(reAttr["level"], 2);
        }

        internal static Protocol.RoomOptions ConvertToRoomOptions(string roomName, RoomOptions options, List<string> expectedUserIds) {
            var roomOptions = new Protocol.RoomOptions();
            if (!string.IsNullOrEmpty(roomName)) {
                roomOptions.Cid = roomName;
            }
            if (options != null) {
                roomOptions.Visible = options.Visible;
                roomOptions.Open = options.Open;
                roomOptions.EmptyRoomTtl = options.EmptyRoomTtl;
                roomOptions.PlayerTtl = options.PlayerTtl;
                roomOptions.MaxMembers = options.MaxPlayerCount;
                roomOptions.Flag = options.Flag;
                roomOptions.Attr = CodecUtils.Encode(options.CustomRoomProperties).ToByteString();
                roomOptions.LobbyAttrKeys.AddRange(options.CustoRoomPropertyKeysForLobby);
            }
            if (expectedUserIds != null) {
                roomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            return roomOptions;
        }
    }
}
