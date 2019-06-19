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

        // 扩展方法
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

        public bool GetBool(int index) {
            return bool.Parse(Data[index].ToString());
        }

        public byte GetByte(int index) {
            return byte.Parse(Data[index].ToString());
        }

        public short GetShort(int index) {
            return short.Parse(Data[index].ToString());
        }

        public int GetInt(int index) {
            return int.Parse(Data[index].ToString());
        }

        public long GetLong(int index) {
            return long.Parse(Data[index].ToString());
        }

        public float GetFloat(int index) {
            return float.Parse(Data[index].ToString());
        }

        public double GetDouble(int index) {
            return double.Parse(Data[index].ToString());
        }

        public string GetString(int index) {
            return Data[index].ToString();
        }

        public byte[] GetBytes(int index) {
            return Data[index] as byte[];
        }

        public PlayObject GetPlayObject(int index) {
            return Data[index] as PlayObject;
        }

        public PlayArray GetPlayArray(int index) {
            return Data[index] as PlayArray;
        }
    }
}
