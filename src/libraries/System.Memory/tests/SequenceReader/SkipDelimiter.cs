// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Text;
using Xunit;

namespace System.Memory.Tests.SequenceReader
{
    class SkipDelimiter
    {
        [Fact]
        public void TryReadTo_SkipDelimiter()
        {
            byte[] expected = Encoding.UTF8.GetBytes("This is our ^|understanding^|");
            ReadOnlySequence<byte> bytes = SequenceFactory.CreateUtf8("This is our ^|understanding^|| you see.");
            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out ReadOnlySpan<byte> span, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(expected, span.ToArray());
            Assert.True(reader.IsNext((byte)' '));
            Assert.Equal(30, reader.Consumed);

            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(expected, span.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(29, reader.Consumed);

            // Put the skip delimiter in another segment
            bytes = SequenceFactory.CreateUtf8("This is our ^|understanding", "^|| you see.");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(expected, span.ToArray());
            Assert.True(reader.IsNext((byte)' '));
            Assert.Equal(30, reader.Consumed);

            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(expected, span.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(29, reader.Consumed);

            // Put the skip delimiter at the end of the segment
            bytes = SequenceFactory.CreateUtf8("This is our ^|understanding^", "|| you see.");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(expected, span.ToArray());
            Assert.True(reader.IsNext((byte)' '));
            Assert.Equal(30, reader.Consumed);

            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(expected, span.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(29, reader.Consumed);

            // No trailing data
            bytes = SequenceFactory.CreateUtf8("This is our ^|understanding^||");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(expected, span.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(29, reader.Consumed);

            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(expected, span.ToArray());
            Assert.True(reader.End);
            Assert.Equal(30, reader.Consumed);

            // All delimiters skipped
            bytes = SequenceFactory.CreateUtf8("This is our ^|understanding^|");
            reader = new SequenceReader<byte>(bytes);
            Assert.False(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(0, reader.Consumed);

            reader = new SequenceReader<byte>(bytes);
            Assert.False(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(0, reader.Consumed);

            bytes = SequenceFactory.CreateUtf8("abc^|de|");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(Encoding.UTF8.GetBytes("abc^|de"), span.ToArray());
            Assert.True(reader.End);
            Assert.Equal(8, reader.Consumed);

            // Escape leads
            bytes = SequenceFactory.CreateUtf8("^|a|b");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(Encoding.UTF8.GetBytes("^|a"), span.ToArray());
            Assert.True(reader.IsNext((byte)'b'));
            Assert.Equal(4, reader.Consumed);

            // Delimiter starts second segment.
            bytes = SequenceFactory.CreateUtf8("^", "|a|b");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(Encoding.UTF8.GetBytes("^|a"), span.ToArray());
            Assert.True(reader.IsNext((byte)'b'));
            Assert.Equal(4, reader.Consumed);
        }

        [Fact]
        public void TryReadTo_SkipDelimiter_Runs()
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.CreateUtf8("abc^^|def");
            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out ReadOnlySpan<byte> span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(Encoding.UTF8.GetBytes("abc^^"), span.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(5, reader.Consumed);

            // Split after escape char
            bytes = SequenceFactory.CreateUtf8("abc^^", "|def");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(Encoding.UTF8.GetBytes("abc^^"), span.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(5, reader.Consumed);

            // Split before and after escape char
            bytes = SequenceFactory.CreateUtf8("abc^", "^", "|def");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(Encoding.UTF8.GetBytes("abc^^"), span.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(5, reader.Consumed);

            // Check advance past delimiter
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(Encoding.UTF8.GetBytes("abc^^"), span.ToArray());
            Assert.True(reader.IsNext((byte)'d'));
            Assert.Equal(6, reader.Consumed);

            // Leading run of 2
            bytes = SequenceFactory.CreateUtf8("^^|abc");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(Encoding.UTF8.GetBytes("^^"), span.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(2, reader.Consumed);

            // Leading run of 3
            bytes = SequenceFactory.CreateUtf8("^^^|abc");
            reader = new SequenceReader<byte>(bytes);
            Assert.False(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.True(reader.IsNext((byte)'^'));
            Assert.Equal(0, reader.Consumed);

            // Trailing run of 3
            bytes = SequenceFactory.CreateUtf8("abc^^^|");
            reader = new SequenceReader<byte>(bytes);
            Assert.False(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.True(reader.IsNext((byte)'a'));
            Assert.Equal(0, reader.Consumed);

            // Trailing run of 3, split
            bytes = SequenceFactory.CreateUtf8("abc^^^", "|");
            reader = new SequenceReader<byte>(bytes);
            Assert.False(reader.TryReadTo(out span, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.True(reader.IsNext((byte)'a'));
            Assert.Equal(0, reader.Consumed);
        }

        [Fact]
        public void TryReadTo_SkipDelimiter_ReadOnlySequence()
        {
            byte[] expected = Encoding.UTF8.GetBytes("This is our ^|understanding^|");
            ReadOnlySequence<byte> bytes = SequenceFactory.CreateUtf8("This is our ^|understanding^|| you see.");
            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out ReadOnlySequence<byte> sequence, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(expected, sequence.ToArray());
            Assert.True(reader.IsNext((byte)' '));
            Assert.Equal(30, reader.Consumed);

            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(expected, sequence.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(29, reader.Consumed);

            // Put the skip delimiter in another segment
            bytes = SequenceFactory.CreateUtf8("This is our ^|understanding", "^|| you see.");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(expected, sequence.ToArray());
            Assert.True(reader.IsNext((byte)' '));
            Assert.Equal(30, reader.Consumed);

            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(expected, sequence.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(29, reader.Consumed);

            // Put the skip delimiter at the end of the segment
            bytes = SequenceFactory.CreateUtf8("This is our ^|understanding^", "|| you see.");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(expected, sequence.ToArray());
            Assert.True(reader.IsNext((byte)' '));
            Assert.Equal(30, reader.Consumed);

            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(expected, sequence.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(29, reader.Consumed);

            // No trailing data
            bytes = SequenceFactory.CreateUtf8("This is our ^|understanding^||");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(expected, sequence.ToArray());
            Assert.True(reader.IsNext((byte)'|'));
            Assert.Equal(29, reader.Consumed);

            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(expected, sequence.ToArray());
            Assert.True(reader.End);
            Assert.Equal(30, reader.Consumed);

            // All delimiters skipped
            bytes = SequenceFactory.CreateUtf8("This is our ^|understanding^|");
            reader = new SequenceReader<byte>(bytes);
            Assert.False(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: false));
            Assert.Equal(0, reader.Consumed);

            reader = new SequenceReader<byte>(bytes);
            Assert.False(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(0, reader.Consumed);

            bytes = SequenceFactory.CreateUtf8("abc^|de|");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(Encoding.UTF8.GetBytes("abc^|de"), sequence.ToArray());
            Assert.True(reader.End);
            Assert.Equal(8, reader.Consumed);

            // Escape leads
            bytes = SequenceFactory.CreateUtf8("^|a|b");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(Encoding.UTF8.GetBytes("^|a"), sequence.ToArray());
            Assert.True(reader.IsNext((byte)'b'));
            Assert.Equal(4, reader.Consumed);

            // Delimiter starts second segment.
            bytes = SequenceFactory.CreateUtf8("^", "|a|b");
            reader = new SequenceReader<byte>(bytes);
            Assert.True(reader.TryReadTo(out sequence, (byte)'|', (byte)'^', advancePastDelimiter: true));
            Assert.Equal(Encoding.UTF8.GetBytes("^|a"), sequence.ToArray());
            Assert.True(reader.IsNext((byte)'b'));
            Assert.Equal(4, reader.Consumed);
        }
    }
}
