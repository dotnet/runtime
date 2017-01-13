// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Extension methods for ReadOnlySpan&lt;T&gt;.
    /// </summary>
    public static class ReadOnlySpanExtensions
    {
        /// <summary>
        /// Creates a new readonly span over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> Slice(this string text)
        {
            if (text == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);

            return new ReadOnlySpan<char>(Unsafe.As<Pinnable<char>>(text), StringAdjustment, text.Length);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target string, beginning at 'start'.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> Slice(this string text, int start)
        {
            if (text == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);
            int textLength = text.Length;
            if ((uint)start > (uint)textLength)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            unsafe
            {
                byte* byteOffset = ((byte*)StringAdjustment) + (uint)(start * sizeof(char));
                return new ReadOnlySpan<char>(Unsafe.As<Pinnable<char>>(text), (IntPtr)byteOffset, textLength - start);
            }
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target string, beginning at <paramref name="start"/>, of given <paramref name="length"/>.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The number of items in the span.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> Slice(this string text, int start, int length)
        {
            if (text == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);
            int textLength = text.Length;
            if ((uint)start > (uint)textLength || (uint)length > (uint)(textLength - start))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            unsafe
            {
                byte* byteOffset = ((byte*)StringAdjustment) + (uint)(start * sizeof(char));
                return new ReadOnlySpan<char>(Unsafe.As<Pinnable<char>>(text), (IntPtr)byteOffset, length);
            }
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence. If not found, returns -1. Values are compared using IEquatable&lt;T&gt;.Equals(T). 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this ReadOnlySpan<T> span, T value)
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
        public static int IndexOf(this ReadOnlySpan<byte> span, byte value)
        {
            return SpanHelpers.IndexOf(ref span.DangerousGetPinnableReference(), value, span.Length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence. If not found, returns -1. 
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this ReadOnlySpan<char> span, char value)
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
        /// Searches for the specified sequence and returns the index of its first occurrence. If not found, returns -1.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The sequence to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
        {
            return SpanHelpers.IndexOf(ref span.DangerousGetPinnableReference(), span.Length, ref value.DangerousGetPinnableReference(), value.Length);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable&lt;T&gt;.Equals(T). 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqual<T>(this ReadOnlySpan<T> first, ReadOnlySpan<T> second)
            where T:struct, IEquatable<T>
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
        /// Determines whether two sequences are equal by comparing the elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqual(this ReadOnlySpan<char> first, ReadOnlySpan<char> second)
        {
            int length = first.Length;
            return length == second.Length && SpanHelpers.SequenceEqual(ref first.DangerousGetPinnableReference(), ref second.DangerousGetPinnableReference(), length);
        }

        private static readonly IntPtr StringAdjustment = MeasureStringAdjustment();

        private static IntPtr MeasureStringAdjustment()
        {
            string sampleString = "a";
            unsafe
            {
                fixed (char* pSampleString = sampleString)
                {
                    return Unsafe.ByteOffset<char>(ref Unsafe.As<Pinnable<char>>(sampleString).Data, ref Unsafe.AsRef<char>(pSampleString));
                }
            }
        }
    }
}
