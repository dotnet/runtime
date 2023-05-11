// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal sealed class RangeByteSearchValues : SearchValues<byte>
    {
        private readonly byte _lowInclusive, _highInclusive;
        private readonly uint _lowUint, _highMinusLow;

        public RangeByteSearchValues(byte lowInclusive, byte highInclusive)
        {
            (_lowInclusive, _highInclusive) = (lowInclusive, highInclusive);
            _lowUint = lowInclusive;
            _highMinusLow = (uint)(highInclusive - lowInclusive);
        }

        internal override byte[] GetValues()
        {
            byte[] values = new byte[_highMinusLow + 1];

            int low = _lowInclusive;
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (byte)(low + i);
            }

            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(byte value) =>
            value - _lowUint <= _highMinusLow;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<byte> span) =>
            span.IndexOfAnyInRange(_lowInclusive, _highInclusive);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<byte> span) =>
            span.IndexOfAnyExceptInRange(_lowInclusive, _highInclusive);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<byte> span) =>
            span.LastIndexOfAnyInRange(_lowInclusive, _highInclusive);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<byte> span) =>
            span.LastIndexOfAnyExceptInRange(_lowInclusive, _highInclusive);
    }
}
