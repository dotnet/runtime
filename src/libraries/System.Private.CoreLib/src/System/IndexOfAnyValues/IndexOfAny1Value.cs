// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal sealed class IndexOfAny1Value<T> : IndexOfAnyValues<T>
        where T : struct, INumber<T>
    {
        private readonly T _e0;

        public IndexOfAny1Value(ReadOnlySpan<T> values)
        {
            Debug.Assert(values.Length == 1);
            _e0 = values[0];
        }

        internal override T[] GetValues() => new[] { _e0 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(T value) =>
            value == _e0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<T> span) =>
            span.IndexOf(_e0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<T> span) =>
            span.IndexOfAnyExcept(_e0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<T> span) =>
            span.LastIndexOf(_e0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<T> span) =>
            span.LastIndexOfAnyExcept(_e0);
    }
}
