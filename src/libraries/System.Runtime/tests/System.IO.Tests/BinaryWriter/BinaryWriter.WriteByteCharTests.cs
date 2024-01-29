// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class BinaryWriter_WriteByteCharTests
    {
        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        /// <summary>
         /// Cases Tested:
         /// 1) Tests that BinaryWriter properly writes chars into a stream.
         /// 2) Tests that if someone writes surrogate characters, an argument exception is thrown
         /// 3) Casting an int to char and writing it, works.
         /// </summary>
        [Fact]
        public void BinaryWriter_WriteCharTest()
        {
            Stream mstr = CreateStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);
            BinaryReader dr2 = new BinaryReader(mstr);

            char[] chArr = new char[0];
            int ii = 0;

            // [] Write a series of characters to a MemoryStream and read them back
            chArr = new char[] { 'A', 'c', '\0', '\u2701', '$', '.', '1', 'l', '\u00FF', '\n', '\t', '\v' };
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
            char ch;
            Stream mem = CreateStream();
            BinaryWriter writer = new BinaryWriter(mem, Encoding.Unicode);

            //between 1 <= x < 255
            int[] randomNumbers = new int[] { 1, 254, 210, 200, 105, 135, 98, 54 };
            for (int i = 0; i < randomNumbers.Length; i++)
            {
                ch = (char)randomNumbers[i];
                writer.Write(ch);
            }

            mem.Position = 0;
            writer.Dispose();
            mem.Dispose();
        }

        [Fact]
        public void BinaryWriter_WriteCharTest_Negative()
        {
            //If someone writes out characters using BinaryWriter's Write(char[]) method, they must use something like BinaryReader's ReadChars(int) method to read it back in.
            //They cannot use BinaryReader's ReadChar().  Similarly, data written using Write(char) can't be read back using ReadChars(int).

            //A high-surrogate is a Unicode code point in the range U+D800 through U+DBFF and a low-surrogate is a Unicode code point in the range U+DC00 through U+DFFF
            char ch;
            Stream mem = CreateStream();
            BinaryWriter writer = new BinaryWriter(mem, Encoding.Unicode);
            // between 55296 <= x < 56319
            int[] randomNumbers = new int[] { 55296, 56318, 55305, 56019, 55888, 55900, 56251 };
            for (int i = 0; i < randomNumbers.Length; i++)
            {
                ch = (char)randomNumbers[i];
                AssertExtensions.Throws<ArgumentException>(null, () => writer.Write(ch));
            }
            // between 56320 <= x < 57343
            randomNumbers = new int[] { 56320, 57342, 56431, 57001, 56453, 57245, 57111 };
            for (int i = 0; i < randomNumbers.Length; i++)
            {
                ch = (char)randomNumbers[i];
                AssertExtensions.Throws<ArgumentException>(null, () => writer.Write(ch));
            }

            writer.Dispose();
            mem.Dispose();
        }

        /// <summary>
        /// Cases Tested:
        /// Writing bytes casted to chars and using a different encoding; iso-2022-jp.
        /// </summary>
        [Fact]
        public void BinaryWriter_WriteCharTest2()
        {
            // BinaryReader/BinaryWriter don't do well when mixing char or char[] data and binary data.
            // This behavior remains for compat.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Stream stream = CreateStream();
            // string name = iso-2022-jp, codepage = 50220 (original test used a code page number).
            // taken from https://docs.microsoft.com/en-us/windows/desktop/Intl/code-page-identifiers
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
                //We pretty much don't expect this to work
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
        public void BinaryWriter_WriteByteTest()
        {
            int ii = 0;
            byte[] bytArr = new byte[] { byte.MinValue, byte.MaxValue, 100, 1, 10, byte.MaxValue / 2, byte.MaxValue - 100 };

            // [] read/Write with Memorystream
            Stream mstr = CreateStream();
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
        public void BinaryWriter_WriteSByteTest()
        {
            int ii = 0;
            sbyte[] sbArr = new sbyte[] {
                sbyte.MinValue, sbyte.MaxValue, -100, 100, 0, sbyte.MinValue / 2, sbyte.MaxValue / 2,
                10, 20, 30, -10, -20, -30, sbyte.MaxValue - 100 };

            // [] read/Write with Memorystream
            Stream mstr = CreateStream();
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
        public void BinaryWriter_WriteSByteTest_NegativeCase()
        {
            int ii = 0;
            sbyte[] sbArr = new sbyte[] {
                sbyte.MinValue, sbyte.MaxValue, -100, 100, 0, sbyte.MinValue / 2, sbyte.MaxValue / 2,
                10, 20, 30, -10, -20, -30, sbyte.MaxValue - 100 };

            Stream mstr = CreateStream();
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
        public void BinaryWriter_WriteBArrayTest()
        {
            int ii = 0;
            byte[] bytArr = new byte[] { byte.MinValue, byte.MaxValue, 1, 5, 10, 100, 200 };

            // [] read/Write with Memorystream
            Stream mstr = CreateStream();
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
        public void BinaryWriter_WriteBArrayTest_Negative()
        {
            int[] iArrInvalidValues = new int[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, int.MinValue, short.MinValue };
            int[] iArrLargeValues = new int[] { int.MaxValue, int.MaxValue - 1, int.MaxValue / 2, int.MaxValue / 10, int.MaxValue / 100 };
            byte[] bArr = new byte[0];
            // [] ArgumentNullException for null argument
            Stream mstr = CreateStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);

            Assert.Throws<ArgumentNullException>(() => dw2.Write((byte[])null));
            mstr.Dispose();
            dw2.Dispose();

            // [] ArgumentNullException for null argument
            mstr = CreateStream();
            dw2 = new BinaryWriter(mstr);
            Assert.Throws<ArgumentNullException>(() => dw2.Write((byte[])null, 0, 0));

            dw2.Dispose();
            mstr.Dispose();

            mstr = CreateStream();
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

            mstr = CreateStream();
            dw2 = new BinaryWriter(mstr);
            for (int iLoop = 0; iLoop < iArrLargeValues.Length; iLoop++)
            {
                // [] Offset out of range
                Assert.Throws<ArgumentOutOfRangeException>(() => dw2.Write(bArr, iArrLargeValues[iLoop], 0));
                // [] Invalid count value
                Assert.Throws<ArgumentOutOfRangeException>(() => dw2.Write(bArr, 0, iArrLargeValues[iLoop]));
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
        public void BinaryWriter_WriteBArrayTest2()
        {
            BinaryWriter dw2 = null;
            BinaryReader dr2 = null;
            Stream mstr = null;
            byte[] bArr = new byte[0];
            int ii = 0;
            byte[] bReadArr = new byte[0];
            int ReturnValue;

            bArr = new byte[1000];
            bArr[0] = byte.MinValue;
            bArr[1] = byte.MaxValue;

            for (ii = 2; ii < 1000; ii++)
                bArr[ii] = (byte)(ii % 255);

            // []read/ Write character values 0-1000 with  Memorystream
            mstr = CreateStream();
            dw2 = new BinaryWriter(mstr);

            dw2.Write(bArr, 0, bArr.Length);
            dw2.Flush();
            mstr.Position = 0;

            dr2 = new BinaryReader(mstr);
            bReadArr = new byte[bArr.Length];
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
        public void BinaryWriter_WriteCharArrayTest()
        {
            int ii = 0;
            char[] chArr = new char[1000];
            chArr[0] = char.MinValue;
            chArr[1] = char.MaxValue;
            chArr[2] = '1';
            chArr[3] = 'A';
            chArr[4] = '\0';
            chArr[5] = '#';
            chArr[6] = '\t';

            for (ii = 7; ii < 1000; ii++)
                chArr[ii] = (char)ii;

            // [] read/Write with Memorystream
            Stream mstr = CreateStream();
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
        public void BinaryWriter_WriteCharArrayTest_Negative()
        {
            int[] iArrInvalidValues = new int[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, int.MinValue, short.MinValue };
            int[] iArrLargeValues = new int[] { int.MaxValue, int.MaxValue - 1, int.MaxValue / 2, int.MaxValue / 10, int.MaxValue / 100 };
            char[] chArr = new char[1000];

            // [] ArgumentNullException for null argument
            Stream mstr = CreateStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);
            Assert.Throws<ArgumentNullException>(() => dw2.Write((char[])null));
            dw2.Dispose();
            mstr.Dispose();


            // [] ArgumentNullException for null argument
            mstr = CreateStream();
            dw2 = new BinaryWriter(mstr);
            Assert.Throws<ArgumentNullException>(() => dw2.Write((char[])null, 0, 0));

            mstr.Dispose();
            dw2.Dispose();

            mstr = CreateStream();
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

            mstr = CreateStream();
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
        /// We don't throw on the second read but then throws continuously - note the loop count difference in the 2 loops
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
        public void BinaryWriter_WriteCharArrayTest2()
        {

            Stream mem = CreateStream();
            BinaryWriter writer = new BinaryWriter(mem, Encoding.Unicode);

            // between 55296 <= x < 56319

            // between 56320 <= x < 57343
            char[] randomChars = new char[] {
                (char)55296, (char)57297, (char)55513, (char)56624, (char)55334, (char)56957, (char)55857,
                (char)56355, (char)56095, (char)56887, (char) 56126, (char) 56735, (char)55748, (char)56405,
                (char)55787, (char)56707, (char) 56300, (char)56417, (char)55465, (char)56944
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

            char[] chars = reader.ReadChars(randomChars.Length);
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
        public void BinaryWriter_WriteCharArrayTest3()
        {
            char[] chArr = new char[1000];
            chArr[0] = char.MinValue;
            chArr[1] = char.MaxValue;
            chArr[2] = '1';
            chArr[3] = 'A';
            chArr[4] = '\0';
            chArr[5] = '#';
            chArr[6] = '\t';

            for (int ii = 7; ii < 1000; ii++)
                chArr[ii] = (char)ii;

            // []read/ Write character values 0-1000 with  Memorystream

            Stream mstr = CreateStream();
            BinaryWriter dw2 = new BinaryWriter(mstr);

            dw2.Write(chArr, 0, chArr.Length);
            dw2.Flush();
            mstr.Position = 0;

            BinaryReader dr2 = new BinaryReader(mstr);
            char[] chReadArr = new char[chArr.Length];
            int charsRead = dr2.Read(chReadArr, 0, chArr.Length);
            Assert.Equal(chArr.Length, charsRead);

            for (int ii = 0; ii < chArr.Length; ii++)
            {
                Assert.Equal(chArr[ii], chReadArr[ii]);
            }

            mstr.Dispose();
            dw2.Dispose();
        }

        [Fact]
        public void BinaryWriter_WriteSpan()
        {
            byte[] bytes = new byte[] { 4, 2, 7, 0xFF };
            char[] chars = new char[] { 'a', '7', char.MaxValue };
            Span<byte> byteSpan = new Span<byte>(bytes);
            Span<char> charSpan = new Span<char>(chars);

            using (Stream memoryStream = CreateStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.Unicode))
                {
                    binaryWriter.Write(byteSpan);
                    binaryWriter.Write(charSpan);

                    Stream baseStream = binaryWriter.BaseStream;
                    baseStream.Position = 2;

                    Assert.Equal(7, baseStream.ReadByte());
                    Assert.Equal(0xFF, baseStream.ReadByte());

                    char testChar;

                    testChar = (char)((ushort)baseStream.ReadByte() + ((ushort)baseStream.ReadByte() << 8));
                    Assert.Equal('a', testChar);

                    testChar = (char)((ushort)baseStream.ReadByte() + ((ushort)baseStream.ReadByte() << 8));
                    Assert.Equal('7', testChar);

                    testChar = (char)((ushort)baseStream.ReadByte() + ((ushort)baseStream.ReadByte() << 8));
                    Assert.Equal(char.MaxValue, testChar);
                }
            }
        }
    }
}
