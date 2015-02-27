// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.Tests
{
    public class UmaTests
    {
        [Fact]
        public static void UmaInvalidReadWrite()
        {
            var capacity = 99;
            FakeSafeBuffer sbuf = new FakeSafeBuffer((ulong)capacity);

            using (var uma = new UnmanagedMemoryAccessor(sbuf, 0, capacity, FileAccess.ReadWrite))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => uma.ReadChar(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => uma.ReadDecimal(capacity));
                Assert.Throws<ArgumentException>(() => uma.ReadSingle(capacity - 1));

                Assert.Throws<ArgumentOutOfRangeException>(() => uma.Write(-1, true));
                Assert.Throws<ArgumentOutOfRangeException>(() => uma.Write(capacity, 12345));
                Assert.Throws<ArgumentException>(() => uma.Write(capacity - 1, 0.123));

                uma.Dispose();
                Assert.Throws<ObjectDisposedException>(() => uma.ReadByte(0));
                Assert.Throws<ObjectDisposedException>(() => uma.Write(0, (byte)123));
            }

            using (var uma = new UnmanagedMemoryAccessor(sbuf, 0, capacity, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => uma.ReadInt16(0));
            }

            using (var uma = new UnmanagedMemoryAccessor(sbuf, 0, capacity, FileAccess.Read))
            {
                Assert.Throws<NotSupportedException>(() => uma.Write(0, (int)123));
            }
        }

        [Fact]
        public static void UmaReadWrite()
        {
            var capacity = 199;

            const bool expectedBool = true; // 1
            const byte expectedByte = 123; // 1
            const sbyte expectedSByte = -128; // 1
            const char expectedChar = (char)255; // 2
            const ushort expectedUInt16 = ushort.MinValue; // 2
            const short expectedInt16 = short.MaxValue - 1; // 2
            const uint expectedUInt32 = 67890; // 4
            const int expectedInt32 = int.MinValue; // 4
            const float expectedSingle = 0.000123f; // 4
            const ulong expectedUInt64 = ulong.MaxValue - 12345; // 8
            const long expectedInt64 = 12345; // 8
            const double expectedDouble = 1234567.890; // 8
            const decimal expectedDecimal = 1.1m; //123.456m; // 16

            using (var buffer = new TestSafeBuffer(capacity))
            using (var uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                long pos = 0;

                uma.Write(pos, expectedBool);
                pos += sizeof(bool);

                uma.Write(pos, expectedByte);
                pos += sizeof(byte);

                uma.Write(pos, expectedSByte);
                pos += sizeof(sbyte);

                pos = EnsureAligned(pos, sizeof(char));
                uma.Write(pos, expectedChar);
                pos += sizeof(char);

                pos = EnsureNotAligned(pos, sizeof(char));
                uma.Write(pos, expectedChar);
                pos += sizeof(char);

                pos = EnsureAligned(pos, sizeof(ushort));
                uma.Write(pos, expectedUInt16);
                pos += sizeof(ushort);

                pos = EnsureNotAligned(pos, sizeof(ushort));
                uma.Write(pos, expectedUInt16);
                pos += sizeof(ushort);

                pos = EnsureAligned(pos, sizeof(short));
                uma.Write(pos, expectedInt16);
                pos += sizeof(short);

                pos = EnsureNotAligned(pos, sizeof(short));
                uma.Write(pos, expectedInt16);
                pos += sizeof(short);

                pos = EnsureAligned(pos, sizeof(uint));
                uma.Write(pos, expectedUInt32);
                pos += sizeof(uint);

                pos = EnsureNotAligned(pos, sizeof(uint));
                uma.Write(pos, expectedUInt32);
                pos += sizeof(uint);

                pos = EnsureAligned(pos, sizeof(int));
                uma.Write(pos, expectedInt32);
                pos += sizeof(int);

                pos = EnsureNotAligned(pos, sizeof(int));
                uma.Write(pos, expectedInt32);
                pos += sizeof(int);

                pos = EnsureAligned(pos, sizeof(float));
                uma.Write(pos, expectedSingle);
                pos += sizeof(float);

                pos = EnsureNotAligned(pos, sizeof(float));
                uma.Write(pos, expectedSingle);
                pos += sizeof(float);

                pos = EnsureAligned(pos, sizeof(ulong));
                uma.Write(pos, expectedUInt64);
                pos += sizeof(ulong);

                pos = EnsureNotAligned(pos, sizeof(ulong));
                uma.Write(pos, expectedUInt64);
                pos += sizeof(ulong);

                pos = EnsureAligned(pos, sizeof(long));
                uma.Write(pos, expectedInt64);
                pos += sizeof(long);

                pos = EnsureNotAligned(pos, sizeof(long));
                uma.Write(pos, expectedInt64);
                pos += sizeof(long);

                pos = EnsureAligned(pos, sizeof(double));
                uma.Write(pos, expectedDouble);
                pos += sizeof(double);

                pos = EnsureNotAligned(pos, sizeof(double));
                uma.Write(pos, expectedDouble);
                pos += sizeof(double);

                pos = EnsureAligned(pos, sizeof(decimal));
                uma.Write(pos, expectedDecimal);
                pos += sizeof(decimal);

                pos = EnsureNotAligned(pos, sizeof(decimal));
                uma.Write(pos, expectedDecimal);

                pos = 0;
                Assert.Equal(expectedBool, uma.ReadBoolean(pos));
                pos += sizeof(bool);

                Assert.Equal(expectedByte, uma.ReadByte(pos));
                pos += sizeof(byte);

                Assert.Equal(expectedSByte, uma.ReadSByte(pos));
                pos += sizeof(sbyte);

                pos = EnsureAligned(pos, sizeof(char));
                Assert.Equal(expectedChar, uma.ReadChar(pos));
                pos += sizeof(char);

                pos = EnsureNotAligned(pos, sizeof(char));
                Assert.Equal(expectedChar, uma.ReadChar(pos));
                pos += sizeof(char);

                pos = EnsureAligned(pos, sizeof(ushort));
                Assert.Equal(expectedUInt16, uma.ReadUInt16(pos));
                pos += sizeof(ushort);

                pos = EnsureNotAligned(pos, sizeof(ushort));
                Assert.Equal(expectedUInt16, uma.ReadUInt16(pos));
                pos += sizeof(ushort);

                pos = EnsureAligned(pos, sizeof(short));
                Assert.Equal(expectedInt16, uma.ReadInt16(pos));
                pos += sizeof(short);

                pos = EnsureNotAligned(pos, sizeof(short));
                Assert.Equal(expectedInt16, uma.ReadInt16(pos));
                pos += sizeof(short);

                pos = EnsureAligned(pos, sizeof(uint));
                Assert.Equal(expectedUInt32, uma.ReadUInt32(pos));
                pos += sizeof(uint);

                pos = EnsureNotAligned(pos, sizeof(uint));
                Assert.Equal(expectedUInt32, uma.ReadUInt32(pos));
                pos += sizeof(uint);

                pos = EnsureAligned(pos, sizeof(int));
                Assert.Equal(expectedInt32, uma.ReadInt32(pos));
                pos += sizeof(int);

                pos = EnsureNotAligned(pos, sizeof(int));
                Assert.Equal(expectedInt32, uma.ReadInt32(pos));
                pos += sizeof(int);

                pos = EnsureAligned(pos, sizeof(float));
                Assert.Equal(expectedSingle, uma.ReadSingle(pos));
                pos += sizeof(float);

                pos = EnsureNotAligned(pos, sizeof(float));
                Assert.Equal(expectedSingle, uma.ReadSingle(pos));
                pos += sizeof(float);

                pos = EnsureAligned(pos, sizeof(ulong));
                Assert.Equal(expectedUInt64, uma.ReadUInt64(pos));
                pos += sizeof(ulong);

                pos = EnsureNotAligned(pos, sizeof(ulong));
                Assert.Equal(expectedUInt64, uma.ReadUInt64(pos));
                pos += sizeof(ulong);

                pos = EnsureAligned(pos, sizeof(long));
                Assert.Equal(expectedInt64, uma.ReadInt64(pos));
                pos += sizeof(long);

                pos = EnsureNotAligned(pos, sizeof(long));
                Assert.Equal(expectedInt64, uma.ReadInt64(pos));
                pos += sizeof(long);

                pos = EnsureAligned(pos, sizeof(double));
                Assert.Equal(expectedDouble, uma.ReadDouble(pos));
                pos += sizeof(double);

                pos = EnsureNotAligned(pos, sizeof(double));
                Assert.Equal(expectedDouble, uma.ReadDouble(pos));
                pos += sizeof(double);

                pos = EnsureAligned(pos, sizeof(decimal));
                Assert.Equal(expectedDecimal, uma.ReadDecimal(pos));
                pos += sizeof(decimal);

                pos = EnsureNotAligned(pos, sizeof(decimal));
                Assert.Equal(expectedDecimal, uma.ReadDecimal(pos));
            }
        }

        private static long EnsureAligned(long position, int n)
        {
            return ((position & (n - 1)) == 0) ? position : (position / n + 1) * n;
        }

        private static long EnsureNotAligned(long position, int n)
        {
            return ((position & (n - 1)) != 0) ? position : ++position;
        }
    }
}
