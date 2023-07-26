// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal sealed class Any2ByteSearchValues : SearchValues<byte>
    {
        private readonly byte _e0, _e1;

        public Any2ByteSearchValues(ReadOnlySpan<byte> values)
        {
            Debug.Assert(values.Length == 2);
            (_e0, _e1) = (values[0], values[1]);
        }

        internal override byte[] GetValues() => new[] { _e0, _e1 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(byte value) =>
            value == _e0 || value == _e1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<byte> span) =>
            span.IndexOfAny(_e0, _e1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<byte> span) =>
            span.IndexOfAnyExcept(_e0, _e1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<byte> span) =>
            span.LastIndexOfAny(_e0, _e1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<byte> span) =>
            span.LastIndexOfAnyExcept(_e0, _e1);
    }
}
