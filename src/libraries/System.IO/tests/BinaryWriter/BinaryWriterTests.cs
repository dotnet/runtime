// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Tests
{
    public class BinaryWriterTests
    {
        [Fact]
        public static void BinaryWriter_CtorAndWriteTests1()
        {
            // [] Smoke test to ensure that we can write with the constructed writer
            using (MemoryStream mstr = new MemoryStream())
            using (BinaryWriter dw2 = new BinaryWriter(mstr))
            using (BinaryReader dr2 = new BinaryReader(mstr))
            {
                dw2.Write(true);
                dw2.Flush();
                mstr.Position = 0;

                Assert.True(dr2.ReadBoolean());
            }
        }

        [Fact]
        public static void BinaryWriter_CtorAndWriteTests1_Negative()
        {
            // [] Should throw ArgumentNullException for null argument
            Assert.Throws<ArgumentNullException>(() => new BinaryWriter(null));

            // [] Can't construct a BinaryWriter on a readonly stream
            using (MemoryStream memStream = new MemoryStream(new byte[10], false))
            {
                Assert.Throws<ArgumentException>(() => new BinaryWriter(memStream));
            }

            // [] Can't construct a BinaryWriter with a closed stream
            {
                MemoryStream memStream = new MemoryStream();
                memStream.Dispose();
                Assert.Throws<ArgumentException>(() => new BinaryWriter(memStream));
            }
        }

        [Theory]
        [MemberData("EncodingAndEncodingStrings")]
        public static void BinaryWriter_EncodingCtorAndWriteTests(Encoding encoding, string testString)
        {
            using (MemoryStream memStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memStream, encoding))
            using (BinaryReader reader = new BinaryReader(memStream, encoding))
            {
                writer.Write(testString);
                writer.Flush();
                memStream.Position = 0;

                Assert.Equal(testString, reader.ReadString());
            }
        }

        public static IEnumerable<object[]> EncodingAndEncodingStrings
        {
            get
            {
                yield return new object[] { Encoding.UTF8, "This is UTF8\u00FF" };
                yield return new object[] { Encoding.BigEndianUnicode, "This is BigEndianUnicode\u00FF" };
                yield return new object[] { Encoding.Unicode, "This is Unicode\u00FF" };
            }
        }

        [Fact]
        public static void BinaryWriter_EncodingCtorAndWriteTests_Negative()
        {
            // [] Check for ArgumentNullException on null stream
            Assert.Throws<ArgumentNullException>(() => new BinaryReader((Stream)null, Encoding.UTF8));

            // [] Check for ArgumentNullException on null encoding
            Assert.Throws<ArgumentNullException>(() => new BinaryReader(new MemoryStream(), null));
        }

        [Fact]
        public static void BinaryWriter_SeekTests()
        {
            int[] iArrLargeValues = new Int32[] { 10000, 100000, Int32.MaxValue / 200, Int32.MaxValue / 1000, Int16.MaxValue, Int32.MaxValue, Int32.MaxValue - 1, Int32.MaxValue / 2, Int32.MaxValue / 10, Int32.MaxValue / 100 };

            BinaryWriter dw2 = null;
            MemoryStream mstr = null;
            Byte[] bArr = null;
            StringBuilder sb = new StringBuilder();
            Int64 lReturn = 0;

            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("Hello, this is my string".ToCharArray());
            for (int iLoop = 0; iLoop < iArrLargeValues.Length; iLoop++)
            {
                lReturn = dw2.Seek(iArrLargeValues[iLoop], SeekOrigin.Begin);

                Assert.Equal(iArrLargeValues[iLoop], lReturn);
            }
            dw2.Dispose();
            mstr.Dispose();

            // [] Seek from start of stream
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(0, SeekOrigin.Begin);

            Assert.Equal(0, lReturn);

            dw2.Write("lki".ToCharArray());
            dw2.Flush();
            bArr = mstr.ToArray();
            sb = new StringBuilder();
            for (int i = 0; i < bArr.Length; i++)
                sb.Append((Char)bArr[i]);

            Assert.Equal("lki3456789", sb.ToString());

            dw2.Dispose();
            mstr.Dispose();

            // [] Seek into stream from start
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(3, SeekOrigin.Begin);

            Assert.Equal(3, lReturn);

            dw2.Write("lk".ToCharArray());
            dw2.Flush();
            bArr = mstr.ToArray();
            sb = new StringBuilder();
            for (int i = 0; i < bArr.Length; i++)
                sb.Append((Char)bArr[i]);

            Assert.Equal("012lk56789", sb.ToString());

            dw2.Dispose();
            mstr.Dispose();

            // [] Seek from end of stream
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(-3, SeekOrigin.End);

            Assert.Equal(7, lReturn);

            dw2.Write("ll".ToCharArray());
            dw2.Flush();
            bArr = mstr.ToArray();
            sb = new StringBuilder();
            for (int i = 0; i < bArr.Length; i++)
                sb.Append((Char)bArr[i]);

            Assert.Equal("0123456ll9", sb.ToString());

            dw2.Dispose();
            mstr.Dispose();

            // [] Seeking from current position
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            mstr.Position = 2;
            lReturn = dw2.Seek(2, SeekOrigin.Current);

            Assert.Equal(4, lReturn);

            dw2.Write("ll".ToCharArray());
            dw2.Flush();
            bArr = mstr.ToArray();
            sb = new StringBuilder();
            for (int i = 0; i < bArr.Length; i++)
                sb.Append((Char)bArr[i]);

            Assert.Equal("0123ll6789", sb.ToString());

            dw2.Dispose();
            mstr.Dispose();

            // [] Seeking past the end from middle
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(4, SeekOrigin.End); //This wont throw any exception now.

            Assert.Equal(14, mstr.Position);


            dw2.Dispose();
            mstr.Dispose();

            // [] Seek past the end from beginning
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(11, SeekOrigin.Begin);  //This wont throw any exception now.

            Assert.Equal(11, mstr.Position);

            dw2.Dispose();
            mstr.Dispose();

            // [] Seek to the end
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(10, SeekOrigin.Begin);

            Assert.Equal(10, lReturn);

            dw2.Write("ll".ToCharArray());
            bArr = mstr.ToArray();
            sb = new StringBuilder();
            for (int i = 0; i < bArr.Length; i++)
                sb.Append((Char)bArr[i]);

            Assert.Equal("0123456789ll", sb.ToString());

            dw2.Dispose();
            mstr.Dispose();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-2)]
        [InlineData(-10000)]
        [InlineData(Int32.MinValue)]
        public static void BinaryWriter_SeekTests_NegativeOffset(int invalidValue)
        {
            // [] IOException if offset is negative
            using (MemoryStream memStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memStream))
            {
                writer.Write("Hello, this is my string".ToCharArray());
                Assert.Throws<IOException>(() => writer.Seek(invalidValue, SeekOrigin.Begin));
            }
        }

        [Fact]
        public static void BinaryWriter_SeekTests_InvalidSeekOrigin()
        {
            // [] ArgumentException for invalid seekOrigin
            using (MemoryStream memStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memStream))
            {
                writer.Write("012345789".ToCharArray());

                Assert.Throws<ArgumentException>(() => 
                {
                    writer.Seek(3, ~SeekOrigin.Begin);
                });
            }
        }

        [Fact]
        public static void BinaryWriter_BaseStreamTests()
        {
            // [] Get the base stream for MemoryStream
            using (MemoryStream ms2 = new MemoryStream())
            using (BinaryWriter sr2 = new BinaryWriter(ms2))
            {
                Assert.Same(ms2, sr2.BaseStream);
            }
        }

        [Fact]
        public static void BinaryWriter_FlushTests()
        {
            // [] Check that flush updates the underlying stream
            using (MemoryStream memstr2 = new MemoryStream())
            using (BinaryWriter bw2 = new BinaryWriter(memstr2))
            {
                string str = "HelloWorld";
                int expectedLength = str.Length + 1; // 1 for 7-bit encoded length
                bw2.Write(str);
                Assert.Equal(expectedLength, memstr2.Length);
                bw2.Flush();
                Assert.Equal(expectedLength, memstr2.Length);
            }

            // [] Flushing closed writer should not throw an exception.
            using (MemoryStream memstr2 = new MemoryStream())
            {
                BinaryWriter bw2 = new BinaryWriter(memstr2);
                bw2.Dispose();
                bw2.Flush();
            }
        }

        [Fact]
        public static void BinaryWriter_DisposeTests()
        {
            // Disposing multiple times should not throw an exception
            using (MemoryStream memStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memStream))
            {
                binaryWriter.Dispose();
                binaryWriter.Dispose();
                binaryWriter.Dispose();
            }
        }

        [Fact]
        public static void BinaryWriter_DisposeTests_Negative()
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                BinaryWriter binaryWriter = new BinaryWriter(memStream);
                binaryWriter.Dispose();

                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Seek(1, SeekOrigin.Begin));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write(new Byte[2], 0, 2));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write(new Char[2], 0, 2));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write(true));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write((Byte)4));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write(new Byte[] { 1, 2 }));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write('a'));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write(new Char[] { 'a', 'b' }));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write(5.3));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write((Int16)3));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write(33));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write((Int64)42));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write((SByte)4));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write("Hello There"));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write((Single)4.3));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write((UInt16)3));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write((UInt32)4));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write((UInt64)5));
                Assert.Throws<ObjectDisposedException>(() => binaryWriter.Write("Bah"));
            }
        }
    }
}
