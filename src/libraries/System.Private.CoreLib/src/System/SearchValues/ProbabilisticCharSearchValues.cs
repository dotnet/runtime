// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Buffers
{
    internal sealed class ProbabilisticCharSearchValues : SearchValues<char>
    {
        private ProbabilisticMap _map;
        private readonly string _values;

        public ProbabilisticCharSearchValues(scoped ReadOnlySpan<char> values)
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

        internal override int IndexOfAny(ReadOnlySpan<char> span) =>
            ProbabilisticMap.IndexOfAny(ref Unsafe.As<ProbabilisticMap, uint>(ref _map), ref MemoryMarshal.GetReference(span), span.Length, _values);

        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            ProbabilisticMap.IndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length, _values);

        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            ProbabilisticMap.LastIndexOfAny(ref Unsafe.As<ProbabilisticMap, uint>(ref _map), ref MemoryMarshal.GetReference(span), span.Length, _values);

        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            ProbabilisticMap.LastIndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length, _values);
    }
}
