// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal sealed class SingleByteSearchValues : SearchValues<byte>
    {
        private readonly byte _e0;

        public SingleByteSearchValues(ReadOnlySpan<byte> values)
        {
            Debug.Assert(values.Length == 1);
            _e0 = values[0];
        }

        internal override byte[] GetValues() => new[] { _e0 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(byte value) =>
            value == _e0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<byte> span) =>
            span.IndexOf(_e0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<byte> span) =>
            span.IndexOfAnyExcept(_e0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<byte> span) =>
            span.LastIndexOf(_e0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<byte> span) =>
            span.LastIndexOfAnyExcept(_e0);
    }
}
