// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    internal sealed class ProbabilisticCharSearchValues : SearchValues<char>
    {
        private ProbabilisticMapState _map;

        public ProbabilisticCharSearchValues(ReadOnlySpan<char> values, int maxInclusive)
        {
            _map = new ProbabilisticMapState(values, maxInclusive);
        }

        internal override char[] GetValues() =>
            _map.GetValues();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value) =>
            _map.FastContains(value);

        internal override int IndexOfAny(ReadOnlySpan<char> span) =>
            ProbabilisticMap.IndexOfAny<SearchValues.TrueConst>(ref MemoryMarshal.GetReference(span), span.Length, ref _map);

        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            ProbabilisticMapState.IndexOfAnySimpleLoop<SearchValues.TrueConst, IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length, ref _map);

        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            ProbabilisticMap.LastIndexOfAny<SearchValues.TrueConst>(ref MemoryMarshal.GetReference(span), span.Length, ref _map);

        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            ProbabilisticMapState.LastIndexOfAnySimpleLoop<SearchValues.TrueConst, IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length, ref _map);
    }
}
