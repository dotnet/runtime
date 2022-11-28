// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    internal sealed class IndexOfAny5Values<T, TImpl> : IndexOfAnyValues<T>
        where T : struct, IEquatable<T>
        where TImpl : struct, INumber<TImpl>
    {
        private readonly TImpl _e0, _e1, _e2, _e3, _e4;

        public IndexOfAny5Values(ReadOnlySpan<TImpl> values)
        {
            Debug.Assert(Unsafe.SizeOf<T>() == Unsafe.SizeOf<TImpl>());
            Debug.Assert(values.Length == 5);
            (_e0, _e1, _e2, _e3, _e4) = (values[0], values[1], values[2], values[3], values[4]);
        }

        internal override T[] GetValues()
        {
            TImpl e0 = _e0, e1 = _e1, e2 = _e2, e3 = _e3, e4 = _e4;
            return new[] { Unsafe.As<TImpl, T>(ref e0), Unsafe.As<TImpl, T>(ref e1), Unsafe.As<TImpl, T>(ref e2), Unsafe.As<TImpl, T>(ref e3), Unsafe.As<TImpl, T>(ref e4) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<T> span) =>
            SpanHelpers.IndexOfAnyValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, _e4, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<T> span) =>
            SpanHelpers.IndexOfAnyExceptValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, _e4, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<T> span) =>
            SpanHelpers.LastIndexOfAnyValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, _e4, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<T> span) =>
            SpanHelpers.LastIndexOfAnyExceptValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, _e4, span.Length);
    }
}
