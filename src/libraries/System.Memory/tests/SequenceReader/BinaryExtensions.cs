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
                new byte[] { 1, 2, 3, 4 },
                new byte[] { 5, 6       },
                new byte[] { 7, 8, 9,   },
                new byte[] { 0, 1, 2, 3 },
                new byte[] { 4, 5       },
                new byte[] { 6, 7, 8, 9 },
                new byte[] { 0, 1, 2, 3 },
                new byte[] { 4          },
            });

            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);

            Assert.True(reader.TryReadTo(out ReadOnlySequence<byte> bytesValue, 2));
            Span<byte> span = bytesValue.ToArray();
            Assert.Equal(0, span[0]);
            Assert.Equal(1, span[1]);

            Assert.True(reader.TryReadTo(out bytesValue, 5));
            span = bytesValue.ToArray();
            Assert.Equal(3, span[0]);
            Assert.Equal(4, span[1]);

            Assert.True(reader.TryReadTo(out bytesValue, new byte[] { 8, 8 }));
            span = bytesValue.ToArray();
            Assert.Equal(6, span[0]);
            Assert.Equal(7, span[1]);

            Assert.True(SequenceMarshal.TryRead(ref reader, out int intValue));
            Assert.Equal(BitConverter.ToInt32(new byte[] { 0, 1, 0, 2 }), intValue);

            Assert.True(reader.TryReadBigEndian(out intValue));
            Assert.Equal(0x01020304, intValue);

            Assert.True(reader.TryReadLittleEndian(out long longValue));
            Assert.Equal(0x0201000908070605L, longValue);

            Assert.True(reader.TryReadBigEndian(out longValue));
            Assert.Equal(0x0304050607080900L, longValue);

            Assert.True(reader.TryReadLittleEndian(out short shortValue));
            Assert.Equal(0x0201, shortValue);

            Assert.True(reader.TryReadBigEndian(out shortValue));
            Assert.Equal(0x0304, shortValue);
        }
    }
}
