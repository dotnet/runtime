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
    /// Provides an immutable set implementation which implements structural equality
    /// and guarantees deterministic enumeration order via a sorted backing array.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class ImmutableEquatableSet<T> :
        IEquatable<ImmutableEquatableSet<T>>,
        IReadOnlyCollection<T>
        where T : IEquatable<T>, IComparable<T>
    {
        public static ImmutableEquatableSet<T> Empty { get; } = new([]);

        private readonly T[] _values;

        private ImmutableEquatableSet(T[] values)
        {
            _values = values;
        }

        public int Count => _values.Length;
        public bool Contains(T item) => Array.BinarySearch(_values, item) >= 0;

        public bool Equals(ImmutableEquatableSet<T>? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return ((ReadOnlySpan<T>)_values).SequenceEqual(other._values);
        }

        public override bool Equals(object? obj)
            => obj is ImmutableEquatableSet<T> other && Equals(other);

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (T value in _values)
            {
                hash = HashHelpers.Combine(hash, value is null ? 0 : value.GetHashCode());
            }

            return hash;
        }

        public ImmutableEquatableArray<T>.Enumerator GetEnumerator() => new ImmutableEquatableArray<T>.Enumerator(_values);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        internal static ImmutableEquatableSet<T> Create(IEnumerable<T> values)
        {
            HashSet<T> set = new(values);
            if (set.Count == 0)
            {
                return Empty;
            }

            T[] array = new T[set.Count];
            set.CopyTo(array);
            Array.Sort(array);

            return new(array);
        }
    }

    internal static class ImmutableEquatableSet
    {
        public static ImmutableEquatableSet<T> ToImmutableEquatableSet<T>(this IEnumerable<T> values)
            where T : IEquatable<T>, IComparable<T>
            => ImmutableEquatableSet<T>.Create(values);
    }
}
