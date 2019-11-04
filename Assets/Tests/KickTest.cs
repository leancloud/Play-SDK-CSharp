using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;
using LeanCloud.Common;

namespace LeanCloud.Play.Test
{
    public class KickTest
    {
        [SetUp]
        public void SetUp() {
            Common.Logger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            Common.Logger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator Kick() {
            var flag = false;
            var roomName = "kt0_r";
            var c0 = Utils.NewClient("kt0_0");
            var c1 = Utils.NewClient("kt0_1");
            _ = c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c0.OnPlayerRoomJoined += newPlayer => {
                    _ = c0.KickPlayer(newPlayer.ActorId);
                };
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c1.OnRoomKicked += (code, msg) => {
                    Debug.Log($"{c1.UserId} is kicked");
                    Assert.AreEqual(code, null);
                    Assert.AreEqual(msg, null);
                    _ = c0.Close();
                    _ = c1.Close();
                    flag = true;
                };
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log($"{c1.UserId} joined room");
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!flag) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator KickWithMsg() {
            var flag = false;
            var roomName = "kt1_r";
            var c0 = Utils.NewClient("kt1_0");
            var c1 = Utils.NewClient("kt1_1");
            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                c0.OnPlayerRoomJoined += newPlayer => {
                    _ = c0.KickPlayer(newPlayer.ActorId, 404, "You cheat!");
                };
                return c1.Connect();
            }).Unwrap().OnSuccess(_ => {
                c1.OnRoomKicked += (code, msg) => {
                    Assert.AreEqual(code, 404);
                    Debug.Log($"{c1.UserId} is kicked for {msg}");
                    _ = c0.Close();
                    _ = c1.Close();
                    flag = true;
                };
                return c1.JoinRoom(roomName);
            }).Unwrap().OnSuccess(_ => {
                Debug.Log($"{c1.UserId} joined room");
            });

            while (!flag) {
                yield return null;
            }
        }
    }
}
