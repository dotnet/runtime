// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Buffers;
using Xunit;

namespace System.Memory.Tests.SequenceReader
{
    public class ReadTo
    {
        [Theory,
            InlineData(false, false),
            InlineData(false, true),
            InlineData(true, false),
            InlineData(true, true)]
        public void TryReadTo_Span(bool advancePastDelimiter, bool useEscapeOverload)
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 0 },
                new byte[] { 1, 2 },
                new byte[] { },
                new byte[] { 3, 4, 5, 6 }
            });

            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);

            // Read to 0-5
            for (byte i = 0; i < bytes.Length - 1; i++)
            {
                SequenceReader<byte> copy = reader;

                // Can read to the first integer (0-5)
                Assert.True(
                    useEscapeOverload
                        ? copy.TryReadTo(out ReadOnlySpan<byte> span, i, 255, advancePastDelimiter)
                        : copy.TryReadTo(out span, i, advancePastDelimiter));

                // Should never have a null Position object
                Assert.NotNull(copy.Position.GetObject());

                // Should be able to then read to 6
                Assert.True(
                    useEscapeOverload
                        ? copy.TryReadTo(out span, 6, 255, advancePastDelimiter)
                        : copy.TryReadTo(out span, 6, advancePastDelimiter));

                Assert.NotNull(copy.Position.GetObject());

                // If we didn't advance, we should still be able to read to 6
                Assert.Equal(!advancePastDelimiter,
                    useEscapeOverload
                        ? copy.TryReadTo(out span, 6, 255, advancePastDelimiter)
                        : copy.TryReadTo(out span, 6, advancePastDelimiter));
            }
        }

        [Theory,
            InlineData(false, false),
            InlineData(false, true),
            InlineData(true, false),
            InlineData(true, true)]
        public void TryReadTo_Sequence(bool advancePastDelimiter, bool useEscapeOverload)
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 0 },
                new byte[] { 1, 2 },
                new byte[] { },
                new byte[] { 3, 4, 5, 6 }
            });

            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);

            // Read to 0-5
            for (byte i = 0; i < bytes.Length - 1; i++)
            {
                SequenceReader<byte> copy = reader;

                // Can read to the first integer (0-5)
                Assert.True(
                    useEscapeOverload
                        ? copy.TryReadTo(out ReadOnlySequence<byte> sequence, i, 255, advancePastDelimiter)
                        : copy.TryReadTo(out sequence, i, advancePastDelimiter));

                // Should never have a null Position object
                Assert.NotNull(copy.Position.GetObject());
                ReadOnlySequence<byte>.Enumerator enumerator = sequence.GetEnumerator();
                while (enumerator.MoveNext())
                    ;

                // Should be able to read to final 6
                Assert.True(
                    useEscapeOverload
                        ? copy.TryReadTo(out sequence, 6, 255, advancePastDelimiter)
                        : copy.TryReadTo(out sequence, 6, advancePastDelimiter));

                Assert.NotNull(copy.Position.GetObject());
                enumerator = sequence.GetEnumerator();
                while (enumerator.MoveNext())
                    ;

                // If we didn't advance, we should still be able to read to 6
                Assert.Equal(!advancePastDelimiter,
                    useEscapeOverload
                        ? copy.TryReadTo(out sequence, 6, 255, advancePastDelimiter)
                        : copy.TryReadTo(out sequence, 6, advancePastDelimiter));
            }
        }

        [Fact]
        public void TryReadExact_Sequence()
        {
            ReadOnlySequence<int> data = SequenceFactory.Create(new int[][] {
                new int[] { 0 },
                new int[] { 1, 2 },
                new int[] { },
                new int[] { 3, 4 }
            });

            var sequenceReader = new SequenceReader<int>(data);

            Assert.True(sequenceReader.TryReadExact(0, out ReadOnlySequence<int> sequence));
            Assert.Equal(0, sequence.Length);

            for (int i = 0; i < 2; i++)
            {
                Assert.True(sequenceReader.TryReadExact(2, out sequence));
                Assert.Equal(Enumerable.Range(i * 2, 2), sequence.ToArray());
            }

            // There is only 1 item in sequence reader
            Assert.False(sequenceReader.TryReadExact(2, out _));

            // The last 1 item was not advanced so still can be fetched
            Assert.True(sequenceReader.TryReadExact(1, out sequence));
            Assert.Equal(1, sequence.Length);
            Assert.Equal(4, sequence.FirstSpan[0]);

            Assert.True(sequenceReader.End);
        }

        [Theory,
            InlineData(false),
            InlineData(true),]
        public void TryReadToSpan_Sequence(bool advancePastDelimiter)
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 0, 0 },
                new byte[] { 1, 1, 2, 2 },
                new byte[] { },
                new byte[] { 3, 3, 4, 4, 5, 5, 6, 6 }
            });

            SequenceReader<byte> baseReader = new SequenceReader<byte>(bytes);
            for (byte i = 0; i < bytes.Length / 2 - 1; i++)
            {
                byte[] expected = new byte[i * 2 + 1];
                for (int j = 0; j < expected.Length - 1; j++)
                {
                    expected[j] = (byte)(j / 2);
                }
                expected[i * 2] = i;
                ReadOnlySpan<byte> searchFor = new byte[] { i, (byte)(i + 1) };
                SequenceReader<byte> copy = baseReader;

                Assert.True(copy.TryReadTo(out ReadOnlySpan<byte> sp, searchFor, advancePastDelimiter));
                Assert.True(sp.SequenceEqual(expected));

                copy = baseReader;
                Assert.True(copy.TryReadTo(out ReadOnlySequence<byte> seq, searchFor, advancePastDelimiter));
                Assert.True(seq.ToArray().AsSpan().SequenceEqual(expected));
            }

            bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 47, 42, 66, 32, 42, 32, 66, 42, 47 }   // /*b * b*/
            });

            baseReader = new SequenceReader<byte>(bytes);
            SequenceReader<byte> copyReader = baseReader;

            Assert.True(copyReader.TryReadTo(out ReadOnlySpan<byte> span, new byte[] { 42, 47 }, advancePastDelimiter));    //  */
            Assert.True(span.SequenceEqual(new byte[] { 47, 42, 66, 32, 42, 32, 66 }));

            copyReader = baseReader;
            Assert.True(copyReader.TryReadTo(out ReadOnlySequence<byte> sequence, new byte[] { 42, 47 }, advancePastDelimiter));    //  */
            Assert.True(sequence.ToArray().AsSpan().SequenceEqual(new byte[] { 47, 42, 66, 32, 42, 32, 66 }));
        }

        [Theory,
            InlineData(false),
            InlineData(true)]
        public void TryReadTo_NotFound_Span(bool advancePastDelimiter)
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 1 },
                new byte[] { 2, 3, 255 }
            });

            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);
            reader.Advance(4);
            Assert.False(reader.TryReadTo(out ReadOnlySpan<byte> span, 255, 0, advancePastDelimiter));
        }

        [Theory,
            InlineData(false),
            InlineData(true)]
        public void TryReadTo_NotFound_Sequence(bool advancePastDelimiter)
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 1 },
                new byte[] { 2, 3, 255 }
            });

            SequenceReader<byte> reader = new SequenceReader<byte>(bytes);
            reader.Advance(4);
            Assert.False(reader.TryReadTo(out ReadOnlySequence<byte> span, 255, 0, advancePastDelimiter));
        }

        [Fact]
        public void TryReadTo_SingleDelimiter()
        {
            ReadOnlySequence<byte> bytes = SequenceFactory.Create(new byte[][] {
                new byte[] { 1 },
                new byte[] { 2, 3, 4, 5, 6 }
            });

            SequenceReader<byte> baseReader = new SequenceReader<byte>(bytes);

            SequenceReader<byte> spanReader = baseReader;
            SequenceReader<byte> sequenceReader = baseReader;
            Span<byte> delimiter = new byte[] { 1 };

            for (int i = 1; i < 6; i += 1)
            {
                // Also check scanning from the start.
                SequenceReader<byte> resetReader = baseReader;
                delimiter[0] = (byte)i;
                Assert.True(spanReader.TryReadTo(out ReadOnlySpan<byte> span, delimiter, advancePastDelimiter: true));
                Assert.True(resetReader.TryReadTo(out span, delimiter, advancePastDelimiter: true));
                Assert.True(spanReader.TryPeek(out byte value));
                Assert.Equal(i + 1, value);
                Assert.True(resetReader.TryPeek(out value));
                Assert.Equal(i + 1, value);

                // Also check scanning from the start.
                resetReader = baseReader;
                delimiter[0] = (byte)i;
                Assert.True(sequenceReader.TryReadTo(out ReadOnlySequence<byte> sequence, delimiter, advancePastDelimiter: true));
                Assert.True(resetReader.TryReadTo(out sequence, delimiter, advancePastDelimiter: true));
                Assert.True(sequenceReader.TryPeek(out value));
                Assert.Equal(i + 1, value);
                Assert.True(resetReader.TryPeek(out value));
                Assert.Equal(i + 1, value);
            }
        }

        [Fact]
        public void TryReadTo_Span_At_Segments_Boundary()
        {
            Span<byte> delimiter = new byte[] { 13, 10 }; // \r\n
            BufferSegment<byte> segment = new BufferSegment<byte>("Hello\r"u8.ToArray());
            segment.Append("\nWorld"u8.ToArray()); // add next segment
            ReadOnlySequence<byte> inputSeq = new ReadOnlySequence<byte>(segment, 0, segment, 6); // span only the first segment!
            SequenceReader<byte> sr = new SequenceReader<byte>(inputSeq);
            bool r = sr.TryReadTo(out ReadOnlySpan<byte> _, delimiter);
            Assert.False(r);
            r = sr.TryReadTo(out ReadOnlySequence<byte> _, delimiter);
            Assert.False(r);
        }
    }
}
