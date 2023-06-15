// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    internal sealed class SingleCharSearchValues<TShouldUsePacked> : SearchValues<char>
        where TShouldUsePacked : struct, SearchValues.IRuntimeConst
    {
        private char _e0;

        public SingleCharSearchValues(char value) =>
            _e0 = value;

        internal override char[] GetValues() => new[] { _e0 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value) =>
            value == _e0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<char> span) =>
            (PackedSpanHelpers.PackedIndexOfIsSupported && TShouldUsePacked.Value)
                ? PackedSpanHelpers.IndexOf(ref MemoryMarshal.GetReference(span), _e0, span.Length)
                : SpanHelpers.NonPackedIndexOfValueType<short, SpanHelpers.DontNegate<short>>(
                    ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<char, short>(ref _e0),
                    span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            (PackedSpanHelpers.PackedIndexOfIsSupported && TShouldUsePacked.Value)
                ? PackedSpanHelpers.IndexOfAnyExcept(ref MemoryMarshal.GetReference(span), _e0, span.Length)
                : SpanHelpers.NonPackedIndexOfValueType<short, SpanHelpers.Negate<short>>(
                    ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<char, short>(ref _e0),
                    span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            span.LastIndexOf(_e0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            span.LastIndexOfAnyExcept(_e0);
    }
}
