// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class BufferedStream_InvalidParameters
    {
        [Fact]
        public static void NullConstructor_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BufferedStream(null));
        }

        [Fact]
        public static void NegativeBufferSize_Throws_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BufferedStream(new MemoryStream(), -1));
        }

        [Fact]
        public static void ZeroBufferSize_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BufferedStream(new MemoryStream(), 0));
        }

        [Fact]
        public static void UnderlyingStreamDisposed_Throws_ObjectDisposedException()
        {
            MemoryStream disposedStream = new MemoryStream();
            disposedStream.Dispose();
            Assert.Throws<ObjectDisposedException>(() => new BufferedStream(disposedStream));
        }

        [Fact]
        public static void SetPositionToNegativeValue_Throws_ArgumentOutOfRangeException()
        {
            using (BufferedStream stream = new BufferedStream(new MemoryStream()))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = -1);
            }
        }

        [Fact]
        public static void Read_Arguments()
        {
            using (BufferedStream stream = new BufferedStream(new MemoryStream()))
            {
                byte[] array = new byte[10];
                AssertExtensions.Throws<ArgumentNullException>("array", () => stream.Read(null, 1, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(array, -1, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(array, 1, -1));
                AssertExtensions.Throws<ArgumentException>(null, () => stream.Read(array, 9, 2));
            }
        }

        [Fact]
        public static void Write_Arguments()
        {
            using (BufferedStream stream = new BufferedStream(new MemoryStream()))
            {
                byte[] array = new byte[10];
                AssertExtensions.Throws<ArgumentNullException>("array", () => stream.Write(null, 1, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(array, -1, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(array, 1, -1));
                AssertExtensions.Throws<ArgumentException>(null, () => stream.Write(array, 9, 2));
            }
        }

        [Fact]
        public static void SetLength_NegativeValue()
        {
            using (MemoryStream underlying = new MemoryStream())
            using (BufferedStream stream = new BufferedStream(underlying))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.SetLength(-1));
                stream.SetLength(1);
                Assert.Equal(1, underlying.Length);
                Assert.Equal(1, stream.Length);
            }
        }

        [Fact]
        public static void ReadOnUnreadableStream_Throws_NotSupportedException()
        {
            using (WrappedMemoryStream underlying = new WrappedMemoryStream(false, true, true))
            using (BufferedStream stream = new BufferedStream(underlying))
            {
                Assert.Throws<NotSupportedException>(() => stream.Read(new byte[] { 1 }, 0, 1));
            }
        }

        [Fact]
        public static void WriteOnUnwritableStream_Throws_NotSupportedException()
        {
            using (WrappedMemoryStream underlying = new WrappedMemoryStream(true, false, true))
            using (BufferedStream stream = new BufferedStream(underlying))
            {
                Assert.Throws<NotSupportedException>(() => stream.Write(new byte[] { 1 }, 0, 1));
            }
        }

        [Fact]
        public static void SeekOnUnseekableStream_Throws_NotSupportedException()
        {
            using (WrappedMemoryStream underlying = new WrappedMemoryStream(true, true, false))
            using (BufferedStream stream = new BufferedStream(underlying))
            {
                Assert.Throws<NotSupportedException>(() => stream.Seek(0, new SeekOrigin()));
            }
        }

        [Fact]
        public void CopyToAsync_InvalidArguments_Throws()
        {
            using (var s = new BufferedStream(new MemoryStream()))
            {
                // Null destination
                AssertExtensions.Throws<ArgumentNullException>("destination", () => { s.CopyToAsync(null); });

                // Buffer size out-of-range
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => { s.CopyToAsync(new MemoryStream(), 0); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => { s.CopyToAsync(new MemoryStream(), -1, CancellationToken.None); });

                // Copying to non-writable stream
                Assert.Throws<NotSupportedException>(() => { s.CopyToAsync(new WrappedMemoryStream(canRead: true, canWrite: false, canSeek: true)); });

                // Copying to a non-writable and non-readable stream
                Assert.Throws<ObjectDisposedException>(() => { s.CopyToAsync(new WrappedMemoryStream(canRead: false, canWrite: false, canSeek: false)); });

                // Copying after disposing the buffer stream
                s.Dispose();
                Assert.Throws<ObjectDisposedException>(() => { s.CopyToAsync(new MemoryStream()); });
            }

            // Copying after disposing the underlying stream
            using (var ms = new MemoryStream())
            using (var s = new BufferedStream(ms))
            {
                ms.Dispose();
                Assert.Throws<ObjectDisposedException>(() => { s.CopyToAsync(new MemoryStream()); });
            }

            // Copying from a non-readable source
            using (var s = new BufferedStream(new WrappedMemoryStream(canRead: false, canWrite: true, canSeek: true)))
            {
                Assert.Throws<NotSupportedException>(() => { s.CopyToAsync(new MemoryStream()); });
            }
        }
    }
}
