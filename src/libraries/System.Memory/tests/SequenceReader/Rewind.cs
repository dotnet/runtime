// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.Memory.Tests.SequenceReader
{
    public class Rewind
    {
        [Fact]
        public void Rewind_Full()
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 0          },
                new byte[] { 1, 2       },
                new byte[] { 3, 4       },
                new byte[] { 5, 6, 7, 8 }
            });

            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);
            reader.Advance(1);
            SequenceReader<byte> copy = reader;
            for (int i = 1; i < bytes.Length; i++)
            {
                reader.Advance(i);
                reader.Rewind(i);

                Assert.Equal(copy.Position, reader.Position);
                Assert.Equal(copy.Consumed, reader.Consumed);
                Assert.Equal(copy.CurrentSpanIndex, reader.CurrentSpanIndex);
                Assert.Equal(copy.End, reader.End);
                Assert.True(copy.CurrentSpan.SequenceEqual(reader.CurrentSpan));
            }
        }

        [Fact]
        public void Rewind_ByOne()
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 0          },
                new byte[] { 1, 2       },
                new byte[] { 3, 4       },
                new byte[] { 5, 6, 7, 8 }
            });

            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);
            reader.Advance(1);
            SequenceReader<byte> copy = reader;
            for (int i = 1; i < bytes.Length; i++)
            {
                reader.Advance(i);
                Assert.Equal(i + 1, reader.Consumed);

                for (int j = 0; j < i; j++)
                {
                    reader.Rewind(1);
                    Assert.Equal(i - j, reader.Consumed);
                    Assert.False(reader.End);
                }

                Assert.Equal(copy.Position, reader.Position);
                Assert.Equal(copy.Consumed, reader.Consumed);
                Assert.Equal(copy.CurrentSpanIndex, reader.CurrentSpanIndex);
                Assert.Equal(copy.End, reader.End);
                Assert.True(copy.CurrentSpan.SequenceEqual(reader.CurrentSpan));
            }
        }

        [Fact]
        public void Rewind_Exception()
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 0    },
                new byte[] { 1, 2 }
            });

            // Can't go negative
            Assert.Throws<ArgumentOutOfRangeException>(() => new SequenceReader<byte>(bytes).Rewind(-1));

            // Can't pull more than we consumed
            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);
            try
            {
                reader.Rewind(1);
                Assert.Fail("no exception thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Expected
            }
            Assert.Equal(0, reader.Consumed);

            reader.Advance(1);
            try
            {
                reader.Rewind(2);
                Assert.Fail("no exception thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Expected
            }
            Assert.Equal(1, reader.Consumed);
        }

        [Fact]
        public void RewindEmptyFirstSpan()
        {
            // This is to hit the "if (memory.Length == 0)" branch in ResetReader.
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[0],
                new byte[] { 1, 2 },
                new byte[] { 3, 4 }
            });

            var reader = new SequenceReader<byte>(bytes);
            reader.Advance(3);
            Assert.True(reader.IsNext(4));
            reader.Rewind(2);
            Assert.Equal(new byte[] { 1, 2 }, reader.CurrentSpan.ToArray());
        }

        [Fact]
        public void RewindZero_DoesNothing()
        {
            var emptySeq = ReadOnlySequence<byte>.Empty;
            var reader = new SequenceReader<byte>(emptySeq);

            Assert.True(reader.End);
            Assert.False(reader.TryRead(out byte _));

            reader.Rewind(0);

            Assert.True(reader.End);
            Assert.False(reader.TryRead(out byte _));
        }
    }
}
