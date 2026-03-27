// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics.Hashing;

namespace SourceGenerators
{
    /// <summary>
    /// Provides an immutable dictionary implementation which implements structural equality
    /// and guarantees deterministic enumeration order via sorted backing arrays.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class ImmutableEquatableDictionary<TKey, TValue> :
        IEquatable<ImmutableEquatableDictionary<TKey, TValue>>,
        IReadOnlyDictionary<TKey, TValue>
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TValue : IEquatable<TValue>
    {
        public static ImmutableEquatableDictionary<TKey, TValue> Empty { get; } = new([], []);

        private readonly TKey[] _keys;
        private readonly TValue[] _values;

        private ImmutableEquatableDictionary(TKey[] keys, TValue[] values)
        {
            Debug.Assert(keys.Length == values.Length);
            _keys = keys;
            _values = values;
        }

        public int Count => _keys.Length;

        public bool ContainsKey(TKey key) => Array.BinarySearch(_keys, key) >= 0;

        public bool TryGetValue(TKey key, out TValue value)
        {
            int index = Array.BinarySearch(_keys, key);
            if (index >= 0)
            {
                value = _values[index];
                return true;
            }

            value = default!;
            return false;
        }

        public TValue this[TKey key]
        {
            get
            {
                int index = Array.BinarySearch(_keys, key);
                if (index < 0)
                {
                    throw new KeyNotFoundException();
                }

                return _values[index];
            }
        }

        public IEnumerable<TKey> Keys => _keys;
        public IEnumerable<TValue> Values => _values;

        public bool Equals(ImmutableEquatableDictionary<TKey, TValue>? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (_keys.Length != other._keys.Length)
            {
                return false;
            }

            for (int i = 0; i < _keys.Length; i++)
            {
                if (!_keys[i].Equals(other._keys[i]) ||
                    !EqualityComparer<TValue>.Default.Equals(_values[i], other._values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj)
            => obj is ImmutableEquatableDictionary<TKey, TValue> other && Equals(other);

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < _keys.Length; i++)
            {
                int keyHash = _keys[i].GetHashCode();
                int valueHash = _values[i] is null ? 0 : _values[i].GetHashCode();
                hash = HashHelpers.Combine(hash, HashHelpers.Combine(keyHash, valueHash));
            }

            return hash;
        }

        public Enumerator GetEnumerator() => new(_keys, _values);
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly TKey[] _keys;
            private readonly TValue[] _values;
            private int _index;

            internal Enumerator(TKey[] keys, TValue[] values)
            {
                _keys = keys;
                _values = values;
                _index = -1;
            }

            public bool MoveNext()
            {
                int newIndex = _index + 1;
                if ((uint)newIndex < (uint)_keys.Length)
                {
                    _index = newIndex;
                    return true;
                }

                return false;
            }

            public readonly KeyValuePair<TKey, TValue> Current => new(_keys[_index], _values[_index]);

            object IEnumerator.Current => Current;
            public void Reset() => _index = -1;
            public void Dispose() { }
        }

        internal static ImmutableEquatableDictionary<TKey, TValue> Create(Dictionary<TKey, TValue> source)
        {
            if (source.Count == 0)
            {
                return Empty;
            }

            TKey[] keys = new TKey[source.Count];
            TValue[] values = new TValue[source.Count];
            int i = 0;
            foreach (KeyValuePair<TKey, TValue> entry in source)
            {
                keys[i] = entry.Key;
                values[i] = entry.Value;
                i++;
            }

            Array.Sort(keys, values);

            return new(keys, values);
        }
    }

    internal static class ImmutableEquatableDictionary
    {
        public static ImmutableEquatableDictionary<TKey, TValue> ToImmutableEquatableDictionary<TKey, TValue>(
            this Hashtable source)
            where TKey : IEquatable<TKey>, IComparable<TKey>
            where TValue : IEquatable<TValue>
        {
            if (source.Count == 0)
            {
                return ImmutableEquatableDictionary<TKey, TValue>.Empty;
            }

            Dictionary<TKey, TValue> dict = new(source.Count);
            foreach (DictionaryEntry entry in source)
            {
                dict.Add((TKey)entry.Key, (TValue)entry.Value!);
            }

            return ImmutableEquatableDictionary<TKey, TValue>.Create(dict);
        }
    }
}
