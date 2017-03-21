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
        /// Searches for the specified value and returns the index of its first occurrence. If not found, returns -1. 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this Span<byte> span, byte value)
        {
            return SpanHelpers.IndexOf(ref span.DangerousGetPinnableReference(), value, span.Length);
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
        /// Searches for the specified sequence and returns the index of its first occurrence. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this Span<byte> span, ReadOnlySpan<byte> value)
        {
            return SpanHelpers.IndexOf(ref span.DangerousGetPinnableReference(), span.Length, ref value.DangerousGetPinnableReference(), value.Length);
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

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqual(this Span<byte> first, ReadOnlySpan<byte> second)
        {
            int length = first.Length;
            return length == second.Length && SpanHelpers.SequenceEqual(ref first.DangerousGetPinnableReference(), ref second.DangerousGetPinnableReference(), length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable&lt;T&gt;.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this ReadOnlySpan<T> span, T value)
            where T : struct, IEquatable<T>
        {
            return SpanHelpers.IndexOf<T>(ref span.DangerousGetPinnableReference(), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence. If not found, returns -1. 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this ReadOnlySpan<byte> span, byte value)
        {
            return SpanHelpers.IndexOf(ref span.DangerousGetPinnableReference(), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable&lt;T&gt;.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value)
            where T : struct, IEquatable<T>
        {
            return SpanHelpers.IndexOf<T>(ref span.DangerousGetPinnableReference(), span.Length, ref value.DangerousGetPinnableReference(), value.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its first occurrence. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> value)
        {
            return SpanHelpers.IndexOf(ref span.DangerousGetPinnableReference(), span.Length, ref value.DangerousGetPinnableReference(), value.Length);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable&lt;T&gt;.Equals(T). 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqual<T>(this ReadOnlySpan<T> first, ReadOnlySpan<T> second)
            where T : struct, IEquatable<T>
        {
            int length = first.Length;
            return length == second.Length && SpanHelpers.SequenceEqual(ref first.DangerousGetPinnableReference(), ref second.DangerousGetPinnableReference(), length);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqual(this ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
        {
            int length = first.Length;
            return length == second.Length && SpanHelpers.SequenceEqual(ref first.DangerousGetPinnableReference(), ref second.DangerousGetPinnableReference(), length);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the start of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith(this Span<byte> span, ReadOnlySpan<byte> value)
        {
            int valueLength = value.Length;
            return valueLength <= span.Length && SpanHelpers.SequenceEqual(ref span.DangerousGetPinnableReference(), ref value.DangerousGetPinnableReference(), valueLength);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the start of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith<T>(this Span<T> span, ReadOnlySpan<T> value)
            where T : struct, IEquatable<T>
        {
            int valueLength = value.Length;
            return valueLength <= span.Length && SpanHelpers.SequenceEqual(ref span.DangerousGetPinnableReference(), ref value.DangerousGetPinnableReference(), valueLength);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the start of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> value)
        {
            int valueLength = value.Length;
            return valueLength <= span.Length && SpanHelpers.SequenceEqual(ref span.DangerousGetPinnableReference(), ref value.DangerousGetPinnableReference(), valueLength);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the start of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value)
            where T : struct, IEquatable<T>
        {
            int valueLength = value.Length;
            return valueLength <= span.Length && SpanHelpers.SequenceEqual(ref span.DangerousGetPinnableReference(), ref value.DangerousGetPinnableReference(), valueLength);
        }
    }
}