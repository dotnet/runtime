// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class BinaryWriter_WriteTests
    {
        [Fact]
        public void BinaryWriter_WriteBoolTest()
        {
            // [] Write a series of booleans to a stream
            using(Stream mstr = CreateStream())
            using(BinaryWriter dw2 = new BinaryWriter(mstr))
            using(BinaryReader dr2 = new BinaryReader(mstr))
            {
                dw2.Write(false);
                dw2.Write(false);
                dw2.Write(true);
                dw2.Write(false);
                dw2.Write(true);
                dw2.Write(5);
                dw2.Write(0);

                dw2.Flush();
                mstr.Position = 0;

                Assert.False(dr2.ReadBoolean()); //false
                Assert.False(dr2.ReadBoolean()); //false
                Assert.True(dr2.ReadBoolean());  //true
                Assert.False(dr2.ReadBoolean()); //false
                Assert.True(dr2.ReadBoolean());  //true
                Assert.Equal(5, dr2.ReadInt32());  //5
                Assert.Equal(0, dr2.ReadInt32()); //0
            }
        }

        [Fact]
        public void BinaryWriter_WriteSingleTest()
        {
            float[] sglArr = new float[] {
                float.MinValue, float.MaxValue, float.Epsilon, float.PositiveInfinity, float.NegativeInfinity, new float(),
                0, (float)(-1E20), (float)(-3.5E-20), (float)(1.4E-10), (float)10000.2, (float)2.3E30
            };

            WriteTest(sglArr, (bw, s) => bw.Write(s), (br) => br.ReadSingle());
        }

        [Fact]
        public void BinaryWriter_WriteDecimalTest()
        {
            decimal[] decArr = new decimal[] {
                decimal.One, decimal.Zero, decimal.MinusOne, decimal.MinValue, decimal.MaxValue,
                new decimal(-1000.5), new decimal(-10.0E-40), new decimal(3.4E-40898), new decimal(3.4E-28),
                new decimal(3.4E+28), new decimal(0.45), new decimal(5.55), new decimal(3.4899E23)
            };

            WriteTest(decArr, (bw, s) => bw.Write(s), (br) => br.ReadDecimal());
        }

        [Fact]
        public void BinaryWriter_WriteDoubleTest()
        {
            double[] dblArr = new double[] {
                double.NegativeInfinity, double.PositiveInfinity, double.Epsilon, double.MinValue, double.MaxValue,
                -3E59, -1000.5, -1E-40, 3.4E-37, 0.45, 5.55, 3.4899E233
            };

            WriteTest(dblArr, (bw, s) => bw.Write(s), (br) => br.ReadDouble());
        }

        [Fact]
        public void BinaryWriter_WriteHalfTest()
        {
            Half[] hlfArr = new Half[] {
                Half.NegativeInfinity, Half.PositiveInfinity, Half.Epsilon, Half.MinValue, Half.MaxValue,
                (Half)0.45, (Half)5.55
            };

            WriteTest(hlfArr, (bw, s) => bw.Write(s), (br) => br.ReadHalf());
        }

        [Fact]
        public void BinaryWriter_WriteInt16Test()
        {
            short[] i16Arr = new short[] { short.MinValue, short.MaxValue, 0, -10000, 10000, -50, 50 };

            WriteTest(i16Arr, (bw, s) => bw.Write(s), (br) => br.ReadInt16());
        }

        [Fact]
        public void BinaryWriter_WriteInt32Test()
        {
            int[] i32arr = new int[] { int.MinValue, int.MaxValue, 0, -10000, 10000, -50, 50 };

            WriteTest(i32arr, (bw, s) => bw.Write(s), (br) => br.ReadInt32());
        }

        [Fact]
        public void BinaryWriter_Write7BitEncodedIntTest()
        {
            int[] i32arr = new int[]
            {
                int.MinValue, int.MaxValue, 0, -10000, 10000, -50, 50,
                unchecked((int)uint.MinValue), unchecked((int)uint.MaxValue), unchecked((int)(uint.MaxValue - 100))
            };

            WriteTest(i32arr, (bw, s) => bw.Write7BitEncodedInt(s), (br) => br.Read7BitEncodedInt());
        }

        [Fact]
        public void BinaryWriter_WriteInt64Test()
        {
            long[] i64arr = new long[] { long.MinValue, long.MaxValue, 0, -10000, 10000, -50, 50 };

            WriteTest(i64arr, (bw, s) => bw.Write(s), (br) => br.ReadInt64());
        }

        [Fact]
        public void BinaryWriter_Write7BitEncodedInt64Test()
        {
            long[] i64arr = new long[]
            {
                long.MinValue, long.MaxValue, 0, -10000, 10000, -50, 50,
                unchecked((long)ulong.MinValue), unchecked((long)ulong.MaxValue), unchecked((long)(ulong.MaxValue - 100))
            };

            WriteTest(i64arr, (bw, s) => bw.Write7BitEncodedInt64(s), (br) => br.Read7BitEncodedInt64());
        }

        [Fact]
        public void BinaryWriter_WriteUInt16Test()
        {
            ushort[] ui16Arr = new ushort[] { ushort.MinValue, ushort.MaxValue, 0, 100, 1000, 10000, ushort.MaxValue - 100 };

            WriteTest(ui16Arr, (bw, s) => bw.Write(s), (br) => br.ReadUInt16());
        }

        [Fact]
        public void BinaryWriter_WriteUInt32Test()
        {
            uint[] ui32Arr = new uint[] { uint.MinValue, uint.MaxValue, 0, 100, 1000, 10000, uint.MaxValue - 100 };

            WriteTest(ui32Arr, (bw, s) => bw.Write(s), (br) => br.ReadUInt32());
        }

        [Fact]
        public void BinaryWriter_WriteUInt64Test()
        {
            ulong[] ui64Arr = new ulong[] { ulong.MinValue, ulong.MaxValue, 0, 100, 1000, 10000, ulong.MaxValue - 100 };

            WriteTest(ui64Arr, (bw, s) => bw.Write(s), (br) => br.ReadUInt64());
        }

        [Fact]
        public void BinaryWriter_WriteStringTest()
        {
            StringBuilder sb = new StringBuilder();
            string str1;
            for (int ii = 0; ii < 5; ii++)
                sb.Append("abc");
            str1 = sb.ToString();

            string[] strArr = new string[] {
                "ABC", "\t\t\n\n\n\0\r\r\v\v\t\0\rHello", "This is a normal string", "12345667789!@#$%^&&())_+_)@#",
                "ABSDAFJPIRUETROPEWTGRUOGHJDOLJHLDHWEROTYIETYWsdifhsiudyoweurscnkjhdfusiyugjlskdjfoiwueriye", "     ",
                "\0\0\0\t\t\tHey\"\"", "\u0022\u0011", str1, string.Empty };

            WriteTest(strArr, (bw, s) => bw.Write(s), (br) => br.ReadString());
        }

        [Fact]
        public void BinaryWriter_WriteStringTest_Null()
        {
            using (Stream memStream = CreateStream())
            using (BinaryWriter dw2 = new BinaryWriter(memStream))
            {
                Assert.Throws<ArgumentNullException>(() => dw2.Write((string)null));
            }
        }

        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        private void WriteTest<T>(T[] testElements, Action<BinaryWriter, T> write, Func<BinaryReader, T> read)
        {
            // Non-derived BinaryWriter/BinaryReader, UTF-8 encoding
            using (Stream memStream = CreateStream())
            using (var writer = new BinaryWriter(memStream))
            using (var reader = new BinaryReader(memStream))
            {
                WriteTest(memStream, writer, reader, testElements, write, read);
            }

            // Derived BinaryWriter/BinaryReader, UTF-8 encoding
            using (Stream memStream = CreateStream())
            using (var writer = new TestWriter(memStream))
            using (var reader = new TestReader(memStream))
            {
                WriteTest(memStream, writer, reader, testElements, write, read);
            }

            // Non-derived BinaryWriter/BinaryReader, UTF-16 encoding
            using (Stream memStream = CreateStream())
            using (var writer = new BinaryWriter(memStream, Encoding.Unicode))
            using (var reader = new BinaryReader(memStream, Encoding.Unicode))
            {
                WriteTest(memStream, writer, reader, testElements, write, read);
            }

            // Derived BinaryWriter/BinaryReader, UTF-16 encoding
            using (Stream memStream = CreateStream())
            using (var writer = new TestWriter(memStream, Encoding.Unicode))
            using (var reader = new TestReader(memStream, Encoding.Unicode))
            {
                WriteTest(memStream, writer, reader, testElements, write, read);
            }
        }

        private void WriteTest<T>(Stream stream, BinaryWriter writer, BinaryReader reader, T[] testElements, Action<BinaryWriter, T> write, Func<BinaryReader, T> read)
        {
            for (int i = 0; i < testElements.Length; i++)
            {
                write(writer, testElements[i]);
            }

            writer.Flush();
            stream.Position = 0;

            for (int i = 0; i < testElements.Length; i++)
            {
                Assert.Equal(testElements[i], read(reader));
            }

            if (writer is TestWriter derivedWriter && reader is TestReader derivedReader)
            {
                // Checking if the internally tracked positions of a derived reader/writer are in sync (#107265)
                Assert.Equal(derivedReader.Position, derivedWriter.Position);
            }

            // We've reached the end of the stream.  Check for expected EndOfStreamException
            Assert.Throws<EndOfStreamException>(() => read(reader));
        }


        private class TestWriter : BinaryWriter
        {
            private readonly Encoding _encoding;
            public long Position { get; private set; }

            public TestWriter(Stream stream, Encoding? encoding = null)
                : base(stream, encoding ?? Encoding.UTF8)
            {
                _encoding = encoding ?? Encoding.UTF8;
            }

            public override void Write(bool value)
            {
                Advance(sizeof(byte));
                base.Write(value);
            }

            public override void Write(byte value)
            {
                Advance(sizeof(byte));
                base.Write(value);
            }

            public override void Write(byte[] buffer)
            {
                Advance(buffer.Length);
                base.Write(buffer);
            }

            public override void Write(byte[] buffer, int index, int count)
            {
                Advance(count);
                base.Write(buffer, index, count);
            }

            public override void Write(char ch)
            {
                Advance(_encoding.GetBytes([ch]).Length);
                base.Write(ch);
            }

            public override void Write(char[] chars)
            {
                Advance(_encoding.GetBytes(chars).Length);
                base.Write(chars);
            }

            public override void Write(char[] chars, int index, int count)
            {
                Advance(_encoding.GetBytes(chars, index, count).Length);
                base.Write(chars, index, count);
            }

            public override void Write(decimal value)
            {
                Advance(sizeof(decimal));
                base.Write(value);
            }

            public override void Write(double value)
            {
                Advance(sizeof(double));
                base.Write(value);
            }

            public override void Write(float value)
            {
                Advance(sizeof(float));
                base.Write(value);
            }

            public override void Write(int value)
            {
                Advance(sizeof(int));
                base.Write(value);
            }

            public override void Write(long value)
            {
                Advance(sizeof(long));
                base.Write(value);
            }

            public override void Write(sbyte value)
            {
                Advance(sizeof(sbyte));
                base.Write(value);
            }

            public override void Write(short value)
            {
                Advance(sizeof(short));
                base.Write(value);
            }

            public override void Write(string value)
            {
                Advance(_encoding.GetBytes(value).Length);
                base.Write(value);
            }

            public override void Write(uint value)
            {
                Advance(sizeof(uint));
                base.Write(value);
            }

            public override void Write(ulong value)
            {
                Advance(sizeof(ulong));
                base.Write(value);
            }

            public override void Write(ushort value)
            {
                Advance(sizeof(ushort));
                base.Write(value);
            }

            public override unsafe void Write(Half value)
            {
                Advance(sizeof(Half));
                base.Write(value);
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                Advance(buffer.Length);
                base.Write(buffer);
            }

            public override void Write(ReadOnlySpan<char> chars)
            {
                Advance(_encoding.GetBytes(chars.ToArray()).Length);
                base.Write(chars);
            }

            private void Advance(int offset) => Position += offset;
        }

        private class TestReader : BinaryReader
        {
            private readonly Encoding _encoding;
            public long Position { get; private set; }

            public TestReader(Stream s, Encoding? encoding = null)
                : base(s, encoding ?? Encoding.UTF8)
            {
                _encoding = encoding ?? Encoding.UTF8;
            }

            public override int Read()
            {
                var current = BaseStream.Position;
                var result = base.Read();
                Advance(BaseStream.Position - current);
                return result;
            }

            public override int Read(byte[] buffer, int index, int count)
            {
                var result = base.Read(buffer, index, count);
                Advance(result);
                return result;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                var result = base.Read(buffer, index, count);
                Advance(_encoding.GetBytes(buffer, 0, result).Length);
                return result;
            }

            public override bool ReadBoolean()
            {
                Advance(sizeof(bool));
                return base.ReadBoolean();
            }

            public override byte ReadByte()
            {
                Advance(sizeof(byte));
                return base.ReadByte();
            }

            public override byte[] ReadBytes(int count)
            {
                var result = base.ReadBytes(count);
                Advance(result.Length);
                return result;
            }

            public override char ReadChar()
            {
                var result = base.ReadChar();
                Advance(_encoding.GetBytes([result]).Length);
                return result;
            }

            public override char[] ReadChars(int count)
            {
                var result = base.ReadChars(count);
                Advance(_encoding.GetBytes(result).Length);
                return result;
            }

            public override decimal ReadDecimal()
            {
                Advance(sizeof(decimal));
                return base.ReadDecimal();
            }

            public override double ReadDouble()
            {
                Advance(sizeof(double));
                return base.ReadDouble();
            }

            public override short ReadInt16()
            {
                Advance(sizeof(short));
                return base.ReadInt16();
            }

            public override int ReadInt32()
            {
                Advance(sizeof(int));
                return base.ReadInt32();
            }

            public override long ReadInt64()
            {
                Advance(sizeof(long));
                return base.ReadInt64();
            }

            public override sbyte ReadSByte()
            {
                Advance(sizeof(sbyte));
                return base.ReadSByte();
            }

            public override float ReadSingle()
            {
                Advance(sizeof(float));
                return base.ReadSingle();
            }

            public override string ReadString()
            {
                var result = base.ReadString();
                Advance(_encoding.GetBytes(result).Length);
                return result;
            }

            public override ushort ReadUInt16()
            {
                Advance(sizeof(ushort));
                return base.ReadUInt16();
            }

            public override uint ReadUInt32()
            {
                Advance(sizeof(uint));
                return base.ReadUInt32();
            }

            public override ulong ReadUInt64()
            {
                Advance(sizeof(ulong));
                return base.ReadUInt64();
            }

            public override unsafe Half ReadHalf()
            {
                Advance(sizeof(Half));
                return base.ReadHalf();
            }

            public override int Read(Span<byte> buffer)
            {
                var result = base.Read(buffer);
                Advance(result);
                return result;
            }

            public override int Read(Span<char> buffer)
            {
                var result = base.Read(buffer);
                Advance(_encoding.GetBytes(buffer[..result].ToArray()).Length);
                return result;
            }

            private void Advance(long offset) => Position += offset;
        }
    }
}
