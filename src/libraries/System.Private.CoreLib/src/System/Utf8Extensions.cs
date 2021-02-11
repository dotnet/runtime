// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

namespace System
{
    public static partial class Utf8Extensions
    {
        /// <summary>
        /// Projects <paramref name="text"/> as a <see cref="ReadOnlySpan{Byte}"/>.
        /// </summary>
        public static ReadOnlySpan<byte> AsBytes(this ReadOnlySpan<Char8> text)
        {
            return MemoryMarshal.Cast<Char8, byte>(text);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target <see cref="Utf8String"/>.
        /// </summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> AsBytes(this Utf8String? text)
        {
            if (text is null)
                return default;

            return CreateSpan(text);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target <see cref="Utf8String"/>.
        /// </summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;text.Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> AsBytes(this Utf8String? text, int start)
        {
            if (text is null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

            if ((uint)start > (uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            return CreateSpan(text, start);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target <see cref="Utf8String"/>.
        /// </summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index or <paramref name="length"/> is not in range.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> AsBytes(this Utf8String? text, int start, int length)
        {
            if (text is null)
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

            return CreateSpan(text, start, length);
        }

        /// <summary>
        /// Creates a new <see cref="Utf8Span"/> over the target <see cref="Utf8String"/>.
        /// </summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Utf8Span AsSpan(this Utf8String? text)
        {
            if (text is null)
                return default;

            return new Utf8Span(text);
        }

        /// <summary>
        /// Creates a new <see cref="Utf8Span"/> over the portion of the target <see cref="Utf8String"/>.
        /// </summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;text.Length).
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the resulting span would split a multi-byte UTF-8 subsequence.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Utf8Span AsSpan(this Utf8String? text, int start)
        {
            if (text is null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

            if ((uint)start > (uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            // It's always safe for us to read just past the end of the string (since there's a null terminator),
            // so we don't need to perform any additional bounds checking. We only need to check that we're not
            // splitting in the middle of a multi-byte UTF-8 subsequence.

            if (Utf8Utility.IsUtf8ContinuationByte(text.DangerousGetMutableReference(start)))
            {
                Utf8String.ThrowImproperStringSplit();
            }

            return Utf8Span.UnsafeCreateWithoutValidation(CreateSpan(text, start));
        }

        /// <summary>
        /// Creates a new <see cref="Utf8Span"/> over the portion of the target <see cref="Utf8String"/>.
        /// </summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index or <paramref name="length"/> is not in range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the resulting span would split a multi-byte UTF-8 subsequence.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Utf8Span AsSpan(this Utf8String? text, int start, int length)
        {
            if (text is null)
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

            // It's always safe for us to read just past the end of the string (since there's a null terminator),
            // so we don't need to perform any additional bounds checking. We only need to check that we're not
            // splitting in the middle of a multi-byte UTF-8 subsequence.

            if (Utf8Utility.IsUtf8ContinuationByte(text.DangerousGetMutableReference(start))
                || Utf8Utility.IsUtf8ContinuationByte(text.DangerousGetMutableReference(start + length)))
            {
                Utf8String.ThrowImproperStringSplit();
            }

            return Utf8Span.UnsafeCreateWithoutValidation(CreateSpan(text, start, length));
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target <see cref="Utf8String"/>.</summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        public static ReadOnlyMemory<byte> AsMemoryBytes(this Utf8String? text)
        {
            if (text is null)
                return default;

            return CreateMemoryBytes(text, 0, text.Length);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target <see cref="Utf8String"/>.</summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;text.Length).
        /// </exception>
        public static ReadOnlyMemory<byte> AsMemoryBytes(this Utf8String? text, int start)
        {
            if (text is null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

            if ((uint)start > (uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            return CreateMemoryBytes(text, start, text.Length - start);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target <see cref="Utf8String"/>.</summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="startIndex">The index at which to begin this slice.</param>
        public static ReadOnlyMemory<byte> AsMemoryBytes(this Utf8String? text, Index startIndex)
        {
            if (text is null)
            {
                if (!startIndex.Equals(Index.Start))
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);

                return default;
            }

            int actualIndex = startIndex.GetOffset(text.Length);
            if ((uint)actualIndex > (uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return CreateMemoryBytes(text, actualIndex, text.Length - actualIndex);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target <see cref="Utf8String"/>.</summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index or <paramref name="length"/> is not in range.
        /// </exception>
        public static ReadOnlyMemory<byte> AsMemoryBytes(this Utf8String? text, int start, int length)
        {
            if (text is null)
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

            return CreateMemoryBytes(text, start, length);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target <see cref="Utf8String"/>.</summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="range">The range used to indicate the start and length of the sliced string.</param>
        public static ReadOnlyMemory<byte> AsMemoryBytes(this Utf8String? text, Range range)
        {
            if (text is null)
            {
                Index startIndex = range.Start;
                Index endIndex = range.End;

                if (!startIndex.Equals(Index.Start) || !endIndex.Equals(Index.Start))
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);

                return default;
            }

            (int start, int length) = range.GetOffsetAndLength(text.Length);
            return CreateMemoryBytes(text, start, length);
        }

        /// <summary>
        /// Returns a <see cref="Utf8String"/> representation of this <see cref="Rune"/> instance.
        /// </summary>
        public static Utf8String ToUtf8String(this Rune rune) => Utf8String.CreateFromRune(rune);
    }
}
