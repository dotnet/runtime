// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Internal;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    //
    // Test reading/decoding of compressed integers as described in the ECMA 335 CLI specification.
    // See Section II.3.2 - Blobs and Signatures.
    //
    public class CompressedIntegerTests
    {
        [Fact]
        public void DecompressUnsignedIntegersFromSpecExamples()
        {
            // These examples are straight from the CLI spec.
            // Test that we read them correctly straight from their explicit encoded values.

            Assert.Equal(0x03, ReadCompressedUnsignedInteger(0x03));
            Assert.Equal(0x7f, ReadCompressedUnsignedInteger(0x7f));
            Assert.Equal(0x80, ReadCompressedUnsignedInteger(0x80, 0x80));
            Assert.Equal(0x2E57, ReadCompressedUnsignedInteger(0xAE, 0x57));
            Assert.Equal(0x3FFF, ReadCompressedUnsignedInteger(0xBF, 0xFF));
            Assert.Equal(0x4000, ReadCompressedUnsignedInteger(0xC0, 0x00, 0x40, 0x00));
            Assert.Equal(0x1FFFFFFF, ReadCompressedUnsignedInteger(0xDF, 0xFF, 0xFF, 0xFF));
        }

        [Fact]
        public void CompressUnsignedIntegersFromSpecExamples()
        {
            // These examples are straight from the CLI spec.
            // Test that our compression routine (written below for test purposes) encodes them the same way. 

            Assert.Equal(CompressUnsignedInteger(0x03), new byte[] { 0x03 });
            Assert.Equal(CompressUnsignedInteger(0x7F), new byte[] { 0x7f });
            Assert.Equal(CompressUnsignedInteger(0x80), new byte[] { 0x80, 0x80 });
            Assert.Equal(CompressUnsignedInteger(0x2E57), new byte[] { 0xAE, 0x57 });
            Assert.Equal(CompressUnsignedInteger(0x3FFF), new byte[] { 0xBF, 0xFF });
            Assert.Equal(CompressUnsignedInteger(0x4000), new byte[] { 0xC0, 0x00, 0x40, 0x00 });
            Assert.Equal(CompressUnsignedInteger(0x1FFFFFFF), new byte[] { 0xDF, 0xFF, 0xFF, 0xFF });
        }

        [Fact]
        public void DecompressInvalidUnsignedIntegers()
        {
            // Too few bytes
            Assert.Equal(BlobReader.InvalidCompressedInteger, ReadCompressedUnsignedInteger(new byte[0]));
            Assert.Equal(BlobReader.InvalidCompressedInteger, ReadCompressedUnsignedInteger(0x80));
            Assert.Equal(BlobReader.InvalidCompressedInteger, ReadCompressedUnsignedInteger(0xC0, 0xFF));

            // No compressed integer can lead with 3 bits set.
            Assert.Equal(BlobReader.InvalidCompressedInteger, ReadCompressedUnsignedInteger(0xFF, 0xFF, 0xFF, 0xFF));
            Assert.Equal(BlobReader.InvalidCompressedInteger, ReadCompressedUnsignedInteger(0xE0, 0x00, 0x00, 0x00));
        }

        [Fact]
        public void DecompressSignedIntegersFromSpecExamples()
        {
            // These examples are straight from the CLI spec.
            // Test that we read them correctly straight from their explicit encoded values.
            Assert.Equal(3, ReadCompressedSignedInteger(0x06));
            Assert.Equal(-3, ReadCompressedSignedInteger(0x7B));
            Assert.Equal(64, ReadCompressedSignedInteger(0x80, 0x80));
            Assert.Equal(-64, ReadCompressedSignedInteger(0x01));
            Assert.Equal(8192, ReadCompressedSignedInteger(0xC0, 0x00, 0x40, 0x00));
            Assert.Equal(-8192, ReadCompressedSignedInteger(0x80, 0x01));
            Assert.Equal(268435455, ReadCompressedSignedInteger(0xDF, 0xFF, 0xFF, 0xFE));
            Assert.Equal(-268435456, ReadCompressedSignedInteger(0xC0, 0x00, 0x00, 0x01));
        }


        [Fact]
        public void CheckCompressedUnsignedIntegers()
        {
            // Test that we can round trip unsigned integers near the edge conditions

            // Around 0
            CheckCompressedUnsignedInteger(0, 1);
            CheckCompressedUnsignedInteger(-1, -1);
            CheckCompressedUnsignedInteger(1, 1);

            // Near 1 byte limit
            CheckCompressedUnsignedInteger((1 << 7), 2);
            CheckCompressedUnsignedInteger((1 << 7) - 1, 1);
            CheckCompressedUnsignedInteger((1 << 7) + 1, 2);

            // Near 2 byte limit
            CheckCompressedUnsignedInteger((1 << 14), 4);
            CheckCompressedUnsignedInteger((1 << 14) - 1, 2);
            CheckCompressedUnsignedInteger((1 << 14) + 1, 4);

            // Near 4 byte limit
            CheckCompressedUnsignedInteger((1 << 29), -1);
            CheckCompressedUnsignedInteger((1 << 29) - 1, 4);
            CheckCompressedUnsignedInteger((1 << 29) + 1, -1);
        }

        [Fact]
        public void CompressSignedIntegersFromSpecExamples()
        {
            // These examples are straight from the CLI spec.
            // Test that our compression routine (written below for test purposes) encodes them the same way. 
            Assert.Equal(CompressSignedInteger(3), new byte[] { 0x06 });
            Assert.Equal(CompressSignedInteger(-3), new byte[] { 0x7b });
            Assert.Equal(CompressSignedInteger(64), new byte[] { 0x80, 0x80 });
            Assert.Equal(CompressSignedInteger(-64), new byte[] { 0x01 });
            Assert.Equal(CompressSignedInteger(8192), new byte[] { 0xC0, 0x00, 0x40, 0x00 });
            Assert.Equal(CompressSignedInteger(-8192), new byte[] { 0x80, 0x01 });
            Assert.Equal(CompressSignedInteger(268435455), new byte[] { 0xDF, 0xFF, 0xFF, 0xFE });
            Assert.Equal(CompressSignedInteger(-268435456), new byte[] { 0xC0, 0x00, 0x00, 0x01 });
        }

        [Fact]
        public unsafe void DecompressInvalidSignedIntegers()
        {
            // Too few bytes
            Assert.Equal(BlobReader.InvalidCompressedInteger, ReadCompressedSignedInteger(new byte[0]));
            Assert.Equal(BlobReader.InvalidCompressedInteger, ReadCompressedSignedInteger(0x80));
            Assert.Equal(BlobReader.InvalidCompressedInteger, ReadCompressedSignedInteger(0xC0, 0xFF));

            // No compressed integer can lead with 3 bits set.
            Assert.Equal(BlobReader.InvalidCompressedInteger, ReadCompressedSignedInteger(0xFF, 0xFF, 0xFF, 0xFF));
            Assert.Equal(BlobReader.InvalidCompressedInteger, ReadCompressedSignedInteger(0xE0, 0x00, 0x00, 0x00));
        }

        [Fact]
        public void CheckCompressedSignedIntegers()
        {
            // Test that we can round trip signed integers near the edge conditions.

            // around 0
            CheckCompressedSignedInteger(-1, 1);
            CheckCompressedSignedInteger(1, 1);

            // around 1 byte limit
            CheckCompressedSignedInteger(-(1 << 6), 1);
            CheckCompressedSignedInteger(-(1 << 6) - 1, 2);
            CheckCompressedSignedInteger(-(1 << 6) + 1, 1);
            CheckCompressedSignedInteger((1 << 6), 2);
            CheckCompressedSignedInteger((1 << 6) - 1, 1);
            CheckCompressedSignedInteger((1 << 6) + 1, 2);

            // around 2 byte limit
            CheckCompressedSignedInteger(-(1 << 13), 2);
            CheckCompressedSignedInteger(-(1 << 13) - 1, 4);
            CheckCompressedSignedInteger(-(1 << 13) + 1, 2);
            CheckCompressedSignedInteger((1 << 13), 4);
            CheckCompressedSignedInteger((1 << 13) - 1, 2);
            CheckCompressedSignedInteger((1 << 13) + 1, 4);

            // around 4 byte limit
            CheckCompressedSignedInteger(-(1 << 28), 4);
            CheckCompressedSignedInteger(-(1 << 28) - 1, -1);
            CheckCompressedSignedInteger(-(1 << 28) + 1, 4);
            CheckCompressedSignedInteger((1 << 28), -1);
            CheckCompressedSignedInteger((1 << 28) - 1, 4);
            CheckCompressedSignedInteger((1 << 28) + 1, -1);
        }

        private void CheckCompressedSignedInteger(int valueToRoundTrip, int numberOfBytesExpected)
        {
            CheckCompressedInteger(valueToRoundTrip, numberOfBytesExpected, CompressSignedInteger, ReadCompressedSignedInteger);
        }

        private void CheckCompressedUnsignedInteger(int valueToRoundTrip, int numberOfBytesExpected)
        {
            CheckCompressedInteger(valueToRoundTrip, numberOfBytesExpected, CompressUnsignedInteger, ReadCompressedUnsignedInteger);
        }

        private void CheckCompressedInteger(int valueToRoundTrip, int numberOfBytesExpected, Func<int, byte[]> compress, Func<byte[], int> read)
        {
            byte[] bytes = compress(valueToRoundTrip);

            if (bytes == null)
            {
                Assert.Equal(-1, numberOfBytesExpected);
            }
            else
            {
                Assert.Equal(numberOfBytesExpected, bytes.Length);
                Assert.Equal(valueToRoundTrip, read(bytes));
            }
        }

        // NOTE: The compression routines below can be optimized, but please don't do that.
        // The whole idea is to follow the verbal descriptions of the algorithms in 
        // the spec as closely as possible.

        private byte[] CompressUnsignedInteger(int value)
        {
            if (value < 0)
            {
                return null; // too small
            }

            if (value < (1 << 7))
            {
                return EncodeInteger(value, 1);
            }

            if (value < (1 << 14))
            {
                return EncodeInteger(value, 2);
            }

            if (value < (1 << 29))
            {
                return EncodeInteger(value, 4);
            }

            return null; // too big
        }

        private byte[] CompressSignedInteger(int value)
        {
            if (value >= -(1 << 6) && value < (1 << 6))
            {
                return EncodeInteger(Rotate(value, 0x7f), 1);
            }

            if (value >= -(1 << 13) && value < (1 << 13))
            {
                return EncodeInteger(Rotate(value, 0x3fff), 2);
            }

            if (value >= -(1 << 28) && value < (1 << 28))
            {
                return EncodeInteger(Rotate(value, 0x1fffffff), 4);
            }

            return null; // out of compressible range.
        }

        private int Rotate(int value, int mask)
        {
            int signBit = value >= 0 ? 0 : 1;
            value <<= 1;
            value |= signBit;
            value &= mask;
            return value;
        }

        private byte[] EncodeInteger(int value, int byteCount)
        {
            Assert.True(value >= 0);

            switch (byteCount)
            {
                case 1:
                    Assert.True(value < (1 << 7));
                    return new byte[]
                    {
                        // 1 byte encoding: bit 7 clear, 
                        // 7-bit value in bits 0-6
                        (byte)value
                    };

                case 2:
                    Assert.True(value < (1 << 14));
                    return new byte[]
                    {
                        // 2 byte encoding: bit 15 set, bit 14 clear, 
                        // 14-bit value stored big-endian in bits 0-13
                        (byte)(0x80 | ((value >> 8) & 0x3f)),
                        (byte)(       ((value >> 0) & 0xff)),
                    };

                case 4:
                    Assert.True(value < (1 << 29));
                    return new byte[]
                    {
                        // 4 byte encoding: bit 31 set, bit 30 set, bit 29 clear,
                        // 29-bit value stored big-endian in bits 0-28
                        (byte)(0xC0 | ((value >> 24) & 0x1f)),
                        (byte)(       ((value >> 16) & 0xff)),
                        (byte)(       ((value >> 8)  & 0xff)),
                        (byte)(       ((value >> 0)  & 0xff)),
                    };

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int ReadCompressedUnsignedInteger(params byte[] bytes)
        {
            return ReadCompressedInteger(
                bytes,
                delegate (ref BlobReader reader, out int value) { return reader.TryReadCompressedInteger(out value); },
                delegate (ref BlobReader reader) { return reader.ReadCompressedInteger(); });
        }

        private int ReadCompressedSignedInteger(params byte[] bytes)
        {
            return ReadCompressedInteger(
                bytes,
                delegate (ref BlobReader reader, out int value) { return reader.TryReadCompressedSignedInteger(out value); },
                delegate (ref BlobReader reader) { return reader.ReadCompressedSignedInteger(); });
        }

        private delegate bool TryReadFunc(ref BlobReader reader, out int value);
        private delegate int ReadFunc(ref BlobReader reader);

        private unsafe int ReadCompressedInteger(byte[] bytes, TryReadFunc tryRead, ReadFunc read)
        {
            fixed (byte* ptr = bytes)
            {
                var reader = new BlobReader(new MemoryBlock(ptr, bytes.Length));

                int value;
                bool valid = tryRead(ref reader, out value);

                if (valid)
                {
                    Assert.Equal(bytes.Length, reader.Offset);
                    reader.Reset();
                    Assert.Equal(value, read(ref reader));
                    Assert.Equal(bytes.Length, reader.Offset);
                }
                else
                {
                    Assert.Equal(0, reader.Offset);
                    Assert.Throws<BadImageFormatException>(() => reader.ReadCompressedInteger());
                    Assert.Equal(value, BlobReader.InvalidCompressedInteger);
                    Assert.Equal(0, reader.Offset);
                }

                return value;
            }
        }
    }
}
