// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;

namespace System.IO.Tests
{
    public partial class MemoryStreamTests
    {
        [Fact]
        public static void MemoryStream_Write_BeyondCapacity()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                long origLength = memoryStream.Length;
                byte[] bytes = new byte[10];
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = (byte)i;
                int spanPastEnd = 5;
                memoryStream.Seek(spanPastEnd, SeekOrigin.End);
                Assert.Equal(memoryStream.Length + spanPastEnd, memoryStream.Position);

                // Test Write
                memoryStream.Write(bytes, 0, bytes.Length);
                long pos = memoryStream.Position;
                Assert.Equal(pos, origLength + spanPastEnd + bytes.Length);
                Assert.Equal(memoryStream.Length, origLength + spanPastEnd + bytes.Length);

                // Verify bytes were correct.
                memoryStream.Position = origLength;
                byte[] newData = new byte[bytes.Length + spanPastEnd];
                int n = memoryStream.Read(newData, 0, newData.Length);
                Assert.Equal(n, newData.Length);
                for (int i = 0; i < spanPastEnd; i++)
                    Assert.Equal(0, newData[i]);
                for (int i = 0; i < bytes.Length; i++)
                    Assert.Equal(bytes[i], newData[i + spanPastEnd]);
            }
        }

        [Fact]
        public static void MemoryStream_WriteByte_BeyondCapacity()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                long origLength = memoryStream.Length;
                byte[] bytes = new byte[10];
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = (byte)i;
                int spanPastEnd = 5;
                memoryStream.Seek(spanPastEnd, SeekOrigin.End);
                Assert.Equal(memoryStream.Length + spanPastEnd, memoryStream.Position);

                // Test WriteByte
                origLength = memoryStream.Length;
                memoryStream.Position = memoryStream.Length + spanPastEnd;
                memoryStream.WriteByte(0x42);
                long expected = origLength + spanPastEnd + 1;
                Assert.Equal(expected, memoryStream.Position);
                Assert.Equal(expected, memoryStream.Length);
            }
        }

        [Fact]
        public static void MemoryStream_GetPositionTest_Negative()
        {
            int iArrLen = 100;
            byte[] bArr = new byte[iArrLen];

            using (MemoryStream ms = new MemoryStream(bArr))
            {
                long iCurrentPos = ms.Position;
                for (int i = -1; i > -6; i--)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => ms.Position = i);
                    Assert.Equal(ms.Position, iCurrentPos);
                }
            }
        }

        [Fact]
        public static void MemoryStream_LengthTest()
        {
            using (MemoryStream ms2 = new MemoryStream())
            {
                // [] Get the Length when position is at length
                ms2.SetLength(50);
                ms2.Position = 50;
                StreamWriter sw2 = new StreamWriter(ms2);
                for (char c = 'a'; c < 'f'; c++)
                    sw2.Write(c);
                sw2.Flush();
                Assert.Equal(55, ms2.Length);

                // Somewhere in the middle (set the length to be shorter.)
                ms2.SetLength(30);
                Assert.Equal(30, ms2.Length);
                Assert.Equal(30, ms2.Position);

                // Increase the length
                ms2.SetLength(100);
                Assert.Equal(100, ms2.Length);
                Assert.Equal(30, ms2.Position);
            }
        }

        [Fact]
        public static void MemoryStream_LengthTest_Negative()
        {
            using (MemoryStream ms2 = new MemoryStream())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => ms2.SetLength(Int64.MaxValue));
                Assert.Throws<ArgumentOutOfRangeException>(() => ms2.SetLength(-2));
            }
        }

        [Fact]
        public static void MemoryStream_ReadTest_Negative()
        {
            MemoryStream ms2 = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() => ms2.Read(null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ms2.Read(new byte[] { 1 }, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ms2.Read(new byte[] { 1 }, 0, -1));
            AssertExtensions.Throws<ArgumentException>(null, () => ms2.Read(new byte[] { 1 }, 2, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => ms2.Read(new byte[] { 1 }, 0, 2));

            ms2.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ms2.Read(new byte[] { 1 }, 0, 1));
        }

        [Fact]
        public static void MemoryStream_WriteToTests()
        {
            using (MemoryStream ms2 = new MemoryStream())
            {
                byte[] bytArrRet;
                byte[] bytArr = new byte[] { byte.MinValue, byte.MaxValue, 1, 2, 3, 4, 5, 6, 128, 250 };

                // [] Write to FileStream, check the filestream
                ms2.Write(bytArr, 0, bytArr.Length);

                using (MemoryStream readonlyStream = new MemoryStream())
                {
                    ms2.WriteTo(readonlyStream);
                    readonlyStream.Flush();
                    readonlyStream.Position = 0;
                    bytArrRet = new byte[(int)readonlyStream.Length];
                    readonlyStream.Read(bytArrRet, 0, (int)readonlyStream.Length);
                    for (int i = 0; i < bytArr.Length; i++)
                    {
                        Assert.Equal(bytArr[i], bytArrRet[i]);
                    }
                }
            }

            // [] Write to memoryStream, check the memoryStream
            using (MemoryStream ms2 = new MemoryStream())
            using (MemoryStream ms3 = new MemoryStream())
            {
                byte[] bytArrRet;
                byte[] bytArr = new byte[] { byte.MinValue, byte.MaxValue, 1, 2, 3, 4, 5, 6, 128, 250 };

                ms2.Write(bytArr, 0, bytArr.Length);
                ms2.WriteTo(ms3);
                ms3.Position = 0;
                bytArrRet = new byte[(int)ms3.Length];
                ms3.Read(bytArrRet, 0, (int)ms3.Length);
                for (int i = 0; i < bytArr.Length; i++)
                {
                    Assert.Equal(bytArr[i], bytArrRet[i]);
                }
            }
        }

        [Fact]
        public static void MemoryStream_WriteToTests_Negative()
        {
            using (MemoryStream ms2 = new MemoryStream())
            {
                Assert.Throws<ArgumentNullException>(() => ms2.WriteTo(null));

                ms2.Write(new byte[] { 1 }, 0, 1);
                MemoryStream readonlyStream = new MemoryStream(new byte[1028], false);
                Assert.Throws<NotSupportedException>(() => ms2.WriteTo(readonlyStream));

                readonlyStream.Dispose();

                // [] Pass in a closed stream
                Assert.Throws<ObjectDisposedException>(() => ms2.WriteTo(readonlyStream));
            }
        }

        [Fact]
        public static void MemoryStream_CopyTo_Invalid()
        {
            MemoryStream memoryStream;
            using (memoryStream = new MemoryStream())
            {
                AssertExtensions.Throws<ArgumentNullException>("destination", () => memoryStream.CopyTo(destination: null));

                // Validate the destination parameter first.
                AssertExtensions.Throws<ArgumentNullException>("destination", () => memoryStream.CopyTo(destination: null, bufferSize: 0));
                AssertExtensions.Throws<ArgumentNullException>("destination", () => memoryStream.CopyTo(destination: null, bufferSize: -1));

                // Then bufferSize.
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => memoryStream.CopyTo(Stream.Null, bufferSize: 0)); // 0-length buffer doesn't make sense.
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => memoryStream.CopyTo(Stream.Null, bufferSize: -1));
            }

            // After the Stream is disposed, we should fail on all CopyTos.
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => memoryStream.CopyTo(Stream.Null, bufferSize: 0)); // Not before bufferSize is validated.
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => memoryStream.CopyTo(Stream.Null, bufferSize: -1));

            MemoryStream disposedStream = memoryStream;

            // We should throw first for the source being disposed...
            Assert.Throws<ObjectDisposedException>(() => memoryStream.CopyTo(disposedStream, 1));

            // Then for the destination being disposed.
            memoryStream = new MemoryStream();
            Assert.Throws<ObjectDisposedException>(() => memoryStream.CopyTo(disposedStream, 1));

            // Then we should check whether we can't read but can write, which isn't possible for non-subclassed MemoryStreams.

            // THen we should check whether the destination can read but can't write.
            var readOnlyStream = new DelegateStream(
                canReadFunc: () => true,
                canWriteFunc: () => false
            );

            Assert.Throws<NotSupportedException>(() => memoryStream.CopyTo(readOnlyStream, 1));
        }

        [Theory]
        [MemberData(nameof(CopyToData))]
        public void CopyTo(Stream source, byte[] expected)
        {
            using (var destination = new MemoryStream())
            {
                source.CopyTo(destination);
                Assert.InRange(source.Position, source.Length, int.MaxValue); // Copying the data should have read to the end of the stream or stayed past the end.
                Assert.Equal(expected, destination.ToArray());
            }
        }

        public static IEnumerable<object[]> CopyToData()
        {
            // Stream is positioned @ beginning of data
            var data1 = new byte[] { 1, 2, 3 };
            var stream1 = new MemoryStream(data1);

            yield return new object[] { stream1, data1 };

            // Stream is positioned in the middle of data
            var data2 = new byte[] { 0xff, 0xf3, 0xf0 };
            var stream2 = new MemoryStream(data2) { Position = 1 };

            yield return new object[] { stream2, new byte[] { 0xf3, 0xf0 } };

            // Stream is positioned after end of data
            var data3 = data2;
            var stream3 = new MemoryStream(data3) { Position = data3.Length + 1 };

            yield return new object[] { stream3, Array.Empty<byte>() };
        }
    }
}
