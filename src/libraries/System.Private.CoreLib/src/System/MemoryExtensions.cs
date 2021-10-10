// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Extension methods for Span{T}, Memory{T}, and friends.
    /// </summary>
    public static partial class MemoryExtensions
    {
        /// <summary>
        /// Creates a new span over the portion of the target array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this T[]? array, int start)
        {
            if (array == null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                return default;
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();
            if ((uint)start > (uint)array.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new Span<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start /* force zero-extension */), array.Length - start);
        }

        /// <summary>
        /// Creates a new span over the portion of the target array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this T[]? array, Index startIndex)
        {
            if (array == null)
            {
                if (!startIndex.Equals(Index.Start))
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

                return default;
            }

            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();

            int actualIndex = startIndex.GetOffset(array.Length);
            if ((uint)actualIndex > (uint)array.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new Span<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)actualIndex /* force zero-extension */), array.Length - actualIndex);
        }

        /// <summary>
        /// Creates a new span over the portion of the target array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this T[]? array, Range range)
        {
            if (array == null)
            {
                Index startIndex = range.Start;
                Index endIndex = range.End;

                if (!startIndex.Equals(Index.Start) || !endIndex.Equals(Index.Start))
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

                return default;
            }

            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();

            (int start, int length) = range.GetOffsetAndLength(array.Length);
            return new Span<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start /* force zero-extension */), length);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> AsSpan(this string? text)
        {
            if (text == null)
                return default;

            return new ReadOnlySpan<char>(ref text.GetRawStringData(), text.Length);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;text.Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> AsSpan(this string? text, int start)
        {
            if (text == null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

            if ((uint)start > (uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            return new ReadOnlySpan<char>(ref Unsafe.Add(ref text.GetRawStringData(), (nint)(uint)start /* force zero-extension */), text.Length - start);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index or <paramref name="length"/> is not in range.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> AsSpan(this string? text, int start, int length)
        {
            if (text == null)
            {
                if (start != 0 || length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

#if TARGET_64BIT
            // See comment in Span<T>.Slice for how this works.
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
#else
            if ((uint)start > (uint)text.Length || (uint)length > (uint)(text.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
#endif

            return new ReadOnlySpan<char>(ref Unsafe.Add(ref text.GetRawStringData(), (nint)(uint)start /* force zero-extension */), length);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.</summary>
        /// <param name="text">The target string.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        public static ReadOnlyMemory<char> AsMemory(this string? text)
        {
            if (text == null)
                return default;

            return new ReadOnlyMemory<char>(text, 0, text.Length);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.</summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;text.Length).
        /// </exception>
        public static ReadOnlyMemory<char> AsMemory(this string? text, int start)
        {
            if (text == null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

            if ((uint)start > (uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            return new ReadOnlyMemory<char>(text, start, text.Length - start);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.</summary>
        /// <param name="text">The target string.</param>
        /// <param name="startIndex">The index at which to begin this slice.</param>
        public static ReadOnlyMemory<char> AsMemory(this string? text, Index startIndex)
        {
            if (text == null)
            {
                if (!startIndex.Equals(Index.Start))
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);

                return default;
            }

            int actualIndex = startIndex.GetOffset(text.Length);
            if ((uint)actualIndex > (uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new ReadOnlyMemory<char>(text, actualIndex, text.Length - actualIndex);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.</summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index or <paramref name="length"/> is not in range.
        /// </exception>
        public static ReadOnlyMemory<char> AsMemory(this string? text, int start, int length)
        {
            if (text == null)
            {
                if (start != 0 || length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

#if TARGET_64BIT
            // See comment in Span<T>.Slice for how this works.
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
#else
            if ((uint)start > (uint)text.Length || (uint)length > (uint)(text.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
#endif

            return new ReadOnlyMemory<char>(text, start, length);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.</summary>
        /// <param name="text">The target string.</param>
        /// <param name="range">The range used to indicate the start and length of the sliced string.</param>
        public static ReadOnlyMemory<char> AsMemory(this string? text, Range range)
        {
            if (text == null)
            {
                Index startIndex = range.Start;
                Index endIndex = range.End;

                if (!startIndex.Equals(Index.Start) || !endIndex.Equals(Index.Start))
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);

                return default;
            }

            (int start, int length) = range.GetOffsetAndLength(text.Length);
            return new ReadOnlyMemory<char>(text, start, length);
        }

        /// <summary>
        /// Searches for the specified value and returns true if found. If not found, returns false. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this Span<T> span, T value) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.Contains(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.Contains(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, char>(ref value),
                        span.Length);
            }

            return SpanHelpers.Contains(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified value and returns true if found. If not found, returns false. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.Contains(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.Contains(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, char>(ref value),
                        span.Length);
            }

            return SpanHelpers.Contains(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this Span<T> span, T value) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.IndexOf(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.IndexOf(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, char>(ref value),
                        span.Length);
            }

            return SpanHelpers.IndexOf(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.IndexOf(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                        value.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.IndexOf(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(value)),
                        value.Length);
            }

            return SpanHelpers.IndexOf(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its last occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this Span<T> span, T value) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.LastIndexOf(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.LastIndexOf(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, char>(ref value),
                        span.Length);
            }

            return SpanHelpers.LastIndexOf<T>(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its last occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            if (Unsafe.SizeOf<T>() == sizeof(byte) && RuntimeHelpers.IsBitwiseEquatable<T>())
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
        public static bool SequenceEqual<T>(this Span<T> span, ReadOnlySpan<T> other) where T : IEquatable<T>
        {
            int length = span.Length;

            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                nuint size = (nuint)Unsafe.SizeOf<T>();
                return length == other.Length &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)),
                    ((uint)length) * size);  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this api in such a case so we choose not to take the overhead of checking.
            }

            return length == other.Length && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(other), length);
        }

        /// <summary>
        /// Determines the relative order of the sequences being compared by comparing the elements using IComparable{T}.CompareTo(T).
        /// </summary>
        public static int SequenceCompareTo<T>(this Span<T> span, ReadOnlySpan<T> other)
            where T : IComparable<T>
        {
            // Can't use IsBitwiseEquatable<T>() below because that only tells us about
            // equality checks, not about CompareTo checks.

            if (typeof(T) == typeof(byte))
                return SpanHelpers.SequenceCompareTo(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)),
                    other.Length);

            if (typeof(T) == typeof(char))
                return SpanHelpers.SequenceCompareTo(
                    ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(other)),
                    other.Length);

            return SpanHelpers.SequenceCompareTo(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(other), other.Length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.IndexOf(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.IndexOf(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, char>(ref value),
                        span.Length);
            }

            return SpanHelpers.IndexOf(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.IndexOf(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                        value.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.IndexOf(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(value)),
                        value.Length);
            }

            return SpanHelpers.IndexOf(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its last occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.LastIndexOf(
                            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                            Unsafe.As<T, byte>(ref value),
                            span.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.LastIndexOf(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, char>(ref value),
                        span.Length);
            }

            return SpanHelpers.LastIndexOf<T>(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its last occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            if (Unsafe.SizeOf<T>() == sizeof(byte) && RuntimeHelpers.IsBitwiseEquatable<T>())
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
        public static int IndexOfAny<T>(this Span<T> span, T value0, T value1) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.IndexOfAny(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        span.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.IndexOfAny(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, char>(ref value0),
                        Unsafe.As<T, char>(ref value1),
                        span.Length);
            }

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
        public static int IndexOfAny<T>(this Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.IndexOfAny(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        Unsafe.As<T, byte>(ref value2),
                        span.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.IndexOfAny(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, char>(ref value0),
                        Unsafe.As<T, char>(ref value1),
                        Unsafe.As<T, char>(ref value2),
                        span.Length);
            }

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this Span<T> span, ReadOnlySpan<T> values) where T : IEquatable<T> =>
            IndexOfAny((ReadOnlySpan<T>)span, values);

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.IndexOfAny(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        span.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.IndexOfAny(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, char>(ref value0),
                        Unsafe.As<T, char>(ref value1),
                        span.Length);
            }

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
        public static int IndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                    return SpanHelpers.IndexOfAny(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        Unsafe.As<T, byte>(ref value2),
                        span.Length);

                if (Unsafe.SizeOf<T>() == sizeof(char))
                    return SpanHelpers.IndexOfAny(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, char>(ref value0),
                        Unsafe.As<T, char>(ref value1),
                        Unsafe.As<T, char>(ref value2),
                        span.Length);
            }

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                {
                    ref byte valueRef = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values));
                    if (values.Length == 2)
                    {
                        return SpanHelpers.IndexOfAny(
                            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                            valueRef,
                            Unsafe.Add(ref valueRef, 1),
                            span.Length);
                    }
                    else if (values.Length == 3)
                    {
                        return SpanHelpers.IndexOfAny(
                            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                            valueRef,
                            Unsafe.Add(ref valueRef, 1),
                            Unsafe.Add(ref valueRef, 2),
                            span.Length);
                    }
                }

                if (Unsafe.SizeOf<T>() == sizeof(char))
                {
                    ref char valueRef = ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(values));
                    if (values.Length == 5)
                    {
                        // Length 5 is a common length for FileSystemName expression (", <, >, *, ?) and in preference to 2 as it has an explicit overload
                        return SpanHelpers.IndexOfAny(
                            ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                            valueRef,
                            Unsafe.Add(ref valueRef, 1),
                            Unsafe.Add(ref valueRef, 2),
                            Unsafe.Add(ref valueRef, 3),
                            Unsafe.Add(ref valueRef, 4),
                            span.Length);
                    }
                    else if (values.Length == 2)
                    {
                        // Length 2 is a common length for simple wildcards (*, ?),  directory separators (/, \), quotes (", '), brackets, etc
                        return SpanHelpers.IndexOfAny(
                            ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                            valueRef,
                            Unsafe.Add(ref valueRef, 1),
                            span.Length);
                    }
                    else if (values.Length == 4)
                    {
                        // Length 4 before 3 as 3 has an explicit overload
                        return SpanHelpers.IndexOfAny(
                            ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                            valueRef,
                            Unsafe.Add(ref valueRef, 1),
                            Unsafe.Add(ref valueRef, 2),
                            Unsafe.Add(ref valueRef, 3),
                            span.Length);
                    }
                    else if (values.Length == 3)
                    {
                        return SpanHelpers.IndexOfAny(
                            ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                            valueRef,
                            Unsafe.Add(ref valueRef, 1),
                            Unsafe.Add(ref valueRef, 2),
                            span.Length);
                    }
                    else if (values.Length == 1)
                    {
                        // Length 1 last, as ctoring a ReadOnlySpan to call this overload for a single value
                        // is already throwing away a bunch of performance vs just calling IndexOf
                        return SpanHelpers.IndexOf(
                            ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                            valueRef,
                            span.Length);
                    }
                }
            }

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(values), values.Length);
        }

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAny<T>(this Span<T> span, T value0, T value1) where T : IEquatable<T>
        {
            if (Unsafe.SizeOf<T>() == sizeof(byte) && RuntimeHelpers.IsBitwiseEquatable<T>())
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
        public static int LastIndexOfAny<T>(this Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>
        {
            if (Unsafe.SizeOf<T>() == sizeof(byte) && RuntimeHelpers.IsBitwiseEquatable<T>())
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
        public static int LastIndexOfAny<T>(this Span<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>
        {
            if (Unsafe.SizeOf<T>() == sizeof(byte) && RuntimeHelpers.IsBitwiseEquatable<T>())
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
        public static int LastIndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>
        {
            if (Unsafe.SizeOf<T>() == sizeof(byte) && RuntimeHelpers.IsBitwiseEquatable<T>())
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
        public static int LastIndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>
        {
            if (Unsafe.SizeOf<T>() == sizeof(byte) && RuntimeHelpers.IsBitwiseEquatable<T>())
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
        public static int LastIndexOfAny<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>
        {
            return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(values), values.Length);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other) where T : IEquatable<T>
        {
            int length = span.Length;
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                nuint size = (nuint)Unsafe.SizeOf<T>();
                return length == other.Length &&
                    SpanHelpers.SequenceEqual(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)),
                        ((uint)length) * size);  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this API in such a case so we choose not to take the overhead of checking.
            }

            return length == other.Length && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(other), length);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using an <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="span">The first sequence to compare.</param>
        /// <param name="other">The second sequence to compare.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing elements, or null to use the default <see cref="IEqualityComparer{T}"/> for the type of an element.</param>
        /// <returns>true if the two sequences are equal; otherwise, false.</returns>
        public static bool SequenceEqual<T>(this Span<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null) =>
            SequenceEqual((ReadOnlySpan<T>)span, other, comparer);

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using an <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="span">The first sequence to compare.</param>
        /// <param name="other">The second sequence to compare.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing elements, or null to use the default <see cref="IEqualityComparer{T}"/> for the type of an element.</param>
        /// <returns>true if the two sequences are equal; otherwise, false.</returns>
        public static bool SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
        {
            // If the spans differ in length, they're not equal.
            if (span.Length != other.Length)
            {
                return false;
            }

            if (typeof(T).IsValueType)
            {
                if (comparer is null || comparer == EqualityComparer<T>.Default)
                {
                    // If no comparer was supplied and the type is bitwise equatable, take the fast path doing a bitwise comparison.
                    if (RuntimeHelpers.IsBitwiseEquatable<T>())
                    {
                        nuint size = (nuint)Unsafe.SizeOf<T>();
                        return SpanHelpers.SequenceEqual(
                            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)),
                            ((uint)span.Length) * size);  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this API in such a case so we choose not to take the overhead of checking.
                    }

                    // Otherwise, compare each element using EqualityComparer<T>.Default.Equals in a way that will enable it to devirtualize.
                    for (int i = 0; i < span.Length; i++)
                    {
                        if (!EqualityComparer<T>.Default.Equals(span[i], other[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            // Use the comparer to compare each element.
            comparer ??= EqualityComparer<T>.Default;
            for (int i = 0; i < span.Length; i++)
            {
                if (!comparer.Equals(span[i], other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines the relative order of the sequences being compared by comparing the elements using IComparable{T}.CompareTo(T).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SequenceCompareTo<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other)
            where T : IComparable<T>
        {
            // Can't use IsBitwiseEquatable<T>() below because that only tells us about
            // equality checks, not about CompareTo checks.

            if (typeof(T) == typeof(byte))
                return SpanHelpers.SequenceCompareTo(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)),
                    other.Length);

            if (typeof(T) == typeof(char))
                return SpanHelpers.SequenceCompareTo(
                    ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(other)),
                    other.Length);

            return SpanHelpers.SequenceCompareTo(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(other), other.Length);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the start of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            int valueLength = value.Length;
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                nuint size = (nuint)Unsafe.SizeOf<T>();
                return valueLength <= span.Length &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    ((uint)valueLength) * size);  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this api in such a case so we choose not to take the overhead of checking.
            }

            return valueLength <= span.Length && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(value), valueLength);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the start of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            int valueLength = value.Length;
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                nuint size = (nuint)Unsafe.SizeOf<T>();
                return valueLength <= span.Length &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    ((uint)valueLength) * size);  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this api in such a case so we choose not to take the overhead of checking.
            }

            return valueLength <= span.Length && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(value), valueLength);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the end of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            int spanLength = span.Length;
            int valueLength = value.Length;
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                nuint size = (nuint)Unsafe.SizeOf<T>();
                return valueLength <= spanLength &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)(spanLength - valueLength) /* force zero-extension */)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    ((uint)valueLength) * size);  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this api in such a case so we choose not to take the overhead of checking.
            }

            return valueLength <= spanLength &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)(spanLength - valueLength) /* force zero-extension */),
                    ref MemoryMarshal.GetReference(value),
                    valueLength);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the end of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            int spanLength = span.Length;
            int valueLength = value.Length;
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                nuint size = (nuint)Unsafe.SizeOf<T>();
                return valueLength <= spanLength &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)(spanLength - valueLength) /* force zero-extension */)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    ((uint)valueLength) * size);  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this api in such a case so we choose not to take the overhead of checking.
            }

            return valueLength <= spanLength &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)(spanLength - valueLength) /* force zero-extension */),
                    ref MemoryMarshal.GetReference(value),
                    valueLength);
        }

        /// <summary>
        /// Reverses the sequence of the elements in the entire span.
        /// </summary>
        public static void Reverse<T>(this Span<T> span)
        {
            if (span.Length <= 1)
            {
                return;
            }

            ref T first = ref MemoryMarshal.GetReference(span);
            ref T last = ref Unsafe.Add(ref Unsafe.Add(ref first, span.Length), -1);
            do
            {
                T temp = first;
                first = last;
                last = temp;
                first = ref Unsafe.Add(ref first, 1);
                last = ref Unsafe.Add(ref last, -1);
            } while (Unsafe.IsAddressLessThan(ref first, ref last));
        }

        /// <summary>
        /// Creates a new span over the target array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this T[]? array)
        {
            return new Span<T>(array);
        }

        /// <summary>
        /// Creates a new Span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the Span.</param>
        /// <param name="length">The number of items in the Span.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this T[]? array, int start, int length)
        {
            return new Span<T>(array, start, length);
        }

        /// <summary>
        /// Creates a new span over the portion of the target array segment.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ArraySegment<T> segment)
        {
            return new Span<T>(segment.Array, segment.Offset, segment.Count);
        }

        /// <summary>
        /// Creates a new Span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="segment">The target array.</param>
        /// <param name="start">The index at which to begin the Span.</param>
        /// <remarks>Returns default when <paramref name="segment"/> is null.</remarks>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="segment"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;segment.Count).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ArraySegment<T> segment, int start)
        {
            if (((uint)start) > (uint)segment.Count)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            return new Span<T>(segment.Array, segment.Offset + start, segment.Count - start);
        }

        /// <summary>
        /// Creates a new Span over the portion of the target array beginning
        /// at 'startIndex' and ending at the end of the segment.
        /// </summary>
        /// <param name="segment">The target array.</param>
        /// <param name="startIndex">The index at which to begin the Span.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ArraySegment<T> segment, Index startIndex)
        {
            int actualIndex = startIndex.GetOffset(segment.Count);
            return AsSpan(segment, actualIndex);
        }

        /// <summary>
        /// Creates a new Span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="segment">The target array.</param>
        /// <param name="start">The index at which to begin the Span.</param>
        /// <param name="length">The number of items in the Span.</param>
        /// <remarks>Returns default when <paramref name="segment"/> is null.</remarks>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="segment"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;segment.Count).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ArraySegment<T> segment, int start, int length)
        {
            if (((uint)start) > (uint)segment.Count)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
            if (((uint)length) > (uint)(segment.Count - start))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);

            return new Span<T>(segment.Array, segment.Offset + start, length);
        }

        /// <summary>
        /// Creates a new Span over the portion of the target array using the range start and end indexes
        /// </summary>
        /// <param name="segment">The target array.</param>
        /// <param name="range">The range which has start and end indexes to use for slicing the array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ArraySegment<T> segment, Range range)
        {
            (int start, int length) = range.GetOffsetAndLength(segment.Count);
            return new Span<T>(segment.Array, segment.Offset + start, length);
        }

        /// <summary>
        /// Creates a new memory over the target array.
        /// </summary>
        public static Memory<T> AsMemory<T>(this T[]? array) => new Memory<T>(array);

        /// <summary>
        /// Creates a new memory over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;array.Length).
        /// </exception>
        public static Memory<T> AsMemory<T>(this T[]? array, int start) => new Memory<T>(array, start);

        /// <summary>
        /// Creates a new memory over the portion of the target array starting from
        /// 'startIndex' to the end of the array.
        /// </summary>
        public static Memory<T> AsMemory<T>(this T[]? array, Index startIndex)
        {
            if (array == null)
            {
                if (!startIndex.Equals(Index.Start))
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

                return default;
            }

            int actualIndex = startIndex.GetOffset(array.Length);
            return new Memory<T>(array, actualIndex);
        }

        /// <summary>
        /// Creates a new memory over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;Length).
        /// </exception>
        public static Memory<T> AsMemory<T>(this T[]? array, int start, int length) => new Memory<T>(array, start, length);

        /// <summary>
        /// Creates a new memory over the portion of the target array beginning at inclusive start index of the range
        /// and ending at the exclusive end index of the range.
        /// </summary>
        public static Memory<T> AsMemory<T>(this T[]? array, Range range)
        {
            if (array == null)
            {
                Index startIndex = range.Start;
                Index endIndex = range.End;
                if (!startIndex.Equals(Index.Start) || !endIndex.Equals(Index.Start))
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

                return default;
            }

            (int start, int length) = range.GetOffsetAndLength(array.Length);
            return new Memory<T>(array, start, length);
        }

        /// <summary>
        /// Creates a new memory over the portion of the target array.
        /// </summary>
        public static Memory<T> AsMemory<T>(this ArraySegment<T> segment) => new Memory<T>(segment.Array, segment.Offset, segment.Count);

        /// <summary>
        /// Creates a new memory over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="segment">The target array.</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <remarks>Returns default when <paramref name="segment"/> is null.</remarks>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="segment"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;segment.Count).
        /// </exception>
        public static Memory<T> AsMemory<T>(this ArraySegment<T> segment, int start)
        {
            if (((uint)start) > (uint)segment.Count)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            return new Memory<T>(segment.Array, segment.Offset + start, segment.Count - start);
        }

        /// <summary>
        /// Creates a new memory over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="segment">The target array.</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        /// <remarks>Returns default when <paramref name="segment"/> is null.</remarks>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="segment"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;segment.Count).
        /// </exception>
        public static Memory<T> AsMemory<T>(this ArraySegment<T> segment, int start, int length)
        {
            if (((uint)start) > (uint)segment.Count)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
            if (((uint)length) > (uint)(segment.Count - start))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);

            return new Memory<T>(segment.Array, segment.Offset + start, length);
        }

        /// <summary>
        /// Copies the contents of the array into the span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        ///
        ///<param name="source">The array to copy items from.</param>
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the destination Span is shorter than the source array.
        /// </exception>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this T[]? source, Span<T> destination)
        {
            new ReadOnlySpan<T>(source).CopyTo(destination);
        }

        /// <summary>
        /// Copies the contents of the array into the memory. If the source
        /// and destinations overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.
        ///
        ///<param name="source">The array to copy items from.</param>
        /// <param name="destination">The memory to copy items into.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the destination is shorter than the source array.
        /// </exception>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this T[]? source, Memory<T> destination)
        {
            source.CopyTo(destination.Span);
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
        //  unsigned integer arithmetic modulo 2^32.
        //
        //  Example
        //  -------
        //
        //  Let's say there are two sequences, x and y. Let
        //
        //      ref T xRef = MemoryMarshal.GetReference(x)
        //      uint xLength = x.Length * Unsafe.SizeOf<T>()
        //      ref T yRef = MemoryMarshal.GetReference(y)
        //      uint yLength = y.Length * Unsafe.SizeOf<T>()
        //
        //  Visually, the two sequences are located somewhere in the 32-bit
        //  address space as follows:
        //
        //      [----------------------------------------------)                            normal address space
        //      0                                             2^32
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
        //                            0            .     .                          2^32
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
        //  (yRef - xRef + 2^32). So, in the example above, y1 moves to the right
        //  of x2.
        //
        //  y2 is simply y1 + yLength. Note that this can overflow, as in the
        //  example above, which must be avoided.
        //
        //  The two sequences do *not* overlap if y is entirely in the space
        //  right of x in the relative address space. (It can't be left of it!)
        //
        //          (y1 >= x2) && (y2 <= 2^32)
        //
        //  Inversely, they do overlap if
        //
        //          (y1 < x2) || (y2 > 2^32)
        //
        //  After substituting x2 and y2 with their respective definition:
        //
        //      == (y1 < xLength) || (y1 + yLength > 2^32)
        //
        //  Since yLength can't be greater than the size of the address space,
        //  the overflow can be avoided as follows:
        //
        //      == (y1 < xLength) || (y1 > 2^32 - yLength)
        //
        //  However, 2^32 cannot be stored in an unsigned 32-bit integer, so one
        //  more change is needed to keep doing everything with unsigned 32-bit
        //  integers:
        //
        //      == (y1 < xLength) || (y1 > -yLength)
        //
        //  Due to modulo arithmetic, this gives exactly same result *except* if
        //  yLength is zero, since 2^32 - 0 is 0 and not 2^32. So the case
        //  y.IsEmpty must be handled separately first.
        //

        /// <summary>
        /// Determines whether two sequences overlap in memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlaps<T>(this Span<T> span, ReadOnlySpan<T> other)
        {
            return Overlaps((ReadOnlySpan<T>)span, other);
        }

        /// <summary>
        /// Determines whether two sequences overlap in memory and outputs the element offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlaps<T>(this Span<T> span, ReadOnlySpan<T> other, out int elementOffset)
        {
            return Overlaps((ReadOnlySpan<T>)span, other, out elementOffset);
        }

        /// <summary>
        /// Determines whether two sequences overlap in memory.
        /// </summary>
        public static bool Overlaps<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other)
        {
            if (span.IsEmpty || other.IsEmpty)
            {
                return false;
            }

            IntPtr byteOffset = Unsafe.ByteOffset(
                ref MemoryMarshal.GetReference(span),
                ref MemoryMarshal.GetReference(other));

            if (Unsafe.SizeOf<IntPtr>() == sizeof(int))
            {
                return (uint)byteOffset < (uint)(span.Length * Unsafe.SizeOf<T>()) ||
                       (uint)byteOffset > (uint)-(other.Length * Unsafe.SizeOf<T>());
            }
            else
            {
                return (ulong)byteOffset < (ulong)((long)span.Length * Unsafe.SizeOf<T>()) ||
                       (ulong)byteOffset > (ulong)-((long)other.Length * Unsafe.SizeOf<T>());
            }
        }

        /// <summary>
        /// Determines whether two sequences overlap in memory and outputs the element offset.
        /// </summary>
        public static bool Overlaps<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, out int elementOffset)
        {
            if (span.IsEmpty || other.IsEmpty)
            {
                elementOffset = 0;
                return false;
            }

            IntPtr byteOffset = Unsafe.ByteOffset(
                ref MemoryMarshal.GetReference(span),
                ref MemoryMarshal.GetReference(other));

            if (Unsafe.SizeOf<IntPtr>() == sizeof(int))
            {
                if ((uint)byteOffset < (uint)(span.Length * Unsafe.SizeOf<T>()) ||
                    (uint)byteOffset > (uint)-(other.Length * Unsafe.SizeOf<T>()))
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
                if ((ulong)byteOffset < (ulong)((long)span.Length * Unsafe.SizeOf<T>()) ||
                    (ulong)byteOffset > (ulong)-((long)other.Length * Unsafe.SizeOf<T>()))
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
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name = "comparable" /> is <see langword="null"/> .
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
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name = "comparable" /> is <see langword="null"/> .
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
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name = "comparer" /> is <see langword="null"/> .
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
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name = "comparable" /> is <see langword="null"/> .
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
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name = "comparable" /> is <see langword="null"/> .
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
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name = "comparer" /> is <see langword="null"/> .
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

        /// <summary>
        /// Sorts the elements in the entire <see cref="Span{T}" /> using the <see cref="IComparable{T}" /> implementation
        /// of each element of the <see cref= "Span{T}" />
        /// </summary>
        /// <typeparam name="T">The type of the elements of the span.</typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to sort.</param>
        /// <exception cref="InvalidOperationException">
        /// One or more elements in <paramref name="span"/> do not implement the <see cref="IComparable{T}" /> interface.
        /// </exception>
        public static void Sort<T>(this Span<T> span) =>
            Sort(span, (IComparer<T>?)null);

        /// <summary>
        /// Sorts the elements in the entire <see cref="Span{T}" /> using the <typeparamref name="TComparer" />.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the span.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer to use to compare elements.</typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to sort.</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing elements, or null to
        /// use the <see cref="IComparable{T}"/> interface implementation of each element.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="comparer"/> is null, and one or more elements in <paramref name="span"/> do not
        /// implement the <see cref="IComparable{T}" /> interface.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The implementation of <paramref name="comparer"/> caused an error during the sort.
        /// </exception>
        public static void Sort<T, TComparer>(this Span<T> span, TComparer comparer)
            where TComparer : IComparer<T>?
        {
            if (span.Length > 1)
            {
                ArraySortHelper<T>.Default.Sort(span, comparer); // value-type comparer will be boxed
            }
        }

        /// <summary>
        /// Sorts the elements in the entire <see cref="Span{T}" /> using the specified <see cref="Comparison{T}" />.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the span.</typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to sort.</param>
        /// <param name="comparison">The <see cref="Comparison{T}"/> to use when comparing elements.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is null.</exception>
        public static void Sort<T>(this Span<T> span, Comparison<T> comparison)
        {
            if (comparison == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);

            if (span.Length > 1)
            {
                ArraySortHelper<T>.Sort(span, comparison);
            }
        }

        /// <summary>
        /// Sorts a pair of spans (one containing the keys and the other containing the corresponding items)
        /// based on the keys in the first <see cref="Span{TKey}" /> using the <see cref="IComparable{T}" />
        /// implementation of each key.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements of the key span.</typeparam>
        /// <typeparam name="TValue">The type of the elements of the items span.</typeparam>
        /// <param name="keys">The span that contains the keys to sort.</param>
        /// <param name="items">The span that contains the items that correspond to the keys in <paramref name="keys"/>.</param>
        /// <exception cref="ArgumentException">
        /// The length of <paramref name="keys"/> isn't equal to the length of <paramref name="items"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// One or more elements in <paramref name="keys"/> do not implement the <see cref="IComparable{T}" /> interface.
        /// </exception>
        public static void Sort<TKey, TValue>(this Span<TKey> keys, Span<TValue> items) =>
            Sort(keys, items, (IComparer<TKey>?)null);

        /// <summary>
        /// Sorts a pair of spans (one containing the keys and the other containing the corresponding items)
        /// based on the keys in the first <see cref="Span{TKey}" /> using the specified comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements of the key span.</typeparam>
        /// <typeparam name="TValue">The type of the elements of the items span.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer to use to compare elements.</typeparam>
        /// <param name="keys">The span that contains the keys to sort.</param>
        /// <param name="items">The span that contains the items that correspond to the keys in <paramref name="keys"/>.</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing elements, or null to
        /// use the <see cref="IComparable{T}"/> interface implementation of each element.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The length of <paramref name="keys"/> isn't equal to the length of <paramref name="items"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="comparer"/> is null, and one or more elements in <paramref name="keys"/> do not
        /// implement the <see cref="IComparable{T}" /> interface.
        /// </exception>
        public static void Sort<TKey, TValue, TComparer>(this Span<TKey> keys, Span<TValue> items, TComparer comparer)
            where TComparer : IComparer<TKey>?
        {
            if (keys.Length != items.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_SpansMustHaveSameLength);

            if (keys.Length > 1)
            {
                ArraySortHelper<TKey, TValue>.Default.Sort(keys, items, comparer); // value-type comparer will be boxed
            }
        }

        /// <summary>
        /// Sorts a pair of spans (one containing the keys and the other containing the corresponding items)
        /// based on the keys in the first <see cref="Span{TKey}" /> using the specified comparison.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements of the key span.</typeparam>
        /// <typeparam name="TValue">The type of the elements of the items span.</typeparam>
        /// <param name="keys">The span that contains the keys to sort.</param>
        /// <param name="items">The span that contains the items that correspond to the keys in <paramref name="keys"/>.</param>
        /// <param name="comparison">The <see cref="Comparison{T}"/> to use when comparing elements.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// The length of <paramref name="keys"/> isn't equal to the length of <paramref name="items"/>.
        /// </exception>
        public static void Sort<TKey, TValue>(this Span<TKey> keys, Span<TValue> items, Comparison<TKey> comparison)
        {
            if (comparison == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
            if (keys.Length != items.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_SpansMustHaveSameLength);

            if (keys.Length > 1)
            {
                ArraySortHelper<TKey, TValue>.Default.Sort(keys, items, new ComparisonComparer<TKey>(comparison));
            }
        }

        /// <summary>Writes the specified interpolated string to the character span.</summary>
        /// <param name="destination">The span to which the interpolated string should be formatted.</param>
        /// <param name="handler">The interpolated string.</param>
        /// <param name="charsWritten">The number of characters written to the span.</param>
        /// <returns>true if the entire interpolated string could be formatted successfully; otherwise, false.</returns>
        public static bool TryWrite(this Span<char> destination, [InterpolatedStringHandlerArgument("destination")] ref TryWriteInterpolatedStringHandler handler, out int charsWritten)
        {
            // The span argument isn't used directly in the method; rather, it'll be used by the compiler to create the handler.
            // We could validate here that span == handler._destination, but that doesn't seem necessary.
            if (handler._success)
            {
                charsWritten = handler._pos;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        /// <summary>Writes the specified interpolated string to the character span.</summary>
        /// <param name="destination">The span to which the interpolated string should be formatted.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The interpolated string.</param>
        /// <param name="charsWritten">The number of characters written to the span.</param>
        /// <returns>true if the entire interpolated string could be formatted successfully; otherwise, false.</returns>
        public static bool TryWrite(this Span<char> destination, IFormatProvider? provider, [InterpolatedStringHandlerArgument("destination", "provider")] ref TryWriteInterpolatedStringHandler handler, out int charsWritten) =>
            // The provider is passed to the handler by the compiler, so the actual implementation of the method
            // is the same as the non-provider overload.
            TryWrite(destination, ref handler, out charsWritten);

        /// <summary>Provides a handler used by the language compiler to format interpolated strings into character spans.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [InterpolatedStringHandler]
        public ref struct TryWriteInterpolatedStringHandler
        {
            // Implementation note:
            // As this type is only intended to be targeted by the compiler, public APIs eschew argument validation logic
            // in a variety of places, e.g. allowing a null input when one isn't expected to produce a NullReferenceException rather
            // than an ArgumentNullException.

            /// <summary>The destination buffer.</summary>
            private readonly Span<char> _destination;
            /// <summary>Optional provider to pass to IFormattable.ToString or ISpanFormattable.TryFormat calls.</summary>
            private readonly IFormatProvider? _provider;
            /// <summary>The number of characters written to <see cref="_destination"/>.</summary>
            internal int _pos;
            /// <summary>true if all formatting operations have succeeded; otherwise, false.</summary>
            internal bool _success;
            /// <summary>Whether <see cref="_provider"/> provides an ICustomFormatter.</summary>
            /// <remarks>
            /// Custom formatters are very rare.  We want to support them, but it's ok if we make them more expensive
            /// in order to make them as pay-for-play as possible.  So, we avoid adding another reference type field
            /// to reduce the size of the handler and to reduce required zero'ing, by only storing whether the provider
            /// provides a formatter, rather than actually storing the formatter.  This in turn means, if there is a
            /// formatter, we pay for the extra interface call on each AppendFormatted that needs it.
            /// </remarks>
            private readonly bool _hasCustomFormatter;

            /// <summary>Creates a handler used to write an interpolated string into a <see cref="Span{Char}"/>.</summary>
            /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
            /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
            /// <param name="destination">The destination buffer.</param>
            /// <param name="shouldAppend">Upon return, true if the destination may be long enough to support the formatting, or false if it won't be.</param>
            /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
            public TryWriteInterpolatedStringHandler(int literalLength, int formattedCount, Span<char> destination, out bool shouldAppend)
            {
                _destination = destination;
                _provider = null;
                _pos = 0;
                _success = shouldAppend = destination.Length >= literalLength;
                _hasCustomFormatter = false;
            }

            /// <summary>Creates a handler used to write an interpolated string into a <see cref="Span{Char}"/>.</summary>
            /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
            /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
            /// <param name="destination">The destination buffer.</param>
            /// <param name="provider">An object that supplies culture-specific formatting information.</param>
            /// <param name="shouldAppend">Upon return, true if the destination may be long enough to support the formatting, or false if it won't be.</param>
            /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
            public TryWriteInterpolatedStringHandler(int literalLength, int formattedCount, Span<char> destination, IFormatProvider? provider, out bool shouldAppend)
            {
                _destination = destination;
                _provider = provider;
                _pos = 0;
                _success = shouldAppend = destination.Length >= literalLength;
                _hasCustomFormatter = provider is not null && DefaultInterpolatedStringHandler.HasCustomFormatter(provider);
            }

            /// <summary>Writes the specified string to the handler.</summary>
            /// <param name="value">The string to write.</param>
            /// <returns>true if the value could be formatted to the span; otherwise, false.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AppendLiteral(string value)
            {
                // See comment on inlining and special-casing in DefaultInterpolatedStringHandler.AppendLiteral.

                if (value.Length == 1)
                {
                    Span<char> destination = _destination;
                    int pos = _pos;
                    if ((uint)pos < (uint)destination.Length)
                    {
                        destination[pos] = value[0];
                        _pos = pos + 1;
                        return true;
                    }

                    return Fail();
                }

                if (value.Length == 2)
                {
                    Span<char> destination = _destination;
                    int pos = _pos;
                    if ((uint)pos < destination.Length - 1)
                    {
                        Unsafe.WriteUnaligned(
                            ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(destination), pos)),
                            Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref value.GetRawStringData())));
                        _pos = pos + 2;
                        return true;
                    }

                    return Fail();
                }

                return AppendStringDirect(value);
            }

            /// <summary>Writes the specified string to the handler.</summary>
            /// <param name="value">The string to write.</param>
            /// <returns>true if the value could be appended to the span; otherwise, false.</returns>
            private bool AppendStringDirect(string value)
            {
                if (value.TryCopyTo(_destination.Slice(_pos)))
                {
                    _pos += value.Length;
                    return true;
                }

                return Fail();
            }

            #region AppendFormatted
            // Design note:
            // This provides the same set of overloads and semantics as DefaultInterpolatedStringHandler.

            #region AppendFormatted T
            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public bool AppendFormatted<T>(T value)
            {
                // This method could delegate to AppendFormatted with a null format, but explicitly passing
                // default as the format to TryFormat helps to improve code quality in some cases when TryFormat is inlined,
                // e.g. for Int32 it enables the JIT to eliminate code in the inlined method based on a length check on the format.

                // If there's a custom formatter, always use it.
                if (_hasCustomFormatter)
                {
                    return AppendCustomFormatter(value, format: null);
                }

                // Check first for IFormattable, even though we'll prefer to use ISpanFormattable, as the latter
                // derives from the former.  For value types, it won't matter as the type checks devolve into
                // JIT-time constants.  For reference types, they're more likely to implement IFormattable
                // than they are to implement ISpanFormattable: if they don't implement either, we save an
                // interface check over first checking for ISpanFormattable and then for IFormattable, and
                // if it only implements IFormattable, we come out even: only if it implements both do we
                // end up paying for an extra interface check.
                string? s;
                if (value is IFormattable)
                {
                    // If the value can format itself directly into our buffer, do so.
                    if (value is ISpanFormattable)
                    {
                        int charsWritten;
                        if (((ISpanFormattable)value).TryFormat(_destination.Slice(_pos), out charsWritten, default, _provider)) // constrained call avoiding boxing for value types
                        {
                            _pos += charsWritten;
                            return true;
                        }

                        return Fail();
                    }

                    s = ((IFormattable)value).ToString(format: null, _provider); // constrained call avoiding boxing for value types
                }
                else
                {
                    s = value?.ToString();
                }

                return s is null || AppendStringDirect(s);
            }

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public bool AppendFormatted<T>(T value, string? format)
            {
                // If there's a custom formatter, always use it.
                if (_hasCustomFormatter)
                {
                    return AppendCustomFormatter(value, format);
                }

                // Check first for IFormattable, even though we'll prefer to use ISpanFormattable, as the latter
                // derives from the former.  For value types, it won't matter as the type checks devolve into
                // JIT-time constants.  For reference types, they're more likely to implement IFormattable
                // than they are to implement ISpanFormattable: if they don't implement either, we save an
                // interface check over first checking for ISpanFormattable and then for IFormattable, and
                // if it only implements IFormattable, we come out even: only if it implements both do we
                // end up paying for an extra interface check.
                string? s;
                if (value is IFormattable)
                {
                    // If the value can format itself directly into our buffer, do so.
                    if (value is ISpanFormattable)
                    {
                        int charsWritten;
                        if (((ISpanFormattable)value).TryFormat(_destination.Slice(_pos), out charsWritten, format, _provider)) // constrained call avoiding boxing for value types
                        {
                            _pos += charsWritten;
                            return true;
                        }

                        return Fail();
                    }

                    s = ((IFormattable)value).ToString(format, _provider); // constrained call avoiding boxing for value types
                }
                else
                {
                    s = value?.ToString();
                }

                return s is null || AppendStringDirect(s);
            }

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public bool AppendFormatted<T>(T value, int alignment)
            {
                int startingPos = _pos;
                if (AppendFormatted(value))
                {
                    return alignment == 0 || TryAppendOrInsertAlignmentIfNeeded(startingPos, alignment);
                }

                return Fail();
            }

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public bool AppendFormatted<T>(T value, int alignment, string? format)
            {
                int startingPos = _pos;
                if (AppendFormatted(value, format))
                {
                    return alignment == 0 || TryAppendOrInsertAlignmentIfNeeded(startingPos, alignment);
                }

                return Fail();
            }
            #endregion

            #region AppendFormatted ReadOnlySpan<char>
            /// <summary>Writes the specified character span to the handler.</summary>
            /// <param name="value">The span to write.</param>
            public bool AppendFormatted(ReadOnlySpan<char> value)
            {
                // Fast path for when the value fits in the current buffer
                if (value.TryCopyTo(_destination.Slice(_pos)))
                {
                    _pos += value.Length;
                    return true;
                }

                return Fail();
            }

            /// <summary>Writes the specified string of chars to the handler.</summary>
            /// <param name="value">The span to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public bool AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
            {
                bool leftAlign = false;
                if (alignment < 0)
                {
                    leftAlign = true;
                    alignment = -alignment;
                }

                int paddingRequired = alignment - value.Length;
                if (paddingRequired <= 0)
                {
                    // The value is as large or larger than the required amount of padding,
                    // so just write the value.
                    return AppendFormatted(value);
                }

                // Write the value along with the appropriate padding.
                Debug.Assert(alignment > value.Length);
                if (alignment <= _destination.Length - _pos)
                {
                    if (leftAlign)
                    {
                        value.CopyTo(_destination.Slice(_pos));
                        _pos += value.Length;
                        _destination.Slice(_pos, paddingRequired).Fill(' ');
                        _pos += paddingRequired;
                    }
                    else
                    {
                        _destination.Slice(_pos, paddingRequired).Fill(' ');
                        _pos += paddingRequired;
                        value.CopyTo(_destination.Slice(_pos));
                        _pos += value.Length;
                    }

                    return true;
                }

                return Fail();
            }
            #endregion

            #region AppendFormatted string
            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            public bool AppendFormatted(string? value)
            {
                if (_hasCustomFormatter)
                {
                    return AppendCustomFormatter(value, format: null);
                }

                if (value is null)
                {
                    return true;
                }

                if (value.TryCopyTo(_destination.Slice(_pos)))
                {
                    _pos += value.Length;
                    return true;
                }

                return Fail();
            }

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public bool AppendFormatted(string? value, int alignment = 0, string? format = null) =>
                // Format is meaningless for strings and doesn't make sense for someone to specify.  We have the overload
                // simply to disambiguate between ROS<char> and object, just in case someone does specify a format, as
                // string is implicitly convertible to both. Just delegate to the T-based implementation.
                AppendFormatted<string?>(value, alignment, format);
            #endregion

            #region AppendFormatted object
            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public bool AppendFormatted(object? value, int alignment = 0, string? format = null) =>
                // This overload is expected to be used rarely, only if either a) something strongly typed as object is
                // formatted with both an alignment and a format, or b) the compiler is unable to target type to T. It
                // exists purely to help make cases from (b) compile. Just delegate to the T-based implementation.
                AppendFormatted<object?>(value, alignment, format);
            #endregion
            #endregion

            /// <summary>Formats the value using the custom formatter from the provider.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            [MethodImpl(MethodImplOptions.NoInlining)]
            private bool AppendCustomFormatter<T>(T value, string? format)
            {
                // This case is very rare, but we need to handle it prior to the other checks in case
                // a provider was used that supplied an ICustomFormatter which wanted to intercept the particular value.
                // We do the cast here rather than in the ctor, even though this could be executed multiple times per
                // formatting, to make the cast pay for play.
                Debug.Assert(_hasCustomFormatter);
                Debug.Assert(_provider != null);

                ICustomFormatter? formatter = (ICustomFormatter?)_provider.GetFormat(typeof(ICustomFormatter));
                Debug.Assert(formatter != null, "An incorrectly written provider said it implemented ICustomFormatter, and then didn't");

                if (formatter is not null && formatter.Format(format, value, _provider) is string customFormatted)
                {
                    return AppendStringDirect(customFormatted);
                }

                return true;
            }

            /// <summary>Handles adding any padding required for aligning a formatted value in an interpolation expression.</summary>
            /// <param name="startingPos">The position at which the written value started.</param>
            /// <param name="alignment">Non-zero minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            private bool TryAppendOrInsertAlignmentIfNeeded(int startingPos, int alignment)
            {
                Debug.Assert(startingPos >= 0 && startingPos <= _pos);
                Debug.Assert(alignment != 0);

                int charsWritten = _pos - startingPos;

                bool leftAlign = false;
                if (alignment < 0)
                {
                    leftAlign = true;
                    alignment = -alignment;
                }

                int paddingNeeded = alignment - charsWritten;
                if (paddingNeeded <= 0)
                {
                    return true;
                }

                if (paddingNeeded <= _destination.Length - _pos)
                {
                    if (leftAlign)
                    {
                        _destination.Slice(_pos, paddingNeeded).Fill(' ');
                    }
                    else
                    {
                        _destination.Slice(startingPos, charsWritten).CopyTo(_destination.Slice(startingPos + paddingNeeded));
                        _destination.Slice(startingPos, paddingNeeded).Fill(' ');
                    }

                    _pos += paddingNeeded;
                    return true;
                }

                return Fail();
            }

            /// <summary>Marks formatting as having failed and returns false.</summary>
            private bool Fail()
            {
                _success = false;
                return false;
            }
        }
    }
}
