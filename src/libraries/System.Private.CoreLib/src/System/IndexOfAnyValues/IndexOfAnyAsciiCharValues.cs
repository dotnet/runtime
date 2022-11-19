// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Buffers
{
    internal sealed class IndexOfAnyAsciiCharValues<TOptimizations> : IndexOfAnyValues<char>
        where TOptimizations : struct, IndexOfAnyAsciiSearcher.IOptimizations
    {
        private readonly Vector128<byte> _bitmap;
        private readonly BitVector256 _lookup;

        public IndexOfAnyAsciiCharValues(Vector128<byte> bitmap, BitVector256 lookup)
        {
            _bitmap = bitmap;
            _lookup = lookup;
        }

        internal override char[] GetValues() => _lookup.GetCharValues();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<char> span) =>
            IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            IndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            LastIndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            LastIndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            return IndexOfAnyAsciiSearcher.IsVectorizationSupported && searchSpaceLength >= Vector128<short>.Count
                ? IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<TNegator, TOptimizations>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, _bitmap)
                : IndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int LastIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            return IndexOfAnyAsciiSearcher.IsVectorizationSupported && searchSpaceLength >= Vector128<short>.Count
                ? IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<TNegator, TOptimizations>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, _bitmap)
                : LastIndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
        }

        private int IndexOfAnyScalar<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = 0; i < searchSpaceLength; i++)
            {
                if (TNegator.NegateIfNeeded(_lookup.Contains128(Unsafe.Add(ref searchSpace, i))))
                {
                    return i;
                }
            }

            return -1;
        }

        private int LastIndexOfAnyScalar<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = searchSpaceLength - 1; i >= 0; i--)
            {
                if (TNegator.NegateIfNeeded(_lookup.Contains128(Unsafe.Add(ref searchSpace, i))))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
