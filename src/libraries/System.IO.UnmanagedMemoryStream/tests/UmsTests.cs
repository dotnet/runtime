// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Threading;

// TODO: add CopyTo tests

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
                Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Position = unchecked(Int64.MaxValue + 1); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Position = Int32.MinValue; });

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
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.SetLength(SByte.MinValue));

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

                Assert.Throws<IOException>(() => stream.Seek(unchecked(Int32.MaxValue + 1), SeekOrigin.Begin));
                Assert.Throws<IOException>(() => stream.Seek(Int64.MinValue, SeekOrigin.End));
                Assert.Throws<ArgumentException>(() => stream.Seek(0, (SeekOrigin)7)); // Invalid seek origin

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
    }
}
