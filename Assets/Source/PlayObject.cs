using System;
using System.Collections;
using System.Collections.Generic;

namespace LeanCloud.Play {
    /// <summary>
    /// 字典类结构，实现 IDictionary
    /// </summary>
    public class PlayObject : IDictionary<object, object> {
        internal Dictionary<object, object> Data {
            get; private set;
        }

        public ICollection<object> Keys => ((IDictionary<object, object>)Data).Keys;

        public ICollection<object> Values => ((IDictionary<object, object>)Data).Values;

        public int Count => ((IDictionary<object, object>)Data).Count;

        public bool IsReadOnly => ((IDictionary<object, object>)Data).IsReadOnly;

        public object this[object key] { get => ((IDictionary<object, object>)Data)[key]; set => ((IDictionary<object, object>)Data)[key] = value; }

        public void Add(object key, object value) {
            ((IDictionary<object, object>)Data).Add(key, value);
        }

        public bool ContainsKey(object key) {
            return ((IDictionary<object, object>)Data).ContainsKey(key);
        }

        public bool Remove(object key) {
            return ((IDictionary<object, object>)Data).Remove(key);
        }

        public bool TryGetValue(object key, out object value) {
            return ((IDictionary<object, object>)Data).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<object, object> item) {
            ((IDictionary<object, object>)Data).Add(item);
        }

        public void Clear() {
            ((IDictionary<object, object>)Data).Clear();
        }

        public bool Contains(KeyValuePair<object, object> item) {
            return ((IDictionary<object, object>)Data).Contains(item);
        }

        public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex) {
            ((IDictionary<object, object>)Data).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<object, object> item) {
            return ((IDictionary<object, object>)Data).Remove(item);
        }

        public IEnumerator<KeyValuePair<object, object>> GetEnumerator() {
            return ((IDictionary<object, object>)Data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IDictionary<object, object>)Data).GetEnumerator();
        }

        // 扩展接口
        public PlayObject(int capacity) {
            Data = new Dictionary<object, object>(capacity);
        }

        public PlayObject() : this(0) {

        }

        public PlayObject(IDictionary dictionary) : this() {
            if (dictionary != null) {
                foreach (DictionaryEntry entry in dictionary) {
                    Data.Add(entry.Key, entry.Value);
                }
            }
        }

        public bool IsEmpty {
            get {
                return Data == null || Data.Count == 0;
            }
        }

        public bool TryGetBool(object key, out bool val) {
            if (Data.TryGetValue(key, out var valObj) &&
                bool.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = false;
            return false;
        }

        public bool TryGetByte(object key, out byte val) {
            if (Data.TryGetValue(key, out var valObj) &&
                byte.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0;
            return false;
        }

        public bool TryGetShort(object key, out short val) {
            if (Data.TryGetValue(key, out var valObj) &&
                short.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0;
            return false;
        }

        public bool TryGetInt(object key, out int val) {
            if (Data.TryGetValue(key, out var valObj) &&
                int.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0;
            return false;
        }

        public bool TryGetLong(object key, out long val) {
            if (Data.TryGetValue(key, out var valObj) &&
                long.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0;
            return false;
        }

        public bool TryGetFloat(object key, out float val) {
            if (Data.TryGetValue(key, out var valObj) &&
                float.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0f;
            return false;
        }

        public bool TryGetDouble(object key, out double val) {
            if (Data.TryGetValue(key, out var valObj) &&
                double.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0;
            return false;
        }

        public bool TryGetString(object key, out string val) {
            if (Data.TryGetValue(key, out var valObj)) {
                val = valObj.ToString();
                return true;
            }
            val = null;
            return false;
        }

        public bool TryGetBytes(object key, out byte[] val) {
            if (Data.TryGetValue(key, out var valObj)) {
                val = valObj as byte[];
                return true;
            }
            val = null;
            return false;
        }

        public bool TryGetPlayObject(object key, out PlayObject val) {
            if (Data.TryGetValue(key, out var valObj)) {
                val = valObj as PlayObject;
                return true;
            }
            val = null;
            return false;
        }

        public bool TryGetPlayArray(object key, out PlayArray val) {
            if (Data.TryGetValue(key, out var valObj)) {
                val = valObj as PlayArray;
                return true;
            }
            val = null;
            return false;
        }

        public bool GetBool(object key) {
            TryGetBool(key, out bool val);
            return val;
        }

        public byte GetByte(object key) {
            TryGetByte(key, out byte val);
            return val;
        }

        public short GetShort(object key) {
            TryGetShort(key, out short val);
            return val;
        }

        public int GetInt(object key) {
            TryGetInt(key, out int val);
            return val;
        }

        public long GetLong(object key) {
            TryGetLong(key, out long val);
            return val;
        }

        public float GetFloat(object key) {
            TryGetFloat(key, out float val);
            return val;
        }

        public double GetDouble(object key) {
            TryGetDouble(key, out double val);
            return val;
        }

        public string GetString(object key) {
            TryGetString(key, out string val);
            return val;
        }

        public byte[] GetBytes(object key) {
            TryGetBytes(key, out byte[] val);
            return val;
        }

        public PlayObject GetPlayObject(object key) {
            TryGetPlayObject(key, out PlayObject val);
            return val;
        }

        public PlayArray GetPlayArray(object key) {
            TryGetPlayArray(key, out PlayArray val);
            return val;
        }
    }
}
