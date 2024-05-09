// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers
{
    internal sealed class Any2CharPackedIgnoreCaseSearchValues : SearchValues<char>
    {
        // While this most commonly applies to ASCII letters, it also works for other values that differ by 0x20 (e.g. "[]{}" => "{}").
        // _e0 and _e1 are therefore not necessarily lower case ASCII letters, but just the higher values (the ones with the 0x20 bit set).
        private readonly char _e0, _e1;
        private readonly uint _uint0, _uint1;
        private IndexOfAnyAsciiSearcher.AsciiState _state;

        public Any2CharPackedIgnoreCaseSearchValues(char value0, char value1)
        {
            Debug.Assert((value0 | 0x20) == value0 && char.IsAscii(value0));
            Debug.Assert((value1 | 0x20) == value1 && char.IsAscii(value1));

            (_e0, _e1) = (value0, value1);
            (_uint0, _uint1) = (value0, value1);
            IndexOfAnyAsciiSearcher.ComputeAsciiState([(char)(_e0 & ~0x20), _e0, (char)(_e1 & ~0x20), _e1], out _state);
        }

        internal override char[] GetValues() =>
            [(char)(_e0 & ~0x20), _e0, (char)(_e1 & ~0x20), _e1];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value)
        {
            uint lowerCase = (uint)(value | 0x20);
            return lowerCase == _uint0 || lowerCase == _uint1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        internal override int IndexOfAny(ReadOnlySpan<char> span) =>
            PackedSpanHelpers.IndexOfAnyIgnoreCase(ref MemoryMarshal.GetReference(span), _e0, _e1, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            PackedSpanHelpers.IndexOfAnyExceptIgnoreCase(ref MemoryMarshal.GetReference(span), _e0, _e1, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            IndexOfAnyAsciiSearcher.LastIndexOfAny<IndexOfAnyAsciiSearcher.DontNegate, IndexOfAnyAsciiSearcher.Default>(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            IndexOfAnyAsciiSearcher.LastIndexOfAny<IndexOfAnyAsciiSearcher.Negate, IndexOfAnyAsciiSearcher.Default>(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _state);
    }
}
