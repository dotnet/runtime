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
        public static SequencePosition? PositionOf<T>(in this ReadOnlySequence<T> sequence, T value) where T : IEquatable<T>
        {
            if (sequence.IsSingleSegment)
            {
                int index = sequence.First.Span.IndexOf(value);
                if (index != -1)
                {
                    return sequence.GetPosition(index);
                }

                return null;
            }
            else
            {
                return PositionOfMultiSegement(sequence, value);
            }
        }

        private static SequencePosition? PositionOfMultiSegement<T>(in ReadOnlySequence<T> sequence, T value) where T : IEquatable<T>
        {
            SequencePosition position = sequence.Start;
            SequencePosition result = position;
            while (sequence.TryGet(ref position, out ReadOnlyMemory<T> memory))
            {
                int index = memory.Span.IndexOf(value);
                if (index != -1)
                {
                    return sequence.GetPosition(index, result);
                }
                else if (position.GetObject() == null)
                {
                    break;
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
        public static void CopyTo<T>(in this ReadOnlySequence<T> sequence, Span<T> destination)
        {
            if (sequence.Length > destination.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.destination);

            if (sequence.IsSingleSegment)
            {
                sequence.First.Span.CopyTo(destination);
            }
            else
            {
                CopyToMultiSegement(sequence, destination);
            }
        }

        private static void CopyToMultiSegement<T>(in ReadOnlySequence<T> sequence, Span<T> destination)
        {
            SequencePosition position = sequence.Start;
            while (sequence.TryGet(ref position, out ReadOnlyMemory<T> memory))
            {
                ReadOnlySpan<T> span = memory.Span;
                span.CopyTo(destination);
                if (position.GetObject() != null)
                {
                    destination = destination.Slice(span.Length);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Converts the <see cref="ReadOnlySequence{T}"/> to an array
        /// </summary>
        public static T[] ToArray<T>(in this ReadOnlySequence<T> sequence)
        {
            var array = new T[sequence.Length];
            sequence.CopyTo(array);
            return array;
        }

        /// <summary>
        /// Writes contents of <paramref name="source"/> to <paramref name="bufferWriter"/>
        /// </summary>
        public static void Write<T>(this IBufferWriter<T> bufferWriter, ReadOnlySpan<T> source)
        {
            Span<T> destination = bufferWriter.GetSpan();

            // Fast path, try copying to the available memory directly
            if (source.Length <= destination.Length)
            {
                source.CopyTo(destination);
                bufferWriter.Advance(source.Length);
            }
            else
            {
                WriteMultiSegment(bufferWriter, source, destination);
            }
        }

        private static void WriteMultiSegment<T>(IBufferWriter<T> bufferWriter, in ReadOnlySpan<T> source, Span<T> destination)
        {
            ReadOnlySpan<T> input = source;
            while (true)
            {
                int writeSize = Math.Min(destination.Length, input.Length);
                input.Slice(0, writeSize).CopyTo(destination);
                bufferWriter.Advance(writeSize);
                input = input.Slice(writeSize);
                if (input.Length > 0)
                {
                    destination = bufferWriter.GetSpan(input.Length);
                    continue;
                }

                return;
            }
        }
    }
}
