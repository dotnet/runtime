// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.IO;
using System.Text;

namespace BinaryWriterTests
{
    public class BinaryWriter_WriteByteCharTests
    {
        /// <summary>
        /// Cases Tested:
        /// 1) Tests that BinaryWriter properly writes chars into a stream.
        /// 2) Tests that if someone writes surrogate characters, an argument exception is thrown
        /// 3) Casting an int to char and writing it, works.
        /// </summary>
        [Fact]
        public static void BinaryWriter_WriteCharTest()
        {
            MemoryStream mstr = new MemoryStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);
            BinaryReader dr2 = new BinaryReader(mstr);

            Char[] chArr = new Char[0];
            int ii = 0;

            // [] Write a series of characters to a MemoryStream and read them back
            chArr = new Char[] { 'A', 'c', '\0', '\u2701', '$', '.', '1', 'l', '\u00FF', '\n', '\t', '\v' };
            for (ii = 0; ii < chArr.Length; ii++)
                dw2.Write(chArr[ii]);

            dw2.Flush();
            mstr.Position = 0;
            for (ii = 0; ii < chArr.Length; ii++)
            {
                char c = dr2.ReadChar();
                Assert.Equal(chArr[ii], c);
            }
            Assert.Throws<EndOfStreamException>(() => dr2.ReadChar());

            dw2.Dispose();
            dr2.Dispose();
            mstr.Dispose();

            //If someone writes out characters using BinaryWriter's Write(char[]) method, they must use something like BinaryReader's ReadChars(int) method to read it back in.  
            //They cannot use BinaryReader's ReadChar().  Similarly, data written using Write(char) can't be read back using ReadChars(int).

            //A high-surrogate is a Unicode code point in the range U+D800 through U+DBFF and a low-surrogate is a Unicode code point in the range U+DC00 through U+DFFF
            Char ch;
            MemoryStream mem = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(mem, Encoding.Unicode);

            //between 1 <= x < 255
            int[] randomNumbers = new int[] { 1, 254, 210, 200, 105, 135, 98, 54 };
            for (int i = 0; i < randomNumbers.Length; i++)
            {
                ch = (Char)randomNumbers[i];
                writer.Write(ch);
            }

            mem.Position = 0;
            writer.Dispose();
            mem.Dispose();
        }

        [Fact]
        public static void BinaryWriter_WriteCharTest_Negative()
        {
            //If someone writes out characters using BinaryWriter's Write(char[]) method, they must use something like BinaryReader's ReadChars(int) method to read it back in.  
            //They cannot use BinaryReader's ReadChar().  Similarly, data written using Write(char) can't be read back using ReadChars(int).

            //A high-surrogate is a Unicode code point in the range U+D800 through U+DBFF and a low-surrogate is a Unicode code point in the range U+DC00 through U+DFFF
            Char ch;
            MemoryStream mem = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(mem, Encoding.Unicode);
            // between 55296 <= x < 56319
            int[] randomNumbers = new int[] { 55296, 56318, 55305, 56019, 55888, 55900, 56251 };
            for (int i = 0; i < randomNumbers.Length; i++)
            {
                ch = (Char)randomNumbers[i];
                Assert.Throws<ArgumentException>(() => writer.Write(ch));
            }
            // between 56320 <= x < 57343
            randomNumbers = new int[] { 56320, 57342, 56431, 57001, 56453, 57245, 57111 };
            for (int i = 0; i < randomNumbers.Length; i++)
            {
                ch = (Char)randomNumbers[i];
                Assert.Throws<ArgumentException>(() => writer.Write(ch));
            }

            writer.Dispose();
            mem.Dispose();
        }

