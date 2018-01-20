// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

namespace System
{
    /// <summary>
    /// Extension methods for Span{T}, Memory{T}, and friends.
    /// </summary>
    public static partial class MemoryExtensions
    {
        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this Span<T> span, T value)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.IndexOf(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value),
                    span.Length);
            return SpanHelpers.IndexOf<T>(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this Span<T> span, ReadOnlySpan<T> value)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.IndexOf(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    value.Length);
            return SpanHelpers.IndexOf<T>(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its last occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this Span<T> span, T value)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.LastIndexOf(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value),
                    span.Length);
            return SpanHelpers.LastIndexOf<T>(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its last occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this Span<T> span, ReadOnlySpan<T> value)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.LastIndexOf(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    value.Length);
            return SpanHelpers.LastIndexOf<T>(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T). 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqual<T>(this Span<T> first, ReadOnlySpan<T> second)
            where T : IEquatable<T>
        {
            int length = first.Length;
            if (typeof(T) == typeof(byte))
                return length == second.Length &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(first)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(second)),
                    length);
            return length == second.Length && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(first), ref MemoryMarshal.GetReference(second), length);
        }

        /// <summary>
        /// Determines the relative order of the sequences being compared by comparing the elements using IComparable{T}.CompareTo(T). 
        /// </summary>
        public static int SequenceCompareTo<T>(this Span<T> first, ReadOnlySpan<T> second)
            where T : IComparable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.SequenceCompareTo(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(first)),
                    first.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(second)),
                    second.Length);
            return SpanHelpers.SequenceCompareTo(ref MemoryMarshal.GetReference(first), first.Length, ref MemoryMarshal.GetReference(second), second.Length);
        }

        /// <summary>
        /// Reverses the sequence of the elements in the entire span.
        /// </summary>
        public static void Reverse<T>(this Span<T> span)
        {
            ref T p = ref MemoryMarshal.GetReference(span);
            int i = 0;
            int j = span.Length - 1;
            while (i < j)
            {
                T temp = Unsafe.Add(ref p, i);
                Unsafe.Add(ref p, i) = Unsafe.Add(ref p, j);
                Unsafe.Add(ref p, j) = temp;
                i++;
                j--;
            }
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this ReadOnlySpan<T> span, T value)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.IndexOf(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value),
                    span.Length);
            return SpanHelpers.IndexOf<T>(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.IndexOf(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    value.Length);
            return SpanHelpers.IndexOf<T>(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its last occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this ReadOnlySpan<T> span, T value)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.LastIndexOf(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value),
                    span.Length);
            return SpanHelpers.LastIndexOf<T>(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its last occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.LastIndexOf(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    value.Length);
            return SpanHelpers.LastIndexOf<T>(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this Span<T> span, T value0, T value1)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.IndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value0),
                    Unsafe.As<T, byte>(ref value1),
                    span.Length);

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, span.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        /// <param name="value2">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this Span<T> span, T value0, T value1, T value2)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.IndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value0),
                    Unsafe.As<T, byte>(ref value1),
                    Unsafe.As<T, byte>(ref value2),
                    span.Length);

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1. 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this Span<T> span, ReadOnlySpan<T> values)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.IndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)),
                    values.Length);

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(values), values.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.IndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value0),
                    Unsafe.As<T, byte>(ref value1),
                    span.Length);

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, span.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1. 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        /// <param name="value2">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.IndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value0),
                    Unsafe.As<T, byte>(ref value1),
                    Unsafe.As<T, byte>(ref value2),
                    span.Length);

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1. 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.IndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)),
                    values.Length);

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(values), values.Length);
        }

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAny<T>(this Span<T> span, T value0, T value1)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.LastIndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value0),
                    Unsafe.As<T, byte>(ref value1),
                    span.Length);
            return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, span.Length);
        }

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        /// <param name="value2">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAny<T>(this Span<T> span, T value0, T value1, T value2)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.LastIndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value0),
                    Unsafe.As<T, byte>(ref value1),
                    Unsafe.As<T, byte>(ref value2),
                    span.Length);
            return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1. 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAny<T>(this Span<T> span, ReadOnlySpan<T> values)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.LastIndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)),
                    values.Length);
            return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(values), values.Length);
        }

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.LastIndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value0),
                    Unsafe.As<T, byte>(ref value1),
                    span.Length);
            return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, span.Length);
        }

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1. 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        /// <param name="value2">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.LastIndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    Unsafe.As<T, byte>(ref value0),
                    Unsafe.As<T, byte>(ref value1),
                    Unsafe.As<T, byte>(ref value2),
                    span.Length);
            return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1. 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAny<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values)
            where T : IEquatable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.LastIndexOfAny(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)),
                    values.Length);
            return SpanHelpers.LastIndexOfAny<T>(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(values), values.Length);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T). 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqual<T>(this ReadOnlySpan<T> first, ReadOnlySpan<T> second)
            where T : IEquatable<T>
        {
            int length = first.Length;
            if (typeof(T) == typeof(byte))
                return length == second.Length &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(first)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(second)),
                    length);
            return length == second.Length && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(first), ref MemoryMarshal.GetReference(second), length);
        }

        /// <summary>
        /// Determines the relative order of the sequences being compared by comparing the elements using IComparable{T}.CompareTo(T). 
        /// </summary>
        public static int SequenceCompareTo<T>(this ReadOnlySpan<T> first, ReadOnlySpan<T> second)
            where T : IComparable<T>
        {
            if (typeof(T) == typeof(byte))
                return SpanHelpers.SequenceCompareTo(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(first)),
                    first.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(second)),
                    second.Length);
            return SpanHelpers.SequenceCompareTo(ref MemoryMarshal.GetReference(first), first.Length, ref MemoryMarshal.GetReference(second), second.Length);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the start of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith<T>(this Span<T> span, ReadOnlySpan<T> value)
            where T : IEquatable<T>
        {
            int valueLength = value.Length;
            if (typeof(T) == typeof(byte))
                return valueLength <= span.Length &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    valueLength);
            return valueLength <= span.Length && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(value), valueLength);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the start of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value)
            where T : IEquatable<T>
        {
            int valueLength = value.Length;
            if (typeof(T) == typeof(byte))
                return valueLength <= span.Length &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    valueLength);
            return valueLength <= span.Length && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(value), valueLength);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the end of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith<T>(this Span<T> span, ReadOnlySpan<T> value)
            where T : IEquatable<T>
        {
            int spanLength = span.Length;
            int valueLength = value.Length;
            if (typeof(T) == typeof(byte))
                return valueLength <= spanLength &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), spanLength - valueLength)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    valueLength);
            return valueLength <= spanLength &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(span), spanLength - valueLength),
                    ref MemoryMarshal.GetReference(value),
                    valueLength);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the end of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value)
            where T : IEquatable<T>
        {
            int spanLength = span.Length;
            int valueLength = value.Length;
            if (typeof(T) == typeof(byte))
                return valueLength <= spanLength &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), spanLength - valueLength)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    valueLength);
            return valueLength <= spanLength &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(span), spanLength - valueLength),
                    ref MemoryMarshal.GetReference(value),
                    valueLength);
        }

        /// <summary>
        /// Creates a new  span over the portion of the target array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this T[] array)
        {
            return new Span<T>(array);
        }

        /// <summary>
        /// Creates a new  span over the portion of the target array segment.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ArraySegment<T> arraySegment)
        {
            return new Span<T>(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
        }

        /// <summary>
        /// Creates a new readonly span over the entire target array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] array)
        {
            return new ReadOnlySpan<T>(array);
        }

        /// <summary>
        /// Creates a new readonly span over the entire target span.
        /// </summary>
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this Span<T> span) => span;

        /// <summary>
        /// Creates a new readonly span over the target array segment.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this ArraySegment<T> arraySegment)
        {
            return new ReadOnlySpan<T>(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
        }

        /// <summary>
        /// Creates a new readonly memory over the entire target memory.
        /// </summary>
        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(this Memory<T> memory) => memory;

        /// <summary>
        /// Copies the contents of the array into the span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// 
        ///<param name="array">The array to copy items from.</param>
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the destination Span is shorter than the source array.
        /// </exception>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this T[] array, Span<T> destination)
        {
            new ReadOnlySpan<T>(array).CopyTo(destination);
        }

        /// <summary>
        /// Copies the contents of the array into the memory. If the source
        /// and destinations overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.
        /// 
        ///<param name="array">The array to copy items from.</param>
        /// <param name="destination">The memory to copy items into.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the destination is shorter than the source array.
        /// </exception>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this T[] array, Memory<T> destination)
        {
            array.CopyTo(destination.Span);
        }

        //
        //  Overlaps
        //  ========
        //
        //  The following methods can be used to determine if two sequences
        //  overlap in memory.
        //
        //  Two sequences overlap if they have positions in common and neither
        //  is empty. Empty sequences do not overlap with any other sequence.
        //
        //  If two sequences overlap, the element offset is the number of
        //  elements by which the second sequence is offset from the first
        //  sequence (i.e., second minus first). An exception is thrown if the
        //  number is not a whole number, which can happen when a sequence of a
        //  smaller type is cast to a sequence of a larger type with unsafe code
        //  or NonPortableCast. If the sequences do not overlap, the offset is
        //  meaningless and arbitrarily set to zero.
        //
        //  Implementation
        //  --------------
        //
        //  Implementing this correctly is quite tricky due of two problems:
        //
        //  * If the sequences refer to two different objects on the managed
        //    heap, the garbage collector can move them freely around or change
        //    their relative order in memory.
        //
        //  * The distance between two sequences can be greater than
        //    int.MaxValue (on a 32-bit system) or long.MaxValue (on a 64-bit
        //    system).
        //
        //  (For simplicity, the following text assumes a 32-bit system, but
        //  everything also applies to a 64-bit system if every 32 is replaced a
        //  64.)
        //
        //  The first problem is solved by calculating the distance with exactly
        //  one atomic operation. If the garbage collector happens to move the
        //  sequences afterwards and the sequences overlapped before, they will
        //  still overlap after the move and their distance hasn't changed. If
        //  the sequences did not overlap, the distance can change but the
        //  sequences still won't overlap.
        //
        //  The second problem is solved by making all addresses relative to the
        //  start of the first sequence and performing all operations in
        //  unsigned integer arithmetic modulo 2³².
        //
        //  Example
        //  -------
        //
        //  Let's say there are two sequences, x and y. Let
        //
        //      ref T xRef    = MemoryMarshal.GetReference(x)
        //      uint  xLength = x.Length * Unsafe.SizeOf<T>()
        //      ref T yRef    = MemoryMarshal.GetReference(y)
        //      uint  yLength = y.Length * Unsafe.SizeOf<T>()
        //
        //  Visually, the two sequences are located somewhere in the 32-bit
        //  address space as follows:
        //
        //      [----------------------------------------------)                            normal address space
        //      0                                             2³²
        //                            [------------------)                                  first sequence
        //                            xRef            xRef + xLength
        //              [--------------------------)     .                                  second sequence
        //              yRef          .         yRef + yLength
        //              :             .            .     .
        //              :             .            .     .
        //                            .            .     .
        //                            .            .     .
        //                            .            .     .
        //                            [----------------------------------------------)      relative address space
        //                            0            .     .                          2³²
        //                            [------------------)             :                    first sequence
        //                            x1           .     x2            :
        //                            -------------)                   [-------------       second sequence
        //                                         y2                  y1
        //
        //  The idea is to make all addresses relative to xRef: Let x1 be the
        //  start address of x in this relative address space, x2 the end
        //  address of x, y1 the start address of y, and y2 the end address of
        //  y:
        //
        //      nuint x1 = 0
        //      nuint x2 = xLength
        //      nuint y1 = (nuint)Unsafe.ByteOffset(xRef, yRef)
        //      nuint y2 = y1 + yLength
        //  
        //  xRef relative to xRef is 0.
        //  
        //  x2 is simply x1 + xLength. This cannot overflow.
        //  
        //  yRef relative to xRef is (yRef - xRef). If (yRef - xRef) is
        //  negative, casting it to an unsigned 32-bit integer turns it into
        //  (yRef - xRef + 2³²). So, in the example above, y1 moves to the right
        //  of x2.
        //  
        //  y2 is simply y1 + yLength. Note that this can overflow, as in the
        //  example above, which must be avoided.
        //
        //  The two sequences do *not* overlap if y is entirely in the space
        //  right of x in the relative address space. (It can't be left of it!)
        //
        //          (y1 >= x2) && (y2 <= 2³²)
        //
        //  Inversely, they do overlap if
        //
        //          (y1 < x2) || (y2 > 2³²)
        //
        //  After substituting x2 and y2 with their respective definition:
        //
        //      ==  (y1 < xLength) || (y1 + yLength > 2³²)
        //
        //  Since yLength can't be greater than the size of the address space,
        //  the overflow can be avoided as follows:
        //
        //      ==  (y1 < xLength) || (y1 > 2³² - yLength)
        //
        //  However, 2³² cannot be stored in an unsigned 32-bit integer, so one
        //  more change is needed to keep doing everything with unsigned 32-bit
        //  integers:
        //
        //      ==  (y1 < xLength) || (y1 > -yLength)
        //  
        //  Due to modulo arithmetic, this gives exactly same result *except* if
        //  yLength is zero, since 2³² - 0 is 0 and not 2³². So the case
        //  y.IsEmpty must be handled separately first.
        //  

        /// <summary>
        /// Determines whether two sequences overlap in memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlaps<T>(this Span<T> first, ReadOnlySpan<T> second)
        {
            return Overlaps((ReadOnlySpan<T>)first, second);
        }

        /// <summary>
        /// Determines whether two sequences overlap in memory and outputs the element offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlaps<T>(this Span<T> first, ReadOnlySpan<T> second, out int elementOffset)
        {
            return Overlaps((ReadOnlySpan<T>)first, second, out elementOffset);
        }

        /// <summary>
        /// Determines whether two sequences overlap in memory.
        /// </summary>
        public static bool Overlaps<T>(this ReadOnlySpan<T> first, ReadOnlySpan<T> second)
        {
            if (first.IsEmpty || second.IsEmpty)
            {
                return false;
            }

            IntPtr byteOffset = Unsafe.ByteOffset(
                ref MemoryMarshal.GetReference(first),
                ref MemoryMarshal.GetReference(second));

            if (Unsafe.SizeOf<IntPtr>() == sizeof(int))
            {
                return (uint)byteOffset < (uint)(first.Length * Unsafe.SizeOf<T>()) ||
                       (uint)byteOffset > (uint)-(second.Length * Unsafe.SizeOf<T>());
            }
            else
            {
                return (ulong)byteOffset < (ulong)((long)first.Length * Unsafe.SizeOf<T>()) ||
                       (ulong)byteOffset > (ulong)-((long)second.Length * Unsafe.SizeOf<T>());
            }
        }

        /// <summary>
        /// Determines whether two sequences overlap in memory and outputs the element offset.
        /// </summary>
        public static bool Overlaps<T>(this ReadOnlySpan<T> first, ReadOnlySpan<T> second, out int elementOffset)
        {
            if (first.IsEmpty || second.IsEmpty)
            {
                elementOffset = 0;
                return false;
            }

            IntPtr byteOffset = Unsafe.ByteOffset(
                ref MemoryMarshal.GetReference(first),
                ref MemoryMarshal.GetReference(second));

            if (Unsafe.SizeOf<IntPtr>() == sizeof(int))
            {
                if ((uint)byteOffset < (uint)(first.Length * Unsafe.SizeOf<T>()) ||
                    (uint)byteOffset > (uint)-(second.Length * Unsafe.SizeOf<T>()))
                {
                    if ((int)byteOffset % Unsafe.SizeOf<T>() != 0)
                        ThrowHelper.ThrowArgumentException_OverlapAlignmentMismatch();

                    elementOffset = (int)byteOffset / Unsafe.SizeOf<T>();
                    return true;
                }
                else
                {
                    elementOffset = 0;
                    return false;
                }
            }
            else
            {
                if ((ulong)byteOffset < (ulong)((long)first.Length * Unsafe.SizeOf<T>()) ||
                    (ulong)byteOffset > (ulong)-((long)second.Length * Unsafe.SizeOf<T>()))
                {
                    if ((long)byteOffset % Unsafe.SizeOf<T>() != 0)
                        ThrowHelper.ThrowArgumentException_OverlapAlignmentMismatch();

                    elementOffset = (int)((long)byteOffset / Unsafe.SizeOf<T>());
                    return true;
                }
                else
                {
                    elementOffset = 0;
                    return false;
                }
            }
        }

        /// <summary>
        /// Searches an entire sorted <see cref="Span{T}"/> for a value
        /// using the specified <see cref="IComparable{T}"/> generic interface.
        /// </summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The sorted <see cref="Span{T}"/> to search.</param>
        /// <param name="comparable">The <see cref="IComparable{T}"/> to use when comparing.</param>
        /// <returns>
        /// The zero-based index of <paramref name="comparable"/> in the sorted <paramref name="span"/>,
        /// if <paramref name="comparable"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="comparable"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="Span{T}.Length"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
		///     <paramref name = "comparable" /> is <see langword="null"/> .
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T>(
            this Span<T> span, IComparable<T> comparable)
        {
            return BinarySearch<T, IComparable<T>>(span, comparable);
        }

        /// <summary>
        /// Searches an entire sorted <see cref="Span{T}"/> for a value
        /// using the specified <typeparamref name="TComparable"/> generic type.
        /// </summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <typeparam name="TComparable">The specific type of <see cref="IComparable{T}"/>.</typeparam>
        /// <param name="span">The sorted <see cref="Span{T}"/> to search.</param>
        /// <param name="comparable">The <typeparamref name="TComparable"/> to use when comparing.</param>
        /// <returns>
        /// The zero-based index of <paramref name="comparable"/> in the sorted <paramref name="span"/>,
        /// if <paramref name="comparable"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="comparable"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="Span{T}.Length"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
		///     <paramref name = "comparable" /> is <see langword="null"/> .
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparable>(
            this Span<T> span, TComparable comparable)
            where TComparable : IComparable<T>
        {
            return BinarySearch((ReadOnlySpan<T>)span, comparable);
        }

        /// <summary>
        /// Searches an entire sorted <see cref="Span{T}"/> for the specified <paramref name="value"/>
        /// using the specified <typeparamref name="TComparer"/> generic type.
        /// </summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <typeparam name="TComparer">The specific type of <see cref="IComparer{T}"/>.</typeparam>
        /// <param name="span">The sorted <see cref="Span{T}"/> to search.</param>
        /// <param name="value">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">The <typeparamref name="TComparer"/> to use when comparing.</param>
        /// /// <returns>
        /// The zero-based index of <paramref name="value"/> in the sorted <paramref name="span"/>,
        /// if <paramref name="value"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="value"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="Span{T}.Length"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name = "comparer" /> is <see langword="null"/> .
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
            this Span<T> span, T value, TComparer comparer)
            where TComparer : IComparer<T>
        {
            return BinarySearch((ReadOnlySpan<T>)span, value, comparer);
        }

        /// <summary>
        /// Searches an entire sorted <see cref="ReadOnlySpan{T}"/> for a value
        /// using the specified <see cref="IComparable{T}"/> generic interface.
        /// </summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The sorted <see cref="ReadOnlySpan{T}"/> to search.</param>
        /// <param name="comparable">The <see cref="IComparable{T}"/> to use when comparing.</param>
        /// <returns>
        /// The zero-based index of <paramref name="comparable"/> in the sorted <paramref name="span"/>,
        /// if <paramref name="comparable"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="comparable"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="ReadOnlySpan{T}.Length"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
		///     <paramref name = "comparable" /> is <see langword="null"/> .
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T>(
            this ReadOnlySpan<T> span, IComparable<T> comparable)
        {
            return BinarySearch<T, IComparable<T>>(span, comparable);
        }

        /// <summary>
        /// Searches an entire sorted <see cref="ReadOnlySpan{T}"/> for a value
        /// using the specified <typeparamref name="TComparable"/> generic type.
        /// </summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <typeparam name="TComparable">The specific type of <see cref="IComparable{T}"/>.</typeparam>
        /// <param name="span">The sorted <see cref="ReadOnlySpan{T}"/> to search.</param>
        /// <param name="comparable">The <typeparamref name="TComparable"/> to use when comparing.</param>
        /// <returns>
        /// The zero-based index of <paramref name="comparable"/> in the sorted <paramref name="span"/>,
        /// if <paramref name="comparable"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="comparable"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="ReadOnlySpan{T}.Length"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
		///     <paramref name = "comparable" /> is <see langword="null"/> .
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparable>(
            this ReadOnlySpan<T> span, TComparable comparable)
            where TComparable : IComparable<T>
        {
            return SpanHelpers.BinarySearch(span, comparable);
        }

        /// <summary>
        /// Searches an entire sorted <see cref="ReadOnlySpan{T}"/> for the specified <paramref name="value"/>
        /// using the specified <typeparamref name="TComparer"/> generic type.
        /// </summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <typeparam name="TComparer">The specific type of <see cref="IComparer{T}"/>.</typeparam>
        /// <param name="span">The sorted <see cref="ReadOnlySpan{T}"/> to search.</param>
        /// <param name="value">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">The <typeparamref name="TComparer"/> to use when comparing.</param>
        /// /// <returns>
        /// The zero-based index of <paramref name="value"/> in the sorted <paramref name="span"/>,
        /// if <paramref name="value"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="value"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="ReadOnlySpan{T}.Length"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name = "comparer" /> is <see langword="null"/> .
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
            this ReadOnlySpan<T> span, T value, TComparer comparer)
            where TComparer : IComparer<T>
        {
            if (comparer == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparer);

            var comparable = new SpanHelpers.ComparerComparable<T, TComparer>(
                value, comparer);
            return BinarySearch(span, comparable);
        }
    }
}
