// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.Memory.Tests.SequenceReader
{
    public class ArrayByte : SingleSegment<byte>
    {
        public ArrayByte() : base(ReadOnlySequenceFactory<byte>.ArrayFactory, s_byteInputData) { }
    }

    public class ArrayChar : SingleSegment<char>
    {
        public ArrayChar() : base(ReadOnlySequenceFactory<char>.ArrayFactory, s_charInputData) { }
    }

    public class MemoryByte : SingleSegment<byte>
    {
        public MemoryByte() : base(ReadOnlySequenceFactory<byte>.MemoryFactory, s_byteInputData) { }
    }

    public class MemoryChar : SingleSegment<char>
    {
        public MemoryChar() : base(ReadOnlySequenceFactory<char>.MemoryFactory, s_charInputData) { }
    }

    public class SingleSegmentByte : SingleSegment<byte>
    {
        public SingleSegmentByte() : base(s_byteInputData) { }
    }

    public class SingleSegmentChar : SingleSegment<char>
    {
        public SingleSegmentChar() : base(s_charInputData) { }
    }

    public abstract class SingleSegment<T> : ReaderBasicTests<T> where T : unmanaged, IEquatable<T>
    {
        public SingleSegment(T[] inputData) : base(ReadOnlySequenceFactory<T>.SingleSegmentFactory, inputData) { }
        internal SingleSegment(ReadOnlySequenceFactory<T> factory, T[] inputData) : base(factory, inputData) { }

        [Fact]
        public void AdvanceSingleBufferSkipsValues()
        {
            SequenceReader<T> reader = new SequenceReader<T>(SequenceFactory.Create(GetInputData(5)));
            Assert.Equal(5, reader.Length);
            Assert.Equal(5, reader.Remaining);
            Assert.Equal(0, reader.Consumed);
            Assert.Equal(0, reader.CurrentSpanIndex);

            // Advance 2 positions
            reader.Advance(2);
            Assert.Equal(5, reader.Length);
            Assert.Equal(3, reader.Remaining);
            Assert.Equal(2, reader.Consumed);
            Assert.Equal(2, reader.CurrentSpanIndex);
            Assert.Equal(InputData[2], reader.CurrentSpan[reader.CurrentSpanIndex]);
            Assert.True(reader.TryPeek(out T value));
            Assert.Equal(InputData[2], value);

            // Advance 2 positions
            reader.Advance(2);
            Assert.Equal(1, reader.Remaining);
            Assert.Equal(4, reader.Consumed);
            Assert.Equal(4, reader.CurrentSpanIndex);
            Assert.Equal(InputData[4], reader.CurrentSpan[reader.CurrentSpanIndex]);
            Assert.True(reader.TryPeek(out value));
            Assert.Equal(InputData[4], value);
        }

        [Fact]
        public void TryReadReturnsValueAndAdvances()
        {
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(GetInputData(2)));
            Assert.Equal(2, reader.Length);
            Assert.Equal(2, reader.Remaining);
            Assert.Equal(0, reader.Consumed);
            Assert.Equal(0, reader.CurrentSpanIndex);
            Assert.Equal(InputData[0], reader.CurrentSpan[reader.CurrentSpanIndex]);

            // Read 1st value
            Assert.True(reader.TryRead(out T value));
            Assert.Equal(InputData[0], value);
            Assert.Equal(1, reader.Remaining);
            Assert.Equal(1, reader.Consumed);
            Assert.Equal(1, reader.CurrentSpanIndex);
            Assert.Equal(InputData[1], reader.CurrentSpan[reader.CurrentSpanIndex]);

            // Read 2nd value
            Assert.True(reader.TryRead(out value));
            Assert.Equal(InputData[1], value);
            Assert.Equal(0, reader.Remaining);
            Assert.Equal(2, reader.Consumed);
            Assert.Equal(2, reader.CurrentSpanIndex);

            // Read at end
            Assert.False(reader.TryRead(out value));
            Assert.Equal(default, value);
            Assert.Equal(0, reader.Remaining);
            Assert.Equal(2, reader.Consumed);
            Assert.Equal(2, reader.CurrentSpanIndex);
            Assert.True(reader.End);
        }

        [Fact]
        public void DefaultState()
        {
            T[] array = new T[] { default };
            SequenceReader<T> reader = default;
            Assert.Equal(0, reader.CurrentSpan.Length);
            Assert.Equal(0, reader.UnreadSpan.Length);
            Assert.Equal(0, reader.UnreadSequence.Length);
            Assert.Equal(0, reader.Consumed);
            Assert.Equal(0, reader.CurrentSpanIndex);
            Assert.Equal(0, reader.Length);
            Assert.Equal(0, reader.Remaining);
            Assert.True(reader.End);
            Assert.False(reader.TryPeek(out T value));
            Assert.Equal(default, value);
            Assert.False(reader.TryRead(out value));
            Assert.Equal(default, value);
            Assert.Equal(0, reader.AdvancePast(default));
            Assert.Equal(0, reader.AdvancePastAny(array));
            Assert.Equal(0, reader.AdvancePastAny(default));
            Assert.False(reader.TryReadTo(out ReadOnlySequence<T> sequence, default(T)));
            Assert.True(sequence.IsEmpty);
            Assert.False(reader.TryReadTo(out sequence, array));
            Assert.True(sequence.IsEmpty);
            Assert.False(reader.TryReadTo(out ReadOnlySpan<T> span, default(T)));
            Assert.True(span.IsEmpty);
            Assert.False(reader.TryReadTo(out span, array));
            Assert.True(span.IsEmpty);
            Assert.False(reader.TryReadToAny(out sequence, array));
            Assert.True(sequence.IsEmpty);
            Assert.False(reader.TryReadToAny(out span, array));
            Assert.True(span.IsEmpty);
            Assert.False(reader.TryAdvanceTo(default));
            Assert.False(reader.TryAdvanceToAny(array));
            Assert.Equal(0, reader.CurrentSpan.Length);
            Assert.Equal(0, reader.UnreadSpan.Length);
            Assert.Equal(0, reader.UnreadSequence.Length);
            Assert.Equal(0, reader.Consumed);
            Assert.Equal(0, reader.CurrentSpanIndex);
            Assert.Equal(0, reader.Length);
            Assert.Equal(0, reader.Remaining);
        }
    }

    public class SegmentPerByte : ReaderBasicTests<byte>
    {
        public SegmentPerByte() : base(ReadOnlySequenceFactory<byte>.SegmentPerItemFactory, s_byteInputData) { }
    }

    public class SegmentPerChar : ReaderBasicTests<char>
    {
        public SegmentPerChar() : base(ReadOnlySequenceFactory<char>.SegmentPerItemFactory, s_charInputData) { }
    }

    public abstract class ReaderBasicTests<T> where T : unmanaged, IEquatable<T>
    {
        internal static byte[] s_byteInputData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        internal static char[] s_charInputData = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A' };
        private T[] _inputData;

        internal ReadOnlySequenceFactory<T> Factory { get; }
        protected ReadOnlySpan<T> InputData { get => _inputData; }

        public T[] GetInputData(int count) => InputData.Slice(0, count).ToArray();

        internal ReaderBasicTests(ReadOnlySequenceFactory<T> factory, T[] inputData)
        {
            Factory = factory;
            _inputData = inputData;
        }

        [Fact]
        public void TryPeekReturnsWithoutMoving()
        {
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(GetInputData(2)));
            Assert.Equal(0, reader.Consumed);
            Assert.Equal(2, reader.Remaining);
            Assert.True(reader.TryPeek(out T value));
            Assert.Equal(InputData[0], value);
            Assert.Equal(0, reader.Consumed);
            Assert.Equal(2, reader.Remaining);
            Assert.True(reader.TryPeek(out value));
            Assert.Equal(InputData[0], value);
            Assert.Equal(0, reader.Consumed);
            Assert.Equal(2, reader.Remaining);
        }

        [Fact]
        public void TryPeekOffset()
        {
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(GetInputData(10)));
            Assert.True(reader.TryRead(out T first));
            Assert.Equal(InputData[0], first);
            Assert.True(reader.TryRead(out T second));
            Assert.Equal(InputData[1], second);

            Assert.True(reader.TryPeek(7, out T value));
            Assert.Equal(InputData[9], value);

            Assert.False(reader.TryPeek(8, out T defaultValue));
            Assert.Equal(default, defaultValue);

            Assert.Equal(2, reader.Consumed);
            Assert.Equal(8, reader.Remaining);
        }

        [Fact]
        public void TryPeekOffset_AfterEnd()
        {
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(GetInputData(2)));
            Assert.True(reader.TryRead(out T first));
            Assert.Equal(InputData[0], first);

            Assert.True(reader.TryPeek(0, out T value));
            Assert.Equal(InputData[1], value);
            Assert.Equal(1, reader.Remaining);

            Assert.False(reader.TryPeek(1, out T defaultValue));
            Assert.Equal(default, defaultValue);
        }

        [Fact]
        public void TryPeekOffset_RemainsZeroOffsetZero()
        {
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(GetInputData(1)));
            Assert.True(reader.TryRead(out T first));
            Assert.Equal(InputData[0], first);
            Assert.Equal(0, reader.Remaining);
            Assert.False(reader.TryPeek(0, out T defaultValue));
            Assert.Equal(default, defaultValue);
        }

        [Fact]
        public void TryPeekOffset_Empty()
        {
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(GetInputData(0)));
            Assert.False(reader.TryPeek(0, out T defaultValue));
            Assert.Equal(default, defaultValue);
        }

        [Fact]
        public void TryPeekOffset_MultiSegment_StarAhead()
        {
            ReadOnlySpan<T> data = (T[])_inputData.Clone();

            SequenceSegment<T> last = new SequenceSegment<T>();
            last.SetMemory(new OwnedArray<T>(data.Slice(5).ToArray()), 0, 5);

            SequenceSegment<T> first = new SequenceSegment<T>();
            first.SetMemory(new OwnedArray<T>(data.Slice(0, 5).ToArray()), 0, 5);
            first.SetNext(last);

            ReadOnlySequence<T> sequence = new ReadOnlySequence<T>(first, first.Start, last, last.End);
            SequenceReader<T> reader = new SequenceReader<T>(sequence);

            // Move by 2 element
            for (int i = 0; i < 2; i++)
            {
                Assert.True(reader.TryRead(out T val));
                Assert.Equal(InputData[i], val);
            }

            // We're on element 3 we peek last element of first segment
            Assert.True(reader.TryPeek(2, out T lastElementFirstSegment));
            Assert.Equal(InputData[4], lastElementFirstSegment);

            // We're on element 3 we peek first element of first segment
            Assert.True(reader.TryPeek(3, out T fistElementSecondSegment));
            Assert.Equal(InputData[5], fistElementSecondSegment);

            // We're on element 3 we peek last element of second segment
            Assert.True(reader.TryPeek(7, out T lastElementSecondSegment));
            Assert.Equal(InputData[9], lastElementSecondSegment);

            // 3 + 8 out of bounds
            Assert.False(reader.TryPeek(8, out T defaultValue));
            Assert.Equal(default, defaultValue);

            Assert.Equal(2, reader.Consumed);
            Assert.Equal(8, reader.Remaining);
        }

        [Fact]
        public void TryPeekOffset_MultiSegment_GetFirstGetLast()
        {
            ReadOnlySpan<T> data = (T[])_inputData.Clone();

            SequenceSegment<T> last = new SequenceSegment<T>();
            last.SetMemory(new OwnedArray<T>(data.Slice(5).ToArray()), 0, 5);

            SequenceSegment<T> first = new SequenceSegment<T>();
            first.SetMemory(new OwnedArray<T>(data.Slice(0, 5).ToArray()), 0, 5);
            first.SetNext(last);

            ReadOnlySequence<T> sequence = new ReadOnlySequence<T>(first, first.Start, last, last.End);
            SequenceReader<T> reader = new SequenceReader<T>(sequence);

            Assert.True(reader.TryPeek(0, out T firstElement));
            Assert.Equal(InputData[0], firstElement);

            Assert.True(reader.TryPeek(data.Length - 1, out T lastElemen));
            Assert.Equal(InputData[data.Length - 1], lastElemen);

            Assert.Equal(0, reader.Consumed);
            Assert.Equal(10, reader.Remaining);
        }

        [Fact]
        public void TryPeekOffset_InvalidOffset()
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(GetInputData(10)));
                reader.TryPeek(-1, out _);
            });

            Assert.Equal("offset", exception.ParamName);
        }

        [Fact]
        public void CursorIsCorrectAtEnd()
        {
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(GetInputData(2)));
            reader.TryRead(out T _);
            reader.TryRead(out T _);
            Assert.True(reader.End);
        }

        [Fact]
        public void CursorIsCorrectWithEmptyLastBlock()
        {
            SequenceSegment<T> last = new SequenceSegment<T>();
            last.SetMemory(new OwnedArray<T>(new T[4]), 0, 4);

            SequenceSegment<T> first = new SequenceSegment<T>();
            first.SetMemory(new OwnedArray<T>(GetInputData(2)), 0, 2);
            first.SetNext(last);

            SequenceReader<T> reader = new SequenceReader<T>(new ReadOnlySequence<T>(first, first.Start, last, last.Start));
            reader.TryRead(out T _);
            reader.TryRead(out T _);
            reader.TryRead(out T _);
            Assert.Same(last, reader.Position.GetObject());
            Assert.Equal(0, reader.Position.GetInteger());
            Assert.True(reader.End);
        }

        [Fact]
        public void TryPeekReturnsDefaultInTheEnd()
        {
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(GetInputData(2)));
            Assert.True(reader.TryRead(out T value));
            Assert.Equal(InputData[0], value);
            Assert.True(reader.TryRead(out value));
            Assert.Equal(InputData[1], value);
            Assert.False(reader.TryRead(out value));
            Assert.Equal(default, value);
        }

        [Fact]
        public void AdvanceToEndThenPeekReturnsDefault()
        {
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(GetInputData(5)));
            reader.Advance(5);
            Assert.True(reader.End);
            Assert.False(reader.TryPeek(out T value));
            Assert.Equal(default, value);
        }

        [Fact]
        public void AdvancingPastLengthThrows()
        {
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(GetInputData(5)));
            try
            {
                reader.Advance(6);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.True(ex is ArgumentOutOfRangeException);
                Assert.Equal(5, reader.Consumed);
                Assert.Equal(0, reader.Remaining);
                Assert.True(reader.End);
            }
        }

        [Fact]
        public void CtorFindsFirstNonEmptySegment()
        {
            ReadOnlySequence<T> buffer = Factory.CreateWithContent(GetInputData(1));
            SequenceReader<T> reader = new SequenceReader<T>(buffer);

            Assert.True(reader.TryPeek(out T value));
            Assert.Equal(InputData[0], value);
            Assert.Equal(0, reader.Consumed);
            Assert.Equal(1, reader.Remaining);
        }

        [Fact]
        public void EmptySegmentsAreSkippedOnMoveNext()
        {
            ReadOnlySequence<T> buffer = Factory.CreateWithContent(GetInputData(2));
            SequenceReader<T> reader = new SequenceReader<T>(buffer);

            Assert.True(reader.TryPeek(out T value));
            Assert.Equal(InputData[0], value);
            reader.Advance(1);
            Assert.True(reader.TryPeek(out value));
            Assert.Equal(InputData[1], value);
        }

        [Fact]
        public void TryPeekGoesToEndIfAllEmptySegments()
        {
            ReadOnlySequence<T> buffer = SequenceFactory.Create(new[] { new T[] { }, new T[] { }, new T[] { }, new T[] { } });
            SequenceReader<T> reader = new SequenceReader<T>(buffer);

            Assert.False(reader.TryPeek(out T value));
            Assert.Equal(default, value);
            Assert.True(reader.End);
        }

        [Fact]
        public void AdvanceTraversesSegments()
        {
            ReadOnlySequence<T> buffer = Factory.CreateWithContent(GetInputData(3));
            SequenceReader<T> reader = new SequenceReader<T>(buffer);

            reader.Advance(2);
            Assert.Equal(InputData[2], reader.CurrentSpan[reader.CurrentSpanIndex]);
            Assert.True(reader.TryRead(out T value));
            Assert.Equal(InputData[2], value);
        }

        [Fact]
        public void AdvanceThrowsPastLengthMultipleSegments()
        {
            ReadOnlySequence<T> buffer = Factory.CreateWithContent(GetInputData(3));
            SequenceReader<T> reader = new SequenceReader<T>(buffer);

            try
            {
                reader.Advance(4);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.True(ex is ArgumentOutOfRangeException);
                Assert.Equal(3, reader.Consumed);
                Assert.Equal(0, reader.Remaining);
                Assert.True(reader.End);
            }
        }

        [Fact]
        public void TryReadTraversesSegments()
        {
            ReadOnlySequence<T> buffer = Factory.CreateWithContent(GetInputData(3));
            SequenceReader<T> reader = new SequenceReader<T>(buffer);

            Assert.True(reader.TryRead(out T value));
            Assert.Equal(InputData[0], value);
            Assert.True(reader.TryRead(out value));
            Assert.Equal(InputData[1], value);
            Assert.True(reader.TryRead(out value));
            Assert.Equal(InputData[2], value);
            Assert.False(reader.TryRead(out value));
            Assert.Equal(default, value);
            Assert.True(reader.End);
        }

        [Fact]
        public void TryPeekTraversesSegments()
        {
            ReadOnlySequence<T> buffer = Factory.CreateWithContent(GetInputData(2));
            SequenceReader<T> reader = new SequenceReader<T>(buffer);

            Assert.Equal(InputData[0], reader.CurrentSpan[reader.CurrentSpanIndex]);
            Assert.True(reader.TryRead(out T value));
            Assert.Equal(InputData[0], value);

            Assert.Equal(InputData[1], reader.CurrentSpan[reader.CurrentSpanIndex]);
            Assert.True(reader.TryPeek(out value));
            Assert.Equal(InputData[1], value);
            Assert.True(reader.TryRead(out value));
            Assert.Equal(InputData[1], value);
            Assert.False(reader.TryPeek(out value));
            Assert.False(reader.TryRead(out value));
            Assert.Equal(default, value);
            Assert.Equal(default, value);
        }

        [Fact]
        public void PeekWorkesWithEmptySegments()
        {
            ReadOnlySequence<T> buffer = Factory.CreateWithContent(GetInputData(1));
            SequenceReader<T> reader = new SequenceReader<T>(buffer);

            Assert.Equal(0, reader.CurrentSpanIndex);
            Assert.Equal(1, reader.CurrentSpan.Length);
            Assert.True(reader.TryPeek(out T value));
            Assert.Equal(InputData[0], value);
            Assert.True(reader.TryRead(out value));
            Assert.Equal(InputData[0], value);
            Assert.False(reader.TryPeek(out value));
            Assert.False(reader.TryRead(out value));
            Assert.Equal(default, value);
            Assert.Equal(default, value);
        }

        [Fact]
        public void WorksWithEmptyBuffer()
        {
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(new T[] { }));

            Assert.Equal(0, reader.CurrentSpanIndex);
            Assert.Equal(0, reader.CurrentSpan.Length);
            Assert.Equal(0, reader.Length);
            Assert.Equal(0, reader.Remaining);
            Assert.True(reader.End);
            Assert.False(reader.TryPeek(out T value));
            Assert.Equal(default, value);
            Assert.False(reader.TryRead(out value));
            Assert.Equal(default, value);
            Assert.True(reader.End);
        }

        [Theory,
            InlineData(0, false),
            InlineData(5, false),
            InlineData(10, false),
            InlineData(11, true),
            InlineData(12, true),
            InlineData(15, true)]
        public void ReturnsCorrectCursor(int takes, bool end)
        {
            ReadOnlySequence<T> readableBuffer = Factory.CreateWithContent(GetInputData(10));
            SequenceReader<T> reader = new SequenceReader<T>(readableBuffer);
            for (int i = 0; i < takes; i++)
            {
                reader.TryRead(out _);
            }

            T[] expected = end ? new T[] { } : readableBuffer.Slice(takes).ToArray();
            Assert.Equal(expected, readableBuffer.Slice(reader.Position).ToArray());
        }

        [Fact]
        public void SlicingBufferReturnsCorrectCursor()
        {
            ReadOnlySequence<T> buffer = Factory.CreateWithContent(GetInputData(10));
            ReadOnlySequence<T> sliced = buffer.Slice(2L);

            SequenceReader<T> reader = new SequenceReader<T>(sliced);
            Assert.Equal(sliced.ToArray(), buffer.Slice(reader.Position).ToArray());
            Assert.True(reader.TryPeek(out T value));
            Assert.Equal(InputData[2], value);
            Assert.Equal(0, reader.CurrentSpanIndex);
        }

        [Fact]
        public void ReaderIndexIsCorrect()
        {
            ReadOnlySequence<T> buffer = Factory.CreateWithContent(GetInputData(10));
            SequenceReader<T> reader = new SequenceReader<T>(buffer);

            int counter = 0;
            while (!reader.End)
            {
                ReadOnlySpan<T> span = reader.CurrentSpan;
                for (int i = reader.CurrentSpanIndex; i < span.Length; i++)
                {
                    Assert.Equal(InputData[counter++], reader.CurrentSpan[i]);
                }
                reader.Advance(span.Length);
            }
            Assert.Equal(buffer.Length, reader.Consumed);
        }

        [Theory,
            InlineData(1),
            InlineData(2),
            InlineData(3)]
        public void Advance_PositionIsCorrect(int advanceBy)
        {
            // Check that advancing through the reader gives the same position
            // as returned directly from the buffer.

            ReadOnlySequence<T> buffer = Factory.CreateWithContent(GetInputData(10));
            SequenceReader<T> reader = new SequenceReader<T>(buffer);

            SequencePosition readerPosition = reader.Position;
            SequencePosition bufferPosition = buffer.GetPosition(0);
            Assert.Equal(readerPosition.GetInteger(), bufferPosition.GetInteger());
            Assert.Same(readerPosition.GetObject(), readerPosition.GetObject());

            for (int i = advanceBy; i <= buffer.Length; i += advanceBy)
            {
                reader.Advance(advanceBy);
                readerPosition = reader.Position;
                bufferPosition = buffer.GetPosition(i);
                Assert.Equal(readerPosition.GetInteger(), bufferPosition.GetInteger());
                Assert.Same(readerPosition.GetObject(), readerPosition.GetObject());
            }
        }

        [Fact]
        public void AdvanceTo()
        {
            // Ensure we can advance to each of the items in the buffer

            T[] inputData = GetInputData(10);
            ReadOnlySequence<T> buffer = Factory.CreateWithContent(inputData);

            for (int i = 0; i < buffer.Length; i++)
            {
                SequenceReader<T> reader = new SequenceReader<T>(buffer);
                Assert.True(reader.TryAdvanceTo(inputData[i], advancePastDelimiter: false));
                Assert.True(reader.TryPeek(out T value));
                Assert.Equal(inputData[i], value);
            }
        }

        [Fact]
        public void AdvanceTo_AdvancePast()
        {
            // Ensure we can advance to each of the items in the buffer (skipping what we advanced to)

            T[] inputData = GetInputData(10);
            ReadOnlySequence<T> buffer = Factory.CreateWithContent(inputData);

            for (int start = 0; start < 2; start++)
            {
                for (int i = start; i < buffer.Length - 1; i += 2)
                {
                    SequenceReader<T> reader = new SequenceReader<T>(buffer);
                    Assert.True(reader.TryAdvanceTo(inputData[i], advancePastDelimiter: true));
                    Assert.True(reader.TryPeek(out T value));
                    Assert.Equal(inputData[i + 1], value);
                }
            }
        }

        [Fact]
        public void AdvanceTo_End()
        {
            ReadOnlySpan<T> data = (T[])_inputData.Clone();

            SequenceSegment<T> last = new SequenceSegment<T>();
            last.SetMemory(new OwnedArray<T>(data.Slice(5).ToArray()), 0, 5);

            SequenceSegment<T> first = new SequenceSegment<T>();
            first.SetMemory(new OwnedArray<T>(data.Slice(0, 5).ToArray()), 0, 5);
            first.SetNext(last);

            ReadOnlySequence<T> sequence = new ReadOnlySequence<T>(first, first.Start, last, last.End);
            SequenceReader<T> reader = new SequenceReader<T>(sequence);

            reader.AdvanceToEnd();

            Assert.Equal(data.Length, reader.Length);
            Assert.Equal(data.Length, reader.Consumed);
            Assert.Equal(reader.Length, reader.Consumed);
            Assert.True(reader.End);
            Assert.Equal(0, reader.CurrentSpanIndex);
            Assert.Equal(sequence.End, reader.Position);
            Assert.Equal(0, reader.Remaining);
            Assert.True(default == reader.UnreadSpan);
            Assert.True(default == reader.CurrentSpan);
        }

        [Fact]
        public void AdvanceTo_End_EmptySegment()
        {
            ReadOnlySpan<T> data = (T[])_inputData.Clone();

            // Empty segment
            SequenceSegment<T> third = new SequenceSegment<T>();

            SequenceSegment<T> second = new SequenceSegment<T>();
            second.SetMemory(new OwnedArray<T>(data.Slice(5).ToArray()), 0, 5);
            second.SetNext(third);

            SequenceSegment<T> first = new SequenceSegment<T>();
            first.SetMemory(new OwnedArray<T>(data.Slice(0, 5).ToArray()), 0, 5);
            first.SetNext(second);

            ReadOnlySequence<T> sequence = new ReadOnlySequence<T>(first, first.Start, third, third.End);
            SequenceReader<T> reader = new SequenceReader<T>(sequence);

            reader.AdvanceToEnd();

            Assert.Equal(first.Length + second.Length, reader.Length);
            Assert.Equal(first.Length + second.Length, reader.Consumed);
            Assert.Equal(reader.Length, reader.Consumed);
            Assert.True(reader.End);
            Assert.Equal(0, reader.CurrentSpanIndex);
            Assert.Equal(sequence.End, reader.Position);
            Assert.Equal(0, reader.Remaining);
            Assert.True(default == reader.UnreadSpan);
            Assert.True(default == reader.CurrentSpan);
        }

        [Fact]
        public void AdvanceTo_End_Rewind_Advance()
        {
            ReadOnlySpan<T> data = (T[])_inputData.Clone();

            SequenceSegment<T> last = new SequenceSegment<T>();
            last.SetMemory(new OwnedArray<T>(data.Slice(5).ToArray()), 0, 5);

            SequenceSegment<T> first = new SequenceSegment<T>();
            first.SetMemory(new OwnedArray<T>(data.Slice(0, 5).ToArray()), 0, 5);
            first.SetNext(last);

            ReadOnlySequence<T> sequence = new ReadOnlySequence<T>(first, first.Start, last, last.End);
            SequenceReader<T> reader = new SequenceReader<T>(sequence);

            reader.AdvanceToEnd();

            Assert.Equal(data.Length, reader.Length);
            Assert.Equal(data.Length, reader.Consumed);
            Assert.Equal(reader.Length, reader.Consumed);
            Assert.True(reader.End);
            Assert.Equal(0, reader.CurrentSpanIndex);
            Assert.Equal(sequence.End, reader.Position);
            Assert.Equal(0, reader.Remaining);
            Assert.True(default == reader.UnreadSpan);
            Assert.True(default == reader.CurrentSpan);

            // Rewind to second element
            reader.Rewind(9);

            Assert.Equal(1, reader.Consumed);
            Assert.False(reader.End);
            Assert.Equal(1, reader.CurrentSpanIndex);
            Assert.Equal(9, reader.Remaining);
            Assert.Equal(sequence.Slice(1), reader.UnreadSequence);

            // Consume next five elements and stop at second element of second segment
            reader.Advance(5);

            Assert.Equal(6, reader.Consumed);
            Assert.False(reader.End);
            Assert.Equal(1, reader.CurrentSpanIndex);
            Assert.Equal(4, reader.Remaining);
            Assert.Equal(sequence.Slice(6), reader.UnreadSequence);

            reader.AdvanceToEnd();

            Assert.Equal(data.Length, reader.Length);
            Assert.Equal(data.Length, reader.Consumed);
            Assert.Equal(reader.Length, reader.Consumed);
            Assert.True(reader.End);
            Assert.Equal(0, reader.CurrentSpanIndex);
            Assert.Equal(sequence.End, reader.Position);
            Assert.Equal(0, reader.Remaining);
            Assert.True(default == reader.UnreadSpan);
            Assert.True(default == reader.CurrentSpan);
        }

        [Fact]
        public void AdvanceTo_End_Multiple()
        {
            ReadOnlySpan<T> data = (T[])_inputData.Clone();

            SequenceSegment<T> last = new SequenceSegment<T>();
            last.SetMemory(new OwnedArray<T>(data.Slice(5).ToArray()), 0, 5);

            SequenceSegment<T> first = new SequenceSegment<T>();
            first.SetMemory(new OwnedArray<T>(data.Slice(0, 5).ToArray()), 0, 5);
            first.SetNext(last);

            ReadOnlySequence<T> sequence = new ReadOnlySequence<T>(first, first.Start, last, last.End);
            SequenceReader<T> reader = new SequenceReader<T>(sequence);

            reader.AdvanceToEnd();
            reader.AdvanceToEnd();
            reader.AdvanceToEnd();

            Assert.Equal(data.Length, reader.Length);
            Assert.Equal(data.Length, reader.Consumed);
            Assert.Equal(reader.Length, reader.Consumed);
            Assert.True(reader.End);
            Assert.Equal(0, reader.CurrentSpanIndex);
            Assert.Equal(sequence.End, reader.Position);
            Assert.Equal(0, reader.Remaining);
            Assert.True(default == reader.UnreadSpan);
            Assert.True(default == reader.CurrentSpan);
        }

        [Fact]
        public void UnreadSequence()
        {
            ReadOnlySpan<T> data = (T[])_inputData.Clone();

            SequenceSegment<T> last = new SequenceSegment<T>();
            last.SetMemory(new OwnedArray<T>(data.Slice(5).ToArray()), 0, 5);

            SequenceSegment<T> first = new SequenceSegment<T>();
            first.SetMemory(new OwnedArray<T>(data.Slice(0, 5).ToArray()), 0, 5);
            first.SetNext(last);

            ReadOnlySequence<T> sequence = new ReadOnlySequence<T>(first, first.Start, last, last.End);
            SequenceReader<T> reader = new SequenceReader<T>(sequence);

            Assert.Equal(sequence, reader.UnreadSequence);
            Assert.Equal(data.Length, reader.UnreadSequence.Length);
            Assert.True(reader.TryRead(out T _));
            Assert.True(reader.TryRead(out T _));
            Assert.Equal(sequence.Slice(2), reader.UnreadSequence);
            // Advance to the end
            reader.Advance(8);
            Assert.Equal(0, reader.UnreadSequence.Length);
        }

        [Fact]
        public void UnreadSequence_EmptySegment()
        {
            ReadOnlySpan<T> data = (T[])_inputData.Clone();

            // Empty segment
            SequenceSegment<T> third = new SequenceSegment<T>();

            SequenceSegment<T> second = new SequenceSegment<T>();
            second.SetMemory(new OwnedArray<T>(data.Slice(5).ToArray()), 0, 5);
            second.SetNext(third);

            SequenceSegment<T> first = new SequenceSegment<T>();
            first.SetMemory(new OwnedArray<T>(data.Slice(0, 5).ToArray()), 0, 5);
            first.SetNext(second);

            ReadOnlySequence<T> sequence = new ReadOnlySequence<T>(first, first.Start, third, third.End);
            SequenceReader<T> reader = new SequenceReader<T>(sequence);

            // Drain until the expected end of data with simple read
            for (int i = 0; i < data.Length; i++)
            {
                reader.TryRead(out T _);
            }

            Assert.Equal(sequence.Slice(data.Length), reader.UnreadSequence);
            Assert.Equal(0, reader.UnreadSequence.Length);
            Assert.False(reader.TryRead(out T _));
        }

        [Fact]
        public void CopyToSmallerBufferWorks()
        {
            T[] content = (T[])_inputData.Clone();

            Span<T> buffer = new T[content.Length];
            SequenceReader<T> reader = new SequenceReader<T>(Factory.CreateWithContent(content));

            // this loop skips more and more items in the reader
            for (int i = 0; i < content.Length; i++)
            {
                // this loop makes the destination buffer smaller and smaller
                for (int j = 0; j < buffer.Length - i; j++)
                {
                    Span<T> bufferSlice = buffer.Slice(0, j);
                    bufferSlice.Clear();
                    Assert.True(reader.TryCopyTo(bufferSlice));
                    Assert.Equal(Math.Min(bufferSlice.Length, content.Length - i), bufferSlice.Length);

                    Assert.True(bufferSlice.SequenceEqual(content.AsSpan(i, j)));
                }

                reader.Advance(1);
            }
        }
    }
}
