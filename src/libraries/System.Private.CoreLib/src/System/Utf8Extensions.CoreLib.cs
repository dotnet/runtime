// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    public static partial class Utf8Extensions
    {
        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target <see cref="Utf8String"/>.</summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        public static ReadOnlyMemory<Char8> AsMemory(this Utf8String? text)
        {
            if (text is null)
                return default;

            return new ReadOnlyMemory<Char8>(text, 0, text.Length);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target <see cref="Utf8String"/>.</summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;text.Length).
        /// </exception>
        public static ReadOnlyMemory<Char8> AsMemory(this Utf8String? text, int start)
        {
            if (text is null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

            if ((uint)start > (uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            return new ReadOnlyMemory<Char8>(text, start, text.Length - start);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target <see cref="Utf8String"/>.</summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="startIndex">The index at which to begin this slice.</param>
        public static ReadOnlyMemory<Char8> AsMemory(this Utf8String? text, Index startIndex)
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

            return new ReadOnlyMemory<Char8>(text, actualIndex, text.Length - actualIndex);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target <see cref="Utf8String"/>.</summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index or <paramref name="length"/> is not in range.
        /// </exception>
        public static ReadOnlyMemory<Char8> AsMemory(this Utf8String? text, int start, int length)
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

            return new ReadOnlyMemory<Char8>(text, start, length);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target <see cref="Utf8String"/>.</summary>
        /// <param name="text">The target <see cref="Utf8String"/>.</param>
        /// <param name="range">The range used to indicate the start and length of the sliced string.</param>
        public static ReadOnlyMemory<Char8> AsMemory(this Utf8String? text, Range range)
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
            return new ReadOnlyMemory<Char8>(text, start, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text) =>
            new ReadOnlySpan<byte>(ref text.DangerousGetMutableReference(), text.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text, int start) =>
            new ReadOnlySpan<byte>(ref text.DangerousGetMutableReference(start), text.Length - start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text, int start, int length) =>
            new ReadOnlySpan<byte>(ref text.DangerousGetMutableReference(start), length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlyMemory<byte> CreateMemoryBytes(Utf8String text, int start, int length) =>
            new ReadOnlyMemory<byte>(text, start, length);
    }
}
