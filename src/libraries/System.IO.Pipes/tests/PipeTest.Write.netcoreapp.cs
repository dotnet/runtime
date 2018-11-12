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
        [Fact]
        public void ReadOnWriteOnlyPipe_Span_Throws_NotSupportedException()
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

                Assert.Throws<NotSupportedException>(() => pipe.Read(new Span<byte>(new byte[9])));
                Assert.Throws<NotSupportedException>(() => { pipe.ReadAsync(new Memory<byte>(new byte[10])); });
            }
        }

        [Fact]
        public async Task WriteZeroLengthBuffer_Span_Nop()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.writeablePipe;

                // Shouldn't throw
                pipe.Write(new Span<byte>(Array.Empty<byte>()));
                await pipe.WriteAsync(new Memory<byte>(Array.Empty<byte>()));
            }
        }

        [Fact]
        public void WriteToDisposedWriteablePipe_Span_Throws_ObjectDisposedException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.writeablePipe;
                pipe.Dispose();
                byte[] buffer = new byte[] { 0, 0, 0, 0 };

                Assert.Throws<ObjectDisposedException>(() => pipe.Write(new Span<byte>(buffer)));
                Assert.Throws<ObjectDisposedException>(() => { pipe.WriteAsync(new Memory<byte>(buffer)); });
            }
        }

        [Fact]
        public virtual void WriteToPipeWithClosedPartner_Span_Throws_IOException()
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

                Assert.Throws<IOException>(() => pair.writeablePipe.Write(new Span<byte>(buffer)));
                Assert.Throws<IOException>(() => { pair.writeablePipe.WriteAsync(new Memory<byte>(buffer)); });
            }
        }

        [Fact]
        public void DisposeAsync_NothingWrittenNeedsToBeFlushed_CompletesSynchronously()
        {
            ServerClientPair pair = CreateServerClientPair();
            for (int i = 0; i < 2; i++)
            {
                Assert.True(pair.readablePipe.DisposeAsync().IsCompletedSuccessfully);
                Assert.True(pair.writeablePipe.DisposeAsync().IsCompletedSuccessfully);
            }
            Assert.Throws<ObjectDisposedException>(() => pair.writeablePipe.Write(new Span<byte>(new byte[1])));
        }
    }
}
