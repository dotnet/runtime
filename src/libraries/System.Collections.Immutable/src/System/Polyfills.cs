// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
#if !NET
    internal interface IReadOnlySet<T> : IReadOnlyCollection<T>
    {
        bool Contains(T item);
        bool IsProperSubsetOf(IEnumerable<T> other);
        bool IsProperSupersetOf(IEnumerable<T> other);
        bool IsSubsetOf(IEnumerable<T> other);
        bool IsSupersetOf(IEnumerable<T> other);
        bool Overlaps(IEnumerable<T> other);
        bool SetEquals(IEnumerable<T> other);
    }
#endif
}

namespace System.Numerics
{
#if !NET
    internal static class BitOperations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RotateLeft(uint value, int offset) => (value << offset) | (value >> (32 - offset));
    }
#endif
}
