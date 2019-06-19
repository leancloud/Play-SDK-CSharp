using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LeanCloud.Play {
    public class PlayArray : IList {
        internal IList Data {
            get; private set;
        }

        public bool IsFixedSize => Data.IsFixedSize;

        public bool IsReadOnly => Data.IsReadOnly;

        public int Count => Data.Count;

        public bool IsSynchronized => Data.IsSynchronized;

        public object SyncRoot => Data.SyncRoot;

        public object this[int index] { get => Data[index]; set => Data[index] = value; }

        public PlayArray(int capacity) {
            Data = new List<object>(capacity);
        }

        public PlayArray() : this(0) {

        }

        public PlayArray(IList data) : this() {
            if (data != null) {
                foreach (var d in data) {
                    Data.Add(d);
                }
            }
        }

        public int Add(object value) {
            return Data.Add(value);
        }

        public void Clear() {
            Data.Clear();
        }

        public bool Contains(object value) {
            return Data.Contains(value);
        }

        public int IndexOf(object value) {
            return Data.IndexOf(value);
        }

        public void Insert(int index, object value) {
            Data.Insert(index, value);
        }

        public void Remove(object value) {
            Data.Remove(value);
        }

        public void RemoveAt(int index) {
            Data.RemoveAt(index);
        }

        public void CopyTo(Array array, int index) {
            Data.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator() {
            return Data.GetEnumerator();
        }
    }
}
