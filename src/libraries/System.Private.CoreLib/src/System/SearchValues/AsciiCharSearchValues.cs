// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Buffers
{
    internal sealed class AsciiCharSearchValues<TOptimizations> : SearchValues<char>
        where TOptimizations : struct, IndexOfAnyAsciiSearcher.IOptimizations
    {
        private Vector256<byte> _bitmap;
        private readonly BitVector256 _lookup;

        public AsciiCharSearchValues(Vector256<byte> bitmap, BitVector256 lookup)
        {
            _bitmap = bitmap;
            _lookup = lookup;
        }

        internal override char[] GetValues() => _lookup.GetCharValues();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value) =>
            _lookup.Contains128(value);

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
                ? IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<TNegator, TOptimizations>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, ref _bitmap)
                : IndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int LastIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            return IndexOfAnyAsciiSearcher.IsVectorizationSupported && searchSpaceLength >= Vector128<short>.Count
                ? IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<TNegator, TOptimizations>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, ref _bitmap)
                : LastIndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
        }

        private int IndexOfAnyScalar<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            ref char searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
            ref char cur = ref searchSpace;

            while (!Unsafe.AreSame(ref cur, ref searchSpaceEnd))
            {
                char c = cur;
                if (TNegator.NegateIfNeeded(_lookup.Contains128(c)))
                {
                    return (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref cur) / sizeof(char));
                }

                cur = ref Unsafe.Add(ref cur, 1);
            }

            return -1;
        }

        private int LastIndexOfAnyScalar<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = searchSpaceLength - 1; i >= 0; i--)
            {
                char c = Unsafe.Add(ref searchSpace, i);
                if (TNegator.NegateIfNeeded(_lookup.Contains128(c)))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
