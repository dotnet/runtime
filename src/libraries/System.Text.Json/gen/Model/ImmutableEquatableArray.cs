// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Provides an immutable list implementation which implements sequence equality.
    /// </summary>
    public sealed class ImmutableEquatableArray<T> : IEquatable<ImmutableEquatableArray<T>>, IReadOnlyList<T>
    {
        public static ImmutableEquatableArray<T> Empty { get; } = new ImmutableEquatableArray<T>(Array.Empty<T>());

        private readonly T[] _values;
        public T this[int index] => _values[index];
        public int Count => _values.Length;

        public ImmutableEquatableArray(IEnumerable<T> values)
            => _values = values.ToArray();

        public bool Equals(ImmutableEquatableArray<T>? other)
            => other != null && _values.SequenceEqual(other._values);

        public override bool Equals(object? obj)
            => obj is ImmutableEquatableArray<T> other && Equals(other);

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (T value in _values)
            {
                Combine(hash, value is null ? 0 : value.GetHashCode());
            }

            return hash;

            static int Combine(int h1, int h2)
            {
                // Taken from https://github.com/dotnet/runtime/blob/de4378f64d41ba82be05a0e62642b127a300151a/src/libraries/System.Private.CoreLib/src/System/Numerics/Hashing/HashHelpers.cs
                uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
                return ((int)rol5 + h1) ^ h2;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(_values);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();

        public struct Enumerator
        {
            private readonly T[] _values;
            private int _index;

            internal Enumerator(T[] values)
            {
                _values = values;
                _index = -1;
            }

            public bool MoveNext() => ++_index < _values.Length;
            public readonly T Current => _values[_index];
        }
    }

    internal static class ImmutableEquatableArray
    {
        public static ImmutableEquatableArray<T> ToImmutableEquatableArray<T>(this IEnumerable<T> values) => new(values);
        public static ImmutableEquatableArray<T> Create<T>(params T[] values) => new(values);
    }
}
