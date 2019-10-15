// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Xunit;

namespace System.Memory.Tests.SequenceReader
{
    public class IsNext
    {
        [Theory,
            InlineData(true),
            InlineData(false)]
        public void IsNext_Empty(bool advancePast)
        {
            var reader = new SequenceReader<char>(ReadOnlySequence<char>.Empty);

            Assert.False(reader.IsNext('Z', advancePast));
            Assert.Equal(0, reader.Consumed);

            // Nothing is always next
            Assert.True(reader.IsNext(ReadOnlySpan<char>.Empty, advancePast));
            Assert.Equal(0, reader.Consumed);

            // Something isn't
            Assert.False(reader.IsNext(new char[] { '\0' }, advancePast));
            Assert.Equal(0, reader.Consumed);
        }

        [Fact]
        public void IsNext_Span()
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 0          },
                new byte[] { 1, 2       },
                new byte[] { 3, 4       },
                new byte[] { 5, 6, 7, 8 }
            });

            var reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.IsNext(ReadOnlySpan<byte>.Empty, advancePast: false));
            Assert.True(reader.IsNext(ReadOnlySpan<byte>.Empty, advancePast: true));
            Assert.True(reader.IsNext(new byte[] { 0 }, advancePast: false));
            Assert.False(reader.IsNext(new byte[] { 0, 2 }, advancePast: false));
            Assert.False(reader.IsNext(new byte[] { 0, 2 }, advancePast: true));
            Assert.True(reader.IsNext(new byte[] { 0, 1 }, advancePast: false));
            Assert.False(reader.IsNext(new byte[] { 0, 1, 3 }, advancePast: false));
            Assert.True(reader.IsNext(new byte[] { 0, 1, 2 }, advancePast: false));
            Assert.False(reader.IsNext(new byte[] { 0, 1, 2, 4 }, advancePast: false));
            Assert.True(reader.IsNext(new byte[] { 0, 1, 2, 3 }, advancePast: false));
            Assert.True(reader.IsNext(new byte[] { 0, 1, 2, 3, 4 }, advancePast: false));
            Assert.True(reader.IsNext(new byte[] { 0, 1, 2, 3, 4, 5 }, advancePast: false));
            Assert.True(reader.IsNext(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, advancePast: false));
            Assert.False(reader.IsNext(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, advancePast: false));
            Assert.False(reader.IsNext(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, advancePast: true));
            Assert.Equal(0, reader.Consumed);

            Assert.True(reader.IsNext(new byte[] { 0, 1, 2, 3 }, advancePast: true));
            Assert.True(reader.IsNext(new byte[] { 4, 5, 6 }, advancePast: true));
            Assert.True(reader.TryPeek(out byte value));
            Assert.Equal(7, value);

            Assert.True(reader.IsNext(new byte[] { 7, 8 }, advancePast: true));
            Assert.True(reader.End);
        }

        [Fact]
        public void IsNext_Value()
        {
            ReadOnlySequence<char> chars = SequenceFactory.Create(new char[][] {
                new char[] { 'A'           },
                new char[] { 'B', 'C'      },
            });

            var reader = new SequenceReader<char>(chars);
            Assert.False(reader.IsNext('Z', advancePast: false));
            Assert.False(reader.IsNext('B', advancePast: false));
            Assert.True(reader.IsNext('A', advancePast: false));
            Assert.True(reader.IsNext('A', advancePast: true));
            Assert.True(reader.IsNext('B', advancePast: true));
            Assert.True(reader.IsNext('C', advancePast: true));
            Assert.False(reader.IsNext('C', advancePast: true));
            Assert.True(reader.End);
        }
    }
}
