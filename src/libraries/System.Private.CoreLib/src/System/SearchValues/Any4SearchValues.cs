// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    internal sealed class Any4SearchValues<T, TImpl> : SearchValues<T>
        where T : unmanaged, IEquatable<T>
        where TImpl : unmanaged, INumber<TImpl>
    {
        private readonly TImpl _e0, _e1, _e2, _e3;

        public Any4SearchValues(ReadOnlySpan<TImpl> values)
        {
            Debug.Assert(Unsafe.SizeOf<T>() == Unsafe.SizeOf<TImpl>());
            Debug.Assert(values.Length == 4);
            (_e0, _e1, _e2, _e3) = (values[0], values[1], values[2], values[3]);
        }

        internal override T[] GetValues()
        {
            return new[] { Unsafe.BitCast<TImpl, T>(_e0), Unsafe.BitCast<TImpl, T>(_e1), Unsafe.BitCast<TImpl, T>(_e2), Unsafe.BitCast<TImpl, T>(_e3) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(T value) =>
            Unsafe.BitCast<T, TImpl>(value) == _e0 ||
            Unsafe.BitCast<T, TImpl>(value) == _e1 ||
            Unsafe.BitCast<T, TImpl>(value) == _e2 ||
            Unsafe.BitCast<T, TImpl>(value) == _e3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<T> span) =>
            SpanHelpers.IndexOfAnyValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<T> span) =>
            SpanHelpers.IndexOfAnyExceptValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<T> span) =>
            SpanHelpers.LastIndexOfAnyValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<T> span) =>
            SpanHelpers.LastIndexOfAnyExceptValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, span.Length);
    }
}
