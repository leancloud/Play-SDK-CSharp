using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LeanCloud.Play.Protocol;

internal class ResponseWrapper {
    internal CommandType Cmd {
        get; set;
    }

    internal OpType Op {
        get; set;
    }

    internal ResponseMessage Response {
        get; set;
    }
}
