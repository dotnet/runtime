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
            var capacity = 99;

            Boolean v1 = true; // 1
            Byte v2 = (Byte)123; // 1
            Char v3 = (Char)255; // 1
            Int16 v4 = Int16.MaxValue - 1; // 2
            Int32 v5 = Int32.MinValue; // 4
            Int64 v6 = 12345; // 8
            Decimal v7 = 1.1m; //123.456m; // 16
            Single v8 = 0.000123f; // 4
            Double v9 = 1234567.890; // 8
            SByte v10 = (SByte)(-128); // 1
            UInt16 v11 = UInt16.MinValue; // 2
            UInt32 v12 = 67890; // 4
            UInt64 v13 = UInt32.MaxValue - 12345; // 8

            using (var buffer = new TestSafeBuffer(capacity))
            using (var uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                long pos = 0;
                uma.Write(pos, v1);
                pos += sizeof(Boolean);
                uma.Write(pos, v2);
                pos += sizeof(Byte);
                uma.Write(pos, v3);
                pos += sizeof(Char);
                uma.Write(pos, v4);
                pos += sizeof(Int16);
                uma.Write(pos, v5);
                pos += sizeof(Int32);
                uma.Write(pos, v6);
                pos += sizeof(Int64);
                long pp = pos;
                uma.Write(pos, v7);
                pos += sizeof(Decimal);
                uma.Write(pos, v8);
                pos += sizeof(Single);
                uma.Write(pos, v9);
                pos += sizeof(Double);
                uma.Write(pos, v10);
                pos += sizeof(SByte);
                uma.Write(pos, v11);
                pos += sizeof(UInt16);
                uma.Write(pos, v12);
                pos += sizeof(UInt32);
                uma.Write(pos, v13);

                pos = 0;
                var v01 = uma.ReadBoolean(pos);
                Assert.Equal(v1, v01);
                pos += sizeof(Boolean);
                var v02 = uma.ReadByte(pos);
                Assert.Equal(v2, v02);
                pos += sizeof(Byte);
                var v03 = uma.ReadChar(pos);
                Assert.Equal(v3, v03);
                pos += sizeof(Char);
                var v04 = uma.ReadInt16(pos);
                Assert.Equal(v4, v04);
                pos += sizeof(Int16);
                var v05 = uma.ReadInt32(pos);
                Assert.Equal(v5, v05);
                pos += sizeof(Int32);
                var v06 = uma.ReadInt64(pos);
                Assert.Equal(v6, v06);
                pos += sizeof(Int64);
                var v07 = uma.ReadDecimal(pos);
                Assert.Equal(v7, v07);
                pos += sizeof(Decimal);
                var v08 = uma.ReadSingle(pos);
                Assert.Equal(v8, v08);
                pos += sizeof(Single);
                var v09 = uma.ReadDouble(pos);
                Assert.Equal(v9, v09);
                pos += sizeof(Double);
                var v010 = uma.ReadSByte(pos);
                Assert.Equal(v10, v010);
                pos += sizeof(SByte);
                var v011 = uma.ReadUInt16(pos);
                Assert.Equal(v11, v011);
                pos += sizeof(UInt16);
                var v012 = uma.ReadUInt32(pos);
                Assert.Equal(v12, v012);
                pos += sizeof(UInt32);
                var v013 = uma.ReadUInt64(pos);
                Assert.Equal(v13, v013);
            }
        }
    }
}
