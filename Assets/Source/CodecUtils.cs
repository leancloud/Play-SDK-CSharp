using System.Collections.Generic;
using LeanCloud.Play.Protocol;
using Google.Protobuf;

namespace LeanCloud.Play {
    public static class CodecUtils {
        public static GenericCollectionValue Encode(object val) {
            GenericCollectionValue genericVal = null;
            if (val is int) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Int,
                    IntValue = (int)val
                };
            } else if (val is float) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Float,
                    FloatValue = (float)val
                };
            } else if (val is string) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.String,
                    StringValue = (string)val
                };
            } else if (val is bool) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Bool,
                    BoolValue = (bool)val
                };
            } else if (val is long) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Long,
                    LongIntValue = (long)val
                };
            } else if (val is PlayObject playObject) {
                var collection = new GenericCollection();
                foreach (KeyValuePair<string, object> entry in playObject) {
                    collection.MapEntryValue.Add(new GenericCollection.Types.MapEntry {
                        Key = entry.Key,
                        Val = Encode(entry.Value)
                    });
                }
                genericVal = new GenericCollectionValue { 
                    Type = GenericCollectionValue.Types.Type.Object,
                    BytesValue = collection.ToByteString()
                };
            } else if (val is PlayArray playArray) {
                var collection = new GenericCollection();
                foreach (object obj in playArray) {
                    collection.ListValue.Add(Encode(obj));
                }
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Array,
                    BytesValue = collection.ToByteString()
                };
            }
            // TODO 自定义类型

            return genericVal;
        }

        public static object Decode(GenericCollectionValue genericValue) {
            object val = null;
            switch (genericValue.Type) {
                case GenericCollectionValue.Types.Type.Int:
                    val = genericValue.IntValue;
                    break;
                case GenericCollectionValue.Types.Type.Float:
                    val = genericValue.FloatValue;
                    break;
                case GenericCollectionValue.Types.Type.String:
                    val = genericValue.StringValue;
                    break;
                case GenericCollectionValue.Types.Type.Bool:
                    val = genericValue.BoolValue;
                    break;
                case GenericCollectionValue.Types.Type.Long:
                    val = genericValue.LongIntValue;
                    break;
                case GenericCollectionValue.Types.Type.Object: {
                        PlayObject playObject = new PlayObject();
                        var collection = GenericCollection.Parser.ParseFrom(genericValue.BytesValue);
                        foreach (var entry in collection.MapEntryValue) {
                            playObject.Add(entry.Key, Decode(entry.Val));
                        }
                        val = playObject;
                    }
                    break;
                case GenericCollectionValue.Types.Type.Array: {
                        PlayArray playArray = new PlayArray();
                        var collection = GenericCollection.Parser.ParseFrom(genericValue.BytesValue);
                        foreach (var element in collection.ListValue) {
                            playArray.Add(Decode(element));
                        }
                        val = playArray;
                    }
                    break;
                    // TODO 自定义类型

                default:
                    break;
            }
            return val;
        }
    }
}
