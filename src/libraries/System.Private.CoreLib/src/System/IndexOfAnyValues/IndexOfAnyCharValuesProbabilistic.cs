// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Buffers
{
    internal sealed class IndexOfAnyCharValuesProbabilistic : IndexOfAnyValues<char>
    {
        private ProbabilisticMap _map;
        private readonly string _values;

        public IndexOfAnyCharValuesProbabilistic(scoped ReadOnlySpan<char> values)
        {
            if (Vector128.IsHardwareAccelerated && values.Length < 8)
            {
                // ProbabilisticMap does a Span.Contains check to confirm potential matches.
                // If we have fewer than 8 values, pad them with existing ones to make the verification faster.
                Span<char> newValues = stackalloc char[8];
                newValues.Fill(values[0]);
                values.CopyTo(newValues);
                values = newValues;
            }

            _values = new string(values);
            _map = new ProbabilisticMap(_values);
        }

        internal override char[] GetValues() => _values.ToCharArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value) =>
            ProbabilisticMap.Contains(ref Unsafe.As<ProbabilisticMap, uint>(ref _map), _values, value);

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int IndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator =>
            ProbabilisticMap.IndexOfAny<TNegator>(ref Unsafe.As<ProbabilisticMap, uint>(ref _map), ref searchSpace, searchSpaceLength, _values);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int LastIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator =>
            ProbabilisticMap.LastIndexOfAny<TNegator>(ref Unsafe.As<ProbabilisticMap, uint>(ref _map), ref searchSpace, searchSpaceLength, _values);
    }
}
