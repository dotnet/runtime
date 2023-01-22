// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal sealed class IndexOfAnyValuesInRange<T> : IndexOfAnyValues<T>
        where T : struct, INumber<T>
    {
        private readonly T _lowInclusive, _highInclusive;
        private readonly uint _lowUint, _highMinusLow;

        public IndexOfAnyValuesInRange(T lowInclusive, T highInclusive)
        {
            Debug.Assert(lowInclusive is byte or char);
            (_lowInclusive, _highInclusive) = (lowInclusive, highInclusive);
            _lowUint = uint.CreateChecked(lowInclusive);
            _highMinusLow = uint.CreateChecked(highInclusive - lowInclusive);
        }

        internal override T[] GetValues()
        {
            T[] values = new T[_highMinusLow + 1];

            T element = _lowInclusive;
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = element;
                element += T.One;
            }

            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(T value) =>
            uint.CreateChecked(value) - _lowUint <= _highMinusLow;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<T> span) =>
            span.IndexOfAnyInRange(_lowInclusive, _highInclusive);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<T> span) =>
            span.IndexOfAnyExceptInRange(_lowInclusive, _highInclusive);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<T> span) =>
            span.LastIndexOfAnyInRange(_lowInclusive, _highInclusive);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<T> span) =>
            span.LastIndexOfAnyExceptInRange(_lowInclusive, _highInclusive);
    }
}
