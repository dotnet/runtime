using Xunit;
using System;
using System.IO;
using System.Text;

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
            Assert.Throws<ArgumentNullException>(() => { BinaryWriter writer = new BinaryWriter(null); });

            // [] Can't construct a BinaryWriter on a readonly stream
            using (MemoryStream memStream = new MemoryStream(new byte[10], false))
            {
                Assert.Throws<ArgumentException>(() => { BinaryWriter writer = new BinaryWriter(memStream); });
            }

            // [] Can't construct a BinaryWriter with a closed stream
            {
                MemoryStream memStream = new MemoryStream();
                memStream.Dispose();
                Assert.Throws<ArgumentException>(() => { BinaryWriter dw2 = new BinaryWriter(memStream); });
            }
        }

        [Fact]
        public static void BinaryWriter_EncodingCtorAndWriteTests_UTF8()
        {
            BinaryWriter_EncodingCtorAndWriteTests(Encoding.UTF8, "This is UTF8\u00FF");
        }

        [Fact]
        public static void BinaryWriter_EncodingCtorAndWriteTests_BigEndianUnicode()
        {
            BinaryWriter_EncodingCtorAndWriteTests(Encoding.BigEndianUnicode, "This is BigEndianUnicode\u00FF");
        }

        [Fact]
        public static void BinaryWriter_EncodingCtorAndWriteTests_Unicode()
        {
            BinaryWriter_EncodingCtorAndWriteTests(Encoding.Unicode, "This is Unicode\u00FF");
        }

        private static void BinaryWriter_EncodingCtorAndWriteTests(Encoding encoding, string testString)
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

        [Fact]
        public static void BinaryWriter_EncodingCtorAndWriteTests_Negative()
        {
            // [] Check for ArgumentNullException on null stream
            Assert.Throws<ArgumentNullException>(
                () => { new BinaryReader((Stream)null, Encoding.UTF8); });

            // [] Check for ArgumentNullException on null encoding
            Assert.Throws<ArgumentNullException>(
               () => { new BinaryReader(new MemoryStream(), null); });
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

                Assert.Equal(lReturn, iArrLargeValues[iLoop]);
            }
            dw2.Dispose();
            mstr.Dispose();

            // [] Seek from start of stream
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(0, SeekOrigin.Begin);

            Assert.Equal(lReturn, 0);

            dw2.Write("lki".ToCharArray());
            dw2.Flush();
            bArr = mstr.ToArray();
            sb = new StringBuilder();
            for (int i = 0; i < bArr.Length; i++)
                sb.Append((Char)bArr[i]);

            Assert.Equal(sb.ToString(), "lki3456789");

            dw2.Dispose();
            mstr.Dispose();

            // [] Seek into stream from start
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(3, SeekOrigin.Begin);

            Assert.Equal(lReturn, 3);

            dw2.Write("lk".ToCharArray());
            dw2.Flush();
            bArr = mstr.ToArray();
            sb = new StringBuilder();
            for (int i = 0; i < bArr.Length; i++)
                sb.Append((Char)bArr[i]);

            Assert.Equal(sb.ToString(), "012lk56789");

            dw2.Dispose();
            mstr.Dispose();

            // [] Seek from end of stream
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(-3, SeekOrigin.End);

            Assert.Equal(lReturn, 7);

            dw2.Write("ll".ToCharArray());
            dw2.Flush();
            bArr = mstr.ToArray();
            sb = new StringBuilder();
            for (int i = 0; i < bArr.Length; i++)
                sb.Append((Char)bArr[i]);

            Assert.Equal(sb.ToString(), "0123456ll9");

            dw2.Dispose();
            mstr.Dispose();

            // [] Seeking from current position
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            mstr.Position = 2;
            lReturn = dw2.Seek(2, SeekOrigin.Current);

            Assert.Equal(lReturn, 4);

            dw2.Write("ll".ToCharArray());
            dw2.Flush();
            bArr = mstr.ToArray();
            sb = new StringBuilder();
            for (int i = 0; i < bArr.Length; i++)
                sb.Append((Char)bArr[i]);

            Assert.Equal(sb.ToString(), "0123ll6789");

            dw2.Dispose();
            mstr.Dispose();

            // [] Seeking past the end from middle
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(4, SeekOrigin.End); //This wont throw any exception now.

            Assert.Equal(mstr.Position, 14);


            dw2.Dispose();
            mstr.Dispose();

            // [] Seek past the end from beginning
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(11, SeekOrigin.Begin);  //This wont throw any exception now.

            Assert.Equal(mstr.Position, 11);

            dw2.Dispose();
            mstr.Dispose();

            // [] Seek to the end
            mstr = new MemoryStream();
            dw2 = new BinaryWriter(mstr);
            dw2.Write("0123456789".ToCharArray());
            lReturn = dw2.Seek(10, SeekOrigin.Begin);

            Assert.Equal(lReturn, 10);

            dw2.Write("ll".ToCharArray());
            bArr = mstr.ToArray();
            sb = new StringBuilder();
            for (int i = 0; i < bArr.Length; i++)
                sb.Append((Char)bArr[i]);

            Assert.Equal(sb.ToString(), "0123456789ll");

            dw2.Dispose();
            mstr.Dispose();
        }

        [Fact]
        public static void BinaryWriter_SeekTests_NegativeOffset()
        {
            int[] invalidValues = new Int32[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, Int32.MinValue, Int16.MinValue };
            
            // [] ArgumentOutOfRangeException if offset is negative
            using (MemoryStream memStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memStream))
            {
                writer.Write("Hello, this is my string".ToCharArray());

                foreach (int invalidValue in invalidValues)
                {
                    Assert.Throws<IOException>(() =>
                    {
                        writer.Seek(invalidValue, SeekOrigin.Begin);
                    });
                }
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
                Assert.Equal(ms2, sr2.BaseStream);
            }
        }

        [Fact]
        public static void BinaryWriter_FlushTests()
        {
            // [] Check that flush updates the underlying stream
            using (MemoryStream memstr2 = new MemoryStream())
            using (BinaryWriter bw2 = new BinaryWriter(memstr2))
            {
                bw2.Write("HelloWorld");
                //TODO:: Ckeck with dev why it's 11 bytes.
                Assert.Equal(11, memstr2.Length);
                bw2.Flush();
                Assert.Equal(11, memstr2.Length);
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
