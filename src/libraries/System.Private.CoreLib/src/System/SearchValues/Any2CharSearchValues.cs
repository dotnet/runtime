﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    internal sealed class Any2CharSearchValues<TShouldUsePacked> : SearchValues<char>
        where TShouldUsePacked : struct, SearchValues.IRuntimeConst
    {
        private char _e0, _e1;

        public Any2CharSearchValues(char value0, char value1) =>
            (_e0, _e1) = (value0, value1);

        internal override char[] GetValues() => new[] { _e0, _e1 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value) =>
            value == _e0 || value == _e1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<char> span) =>
            (PackedSpanHelpers.PackedIndexOfIsSupported && TShouldUsePacked.Value)
                ? PackedSpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), _e0, _e1, span.Length)
                : SpanHelpers.NonPackedIndexOfAnyValueType<short, SpanHelpers.DontNegate<short>>(
                    ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<char, short>(ref _e0),
                    Unsafe.As<char, short>(ref _e1),
                    span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            (PackedSpanHelpers.PackedIndexOfIsSupported && TShouldUsePacked.Value)
                ? PackedSpanHelpers.IndexOfAnyExcept(ref MemoryMarshal.GetReference(span), _e0, _e1, span.Length)
                : SpanHelpers.NonPackedIndexOfAnyValueType<short, SpanHelpers.Negate<short>>(
                    ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<char, short>(ref _e0),
                    Unsafe.As<char, short>(ref _e1),
                    span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            span.LastIndexOfAny(_e0, _e1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            span.LastIndexOfAnyExcept(_e0, _e1);
    }
}
