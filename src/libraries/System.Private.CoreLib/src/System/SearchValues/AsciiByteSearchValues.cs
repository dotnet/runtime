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
    internal sealed class AsciiByteSearchValues<TUniqueLowNibble> : SearchValues<byte>
        where TUniqueLowNibble : struct, SearchValues.IRuntimeConst
    {
        private IndexOfAnyAsciiSearcher.AsciiState _state;

        public AsciiByteSearchValues(ReadOnlySpan<byte> values)
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

        internal override byte[] GetValues() =>
            _state.Lookup.GetByteValues();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(byte value) =>
            _state.Lookup.Contains(value);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<byte> span) =>
            IndexOfAnyAsciiSearcher.IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate, TUniqueLowNibble>(
                ref MemoryMarshal.GetReference(span), span.Length, ref _state);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<byte> span) =>
            IndexOfAnyAsciiSearcher.IndexOfAny<IndexOfAnyAsciiSearcher.Negate, TUniqueLowNibble>(
                ref MemoryMarshal.GetReference(span), span.Length, ref _state);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<byte> span) =>
            IndexOfAnyAsciiSearcher.LastIndexOfAny<IndexOfAnyAsciiSearcher.DontNegate, TUniqueLowNibble>(
                ref MemoryMarshal.GetReference(span), span.Length, ref _state);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<byte> span) =>
            IndexOfAnyAsciiSearcher.LastIndexOfAny<IndexOfAnyAsciiSearcher.Negate, TUniqueLowNibble>(
                ref MemoryMarshal.GetReference(span), span.Length, ref _state);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsAny(ReadOnlySpan<byte> span) =>
            IndexOfAnyAsciiSearcher.ContainsAny<IndexOfAnyAsciiSearcher.DontNegate, TUniqueLowNibble>(
                ref MemoryMarshal.GetReference(span), span.Length, ref _state);

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsAnyExcept(ReadOnlySpan<byte> span) =>
            IndexOfAnyAsciiSearcher.ContainsAny<IndexOfAnyAsciiSearcher.Negate, TUniqueLowNibble>(
                ref MemoryMarshal.GetReference(span), span.Length, ref _state);
    }
}
