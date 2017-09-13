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

using System.IO;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal sealed partial class GdiPlusStreamHelper
    {
        public Stream stream;

        private StreamGetHeaderDelegate sghd = null;
        private StreamGetBytesDelegate sgbd = null;
        private StreamSeekDelegate skd = null;
        private StreamPutBytesDelegate spbd = null;
        private StreamCloseDelegate scd = null;
        private StreamSizeDelegate ssd = null;
        private byte[] start_buf;
        private int start_buf_pos;
        private int start_buf_len;
        private byte[] managedBuf;
        private const int default_bufsize = 4096;

        public GdiPlusStreamHelper(Stream s, bool seekToOrigin)
        {
            managedBuf = new byte[default_bufsize];

            stream = s;
            if (stream != null && stream.CanSeek && seekToOrigin)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
        }

        public int StreamGetHeaderImpl(IntPtr buf, int bufsz)
        {
            int bytesRead;

            start_buf = new byte[bufsz];

            try
            {
                bytesRead = stream.Read(start_buf, 0, bufsz);
            }
            catch (IOException)
            {
                return -1;
            }

            if (bytesRead > 0 && buf != IntPtr.Zero)
            {
                Marshal.Copy(start_buf, 0, (IntPtr)(buf.ToInt64()), bytesRead);
            }

            start_buf_pos = 0;
            start_buf_len = bytesRead;

            return bytesRead;
        }

        public StreamGetHeaderDelegate GetHeaderDelegate
        {
            get
            {
                if (stream != null && stream.CanRead)
                {
                    if (sghd == null)
                    {
                        sghd = new StreamGetHeaderDelegate(StreamGetHeaderImpl);
                    }
                    return sghd;
                }
                return null;
            }
        }

        public int StreamGetBytesImpl(IntPtr buf, int bufsz, bool peek)
        {
            if (buf == IntPtr.Zero && peek)
            {
                return -1;
            }

            if (bufsz > managedBuf.Length)
                managedBuf = new byte[bufsz];
            int bytesRead = 0;
            long streamPosition = 0;

            if (bufsz > 0)
            {
                if (stream.CanSeek)
                {
                    streamPosition = stream.Position;
                }
                if (start_buf_len > 0)
                {
                    if (start_buf_len > bufsz)
                    {
                        Array.Copy(start_buf, start_buf_pos, managedBuf, 0, bufsz);
                        start_buf_pos += bufsz;
                        start_buf_len -= bufsz;
                        bytesRead = bufsz;
                        bufsz = 0;
                    }
                    else
                    {
                        // this is easy
                        Array.Copy(start_buf, start_buf_pos, managedBuf, 0, start_buf_len);
                        bufsz -= start_buf_len;
                        bytesRead = start_buf_len;
                        start_buf_len = 0;
                    }
                }

                if (bufsz > 0)
                {
                    try
                    {
                        bytesRead += stream.Read(managedBuf, bytesRead, bufsz);
                    }
                    catch (IOException)
                    {
                        return -1;
                    }
                }

                if (bytesRead > 0 && buf != IntPtr.Zero)
                {
                    Marshal.Copy(managedBuf, 0, (IntPtr)(buf.ToInt64()), bytesRead);
                }

                if (!stream.CanSeek && (bufsz == 10) && peek)
                {
                    // Special 'hack' to support peeking of the type for gdi+ on non-seekable streams
                }

                if (peek)
                {
                    if (stream.CanSeek)
                    {
                        // If we are peeking bytes, then go back to original position before peeking
                        stream.Seek(streamPosition, SeekOrigin.Begin);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }

            return bytesRead;
        }

        public StreamGetBytesDelegate GetBytesDelegate
        {
            get
            {
                if (stream != null && stream.CanRead)
                {
                    if (sgbd == null)
                    {
                        sgbd = new StreamGetBytesDelegate(StreamGetBytesImpl);
                    }
                    return sgbd;
                }
                return null;
            }
        }

        public long StreamSeekImpl(int offset, int whence)
        {
            // Make sure we have a valid 'whence'.
            if ((whence < 0) || (whence > 2))
                return -1;

            // Invalidate the start_buf if we're actually going to call a Seek method.
            start_buf_pos += start_buf_len;
            start_buf_len = 0;

            SeekOrigin origin;

            // Translate 'whence' into a SeekOrigin enum member.
            switch (whence)
            {
                case 0:
                    origin = SeekOrigin.Begin;
                    break;
                case 1:
                    origin = SeekOrigin.Current;
                    break;
                case 2:
                    origin = SeekOrigin.End;
                    break;

                // The following line is redundant but necessary to avoid a
                // "Use of unassigned local variable" error without actually
                // initializing 'origin' to a dummy value.
                default:
                    return -1;
            }

            // Do the actual seek operation and return its result.
            return stream.Seek((long)offset, origin);
        }

        public StreamSeekDelegate SeekDelegate
        {
            get
            {
                if (stream != null && stream.CanSeek)
                {
                    if (skd == null)
                    {
                        skd = new StreamSeekDelegate(StreamSeekImpl);
                    }
                    return skd;
                }
                return null;
            }
        }

        public int StreamPutBytesImpl(IntPtr buf, int bufsz)
        {
            if (bufsz > managedBuf.Length)
                managedBuf = new byte[bufsz];
            Marshal.Copy(buf, managedBuf, 0, bufsz);
            stream.Write(managedBuf, 0, bufsz);
            return bufsz;
        }

        public StreamPutBytesDelegate PutBytesDelegate
        {
            get
            {
                if (stream != null && stream.CanWrite)
                {
                    if (spbd == null)
                    {
                        spbd = new StreamPutBytesDelegate(StreamPutBytesImpl);
                    }
                    return spbd;
                }
                return null;
            }
        }

        public void StreamCloseImpl()
        {
            stream.Dispose();
        }

        public StreamCloseDelegate CloseDelegate
        {
            get
            {
                if (stream != null)
                {
                    if (scd == null)
                    {
                        scd = new StreamCloseDelegate(StreamCloseImpl);
                    }
                    return scd;
                }
                return null;
            }
        }

        public long StreamSizeImpl()
        {
            try
            {
                return stream.Length;
            }
            catch
            {
                return -1;
            }
        }

        public StreamSizeDelegate SizeDelegate
        {
            get
            {
                if (stream != null)
                {
                    if (ssd == null)
                    {
                        ssd = new StreamSizeDelegate(StreamSizeImpl);
                    }
                    return ssd;
                }
                return null;
            }
        }
    }
}
