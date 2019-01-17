// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.GdiPlusStreamHelper.cs
//   - Originally in System.Drawing.gdipFunctions.cs
//
// Authors: 
//    Alexandre Pigolkine (pigolkine@gmx.de)
//    Jordi Mas i Hernandez (jordi@ximian.com)
//    Sanjay Gupta (gsanjay@novell.com)
//    Ravindra (rkumar@novell.com)
//    Peter Dennis Bartok (pbartok@novell.com)
//    Sebastien Pouliot <sebastien@ximian.com>
//
// Copyright (C) 2004 - 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#if netcoreapp20
using System.Buffers;
#endif

using System.IO;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal sealed partial class GdiPlusStreamHelper
    {
        private Stream _stream;

        public unsafe GdiPlusStreamHelper(Stream stream, bool seekToOrigin)
        {
            // Seeking required
            if (!stream.CanSeek)
            {
                var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                _stream = memoryStream;
            }
            else
            {
                _stream = stream;

                if (seekToOrigin)
                {
                    _stream.Seek(0, SeekOrigin.Begin);
                }
            }

            CloseDelegate = StreamCloseImpl;
            GetBytesDelegate = StreamGetBytesImpl;
            GetHeaderDelegate = StreamGetHeaderImpl;
            PutBytesDelegate = StreamPutBytesImpl;
            SeekDelegate = StreamSeekImpl;
            SizeDelegate = StreamSizeImpl;
        }

        public unsafe int StreamGetHeaderImpl(byte* buf, int bufsz)
        {
            return StreamGetBytesImpl(buf, bufsz, peek: true);
        }

        public unsafe int StreamGetBytesImpl(byte* buf, int bufsz, bool peek)
        {
            if ((buf == null && peek) || !_stream.CanRead)
                return -1;

            if (bufsz <= 0)
                return 0;

            int read = 0;
            long originalPosition = 0;
            if (peek)
            {
                originalPosition = _stream.Position;
            }

            try
            {
                // Stream Span API isn't available in 2.0
#if netcoreapp20
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufsz);
                read = _stream.Read(buffer, 0, bufsz);
                Marshal.Copy(buffer, 0, (IntPtr)buf, read);
                ArrayPool<byte>.Shared.Return(buffer);
#else
                Span<byte> buffer = new Span<byte>(buf, bufsz);
                read = _stream.Read(buffer);
#endif
            }
            catch (IOException)
            {
                return -1;
            }

            if (peek)
            {
                // If we are peeking bytes, then go back to original position before peeking
                _stream.Seek(originalPosition, SeekOrigin.Begin);
            }

            return read;
        }

        public long StreamSeekImpl(int offset, int whence)
        {
            // Make sure we have a valid 'whence'.
            if ((whence < 0) || (whence > 2))
                return -1;

            return _stream.Seek((long)offset, (SeekOrigin)whence);
        }

        public unsafe int StreamPutBytesImpl(byte* buf, int bufsz)
        {
            if (!_stream.CanWrite)
                return -1;

            // Stream Span API isn't available in 2.0
#if netcoreapp20
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufsz);
            Marshal.Copy((IntPtr)buf, buffer, 0, bufsz);
            _stream.Write(buffer, 0, bufsz);
            ArrayPool<byte>.Shared.Return(buffer);
#else
            var buffer = new ReadOnlySpan<byte>(buf, bufsz);
            _stream.Write(buffer);
#endif
            return bufsz;
        }

        public void StreamCloseImpl()
        {
            _stream.Dispose();
        }

        public long StreamSizeImpl()
        {
            try
            {
                return _stream.Length;
            }
            catch
            {
                return -1;
            }
        }

        public StreamCloseDelegate CloseDelegate { get; }
        public StreamGetBytesDelegate GetBytesDelegate { get; }
        public StreamGetHeaderDelegate GetHeaderDelegate { get; }
        public StreamPutBytesDelegate PutBytesDelegate { get; }
        public StreamSeekDelegate SeekDelegate { get; }
        public StreamSizeDelegate SizeDelegate { get; }
    }
}
