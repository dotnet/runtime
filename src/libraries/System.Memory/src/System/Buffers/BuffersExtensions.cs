// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers
{
    /// <summary>
    /// Extension methods for <see cref="ReadOnlySequence{T}"/>
    /// </summary>
    public static class BuffersExtensions
    {
        /// <summary>
        /// Returns position of first occurrence of item in the <see cref="ReadOnlySequence{T}"/>
        /// </summary>
        public static SequencePosition? PositionOf<T>(this ReadOnlySequence<T> sequence, T value) where T : IEquatable<T>
        {
            SequencePosition position = sequence.Start;
            SequencePosition result = position;
            while (sequence.TryGet(ref position, out var memory))
            {
                var index = memory.Span.IndexOf(value);
                if (index != -1)
                {
                    return sequence.GetPosition(result, index);
                }
                result = position;
            }
            return null;
        }

        /// <summary>
        /// Copy the <see cref="ReadOnlySequence{T}"/> to the specified <see cref="Span{Byte}"/>.
        /// </summary>
        /// <param name="sequence">The source <see cref="ReadOnlySequence{T}"/>.</param>
        /// <param name="destination">The destination <see cref="Span{Byte}"/>.</param>
        public static void CopyTo<T>(this ReadOnlySequence<T> sequence, Span<T> destination)
        {
            if (sequence.Length > destination.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.destination);

            foreach (var segment in sequence)
            {
                segment.Span.CopyTo(destination);
                destination = destination.Slice(segment.Length);
            }
        }

        /// <summary>
        /// Converts the <see cref="ReadOnlySequence{T}"/> to an array
        /// </summary>
        public static T[] ToArray<T>(this ReadOnlySequence<T> sequence)
        {
            var array = new T[sequence.Length];
            sequence.CopyTo(array);
            return array;
        }

        /// <summary>
        /// Writes contents of <paramref name="source"/> to <paramref name="bufferWriter"/>
        /// </summary>
        public static void Write<T>(this IBufferWriter<T> bufferWriter, ReadOnlySpan<byte> source)
        {
            Span<byte> destination = bufferWriter.GetSpan();

            // Fast path, try copying to the available memory directly
            if (source.Length <= destination.Length)
            {
                source.CopyTo(destination);
                bufferWriter.Advance(source.Length);
                return;
            }

            while (source.Length > 0)
            {
                int writeSize;
                if (destination.Length == 0)
                {
                    writeSize = Math.Min(source.Length, bufferWriter.MaxBufferSize);
                    destination = bufferWriter.GetSpan(writeSize);
                }
                else
                {
                    writeSize = destination.Length;
                }

                bufferWriter.Advance(writeSize);
                source.Slice(0, writeSize).CopyTo(destination);
                source = source.Slice(writeSize);
                destination = default;
            }
        }
    }
}
