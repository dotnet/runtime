// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class UmsReadTests
    {
        [Fact]
        public static void EmptyStreamRead()
        {
            using (var manager = new UmsManager(FileAccess.Read, 0))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.ReadUmsInvariants(stream);

                var position = stream.Position;
                Assert.Equal(manager.Stream.ReadByte(), -1); // end of stream
                Assert.Equal(stream.Position, position);
            }

            using (HGlobalSafeBuffer buffer = new HGlobalSafeBuffer(1))
            using (var stream = new UnmanagedMemoryStream(buffer, 0, 0))
            {
                var position = stream.Position;
                Assert.Equal(stream.ReadByte(), -1); // end of stream
                Assert.Equal(stream.Position, position);
            }
        }

        [Fact]
        public static void OneByteStreamRead()
        {
            using (var manager = new UmsManager(FileAccess.Read, new byte[] { 100 }))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.ReadUmsInvariants(stream);

                var position = stream.Position;
                Assert.Equal(stream.ReadByte(), 100);
                Assert.Equal(stream.Position, position + 1);

                position = stream.Position;
                Assert.Equal(stream.ReadByte(), -1); // end of stream
                Assert.Equal(stream.Position, position);
            }

            using (HGlobalSafeBuffer buffer = new HGlobalSafeBuffer(1))
            using (var stream = new UnmanagedMemoryStream(buffer, 0, 1, FileAccess.ReadWrite))
            {
                buffer.Write(0, (byte)100);

                var position = stream.Position;
                Assert.Equal(stream.ReadByte(), 100);
                Assert.Equal(stream.Position, position + 1);

                Assert.Equal(stream.ReadByte(), -1); // end of stream
                Assert.Equal(stream.Position, position + 1);
            }
        }

        [Fact]
        public static void CannotReadFromWriteStream()
        {
            using (var manager = new UmsManager(FileAccess.Write, 100))
            {
                Stream stream = manager.Stream;
                Assert.Throws<NotSupportedException>(() => stream.ReadByte());
            }

            using (HGlobalSafeBuffer buffer = new HGlobalSafeBuffer(100))
            using (var stream = new UnmanagedMemoryStream(buffer, 0, 100, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => stream.ReadByte());
            }
        }

        void ReadToEnd(UmsManager manager)
        {
            Stream stream = manager.Stream;
            if (stream.CanRead)
            {
                byte[] read = ReadAllBytes(stream);
                Assert.Equal(stream.Position, read.Length);
                Assert.Equal(manager.ToArray(), read, ArrayHelpers.Comparer<byte>());
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => stream.ReadByte());
            }
        }

        [Fact]
        public static void InvalidReadWrite()
        {
            const int length = 1000;
            using (var manager = new UmsManager(FileAccess.Read, length))
            {
                UnmanagedMemoryStream stream = manager.Stream;

                //case#3: call Read with null, ArgumentNullException should be thrown.
                Assert.Throws<ArgumentNullException>(() => stream.Read(null, 0, 3));
                Assert.Throws<ArgumentNullException>(() => stream.ReadAsync(null, 0, 3).GetAwaiter().GetResult());
                Assert.Throws<ArgumentNullException>(() => stream.Write(null, 0, 7));
                Assert.Throws<ArgumentNullException>(() => stream.WriteAsync(null, 0, 7).GetAwaiter().GetResult());

                //case#4: call Read with start<0, ArgumentOutOfRangeException should be thrown.
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(new byte[] { }, SByte.MinValue, 9));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.ReadAsync(new byte[] { }, SByte.MinValue, 9).GetAwaiter().GetResult());
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(new byte[] { }, -1, 6));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.WriteAsync(new byte[] { }, -1, 6).GetAwaiter().GetResult());

                //case#5: call Read with count<0, ArgumentOutOfRangeException should be thrown.
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(new byte[] { }, 0, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.ReadAsync(new byte[] { }, 0, -1).GetAwaiter().GetResult());
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(new byte[] { }, 1, -2));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.WriteAsync(new byte[] { }, 1, -2).GetAwaiter().GetResult());

                //case#6: call Read with count > ums.Length-startIndex, ArgumentOutOfRangeException should be thrown.
                Assert.Throws<ArgumentException>(() => stream.Read(new byte[10], 0, 11)); // "Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection."
                Assert.Throws<ArgumentException>(() => stream.ReadAsync(new byte[10], 0, 11).GetAwaiter().GetResult());
                Assert.Throws<ArgumentException>(() => stream.Write(new byte[3], 0, 4));
                Assert.Throws<ArgumentException>(() => stream.WriteAsync(new byte[3], 0, 4).GetAwaiter().GetResult());

                //case#10: Call Read on a n length stream, (Capacity is implicitly n), position is set to end, call it,  should throw ArgumentException.
                Assert.Throws<ArgumentException>(() => stream.Read(new byte[] { }, 0, 1));
                Assert.Throws<ArgumentException>(() => stream.Write(new byte[] { }, 0, 1));
            }
        }

        public static byte[] ReadAllBytes(Stream stream)
        {
            List<byte> read = new List<byte>();
            while (true)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) { break; }
                read.AddRange(new ArraySegment<byte>(buffer, 0, bytesRead));
            }
            return read.ToArray();
        }

        [Fact]
        public static unsafe void ReadFromBufferBackedStream()
        {
            const int length = 8192;
            byte[] data = new byte[length];

            using (HGlobalSafeBuffer buffer = new HGlobalSafeBuffer(length))
            {
                for (ulong i = 0; i < length; i++)
                    buffer.Write(i, (byte)i);

                Action validateData = () => {
                    for (int i = 0; i < length; i++)
                        Assert.Equal((byte)i, data[i]);
                };

                using (var stream = new UnmanagedMemoryStream(buffer, 0, length, FileAccess.Read))
                {
                    stream.Position = 0;
                    Assert.Equal(length, stream.Read(data, 0, length));
                    validateData();
                    Array.Clear(data, 0, data.Length);

                    stream.Position = 0;
                    Assert.Equal(length / 2, stream.Read(data, 0, length / 2));
                    Assert.Equal(length / 2, stream.Read(data, length / 2, length / 2));
                    validateData();
                    Array.Clear(data, 0, data.Length);

                    Assert.True(stream.ReadAsync(data, 0, data.Length, new CancellationToken(true)).IsCanceled);

                    stream.Position = 0;
                    Task<int> t = stream.ReadAsync(data, 0, length / 4);
                    Assert.True(t.Status == TaskStatus.RanToCompletion);
                    Assert.Equal(length / 4, t.Result);
                    t = stream.ReadAsync(data, length / 4, length / 4);
                    Assert.True(t.Status == TaskStatus.RanToCompletion);
                    Assert.Equal(length / 4, t.Result);
                    t = stream.ReadAsync(data, length / 2, length / 2);
                    Assert.True(t.Status == TaskStatus.RanToCompletion);
                    Assert.Equal(length / 2, t.Result);
                    validateData();
                    Array.Clear(data, 0, data.Length);
                }
            }
        }

    }
}
