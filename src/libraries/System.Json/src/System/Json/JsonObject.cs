// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using JsonPair = System.Collections.Generic.KeyValuePair<string, System.Json.JsonValue>;
using JsonPairEnumerable = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Json.JsonValue>>;

namespace System.Json
{
    public class JsonObject : JsonValue, IDictionary<string, JsonValue>, ICollection<JsonPair>
    {
        // Use SortedDictionary to make result of ToString() deterministic
        private readonly SortedDictionary<string, JsonValue> _map;

        public JsonObject(params JsonPair[] items)
        {
            _map = new SortedDictionary<string, JsonValue>(StringComparer.Ordinal);

            if (items != null)
            {
                AddRange(items);
            }
        }

        public JsonObject(JsonPairEnumerable items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _map = new SortedDictionary<string, JsonValue>(StringComparer.Ordinal);
            AddRange(items);
        }

        public override int Count => _map.Count;

        public IEnumerator<JsonPair> GetEnumerator() => _map.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _map.GetEnumerator();

        public override sealed JsonValue this[string key]
        {
            get { return _map[key]; }
            set { _map[key] = value; }
        }

        public override JsonType JsonType => JsonType.Object;

        public ICollection<string> Keys => _map.Keys;

        public ICollection<JsonValue> Values => _map.Values;

        public void Add(string key, JsonValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _map.Add(key, value);
        }

        public void Add(JsonPair pair) => Add(pair.Key, pair.Value);

        public void AddRange(JsonPairEnumerable items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var pair in items)
            {
                _map.Add(pair.Key, pair.Value);
            }
        }

        public void AddRange(params JsonPair[] items) => AddRange((JsonPairEnumerable)items);

        public void Clear() => _map.Clear();

        bool ICollection<JsonPair>.Contains(JsonPair item) => (_map as ICollection<JsonPair>).Contains(item);

        bool ICollection<JsonPair>.Remove(JsonPair item) => (_map as ICollection<JsonPair>).Remove(item);

        public override bool ContainsKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _map.ContainsKey(key);
        }

        public void CopyTo(JsonPair[] array, int arrayIndex) => (_map as ICollection<JsonPair>).CopyTo(array, arrayIndex);

        public bool Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _map.Remove(key);
        }

        bool ICollection<JsonPair>.IsReadOnly => false;

        public override void Save(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            stream.WriteByte((byte)'{');

            foreach (JsonPair pair in _map)
            {
                stream.WriteByte((byte)'"');
                byte[] bytes = Encoding.UTF8.GetBytes(EscapeString(pair.Key));
                stream.Write(bytes, 0, bytes.Length);
                stream.WriteByte((byte)'"');
                stream.WriteByte((byte)',');
                stream.WriteByte((byte)' ');
                if (pair.Value == null)
                {
                    stream.WriteByte((byte)'n');
                    stream.WriteByte((byte)'u');
                    stream.WriteByte((byte)'l');
                    stream.WriteByte((byte)'l');
                }
                else
                {
                    pair.Value.Save(stream);
                }
            }

            stream.WriteByte((byte)'}');
        }

        public bool TryGetValue(string key, out JsonValue value) => _map.TryGetValue(key, out value);
    }
}
