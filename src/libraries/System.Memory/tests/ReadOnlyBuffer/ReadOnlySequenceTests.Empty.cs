// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Xunit;

namespace System.Memory.Tests
{
    public class ReadOnlySequenceTestsEmpty
    {
        #region Constructor

        [Fact]
        public void Empty_Constructor()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;
            Assert.Equal(buffer.Start.GetObject(), buffer.End.GetObject());
            Assert.Equal(0, buffer.Start.GetInteger() & int.MaxValue);
            Assert.Equal(0, buffer.End.GetInteger() & int.MaxValue);
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
        public void Empty_GetPosition()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;
            SequencePosition position = new SequencePosition(buffer.Start.GetObject(), 0);

            Assert.Equal(position, buffer.GetPosition(0));
            Assert.Equal(position, buffer.GetPosition(0, buffer.Start));
            Assert.Equal(position, buffer.GetPosition(0, buffer.End));
        }

        [Fact]
        public void Empty_GetPositionPositive()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(1));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(1, buffer.Start));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(1, buffer.End));
        }

        [Fact]
        public void Empty_GetPositionNegative()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(-1));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(-1, buffer.Start));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => buffer.GetPosition(-1, buffer.End));
        }

        #endregion

        #region Slice

        [Fact]
        public void Empty_Slice()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;
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
        public void Empty_SlicePositiveStart()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1, buffer.End));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1L, 0L));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(1L, buffer.End));
        }

        [Fact]
        public void Empty_SliceNegativeStart()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1, -1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1, buffer.End));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1L, 0L));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1L, -1L));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => buffer.Slice(-1L, buffer.End));
        }

        [Fact]
        public void Empty_SlicePositiveLength()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(0L, 1L));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(buffer.Start, 1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(buffer.Start, 1L));
        }

        [Fact]
        public void Empty_SliceNegativeLength()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(0L, -1L));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(buffer.Start, -1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => buffer.Slice(buffer.Start, -1L));
        }

        #endregion

        #region Enumerator

        [Fact]
        public void Empty_Enumerator()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;
            ReadOnlySequence<byte>.Enumerator enumerator = buffer.GetEnumerator();
            {
                Assert.Equal(default, enumerator.Current); 
                Assert.True(enumerator.MoveNext());
                ReadOnlyMemory<byte> memory = enumerator.Current;
                Assert.True(memory.IsEmpty);

                Assert.False(enumerator.MoveNext());
            }
            enumerator = new ReadOnlySequence<byte>.Enumerator(buffer);
            {
                Assert.Equal(default, enumerator.Current); 
                Assert.True(enumerator.MoveNext());
                ReadOnlyMemory<byte> memory = enumerator.Current;
                Assert.True(memory.IsEmpty);

                Assert.False(enumerator.MoveNext());
            }
        }

        #endregion

        #region TryGet

        [Fact]
        public void Empty_TryGet()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;
            ReadOnlyMemory<byte> memory;

            SequencePosition c1 = buffer.Start;
            Assert.True(buffer.TryGet(ref c1, out memory, false));
            Assert.Equal(buffer.Start, c1);
            Assert.True(memory.IsEmpty);

            Assert.True(buffer.TryGet(ref c1, out memory, true));
            Assert.Equal(null, c1.GetObject());
            Assert.True(memory.IsEmpty);

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
            Assert.True(buffer.TryGet(ref c1, out memory, false));
            Assert.Equal(buffer.End, c1);
            Assert.True(memory.IsEmpty);

            Assert.True(buffer.TryGet(ref c1, out memory, true));
            Assert.Equal(null, c1.GetObject());
            Assert.True(memory.IsEmpty);

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
