// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SourceGenerators
{
    /// <summary>
    /// Provides an immutable set implementation which implements structural equality.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class ImmutableEquatableSet<T> :
        IEquatable<ImmutableEquatableSet<T>>,
        IReadOnlyCollection<T>
        where T : IEquatable<T>
    {
        public static ImmutableEquatableSet<T> Empty { get; } = new([]);

        private readonly HashSet<T> _values;

        private ImmutableEquatableSet(HashSet<T> values)
        {
            _values = values;
        }

        public int Count => _values.Count;
        public bool Contains(T item) => _values.Contains(item);

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

            if (_values.Count != other._values.Count)
            {
                return false;
            }

            foreach (T value in _values)
            {
                if (!other._values.Contains(value))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj)
            => obj is ImmutableEquatableSet<T> other && Equals(other);

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (T value in _values)
            {
                hash ^= value is null ? 0 : value.GetHashCode();
            }

            return hash;
        }

        public HashSet<T>.Enumerator GetEnumerator() => _values.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        internal static ImmutableEquatableSet<T> UnsafeCreateFromHashSet(HashSet<T> values)
            => new(values);
    }

    internal static class ImmutableEquatableSet
    {
        public static ImmutableEquatableSet<T> ToImmutableEquatableSet<T>(this IEnumerable<T> values) where T : IEquatable<T>
            => values is ICollection<T> { Count: 0 }
                ? ImmutableEquatableSet<T>.Empty
                : ImmutableEquatableSet<T>.UnsafeCreateFromHashSet(new(values));
    }
}
