// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Buffers
{
    internal sealed class AnyByteSearchValues : SearchValues<byte>
    {
        private Vector512<byte> _bitmaps;
        private readonly BitVector256 _lookup;

        public AnyByteSearchValues(ReadOnlySpan<byte> values)
        {
            IndexOfAnyAsciiSearcher.ComputeBitmap256(values, out Vector256<byte> bitmap0, out Vector256<byte> bitmap1, out _lookup);
            _bitmaps = Vector512.Create(bitmap0, bitmap1);
        }

        internal override byte[] GetValues() => _lookup.GetByteValues();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(byte value) =>
            _lookup.Contains(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<byte> span) =>
            IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<byte> span) =>
            IndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<byte> span) =>
            LastIndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<byte> span) =>
            LastIndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexOfAny<TNegator>(ref byte searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            return IndexOfAnyAsciiSearcher.IsVectorizationSupported && searchSpaceLength >= sizeof(ulong)
                ? IndexOfAnyAsciiSearcher.IndexOfAnyVectorizedAnyByte<TNegator>(ref searchSpace, searchSpaceLength, ref _bitmaps)
                : IndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int LastIndexOfAny<TNegator>(ref byte searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            return IndexOfAnyAsciiSearcher.IsVectorizationSupported && searchSpaceLength >= sizeof(ulong)
                ? IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorizedAnyByte<TNegator>(ref searchSpace, searchSpaceLength, ref _bitmaps)
                : LastIndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
        }

        private int IndexOfAnyScalar<TNegator>(ref byte searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            ref byte searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
            ref byte cur = ref searchSpace;

            while (!Unsafe.AreSame(ref cur, ref searchSpaceEnd))
            {
                byte b = cur;
                if (TNegator.NegateIfNeeded(_lookup.Contains(b)))
                {
                    return (int)Unsafe.ByteOffset(ref searchSpace, ref cur);
                }

                cur = ref Unsafe.Add(ref cur, 1);
            }

            return -1;
        }

        private int LastIndexOfAnyScalar<TNegator>(ref byte searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = searchSpaceLength - 1; i >= 0; i--)
            {
                byte b = Unsafe.Add(ref searchSpace, i);
                if (TNegator.NegateIfNeeded(_lookup.Contains(b)))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
