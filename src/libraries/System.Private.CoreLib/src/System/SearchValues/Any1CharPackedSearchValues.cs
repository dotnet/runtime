// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers
{
    internal sealed class Any1CharPackedSearchValues : SearchValues<char>
    {
        private readonly char _e0;

        public Any1CharPackedSearchValues(char value) =>
            _e0 = value;

        internal override char[] GetValues() =>
            [_e0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value) =>
            value == _e0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        internal override int IndexOfAny(ReadOnlySpan<char> span) =>
            PackedSpanHelpers.IndexOf(ref MemoryMarshal.GetReference(span), _e0, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            PackedSpanHelpers.IndexOfAnyExcept(ref MemoryMarshal.GetReference(span), _e0, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            span.LastIndexOf(_e0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            span.LastIndexOfAnyExcept(_e0);
    }
}
