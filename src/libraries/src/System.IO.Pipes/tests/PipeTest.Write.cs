// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Tests that cover Write and WriteAsync behaviors that are shared between
    /// AnonymousPipes and NamedPipes
    /// </summary>
    public abstract partial class PipeTest_Write : PipeTestBase
    {
        public virtual bool SupportsBidirectionalReadingWriting => false;

        [Fact]
        public void WriteWithNullBuffer_Throws_ArgumentNullException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.writeablePipe;
                Assert.True(pipe.IsConnected);
                Assert.True(pipe.CanWrite);

                // Null is an invalid Buffer
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => pipe.Write(null, 0, 1));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => { pipe.WriteAsync(null, 0, 1); });

                // Buffer validity is checked before Offset
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => pipe.Write(null, -1, 1));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => { pipe.WriteAsync(null, -1, 1); });

                // Buffer validity is checked before Count
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => pipe.Write(null, -1, -1));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => { pipe.WriteAsync(null, -1, -1); });

            }
        }

        [Fact]
        public void WriteWithNegativeOffset_Throws_ArgumentOutOfRangeException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.writeablePipe;
                Assert.True(pipe.IsConnected);
                Assert.True(pipe.CanWrite);
                Assert.False(pipe.CanSeek);

                // Offset must be nonnegative
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => pipe.Write(new byte[5], -1, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => { pipe.WriteAsync(new byte[5], -1, 1); });
            }
        }

        [Fact]
        public void WriteWithNegativeCount_Throws_ArgumentOutOfRangeException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.writeablePipe;
                Assert.True(pipe.IsConnected);
                Assert.True(pipe.CanWrite);

                // Count must be nonnegative
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => pipe.Write(new byte[5], 0, -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => { pipe.WriteAsync(new byte[5], 0, -1); });
            }
        }

        [Fact]
        public void WriteWithOutOfBoundsArray_Throws_ArgumentException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.writeablePipe;
                Assert.True(pipe.IsConnected);
                Assert.True(pipe.CanWrite);

                // offset out of bounds
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Write(new byte[1], 1, 1));

                // offset out of bounds for 0 count read
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Write(new byte[1], 2, 0));

                // offset out of bounds even for 0 length buffer
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Write(new byte[0], 1, 0));

                // combination offset and count out of bounds
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Write(new byte[2], 1, 2));

                // edges
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Write(new byte[0], int.MaxValue, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Write(new byte[0], int.MaxValue, int.MaxValue));

                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Write(new byte[5], 3, 4));

                // offset out of bounds
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[1], 1, 1); });

                // offset out of bounds for 0 count read
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[1], 2, 0); });

                // offset out of bounds even for 0 length buffer
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[0], 1, 0); });

                // combination offset and count out of bounds
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[2], 1, 2); });

                // edges
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[0], int.MaxValue, 0); });
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[0], int.MaxValue, int.MaxValue); });

                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[5], 3, 4); });
            }
        }

        [Fact]
        public void ReadOnWriteOnlyPipe_Throws_NotSupportedException()
        {
            if (SupportsBidirectionalReadingWriting)
            {
                return;
            }

            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.writeablePipe;
                Assert.True(pipe.IsConnected);
                Assert.False(pipe.CanRead);

                Assert.Throws<NotSupportedException>(() => pipe.Read(new byte[9], 0, 5));

                Assert.Throws<NotSupportedException>(() => pipe.ReadByte());

                Assert.Throws<NotSupportedException>(() => pipe.InBufferSize);

                Assert.Throws<NotSupportedException>(() => { pipe.ReadAsync(new byte[10], 0, 5); });
            }
        }

        [Fact]
        public async Task WriteZeroLengthBuffer_Nop()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.writeablePipe;

                // Shouldn't throw
                pipe.Write(Array.Empty<byte>(), 0, 0);

                Task writeTask = pipe.WriteAsync(Array.Empty<byte>(), 0, 0);
                await writeTask;
            }
        }

        [Fact]
        public void WritePipeUnsupportedMembers_Throws_NotSupportedException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.writeablePipe;
                Assert.True(pipe.IsConnected);

                Assert.Throws<NotSupportedException>(() => pipe.Length);

                Assert.Throws<NotSupportedException>(() => pipe.SetLength(10L));

                Assert.Throws<NotSupportedException>(() => pipe.Position);

                Assert.Throws<NotSupportedException>(() => pipe.Position = 10L);

                Assert.Throws<NotSupportedException>(() => pipe.Seek(10L, System.IO.SeekOrigin.Begin));
            }
        }

        [Fact]
        public void WriteToDisposedWriteablePipe_Throws_ObjectDisposedException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.writeablePipe;
                pipe.Dispose();
                byte[] buffer = new byte[] { 0, 0, 0, 0 };

                Assert.Throws<ObjectDisposedException>(() => pipe.Write(buffer, 0, buffer.Length));
                Assert.Throws<ObjectDisposedException>(() => pipe.WriteByte(5));
                Assert.Throws<ObjectDisposedException>(() => { pipe.WriteAsync(buffer, 0, buffer.Length); });
                Assert.Throws<ObjectDisposedException>(() => pipe.Flush());
                Assert.Throws<ObjectDisposedException>(() => pipe.IsMessageComplete);
                Assert.Throws<ObjectDisposedException>(() => pipe.ReadMode);
            }
        }

        [Fact]
        public virtual void WriteToPipeWithClosedPartner_Throws_IOException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    (pair.readablePipe is NamedPipeClientStream || pair.writeablePipe is NamedPipeClientStream))
                {
                    // On Unix, NamedPipe*Stream is implemented in term of sockets, where information 
                    // about shutdown is not immediately propagated.
                    return;
                }

                pair.readablePipe.Dispose();
                byte[] buffer = new byte[] { 0, 0, 0, 0 };

                Assert.Throws<IOException>(() => pair.writeablePipe.Write(buffer, 0, buffer.Length));
                Assert.Throws<IOException>(() => pair.writeablePipe.WriteByte(123));
                Assert.Throws<IOException>(() => { pair.writeablePipe.WriteAsync(buffer, 0, buffer.Length); });
                Assert.Throws<IOException>(() => pair.writeablePipe.Flush());
            }
        }

        [Fact]
        [ActiveIssue("dotnet/corefx #19287", TargetFrameworkMonikers.NetFramework)]
        public async Task ValidFlush_DoesntThrow()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                Task write = Task.Run(() => pair.writeablePipe.WriteByte(123));
                pair.writeablePipe.Flush();
                Assert.Equal(123, pair.readablePipe.ReadByte());
                await write;
                await pair.writeablePipe.FlushAsync();
            }
        }
    }
}
