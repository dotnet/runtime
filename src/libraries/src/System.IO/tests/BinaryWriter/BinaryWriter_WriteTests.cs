// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.IO;
using System.Text;

namespace BinaryWriterTests
{
    public class BinaryWriter_WriteTests
    {

        [Fact]
        public static void BinaryWriter_WriteBoolTest()
        {
            // [] Write a series of booleans to a stream
            using(MemoryStream mstr = new MemoryStream())
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
                Assert.True(dr2.ReadBoolean());  //5
                Assert.False(dr2.ReadBoolean()); //0
            }
        }

        [Fact]
        public static void BinaryWriter_WriteSingleTest()
        {
            Single[] sglArr = new Single[] { 
                Single.MinValue, Single.MaxValue, Single.Epsilon, Single.PositiveInfinity, Single.NegativeInfinity, new Single(), 
                0, (Single)(-1E20), (Single)(-3.5E-20), (Single)(1.4E-10), (Single)10000.2, (Single)2.3E30 
            };

            WriteTest<Single>(sglArr, (bw, s) => bw.Write(s), (br) => br.ReadSingle());
        }

        [Fact]
        public static void BinaryWriter_WriteDecimalTest()
        {
            Decimal[] decArr = new Decimal[] { 
                Decimal.One, Decimal.Zero, Decimal.MinusOne, Decimal.MinValue, Decimal.MaxValue, 
                new Decimal(-1000.5), new Decimal(-10.0E-40), new Decimal(3.4E-40898), new Decimal(3.4E-28), 
                new Decimal(3.4E+28), new Decimal(0.45), new Decimal(5.55), new Decimal(3.4899E23) 
            };

            WriteTest<Decimal>(decArr, (bw, s) => bw.Write(s), (br) => br.ReadDecimal());
        }

        [Fact]
        public static void BinaryWriter_WriteDoubleTest()
        {
            Double[] dblArr = new Double[] { 
                Double.NegativeInfinity, Double.PositiveInfinity, Double.Epsilon, Double.MinValue, Double.MaxValue, 
                -3E59, -1000.5, -1E-40, 3.4E-37, 0.45, 5.55, 3.4899E233 
            };

            WriteTest<Double>(dblArr, (bw, s) => bw.Write(s), (br) => br.ReadDouble());
        }

        [Fact]
        public static void BinaryWriter_WriteInt16Test()
        {
            Int16[] i16Arr = new Int16[] { Int16.MinValue, Int16.MaxValue, 0, -10000, 10000, -50, 50 };

            WriteTest<Int16>(i16Arr, (bw, s) => bw.Write(s), (br) => br.ReadInt16());
        }

        [Fact]
        public static void BinaryWriter_WriteInt32Test()
        {
            Int32[] i32arr = new Int32[] { Int32.MinValue, Int32.MaxValue, 0, -10000, 10000, -50, 50 };

            WriteTest<Int32>(i32arr, (bw, s) => bw.Write(s), (br) => br.ReadInt32());
        }

        [Fact]
        public static void BinaryWriter_WriteInt64Test()
        {
            Int64[] i64arr = new Int64[] { Int64.MinValue, Int64.MaxValue, 0, -10000, 10000, -50, 50 };

            WriteTest<Int64>(i64arr, (bw, s) => bw.Write(s), (br) => br.ReadInt64());
        }

        [Fact]
        public static void BinaryWriter_WriteUInt16Test()
        {
            UInt16[] ui16Arr = new UInt16[] { UInt16.MinValue, UInt16.MaxValue, 0, 100, 1000, 10000, UInt16.MaxValue - 100 };

            WriteTest<UInt16>(ui16Arr, (bw, s) => bw.Write(s), (br) => br.ReadUInt16());
        }

        [Fact]
        public static void BinaryWriter_WriteUInt32Test()
        {
            UInt32[] ui32Arr = new UInt32[] { UInt32.MinValue, UInt32.MaxValue, 0, 100, 1000, 10000, UInt32.MaxValue - 100 };

            WriteTest<UInt32>(ui32Arr, (bw, s) => bw.Write(s), (br) => br.ReadUInt32());
        }

        [Fact]
        public static void BinaryWriter_WriteUInt64Test()
        {
            UInt64[] ui64Arr = new UInt64[] { UInt64.MinValue, UInt64.MaxValue, 0, 100, 1000, 10000, UInt64.MaxValue - 100 };

            WriteTest<UInt64>(ui64Arr, (bw, s) => bw.Write(s), (br) => br.ReadUInt64());
        }

        [Fact]
        public static void BinaryWriter_WriteStringTest()
        {
            StringBuilder sb = new StringBuilder();
            String str1;
            for (int ii = 0; ii < 5; ii++)
                sb.Append("abc");
            str1 = sb.ToString();

            String[] strArr = new String[] { 
                "ABC", "\t\t\n\n\n\0\r\r\v\v\t\0\rHello", "This is a normal string", "12345667789!@#$%^&&())_+_)@#", 
                "ABSDAFJPIRUETROPEWTGRUOGHJDOLJHLDHWEROTYIETYWsdifhsiudyoweurscnkjhdfusiyugjlskdjfoiwueriye", "     ", 
                "\0\0\0\t\t\tHey\"\"", "\u0022\u0011", str1, String.Empty };

            WriteTest<String>(strArr, (bw, s) => bw.Write(s), (br) => br.ReadString());
        }

        private static void WriteTest<T>(T[] testElements, Action<BinaryWriter, T> write, Func<BinaryReader, T> read)
        {
            using (MemoryStream memStream = new MemoryStream())
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
