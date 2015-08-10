// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace StreamTests
{
    public class StreamMethods
    {
        [Fact]
        public static void MemoryStreamSeekStress()
        {
            //MemoryStream
            var ms1 = new MemoryStream();
            SeekTest(ms1, false);
        }
        [Fact]
        public static void MemoryStreamSeekStressWithInitialBuffer()
        {
            //MemoryStream
            var ms1 = new MemoryStream(new Byte[1024], false);
            SeekTest(ms1, false);
        }

        [Fact]
        public static async Task MemoryStreamStress()
        {
            var ms1 = new MemoryStream();
            await StreamTest(ms1, false);
        }

        static private void SeekTest(Stream stream, Boolean fSuppres)
        {
            long lngPos;
            Byte btValue;

            stream.Position = 0;

            Assert.Equal(0, stream.Position);

            Int32 length = 1 << 10; //fancy way of writing 2 to the pow 10
            Byte[] btArr = new Byte[length];
            for (int i = 0; i < btArr.Length; i++)
                btArr[i] = (byte)i;

            if (stream.CanWrite)
                stream.Write(btArr, 0, btArr.Length);
            else
                stream.Position = btArr.Length;

            Assert.Equal(btArr.Length, stream.Position);

            lngPos = stream.Seek(0, SeekOrigin.Begin);
            Assert.Equal(0, lngPos);

            Assert.Equal(0, stream.Position);

            for (int i = 0; i < btArr.Length; i++)
            {
                if (stream.CanWrite)
                {
                    btValue = (Byte)stream.ReadByte();
                    Assert.Equal(btArr[i], btValue);
                }
                else
                {
                    stream.Seek(1, SeekOrigin.Current);
                }
                Assert.Equal(i + 1, stream.Position);
            }

            Assert.Throws<IOException>(() => stream.Seek(-5, SeekOrigin.Begin));

            lngPos = stream.Seek(5, SeekOrigin.Begin);
            Assert.Equal(5, lngPos);
            Assert.Equal(5, stream.Position);

            lngPos = stream.Seek(5, SeekOrigin.End);
            Assert.Equal(length + 5, lngPos);
            Assert.Throws<IOException>(() => stream.Seek(-(btArr.Length + 1), SeekOrigin.End));

            lngPos = stream.Seek(-5, SeekOrigin.End);
            Assert.Equal(btArr.Length - 5, lngPos);
            Assert.Equal(btArr.Length - 5, stream.Position);

            lngPos = stream.Seek(0, SeekOrigin.End);
            Assert.Equal(btArr.Length, stream.Position);

            for (int i = btArr.Length; i > 0; i--)
            {
                stream.Seek(-1, SeekOrigin.Current);
                Assert.Equal(i - 1, stream.Position);
            }

            Assert.Throws<IOException>(() => stream.Seek(-1, SeekOrigin.Current));
        }

        private static async Task StreamTest(Stream stream, Boolean fSuppress)
        {

            String strValue;
            Int32 iValue;

            //[] We will first use the stream's 2 writing methods
            Int32 iLength = 1 << 10;
            stream.Seek(0, SeekOrigin.Begin);

            for (int i = 0; i < iLength; i++)
                stream.WriteByte((Byte)i);

            Byte[] btArr = new Byte[iLength];
            for (int i = 0; i < iLength; i++)
                btArr[i] = (Byte)i;
            stream.Write(btArr, 0, iLength);

            //we will write many things here using a binary writer
            BinaryWriter bw1 = new BinaryWriter(stream);
            bw1.Write(false);
            bw1.Write(true);

            for (int i = 0; i < 10; i++)
            {
                bw1.Write((Byte)i);
                bw1.Write((SByte)i);
                bw1.Write((Int16)i);
                bw1.Write((Char)i);
                bw1.Write((UInt16)i);
                bw1.Write(i);
                bw1.Write((UInt32)i);
                bw1.Write((Int64)i);
                bw1.Write((UInt64)i);
                bw1.Write((Single)i);
                bw1.Write((Double)i);
            }

            //Some strings, chars and Bytes
            Char[] chArr = new Char[iLength];
            for (int i = 0; i < iLength; i++)
                chArr[i] = (Char)i;

            bw1.Write(chArr);
            bw1.Write(chArr, 512, 512);

            bw1.Write(new String(chArr));
            bw1.Write(new String(chArr));

            //[] we will now read
            stream.Seek(0, SeekOrigin.Begin);
            for (int i = 0; i < iLength; i++)
            {
                Assert.Equal(i % 256, stream.ReadByte());
            }


            btArr = new Byte[iLength];
            stream.Read(btArr, 0, iLength);
            for (int i = 0; i < iLength; i++)
            {
                Assert.Equal((byte)i, btArr[i]);
            }

            //Now, for the binary reader
            BinaryReader br1 = new BinaryReader(stream);

            Assert.False(br1.ReadBoolean());
            Assert.True(br1.ReadBoolean());

            for (int i = 0; i < 10; i++)
            {
                Assert.Equal( (Byte)i, br1.ReadByte());
                Assert.Equal((SByte)i, br1.ReadSByte());
                Assert.Equal((Int16)i, br1.ReadInt16());
                Assert.Equal((Char)i, br1.ReadChar());
                Assert.Equal((UInt16)i, br1.ReadUInt16());
                Assert.Equal(i, br1.ReadInt32());
                Assert.Equal((UInt32)i, br1.ReadUInt32());
                Assert.Equal((Int64)i, br1.ReadInt64());
                Assert.Equal((UInt64)i, br1.ReadUInt64());
                Assert.Equal((Single)i, br1.ReadSingle());
                Assert.Equal((Double)i, br1.ReadDouble());
            }

            chArr = br1.ReadChars(iLength);
            for (int i = 0; i < iLength; i++)
            {
                Assert.Equal((char)i, chArr[i]);
            }

            chArr = new Char[512];
            chArr = br1.ReadChars(iLength / 2);
            for (int i = 0; i < iLength / 2; i++)
            {
                Assert.Equal((char)(iLength / 2 + i), chArr[i]);
            }

            chArr = new Char[iLength];
            for (int i = 0; i < iLength; i++)
                chArr[i] = (Char)i;
            strValue = br1.ReadString();
            Assert.Equal(new String(chArr), strValue);

            strValue = br1.ReadString();
            Assert.Equal(new String(chArr), strValue);

            stream.Seek(1, SeekOrigin.Current); // In the original test, success here would end the test

            //[] we will do some async tests here now
            stream.Position = 0;

            btArr = new Byte[iLength];
            for (int i = 0; i < iLength; i++)
                btArr[i] = (Byte)(i + 5);

            await stream.WriteAsync(btArr, 0, btArr.Length);

            stream.Position = 0;
            for (int i = 0; i < iLength; i++)
            {
                Assert.Equal((byte)(i + 5), stream.ReadByte());
            }

            //we will read asynchronously
            stream.Position = 0;

            Byte[] compArr = new Byte[iLength];

            iValue = await stream.ReadAsync(compArr, 0, compArr.Length);

            Assert.Equal(btArr.Length, iValue);

            for (int i = 0; i < iLength; i++)
            {
                Assert.Equal(compArr[i], btArr[i]);
            }
        }
    }
}
