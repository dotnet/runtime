// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics.Hashing;

namespace SourceGenerators
{
    /// <summary>
    /// Provides an immutable dictionary implementation which implements structural equality.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class ImmutableEquatableDictionary<TKey, TValue> :
        IEquatable<ImmutableEquatableDictionary<TKey, TValue>>,
        IReadOnlyDictionary<TKey, TValue>
        where TKey : IEquatable<TKey>
        where TValue : IEquatable<TValue>
    {
        public static ImmutableEquatableDictionary<TKey, TValue> Empty { get; } = new([]);

        private readonly Dictionary<TKey, TValue> _values;

        private ImmutableEquatableDictionary(Dictionary<TKey, TValue> values)
        {
            _values = values;
        }

        public int Count => _values.Count;
        public bool ContainsKey(TKey key) => _values.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _values.TryGetValue(key, out value!);
        public TValue this[TKey key] => _values[key];
        public IEnumerable<TKey> Keys => _values.Keys;
        public IEnumerable<TValue> Values => _values.Values;

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

            if (_values.Count != other._values.Count)
            {
                return false;
            }

            foreach (KeyValuePair<TKey, TValue> entry in _values)
            {
                if (!other._values.TryGetValue(entry.Key, out TValue? otherValue) ||
                    !entry.Value.Equals(otherValue))
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
            foreach (KeyValuePair<TKey, TValue> entry in _values)
            {
                int keyHash = entry.Key.GetHashCode();
                int valueHash = entry.Value is null ? 0 : entry.Value.GetHashCode();
                hash ^= HashHelpers.Combine(keyHash, valueHash);
            }

            return hash;
        }

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => _values.GetEnumerator();
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => _values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        internal static ImmutableEquatableDictionary<TKey, TValue> UnsafeCreateFromDictionary(Dictionary<TKey, TValue> values)
            => new(values);
    }

    internal static class ImmutableEquatableDictionary
    {
        public static ImmutableEquatableDictionary<TKey, TValue> ToImmutableEquatableDictionary<TSource, TKey, TValue>(
            this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
            where TKey : IEquatable<TKey>
            where TValue : IEquatable<TValue>
        {
            Dictionary<TKey, TValue> dict = source.ToDictionary(keySelector, valueSelector);
            return dict.Count == 0
                ? ImmutableEquatableDictionary<TKey, TValue>.Empty
                : ImmutableEquatableDictionary<TKey, TValue>.UnsafeCreateFromDictionary(dict);
        }

        public static ImmutableEquatableDictionary<TKey, TValue> ToImmutableEquatableDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source)
            where TKey : IEquatable<TKey>
            where TValue : IEquatable<TValue>
        {
            Dictionary<TKey, TValue> dict = source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return dict.Count == 0
                ? ImmutableEquatableDictionary<TKey, TValue>.Empty
                : ImmutableEquatableDictionary<TKey, TValue>.UnsafeCreateFromDictionary(dict);
        }
    }
}
