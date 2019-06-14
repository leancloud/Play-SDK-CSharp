using System.Collections;
using System.Collections.Generic;

namespace LeanCloud.Play {
    public class PlayArray : IList<object> {
        internal List<object> Data {
            get; private set;
        }

        public int Count => ((IList<object>)Data).Count;

        public bool IsReadOnly => ((IList<object>)Data).IsReadOnly;

        public object this[int index] { get => ((IList<object>)Data)[index]; set => ((IList<object>)Data)[index] = value; }

        public int IndexOf(object item) {
            return ((IList<object>)Data).IndexOf(item);
        }

        public void Insert(int index, object item) {
            ((IList<object>)Data).Insert(index, item);
        }

        public void RemoveAt(int index) {
            ((IList<object>)Data).RemoveAt(index);
        }

        public void Add(object item) {
            ((IList<object>)Data).Add(item);
        }

        public void Clear() {
            ((IList<object>)Data).Clear();
        }

        public bool Contains(object item) {
            return ((IList<object>)Data).Contains(item);
        }

        public void CopyTo(object[] array, int arrayIndex) {
            ((IList<object>)Data).CopyTo(array, arrayIndex);
        }

        public bool Remove(object item) {
            return ((IList<object>)Data).Remove(item);
        }

        public IEnumerator<object> GetEnumerator() {
            return ((IList<object>)Data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IList<object>)Data).GetEnumerator();
        }

        public PlayArray() {
            Data = new List<object>();
        }

        public PlayArray(int capacity) {
            Data = new List<object>(capacity);
        }

        public static PlayArray ToPlayArray(List<object> data) {
            if (data == null) {
                return null;
            }
            var playArray = new PlayArray {
                Data = data
            };
            return playArray;
        }
    }
}
