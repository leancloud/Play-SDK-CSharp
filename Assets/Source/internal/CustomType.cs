using System;

namespace LeanCloud.Play {
    internal class CustomType {
        internal Type Type {
            get;
        }

        internal int TypeId {
            get;
        }

        internal EncodeFunc EncodeFunc {
            get;
        }

        internal DecodeFunc DecodeFunc {
            get;
        }

        internal CustomType(Type type, int typeId, EncodeFunc encodeFunc, DecodeFunc decodeFunc) {
            Type = type;
            TypeId = typeId;
            EncodeFunc = encodeFunc;
            DecodeFunc = decodeFunc;
        }
    }
}
