// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Tests
{
    public class UmsTests
    {
        internal static void UmsInvariants(UnmanagedMemoryStream stream)
        {
            Assert.False(stream.CanTimeout);
        }

        internal static void ReadUmsInvariants(UnmanagedMemoryStream stream)
        {
            UmsInvariants(stream);
            Assert.True(stream.CanRead);
            Assert.False(stream.CanWrite);
            Assert.True(stream.CanSeek);
            Assert.Throws<NotSupportedException>(() => stream.SetLength(1000));
        }

        internal static void WriteUmsInvariants(UnmanagedMemoryStream stream)
        {
            UmsInvariants(stream);
            Assert.False(stream.CanRead);
            Assert.True(stream.CanWrite);
            Assert.True(stream.CanSeek);
        }

        internal static void ReadWriteUmsInvariants(UnmanagedMemoryStream stream)
        {
            UmsInvariants(stream);
            Assert.True(stream.CanRead);
            Assert.True(stream.CanWrite);
            Assert.True(stream.CanSeek);
        }

        [Fact]
        public static void PositionTests()
        {
            using (var manager = new UmsManager(FileAccess.ReadWrite, 1000))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.ReadWriteUmsInvariants(stream);

                Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Position = -1; }); // "Non-negative number required."
                Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Position = unchecked(long.MaxValue + 1); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Position = int.MinValue; });

                stream.Position = stream.Length;
                Assert.Equal(stream.Position, stream.Length);

                stream.Position = stream.Capacity;
                Assert.Equal(stream.Position, stream.Capacity);

                int mid = (int)stream.Length / 2;
                stream.Position = mid;
                Assert.Equal(stream.Position, mid);
            }
        }

        [Fact]
        public static void LengthTests()
        {
            using (var manager = new UmsManager(FileAccess.ReadWrite, 1000))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.ReadWriteUmsInvariants(stream);
                Assert.Throws<IOException>(() => stream.SetLength(1001)); 
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.SetLength(sbyte.MinValue));

                const long expectedLength = 500;
                stream.Position = 501;
                stream.SetLength(expectedLength);
                Assert.Equal(expectedLength, stream.Length);
                Assert.Equal(expectedLength, stream.Position);
            }
        }

        [Fact]
        public static void SeekTests()
        {
            const int length = 1000;
            using (var manager = new UmsManager(FileAccess.ReadWrite, length))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.ReadWriteUmsInvariants(stream);

                Assert.Throws<IOException>(() => stream.Seek(unchecked(int.MaxValue + 1), SeekOrigin.Begin));
                Assert.Throws<IOException>(() => stream.Seek(long.MinValue, SeekOrigin.End));
                AssertExtensions.Throws<ArgumentException>(null, () => stream.Seek(0, (SeekOrigin)7)); // Invalid seek origin

                stream.Seek(10, SeekOrigin.Begin);
                Assert.Equal(10, stream.Position);

                Assert.Throws<IOException>(() => stream.Seek(-1, SeekOrigin.Begin)); // An attempt was made to move the position before the beginning of the stream 
                Assert.Equal(10, stream.Position);

                Assert.Throws<IOException>(() => stream.Seek(-(stream.Position + 1), SeekOrigin.Current)); // An attempt was made to move the position before the beginning of the stream
                Assert.Equal(10, stream.Position);

                Assert.Throws<IOException>(() => stream.Seek(-(stream.Length + 1), SeekOrigin.End)); //  "An attempt was made to move the position before the beginning of the stream."
                Assert.Equal(10, stream.Position);

                // Seek from SeekOrigin.Begin
                stream.Seek(0, SeekOrigin.Begin);
                for (int position = 0; position < stream.Length; position++)
                {
                    stream.Seek(position, SeekOrigin.Begin);
                    Assert.Equal(position, stream.Position);
                }

                for (int position = (int)stream.Length; position >= 0; position--)
                {
                    stream.Seek(position, SeekOrigin.Begin);
                    Assert.Equal(position, stream.Position);
                }

                stream.Seek(0, SeekOrigin.Begin);
                // Seek from SeekOrigin.End
                for (int position = 0; position < stream.Length; position++)
                {
                    stream.Seek(-position, SeekOrigin.End);
                    Assert.Equal(length - position, stream.Position);
                }

                for (int position = (int)stream.Length; position >= 0; position--)
                {
                    stream.Seek(-position, SeekOrigin.End);
                    Assert.Equal(length - position, stream.Position);
                }

                // Seek from SeekOrigin.Current
                stream.Seek(0, SeekOrigin.Begin);
                for (int position = 0; position < stream.Length; position++)
                {
                    stream.Seek(1, SeekOrigin.Current);
                    Assert.Equal(position + 1, stream.Position);
                }

                for (int position = (int)stream.Length; position > 0; position--)
                {
                    stream.Seek(-1, SeekOrigin.Current);
                    Assert.Equal(position - 1, stream.Position);
                }
            }
        }

        [Fact]
        public static void CannotUseStreamAfterDispose()
        {
            using (var manager = new UmsManager(FileAccess.ReadWrite, 1000))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.ReadWriteUmsInvariants(stream);

                stream.Dispose();
                Assert.False(stream.CanRead);
                Assert.False(stream.CanWrite);
                Assert.False(stream.CanSeek);

                Assert.Throws<ObjectDisposedException>(() => { long x = stream.Capacity; });

                Assert.Throws<ObjectDisposedException>(() => { long y = stream.Length; });
                Assert.Throws<ObjectDisposedException>(() => { stream.SetLength(2); });

                Assert.Throws<ObjectDisposedException>(() => { long z = stream.Position; });
                Assert.Throws<ObjectDisposedException>(() => { stream.Position = 2; });

                Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Begin));
                Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Current));
                Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.End));

                Assert.Throws<ObjectDisposedException>(() => stream.Flush());
                Assert.Throws<ObjectDisposedException>(() => stream.FlushAsync(CancellationToken.None).GetAwaiter().GetResult());

                byte[] buffer = ArrayHelpers.CreateByteArray(10);
                Assert.Throws<ObjectDisposedException>(() => stream.Read(buffer, 0, buffer.Length));
                Assert.Throws<ObjectDisposedException>(() => stream.ReadAsync(buffer, 0, buffer.Length).GetAwaiter().GetResult());
                Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());

                Assert.Throws<ObjectDisposedException>(() => stream.WriteByte(0));
                Assert.Throws<ObjectDisposedException>(() => stream.Write(buffer, 0, buffer.Length));
                Assert.Throws<ObjectDisposedException>(() => stream.WriteAsync(buffer, 0, buffer.Length).GetAwaiter().GetResult());
            }
        }

        [Fact]
        public static void CopyToTest()
        {
            byte[] testData = ArrayHelpers.CreateByteArray(8192);

            using (var manager = new UmsManager(FileAccess.Read, testData))
            {
                UnmanagedMemoryStream ums = manager.Stream;
                UmsTests.ReadUmsInvariants(ums);
                MemoryStream destination = new MemoryStream();

                destination.Position = 0;
                ums.CopyTo(destination);
                Assert.Equal(testData, destination.ToArray());

                destination.Position = 0;
                ums.CopyTo(destination, 1);
                Assert.Equal(testData, destination.ToArray());
            }

            // copy to disposed stream should throw
            using (var manager = new UmsManager(FileAccess.Read, testData))
            {
                UnmanagedMemoryStream ums = manager.Stream;
                UmsTests.ReadUmsInvariants(ums);

                MemoryStream destination = new MemoryStream();
                destination.Dispose();

                Assert.Throws<ObjectDisposedException>(() => ums.CopyTo(destination));
            }

            // copy from disposed stream should throw
            using (var manager = new UmsManager(FileAccess.Read, testData))
            {
                UnmanagedMemoryStream ums = manager.Stream;
                UmsTests.ReadUmsInvariants(ums);
                ums.Dispose();

                MemoryStream destination = new MemoryStream();

                Assert.Throws<ObjectDisposedException>(() => ums.CopyTo(destination));
            }

            // copying to non-writable stream should throw
            using (var manager = new UmsManager(FileAccess.Read, testData))
            {
                UnmanagedMemoryStream ums = manager.Stream;
                UmsTests.ReadUmsInvariants(ums);

                MemoryStream destination = new MemoryStream(new byte[0], false);

                Assert.Throws<NotSupportedException>(() => ums.CopyTo(destination));
            }

            // copying from non-readable stream should throw
            using (var manager = new UmsManager(FileAccess.Write, testData))
            {
                UnmanagedMemoryStream ums = manager.Stream;
                UmsTests.WriteUmsInvariants(ums);

                MemoryStream destination = new MemoryStream(new byte[0], false);

                Assert.Throws<NotSupportedException>(() => ums.CopyTo(destination));
            }
        }

        [Fact]
        public static async Task CopyToAsyncTest()
        {
            byte[] testData = ArrayHelpers.CreateByteArray(8192);

            using (var manager = new UmsManager(FileAccess.Read, testData))
            {
                UnmanagedMemoryStream ums = manager.Stream;
                UmsTests.ReadUmsInvariants(ums);
                MemoryStream destination = new MemoryStream();

                destination.Position = 0;
                await ums.CopyToAsync(destination);
                Assert.Equal(testData, destination.ToArray());

                destination.Position = 0;
                await ums.CopyToAsync(destination, 2);
                Assert.Equal(testData, destination.ToArray());

                destination.Position = 0;
                await ums.CopyToAsync(destination, 0x1000, new CancellationTokenSource().Token);
                Assert.Equal(testData, destination.ToArray());

                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ums.CopyToAsync(destination, 0x1000, new CancellationToken(true)));
            }

            // copy to disposed stream should throw
            using (var manager = new UmsManager(FileAccess.Read, testData))
            {
                UnmanagedMemoryStream ums = manager.Stream;
                UmsTests.ReadUmsInvariants(ums);

                MemoryStream destination = new MemoryStream();
                destination.Dispose();

                await Assert.ThrowsAsync<ObjectDisposedException>(() => ums.CopyToAsync(destination));
            }

            // copy from disposed stream should throw
            using (var manager = new UmsManager(FileAccess.Read, testData))
            {
                UnmanagedMemoryStream ums = manager.Stream;
                UmsTests.ReadUmsInvariants(ums);
                ums.Dispose();

                MemoryStream destination = new MemoryStream();

                await Assert.ThrowsAsync<ObjectDisposedException>(() => ums.CopyToAsync(destination));
            }

            // copying to non-writable stream should throw
            using (var manager = new UmsManager(FileAccess.Read, testData))
            {
                UnmanagedMemoryStream ums = manager.Stream;
                UmsTests.ReadUmsInvariants(ums);

                MemoryStream destination = new MemoryStream(new byte[0], false);

                await Assert.ThrowsAsync<NotSupportedException>(() => ums.CopyToAsync(destination));
            }

            // copying from non-readable stream should throw
            using (var manager = new UmsManager(FileAccess.Write, testData))
            {
                UnmanagedMemoryStream ums = manager.Stream;
                UmsTests.WriteUmsInvariants(ums);

                MemoryStream destination = new MemoryStream(new byte[0], false);

                await Assert.ThrowsAsync<NotSupportedException>(() => ums.CopyToAsync(destination));
            }
        }
    }
}
