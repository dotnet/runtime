﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers
{
    internal sealed class Any1CharPackedIgnoreCaseSearchValues : SearchValues<char>
    {
        // While this most commonly applies to ASCII letters, it also works for other values that differ by 0x20 (e.g. "[{" => "{").
        // _lowerCase is therefore not necessarily a lower case ASCII letter, but just the higher value (the one with the 0x20 bit set).
        private readonly char _lowerCase, _upperCase;
        private readonly uint _lowerCaseUint;

        public Any1CharPackedIgnoreCaseSearchValues(char value)
        {
            Debug.Assert((value | 0x20) == value);

            _lowerCase = value;
            _upperCase = (char)(value & ~0x20);
            _lowerCaseUint = value;
        }

        internal override char[] GetValues() =>
            [_upperCase, _lowerCase];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value) =>
            (uint)(value | 0x20) == _lowerCaseUint;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        internal override int IndexOfAny(ReadOnlySpan<char> span) =>
            PackedSpanHelpers.IndexOfAnyIgnoreCase(ref MemoryMarshal.GetReference(span), _lowerCase, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            PackedSpanHelpers.IndexOfAnyExceptIgnoreCase(ref MemoryMarshal.GetReference(span), _lowerCase, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            span.LastIndexOfAny(_lowerCase, _upperCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            span.LastIndexOfAnyExcept(_lowerCase, _upperCase);
    }
}
