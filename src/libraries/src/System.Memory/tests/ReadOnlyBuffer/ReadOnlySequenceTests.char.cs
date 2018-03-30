// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Memory.Tests
{
    public abstract class ReadOnlySequenceTestsChar
    {
        public class Array : ReadOnlySequenceTestsChar
        {
            public Array() : base(ReadOnlySequenceFactoryChar.ArrayFactory) { }
        }

        public class String : ReadOnlySequenceTestsChar
        {
            public String() : base(ReadOnlySequenceFactoryChar.StringFactory) { }
        }

        public class Memory : ReadOnlySequenceTestsChar
        {
            public Memory() : base(ReadOnlySequenceFactoryChar.MemoryFactory) { }
        }

        public class SingleSegment : ReadOnlySequenceTestsChar
        {
            public SingleSegment() : base(ReadOnlySequenceFactoryChar.SingleSegmentFactory) { }
        }

        public class SegmentPerChar : ReadOnlySequenceTestsChar
        {
            public SegmentPerChar() : base(ReadOnlySequenceFactoryChar.SegmentPerCharFactory) { }
        }

        internal ReadOnlySequenceFactoryChar Factory { get; }

        internal ReadOnlySequenceTestsChar(ReadOnlySequenceFactoryChar factory)
        {
            Factory = factory;
        }

        [Fact]
        public void EmptyIsCorrect()
        {
            ReadOnlySequence<char> buffer = Factory.CreateOfSize(0);
            Assert.Equal(0, buffer.Length);
            Assert.True(buffer.IsEmpty);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(8)]
        public void LengthIsCorrect(int length)
        {
            ReadOnlySequence<char> buffer = Factory.CreateOfSize(length);
            Assert.Equal(length, buffer.Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(8)]
        public void ToArrayIsCorrect(int length)
        {
            char[] data = Enumerable.Range(0, length).Select(i => (char)i).ToArray();
            ReadOnlySequence<char> buffer = Factory.CreateWithContent(data);
            Assert.Equal(length, buffer.Length);
            Assert.Equal(data, buffer.ToArray());
        }

        [Fact]
        public void ToStringIsCorrect()
        {
            ReadOnlySequence<char> buffer = Factory.CreateWithContent(Enumerable.Range(0, 255).Select(i => (char)i).ToArray());
            Assert.Equal("System.Buffers.ReadOnlySequence<Char>[255]", buffer.ToString());
        }

        [Theory]
        [MemberData(nameof(ValidSliceCases))]
        public void Slice_Works(Func<ReadOnlySequence<char>, ReadOnlySequence<char>> func)
        {
            ReadOnlySequence<char> buffer = Factory.CreateWithContent(new char[] { (char)0, (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9 });
            ReadOnlySequence<char> slice = func(buffer);
            Assert.Equal(new char[] { (char)5, (char)6, (char)7, (char)8, (char)9 }, slice.ToArray());
        }

        [Theory]
        [MemberData(nameof(OutOfRangeSliceCases))]
        public void ReadOnlyBufferDoesNotAllowSlicingOutOfRange(Action<ReadOnlySequence<char>> fail)
        {
            ReadOnlySequence<char> buffer = Factory.CreateOfSize(100);
            Assert.Throws<ArgumentOutOfRangeException>(() => fail(buffer));
        }

        [Fact]
        public void ReadOnlyBufferGetPosition_MovesPosition()
        {
            ReadOnlySequence<char> buffer = Factory.CreateOfSize(100);
            SequencePosition position = buffer.GetPosition(65);
            Assert.Equal(buffer.Slice(position).Length, 35);
            position = buffer.GetPosition(65, buffer.Start);
            Assert.Equal(buffer.Slice(position).Length, 35);
        }

        [Fact]
        public void ReadOnlyBufferGetPosition_ChecksBounds()
        {
            ReadOnlySequence<char> buffer = Factory.CreateOfSize(100);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPosition(101));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPosition(101, buffer.Start));
        }

        [Fact]
        public void ReadOnlyBufferGetPosition_DoesNotAlowNegative()
        {
            ReadOnlySequence<char> buffer = Factory.CreateOfSize(20);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPosition(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPosition(-1, buffer.Start));
        }

        public void ReadOnlyBufferSlice_ChecksEnd()
        {
            ReadOnlySequence<char> buffer = Factory.CreateOfSize(100);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Slice(70, buffer.Start));
        }

        [Fact]
        public void SegmentStartIsConsideredInBoundsCheck()
        {
            // 0               50           100    0             50             100
            // [                ##############] -> [##############                ]
            //                         ^c1            ^c2
            var bufferSegment1 = new BufferSegment<char>(new char[49]);
            BufferSegment<char> bufferSegment2 = bufferSegment1.Append(new char[50]);

            var buffer = new ReadOnlySequence<char>(bufferSegment1, 0, bufferSegment2, 50);

            SequencePosition c1 = buffer.GetPosition(25); // segment 1 index 75
            SequencePosition c2 = buffer.GetPosition(55); // segment 2 index 5

            ReadOnlySequence<char> sliced = buffer.Slice(c1, c2);
            Assert.Equal(30, sliced.Length);

            c1 = buffer.GetPosition(25, buffer.Start); // segment 1 index 75
            c2 = buffer.GetPosition(55, buffer.Start); // segment 2 index 5

            sliced = buffer.Slice(c1, c2);
            Assert.Equal(30, sliced.Length);
        }

        [Fact]
        public void GetPositionPrefersNextSegment()
        {
            BufferSegment<char> bufferSegment1 = new BufferSegment<char>(new char[50]);
            BufferSegment<char> bufferSegment2 = bufferSegment1.Append(new char[0]);

            ReadOnlySequence<char> buffer = new ReadOnlySequence<char>(bufferSegment1, 0, bufferSegment2, 0);

            SequencePosition c1 = buffer.GetPosition(50);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());

            c1 = buffer.GetPosition(50, buffer.Start);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());
        }

        [Fact]
        public void GetPositionDoesNotCrossOutsideBuffer()
        {
            var bufferSegment1 = new BufferSegment<char>(new char[100]);
            BufferSegment<char> bufferSegment2 = bufferSegment1.Append(new char[100]);
            BufferSegment<char> bufferSegment3 = bufferSegment2.Append(new char[0]);

            var buffer = new ReadOnlySequence<char>(bufferSegment1, 0, bufferSegment2, 100);

            SequencePosition c1 = buffer.GetPosition(200);

            Assert.Equal(100, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());

            c1 = buffer.GetPosition(200, buffer.Start);

            Assert.Equal(100, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());
        }

        [Fact]
        public void Create_WorksWithArray()
        {
            var buffer = new ReadOnlySequence<char>(new char[] { (char)1, (char)2, (char)3, (char)4, (char)5 });
            Assert.Equal(buffer.ToArray(), new char[] { (char)1, (char)2, (char)3, (char)4, (char)5 });
        }

        [Fact]
        public void Empty_ReturnsLengthZeroBuffer()
        {
            var buffer = ReadOnlySequence<char>.Empty;
            Assert.Equal(0, buffer.Length);
            Assert.Equal(true, buffer.IsSingleSegment);
            Assert.Equal(0, buffer.First.Length);
        }

        [Fact]
        public void Create_WorksWithArrayWithOffset()
        {
            var buffer = new ReadOnlySequence<char>(new char[] { (char)1, (char)2, (char)3, (char)4, (char)5 }, 2, 3);
            Assert.Equal(buffer.ToArray(), new char[] { (char)3, (char)4, (char)5 });
        }

        [Fact]
        public void C_WorksWithArrayWithOffset()
        {
            var buffer = new ReadOnlySequence<char>(new char[] { (char)1, (char)2, (char)3, (char)4, (char)5 }, 2, 3);
            Assert.Equal(buffer.ToArray(), new char[] { (char)3, (char)4, (char)5 });
        }


        [Fact]
        public void Create_WorksWithMemory()
        {
            var memory = new ReadOnlyMemory<char>(new char[] { (char)1, (char)2, (char)3, (char)4, (char)5 });
            var buffer = new ReadOnlySequence<char>(memory.Slice(2, 3));
            Assert.Equal(new char[] { (char)3, (char)4, (char)5 }, buffer.ToArray());
        }

        [Fact]
        public void SliceToTheEndWorks()
        {
            ReadOnlySequence<char> buffer = Factory.CreateOfSize(10);
            Assert.True(buffer.Slice(buffer.End).IsEmpty);
        }

        [Theory]
        [InlineData("a", 'a', 0)]
        [InlineData("ab", 'a', 0)]
        [InlineData("aab", 'a', 0)]
        [InlineData("acab", 'a', 0)]
        [InlineData("acab", 'c', 1)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", 'l', 11)]
        [InlineData("aaaaaaaaaaacmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", 'm', 12)]
        [InlineData("aaaaaaaaaaarmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", 'r', 11)]
        [InlineData("/localhost:5000/PATH/%2FPATH2/ HTTP/1.1", '%', 21)]
        [InlineData("/localhost:5000/PATH/%2FPATH2/?key=value HTTP/1.1", '%', 21)]
        [InlineData("/localhost:5000/PATH/PATH2/?key=value HTTP/1.1", '?', 27)]
        [InlineData("/localhost:5000/PATH/PATH2/ HTTP/1.1", ' ', 27)]
        public void PositionOf_ReturnsPosition(string raw, char searchFor, int expectIndex)
        {
            ReadOnlySequence<char> buffer = Factory.CreateWithContent(raw);
            SequencePosition? result = buffer.PositionOf((char)searchFor);

            Assert.NotNull(result);
            Assert.Equal(buffer.Slice(result.Value).ToArray(), raw.Substring(expectIndex));
        }

        [Fact]
        public void PositionOf_ReturnsNullIfNotFound()
        {
            ReadOnlySequence<char> buffer = Factory.CreateWithContent(new char[] { (char)1, (char)2, (char)3 });
            SequencePosition? result = buffer.PositionOf((char)4);

            Assert.Null(result);
        }

        [Fact]
        public void CopyTo_ThrowsWhenSourceLargerThenDestination()
        {
            ReadOnlySequence<char> buffer = Factory.CreateOfSize(10);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Span<char> span = new char[5];
                buffer.CopyTo(span);
            });
        }

        public static TheoryData<Func<ReadOnlySequence<char>, ReadOnlySequence<char>>> ValidSliceCases => new TheoryData<Func<ReadOnlySequence<char>, ReadOnlySequence<char>>>
        {
            b => b.Slice(5),
            b => b.Slice(0).Slice(5),
            b => b.Slice(5, 5),
            b => b.Slice(b.GetPosition(5), 5),
            b => b.Slice(5, b.GetPosition(10)),
            b => b.Slice(b.GetPosition(5), b.GetPosition(10)),
            b => b.Slice(b.GetPosition(5, b.Start), 5),
            b => b.Slice(5, b.GetPosition(10, b.Start)),
            b => b.Slice(b.GetPosition(5, b.Start), b.GetPosition(10, b.Start)),

            b => b.Slice((long)5),
            b => b.Slice((long)5, 5),
            b => b.Slice(b.GetPosition(5), (long)5),
            b => b.Slice((long)5, b.GetPosition(10)),
            b => b.Slice(b.GetPosition(5, b.Start), (long)5),
            b => b.Slice((long)5, b.GetPosition(10, b.Start)),
        };

        public static TheoryData<Action<ReadOnlySequence<char>>> OutOfRangeSliceCases => new TheoryData<Action<ReadOnlySequence<char>>>
        {
            b => b.Slice(101),
            b => b.Slice(0, 101),
            b => b.Slice(b.Start, 101),
            b => b.Slice(0, 70).Slice(b.End, b.End),
            b => b.Slice(0, 70).Slice(b.Start, b.End),
            b => b.Slice(0, 70).Slice(0, b.End)
        };
    }
}
