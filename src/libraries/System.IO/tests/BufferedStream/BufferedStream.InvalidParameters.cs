// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
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
                Assert.Throws<ArgumentNullException>("array", () => stream.Read(null, 1, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(array, -1, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(array, 1, -1));
                Assert.Throws<ArgumentException>(() => stream.Read(array, 9, 2));
            }
        }

        [Fact]
        public static void Write_Arguments()
        {
            using (BufferedStream stream = new BufferedStream(new MemoryStream()))
            {
                byte[] array = new byte[10];
                Assert.Throws<ArgumentNullException>("array", () => stream.Write(null, 1, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(array, -1, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(array, 1, -1));
                Assert.Throws<ArgumentException>(() => stream.Write(array, 9, 2));
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
    }
}
