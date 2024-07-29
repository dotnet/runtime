// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable CA1716
namespace Microsoft.Shared.Collections;
#pragma warning restore CA1716

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Static field, lifetime matches the process")]
internal sealed class EmptyReadOnlyList<T> : IReadOnlyList<T>, ICollection<T>
{
    public static readonly EmptyReadOnlyList<T> Instance = new();
    private readonly Enumerator _enumerator = new();

    public IEnumerator<T> GetEnumerator() => _enumerator;
    IEnumerator IEnumerable.GetEnumerator() => _enumerator;
    public int Count => 0;
    public T this[int index] => throw new ArgumentOutOfRangeException(nameof(index));

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        // nop
    }

    bool ICollection<T>.Contains(T item) => false;
    bool ICollection<T>.IsReadOnly => true;
    void ICollection<T>.Add(T item) => throw new NotSupportedException();
    bool ICollection<T>.Remove(T item) => false;

    void ICollection<T>.Clear()
    {
        // nop
    }

    internal sealed class Enumerator : IEnumerator<T>
    {
        public void Dispose()
        {
            // nop
        }

        public void Reset()
        {
            // nop
        }

        public bool MoveNext() => false;
        public T Current => throw new InvalidOperationException();
        object IEnumerator.Current => throw new InvalidOperationException();
    }
}
