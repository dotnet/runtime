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
    internal sealed class Any3CharPackedIgnoreCaseSearchValues : SearchValues<char>
    {
        private readonly char _e0, _e1, _e2;
        private IndexOfAnyAsciiSearcher.AsciiState _state;

        public Any3CharPackedIgnoreCaseSearchValues(char value0, char value1, char value2)
        {
            Debug.Assert(value0 != 0 && (value0 | 0x20) == value0 && char.IsAscii(value0));
            Debug.Assert(value1 != 0 && (value1 | 0x20) == value1 && char.IsAscii(value1));
            Debug.Assert(value2 != 0 && (value2 | 0x20) == value2 && char.IsAscii(value2));

            (_e0, _e1, _e2) = (value0, value1, value2);
            IndexOfAnyAsciiSearcher.ComputeAsciiState([(char)(_e0 & ~0x20), _e0, (char)(_e1 & ~0x20), _e1, (char)(_e2 & ~0x20), _e2], out _state);
        }

        internal override char[] GetValues() =>
            [(char)(_e0 & ~0x20), _e0, (char)(_e1 & ~0x20), _e1, (char)(_e2 & ~0x20), _e2];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value)
        {
            value = (char)(value | 0x20);
            return value == _e0 || value == _e1 || value == _e2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        internal override int IndexOfAny(ReadOnlySpan<char> span) =>
            PackedSpanHelpers.IndexOfAnyIgnoreCase(ref MemoryMarshal.GetReference(span), _e0, _e1, _e2, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            PackedSpanHelpers.IndexOfAnyExceptIgnoreCase(ref MemoryMarshal.GetReference(span), _e0, _e1, _e2, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<IndexOfAnyAsciiSearcher.DontNegate, IndexOfAnyAsciiSearcher.Default>(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, IndexOfAnyAsciiSearcher.Default>(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _state);
    }
}
