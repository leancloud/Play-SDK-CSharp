using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LeanCloud.Play {
    internal class PlayContext : MonoBehaviour {
        Queue<Action> runningActions;
        Queue<Action> waitingActions;

        bool running;

        internal bool IsMessageQueueRunning {
            private get; set;
        }

        void Awake() {
            runningActions = new Queue<Action>();
            waitingActions = new Queue<Action>();
            running = true;
            IsMessageQueueRunning = true;
        }

        void Update() {
            if (!running) {
                return;
            }
            if (!IsMessageQueueRunning) {
                return;
            }
            if (waitingActions.Count > 0) { 
                lock (waitingActions) {
                    var temp = runningActions;
                    runningActions = waitingActions;
                    waitingActions = temp;
                }
                while (runningActions.Count > 0) {
                    var action = runningActions.Dequeue();
                    action.Invoke();
                    // 在执行过程中可能会暂停消息处理，如加入房间成功后，加载场景
                    if (!running || !IsMessageQueueRunning) { 
                        lock (waitingActions) {
                            var temp = waitingActions;
                            waitingActions = runningActions;
                            while (temp.Count > 0) {
                                var waitingAct = temp.Dequeue();
                                waitingActions.Enqueue(waitingAct);
                            }
                        }
                        break;
                    }
                }
            }
        }

        internal void Post(Action action) { 
            if (action == null) {
                return;
            }
            lock (waitingActions) {
                waitingActions.Enqueue(action);
            }
        }

        internal void Pause() {
            running = false;
        }

        internal void Resume() {
            running = true;
        }
    }
}
