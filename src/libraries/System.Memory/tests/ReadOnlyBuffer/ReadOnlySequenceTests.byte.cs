// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Memory.Tests
{
    public abstract class ReadOnlySequenceTestsByte
    {
        public class Array : ReadOnlySequenceTestsByte
        {
            public Array() : base(ReadOnlySequenceFactory<byte>.ArrayFactory) { }
        }

        public class Memory : ReadOnlySequenceTestsByte
        {
            public Memory() : base(ReadOnlySequenceFactory<byte>.MemoryFactory) { }
        }

        public class SingleSegment : ReadOnlySequenceTestsByte
        {
            public SingleSegment() : base(ReadOnlySequenceFactory<byte>.SingleSegmentFactory) { }
        }

        public class SegmentPerByte : ReadOnlySequenceTestsByte
        {
            public SegmentPerByte() : base(ReadOnlySequenceFactory<byte>.SegmentPerItemFactory) { }
        }

        public class SplitInThreeSegments : ReadOnlySequenceTestsByte
        {
            public SplitInThreeSegments() : base(ReadOnlySequenceFactory<byte>.SplitInThree) { }
        }

        internal ReadOnlySequenceFactory<byte> Factory { get; }

        internal ReadOnlySequenceTestsByte(ReadOnlySequenceFactory<byte> factory)
        {
            Factory = factory;
        }

        [Fact]
        public void EmptyIsCorrect()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(0);
            Assert.Equal(0, buffer.Length);
            Assert.True(buffer.IsEmpty);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(8)]
        public void LengthIsCorrect(int length)
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(length);
            Assert.Equal(length, buffer.Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(8)]
        public void ToArrayIsCorrect(int length)
        {
            byte[] data = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
            ReadOnlySequence<byte> buffer = Factory.CreateWithContent(data);
            Assert.Equal(length, buffer.Length);
            Assert.Equal(data, buffer.ToArray());
        }

        [Fact]
        public void ToStringIsCorrect()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateWithContent(Enumerable.Range(0, 255).Select(i => (byte)i).ToArray());
            Assert.Equal("System.Buffers.ReadOnlySequence<Byte>[255]", buffer.ToString());
        }

        [Theory]
        [MemberData(nameof(ValidSliceCases))]
        public void Slice_Works(Func<ReadOnlySequence<byte>, ReadOnlySequence<byte>> func)
        {
            ReadOnlySequence<byte> buffer = Factory.CreateWithContent(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            ReadOnlySequence<byte> slice = func(buffer);
            Assert.Equal(new byte[] { 5, 6, 7, 8, 9 }, slice.ToArray());
        }

        [Theory]
        [MemberData(nameof(OutOfRangeSliceCases))]
        public void ReadOnlyBufferDoesNotAllowSlicingOutOfRange(Action<ReadOnlySequence<byte>> fail)
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(100);
            Assert.Throws<ArgumentOutOfRangeException>(() => fail(buffer));
        }

        [Fact]
        public void ReadOnlyBufferGetPosition_MovesPosition()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(100);

            SequencePosition position = buffer.GetPosition(65);
            Assert.Equal(35, buffer.Slice(position).Length);

            position = buffer.GetPosition(65, buffer.Start);
            Assert.Equal(35, buffer.Slice(position).Length);
        }

        [Fact]
        public void ReadOnlyBufferGetPosition_ChecksBounds()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(100);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPosition(101));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPosition(101, buffer.Start));
        }

        [Fact]
        public void ReadOnlyBufferGetPosition_DoesNotAlowNegative()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(20);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPosition(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPosition(-1, buffer.Start));
        }

        [Fact]
        public void ReadOnlyBufferSlice_ChecksEnd()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(100);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Slice(70, buffer.Start));
        }

        [Fact]
        public void SliceToTheEndWorks()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(10);
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
            ReadOnlySequence<byte> buffer = Factory.CreateWithContent(Encoding.ASCII.GetBytes(raw));
            SequencePosition? result = buffer.PositionOf((byte)searchFor);

            Assert.NotNull(result);
            Assert.Equal(buffer.Slice(result.Value).ToArray(), Encoding.ASCII.GetBytes(raw.Substring(expectIndex)));
        }

        [Fact]
        public void PositionOf_ReturnsNullIfNotFound()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateWithContent(new byte[] { 1, 2, 3 });
            SequencePosition? result = buffer.PositionOf((byte)4);

            Assert.Null(result);
        }

        [Fact]
        public void CopyTo_ThrowsWhenSourceLargerThenDestination()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(10);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Span<byte> span = new byte[5];
                buffer.CopyTo(span);
            });
        }

        [Fact]
        public void End_EqualToGetPositionSize()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(5);
            Assert.Equal(buffer.End, buffer.GetPosition(5));
        }

        [Fact]
        public void Start_EqualToGetPosition0()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(5);
            Assert.Equal(buffer.Start, buffer.GetPosition(0));
        }

        [Fact]
        public void InnerPositionAreNotEqualToEnd()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(3);
            Assert.NotEqual(buffer.GetPosition(1), buffer.End);
            Assert.NotEqual(buffer.GetPosition(2), buffer.End);
        }

        [Fact]
        public void InnerPositionAreNotEqualToStart()
        {
            ReadOnlySequence<byte> buffer = Factory.CreateOfSize(3);
            Assert.NotEqual(buffer.GetPosition(1), buffer.Start);
            Assert.NotEqual(buffer.GetPosition(2), buffer.Start);
        }

        public static TheoryData<Func<ReadOnlySequence<byte>, ReadOnlySequence<byte>>> ValidSliceCases => new TheoryData<Func<ReadOnlySequence<byte>, ReadOnlySequence<byte>>>
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

        public static TheoryData<Action<ReadOnlySequence<byte>>> OutOfRangeSliceCases => new TheoryData<Action<ReadOnlySequence<byte>>>
        {
            // negative start
            b => b.Slice(-1), // no length
            b => b.Slice(-1, -1), // negative length
            b => b.Slice(-1, 0), // zero length
            b => b.Slice(-1, 1), // positive length
            b => b.Slice(-1, 101), // after end length
            b => b.Slice(-1, b.Start), // to start
            b => b.Slice(-1, b.End), // to end

            // zero start
            b => b.Slice(0, -1), // negative length
            b => b.Slice(0, 101), // after end length

            // end start
            b => b.Slice(100, -1), // negative length
            b => b.Slice(100, 1), // after end length
            b => b.Slice(100, b.Start), // to start

            // After end start
            b => b.Slice(101), // no length
            b => b.Slice(101, -1), // negative length
            b => b.Slice(101, 0), // zero length
            b => b.Slice(101, 1), // after end length
            b => b.Slice(101, b.Start), // to start
            b => b.Slice(101, b.End), // to end

            // At Start start
            b => b.Slice(b.Start, -1), // negative length
            b => b.Slice(b.Start, 101), // after end length

            // At End start
            b => b.Slice(b.End, -1), // negative length
            b => b.Slice(b.End, 1), // after end length
            b => b.Slice(b.End, b.Start), // to start

            // Slice at begin
            b => b.Slice(0, 70).Slice(0, b.End), // to after end
            b => b.Slice(0, 70).Slice(b.Start, b.End), // to after end
            // from after end
            b => b.Slice(0, 70).Slice(b.End),
            b => b.Slice(0, 70).Slice(b.End, -1), // negative length
            b => b.Slice(0, 70).Slice(b.End, 0), // zero length
            b => b.Slice(0, 70).Slice(b.End, 1), // after end length
            b => b.Slice(0, 70).Slice(b.End, b.Start), // to start
            b => b.Slice(0, 70).Slice(b.End, b.End), // to after end

            // Slice at begin
            b => b.Slice(b.Start, 70).Slice(0, b.End), // to after end
            b => b.Slice(b.Start, 70).Slice(b.Start, b.End), // to after end
            // from after end
            b => b.Slice(b.Start, 70).Slice(b.End),
            b => b.Slice(b.Start, 70).Slice(b.End, -1), // negative length
            b => b.Slice(b.Start, 70).Slice(b.End, 0), // zero length
            b => b.Slice(b.Start, 70).Slice(b.End, 1), // after end length
            b => b.Slice(b.Start, 70).Slice(b.End, b.Start), // to start
            b => b.Slice(b.Start, 70).Slice(b.End, b.End), // to after end

            // Slice at middle
            b => b.Slice(30, 40).Slice(0, b.Start), // to before start
            b => b.Slice(30, 40).Slice(0, b.End), // to after end
            // from before start
            b => b.Slice(30, 40).Slice(b.Start),
            b => b.Slice(30, 40).Slice(b.Start, -1), // negative length
            b => b.Slice(30, 40).Slice(b.Start, 0), // zero length
            b => b.Slice(30, 40).Slice(b.Start, 1), // positive length
            b => b.Slice(30, 40).Slice(b.Start, 41), // after end length
            b => b.Slice(30, 40).Slice(b.Start, b.Start), // to before start
            b => b.Slice(30, 40).Slice(b.Start, b.End), // to after end
            // from after end
            b => b.Slice(30, 40).Slice(b.End),
            b => b.Slice(b.Start, 70).Slice(b.End, -1), // negative length
            b => b.Slice(b.Start, 70).Slice(b.End, 0), // zero length
            b => b.Slice(b.Start, 70).Slice(b.End, 1), // after end length
            b => b.Slice(30, 40).Slice(b.End, b.Start), // to before start
            b => b.Slice(30, 40).Slice(b.End, b.End), // to after end

            // Slice at end
            b => b.Slice(70, 30).Slice(0, b.Start), // to before start
            // from before start
            b => b.Slice(30, 40).Slice(b.Start),
            b => b.Slice(30, 40).Slice(b.Start, -1), // negative length
            b => b.Slice(30, 40).Slice(b.Start, 0), // zero length
            b => b.Slice(30, 40).Slice(b.Start, 1), // positive length
            b => b.Slice(30, 40).Slice(b.Start, 31), // after end length
            b => b.Slice(30, 40).Slice(b.Start, b.Start), // to before start
            b => b.Slice(30, 40).Slice(b.Start, b.End), // to end
            // from end
            b => b.Slice(70, 30).Slice(b.End, b.Start), // to before start

            // Slice at end
            b => b.Slice(70, 30).Slice(0, b.Start), // to before start
            // from before start
            b => b.Slice(30, 40).Slice(b.Start),
            b => b.Slice(30, 40).Slice(b.Start, -1), // negative length
            b => b.Slice(30, 40).Slice(b.Start, 0), // zero length
            b => b.Slice(30, 40).Slice(b.Start, 1), // positive length
            b => b.Slice(30, 40).Slice(b.Start, 31), // after end length
            b => b.Slice(30, 40).Slice(b.Start, b.Start), // to before start
            b => b.Slice(30, 40).Slice(b.Start, b.End), // to end
            // from end
            b => b.Slice(70, 30).Slice(b.End, b.Start), // to before start
        };
    }
}
