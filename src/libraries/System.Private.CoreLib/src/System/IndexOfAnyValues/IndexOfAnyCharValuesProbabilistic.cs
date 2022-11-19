// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    internal sealed class IndexOfAnyCharValuesProbabilistic<TContains> : IndexOfAnyValues<char>
        where TContains : struct, IndexOfAnyValues.IStringContains
    {
        private readonly ProbabilisticMap _map;
        private readonly string _values;

        public unsafe IndexOfAnyCharValuesProbabilistic(ReadOnlySpan<char> values)
        {
            _values = new string(values);

            ProbabilisticMap map = default;
            ProbabilisticMap.Initialize((uint*)&map, _values);
            _map = map;
        }

        internal override char[] GetValues() => _values.ToCharArray();

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
            string values = _values;

            for (int i = 0; i < searchSpaceLength; i++)
            {
                int ch = Unsafe.Add(ref searchSpace, i);
                if (TNegator.NegateIfNeeded(
                        _map.IsCharBitSet((byte)ch) &&
                        _map.IsCharBitSet((byte)(ch >> 8)) &&
                        TContains.Contains(values, (char)ch)))
                {
                    return i;
                }
            }

            return -1;
        }

        private int LastIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            string values = _values;

            for (int i = searchSpaceLength - 1; i >= 0; i--)
            {
                int ch = Unsafe.Add(ref searchSpace, i);
                if (TNegator.NegateIfNeeded(
                        _map.IsCharBitSet((byte)ch) &&
                        _map.IsCharBitSet((byte)(ch >> 8)) &&
                        TContains.Contains(values, (char)ch)))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
