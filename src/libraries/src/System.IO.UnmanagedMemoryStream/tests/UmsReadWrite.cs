// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public sealed class ArrayUmsReadWriteTests : UmsReadWriteTests
    {
        public override int Read(UnmanagedMemoryStream stream, byte[] array, int offset, int count) =>
            stream.Read(array, offset, count);
        public override void Write(UnmanagedMemoryStream stream, byte[] array, int offset, int count) =>
            stream.Write(array, offset, count);

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
                AssertExtensions.Throws<ArgumentException>(null, () => stream.Read(new byte[10], 0, 11)); // "Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection."
                AssertExtensions.Throws<ArgumentException>(null, () => stream.ReadAsync(new byte[10], 0, 11).GetAwaiter().GetResult());
                AssertExtensions.Throws<ArgumentException>(null, () => stream.Write(new byte[3], 0, 4));
                AssertExtensions.Throws<ArgumentException>(null, () => stream.WriteAsync(new byte[3], 0, 4).GetAwaiter().GetResult());

                //case#10: Call Read on a n length stream, (Capacity is implicitly n), position is set to end, call it,  should throw ArgumentException.
                AssertExtensions.Throws<ArgumentException>(null, () => stream.Read(new byte[] { }, 0, 1));
                AssertExtensions.Throws<ArgumentException>(null, () => stream.Write(new byte[] { }, 0, 1));
            }
        }
    }

    public abstract class UmsReadWriteTests
    {
        public abstract int Read(UnmanagedMemoryStream stream, byte[] array, int offset, int count);
        public abstract void Write(UnmanagedMemoryStream stream, byte[] array, int offset, int count);

        [Fact]
        public void EmptyStreamRead()
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
        public void OneByteStreamRead()
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
        public void CannotReadFromWriteStream()
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

        private byte[] ReadAllBytes(UnmanagedMemoryStream stream)
        {
            var buffer = new byte[1024];
            int numRead;
            var result = new MemoryStream();
            while ((numRead = Read(stream, buffer, 0, buffer.Length)) > 0)
            {
                result.Write(buffer, 0, numRead);
            }
            return result.ToArray();
        }

        [Fact]
        public unsafe void ReadFromBufferBackedStream()
        {
            const int length = 8192;
            byte[] data = new byte[length];

            using (HGlobalSafeBuffer buffer = new HGlobalSafeBuffer(length))
            {
                for (ulong i = 0; i < length; i++)
                    buffer.Write(i, unchecked((byte)i));

                Action validateData = () => {
                    for (int i = 0; i < length; i++)
                        Assert.Equal(unchecked((byte)i), data[i]);
                };

                using (var stream = new UnmanagedMemoryStream(buffer, 0, length, FileAccess.Read))
                {
                    stream.Position = 0;
                    Assert.Equal(length, Read(stream, data, 0, length));
                    validateData();
                    Array.Clear(data, 0, data.Length);

                    stream.Position = 0;
                    Assert.Equal(length / 2, Read(stream, data, 0, length / 2));
                    Assert.Equal(length / 2, Read(stream, data, length / 2, length / 2));
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

        // TODO: add tests for different offsets and lengths
        [Fact]
        public void ReadWrite()
        {
            const int length = 1000;
            using (var manager = new UmsManager(FileAccess.ReadWrite, length))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.ReadWriteUmsInvariants(stream);
                Assert.Equal(stream.Length, length);

                var bytes = ArrayHelpers.CreateByteArray(length);
                var copy = bytes.Copy();
                Write(stream, copy, 0, length);

                var memory = manager.ToArray();
                Assert.Equal(bytes, memory, ArrayHelpers.Comparer<byte>());

                stream.Seek(0, SeekOrigin.Begin);
                byte[] read = ReadAllBytes(stream);
                Assert.Equal(stream.Position, read.Length);

                byte[] current = manager.ToArray();
                Assert.Equal(current, read, ArrayHelpers.Comparer<byte>());
                Assert.Equal(bytes, read, ArrayHelpers.Comparer<byte>());

                Write(stream, new byte[0], 0, 0);
            }
        }

        [Fact]
        public void ReadWriteByte()
        {
            const int length = 1000;
            using (var manager = new UmsManager(FileAccess.ReadWrite, length))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.ReadWriteUmsInvariants(stream);

                var bytes = ArrayHelpers.CreateByteArray(length);

                for (int index = 0; index < bytes.Length; index++)
                {
                    byte byteToWrite = bytes[index];
                    stream.WriteByte(byteToWrite);
                    stream.Position--;
                    int read = stream.ReadByte();
                    Assert.Equal((byte)read, byteToWrite);
                }

                var memory = manager.ToArray();

                Assert.Equal(bytes, memory, ArrayHelpers.Comparer<byte>());
            }
        }


        [Fact]
        public void Write()
        {
            const int length = 1000;
            using (var manager = new UmsManager(FileAccess.Write, length))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.WriteUmsInvariants(stream);
                Assert.Equal(stream.Length, length);

                var bytes = ArrayHelpers.CreateByteArray(length);
                Write(stream, bytes.Copy(), 0, length);
                var memory = manager.ToArray();
                Assert.Equal(bytes, memory, ArrayHelpers.Comparer<byte>());

                Write(stream, new byte[0], 0, 0);

                stream.SetLength(1);
                Assert.Equal(1, stream.Length);
                stream.SetLength(4);
                Assert.Equal(4, stream.Length);
                stream.SetLength(0);
                Assert.Equal(0, stream.Length);

                stream.Position = 1;
                bytes = ArrayHelpers.CreateByteArray(length - 1);
                Write(stream, bytes, 0, length - 1);
                memory = manager.ToArray();
                for (int i = 0; i < bytes.Length; i++)
                {
                    Assert.Equal(bytes[i], memory[i + 1]);
                }

                Assert.True(stream.WriteAsync(bytes, 0, bytes.Length, new CancellationToken(true)).IsCanceled);

                stream.Position = 0;
                bytes = ArrayHelpers.CreateByteArray(length);
                for (int i = 0; i < 4; i++)
                {
                    Task t = stream.WriteAsync(bytes, i * (bytes.Length / 4), bytes.Length / 4);
                    Assert.True(t.Status == TaskStatus.RanToCompletion);
                }
                Assert.Equal(bytes, manager.ToArray(), ArrayHelpers.Comparer<byte>());
            }
        }

        [Fact]
        public void WriteByte()
        {
            const int length = 1000;
            using (var manager = new UmsManager(FileAccess.Write, length))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.WriteUmsInvariants(stream);

                var bytes = ArrayHelpers.CreateByteArray(length);
                for (int index = 0; index < bytes.Length; index++)
                {
                    stream.WriteByte(bytes[index]);
                }
                var memory = manager.ToArray();
                Assert.Equal(bytes, memory, ArrayHelpers.Comparer<byte>());

                stream.SetLength(0);
                stream.Position = 1;
                bytes = ArrayHelpers.CreateByteArray(length);
                for (int index = 1; index < bytes.Length; index++)
                {
                    stream.WriteByte(bytes[index]);
                }
                stream.Position = 0;
                stream.WriteByte(bytes[0]);
                memory = manager.ToArray();
                Assert.Equal(bytes, memory, ArrayHelpers.Comparer<byte>());
            }
        }

        [Fact]
        public void CannotWriteToReadStream()
        {
            using (var manager = new UmsManager(FileAccess.Read, 1000))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.ReadUmsInvariants(stream);

                var bytes = new byte[3];
                Assert.Throws<NotSupportedException>(() => Write(stream, bytes, 0, bytes.Length));
                Assert.Throws<NotSupportedException>(() => stream.WriteByte(1));
            }
        }

        [Fact]
        public void CannotWriteWithOverflow()
        {
            using (var manager = new UmsManager(FileAccess.Write, 1000))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.WriteUmsInvariants(stream);

                stream.Position = long.MaxValue;
                var bytes = new byte[3];
                Assert.Throws<IOException>(() => Write(stream, bytes, 0, bytes.Length));
                Assert.Throws<IOException>(() => stream.WriteByte(1));
            }
        }
    }
}
