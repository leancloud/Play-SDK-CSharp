using System;
using System.Collections.Generic;

namespace LeanCloud.Play
{
    internal static class EventHandler
    {
        internal static void HandleMessage(Play play, Dictionary<string, object> msg) {
            int eventId = (int)msg["eventId"];
            Event @event = (Event)eventId;
            if (msg.TryGetValue("eventData", out object eventDataObj)) {
                Dictionary<string, object> eventData = eventDataObj as Dictionary<string, object>;
                play.Emit(@event, eventData);
            } else {
                play.Emit(@event);
            }
        }
    }
}
