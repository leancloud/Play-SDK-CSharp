using System;
using System.Collections.Generic;

namespace LeanCloud.Play {
    internal static class ErrorHandler {
        internal static void HandleError(Play play, Dictionary<string, object> msg) {
            long code = (long)msg["reasonCode"];
            string detail = msg["detail"] as string;
            Dictionary<string, object> error = new Dictionary<string, object>() {
                { "code", (int)code },
                { "detail", detail },
            };
			play.Emit(Event.ERROR, error);
		}
	}
}
