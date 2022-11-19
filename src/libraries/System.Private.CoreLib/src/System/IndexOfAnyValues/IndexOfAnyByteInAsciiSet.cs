// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    internal sealed class IndexOfAnyByteInAsciiSet<TAsciiSet> : IndexOfAnyValues<byte>
        where TAsciiSet : struct, IndexOfAnyValues.IAsciiSet
    {
        public static readonly IndexOfAnyByteInAsciiSet<TAsciiSet> Instance = new();

        private IndexOfAnyByteInAsciiSet() { }

        internal override byte[] GetValues()
        {
            var bytes = new List<byte>();
            for (int i = 0; i < 128; i++)
            {
                if (TAsciiSet.Contains((char)i))
                {
                    bytes.Add((byte)i);
                }
            }
            return bytes.ToArray();
        }

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
        private static int IndexOfAny<TNegator>(ref byte searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            return IndexOfAnyAsciiSearcher.IsVectorizationSupported && searchSpaceLength >= sizeof(ulong)
                ? IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<TNegator, IndexOfAnyAsciiSearcher.Default>(ref searchSpace, searchSpaceLength, TAsciiSet.Bitmap)
                : IndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LastIndexOfAny<TNegator>(ref byte searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            return IndexOfAnyAsciiSearcher.IsVectorizationSupported && searchSpaceLength >= sizeof(ulong)
                ? IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<TNegator, IndexOfAnyAsciiSearcher.Default>(ref searchSpace, searchSpaceLength, TAsciiSet.Bitmap)
                : LastIndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
        }

        private static int IndexOfAnyScalar<TNegator>(ref byte searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = 0; i < searchSpaceLength; i++)
            {
                if (TNegator.NegateIfNeeded(TAsciiSet.Contains((char)Unsafe.Add(ref searchSpace, i))))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int LastIndexOfAnyScalar<TNegator>(ref byte searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = searchSpaceLength - 1; i >= 0; i--)
            {
                if (TNegator.NegateIfNeeded(TAsciiSet.Contains((char)Unsafe.Add(ref searchSpace, i))))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
