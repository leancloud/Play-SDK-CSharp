using System.Linq;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;
using Newtonsoft.Json;
using Google.Protobuf;

namespace LeanCloud.Play {
    internal static class Utils {
        internal static PlayObject ConvertToPlayObject(RoomSystemProperty property) { 
            if (property == null) {
                return null;
            }
            var obj = new PlayObject();
            if (property.Open != null) {
                obj["open"] = property.Open;
            }
            if (property.Visible != null) {
                obj["visible"] = property.Visible;
            }
            if (property.MaxMembers > 0) {
                obj["maxPlayerCount"] = property.MaxMembers;
            }
            if (!string.IsNullOrEmpty(property.ExpectMembers)) {
                obj["expectedUserIds"] = JsonConvert.DeserializeObject<List<string>>(property.ExpectMembers);
            }
            return obj;
        }
    }
}
