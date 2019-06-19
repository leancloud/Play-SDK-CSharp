using System.Collections.Generic;
using LeanCloud.Play.Protocol;
using Google.Protobuf;

namespace LeanCloud.Play {
    public static class CodecUtils {
        public static GenericCollectionValue Encode(object val) {
            GenericCollectionValue genericVal = null;
            if (val is null) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Null
                };
            } else if (val is byte[]) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Bytes,
                    BytesValue = ByteString.CopyFrom((byte[])val)
                };
            } else if (val is byte) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Byte,
                    IntValue = (byte)val
                };
            } else if (val is short) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Short,
                    IntValue = (short)val
                };
            } else if (val is int) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Int,
                    IntValue = (int)val
                };
            } else if (val is long) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Long,
                    LongIntValue = (long)val
                };
            } else if (val is bool) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Bool,
                    BoolValue = (bool)val
                };
            } else if (val is float) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Float,
                    FloatValue = (float)val
                };
            } else if (val is double) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Double,
                    DoubleValue = (double)val
                };
            } else if (val is string) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.String,
                    StringValue = (string)val
                };
            } else if (val is PlayObject playObject) {
                var bytes = EncodePlayObject(playObject);
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Map,
                    BytesValue = bytes
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
            } else {
                // TODO 自定义类型
            
            }

            return genericVal;
        }

        public static object Decode(GenericCollectionValue genericValue) {
            object val = null;
            switch (genericValue.Type) {
                case GenericCollectionValue.Types.Type.Null:
                    // val = null;
                    break;
                case GenericCollectionValue.Types.Type.Bytes:
                    val = genericValue.BytesValue.ToByteArray();
                    break;
                case GenericCollectionValue.Types.Type.Byte:
                    val = (byte)genericValue.IntValue;
                    break;
                case GenericCollectionValue.Types.Type.Short:
                    val = (short)genericValue.IntValue;
                    break;
                case GenericCollectionValue.Types.Type.Int:
                    val = genericValue.IntValue;
                    break;
                case GenericCollectionValue.Types.Type.Long:
                    val = genericValue.LongIntValue;
                    break;
                case GenericCollectionValue.Types.Type.Bool:
                    val = genericValue.BoolValue;
                    break;
                case GenericCollectionValue.Types.Type.Float:
                    val = genericValue.FloatValue;
                    break;
                case GenericCollectionValue.Types.Type.Double:
                    val = genericValue.DoubleValue;
                    break;
                case GenericCollectionValue.Types.Type.String:
                    val = genericValue.StringValue;
                    break;
                case GenericCollectionValue.Types.Type.Map:
                    val = DecodePlayObject(genericValue.BytesValue);
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
                case GenericCollectionValue.Types.Type.Object: {
                        // TODO 自定义类型

                    }
                    break;
                default:
                    // TODO 异常

                    break;
            }
            return val;
        }

        public static ByteString EncodePlayObject(PlayObject playObject) {
            if (playObject == null) {
                return null;
            }
            var collection = new GenericCollection();
            foreach (var entry in playObject) {
                collection.MapEntryValue.Add(new GenericCollection.Types.MapEntry {
                    Key = entry.Key as string,
                    Val = Encode(entry.Value)
                });
            }
            return collection.ToByteString();
        }

        public static PlayObject DecodePlayObject(ByteString bytes) {
            var collection = GenericCollection.Parser.ParseFrom(bytes);
            var playObject = new PlayObject();
            foreach (var entry in collection.MapEntryValue) {
                playObject[entry.Key] = Decode(entry.Val);
            }
            return playObject; 
        }
    }
}
