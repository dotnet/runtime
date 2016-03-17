// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.IO;
using System.Text;

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
        public void BinaryWriter_WriteInt64Test()
        {
            long[] i64arr = new long[] { long.MinValue, long.MaxValue, 0, -10000, 10000, -50, 50 };

            WriteTest(i64arr, (bw, s) => bw.Write(s), (br) => br.ReadInt64());
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
            using (Stream memStream = CreateStream())
            using (BinaryWriter writer = new BinaryWriter(memStream))
            using (BinaryReader reader = new BinaryReader(memStream))
            {
                for (int i = 0; i < testElements.Length; i++)
                {
                    write(writer, testElements[i]);
                }

                writer.Flush();
                memStream.Position = 0;

                for (int i = 0; i < testElements.Length; i++)
                {
                    Assert.Equal(testElements[i], read(reader));
                }

                // We've reached the end of the stream.  Check for expected EndOfStreamException
                Assert.Throws<EndOfStreamException>(() => read(reader));
            }
        }
    }
}
