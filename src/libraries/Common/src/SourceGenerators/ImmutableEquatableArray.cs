// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Hashing;

namespace SourceGenerators
{
    /// <summary>
    /// Provides an immutable list implementation which implements sequence equality.
    /// </summary>
    public sealed class ImmutableEquatableArray<T> : IEquatable<ImmutableEquatableArray<T>>, IReadOnlyList<T>
        where T : IEquatable<T>
    {
        public static ImmutableEquatableArray<T> Empty { get; } = new ImmutableEquatableArray<T>(Array.Empty<T>());

        private readonly T[] _values;
        public T this[int index] => _values[index];
        public int Count => _values.Length;

        public ImmutableEquatableArray(IEnumerable<T> values)
            => _values = values.ToArray();

        public bool Equals(ImmutableEquatableArray<T>? other)
            => other != null && ((ReadOnlySpan<T>)_values).SequenceEqual(other._values);

        public override bool Equals(object? obj)
            => obj is ImmutableEquatableArray<T> other && Equals(other);

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (T value in _values)
            {
                hash = HashHelpers.Combine(hash, value is null ? 0 : value.GetHashCode());
            }

            return hash;
        }

        public Enumerator GetEnumerator() => new Enumerator(_values);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        public struct Enumerator
        {
            private readonly T[] _values;
            private int _index;

            internal Enumerator(T[] values)
            {
                _values = values;
                _index = -1;
            }

            public bool MoveNext()
            {
                int newIndex = _index + 1;

                if ((uint)newIndex < (uint)_values.Length)
                {
                    _index = newIndex;
                    return true;
                }

                return false;
            }

            public readonly T Current => _values[_index];
        }
    }

    internal static class ImmutableEquatableArray
    {
        public static ImmutableEquatableArray<T> ToImmutableEquatableArray<T>(this IEnumerable<T> values) where T : IEquatable<T>
            => new(values);
    }
}
