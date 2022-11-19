// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    internal sealed class IndexOfAnyLatin1CharValues : IndexOfAnyValues<char>
    {
        private readonly BitVector256 _lookup;

        public IndexOfAnyLatin1CharValues(ReadOnlySpan<char> values)
        {
            foreach (char c in values)
            {
                if (c > 255)
                {
                    // The values were modified concurrent with the call to IndexOfAnyValues.Create
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                _lookup.Set(c);
            }
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

        private int IndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = 0; i < searchSpaceLength; i++)
            {
                if (TNegator.NegateIfNeeded(_lookup.Contains256(Unsafe.Add(ref searchSpace, i))))
                {
                    return i;
                }
            }

            return -1;
        }

        private int LastIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = searchSpaceLength - 1; i >= 0; i--)
            {
                if (TNegator.NegateIfNeeded(_lookup.Contains256(Unsafe.Add(ref searchSpace, i))))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
