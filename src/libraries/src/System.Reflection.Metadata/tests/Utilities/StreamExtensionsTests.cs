// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Internal.Tests
{
    public class StreamExtensionsTests
    {
        private static unsafe void InvalidateMemory(byte* buffer, int size)
        {
            for (int i = 0; i < size; i++)
            {
                buffer[i] = 0xfe;
            }
        }

        private static unsafe byte[] ReadBuffer(byte* buffer, int bufferSize)
        {
            byte* p = buffer + bufferSize - 1;
            while (p >= buffer && *p == 0xfe)
            {
                p--;
            }

            return BlobUtilities.ReadBytes(buffer, (int)(p + 1 - buffer));
        }

        [Fact]
        public unsafe void CopyTo1()
        {
            const int bufferSize = 10;
            byte* buffer = (byte*)Marshal.AllocHGlobal(bufferSize);

            try
            {
                var s = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });

                InvalidateMemory(buffer, bufferSize);
                s.Seek(0, SeekOrigin.Begin);
                s.CopyTo(buffer, 3);
                Assert.Equal(new byte[] { 0, 1, 2 }, ReadBuffer(buffer, bufferSize));

                InvalidateMemory(buffer, bufferSize);
                s.Seek(0, SeekOrigin.Begin);
                s.CopyTo(buffer, 0);
                Assert.Equal(new byte[0], ReadBuffer(buffer, bufferSize));

                InvalidateMemory(buffer, bufferSize);
                s.Seek(0, SeekOrigin.Begin);
                s.CopyTo(buffer, 5);
                Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, ReadBuffer(buffer, bufferSize));

                Assert.Throws<IOException>(() => s.CopyTo(buffer, 6));
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)buffer);
            }
        }

        [Fact]
        public unsafe void CopyTo2()
        {
            const int bufferSize = 2 * StreamExtensions.StreamCopyBufferSize;
            byte* buffer = (byte*)Marshal.AllocHGlobal(bufferSize);

            try
            {
                var a = new byte[StreamExtensions.StreamCopyBufferSize + 1];
                for (int i = 0; i < a.Length; i++)
                {
                    a[i] = 1;
                }

                a[a.Length - 2] = 2;
                a[a.Length - 1] = 3;

                var s = new MemoryStream(a);

                InvalidateMemory(buffer, bufferSize);
                s.Seek(0, SeekOrigin.Begin);
                s.CopyTo(buffer, a.Length);

                Assert.Equal(a, ReadBuffer(buffer, bufferSize));
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)buffer);
            }
        }

        private class TestStream : Stream
        {
            private readonly byte[] _buffer;
            private int _position;

            public TestStream(byte[] buffer)
            {
                _buffer = buffer;
            }

            public override bool CanRead { get { throw new NotImplementedException(); } }
            public override bool CanSeek { get { throw new NotImplementedException(); } }
            public override bool CanWrite { get { throw new NotImplementedException(); } }
            public override long Length { get { throw new NotImplementedException(); } }
            public override long Position { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
            public override void Flush() { throw new NotImplementedException(); }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }
            public override void SetLength(long value) { throw new NotImplementedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (count == 0)
                {
                    return 0;
                }

                buffer[offset] = _buffer[_position];
                _position++;
                return 1;
            }
        }

        [Fact]
        public unsafe void CopyTo3()
        {
            const int bufferSize = 64;
            byte* buffer = (byte*)Marshal.AllocHGlobal(bufferSize);

            try
            {
                var s = new TestStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

                InvalidateMemory(buffer, bufferSize);
                s.CopyTo(buffer, 8);

                Assert.Equal(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, ReadBuffer(buffer, bufferSize));
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)buffer);
            }
        }
    }
}
