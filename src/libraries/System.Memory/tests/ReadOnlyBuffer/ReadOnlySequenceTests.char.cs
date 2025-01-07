// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            public Array() : base(ReadOnlySequenceFactory<char>.ArrayFactory) { }
        }

        public class String : ReadOnlySequenceTestsChar
        {
            public String() : base(ReadOnlySequenceFactoryChar.StringFactory) { }
        }

        public class Memory : ReadOnlySequenceTestsChar
        {
            public Memory() : base(ReadOnlySequenceFactory<char>.MemoryFactory) { }
        }

        public class SingleSegment : ReadOnlySequenceTestsChar
        {
            public SingleSegment() : base(ReadOnlySequenceFactory<char>.SingleSegmentFactory) { }
        }

        public class SegmentPerChar : ReadOnlySequenceTestsChar
        {
            public SegmentPerChar() : base(ReadOnlySequenceFactory<char>.SegmentPerItemFactory) { }
        }

        public class SplitInThreeSegments : ReadOnlySequenceTestsChar
        {
            public SplitInThreeSegments() : base(ReadOnlySequenceFactory<char>.SplitInThree) { }
        }

        internal ReadOnlySequenceFactory<char> Factory { get; }

        internal ReadOnlySequenceTestsChar(ReadOnlySequenceFactory<char> factory)
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
            char[] array = Enumerable.Range(0, 255).Select(i => (char)i).ToArray();
            ReadOnlySequence<char> buffer = Factory.CreateWithContent(array);
            Assert.Equal(new string(array), buffer.ToString());
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
            Assert.Equal(35, buffer.Slice(position).Length);
            position = buffer.GetPosition(65, buffer.Start);
            Assert.Equal(35, buffer.Slice(position).Length);
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
            ReadOnlySequence<char> buffer = Factory.CreateWithContent(raw.ToCharArray());
            SequencePosition? result = buffer.PositionOf((char)searchFor);

            Assert.NotNull(result);
            Assert.Equal(buffer.Slice(result.Value).ToArray(), raw.Substring(expectIndex).ToCharArray());
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
