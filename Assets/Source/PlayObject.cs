using System.Collections;
using System.Collections.Generic;

namespace LeanCloud.Play {
    // 实现 IDictionary 接口
    public class PlayObject : IDictionary<string, object> {
        internal Dictionary<string, object> Data {
            get; private set;
        }

        public ICollection<string> Keys => ((IDictionary<string, object>)Data).Keys;

        public ICollection<object> Values => ((IDictionary<string, object>)Data).Values;

        public int Count => ((IDictionary<string, object>)Data).Count;

        public bool IsReadOnly => ((IDictionary<string, object>)Data).IsReadOnly;

        public object this[string key] { get => ((IDictionary<string, object>)Data)[key]; set => ((IDictionary<string, object>)Data)[key] = value; }

        public void Add(string key, object value) {
            ((IDictionary<string, object>)Data).Add(key, value);
        }

        public bool ContainsKey(string key) {
            return ((IDictionary<string, object>)Data).ContainsKey(key);
        }

        public bool Remove(string key) {
            return ((IDictionary<string, object>)Data).Remove(key);
        }

        public bool TryGetValue(string key, out object value) {
            return ((IDictionary<string, object>)Data).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, object> item) {
            ((IDictionary<string, object>)Data).Add(item);
        }

        public void Clear() {
            ((IDictionary<string, object>)Data).Clear();
        }

        public bool Contains(KeyValuePair<string, object> item) {
            return ((IDictionary<string, object>)Data).Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
            ((IDictionary<string, object>)Data).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, object> item) {
            return ((IDictionary<string, object>)Data).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
            return ((IDictionary<string, object>)Data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IDictionary<string, object>)Data).GetEnumerator();
        }

        public PlayObject() {
            Data = new Dictionary<string, object>();
        }

        public static PlayObject ToPlayObject(Dictionary<string, object> data) {
            if (data == null) {
                return null;
            }
            var playObject = new PlayObject {
                Data = data
            };
            return playObject;
        }


        // Getter
        public bool TryGetBool(string key, out bool val) {
            if (Data.TryGetValue(key, out object obj) && bool.TryParse(obj.ToString(), out val)) {
                return true;
            }
            val = false;
            return false;
        }

        public bool TryGetInt(string key, out int val) {
            if (Data.TryGetValue(key, out object obj) && int.TryParse(obj.ToString(), out val)) {
                return true;
            }
            val = 0;
            return false;
        }

        public bool TryGetFloat(string key, out float val) {
            if (Data.TryGetValue(key, out object obj) && float.TryParse(obj.ToString(), out val)) {
                return true;
            }
            val = 0f;
            return false;
        }

        public bool TryGetString(string key, out string val) {
            if (Data.TryGetValue(key, out object obj)) {
                val = (string)obj;
                return true;
            }
            val = null;
            return false;
        }

        public bool TryGetPlayObject(string key, out PlayObject val) {
            if (Data.TryGetValue(key, out object obj)) {
                val = (PlayObject)obj;
                return true;
            }
            val = null;
            return false;
        }

        public T GetObject<T>(string key) {
            return (T)Data[key];
        }
    }
}
