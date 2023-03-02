﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable 8500 // address of managed types

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

        internal override unsafe T[] GetValues()
        {
            return new[] { Unsafe.BitCast<TImpl, T>(_e0), Unsafe.BitCast<TImpl, T>(_e1), Unsafe.BitCast<TImpl, T>(_e2), Unsafe.BitCast<TImpl, T>(_e3), Unsafe.BitCast<TImpl, T>(_e4) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override unsafe bool ContainsCore(T value) =>
            Unsafe.BitCast<T, TImpl>(value) == _e0 ||
            Unsafe.BitCast<T, TImpl>(value) == _e1 ||
            Unsafe.BitCast<T, TImpl>(value) == _e2 ||
            Unsafe.BitCast<T, TImpl>(value) == _e3 ||
            Unsafe.BitCast<T, TImpl>(value) == _e4;

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
