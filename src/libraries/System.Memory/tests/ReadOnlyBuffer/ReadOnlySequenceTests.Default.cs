// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.Memory.Tests
{
    public class ReadOnlySequenceTestsDefault
    {
        #region Constructor

        [Fact]
        public void Default_Constructor()
        {
            ReadOnlySequence<byte> buffer = default;
            Assert.Equal(default, buffer.Start);
            Assert.Equal(default, buffer.End);
            Assert.True(buffer.IsEmpty);
            Assert.True(buffer.IsSingleSegment);
            Assert.Equal(0, buffer.Length);
            Assert.True(buffer.First.IsEmpty);
            Assert.True(buffer.FirstSpan.IsEmpty);
            Assert.Equal($"System.Buffers.ReadOnlySequence<{typeof(byte).Name}>[0]", buffer.ToString());
        }

        #endregion

        #region GetPosition

        [Fact]
        public void Default_GetPosition()
        {
            ReadOnlySequence<byte> buffer = default;
            SequencePosition position = default;

            Assert.Equal(position, buffer.GetPosition(0));
            Assert.Equal(position, buffer.GetPosition(0, buffer.Start));
            Assert.Equal(position, buffer.GetPosition(0, buffer.End));
        }

        [Fact]
        public void Default_GetPositionPositive()
        {
            ReadOnlySequence<byte> buffer = default;
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(1));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(1, buffer.Start));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(1, buffer.End));
        }

        [Fact]
        public void Default_GetPositionNegative()
        {
            ReadOnlySequence<byte> buffer = default;
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(-1));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(-1, buffer.Start));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(-1, buffer.End));
        }

        #endregion

        #region Slice

        [Fact]
        public void Default_Slice()
        {
            ReadOnlySequence<byte> buffer = default;
            Assert.Equal(buffer, buffer.Slice(0, 0));
            Assert.Equal(buffer, buffer.Slice(0, buffer.End));
            Assert.Equal(buffer, buffer.Slice(0));
            Assert.Equal(buffer, buffer.Slice(0L, 0L));
            Assert.Equal(buffer, buffer.Slice(0L, buffer.End));
            Assert.Equal(buffer, buffer.Slice(buffer.Start));
            Assert.Equal(buffer, buffer.Slice(buffer.Start, 0));
            Assert.Equal(buffer, buffer.Slice(buffer.Start, 0L));
            Assert.Equal(buffer, buffer.Slice(buffer.Start, buffer.End));
        }

        [Fact]
        public void Default_SlicePositiveStart()
        {
            ReadOnlySequence<byte> buffer = default;
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1, buffer.End));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1L, 0L));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1L, buffer.End));
        }

        [Fact]
        public void Default_SliceNegativeStart()
        {
            ReadOnlySequence<byte> buffer = default;
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1, -1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1, buffer.End));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1L, 0L));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1L, -1L));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1L, buffer.End));
        }

        [Fact]
        public void Default_SlicePositiveLength()
        {
            ReadOnlySequence<byte> buffer = default;
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(0L, 1L));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(buffer.Start, 1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(buffer.Start, 1L));
        }

        [Fact]
        public void Default_SliceNegativeLength()
        {
            ReadOnlySequence<byte> buffer = default;
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(0L, -1L));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(buffer.Start, -1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(buffer.Start, -1L));
        }

        [Fact]
        public void Slice_DefaultSequencePosition()
        {
            var firstSegment = new BufferSegment<byte>(new byte[4]);
            BufferSegment<byte> secondSegment = firstSegment.Append(new byte[4]);

            var sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, firstSegment.Memory.Length);
            ReadOnlySequence<byte> slicedSequence = sequence.Slice(default(SequencePosition));
            Assert.Equal(sequence, slicedSequence);

            // Slice(default, default) should return an empty sequence
            slicedSequence = sequence.Slice(default(SequencePosition), default(SequencePosition));
            Assert.Equal(0, slicedSequence.Length);

            // Slice(x, default) returns empty if x = 0. Otherwise throws
            sequence = sequence.Slice(2);
            slicedSequence = sequence.Slice(0, default(SequencePosition));
            Assert.Equal(0, slicedSequence.Length);

            Assert.Throws<ArgumentOutOfRangeException>(() => sequence.Slice(1, default(SequencePosition)));

            // Slice(default, x) returns sequence from the beginning to x
            slicedSequence = sequence.Slice(default(SequencePosition), 1);
            Assert.Equal(1, slicedSequence.Length);
            Assert.Equal(sequence.Start, slicedSequence.Start);
        }

        #endregion

        #region Enumerator

        [Fact]
        public void Default_Enumerator()
        {
            ReadOnlySequence<byte> buffer = default;
            ReadOnlySequence<byte>.Enumerator enumerator = buffer.GetEnumerator();
            {
                Assert.Equal(default, enumerator.Current);
                Assert.False(enumerator.MoveNext());
            }
            enumerator = new ReadOnlySequence<byte>.Enumerator(default);
            {
                Assert.Equal(default, enumerator.Current);
                Assert.False(enumerator.MoveNext());
            }
        }

        #endregion

        #region TryGet

        [Fact]
        public void Default_TryGet()
        {
            ReadOnlySequence<byte> buffer = default;
            ReadOnlyMemory<byte> memory;

            SequencePosition c1 = buffer.Start;
            Assert.False(buffer.TryGet(ref c1, out memory, false));
            Assert.Null(c1.GetObject());
            Assert.True(memory.IsEmpty);

            Assert.False(buffer.TryGet(ref c1, out memory, true));
            Assert.Null(c1.GetObject());
            Assert.True(memory.IsEmpty);

            Assert.False(buffer.TryGet(ref c1, out memory, false));
            Assert.Null(c1.GetObject());
            Assert.True(memory.IsEmpty);

            Assert.False(buffer.TryGet(ref c1, out memory, true));
            Assert.Null(c1.GetObject());
            Assert.True(memory.IsEmpty);

            c1 = buffer.End;
            Assert.False(buffer.TryGet(ref c1, out memory, false));
            Assert.Null(c1.GetObject());
            Assert.True(memory.IsEmpty);

            Assert.False(buffer.TryGet(ref c1, out memory, true));
            Assert.Null(c1.GetObject());
            Assert.True(memory.IsEmpty);

            Assert.False(buffer.TryGet(ref c1, out memory, false));
            Assert.Null(c1.GetObject());
            Assert.True(memory.IsEmpty);

            Assert.False(buffer.TryGet(ref c1, out memory, true));
            Assert.Null(c1.GetObject());
            Assert.True(memory.IsEmpty);
        }

        #endregion
    }
}
