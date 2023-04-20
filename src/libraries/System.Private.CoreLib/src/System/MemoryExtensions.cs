// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

#pragma warning disable 8500 // sizeof of managed types

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
        [Intrinsic] // When input is a string literal
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

        /// <summary>Creates a new <see cref="ReadOnlySpan{Char}"/> over a portion of the target string from a specified position to the end of the string.</summary>
        /// <param name="text">The target string.</param>
        /// <param name="startIndex">The index at which to begin this slice.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than 0 or greater than <paramref name="text"/>.Length.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> AsSpan(this string? text, Index startIndex)
        {
            if (text is null)
            {
                if (!startIndex.Equals(Index.Start))
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex);
                }

                return default;
            }

            int actualIndex = startIndex.GetOffset(text.Length);
            if ((uint)actualIndex > (uint)text.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex);
            }

            return new ReadOnlySpan<char>(ref Unsafe.Add(ref text.GetRawStringData(), (nint)(uint)actualIndex /* force zero-extension */), text.Length - actualIndex);
        }

        /// <summary>Creates a new <see cref="ReadOnlySpan{Char}"/> over a portion of a target string using the range start and end indexes.</summary>
        /// <param name="text">The target string.</param>
        /// <param name="range">The range which has start and end indexes to use for slicing the string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="range"/>'s start or end index is not within the bounds of the string.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="range"/>'s start index is greater than its end index.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> AsSpan(this string? text, Range range)
        {
            if (text is null)
            {
                Index startIndex = range.Start;
                Index endIndex = range.End;

                if (!startIndex.Equals(Index.Start) || !endIndex.Equals(Index.Start))
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);
                }

                return default;
            }

            (int start, int length) = range.GetOffsetAndLength(text.Length);
            return new ReadOnlySpan<char>(ref Unsafe.Add(ref text.GetRawStringData(), (nint)(uint)start /* force zero-extension */), length);
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
        public static unsafe bool Contains<T>(this Span<T> span, T value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.ContainsValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.ContainsValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(int))
                {
                    return SpanHelpers.ContainsValueType(
                        ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, int>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(long))
                {
                    return SpanHelpers.ContainsValueType(
                        ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, long>(ref value),
                        span.Length);
                }
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
        public static unsafe bool Contains<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.ContainsValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.ContainsValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(int))
                {
                    return SpanHelpers.ContainsValueType(
                        ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, int>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(long))
                {
                    return SpanHelpers.ContainsValueType(
                        ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, long>(ref value),
                        span.Length);
                }
            }

            return SpanHelpers.Contains(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int IndexOf<T>(this Span<T> span, T value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                    return SpanHelpers.IndexOfValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);

                if (sizeof(T) == sizeof(short))
                    return SpanHelpers.IndexOfValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value),
                        span.Length);

                if (sizeof(T) == sizeof(int))
                    return SpanHelpers.IndexOfValueType(
                        ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, int>(ref value),
                        span.Length);

                if (sizeof(T) == sizeof(long))
                    return SpanHelpers.IndexOfValueType(
                        ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, long>(ref value),
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
        public static unsafe int IndexOf<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                    return SpanHelpers.IndexOf(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                        value.Length);

                if (sizeof(T) == sizeof(char))
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
        public static unsafe int LastIndexOf<T>(this Span<T> span, T value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(int))
                {
                    return SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, int>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(long))
                {
                    return SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, long>(ref value),
                        span.Length);
                }
            }

            return SpanHelpers.LastIndexOf<T>(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its last occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int LastIndexOf<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOf(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                        value.Length);
                }
                else if (sizeof(T) == sizeof(char))
                {
                    return SpanHelpers.LastIndexOf(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(value)),
                        value.Length);
                }
            }

            return SpanHelpers.LastIndexOf<T>(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }

        /// <summary>Searches for the first index of any value other than the specified <paramref name="value"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">A value to avoid.</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than <paramref name="value"/>.
        /// If all of the values are <paramref name="value"/>, returns -1.
        /// </returns>
        public static int IndexOfAnyExcept<T>(this Span<T> span, T value) where T : IEquatable<T>? =>
            IndexOfAnyExcept((ReadOnlySpan<T>)span, value);

        /// <summary>Searches for the first index of any value other than the specified <paramref name="value0"/> or <paramref name="value1"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">A value to avoid.</param>
        /// <param name="value1">A value to avoid</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than <paramref name="value0"/> and <paramref name="value1"/>.
        /// If all of the values are <paramref name="value0"/> or <paramref name="value1"/>, returns -1.
        /// </returns>
        public static int IndexOfAnyExcept<T>(this Span<T> span, T value0, T value1) where T : IEquatable<T>? =>
            IndexOfAnyExcept((ReadOnlySpan<T>)span, value0, value1);

        /// <summary>Searches for the first index of any value other than the specified <paramref name="value0"/>, <paramref name="value1"/>, or <paramref name="value2"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">A value to avoid.</param>
        /// <param name="value1">A value to avoid</param>
        /// <param name="value2">A value to avoid</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than <paramref name="value0"/>, <paramref name="value1"/>, and <paramref name="value2"/>.
        /// If all of the values are <paramref name="value0"/>, <paramref name="value1"/>, and <paramref name="value2"/>, returns -1.
        /// </returns>
        public static int IndexOfAnyExcept<T>(this Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>? =>
            IndexOfAnyExcept((ReadOnlySpan<T>)span, value0, value1, value2);

        /// <summary>Searches for the first index of any value other than the specified <paramref name="values"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The values to avoid.</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than those in <paramref name="values"/>.
        /// If all of the values are in <paramref name="values"/>, returns -1.
        /// </returns>
        public static int IndexOfAnyExcept<T>(this Span<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>? =>
            IndexOfAnyExcept((ReadOnlySpan<T>)span, values);

        /// <summary>Searches for the first index of any value other than the specified <paramref name="values"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The values to avoid.</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than those in <paramref name="values"/>.
        /// If all of the values are in <paramref name="values"/>, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAnyExcept<T>(this Span<T> span, IndexOfAnyValues<T> values) where T : IEquatable<T>? =>
            IndexOfAnyExcept((ReadOnlySpan<T>)span, values);

        /// <summary>Searches for the first index of any value other than the specified <paramref name="value"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">A value to avoid.</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than <paramref name="value"/>.
        /// If all of the values are <paramref name="value"/>, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int IndexOfAnyExcept<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.IndexOfAnyExceptValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.IndexOfAnyExceptValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(int))
                {
                    return SpanHelpers.IndexOfAnyExceptValueType(
                        ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, int>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(long))
                {
                    return SpanHelpers.IndexOfAnyExceptValueType(
                        ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, long>(ref value),
                        span.Length);
                }
            }

            return SpanHelpers.IndexOfAnyExcept(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>Searches for the first index of any value other than the specified <paramref name="value0"/> or <paramref name="value1"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">A value to avoid.</param>
        /// <param name="value1">A value to avoid</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than <paramref name="value0"/> and <paramref name="value1"/>.
        /// If all of the values are <paramref name="value0"/> or <paramref name="value1"/>, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int IndexOfAnyExcept<T>(this ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.IndexOfAnyExceptValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.IndexOfAnyExceptValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        span.Length);
                }
            }

            return SpanHelpers.IndexOfAnyExcept(ref MemoryMarshal.GetReference(span), value0, value1, span.Length);
        }

        /// <summary>Searches for the first index of any value other than the specified <paramref name="value0"/>, <paramref name="value1"/>, or <paramref name="value2"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">A value to avoid.</param>
        /// <param name="value1">A value to avoid</param>
        /// <param name="value2">A value to avoid</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than <paramref name="value0"/>, <paramref name="value1"/>, and <paramref name="value2"/>.
        /// If all of the values are <paramref name="value0"/>, <paramref name="value1"/>, and <paramref name="value2"/>, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int IndexOfAnyExcept<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.IndexOfAnyExceptValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        Unsafe.As<T, byte>(ref value2),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.IndexOfAnyExceptValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        Unsafe.As<T, short>(ref value2),
                        span.Length);
                }
            }

            return SpanHelpers.IndexOfAnyExcept(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int IndexOfAnyExcept<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2, T value3) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.IndexOfAnyExceptValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        Unsafe.As<T, byte>(ref value2),
                        Unsafe.As<T, byte>(ref value3),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.IndexOfAnyExceptValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        Unsafe.As<T, short>(ref value2),
                        Unsafe.As<T, short>(ref value3),
                        span.Length);
                }
            }

            return SpanHelpers.IndexOfAnyExcept(ref MemoryMarshal.GetReference(span), value0, value1, value2, value3, span.Length);
        }

        /// <summary>Searches for the first index of any value other than the specified <paramref name="values"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The values to avoid.</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than those in <paramref name="values"/>.
        /// If all of the values are in <paramref name="values"/>, returns -1.
        /// </returns>
        public static unsafe int IndexOfAnyExcept<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>?
        {
            switch (values.Length)
            {
                case 0:
                    // If the span is empty, we want to return -1.
                    // If the span is non-empty, we want to return the index of the first char that's not in the empty set,
                    // which is every character, and so the first char in the span.
                    return span.IsEmpty ? -1 : 0;

                case 1:
                    return IndexOfAnyExcept(span, values[0]);

                case 2:
                    return IndexOfAnyExcept(span, values[0], values[1]);

                case 3:
                    return IndexOfAnyExcept(span, values[0], values[1], values[2]);

                case 4:
                    return IndexOfAnyExcept(span, values[0], values[1], values[2], values[3]);

                default:
                    if (RuntimeHelpers.IsBitwiseEquatable<T>())
                    {
                        if (sizeof(T) == sizeof(byte) && values.Length == 5)
                        {
                            ref byte valuesRef = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values));

                            return SpanHelpers.IndexOfAnyExceptValueType(
                                ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                                valuesRef,
                                Unsafe.Add(ref valuesRef, 1),
                                Unsafe.Add(ref valuesRef, 2),
                                Unsafe.Add(ref valuesRef, 3),
                                Unsafe.Add(ref valuesRef, 4),
                                span.Length);
                        }
                        else if (sizeof(T) == sizeof(short) && values.Length == 5)
                        {
                            ref short valuesRef = ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(values));

                            return SpanHelpers.IndexOfAnyExceptValueType(
                                ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                                valuesRef,
                                Unsafe.Add(ref valuesRef, 1),
                                Unsafe.Add(ref valuesRef, 2),
                                Unsafe.Add(ref valuesRef, 3),
                                Unsafe.Add(ref valuesRef, 4),
                                span.Length);
                        }
                    }

                    if (RuntimeHelpers.IsBitwiseEquatable<T>() && sizeof(T) == sizeof(char))
                    {
                        return ProbabilisticMap.IndexOfAnyExcept(
                            ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                            span.Length,
                            ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(values)),
                            values.Length);
                    }

                    for (int i = 0; i < span.Length; i++)
                    {
                        if (!values.Contains(span[i]))
                        {
                            return i;
                        }
                    }

                    return -1;
            }
        }

        /// <summary>Searches for the first index of any value other than the specified <paramref name="values"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The values to avoid.</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than those in <paramref name="values"/>.
        /// If all of the values are in <paramref name="values"/>, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAnyExcept<T>(this ReadOnlySpan<T> span, IndexOfAnyValues<T> values) where T : IEquatable<T>? =>
            IndexOfAnyValues<T>.IndexOfAnyExcept(span, values);

        /// <summary>Searches for the last index of any value other than the specified <paramref name="value"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">A value to avoid.</param>
        /// <returns>
        /// The index in the span of the last occurrence of any value other than <paramref name="value"/>.
        /// If all of the values are <paramref name="value"/>, returns -1.
        /// </returns>
        public static int LastIndexOfAnyExcept<T>(this Span<T> span, T value) where T : IEquatable<T>? =>
            LastIndexOfAnyExcept((ReadOnlySpan<T>)span, value);

        /// <summary>Searches for the last index of any value other than the specified <paramref name="value0"/> or <paramref name="value1"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">A value to avoid.</param>
        /// <param name="value1">A value to avoid</param>
        /// <returns>
        /// The index in the span of the last occurrence of any value other than <paramref name="value0"/> and <paramref name="value1"/>.
        /// If all of the values are <paramref name="value0"/> or <paramref name="value1"/>, returns -1.
        /// </returns>
        public static int LastIndexOfAnyExcept<T>(this Span<T> span, T value0, T value1) where T : IEquatable<T>? =>
            LastIndexOfAnyExcept((ReadOnlySpan<T>)span, value0, value1);

        /// <summary>Searches for the last index of any value other than the specified <paramref name="value0"/>, <paramref name="value1"/>, or <paramref name="value2"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">A value to avoid.</param>
        /// <param name="value1">A value to avoid</param>
        /// <param name="value2">A value to avoid</param>
        /// <returns>
        /// The index in the span of the last occurrence of any value other than <paramref name="value0"/>, <paramref name="value1"/>, and <paramref name="value2"/>.
        /// If all of the values are <paramref name="value0"/>, <paramref name="value1"/>, and <paramref name="value2"/>, returns -1.
        /// </returns>
        public static int LastIndexOfAnyExcept<T>(this Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>? =>
            LastIndexOfAnyExcept((ReadOnlySpan<T>)span, value0, value1, value2);

        /// <summary>Searches for the last index of any value other than the specified <paramref name="values"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The values to avoid.</param>
        /// <returns>
        /// The index in the span of the last occurrence of any value other than those in <paramref name="values"/>.
        /// If all of the values are in <paramref name="values"/>, returns -1.
        /// </returns>
        public static int LastIndexOfAnyExcept<T>(this Span<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>? =>
            LastIndexOfAnyExcept((ReadOnlySpan<T>)span, values);

        /// <summary>Searches for the last index of any value other than the specified <paramref name="values"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The values to avoid.</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than those in <paramref name="values"/>.
        /// If all of the values are in <paramref name="values"/>, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAnyExcept<T>(this Span<T> span, IndexOfAnyValues<T> values) where T : IEquatable<T>? =>
            LastIndexOfAnyExcept((ReadOnlySpan<T>)span, values);

        /// <summary>Searches for the last index of any value other than the specified <paramref name="value"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">A value to avoid.</param>
        /// <returns>
        /// The index in the span of the last occurrence of any value other than <paramref name="value"/>.
        /// If all of the values are <paramref name="value"/>, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int LastIndexOfAnyExcept<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOfAnyExceptValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.LastIndexOfAnyExceptValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(int))
                {
                    return SpanHelpers.LastIndexOfAnyExceptValueType(
                        ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, int>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(long))
                {
                    return SpanHelpers.LastIndexOfAnyExceptValueType(
                        ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, long>(ref value),
                        span.Length);
                }
            }

            return SpanHelpers.LastIndexOfAnyExcept(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>Searches for the last index of any value other than the specified <paramref name="value0"/> or <paramref name="value1"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">A value to avoid.</param>
        /// <param name="value1">A value to avoid</param>
        /// <returns>
        /// The index in the span of the last occurrence of any value other than <paramref name="value0"/> and <paramref name="value1"/>.
        /// If all of the values are <paramref name="value0"/> or <paramref name="value1"/>, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int LastIndexOfAnyExcept<T>(this ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOfAnyExceptValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.LastIndexOfAnyExceptValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        span.Length);
                }
            }

            return SpanHelpers.LastIndexOfAnyExcept(ref MemoryMarshal.GetReference(span), value0, value1, span.Length);
        }

        /// <summary>Searches for the last index of any value other than the specified <paramref name="value0"/>, <paramref name="value1"/>, or <paramref name="value2"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">A value to avoid.</param>
        /// <param name="value1">A value to avoid</param>
        /// <param name="value2">A value to avoid</param>
        /// <returns>
        /// The index in the span of the last occurrence of any value other than <paramref name="value0"/>, <paramref name="value1"/>, and <paramref name="value2"/>.
        /// If all of the values are <paramref name="value0"/>, <paramref name="value1"/>, and <paramref name="value2"/>, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int LastIndexOfAnyExcept<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOfAnyExceptValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        Unsafe.As<T, byte>(ref value2),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.LastIndexOfAnyExceptValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        Unsafe.As<T, short>(ref value2),
                        span.Length);
                }
            }

            return SpanHelpers.LastIndexOfAnyExcept(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int LastIndexOfAnyExcept<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2, T value3) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOfAnyExceptValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        Unsafe.As<T, byte>(ref value2),
                        Unsafe.As<T, byte>(ref value3),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.LastIndexOfAnyExceptValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        Unsafe.As<T, short>(ref value2),
                        Unsafe.As<T, short>(ref value3),
                        span.Length);
                }
            }

            return SpanHelpers.LastIndexOfAnyExcept(ref MemoryMarshal.GetReference(span), value0, value1, value2, value3, span.Length);
        }

        /// <summary>Searches for the last index of any value other than the specified <paramref name="values"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The values to avoid.</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than those in <paramref name="values"/>.
        /// If all of the values are in <paramref name="values"/>, returns -1.
        /// </returns>
        public static unsafe int LastIndexOfAnyExcept<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>?
        {
            switch (values.Length)
            {
                case 0:
                    // If the span is empty, we want to return -1.
                    // If the span is non-empty, we want to return the index of the last char that's not in the empty set,
                    // which is every character, and so the last char in the span.
                    // Either way, we want to return span.Length - 1.
                    return span.Length - 1;

                case 1:
                    return LastIndexOfAnyExcept(span, values[0]);

                case 2:
                    return LastIndexOfAnyExcept(span, values[0], values[1]);

                case 3:
                    return LastIndexOfAnyExcept(span, values[0], values[1], values[2]);

                case 4:
                    return LastIndexOfAnyExcept(span, values[0], values[1], values[2], values[3]);

                default:
                    if (RuntimeHelpers.IsBitwiseEquatable<T>())
                    {
                        if (sizeof(T) == sizeof(byte) && values.Length == 5)
                        {
                            ref byte valuesRef = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values));

                            return SpanHelpers.LastIndexOfAnyExceptValueType(
                                ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                                valuesRef,
                                Unsafe.Add(ref valuesRef, 1),
                                Unsafe.Add(ref valuesRef, 2),
                                Unsafe.Add(ref valuesRef, 3),
                                Unsafe.Add(ref valuesRef, 4),
                                span.Length);
                        }
                        else if (sizeof(T) == sizeof(short) && values.Length == 5)
                        {
                            ref short valuesRef = ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(values));

                            return SpanHelpers.LastIndexOfAnyExceptValueType(
                                ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                                valuesRef,
                                Unsafe.Add(ref valuesRef, 1),
                                Unsafe.Add(ref valuesRef, 2),
                                Unsafe.Add(ref valuesRef, 3),
                                Unsafe.Add(ref valuesRef, 4),
                                span.Length);
                        }
                    }

                    if (RuntimeHelpers.IsBitwiseEquatable<T>() && sizeof(T) == sizeof(char))
                    {
                        return ProbabilisticMap.LastIndexOfAnyExcept(
                            ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                            span.Length,
                            ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(values)),
                            values.Length);
                    }

                    for (int i = span.Length - 1; i >= 0; i--)
                    {
                        if (!values.Contains(span[i]))
                        {
                            return i;
                        }
                    }

                    return -1;
            }
        }

        /// <summary>Searches for the last index of any value other than the specified <paramref name="values"/>.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The values to avoid.</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value other than those in <paramref name="values"/>.
        /// If all of the values are in <paramref name="values"/>, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAnyExcept<T>(this ReadOnlySpan<T> span, IndexOfAnyValues<T> values) where T : IEquatable<T>? =>
            IndexOfAnyValues<T>.LastIndexOfAnyExcept(span, values);

        /// <inheritdoc cref="IndexOfAnyInRange{T}(ReadOnlySpan{T}, T, T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAnyInRange<T>(this Span<T> span, T lowInclusive, T highInclusive)
            where T : IComparable<T> =>
            IndexOfAnyInRange((ReadOnlySpan<T>)span, lowInclusive, highInclusive);

        /// <summary>Searches for the first index of any value in the range between <paramref name="lowInclusive"/> and <paramref name="highInclusive"/>, inclusive.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="lowInclusive">A lower bound, inclusive, of the range for which to search.</param>
        /// <param name="highInclusive">A upper bound, inclusive, of the range for which to search.</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value in the specified range.
        /// If all of the values are outside of the specified range, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAnyInRange<T>(this ReadOnlySpan<T> span, T lowInclusive, T highInclusive)
            where T : IComparable<T>
        {
            if (lowInclusive is null || highInclusive is null)
            {
                ThrowNullLowHighInclusive(lowInclusive, highInclusive);
            }

            if (Vector128.IsHardwareAccelerated)
            {
                if (lowInclusive is byte or sbyte)
                {
                    return SpanHelpers.IndexOfAnyInRangeUnsignedNumber(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref lowInclusive),
                        Unsafe.As<T, byte>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is short or ushort or char)
                {
                    return SpanHelpers.IndexOfAnyInRangeUnsignedNumber(
                        ref Unsafe.As<T, ushort>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, ushort>(ref lowInclusive),
                        Unsafe.As<T, ushort>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is int or uint || (IntPtr.Size == 4 && (lowInclusive is nint or nuint)))
                {
                    return SpanHelpers.IndexOfAnyInRangeUnsignedNumber(
                        ref Unsafe.As<T, uint>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, uint>(ref lowInclusive),
                        Unsafe.As<T, uint>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is long or ulong || (IntPtr.Size == 8 && (lowInclusive is nint or nuint)))
                {
                    return SpanHelpers.IndexOfAnyInRangeUnsignedNumber(
                        ref Unsafe.As<T, ulong>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, ulong>(ref lowInclusive),
                        Unsafe.As<T, ulong>(ref highInclusive),
                        span.Length);
                }
            }

            return SpanHelpers.IndexOfAnyInRange(ref MemoryMarshal.GetReference(span), lowInclusive, highInclusive, span.Length);
        }

        /// <inheritdoc cref="IndexOfAnyExceptInRange{T}(ReadOnlySpan{T}, T, T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAnyExceptInRange<T>(this Span<T> span, T lowInclusive, T highInclusive)
            where T : IComparable<T> =>
            IndexOfAnyExceptInRange((ReadOnlySpan<T>)span, lowInclusive, highInclusive);

        /// <summary>Searches for the first index of any value outside of the range between <paramref name="lowInclusive"/> and <paramref name="highInclusive"/>, inclusive.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="lowInclusive">A lower bound, inclusive, of the excluded range.</param>
        /// <param name="highInclusive">A upper bound, inclusive, of the excluded range.</param>
        /// <returns>
        /// The index in the span of the first occurrence of any value outside of the specified range.
        /// If all of the values are inside of the specified range, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAnyExceptInRange<T>(this ReadOnlySpan<T> span, T lowInclusive, T highInclusive)
            where T : IComparable<T>
        {
            if (lowInclusive is null || highInclusive is null)
            {
                ThrowNullLowHighInclusive(lowInclusive, highInclusive);
            }

            if (Vector128.IsHardwareAccelerated)
            {
                if (lowInclusive is byte or sbyte)
                {
                    return SpanHelpers.IndexOfAnyExceptInRangeUnsignedNumber(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref lowInclusive),
                        Unsafe.As<T, byte>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is short or ushort or char)
                {
                    return SpanHelpers.IndexOfAnyExceptInRangeUnsignedNumber(
                        ref Unsafe.As<T, ushort>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, ushort>(ref lowInclusive),
                        Unsafe.As<T, ushort>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is int or uint || (IntPtr.Size == 4 && (lowInclusive is nint or nuint)))
                {
                    return SpanHelpers.IndexOfAnyExceptInRangeUnsignedNumber(
                        ref Unsafe.As<T, uint>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, uint>(ref lowInclusive),
                        Unsafe.As<T, uint>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is long or ulong || (IntPtr.Size == 8 && (lowInclusive is nint or nuint)))
                {
                    return SpanHelpers.IndexOfAnyExceptInRangeUnsignedNumber(
                        ref Unsafe.As<T, ulong>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, ulong>(ref lowInclusive),
                        Unsafe.As<T, ulong>(ref highInclusive),
                        span.Length);
                }
            }

            return SpanHelpers.IndexOfAnyExceptInRange(ref MemoryMarshal.GetReference(span), lowInclusive, highInclusive, span.Length);
        }

        /// <inheritdoc cref="LastIndexOfAnyInRange{T}(ReadOnlySpan{T}, T, T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAnyInRange<T>(this Span<T> span, T lowInclusive, T highInclusive)
            where T : IComparable<T> =>
            LastIndexOfAnyInRange((ReadOnlySpan<T>)span, lowInclusive, highInclusive);

        /// <summary>Searches for the last index of any value in the range between <paramref name="lowInclusive"/> and <paramref name="highInclusive"/>, inclusive.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="lowInclusive">A lower bound, inclusive, of the range for which to search.</param>
        /// <param name="highInclusive">A upper bound, inclusive, of the range for which to search.</param>
        /// <returns>
        /// The index in the span of the last occurrence of any value in the specified range.
        /// If all of the values are outside of the specified range, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAnyInRange<T>(this ReadOnlySpan<T> span, T lowInclusive, T highInclusive)
            where T : IComparable<T>
        {
            if (lowInclusive is null || highInclusive is null)
            {
                ThrowNullLowHighInclusive(lowInclusive, highInclusive);
            }

            if (Vector128.IsHardwareAccelerated)
            {
                if (lowInclusive is byte or sbyte)
                {
                    return SpanHelpers.LastIndexOfAnyInRangeUnsignedNumber(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref lowInclusive),
                        Unsafe.As<T, byte>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is short or ushort or char)
                {
                    return SpanHelpers.LastIndexOfAnyInRangeUnsignedNumber(
                        ref Unsafe.As<T, ushort>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, ushort>(ref lowInclusive),
                        Unsafe.As<T, ushort>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is int or uint || (IntPtr.Size == 4 && (lowInclusive is nint or nuint)))
                {
                    return SpanHelpers.LastIndexOfAnyInRangeUnsignedNumber(
                        ref Unsafe.As<T, uint>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, uint>(ref lowInclusive),
                        Unsafe.As<T, uint>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is long or ulong || (IntPtr.Size == 8 && (lowInclusive is nint or nuint)))
                {
                    return SpanHelpers.LastIndexOfAnyInRangeUnsignedNumber(
                        ref Unsafe.As<T, ulong>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, ulong>(ref lowInclusive),
                        Unsafe.As<T, ulong>(ref highInclusive),
                        span.Length);
                }
            }

            return SpanHelpers.LastIndexOfAnyInRange(ref MemoryMarshal.GetReference(span), lowInclusive, highInclusive, span.Length);
        }

        /// <inheritdoc cref="LastIndexOfAnyExceptInRange{T}(ReadOnlySpan{T}, T, T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAnyExceptInRange<T>(this Span<T> span, T lowInclusive, T highInclusive)
            where T : IComparable<T> =>
            LastIndexOfAnyExceptInRange((ReadOnlySpan<T>)span, lowInclusive, highInclusive);

        /// <summary>Searches for the last index of any value outside of the range between <paramref name="lowInclusive"/> and <paramref name="highInclusive"/>, inclusive.</summary>
        /// <typeparam name="T">The type of the span and values.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="lowInclusive">A lower bound, inclusive, of the excluded range.</param>
        /// <param name="highInclusive">A upper bound, inclusive, of the excluded range.</param>
        /// <returns>
        /// The index in the span of the last occurrence of any value outside of the specified range.
        /// If all of the values are inside of the specified range, returns -1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAnyExceptInRange<T>(this ReadOnlySpan<T> span, T lowInclusive, T highInclusive)
            where T : IComparable<T>
        {
            if (lowInclusive is null || highInclusive is null)
            {
                ThrowNullLowHighInclusive(lowInclusive, highInclusive);
            }

            if (Vector128.IsHardwareAccelerated)
            {
                if (lowInclusive is byte or sbyte)
                {
                    return SpanHelpers.LastIndexOfAnyExceptInRangeUnsignedNumber(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref lowInclusive),
                        Unsafe.As<T, byte>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is short or ushort or char)
                {
                    return SpanHelpers.LastIndexOfAnyExceptInRangeUnsignedNumber(
                        ref Unsafe.As<T, ushort>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, ushort>(ref lowInclusive),
                        Unsafe.As<T, ushort>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is int or uint || (IntPtr.Size == 4 && (lowInclusive is nint or nuint)))
                {
                    return SpanHelpers.LastIndexOfAnyExceptInRangeUnsignedNumber(
                        ref Unsafe.As<T, uint>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, uint>(ref lowInclusive),
                        Unsafe.As<T, uint>(ref highInclusive),
                        span.Length);
                }

                if (lowInclusive is long or ulong || (IntPtr.Size == 8 && (lowInclusive is nint or nuint)))
                {
                    return SpanHelpers.LastIndexOfAnyExceptInRangeUnsignedNumber(
                        ref Unsafe.As<T, ulong>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, ulong>(ref lowInclusive),
                        Unsafe.As<T, ulong>(ref highInclusive),
                        span.Length);
                }
            }

            return SpanHelpers.LastIndexOfAnyExceptInRange(ref MemoryMarshal.GetReference(span), lowInclusive, highInclusive, span.Length);
        }

        /// <summary>Throws an <see cref="ArgumentNullException"/> for <paramref name="lowInclusive"/> or <paramref name="highInclusive"/> being null.</summary>
        [DoesNotReturn]
        private static void ThrowNullLowHighInclusive<T>(T? lowInclusive, T? highInclusive)
        {
            Debug.Assert(lowInclusive is null || highInclusive is null);
            throw new ArgumentNullException(lowInclusive is null ? nameof(lowInclusive) : nameof(highInclusive));
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        [Intrinsic] // Unrolled and vectorized for half-constant input
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool SequenceEqual<T>(this Span<T> span, ReadOnlySpan<T> other) where T : IEquatable<T>?
        {
            int length = span.Length;
            int otherLength = other.Length;

            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                return length == otherLength &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)),
                    ((uint)otherLength) * (nuint)sizeof(T));  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this api in such a case so we choose not to take the overhead of checking.
            }

            return length == otherLength && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(other), length);
        }

        /// <summary>
        /// Determines the relative order of the sequences being compared by comparing the elements using IComparable{T}.CompareTo(T).
        /// </summary>
        public static int SequenceCompareTo<T>(this Span<T> span, ReadOnlySpan<T> other)
            where T : IComparable<T>?
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
        public static unsafe int IndexOf<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                    return SpanHelpers.IndexOfValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);

                if (sizeof(T) == sizeof(short))
                    return SpanHelpers.IndexOfValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value),
                        span.Length);

                if (sizeof(T) == sizeof(int))
                    return SpanHelpers.IndexOfValueType(
                        ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, int>(ref value),
                        span.Length);

                if (sizeof(T) == sizeof(long))
                    return SpanHelpers.IndexOfValueType(
                        ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, long>(ref value),
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
        public static unsafe int IndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                    return SpanHelpers.IndexOf(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                        value.Length);

                if (sizeof(T) == sizeof(char))
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
        public static unsafe int LastIndexOf<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(int))
                {
                    return SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, int>(ref value),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(long))
                {
                    return SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, long>(ref value),
                        span.Length);
                }
            }

            return SpanHelpers.LastIndexOf<T>(ref MemoryMarshal.GetReference(span), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified sequence and returns the index of its last occurrence. If not found, returns -1. Values are compared using IEquatable{T}.Equals(T).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int LastIndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOf(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                        value.Length);
                }
                if (sizeof(T) == sizeof(char))
                {
                    return SpanHelpers.LastIndexOf(
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(value)),
                        value.Length);
                }
            }

            return SpanHelpers.LastIndexOf<T>(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int IndexOfAny<T>(this Span<T> span, T value0, T value1) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.IndexOfAnyValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.IndexOfAnyValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        span.Length);
                }
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
        public static unsafe int IndexOfAny<T>(this Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.IndexOfAnyValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        Unsafe.As<T, byte>(ref value2),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.IndexOfAnyValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        Unsafe.As<T, short>(ref value2),
                        span.Length);
                }
            }

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this Span<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>? =>
            IndexOfAny((ReadOnlySpan<T>)span, values);

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this Span<T> span, IndexOfAnyValues<T> values) where T : IEquatable<T>? =>
            IndexOfAny((ReadOnlySpan<T>)span, values);

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int IndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.IndexOfAnyValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.IndexOfAnyValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        span.Length);
                }
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
        public static unsafe int IndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.IndexOfAnyValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        Unsafe.As<T, byte>(ref value2),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.IndexOfAnyValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        Unsafe.As<T, short>(ref value2),
                        span.Length);
                }
            }

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int IndexOfAny<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    ref byte spanRef = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span));
                    ref byte valueRef = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values));
                    switch (values.Length)
                    {
                        case 0:
                            return -1;

                        case 1:
                            return SpanHelpers.IndexOfValueType(ref spanRef, valueRef, span.Length);

                        case 2:
                            return SpanHelpers.IndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                span.Length);

                        case 3:
                            return SpanHelpers.IndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                span.Length);

                        case 4:
                            return SpanHelpers.IndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                Unsafe.Add(ref valueRef, 3),
                                span.Length);

                        case 5:
                            return SpanHelpers.IndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                Unsafe.Add(ref valueRef, 3),
                                Unsafe.Add(ref valueRef, 4),
                                span.Length);
                    }
                }

                if (sizeof(T) == sizeof(short))
                {
                    ref short spanRef = ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span));
                    ref short valueRef = ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(values));
                    switch (values.Length)
                    {
                        case 0:
                            return -1;

                        case 1:
                            return SpanHelpers.IndexOfValueType(ref spanRef, valueRef, span.Length);

                        case 2:
                            return SpanHelpers.IndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                span.Length);

                        case 3:
                            return SpanHelpers.IndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                span.Length);

                        case 4:
                            return SpanHelpers.IndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                Unsafe.Add(ref valueRef, 3),
                                span.Length);

                        case 5:
                            return SpanHelpers.IndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                Unsafe.Add(ref valueRef, 3),
                                Unsafe.Add(ref valueRef, 4),
                                span.Length);

                        default:
                            return ProbabilisticMap.IndexOfAny(ref Unsafe.As<short, char>(ref spanRef), span.Length, ref Unsafe.As<short, char>(ref valueRef), values.Length);
                    }
                }
            }

            return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(values), values.Length);
        }

        /// <summary>
        /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAny<T>(this ReadOnlySpan<T> span, IndexOfAnyValues<T> values) where T : IEquatable<T>? =>
            IndexOfAnyValues<T>.IndexOfAny(span, values);

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int LastIndexOfAny<T>(this Span<T> span, T value0, T value1) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOfAnyValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.LastIndexOfAnyValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        span.Length);
                }
            }

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
        public static unsafe int LastIndexOfAny<T>(this Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOfAnyValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        Unsafe.As<T, byte>(ref value2),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.LastIndexOfAnyValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        Unsafe.As<T, short>(ref value2),
                        span.Length);
                }
            }

            return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAny<T>(this Span<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>? =>
            LastIndexOfAny((ReadOnlySpan<T>)span, values);

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAny<T>(this Span<T> span, IndexOfAnyValues<T> values) where T : IEquatable<T>? =>
            LastIndexOfAny((ReadOnlySpan<T>)span, values);

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value0">One of the values to search for.</param>
        /// <param name="value1">One of the values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int LastIndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOfAnyValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.LastIndexOfAnyValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        span.Length);
                }
            }

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
        public static unsafe int LastIndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    return SpanHelpers.LastIndexOfAnyValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value0),
                        Unsafe.As<T, byte>(ref value1),
                        Unsafe.As<T, byte>(ref value2),
                        span.Length);
                }
                else if (sizeof(T) == sizeof(short))
                {
                    return SpanHelpers.LastIndexOfAnyValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value0),
                        Unsafe.As<T, short>(ref value1),
                        Unsafe.As<T, short>(ref value2),
                        span.Length);
                }
            }

            return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
        }

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int LastIndexOfAny<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    ref byte spanRef = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span));
                    ref byte valueRef = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values));
                    switch (values.Length)
                    {
                        case 0:
                            return -1;

                        case 1:
                            return SpanHelpers.LastIndexOfValueType(ref spanRef, valueRef, span.Length);

                        case 2:
                            return SpanHelpers.LastIndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                span.Length);

                        case 3:
                            return SpanHelpers.LastIndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                span.Length);

                        case 4:
                            return SpanHelpers.LastIndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                Unsafe.Add(ref valueRef, 3),
                                span.Length);

                        case 5:
                            return SpanHelpers.LastIndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                Unsafe.Add(ref valueRef, 3),
                                Unsafe.Add(ref valueRef, 4),
                                span.Length);
                    }
                }

                if (sizeof(T) == sizeof(short))
                {
                    ref short spanRef = ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span));
                    ref short valueRef = ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(values));
                    switch (values.Length)
                    {
                        case 0:
                            return -1;

                        case 1:
                            return SpanHelpers.LastIndexOfValueType(ref spanRef, valueRef, span.Length);

                        case 2:
                            return SpanHelpers.LastIndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                span.Length);

                        case 3:
                            return SpanHelpers.LastIndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                span.Length);

                        case 4:
                            return SpanHelpers.LastIndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                Unsafe.Add(ref valueRef, 3),
                                span.Length);

                        case 5:
                            return SpanHelpers.LastIndexOfAnyValueType(
                                ref spanRef,
                                valueRef,
                                Unsafe.Add(ref valueRef, 1),
                                Unsafe.Add(ref valueRef, 2),
                                Unsafe.Add(ref valueRef, 3),
                                Unsafe.Add(ref valueRef, 4),
                                span.Length);

                        default:
                            return ProbabilisticMap.LastIndexOfAny(ref Unsafe.As<short, char>(ref spanRef), span.Length, ref Unsafe.As<short, char>(ref valueRef), values.Length);
                    }
                }
            }

            return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(values), values.Length);
        }

        /// <summary>
        /// Searches for the last index of any of the specified values similar to calling LastIndexOf several times with the logical OR operator. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The set of values to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAny<T>(this ReadOnlySpan<T> span, IndexOfAnyValues<T> values) where T : IEquatable<T>? =>
            IndexOfAnyValues<T>.LastIndexOfAny(span, values);

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        [Intrinsic] // Unrolled and vectorized for half-constant input
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other) where T : IEquatable<T>?
        {
            int length = span.Length;
            int otherLength = other.Length;

            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                return length == otherLength &&
                    SpanHelpers.SequenceEqual(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)),
                        ((uint)otherLength) * (nuint)sizeof(T));  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this API in such a case so we choose not to take the overhead of checking.
            }

            return length == otherLength && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(other), length);
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
        public static unsafe bool SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
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
                        return SpanHelpers.SequenceEqual(
                            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)),
                            ((uint)span.Length) * (nuint)sizeof(T));  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this API in such a case so we choose not to take the overhead of checking.
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
        public static unsafe int SequenceCompareTo<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other)
            where T : IComparable<T>?
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
        [Intrinsic] // Unrolled and vectorized for half-constant input
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool StartsWith<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>?
        {
            int valueLength = value.Length;
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                return valueLength <= span.Length &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    ((uint)valueLength) * (nuint)sizeof(T));  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this api in such a case so we choose not to take the overhead of checking.
            }

            return valueLength <= span.Length && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(value), valueLength);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the start of the span.
        /// </summary>
        [Intrinsic] // Unrolled and vectorized for half-constant input
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool StartsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>?
        {
            int valueLength = value.Length;
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                return valueLength <= span.Length &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    ((uint)valueLength) * (nuint)sizeof(T));  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this api in such a case so we choose not to take the overhead of checking.
            }

            return valueLength <= span.Length && SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(value), valueLength);
        }

        /// <summary>
        /// Determines whether the specified sequence appears at the end of the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool EndsWith<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>?
        {
            int spanLength = span.Length;
            int valueLength = value.Length;
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                return valueLength <= spanLength &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)(spanLength - valueLength) /* force zero-extension */)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    ((uint)valueLength) * (nuint)sizeof(T));  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this api in such a case so we choose not to take the overhead of checking.
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
        public static unsafe bool EndsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>?
        {
            int spanLength = span.Length;
            int valueLength = value.Length;
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                return valueLength <= spanLength &&
                SpanHelpers.SequenceEqual(
                    ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)(spanLength - valueLength) /* force zero-extension */)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)),
                    ((uint)valueLength) * (nuint)sizeof(T));  // If this multiplication overflows, the Span we got overflows the entire address range. There's no happy outcome for this api in such a case so we choose not to take the overhead of checking.
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
            if (span.Length > 1)
            {
                SpanHelpers.Reverse(ref MemoryMarshal.GetReference(span), (nuint)span.Length);
            }
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
        /// </summary>
        ///<param name="source">The array to copy items from.</param>
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the destination Span is shorter than the source array.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this T[]? source, Span<T> destination)
        {
            new ReadOnlySpan<T>(source).CopyTo(destination);
        }

        /// <summary>
        /// Copies the contents of the array into the memory. If the source
        /// and destinations overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        ///<param name="source">The array to copy items from.</param>
        /// <param name="destination">The memory to copy items into.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the destination is shorter than the source array.
        /// </exception>
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
        //      uint xLength = x.Length * sizeof(T)
        //      ref T yRef = MemoryMarshal.GetReference(y)
        //      uint yLength = y.Length * sizeof(T)
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
        public static unsafe bool Overlaps<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other)
        {
            if (span.IsEmpty || other.IsEmpty)
            {
                return false;
            }

            nint byteOffset = Unsafe.ByteOffset(
                ref MemoryMarshal.GetReference(span),
                ref MemoryMarshal.GetReference(other));

            return (nuint)byteOffset < (nuint)((nint)span.Length * sizeof(T)) ||
                    (nuint)byteOffset > (nuint)(-((nint)other.Length * sizeof(T)));
        }

        /// <summary>
        /// Determines whether two sequences overlap in memory and outputs the element offset.
        /// </summary>
        public static unsafe bool Overlaps<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, out int elementOffset)
        {
            if (span.IsEmpty || other.IsEmpty)
            {
                elementOffset = 0;
                return false;
            }

            nint byteOffset = Unsafe.ByteOffset(
                ref MemoryMarshal.GetReference(span),
                ref MemoryMarshal.GetReference(other));

            if ((nuint)byteOffset < (nuint)((nint)span.Length * sizeof(T)) ||
                (nuint)byteOffset > (nuint)(-((nint)other.Length * sizeof(T))))
            {
                if (byteOffset % sizeof(T) != 0)
                    ThrowHelper.ThrowArgumentException_OverlapAlignmentMismatch();

                elementOffset = (int)(byteOffset / sizeof(T));
                return true;
            }
            else
            {
                elementOffset = 0;
                return false;
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

        /// <summary>
        /// Replaces all occurrences of <paramref name="oldValue"/> with <paramref name="newValue"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the span.</typeparam>
        /// <param name="span">The span in which the elements should be replaced.</param>
        /// <param name="oldValue">The value to be replaced with <paramref name="newValue"/>.</param>
        /// <param name="newValue">The value to replace all occurrences of <paramref name="oldValue"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Replace<T>(this Span<T> span, T oldValue, T newValue) where T : IEquatable<T>?
        {
            nuint length = (uint)span.Length;

            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    ref byte src = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span));
                    SpanHelpers.ReplaceValueType(
                        ref src,
                        ref src,
                        Unsafe.As<T, byte>(ref oldValue),
                        Unsafe.As<T, byte>(ref newValue),
                        length);
                    return;
                }
                else if (sizeof(T) == sizeof(ushort))
                {
                    // Use ushort rather than short, as this avoids a sign-extending move.
                    ref ushort src = ref Unsafe.As<T, ushort>(ref MemoryMarshal.GetReference(span));
                    SpanHelpers.ReplaceValueType(
                        ref src,
                        ref src,
                        Unsafe.As<T, ushort>(ref oldValue),
                        Unsafe.As<T, ushort>(ref newValue),
                        length);
                    return;
                }
                else if (sizeof(T) == sizeof(int))
                {
                    ref int src = ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span));
                    SpanHelpers.ReplaceValueType(
                        ref src,
                        ref src,
                        Unsafe.As<T, int>(ref oldValue),
                        Unsafe.As<T, int>(ref newValue),
                        length);
                    return;
                }
                else if (sizeof(T) == sizeof(long))
                {
                    ref long src = ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span));
                    SpanHelpers.ReplaceValueType(
                        ref src,
                        ref src,
                        Unsafe.As<T, long>(ref oldValue),
                        Unsafe.As<T, long>(ref newValue),
                        length);
                    return;
                }
            }

            ref T src2 = ref MemoryMarshal.GetReference(span);
            SpanHelpers.Replace(ref src2, ref src2, oldValue, newValue, length);
        }

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, replacing all occurrences of <paramref name="oldValue"/> with <paramref name="newValue"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the spans.</typeparam>
        /// <param name="source">The span to copy.</param>
        /// <param name="destination">The span into which the copied and replaced values should be written.</param>
        /// <param name="oldValue">The value to be replaced with <paramref name="newValue"/>.</param>
        /// <param name="newValue">The value to replace all occurrences of <paramref name="oldValue"/>.</param>
        /// <exception cref="ArgumentException">The <paramref name="destination"/> span was shorter than the <paramref name="source"/> span.</exception>
        /// <exception cref="ArgumentException">The <paramref name="source"/> and <paramref name="destination"/> were overlapping but not referring to the same starting location.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Replace<T>(this ReadOnlySpan<T> source, Span<T> destination, T oldValue, T newValue) where T : IEquatable<T>?
        {
            nuint length = (uint)source.Length;
            if (length == 0)
            {
                return;
            }

            if (length > (uint)destination.Length)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            ref T src = ref MemoryMarshal.GetReference(source);
            ref T dst = ref MemoryMarshal.GetReference(destination);

            nint byteOffset = Unsafe.ByteOffset(ref src, ref dst);
            if (byteOffset != 0 &&
                ((nuint)byteOffset < (nuint)((nint)source.Length * sizeof(T)) ||
                 (nuint)byteOffset > (nuint)(-((nint)destination.Length * sizeof(T)))))
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.InvalidOperation_SpanOverlappedOperation);
            }

            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    SpanHelpers.ReplaceValueType(
                        ref Unsafe.As<T, byte>(ref src),
                        ref Unsafe.As<T, byte>(ref dst),
                        Unsafe.As<T, byte>(ref oldValue),
                        Unsafe.As<T, byte>(ref newValue),
                        length);
                    return;
                }
                else if (sizeof(T) == sizeof(ushort))
                {
                    // Use ushort rather than short, as this avoids a sign-extending move.
                    SpanHelpers.ReplaceValueType(
                        ref Unsafe.As<T, ushort>(ref src),
                        ref Unsafe.As<T, ushort>(ref dst),
                        Unsafe.As<T, ushort>(ref oldValue),
                        Unsafe.As<T, ushort>(ref newValue),
                        length);
                    return;
                }
                else if (sizeof(T) == sizeof(int))
                {
                    SpanHelpers.ReplaceValueType(
                        ref Unsafe.As<T, int>(ref src),
                        ref Unsafe.As<T, int>(ref dst),
                        Unsafe.As<T, int>(ref oldValue),
                        Unsafe.As<T, int>(ref newValue),
                        length);
                    return;
                }
                else if (sizeof(T) == sizeof(long))
                {
                    SpanHelpers.ReplaceValueType(
                        ref Unsafe.As<T, long>(ref src),
                        ref Unsafe.As<T, long>(ref dst),
                        Unsafe.As<T, long>(ref oldValue),
                        Unsafe.As<T, long>(ref newValue),
                        length);
                    return;
                }
            }

            SpanHelpers.Replace(ref src, ref dst, oldValue, newValue, length);
        }

        /// <summary>Finds the length of any common prefix shared between <paramref name="span"/> and <paramref name="other"/>.</summary>
        /// <typeparam name="T">The type of the elements in the spans.</typeparam>
        /// <param name="span">The first sequence to compare.</param>
        /// <param name="other">The second sequence to compare.</param>
        /// <returns>The length of the common prefix shared by the two spans.  If there's no shared prefix, 0 is returned.</returns>
        public static int CommonPrefixLength<T>(this Span<T> span, ReadOnlySpan<T> other) =>
            CommonPrefixLength((ReadOnlySpan<T>)span, other);

        /// <summary>Finds the length of any common prefix shared between <paramref name="span"/> and <paramref name="other"/>.</summary>
        /// <typeparam name="T">The type of the elements in the spans.</typeparam>
        /// <param name="span">The first sequence to compare.</param>
        /// <param name="other">The second sequence to compare.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing elements, or null to use the default <see cref="IEqualityComparer{T}"/> for the type of an element.</param>
        /// <returns>The length of the common prefix shared by the two spans.  If there's no shared prefix, 0 is returned.</returns>
        public static int CommonPrefixLength<T>(this Span<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer) =>
            CommonPrefixLength((ReadOnlySpan<T>)span, other, comparer);

        /// <summary>Finds the length of any common prefix shared between <paramref name="span"/> and <paramref name="other"/>.</summary>
        /// <typeparam name="T">The type of the elements in the spans.</typeparam>
        /// <param name="span">The first sequence to compare.</param>
        /// <param name="other">The second sequence to compare.</param>
        /// <returns>The length of the common prefix shared by the two spans.  If there's no shared prefix, 0 is returned.</returns>
        public static unsafe int CommonPrefixLength<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other)
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                nuint length = Math.Min((nuint)(uint)span.Length, (nuint)(uint)other.Length);
                nuint size = (uint)sizeof(T);
                nuint index = SpanHelpers.CommonPrefixLength(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)),
                    length * size);

                // A byte-wise comparison in CommonPrefixLength can be used for multi-byte types,
                // that are bitwise-equatable, too. In order to get the correct index in terms of type T
                // of the first mismatch, integer division by the size of T is used.
                //
                // Example for short:
                // index (byte-based):   b-1,  b,    b+1,    b+2,  b+3
                // index (short-based):  s-1,  s,            s+1
                // byte sequence 1:    { ..., [0x42, 0x43], [0x37, 0x38], ... }
                // byte sequence 2:    { ..., [0x42, 0x43], [0x37, 0xAB], ... }
                // So the mismatch is a byte-index b+3, which gives integer divided by the size of short:
                // 3 / 2 = 1, thus the expected index short-based.
                return (int)(index / size);
            }

            // Shrink one of the spans if necessary to ensure they're both the same length. We can then iterate until
            // the Length of one of them and at least have bounds checks removed from that one.
            SliceLongerSpanToMatchShorterLength(ref span, ref other);

            // Find the first element pairwise that is not equal, and return its index as the length
            // of the sequence before it that matches.
            for (int i = 0; i < span.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(span[i], other[i]))
                {
                    return i;
                }
            }

            return span.Length;
        }

        /// <summary>Determines the length of any common prefix shared between <paramref name="span"/> and <paramref name="other"/>.</summary>
        /// <typeparam name="T">The type of the elements in the sequences.</typeparam>
        /// <param name="span">The first sequence to compare.</param>
        /// <param name="other">The second sequence to compare.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing elements, or null to use the default <see cref="IEqualityComparer{T}"/> for the type of an element.</param>
        /// <returns>The length of the common prefix shared by the two spans.  If there's no shared prefix, 0 is returned.</returns>
        public static int CommonPrefixLength<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer)
        {
            // If the comparer is null or the default, and T is a value type, we want to use EqualityComparer<T>.Default.Equals
            // directly to enable devirtualization.  The non-comparer overload already does so, so just use it.
            if (typeof(T).IsValueType && (comparer is null || comparer == EqualityComparer<T>.Default))
            {
                return CommonPrefixLength(span, other);
            }

            // Shrink one of the spans if necessary to ensure they're both the same length. We can then iterate until
            // the Length of one of them and at least have bounds checks removed from that one.
            SliceLongerSpanToMatchShorterLength(ref span, ref other);

            // Ensure we have a comparer, then compare the spans.
            comparer ??= EqualityComparer<T>.Default;
            for (int i = 0; i < span.Length; i++)
            {
                if (!comparer.Equals(span[i], other[i]))
                {
                    return i;
                }
            }

            return span.Length;
        }

        /// <summary>Determines if one span is longer than the other, and slices the longer one to match the length of the shorter.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SliceLongerSpanToMatchShorterLength<T>(ref ReadOnlySpan<T> span, ref ReadOnlySpan<T> other)
        {
            if (other.Length > span.Length)
            {
                other = other.Slice(0, span.Length);
            }
            else if (span.Length > other.Length)
            {
                span = span.Slice(0, other.Length);
            }
            Debug.Assert(span.Length == other.Length);
        }

        /// <summary>
        /// Parses the source <see cref="ReadOnlySpan{Char}"/> for the specified <paramref name="separator"/>, populating the <paramref name="destination"/> span
        /// with <see cref="Range"/> instances representing the regions between the separators.
        /// </summary>
        /// <param name="source">The source span to parse.</param>
        /// <param name="destination">The destination span into which the resulting ranges are written.</param>
        /// <param name="separator">A character that delimits the regions in this instance.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
        /// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
        /// <remarks>
        /// <para>
        /// Delimiter characters are not included in the elements of the returned array.
        /// </para>
        /// <para>
        /// If the <paramref name="destination"/> span is empty, or if the <paramref name="options"/> specifies <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <paramref name="source"/> is empty,
        /// or if <paramref name="options"/> specifies both <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <see cref="StringSplitOptions.TrimEntries"/> and the <paramref name="source"/> is
        /// entirely whitespace, no ranges are written to the destination.
        /// </para>
        /// <para>
        /// If the span does not contain <paramref name="separator"/>, or if <paramref name="destination"/>'s length is 1, a single range will be output containing the entire <paramref name="source"/>,
        /// subject to the processing implied by <paramref name="options"/>.
        /// </para>
        /// <para>
        /// If there are more regions in <paramref name="source"/> than will fit in <paramref name="destination"/>, the first <paramref name="destination"/> length minus 1 ranges are
        /// stored in <paramref name="destination"/>, and a range for the remainder of <paramref name="source"/> is stored in <paramref name="destination"/>.
        /// </para>
        /// </remarks>
        public static int Split(this ReadOnlySpan<char> source, Span<Range> destination, char separator, StringSplitOptions options = StringSplitOptions.None)
        {
            string.CheckStringSplitOptions(options);

            return SplitCore(source, destination, new ReadOnlySpan<char>(in separator), default, isAny: true, options);
        }

        /// <summary>
        /// Parses the source <see cref="ReadOnlySpan{Char}"/> for the specified <paramref name="separator"/>, populating the <paramref name="destination"/> span
        /// with <see cref="Range"/> instances representing the regions between the separators.
        /// </summary>
        /// <param name="source">The source span to parse.</param>
        /// <param name="destination">The destination span into which the resulting ranges are written.</param>
        /// <param name="separator">A character that delimits the regions in this instance.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
        /// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
        /// <remarks>
        /// <para>
        /// Delimiter characters are not included in the elements of the returned array.
        /// </para>
        /// <para>
        /// If the <paramref name="destination"/> span is empty, or if the <paramref name="options"/> specifies <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <paramref name="source"/> is empty,
        /// or if <paramref name="options"/> specifies both <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <see cref="StringSplitOptions.TrimEntries"/> and the <paramref name="source"/> is
        /// entirely whitespace, no ranges are written to the destination.
        /// </para>
        /// <para>
        /// If the span does not contain <paramref name="separator"/>, or if <paramref name="destination"/>'s length is 1, a single range will be output containing the entire <paramref name="source"/>,
        /// subject to the processing implied by <paramref name="options"/>.
        /// </para>
        /// <para>
        /// If there are more regions in <paramref name="source"/> than will fit in <paramref name="destination"/>, the first <paramref name="destination"/> length minus 1 ranges are
        /// stored in <paramref name="destination"/>, and a range for the remainder of <paramref name="source"/> is stored in <paramref name="destination"/>.
        /// </para>
        /// </remarks>
        public static int Split(this ReadOnlySpan<char> source, Span<Range> destination, ReadOnlySpan<char> separator, StringSplitOptions options = StringSplitOptions.None)
        {
            string.CheckStringSplitOptions(options);

            // If the separator is an empty string, the whole input is considered the sole range.
            if (separator.IsEmpty)
            {
                if (!destination.IsEmpty)
                {
                    int startInclusive = 0, endExclusive = source.Length;

                    if ((options & StringSplitOptions.TrimEntries) != 0)
                    {
                        (startInclusive, endExclusive) = TrimSplitEntry(source, startInclusive, endExclusive);
                    }

                    if (startInclusive != endExclusive || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
                    {
                        destination[0] = startInclusive..endExclusive;
                        return 1;
                    }
                }

                return 0;
            }

            return SplitCore(source, destination, separator, default, isAny: false, options);
        }

        /// <summary>
        /// Parses the source <see cref="ReadOnlySpan{Char}"/> for one of the specified <paramref name="separators"/>, populating the <paramref name="destination"/> span
        /// with <see cref="Range"/> instances representing the regions between the separators.
        /// </summary>
        /// <param name="source">The source span to parse.</param>
        /// <param name="destination">The destination span into which the resulting ranges are written.</param>
        /// <param name="separators">Any number of characters that may delimit the regions in this instance. If empty, all Unicode whitespace characters are used as the separators.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
        /// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
        /// <remarks>
        /// <para>
        /// Delimiter characters are not included in the elements of the returned array.
        /// </para>
        /// <para>
        /// If the <paramref name="destination"/> span is empty, or if the <paramref name="options"/> specifies <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <paramref name="source"/> is empty,
        /// or if <paramref name="options"/> specifies both <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <see cref="StringSplitOptions.TrimEntries"/> and the <paramref name="source"/> is
        /// entirely whitespace, no ranges are written to the destination.
        /// </para>
        /// <para>
        /// If the span does not contain any of the <paramref name="separators"/>, or if <paramref name="destination"/>'s length is 1, a single range will be output containing the entire <paramref name="source"/>,
        /// subject to the processing implied by <paramref name="options"/>.
        /// </para>
        /// <para>
        /// If there are more regions in <paramref name="source"/> than will fit in <paramref name="destination"/>, the first <paramref name="destination"/> length minus 1 ranges are
        /// stored in <paramref name="destination"/>, and a range for the remainder of <paramref name="source"/> is stored in <paramref name="destination"/>.
        /// </para>
        /// </remarks>
        public static int SplitAny(this ReadOnlySpan<char> source, Span<Range> destination, ReadOnlySpan<char> separators, StringSplitOptions options = StringSplitOptions.None)
        {
            string.CheckStringSplitOptions(options);

            // If the separators list is empty, whitespace is used as separators.  In that case, we want to ignore TrimEntries if specified,
            // since TrimEntries also impacts whitespace.  The TrimEntries flag must be left intact if we are constrained by count because we need to process last substring.
            if (separators.IsEmpty && destination.Length > source.Length)
            {
                options &= ~StringSplitOptions.TrimEntries;
            }

            return SplitCore(source, destination, separators, default, isAny: true, options);
        }

        /// <summary>
        /// Parses the source <see cref="ReadOnlySpan{Char}"/> for one of the specified <paramref name="separators"/>, populating the <paramref name="destination"/> span
        /// with <see cref="Range"/> instances representing the regions between the separators.
        /// </summary>
        /// <param name="source">The source span to parse.</param>
        /// <param name="destination">The destination span into which the resulting ranges are written.</param>
        /// <param name="separators">Any number of strings that may delimit the regions in this instance.  If empty, all Unicode whitespace characters are used as the separators.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
        /// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
        /// <remarks>
        /// <para>
        /// Delimiter characters are not included in the elements of the returned array.
        /// </para>
        /// <para>
        /// If the <paramref name="destination"/> span is empty, or if the <paramref name="options"/> specifies <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <paramref name="source"/> is empty,
        /// or if <paramref name="options"/> specifies both <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <see cref="StringSplitOptions.TrimEntries"/> and the <paramref name="source"/> is
        /// entirely whitespace, no ranges are written to the destination.
        /// </para>
        /// <para>
        /// If the span does not contain any of the <paramref name="separators"/>, or if <paramref name="destination"/>'s length is 1, a single range will be output containing the entire <paramref name="source"/>,
        /// subject to the processing implied by <paramref name="options"/>.
        /// </para>
        /// <para>
        /// If there are more regions in <paramref name="source"/> than will fit in <paramref name="destination"/>, the first <paramref name="destination"/> length minus 1 ranges are
        /// stored in <paramref name="destination"/>, and a range for the remainder of <paramref name="source"/> is stored in <paramref name="destination"/>.
        /// </para>
        /// </remarks>
        public static int SplitAny(this ReadOnlySpan<char> source, Span<Range> destination, ReadOnlySpan<string> separators, StringSplitOptions options = StringSplitOptions.None)
        {
            string.CheckStringSplitOptions(options);

            // If the separators list is empty, whitespace is used as separators.  In that case, we want to ignore TrimEntries if specified,
            // since TrimEntries also impacts whitespace.  The TrimEntries flag must be left intact if we are constrained by count because we need to process last substring.
            if (separators.IsEmpty && destination.Length > source.Length)
            {
                options &= ~StringSplitOptions.TrimEntries;
            }

            return SplitCore(source, destination, default, separators!, isAny: true, options);
        }

        /// <summary>Core implementation for all of the Split{Any}AsRanges methods.</summary>
        /// <param name="source">The source span to parse.</param>
        /// <param name="destination">The destination span into which the resulting ranges are written.</param>
        /// <param name="separatorOrSeparators">Either a single separator (one or more characters in length) or multiple individual 1-character separators.</param>
        /// <param name="stringSeparators">Strings to use as separators instead of <paramref name="separatorOrSeparators"/>.</param>
        /// <param name="isAny">true if the separators are a set; false if <paramref name="separatorOrSeparators"/> should be treated as a single separator.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
        /// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
        /// <remarks>This implementation matches the various quirks of string.Split.</remarks>
        private static int SplitCore(
            ReadOnlySpan<char> source, Span<Range> destination,
            ReadOnlySpan<char> separatorOrSeparators, ReadOnlySpan<string?> stringSeparators, bool isAny,
            StringSplitOptions options)
        {
            // If the destination is empty, there's nothing to do.
            if (destination.IsEmpty)
            {
                return 0;
            }

            bool keepEmptyEntries = (options & StringSplitOptions.RemoveEmptyEntries) == 0;
            bool trimEntries = (options & StringSplitOptions.TrimEntries) != 0;

            // If the input is empty, then we either return an empty range as the sole range, or if empty entries
            // are to be removed, we return nothing.
            if (source.Length == 0)
            {
                if (keepEmptyEntries)
                {
                    destination[0] = default;
                    return 1;
                }

                return 0;
            }

            int startInclusive = 0, endExclusive;

            // If the destination has only one slot, then we need to return the whole input, subject to the options.
            if (destination.Length == 1)
            {
                endExclusive = source.Length;
                if (trimEntries)
                {
                    (startInclusive, endExclusive) = TrimSplitEntry(source, startInclusive, endExclusive);
                }

                if (startInclusive != endExclusive || keepEmptyEntries)
                {
                    destination[0] = startInclusive..endExclusive;
                    return 1;
                }

                return 0;
            }

            scoped ValueListBuilder<int> separatorList = new ValueListBuilder<int>(stackalloc int[string.StackallocIntBufferSizeLimit]);
            scoped ValueListBuilder<int> lengthList = default;

            int separatorLength;
            int rangeCount = 0;
            if (!stringSeparators.IsEmpty)
            {
                lengthList = new ValueListBuilder<int>(stackalloc int[string.StackallocIntBufferSizeLimit]);
                string.MakeSeparatorListAny(source, stringSeparators, ref separatorList, ref lengthList);
                separatorLength = -1; // Will be set on each iteration of the loop
            }
            else if (isAny)
            {
                string.MakeSeparatorListAny(source, separatorOrSeparators, ref separatorList);
                separatorLength = 1;
            }
            else
            {
                string.MakeSeparatorList(source, separatorOrSeparators, ref separatorList);
                separatorLength = separatorOrSeparators.Length;
            }

            // Try to fill in all but the last slot in the destination.  The last slot is reserved for whatever remains
            // after the last discovered separator. If the options specify that empty entries are to be removed, then we
            // need to skip past all of those here as well, including any that occur at the beginning of the last entry,
            // which is why we enter the loop if remove empty entries is set, even if we've already added enough entries.
            int separatorIndex = 0;
            Span<Range> destinationMinusOne = destination.Slice(0, destination.Length - 1);
            while (separatorIndex < separatorList.Length && (rangeCount < destinationMinusOne.Length || !keepEmptyEntries))
            {
                endExclusive = separatorList[separatorIndex];
                if (separatorIndex < lengthList.Length)
                {
                    separatorLength = lengthList[separatorIndex];
                }
                separatorIndex++;

                // Trim off whitespace from the start and end of the range.
                int untrimmedEndEclusive = endExclusive;
                if (trimEntries)
                {
                    (startInclusive, endExclusive) = TrimSplitEntry(source, startInclusive, endExclusive);
                }

                // If the range is not empty or we're not ignoring empty ranges, store it.
                Debug.Assert(startInclusive <= endExclusive);
                if (startInclusive != endExclusive || keepEmptyEntries)
                {
                    // If we're not keeping empty entries, we may have entered the loop even if we'd
                    // already written enough ranges.  Now that we know this entry isn't empty, we
                    // need to validate there's still room remaining.
                    if ((uint)rangeCount >= (uint)destinationMinusOne.Length)
                    {
                        break;
                    }

                    destinationMinusOne[rangeCount] = startInclusive..endExclusive;
                    rangeCount++;
                }

                // Reset to be just past the separator, and loop around to go again.
                startInclusive = untrimmedEndEclusive + separatorLength;
            }

            separatorList.Dispose();
            lengthList.Dispose();

            // Either we found at least destination.Length - 1 ranges or we didn't find any more separators.
            // If we still have a last destination slot available and there's anything left in the source,
            // put a range for the remainder of the source into the destination.
            if ((uint)rangeCount < (uint)destination.Length)
            {
                endExclusive = source.Length;
                if (trimEntries)
                {
                    (startInclusive, endExclusive) = TrimSplitEntry(source, startInclusive, endExclusive);
                }

                if (startInclusive != endExclusive || keepEmptyEntries)
                {
                    destination[rangeCount] = startInclusive..endExclusive;
                    rangeCount++;
                }
            }

            // Return how many ranges were written.
            return rangeCount;
        }

        /// <summary>Updates the starting and ending markers for a range to exclude whitespace.</summary>
        private static (int StartInclusive, int EndExclusive) TrimSplitEntry(ReadOnlySpan<char> source, int startInclusive, int endExclusive)
        {
            while (startInclusive < endExclusive && char.IsWhiteSpace(source[startInclusive]))
            {
                startInclusive++;
            }

            while (endExclusive > startInclusive && char.IsWhiteSpace(source[endExclusive - 1]))
            {
                endExclusive--;
            }

            return (startInclusive, endExclusive);
        }

        /// <summary>Counts the number of times the specified <paramref name="value"/> occurs in the <paramref name="span"/>.</summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value for which to search.</param>
        /// <returns>The number of times <paramref name="value"/> was found in the <paramref name="span"/>.</returns>
        public static int Count<T>(this Span<T> span, T value) where T : IEquatable<T>? =>
            Count((ReadOnlySpan<T>)span, value);

        /// <summary>Counts the number of times the specified <paramref name="value"/> occurs in the <paramref name="span"/>.</summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value for which to search.</param>
        /// <returns>The number of times <paramref name="value"/> was found in the <paramref name="span"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
        {
            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                {
                    return SpanHelpers.CountValueType(
                        ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, byte>(ref value),
                        span.Length);
                }
                else if (Unsafe.SizeOf<T>() == sizeof(short))
                {
                    return SpanHelpers.CountValueType(
                        ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, short>(ref value),
                        span.Length);
                }
                else if (Unsafe.SizeOf<T>() == sizeof(int))
                {
                    return SpanHelpers.CountValueType(
                        ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, int>(ref value),
                        span.Length);
                }
                else if (Unsafe.SizeOf<T>() == sizeof(long))
                {
                    return SpanHelpers.CountValueType(
                        ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)),
                        Unsafe.As<T, long>(ref value),
                        span.Length);
                }
            }

            return SpanHelpers.Count(
                ref MemoryMarshal.GetReference(span),
                value,
                span.Length);
        }

        /// <summary>Counts the number of times the specified <paramref name="value"/> occurs in the <paramref name="span"/>.</summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value for which to search.</param>
        /// <returns>The number of times <paramref name="value"/> was found in the <paramref name="span"/>.</returns>
        public static int Count<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>? =>
            Count((ReadOnlySpan<T>)span, value);

        /// <summary>Counts the number of times the specified <paramref name="value"/> occurs in the <paramref name="span"/>.</summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value for which to search.</param>
        /// <returns>The number of times <paramref name="value"/> was found in the <paramref name="span"/>.</returns>
        public static int Count<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>?
        {
            switch (value.Length)
            {
                case 0:
                    return 0;

                case 1:
                    return Count(span, value[0]);

                default:
                    int count = 0;

                    int pos;
                    while ((pos = span.IndexOf(value)) >= 0)
                    {
                        span = span.Slice(pos + value.Length);
                        count++;
                    }

                    return count;
            }
        }

        /// <summary>Writes the specified interpolated string to the character span.</summary>
        /// <param name="destination">The span to which the interpolated string should be formatted.</param>
        /// <param name="handler">The interpolated string.</param>
        /// <param name="charsWritten">The number of characters written to the span.</param>
        /// <returns>true if the entire interpolated string could be formatted successfully; otherwise, false.</returns>
        public static bool TryWrite(this Span<char> destination, [InterpolatedStringHandlerArgument(nameof(destination))] ref TryWriteInterpolatedStringHandler handler, out int charsWritten)
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
        public static bool TryWrite(this Span<char> destination, IFormatProvider? provider, [InterpolatedStringHandlerArgument(nameof(destination), nameof(provider))] ref TryWriteInterpolatedStringHandler handler, out int charsWritten) =>
            // The provider is passed to the handler by the compiler, so the actual implementation of the method
            // is the same as the non-provider overload.
            TryWrite(destination, ref handler, out charsWritten);

        /// <summary>
        /// Writes the <see cref="CompositeFormat"/> string to the character span, substituting the format item or items
        /// with the string representation of the corresponding arguments.
        /// </summary>
        /// <typeparam name="TArg0">The type of the first object to format.</typeparam>
        /// <param name="destination">The span to which the string should be formatted.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">A <see cref="CompositeFormat"/>.</param>
        /// <param name="charsWritten">The number of characters written to the span.</param>
        /// <param name="arg0">The first object to format.</param>
        /// <returns><see langword="true"/> if the entire interpolated string could be formatted successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public static bool TryWrite<TArg0>(this Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, TArg0 arg0)
        {
            ArgumentNullException.ThrowIfNull(format);
            format.ValidateNumberOfArgs(1);
            return TryWrite(destination, provider, format, out charsWritten, arg0, 0, 0, default);
        }

        /// <summary>
        /// Writes the <see cref="CompositeFormat"/> string to the character span, substituting the format item or items
        /// with the string representation of the corresponding arguments.
        /// </summary>
        /// <typeparam name="TArg0">The type of the first object to format.</typeparam>
        /// <typeparam name="TArg1">The type of the second object to format.</typeparam>
        /// <param name="destination">The span to which the string should be formatted.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">A <see cref="CompositeFormat"/>.</param>
        /// <param name="charsWritten">The number of characters written to the span.</param>
        /// <param name="arg0">The first object to format.</param>
        /// <param name="arg1">The second object to format.</param>
        /// <returns><see langword="true"/> if the entire interpolated string could be formatted successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public static bool TryWrite<TArg0, TArg1>(this Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1)
        {
            ArgumentNullException.ThrowIfNull(format);
            format.ValidateNumberOfArgs(2);
            return TryWrite(destination, provider, format, out charsWritten, arg0, arg1, 0, default);
        }

        /// <summary>
        /// Writes the <see cref="CompositeFormat"/> string to the character span, substituting the format item or items
        /// with the string representation of the corresponding arguments.
        /// </summary>
        /// <typeparam name="TArg0">The type of the first object to format.</typeparam>
        /// <typeparam name="TArg1">The type of the second object to format.</typeparam>
        /// <typeparam name="TArg2">The type of the third object to format.</typeparam>
        /// <param name="destination">The span to which the string should be formatted.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">A <see cref="CompositeFormat"/>.</param>
        /// <param name="charsWritten">The number of characters written to the span.</param>
        /// <param name="arg0">The first object to format.</param>
        /// <param name="arg1">The second object to format.</param>
        /// <param name="arg2">The third object to format.</param>
        /// <returns><see langword="true"/> if the entire interpolated string could be formatted successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public static bool TryWrite<TArg0, TArg1, TArg2>(this Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            ArgumentNullException.ThrowIfNull(format);
            format.ValidateNumberOfArgs(3);
            return TryWrite(destination, provider, format, out charsWritten, arg0, arg1, arg2, default);
        }

        /// <summary>
        /// Writes the <see cref="CompositeFormat"/> string to the character span, substituting the format item or items
        /// with the string representation of the corresponding arguments.
        /// </summary>
        /// <param name="destination">The span to which the string should be formatted.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">A <see cref="CompositeFormat"/>.</param>
        /// <param name="charsWritten">The number of characters written to the span.</param>
        /// <param name="args">An array of objects to format.</param>
        /// <returns><see langword="true"/> if the entire interpolated string could be formatted successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is null.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public static bool TryWrite(this Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(format);
            ArgumentNullException.ThrowIfNull(args);
            return TryWrite(destination, provider, format, out charsWritten, (ReadOnlySpan<object?>)args);
        }

        /// <summary>
        /// Writes the <see cref="CompositeFormat"/> string to the character span, substituting the format item or items
        /// with the string representation of the corresponding arguments.
        /// </summary>
        /// <param name="destination">The span to which the string should be formatted.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">A <see cref="CompositeFormat"/>.</param>
        /// <param name="charsWritten">The number of characters written to the span.</param>
        /// <param name="args">A span of objects to format.</param>
        /// <returns><see langword="true"/> if the entire interpolated string could be formatted successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public static bool TryWrite(this Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, ReadOnlySpan<object?> args)
        {
            ArgumentNullException.ThrowIfNull(format);
            format.ValidateNumberOfArgs(args.Length);
            return args.Length switch
            {
                0 => TryWrite(destination, provider, format, out charsWritten, 0, 0, 0, args),
                1 => TryWrite(destination, provider, format, out charsWritten, args[0], 0, 0, args),
                2 => TryWrite(destination, provider, format, out charsWritten, args[0], args[1], 0, args),
                _ => TryWrite(destination, provider, format, out charsWritten, args[0], args[1], args[2], args),
            };
        }

        private static bool TryWrite<TArg0, TArg1, TArg2>(Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1, TArg2 arg2, ReadOnlySpan<object?> args)
        {
            // Create the interpolated string handler.
            var handler = new TryWriteInterpolatedStringHandler(format._literalLength, format._formattedCount, destination, provider, out bool shouldAppend);

            if (shouldAppend)
            {
                // Write each segment.
                foreach ((string? Literal, int ArgIndex, int Alignment, string? Format) segment in format._segments)
                {
                    bool appended;
                    if (segment.Literal is string literal)
                    {
                        appended = handler.AppendLiteral(literal);
                    }
                    else
                    {
                        int index = segment.ArgIndex;
                        switch (index)
                        {
                            case 0:
                                appended = handler.AppendFormatted(arg0, segment.Alignment, segment.Format);
                                break;

                            case 1:
                                appended = handler.AppendFormatted(arg1, segment.Alignment, segment.Format);
                                break;

                            case 2:
                                appended = handler.AppendFormatted(arg2, segment.Alignment, segment.Format);
                                break;

                            default:
                                Debug.Assert(index > 2);
                                appended = handler.AppendFormatted(args[index], segment.Alignment, segment.Format);
                                break;
                        }
                    }

                    if (!appended)
                    {
                        break;
                    }
                }
            }

            // Complete the operation.
            return TryWrite(destination, provider, ref handler, out charsWritten);
        }

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

                    if (typeof(T).IsEnum)
                    {
                        if (Enum.TryFormatUnconstrained(value, _destination.Slice(_pos), out int charsWritten))
                        {
                            _pos += charsWritten;
                            return true;
                        }

                        return Fail();
                    }

                    if (value is ISpanFormattable)
                    {
                        if (((ISpanFormattable)value).TryFormat(_destination.Slice(_pos), out int charsWritten, default, _provider)) // constrained call avoiding boxing for value types
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

                return s is null || AppendLiteral(s);
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

                    if (typeof(T).IsEnum)
                    {
                        if (Enum.TryFormatUnconstrained(value, _destination.Slice(_pos), out int charsWritten, format))
                        {
                            _pos += charsWritten;
                            return true;
                        }

                        return Fail();
                    }

                    if (value is ISpanFormattable)
                    {
                        if (((ISpanFormattable)value).TryFormat(_destination.Slice(_pos), out int charsWritten, format, _provider)) // constrained call avoiding boxing for value types
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

                return s is null || AppendLiteral(s);
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
            public bool AppendFormatted(scoped ReadOnlySpan<char> value)
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
            public bool AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
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
                    return AppendLiteral(customFormatted);
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
