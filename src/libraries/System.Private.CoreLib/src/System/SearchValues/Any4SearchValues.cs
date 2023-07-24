// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable 8500 // address of managed types

namespace System.Buffers
{
    internal sealed class Any4SearchValues<T, TImpl> : SearchValues<T>
        where T : struct, IEquatable<T>
        where TImpl : struct, INumber<TImpl>
    {
        private readonly TImpl _e0, _e1, _e2, _e3;

        public Any4SearchValues(ReadOnlySpan<TImpl> values)
        {
            Debug.Assert(Unsafe.SizeOf<T>() == Unsafe.SizeOf<TImpl>());
            Debug.Assert(values.Length == 4);
            (_e0, _e1, _e2, _e3) = (values[0], values[1], values[2], values[3]);
        }

        internal override unsafe T[] GetValues()
        {
            TImpl e0 = _e0, e1 = _e1, e2 = _e2, e3 = _e3;
            return new[] { *(T*)&e0, *(T*)&e1, *(T*)&e2, *(T*)&e3 };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override unsafe bool ContainsCore(T value) =>
            *(TImpl*)&value == _e0 ||
            *(TImpl*)&value == _e1 ||
            *(TImpl*)&value == _e2 ||
            *(TImpl*)&value == _e3;

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
