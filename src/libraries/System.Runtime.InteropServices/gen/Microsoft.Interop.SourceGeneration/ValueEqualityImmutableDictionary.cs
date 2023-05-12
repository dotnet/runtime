// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.Interop
{
    public record struct ValueEqualityImmutableDictionary<T, U>(ImmutableDictionary<T, U> Map) : IDictionary<T, U>
    {
        public bool Equals(ValueEqualityImmutableDictionary<T, U> other)
        {
            if (Count != other.Count)
            {
                return false;
            }

            foreach(var kvp in this)
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
            int hash = 17;
            foreach(var value in Map)
            {
                hash = Hash.Combine(hash, value.Key.GetHashCode());
                hash = Hash.Combine(hash, value.Value.GetHashCode());
            }
            return hash;
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

    public static partial class CollectionExtensions
    {
        public static ValueEqualityImmutableDictionary<TKey, TValue> ToValueEqualityImmutableDictionary<TSrc, TKey, TValue>(this IEnumerable<TSrc> srcs, Func<TSrc, TKey> keyMap, Func<TSrc, TValue> valueMap)
        {
            return new(srcs.ToImmutableDictionary(keyMap, valueMap));
        }

        public static ValueEqualityImmutableDictionary<TKey, TValue> ToValueEqual<TKey, TValue>(this ImmutableDictionary<TKey, TValue> dict)
        {
            return new(dict);
        }

        public static ValueEqualityImmutableDictionary<TKey, TValue> ToValueEqualImmutable<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        {
            return new(dict.ToImmutableDictionary());
        }
    }
}
