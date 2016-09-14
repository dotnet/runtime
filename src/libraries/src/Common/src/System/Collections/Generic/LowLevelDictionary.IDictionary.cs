// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Generic
{
    internal partial class LowLevelDictionary<TKey, TValue> : IDictionary
    {
        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;

        object IDictionary.this[object key]
        {
            get
            {
                Entry e = Find((TKey)key);
                return e != null ? (object)e._value : null;
            }
            set { this[(TKey)key] = (TValue)value; }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                if (_numEntries == 0)
                {
                    return Array.Empty<TKey>();
                }

                var keys = new TKey[_numEntries];
                int dst = 0;
                for (int bucket = 0; bucket < _buckets.Length; bucket++)
                {
                    for (Entry entry = _buckets[bucket]; entry != null; entry = entry._next)
                    {
                        keys[dst++] = entry._key;
                    }
                }
                return keys;
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                if (_numEntries == 0)
                {
                    return Array.Empty<TValue>();
                }

                var values = new TValue[_numEntries];
                int dst = 0;
                for (int bucket = 0; bucket < _buckets.Length; bucket++)
                {
                    for (Entry entry = _buckets[bucket]; entry != null; entry = entry._next)
                    {
                        values[dst++] = entry._value;
                    }
                }
                return values;
            }
        }

        public LowLevelDictionary<TKey, TValue> Clone()
        {
            var result = new LowLevelDictionary<TKey, TValue>(_numEntries);
            for (int bucket = 0; bucket < _buckets.Length; bucket++)
            {
                for (Entry entry = _buckets[bucket]; entry != null; entry = entry._next)
                {
                    result.Add(entry._key, entry._value);
                }
            }
            return result;
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        void IDictionary.Add(object key, object value) => Add((TKey)key, (TValue)value);

        bool IDictionary.Contains(object key) => Find((TKey)key) != null;

        IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new DictionaryEnumerator(this);

        void IDictionary.Remove(object key) { Remove((TKey)key); }

        void IDictionary.Clear() => Clear();

        void ICollection.CopyTo(Array array, int index)
        {
            for (int bucket = 0; bucket < _buckets.Length; bucket++)
            {
                for (Entry entry = _buckets[bucket]; entry != null; entry = entry._next)
                {
                    array.SetValue(new DictionaryEntry(entry._key, entry._value), index++);
                }
            }
        }

        private sealed class DictionaryEnumerator : IDictionaryEnumerator
        {
            private readonly DictionaryEntry[] _entries;
            private int _pos = -1;

            internal DictionaryEnumerator(LowLevelDictionary<TKey, TValue> dict)
            {
                var entries = new DictionaryEntry[dict._numEntries];
                int dst = 0;
                for (int bucket = 0; bucket < dict._buckets.Length; bucket++)
                {
                    for (Entry entry = dict._buckets[bucket]; entry != null; entry = entry._next)
                    {
                        entries[dst++] = new DictionaryEntry(entry._key, entry._value);
                    }
                }
                _entries = entries;
            }

            public object Current => Entry;
            public object Key => Entry.Key;
            public object Value => Entry.Value;

            public DictionaryEntry Entry
            {
                get
                {
                    if (_pos < 0 || _pos >= _entries.Length)
                    {
                        throw new InvalidOperationException();
                    }
                    return _entries[_pos];
                }
            }

            public bool MoveNext()
            {
                if (_pos < _entries.Length)
                {
                    _pos++;
                }
                return _pos < _entries.Length;
            }

            public void Reset() { _pos = -1; }
        }
    }
}
