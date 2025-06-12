// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace System.Buffers
{
    internal sealed class AsciiCharSearchValues<TOptimizations, TUniqueLowNibble> : SearchValues<char>
        where TOptimizations : struct, IndexOfAnyAsciiSearcher.IOptimizations
        where TUniqueLowNibble : struct, SearchValues.IRuntimeConst
    {
        private IndexOfAnyAsciiSearcher.AsciiState _state;

        public AsciiCharSearchValues(ReadOnlySpan<char> values)
        {
            // Despite the name being Ascii, this type may be used with non-ASCII values on ARM.
            // See IndexOfAnyAsciiSearcher.CanUseUniqueLowNibbleSearch.
            Debug.Assert(Ascii.IsValid(values) || (AdvSimd.IsSupported && TUniqueLowNibble.Value));

            if (TUniqueLowNibble.Value)
            {
                IndexOfAnyAsciiSearcher.ComputeUniqueLowNibbleState(values, out _state);
            }
            else
            {
                IndexOfAnyAsciiSearcher.ComputeAsciiState(values, out _state);
            }
        }

        internal override char[] GetValues() =>
            _state.Lookup.GetCharValues();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value) =>
            _state.Lookup.Contains256(value);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<char> span) =>
            IndexOfAnyAsciiSearcher.IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate, TOptimizations, TUniqueLowNibble>(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _state);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            IndexOfAnyAsciiSearcher.IndexOfAny<IndexOfAnyAsciiSearcher.Negate, TOptimizations, TUniqueLowNibble>(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _state);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            IndexOfAnyAsciiSearcher.LastIndexOfAny<IndexOfAnyAsciiSearcher.DontNegate, TOptimizations, TUniqueLowNibble>(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _state);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            IndexOfAnyAsciiSearcher.LastIndexOfAny<IndexOfAnyAsciiSearcher.Negate, TOptimizations, TUniqueLowNibble>(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _state);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsAny(ReadOnlySpan<char> span) =>
            IndexOfAnyAsciiSearcher.ContainsAny<IndexOfAnyAsciiSearcher.DontNegate, TOptimizations, TUniqueLowNibble>(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _state);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsAnyExcept(ReadOnlySpan<char> span) =>
            IndexOfAnyAsciiSearcher.ContainsAny<IndexOfAnyAsciiSearcher.Negate, TOptimizations, TUniqueLowNibble>(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _state);
    }
}