        /// <summary>
        /// Cases Tested:
        /// Writing bytes casted to chars and using a different encoding; iso-2022-jp.
        /// </summary>
        [Fact]
        public static void BinaryWriter_WriteCharTest2()
        {
            // BinaryReader/BinaryWriter don't do well when mixing char or char[] data and binary data.
            // This behavior remains for compat.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Stream stream = new MemoryStream();
            // string name = iso-2022-jp, codepage = 50220 (original test used a code page number).
            // taken from http://msdn.microsoft.com/en-us/library/windows/desktop/dd317756(v=vs.85).aspx
            string codepageName = "iso-2022-jp";
            BinaryWriter writer = new BinaryWriter(stream, Encoding.GetEncoding(codepageName));

            byte[] bytes = { 0x01, 0x02, 0x03, 0x04, 0x05 };

            writer.Write((char)0x30ca);
            writer.Write(bytes);
            writer.Flush();
            writer.Write('\0');

            stream.Seek(0, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader(stream, Encoding.GetEncoding(codepageName));
            char japanese = reader.ReadChar();
            Assert.Equal(0x30ca, (int)japanese);
            byte[] readBytes = reader.ReadBytes(5);
            for (int i = 0; i < 5; i++)
            {
                //We pretty much dont expect this to work
                Assert.NotEqual(readBytes[i], bytes[i]);
            }

            stream.Dispose();
            writer.Dispose();
            reader.Dispose();
        }

        /// <summary>
        /// Testing that bytes can be written to a stream with BinaryWriter.
        /// </summary>
        [Fact]
        public static void BinaryWriter_WriteByteTest()
        {
            int ii = 0;
            Byte[] bytArr = new Byte[] { Byte.MinValue, Byte.MaxValue, 100, 1, 10, Byte.MaxValue / 2, Byte.MaxValue - 100 };

            // [] read/Write with Memorystream
            MemoryStream mstr = new MemoryStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);

            for (ii = 0; ii < bytArr.Length; ii++)
                dw2.Write(bytArr[ii]);

            dw2.Flush();
            mstr.Position = 0;
            BinaryReader dr2 = new BinaryReader(mstr);

            for (ii = 0; ii < bytArr.Length; ii++)
            {
                Assert.Equal(bytArr[ii], dr2.ReadByte());
            }

            // [] Check End Of Stream
            Assert.Throws<EndOfStreamException>(() => dr2.ReadByte());

            dr2.Dispose();
            dw2.Dispose();
            mstr.Dispose();
        }

        /// <summary>
        /// Testing that SBytes can be written to a stream with BinaryWriter.
        /// </summary>
        [Fact]
        public static void BinaryWriter_WriteSByteTest()
        {
            int ii = 0;
            SByte[] sbArr = new SByte[] { 
                SByte.MinValue, SByte.MaxValue, -100, 100, 0, SByte.MinValue / 2, SByte.MaxValue / 2, 
                10, 20, 30, -10, -20, -30, SByte.MaxValue - 100 };

            // [] read/Write with Memorystream
            MemoryStream mstr = new MemoryStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);

            for (ii = 0; ii < sbArr.Length; ii++)
                dw2.Write(sbArr[ii]);

            dw2.Flush();
            mstr.Position = 0;
            BinaryReader dr2 = new BinaryReader(mstr);

            for (ii = 0; ii < sbArr.Length; ii++)
            {
                Assert.Equal(sbArr[ii], dr2.ReadSByte());
            }

