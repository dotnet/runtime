// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.Interop
{
    public record struct ValueEqualityImmutableDictionary<T, U>(ImmutableDictionary<T, U> Map) : IDictionary<T, U>
    {
        // Since this is immutable, we can cache the hash, which requires sorting and is expensive to calculate
        private int? _hash = null;

        public bool Equals(ValueEqualityImmutableDictionary<T, U> other)
        {
            if (Count != other.Count)
            {
                return false;
            }

            foreach (var kvp in this)
            {
                if (!other.TryGetValue(kvp.Key, out var value) || !kvp.Value.Equals(value))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            if (_hash.HasValue)
                return _hash.Value;

            _hash = 0;
            foreach (var kvp in Map.ToImmutableArray().Sort())
            {
                _hash = HashCode.Combine(_hash, kvp.Key, kvp.Value);
            }
            return _hash.Value;
        }

        public U this[T key] { get => ((IDictionary<T, U>)Map)[key]; set => ((IDictionary<T, U>)Map)[key] = value; }
        public ICollection<T> Keys => ((IDictionary<T, U>)Map).Keys;
        public ICollection<U> Values => ((IDictionary<T, U>)Map).Values;
        public int Count => Map.Count;
        public bool IsReadOnly => ((ICollection<KeyValuePair<T, U>>)Map).IsReadOnly;
        public bool Contains(KeyValuePair<T, U> item) => Map.Contains(item);
        public bool ContainsKey(T key) => Map.ContainsKey(key);
        public void CopyTo(KeyValuePair<T, U>[] array, int arrayIndex) => ((ICollection<KeyValuePair<T, U>>)Map).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<T, U>> GetEnumerator() => ((IEnumerable<KeyValuePair<T, U>>)Map).GetEnumerator();
        public bool Remove(T key) => ((IDictionary<T, U>)Map).Remove(key);
        public bool Remove(KeyValuePair<T, U> item) => ((ICollection<KeyValuePair<T, U>>)Map).Remove(item);
        public bool TryGetValue(T key, out U value) => Map.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Map).GetEnumerator();
        public void Add(T key, U value) => ((IDictionary<T, U>)Map).Add(key, value);
        public void Add(KeyValuePair<T, U> item) => ((ICollection<KeyValuePair<T, U>>)Map).Add(item);
        public void Clear() => ((ICollection<KeyValuePair<T, U>>)Map).Clear();
    }

    public static class ValueEqualityImmutableDictionaryHelperExtensions
    {
        public static ValueEqualityImmutableDictionary<TKey, TValue> ToValueEqualityImmutableDictionary<TSource, TKey, TValue>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector)
        {
            return new ValueEqualityImmutableDictionary<TKey, TValue>(source.ToImmutableDictionary(keySelector, valueSelector));
        }
        public static ValueEqualityImmutableDictionary<TKey, TValue> ToValueEquals<TKey, TValue>(this ImmutableDictionary<TKey, TValue> source)
        {
            return new ValueEqualityImmutableDictionary<TKey, TValue>(source);
        }

    }
}
