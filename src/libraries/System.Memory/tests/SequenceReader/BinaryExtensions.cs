// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Memory.Tests.SequenceReader
{
    public class BinaryExtensions
    {
        [Fact]
        public void MultiSegmentBytesReaderNumbers()
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 0          },
                new byte[] { 1, 2       },
                new byte[] { 3, 4       },
                new byte[] { 5, 6, 7, 8 },
                new byte[] { 8, 0       },
                new byte[] { 1,         },
                new byte[] { 0, 2,      },
                new byte[] { 6, 7, 8, 9 },
                new byte[] { 1, 2, 3, 4 },
                new byte[] { 5, 6       },
                new byte[] { 7, 8, 9,   },
                new byte[] { 0, 1, 2, 3 },
                new byte[] { 4, 5       },
                new byte[] { 6, 7, 8, 9 },
                new byte[] { 0, 1, 2, 3 },
                new byte[] { 4, 6       },
            });

            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);

            Assert.True(reader.TryReadTo(out ReadOnlySequence<byte> bytesValue, 2));
            Span<byte> span = bytesValue.ToArray();
            Assert.Equal(0, span[0]);
            Assert.Equal(1, span[1]);
            Assert.Equal(2, span.Length);
            Assert.Equal(3, reader.Consumed);

            Assert.True(reader.TryReadTo(out bytesValue, 5));
            span = bytesValue.ToArray();
            Assert.Equal(3, span[0]);
            Assert.Equal(4, span[1]);
            Assert.Equal(2, span.Length);
            Assert.Equal(6, reader.Consumed);

            Assert.True(reader.TryReadTo(out bytesValue, new byte[] { 8, 8 }));
            span = bytesValue.ToArray();
            Assert.Equal(6, span[0]);
            Assert.Equal(7, span[1]);
            Assert.Equal(2, span.Length);
            Assert.Equal(10, reader.Consumed);

            Assert.True(SequenceMarshal.TryRead(ref reader, out int intValue));
            Assert.Equal(BitConverter.ToInt32(new byte[] { 0, 1, 0, 2 }), intValue);
            Assert.Equal(14, reader.Consumed);

            Assert.True(reader.TryPeekLittleEndian(out intValue));
            Assert.Equal(0x09080706, intValue);
            Assert.Equal(14, reader.Consumed);

            Assert.True(reader.TryReadLittleEndian(out intValue));
            Assert.Equal(0x09080706, intValue);
            Assert.Equal(18, reader.Consumed);

            Assert.True(reader.TryPeekBigEndian(out intValue));
            Assert.Equal(0x01020304, intValue);
            Assert.Equal(18, reader.Consumed);

            Assert.True(reader.TryReadBigEndian(out intValue));
            Assert.Equal(0x01020304, intValue);
            Assert.Equal(22, reader.Consumed);

            Assert.True(reader.TryPeekLittleEndian(out long longValue));
            Assert.Equal(0x0201000908070605L, longValue);
            Assert.Equal(22, reader.Consumed);

            Assert.True(reader.TryReadLittleEndian(out longValue));
            Assert.Equal(0x0201000908070605L, longValue);
            Assert.Equal(30, reader.Consumed);

            Assert.True(reader.TryPeekBigEndian(out longValue));
            Assert.Equal(0x0304050607080900L, longValue);
            Assert.Equal(30, reader.Consumed);

            Assert.True(reader.TryReadBigEndian(out longValue));
            Assert.Equal(0x0304050607080900L, longValue);
            Assert.Equal(38, reader.Consumed);

            Assert.True(reader.TryPeekLittleEndian(out short shortValue));
            Assert.Equal(0x0201, shortValue);
            Assert.Equal(38, reader.Consumed);

            Assert.True(reader.TryReadLittleEndian(out shortValue));
            Assert.Equal(0x0201, shortValue);
            Assert.Equal(40, reader.Consumed);

            Assert.True(reader.TryPeekBigEndian(out shortValue));
            Assert.Equal(0x0304, shortValue);
            Assert.Equal(40, reader.Consumed);

            Assert.True(reader.TryReadBigEndian(out shortValue));
            Assert.Equal(0x0304, shortValue);
            Assert.Equal(42, reader.Consumed);

            // There is only one byte left, all Try* methods should return false

            Assert.False(SequenceMarshal.TryRead(ref reader, out intValue));
            Assert.Equal(0, intValue);
            Assert.Equal(42, reader.Consumed);

            Assert.False(reader.TryPeekBigEndian(out intValue));
            Assert.Equal(0, intValue);
            Assert.Equal(42, reader.Consumed);

            Assert.False(reader.TryReadBigEndian(out intValue));
            Assert.Equal(0, intValue);
            Assert.Equal(42, reader.Consumed);

            Assert.False(reader.TryPeekLittleEndian(out longValue));
            Assert.Equal(0, longValue);
            Assert.Equal(42, reader.Consumed);

            Assert.False(reader.TryReadLittleEndian(out longValue));
            Assert.Equal(0, longValue);
            Assert.Equal(42, reader.Consumed);

            Assert.False(reader.TryPeekBigEndian(out longValue));
            Assert.Equal(0, longValue);
            Assert.Equal(42, reader.Consumed);

            Assert.False(reader.TryReadBigEndian(out longValue));
            Assert.Equal(0, longValue);
            Assert.Equal(42, reader.Consumed);

            Assert.False(reader.TryPeekLittleEndian(out shortValue));
            Assert.Equal(0, shortValue);
            Assert.Equal(42, reader.Consumed);

            Assert.False(reader.TryReadLittleEndian(out shortValue));
            Assert.Equal(0, shortValue);
            Assert.Equal(42, reader.Consumed);

            Assert.False(reader.TryPeekBigEndian(out shortValue));
            Assert.Equal(0, shortValue);
            Assert.Equal(42, reader.Consumed);

            Assert.False(reader.TryReadBigEndian(out shortValue));
            Assert.Equal(0, shortValue);
            Assert.Equal(42, reader.Consumed);
        }
    }
}
