// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Interop
{
    /// <summary>
    /// This method provides a wrapper for an <see cref="ImmutableArray{T}" /> that overrides the equality operation to provide elementwise comparison.
    /// The default equality operation for an <see cref="ImmutableArray{T}" /> is reference equality of the underlying array, which is too strict
    /// for many scenarios. This wrapper type allows us to use <see cref="ImmutableArray{T}" />s in our other record types without having to write an Equals method
    /// that we may forget to update if we add new elements to the record.
    /// </summary>
    public readonly record struct SequenceEqualImmutableArray<T>(ImmutableArray<T> Array, IEqualityComparer<T> Comparer) : IList<T>
    {
        public SequenceEqualImmutableArray(ImmutableArray<T> array)
            : this(array, EqualityComparer<T>.Default)
        {
        }

        public T this[int index] { get => ((IList<T>)Array)[index]; set => ((IList<T>)Array)[index] = value; }

        public int Count => ((ICollection<T>)Array).Count;

        public bool IsReadOnly => ((ICollection<T>)Array).IsReadOnly;

        public void Add(T item) => ((ICollection<T>)Array).Add(item);
        public void Clear() => ((ICollection<T>)Array).Clear();
        public bool Contains(T item) => Array.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => Array.CopyTo(array, arrayIndex);

        public bool Equals(SequenceEqualImmutableArray<T> other)
        {
            return Array.SequenceEqual(other.Array, Comparer);
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Array).GetEnumerator();
        public override int GetHashCode() => throw new UnreachableException();
        public int IndexOf(T item) => Array.IndexOf(item);
        public void Insert(int index, T item) => ((IList<T>)Array).Insert(index, item);
        public bool Remove(T item) => ((ICollection<T>)Array).Remove(item);
        public void RemoveAt(int index) => ((IList<T>)Array).RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Array).GetEnumerator();
    }

    public static class IEnumerableSequenceEqualImmutableArrayExtensions
    {
        public static SequenceEqualImmutableArray<T> ToSequenceEqualImmutableArray<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return new(source.ToImmutableArray(), comparer);
        }
        public static SequenceEqualImmutableArray<T> ToSequenceEqualImmutableArray<T>(this IEnumerable<T> source)
        {
            return new(source.ToImmutableArray());
        }
    }
}
