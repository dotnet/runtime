// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Buffers
{
    internal sealed class IndexOfAnyByteValues : IndexOfAnyValues<byte>
    {
        private readonly Vector128<byte> _bitmap0;
        private readonly Vector128<byte> _bitmap1;
        private readonly BitVector256 _lookup;

        public IndexOfAnyByteValues(ReadOnlySpan<byte> values) =>
            IndexOfAnyAsciiSearcher.ComputeBitmap256(values, out _bitmap0, out _bitmap1, out _lookup);

        internal override byte[] GetValues() => _lookup.GetByteValues();

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
                ? IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<TNegator>(ref searchSpace, searchSpaceLength, _bitmap0, _bitmap1)
                : IndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int LastIndexOfAny<TNegator>(ref byte searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            return IndexOfAnyAsciiSearcher.IsVectorizationSupported && searchSpaceLength >= sizeof(ulong)
                ? IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<TNegator>(ref searchSpace, searchSpaceLength, _bitmap0, _bitmap1)
                : LastIndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
        }

        private int IndexOfAnyScalar<TNegator>(ref byte searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = 0; i < searchSpaceLength; i++)
            {
                if (TNegator.NegateIfNeeded(_lookup.Contains(Unsafe.Add(ref searchSpace, i))))
                {
                    return i;
                }
            }

            return -1;
        }

        private int LastIndexOfAnyScalar<TNegator>(ref byte searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = searchSpaceLength - 1; i >= 0; i--)
            {
                if (TNegator.NegateIfNeeded(_lookup.Contains(Unsafe.Add(ref searchSpace, i))))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