            dr2.Dispose();
            dw2.Dispose();
            mstr.Dispose();
        }

        /// <summary>
        /// Testing that an ArgumentException is thrown when Sbytes are written to a stream
        /// and read past the end of that stream.
        /// </summary>
        [Fact]
        public static void BinaryWriter_WriteSByteTest_NegativeCase()
        {
            int ii = 0;
            SByte[] sbArr = new SByte[] { 
                SByte.MinValue, SByte.MaxValue, -100, 100, 0, SByte.MinValue / 2, SByte.MaxValue / 2, 
                10, 20, 30, -10, -20, -30, SByte.MaxValue - 100 };

            MemoryStream mstr = new MemoryStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);

            for (ii = 0; ii < sbArr.Length; ii++)
                dw2.Write(sbArr[ii]);

            dw2.Flush();

            BinaryReader dr2 = new BinaryReader(mstr);
            // [] Check End Of Stream
            Assert.Throws<EndOfStreamException>(() => dr2.ReadSByte());

            dr2.Dispose();
            dw2.Dispose();
            mstr.Dispose();
        }

        /// <summary>
        /// Testing that a byte[] can be written to a stream with BinaryWriter.
        /// </summary>
        [Fact]
        public static void BinaryWriter_WriteBArrayTest()
        {
            int ii = 0;
            Byte[] bytArr = new Byte[] { Byte.MinValue, Byte.MaxValue, 1, 5, 10, 100, 200 };

            // [] read/Write with Memorystream
            MemoryStream mstr = new MemoryStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);

            dw2.Write(bytArr);
            dw2.Flush();
            mstr.Position = 0;

            BinaryReader dr2 = new BinaryReader(mstr);

            for (ii = 0; ii < bytArr.Length; ii++)
            {
                Assert.Equal(bytArr[ii], dr2.ReadByte());
            }

            // [] Check End Of Stream
            Assert.Throws<EndOfStreamException>(() => dr2.ReadByte());

            mstr.Dispose();
            dw2.Dispose();
            dr2.Dispose();
        }

        [Fact]
        public static void BinaryWriter_WriteBArrayTest_Negative()
        {
            int[] iArrInvalidValues = new Int32[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, Int32.MinValue, Int16.MinValue };
            int[] iArrLargeValues = new Int32[] { Int32.MaxValue, Int32.MaxValue - 1, Int32.MaxValue / 2, Int32.MaxValue / 10, Int32.MaxValue / 100 };
            Byte[] bArr = new Byte[0];
            // [] ArgumentNullException for null argument
            MemoryStream mstr = new MemoryStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);

            Assert.Throws<ArgumentNullException>(() => dw2.Write((Byte[])null));
            mstr.Dispose();
            dw2.Dispose();

            // [] ArgumentNullException for null argument
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            Assert.Throws<ArgumentNullException>(() => dw2.Write((Byte[])null, 0, 0));

            dw2.Dispose();
            mstr.Dispose();

            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            for (int iLoop = 0; iLoop < iArrInvalidValues.Length; iLoop++)
            {
                // [] ArgumentOutOfRange for negative offset
                Assert.Throws<ArgumentOutOfRangeException>(() => dw2.Write(bArr, iArrInvalidValues[iLoop], 0));
                // [] ArgumentOutOfRangeException for negative count
                Assert.Throws<ArgumentOutOfRangeException>(() => dw2.Write(bArr, 0, iArrInvalidValues[iLoop]));
            }
            dw2.Dispose();
            mstr.Dispose();

            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            for (int iLoop = 0; iLoop < iArrLargeValues.Length; iLoop++)
            {
                // [] Offset out of range
                Assert.Throws<ArgumentException>(() => dw2.Write(bArr, iArrLargeValues[iLoop], 0));
                // [] Invalid count value
                Assert.Throws<ArgumentException>(() => dw2.Write(bArr, 0, iArrLargeValues[iLoop]));
            }
            dw2.Dispose();
            mstr.Dispose();
        }

        /// <summary>
        /// Cases Tested:
        /// 1) Testing that bytes can be written to a stream with BinaryWriter.
        /// 2) Tests exceptional scenarios.
        /// </summary>
        [Fact]
        public static void BinaryWriter_WriteBArrayTest2()
        {
            BinaryWriter dw2 = null;
            BinaryReader dr2 = null;
            MemoryStream mstr = null;
            Byte[] bArr = new Byte[0];
            int ii = 0;
            Byte[] bReadArr = new Byte[0];
            Int32 ReturnValue;

            bArr = new Byte[1000];
            bArr[0] = Byte.MinValue;
            bArr[1] = Byte.MaxValue;

            for (ii = 2; ii < 1000; ii++)
                bArr[ii] = (Byte)(ii % 255);

            // []read/ Write character values 0-1000 with  Memorystream
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);

            dw2.Write(bArr, 0, bArr.Length);
            dw2.Flush();
            mstr.Position = 0;

            dr2 = new BinaryReader(mstr);
            bReadArr = new Byte[bArr.Length];
            ReturnValue = dr2.Read(bReadArr, 0, bArr.Length);

            Assert.Equal(bArr.Length, ReturnValue);

            for (ii = 0; ii < bArr.Length; ii++)
            {
                Assert.Equal(bArr[ii], bReadArr[ii]);
            }

            dw2.Dispose();
            dr2.Dispose();
            mstr.Dispose();
        }

        /// <summary>
        /// Cases Tested:
        /// 1) Testing that char[] can be written to a stream with BinaryWriter.
        /// 2) Tests exceptional scenarios.
        /// </summary>
        [Fact]
        public static void BinaryWriter_WriteCharArrayTest()
        {
            int ii = 0;
            Char[] chArr = new Char[1000];
            chArr[0] = Char.MinValue;
            chArr[1] = Char.MaxValue;
            chArr[2] = '1';
            chArr[3] = 'A';
            chArr[4] = '\0';
            chArr[5] = '#';
            chArr[6] = '\t';

            for (ii = 7; ii < 1000; ii++)
                chArr[ii] = (Char)ii;

            // [] read/Write with Memorystream
            MemoryStream mstr = new MemoryStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);

            dw2.Write(chArr);
            dw2.Flush();
            mstr.Position = 0;

            BinaryReader dr2 = new BinaryReader(mstr);

            for (ii = 0; ii < chArr.Length; ii++)
            {
                Assert.Equal(chArr[ii], dr2.ReadChar());
            }

            // [] Check End Of Stream
            Assert.Throws<EndOfStreamException>(() => dr2.ReadChar());

            dw2.Dispose();
            dr2.Dispose();
            mstr.Dispose();
        }

        [Fact]
        public static void BinaryWriter_WriteCharArrayTest_Negative()
        {
            int[] iArrInvalidValues = new Int32[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, Int32.MinValue, Int16.MinValue };
            int[] iArrLargeValues = new Int32[] { Int32.MaxValue, Int32.MaxValue - 1, Int32.MaxValue / 2, Int32.MaxValue / 10, Int32.MaxValue / 100 };
            Char[] chArr = new Char[1000];

            // [] ArgumentNullException for null argument
            MemoryStream mstr = new MemoryStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);
            Assert.Throws<ArgumentNullException>(() => dw2.Write((Char[])null));
            dw2.Dispose();
            mstr.Dispose();


            // [] ArgumentNullException for null argument
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            Assert.Throws<ArgumentNullException>(() => dw2.Write((Char[])null, 0, 0));

            mstr.Dispose();
            dw2.Dispose();

            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);

            for (int iLoop = 0; iLoop < iArrInvalidValues.Length; iLoop++)
            {
                // [] ArgumentOutOfRange for negative offset
                Assert.Throws<ArgumentOutOfRangeException>(() => dw2.Write(chArr, iArrInvalidValues[iLoop], 0));
                // [] negative count.
                Assert.Throws<ArgumentOutOfRangeException>(() => dw2.Write(chArr, 0, iArrInvalidValues[iLoop]));
            }
            mstr.Dispose();
            dw2.Dispose();

            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            for (int iLoop = 0; iLoop < iArrLargeValues.Length; iLoop++)
            {
                // [] Offset out of range
                Assert.Throws<ArgumentOutOfRangeException>(() => dw2.Write(chArr, iArrLargeValues[iLoop], 0));
                // [] Invalid count value
                Assert.Throws<ArgumentOutOfRangeException>(() => dw2.Write(chArr, 0, iArrLargeValues[iLoop]));
            }
            mstr.Dispose();
            dw2.Dispose();
        }

        /// <summary>
        /// Cases Tested:
        /// If someone writes out characters using BinaryWriter's Write(char[]) method, they must use something like BinaryReader's ReadChars(int) method to read it back in.  
        /// They cannot use BinaryReader's ReadChar().  Similarly, data written using Write(char) can't be read back using ReadChars(int).
        /// A high-surrogate is a Unicode code point in the range U+D800 through U+DBFF and a low-surrogate is a Unicode code point in the range U+DC00 through U+DFFF
        /// 
        /// We dont throw on the second read but then throws continuously - note the loop count difference in the 2 loops
        /// 
        /// BinaryReader was reverting to its original location instead of advancing. This was changed to skip past the char in the surrogate range.
        /// The affected method is InternalReadOneChar (IROC). Note that the work here is slightly complicated by the way surrogates are handled by 
        /// the decoding classes. When IROC calls decoder.GetChars(), if the bytes passed in are surrogates, UnicodeEncoding doesn't report it. 
        /// charsRead would end up being one value, and since BinaryReader doesn't have the logic telling it exactly how many bytes total to expect, 
        /// it calls GetChars in a second loop. In that loop, UnicodeEncoding matches up a surrogate pair. If it realizes it's too big for the encoding, 
        /// then it throws an ArgumentException (chars overflow). This meant that BinaryReader.IROC is advancing past two chars in the surrogate 
        /// range, which is why the position actually needs to be moved back (but not past the first surrogate char).  
        /// 
        /// Note that UnicodeEncoding doesn't always throw when it encounters two successive chars in the surrogate range. The exception 
        /// encountered here happens if it finds a valid pair but then determines it's too long. If the pair isn't valid (a low then a high), 
        /// then it returns 0xfffd, which is why BinaryReader.ReadChar needs to do an explicit check. (It always throws when it encounters a surrogate)
        /// </summary>
        [Fact]
        public static void BinaryWriter_WriteCharArrayTest2()
        {

            MemoryStream mem = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(mem, Encoding.Unicode);

            // between 55296 <= x < 56319

            // between 56320 <= x < 57343
            Char[] randomChars = new Char[] { 
                (Char)55296, (Char)57297, (Char)55513, (Char)56624, (Char)55334, (Char)56957, (Char)55857, 
                (Char)56355, (Char)56095, (Char)56887, (Char) 56126, (Char) 56735, (Char)55748, (Char)56405,
                (Char)55787, (Char)56707, (Char) 56300, (Char)56417, (Char)55465, (Char)56944
            };

            writer.Write(randomChars);
            mem.Position = 0;
            BinaryReader reader = new BinaryReader(mem, Encoding.Unicode);

            for (int i = 0; i < 50; i++)
            {
                try
                {
                    reader.ReadChar();

                    Assert.Equal(1, i);
                }
                catch (ArgumentException)
                {
                    // ArgumentException is sometimes thrown on ReadChar() due to the 
                    // behavior outlined in the method summary.
                }
            }

            Char[] chars = reader.ReadChars(randomChars.Length);
            for (int i = 0; i < randomChars.Length; i++)
                Assert.Equal(randomChars[i], chars[i]);

            reader.Dispose();
            writer.Dispose();
        }

        /// <summary>
        /// Cases Tested:
        /// 1) Tests that char[] can be written to a stream with BinaryWriter.
        /// 2) Tests Exceptional cases.
        /// </summary>
        [Fact]
        public static void BinaryWriter_WriteCharArrayTest3()
        {
            BinaryWriter dw2 = null;
            BinaryReader dr2 = null;
            MemoryStream mstr = null;
            Char[] chArr = new Char[0];
            int ii = 0;
            Char[] chReadArr = new Char[0];
            Int32 ReturnValue;

            chArr = new Char[1000];
            chArr[0] = Char.MinValue;
            chArr[1] = Char.MaxValue;
            chArr[2] = '1';
            chArr[3] = 'A';
            chArr[4] = '\0';
            chArr[5] = '#';
            chArr[6] = '\t';

            for (ii = 7; ii < 1000; ii++)
                chArr[ii] = (Char)ii;

            // []read/ Write character values 0-1000 with  Memorystream

            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);

            dw2.Write(chArr, 0, chArr.Length);
            dw2.Flush();
            mstr.Position = 0;

            dr2 = new BinaryReader(mstr);
            chReadArr = new Char[chArr.Length];
            ReturnValue = dr2.Read(chReadArr, 0, chArr.Length);
            Assert.Equal(chArr.Length, ReturnValue);

            for (ii = 0; ii < chArr.Length; ii++)
            {
                Assert.Equal(chArr[ii], chReadArr[ii]);
            }

            mstr.Dispose();
            dw2.Dispose();
        }
    }
}
