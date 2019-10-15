// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace System.Json
{
    public class JsonArray : JsonValue, IList<JsonValue>
    {
        private readonly List<JsonValue> _list;

        public JsonArray(params JsonValue[] items)
        {
            _list = new List<JsonValue>();
            AddRange(items);
        }

        public JsonArray(IEnumerable<JsonValue> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _list = new List<JsonValue>(items);
        }

        public override int Count => _list.Count;

        public bool IsReadOnly => false;

        public override sealed JsonValue this[int index]
        {
            get { return _list[index]; }
            set { _list[index] = value; }
        }

        public override JsonType JsonType => JsonType.Array;

        public void Add(JsonValue item)
        {
            _list.Add(item);
        }

        public void AddRange(IEnumerable<JsonValue> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _list.AddRange(items);
        }

        public void AddRange(params JsonValue[] items)
        {
            if (items != null)
            {
                _list.AddRange(items);
            }
        }

        public void Clear() => _list.Clear();

        public bool Contains(JsonValue item) => _list.Contains(item);

        public void CopyTo(JsonValue[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public int IndexOf(JsonValue item) => _list.IndexOf(item);

        public void Insert(int index, JsonValue item) => _list.Insert(index, item);

        public bool Remove(JsonValue item) => _list.Remove(item);

        public void RemoveAt(int index) => _list.RemoveAt(index);

        public override void Save(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            stream.WriteByte((byte)'[');

            for (int i = 0; i < _list.Count; i++)
            {
                JsonValue v = _list[i];
                if (v != null)
                {
                    v.Save(stream);
                }
                else
                {
                    stream.WriteByte((byte)'n');
                    stream.WriteByte((byte)'u');
                    stream.WriteByte((byte)'l');
                    stream.WriteByte((byte)'l');
                }

                if (i < Count - 1)
                {
                    stream.WriteByte((byte)',');
                    stream.WriteByte((byte)' ');
                }
            }

            stream.WriteByte((byte)']');
        }

        IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
    }
}
