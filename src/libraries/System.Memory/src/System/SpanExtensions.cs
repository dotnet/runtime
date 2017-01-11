// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Extension methods for Span&lt;T&gt;.
    /// </summary>
    public static class SpanExtensions
    {
        /// <summary>
        /// Casts a Span of one primitive type <typeparamref name="T"/> to Span of bytes.
        /// That type may not contain pointers or references. This is checked at runtime in order to preserve type safety.
        /// </summary>
        /// <param name="source">The source slice, of type <typeparamref name="T"/>.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <typeparamref name="T"/> contains pointers.
        /// </exception>
        /// <exception cref="System.OverflowException">
        /// Thrown if the Length property of the new Span would exceed Int32.MaxValue.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> AsBytes<T>(this Span<T> source)
            where T : struct
        {
            if (!SpanHelpers.IsReferenceFree<T>())
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));

            int newLength = checked(source.Length * Unsafe.SizeOf<T>());
            return new Span<byte>(Unsafe.As<Pinnable<byte>>(source.Pinnable), source.ByteOffset, newLength);
        }

        /// <summary>
        /// Casts a ReadOnlySpan of one primitive type <typeparamref name="T"/> to ReadOnlySpan of bytes.
        /// That type may not contain pointers or references. This is checked at runtime in order to preserve type safety.
        /// </summary>
        /// <param name="source">The source slice, of type <typeparamref name="T"/>.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <typeparamref name="T"/> contains pointers.
        /// </exception>
        /// <exception cref="System.OverflowException">
        /// Thrown if the Length property of the new Span would exceed Int32.MaxValue.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> source)
            where T : struct
        {
            if (!SpanHelpers.IsReferenceFree<T>())
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));

            int newLength = checked(source.Length * Unsafe.SizeOf<T>());
            return new ReadOnlySpan<byte>(Unsafe.As<Pinnable<byte>>(source.Pinnable), source.ByteOffset, newLength);
        }

        /// <summary>
        /// Casts a Span of one primitive type <typeparamref name="TFrom"/> to another primitive type <typeparamref name="TTo"/>.
        /// These types may not contain pointers or references. This is checked at runtime in order to preserve type safety.
        /// </summary>
        /// <remarks>
        /// Supported only for platforms that support misaligned memory access.
        /// </remarks>
        /// <param name="source">The source slice, of type <typeparamref name="TFrom"/>.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <typeparamref name="TFrom"/> or <typeparamref name="TTo"/> contains pointers.
        /// </exception>
        /// <exception cref="System.OverflowException">
        /// Thrown if the Length property of the new Span would exceed Int32.MaxValue.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<TTo> NonPortableCast<TFrom, TTo>(this Span<TFrom> source)
            where TFrom : struct
            where TTo : struct
        {
            if (!SpanHelpers.IsReferenceFree<TFrom>())
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(TFrom));

            if (!SpanHelpers.IsReferenceFree<TTo>())
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(TTo));

            int newLength = checked((int)((long)source.Length * Unsafe.SizeOf<TFrom>() / Unsafe.SizeOf<TTo>()));
            return new Span<TTo>(Unsafe.As<Pinnable<TTo>>(source.Pinnable), source.ByteOffset, newLength);
        }

        /// <summary>
        /// Casts a ReadOnlySpan of one primitive type <typeparamref name="TFrom"/> to another primitive type <typeparamref name="TTo"/>.
        /// These types may not contain pointers or references. This is checked at runtime in order to preserve type safety.
        /// </summary>
        /// <remarks>
        /// Supported only for platforms that support misaligned memory access.
        /// </remarks>
        /// <param name="source">The source slice, of type <typeparamref name="TFrom"/>.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <typeparamref name="TFrom"/> or <typeparamref name="TTo"/> contains pointers.
        /// </exception>
        /// <exception cref="System.OverflowException">
        /// Thrown if the Length property of the new Span would exceed Int32.MaxValue.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<TTo> NonPortableCast<TFrom, TTo>(this ReadOnlySpan<TFrom> source)
            where TFrom : struct
            where TTo : struct
        {
            if (!SpanHelpers.IsReferenceFree<TFrom>())
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(TFrom));

            if (!SpanHelpers.IsReferenceFree<TTo>())
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(TTo));

            int newLength = checked((int)((long)source.Length * Unsafe.SizeOf<TFrom>() / Unsafe.SizeOf<TTo>()));
            return new ReadOnlySpan<TTo>(Unsafe.As<Pinnable<TTo>>(source.Pinnable), source.ByteOffset, newLength);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable&lt;T&gt;.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this Span<T> span, T value)
            where T:struct, IEquatable<T>
        {
            return SpanHelpers.IndexOf<T>(ref span.DangerousGetPinnableReference(), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable&lt;T&gt;.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this Span<T> span, ReadOnlySpan<T> value)
            where T : struct, IEquatable<T>
        {
            return SpanHelpers.IndexOf<T>(ref span.DangerousGetPinnableReference(), span.Length, ref value.DangerousGetPinnableReference(), value.Length);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable&lt;T&gt;.Equals(T). 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqual<T>(this Span<T> first, ReadOnlySpan<T> second)
            where T:struct, IEquatable<T>
        {
            int length = first.Length;
            return length == second.Length && SpanHelpers.SequenceEqual(ref first.DangerousGetPinnableReference(), ref second.DangerousGetPinnableReference(), length);
        }
    }
}
