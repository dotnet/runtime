// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.MemoryTests;
using System.Text;

namespace System.Memory.Tests
{
    public abstract class ReadOnlySequenceFactoryChar
    {
        public static ReadOnlySequenceFactoryChar ArrayFactory { get; } = new ArrayTestSequenceFactoryChar();
        public static ReadOnlySequenceFactoryChar MemoryFactory { get; } = new MemoryTestSequenceFactoryChar();
        public static ReadOnlySequenceFactoryChar StringFactory { get; } = new StringTestSequenceFactoryChar();
        public static ReadOnlySequenceFactoryChar OwnedMemoryFactory { get; } = new OwnedMemoryTestSequenceFactoryChar();
        public static ReadOnlySequenceFactoryChar SingleSegmentFactory { get; } = new SingleSegmentTestSequenceFactoryChar();
        public static ReadOnlySequenceFactoryChar SegmentPerCharFactory { get; } = new CharPerSegmentTestSequenceFactoryChar();

        public abstract ReadOnlySequence<char> CreateOfSize(int size);
        public abstract ReadOnlySequence<char> CreateWithContent(char[] data);

        public ReadOnlySequence<char> CreateWithContent(string data)
        {
            return CreateWithContent(data.ToCharArray());
        }

        internal class ArrayTestSequenceFactoryChar : ReadOnlySequenceFactoryChar
        {
            public override ReadOnlySequence<char> CreateOfSize(int size)
            {
                return new ReadOnlySequence<char>(new char[size + 20], 10, size);
            }

            public override ReadOnlySequence<char> CreateWithContent(char[] data)
            {
                var startSegment = new char[data.Length + 20];
                Array.Copy(data, 0, startSegment, 10, data.Length);
                return new ReadOnlySequence<char>(startSegment, 10, data.Length);
            }
        }

        internal class StringTestSequenceFactoryChar : ReadOnlySequenceFactoryChar
        {
            static string s_stringData = InitalizeStringData();

            static string InitalizeStringData()
            {
                IEnumerable<int> ascii = Enumerable.Range(' ', (char)0x7f - ' ');

                return new string(ascii.Concat(ascii)
                    .Concat(ascii)
                    .Concat(ascii)
                    .Concat(ascii)
                    .Concat(ascii)
                    .Concat(ascii)
                    .Select(c => (char)c)
                    .ToArray());
            }

            public override ReadOnlySequence<char> CreateOfSize(int size)
            {
                return new ReadOnlySequence<char>(s_stringData.AsMemory(10, size));
            }

            public override ReadOnlySequence<char> CreateWithContent(char[] data)
            {
                var startSegment = new char[data.Length + 20];
                Array.Copy(data, 0, startSegment, 10, data.Length);
                var text = new string(startSegment);
                return new ReadOnlySequence<char>(text.AsMemory(10, data.Length));
            }
        }

        internal class MemoryTestSequenceFactoryChar : ReadOnlySequenceFactoryChar
        {
            public override ReadOnlySequence<char> CreateOfSize(int size)
            {
                return CreateWithContent(new char[size]);
            }

            public override ReadOnlySequence<char> CreateWithContent(char[] data)
            {
                var startSegment = new char[data.Length + 20];
                Array.Copy(data, 0, startSegment, 10, data.Length);
                return new ReadOnlySequence<char>(new ReadOnlyMemory<char>(startSegment, 10, data.Length));
            }
        }

        internal class OwnedMemoryTestSequenceFactoryChar : ReadOnlySequenceFactoryChar
        {
            public override ReadOnlySequence<char> CreateOfSize(int size)
            {
                return CreateWithContent(new char[size]);
            }

            public override ReadOnlySequence<char> CreateWithContent(char[] data)
            {
                var startSegment = new char[data.Length + 20];
                Array.Copy(data, 0, startSegment, 10, data.Length);
                return new ReadOnlySequence<char>(new CustomMemoryForTest<char>(startSegment, 10, data.Length));
            }
        }

        internal class SingleSegmentTestSequenceFactoryChar : ReadOnlySequenceFactoryChar
        {
            public override ReadOnlySequence<char> CreateOfSize(int size)
            {
                return CreateWithContent(new char[size]);
            }

            public override ReadOnlySequence<char> CreateWithContent(char[] data)
            {
                return CreateSegments(data);
            }
        }

        internal class CharPerSegmentTestSequenceFactoryChar : ReadOnlySequenceFactoryChar
        {
            public override ReadOnlySequence<char> CreateOfSize(int size)
            {
                return CreateWithContent(new char[size]);
            }

            public override ReadOnlySequence<char> CreateWithContent(char[] data)
            {
                var segments = new List<char[]>();

                segments.Add(Array.Empty<char>());
                foreach (var b in data)
                {
                    segments.Add(new[] { b });
                    segments.Add(Array.Empty<char>());
                }

                return CreateSegments(segments.ToArray());
            }
        }

        public static ReadOnlySequence<char> CreateSegments(params char[][] inputs)
        {
            if (inputs == null || inputs.Length == 0)
            {
                throw new InvalidOperationException();
            }

            int i = 0;

            BufferSegment<char> last = null;
            BufferSegment<char> first = null;

            do
            {
                char[] s = inputs[i];
                int length = s.Length;
                int dataOffset = length;
                var chars = new char[length * 2];

                for (int j = 0; j < length; j++)
                {
                    chars[dataOffset + j] = s[j];
                }

                // Create a segment that has offset relative to the OwnedMemory and OwnedMemory itself has offset relative to array
                Memory<char> memory = new Memory<char>(chars).Slice(length, length);

                if (first == null)
                {
                    first = new BufferSegment<char>(memory);
                    last = first;
                }
                else
                {
                    last = last.Append(memory);
                }
                i++;
            } while (i < inputs.Length);

            return new ReadOnlySequence<char>(first, 0, last, last.Memory.Length);
        }
    }
}
