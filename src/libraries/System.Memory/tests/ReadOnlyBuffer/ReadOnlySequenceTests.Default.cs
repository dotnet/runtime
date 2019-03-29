// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Assert.Equal(null, c1.GetObject());
            Assert.True(memory.IsEmpty);
            
            Assert.False(buffer.TryGet(ref c1, out memory, true));
            Assert.Equal(null, c1.GetObject());
            Assert.True(memory.IsEmpty);

            Assert.False(buffer.TryGet(ref c1, out memory, false));
            Assert.Equal(null, c1.GetObject());
            Assert.True(memory.IsEmpty);

            Assert.False(buffer.TryGet(ref c1, out memory, true));
            Assert.Equal(null, c1.GetObject());
            Assert.True(memory.IsEmpty);

            c1 = buffer.End;
            Assert.False(buffer.TryGet(ref c1, out memory, false));
            Assert.Equal(null, c1.GetObject());
            Assert.True(memory.IsEmpty);
            
            Assert.False(buffer.TryGet(ref c1, out memory, true));
            Assert.Equal(null, c1.GetObject());
            Assert.True(memory.IsEmpty);

            Assert.False(buffer.TryGet(ref c1, out memory, false));
            Assert.Equal(null, c1.GetObject());
            Assert.True(memory.IsEmpty);

            Assert.False(buffer.TryGet(ref c1, out memory, true));
            Assert.Equal(null, c1.GetObject());
            Assert.True(memory.IsEmpty);
        }

        #endregion
    }
}
