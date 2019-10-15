// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Text;

namespace System.Memory.Tests.SequenceReader
{
    public static class SequenceFactory
    {
        public static ReadOnlySequence<T> CreateSplit<T>(T[] buffer, int minSize, int maxSize) where T : struct
        {
            if (buffer == null || buffer.Length == 0 || minSize <= 0 || maxSize <= 0 || minSize > maxSize)
            {
                throw new InvalidOperationException();
            }

            Random r = new Random(0xFEED);

            SequenceSegment<T> last = null;
            SequenceSegment<T> first = null;
            OwnedArray<T> ownedBuffer = new OwnedArray<T>(buffer);

            int remaining = buffer.Length;
            int position = 0;
            while (remaining > 0)
            {
                int take = Math.Min(r.Next(minSize, maxSize), remaining);
                SequenceSegment<T> current = new SequenceSegment<T>();
                current.SetMemory(ownedBuffer, position, position + take);
                if (first == null)
                {
                    first = current;
                    last = current;
                }
                else
                {
                    last.SetNext(current);
                    last = current;
                }
                remaining -= take;
                position += take;
            }

            return new ReadOnlySequence<T>(first, 0, last, last.Length);
        }

        public static ReadOnlySequence<T> Create<T>(params T[][] inputs) where T : struct
        {
            if (inputs == null || inputs.Length == 0)
            {
                throw new InvalidOperationException();
            }

            SequenceSegment<T> last = null;
            SequenceSegment<T> first = null;

            for (int i = 0; i < inputs.Length; i++)
            {
                T[] source = inputs[i];
                int length = source.Length;
                int dataOffset = length;

                // Shift the incoming data for testing
                T[] chars = new T[length * 8];
                for (int j = 0; j < length; j++)
                {
                    chars[dataOffset + j] = source[j];
                }

                // Create a segment that has offset relative to the OwnedMemory and OwnedMemory itself has offset relative to array
                OwnedArray<T> ownedBuffer = new OwnedArray<T>(chars);
                SequenceSegment<T> current = new SequenceSegment<T>();
                current.SetMemory(ownedBuffer, length, length * 2);
                if (first == null)
                {
                    first = current;
                    last = current;
                }
                else
                {
                    last.SetNext(current);
                    last = current;
                }
            }

            return new ReadOnlySequence<T>(first, 0, last, last.Length);
        }

        public static ReadOnlySequence<byte> CreateUtf8(params string[] inputs)
        {
            byte[][] buffers = new byte[inputs.Length][];
            for (int i = 0; i < inputs.Length; i++)
            {
                buffers[i] = Encoding.UTF8.GetBytes(inputs[i]);
            }
            return Create(buffers);
        }

        public static ReadOnlySequence<T> Create<T>(params int[] inputs) where T : struct
        {
            T[][] buffers;
            if (inputs.Length == 0)
            {
                buffers = new[] { new T[] { } };
            }
            else
            {
                buffers = new T[inputs.Length][];
                for (int i = 0; i < inputs.Length; i++)
                {
                    buffers[i] = new T[inputs[i]];
                }
            }
            return Create<T>(buffers);
        }
    }
}
