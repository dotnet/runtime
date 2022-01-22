// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic
{
    internal interface IInternalReadOnlySpanEqualityComparer<T>
    {
        bool Equals(ReadOnlySpan<T> x, ReadOnlySpan<T> y) => MemoryExtensions.SequenceEqual(x, y);
        int GetHashCode(ReadOnlySpan<T> obj) => obj.Length;
    }
}
