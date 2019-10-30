// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Xunit;

namespace System.Memory.Tests.SequenceReader
{
    public class Advance
    {
        [Theory,
            InlineData(true),
            InlineData(false)]
        public void Basic(bool singleSegment)
        {
            byte[] buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            ReadOnlySequence<byte> bytes = singleSegment
                ? new ReadOnlySequence<byte>(buffer)
                : SequenceFactory.CreateSplit(buffer, 2, 4);

            SequenceReader<byte> skipReader = new SequenceReader<byte>(bytes);
            Assert.False(skipReader.TryAdvanceTo(10));
            Assert.True(skipReader.TryAdvanceTo(4, advancePastDelimiter: false));
            Assert.True(skipReader.TryRead(out byte value));
            Assert.Equal(4, value);

            Assert.True(skipReader.TryAdvanceToAny(new byte[] { 3, 12, 7 }, advancePastDelimiter: false));
            Assert.True(skipReader.TryRead(out value));
            Assert.Equal(7, value);
            Assert.Equal(1, skipReader.AdvancePast(8));
            Assert.True(skipReader.TryRead(out value));
            Assert.Equal(9, value);

            skipReader = new SequenceReader<byte>(bytes);
            Assert.Equal(0, skipReader.AdvancePast(2));
            Assert.Equal(3, skipReader.AdvancePastAny(new byte[] { 2, 3, 1 }));
            Assert.True(skipReader.TryRead(out value));
            Assert.Equal(4, value);
            skipReader.Rewind(skipReader.Consumed);
            Assert.Equal(0, skipReader.AdvancePast(2));
            Assert.Equal(3, skipReader.AdvancePastAny(2, 3, 1));
            Assert.True(skipReader.TryRead(out value));
            Assert.Equal(4, value);
            skipReader.Rewind(skipReader.Consumed);
            Assert.Equal(0, skipReader.AdvancePast(2));
            Assert.Equal(3, skipReader.AdvancePastAny(2, 3, 1, 7));
            Assert.True(skipReader.TryRead(out value));
            Assert.Equal(4, value);
            skipReader.Rewind(skipReader.Consumed - 1);
            Assert.Equal(0, skipReader.AdvancePast(1));
            Assert.Equal(2, skipReader.AdvancePastAny(2, 3));
            Assert.True(skipReader.TryRead(out value));
            Assert.Equal(4, value);
        }

        [Fact]
        public void PastEmptySegments()
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 0 },
                new byte[] { },
                new byte[] { },
                new byte[] { }
            });

            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);
            reader.Advance(1);
            Assert.Equal(0, reader.CurrentSpanIndex);
            Assert.Equal(0, reader.CurrentSpan.Length);
            Assert.False(reader.TryPeek(out byte value));
            ReadOnlySequence<byte> sequence = reader.Sequence.Slice(reader.Position);
            Assert.Equal(0, sequence.Length);
        }

        [Fact]
        public void Advance_Exception()
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 0          },
                new byte[] { 1, 2       },
                new byte[] { 3, 4       },
                new byte[] { 5, 6, 7, 8 }
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => new SequenceReader<byte>(bytes).Advance(-1));
        }
    }
}
