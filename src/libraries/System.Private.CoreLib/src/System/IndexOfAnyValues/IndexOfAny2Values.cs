// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal sealed class IndexOfAny2Values<T> : IndexOfAnyValues<T>
        where T : struct, IEquatable<T>
    {
        private readonly T _e0, _e1;

        public IndexOfAny2Values(ReadOnlySpan<T> values)
        {
            Debug.Assert(values.Length == 2);
            (_e0, _e1) = (values[0], values[1]);
        }

        internal override T[] GetValues() => new[] { _e0, _e1 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<T> span) =>
            span.IndexOfAny(_e0, _e1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<T> span) =>
            span.IndexOfAnyExcept(_e0, _e1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<T> span) =>
            span.LastIndexOfAny(_e0, _e1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<T> span) =>
            span.LastIndexOfAnyExcept(_e0, _e1);
    }
}
