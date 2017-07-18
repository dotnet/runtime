// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Drawing.Internal
{
    internal class GPStream : UnsafeNativeMethods.IStream
    {
        protected Stream dataStream;

        // to support seeking ahead of the stream length...
        private long _virtualPosition = -1;

        internal GPStream(Stream stream)
        {
            if (!stream.CanSeek)
            {
                const int ReadBlock = 256;
                byte[] bytes = new byte[ReadBlock];
                int readLen;
                int current = 0;
                do
                {
                    if (bytes.Length < current + ReadBlock)
                    {
                        byte[] newData = new byte[bytes.Length * 2];
                        Array.Copy(bytes, newData, bytes.Length);
                        bytes = newData;
                    }

                    readLen = stream.Read(bytes, current, ReadBlock);
                    current += readLen;
                } while (readLen != 0);

                dataStream = new MemoryStream(bytes);
            }
            else
            {
                dataStream = stream;
            }
        }

        private void ActualizeVirtualPosition()
        {
            if (_virtualPosition == -1)
            {
                return;
            }

            if (_virtualPosition > dataStream.Length)
            {
                dataStream.SetLength(_virtualPosition);
            }

            dataStream.Position = _virtualPosition;

            _virtualPosition = -1;
        }

        public virtual UnsafeNativeMethods.IStream Clone()
        {
            NotImplemented();
            return null;
        }

        public virtual void Commit(int grfCommitFlags)
        {
            dataStream.Flush();
            // Extend the length of the file if needed.
            ActualizeVirtualPosition();
        }

        public virtual long CopyTo(UnsafeNativeMethods.IStream pstm, long cb, long[] pcbRead)
        {
            int bufsize = 4096; // one page
            IntPtr buffer = Marshal.AllocHGlobal(bufsize);
            if (buffer == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            long written = 0;
            try
            {
                while (written < cb)
                {
                    int toRead = bufsize;
                    if (written + toRead > cb)
                    {
                        toRead = (int)(cb - written);
                    }

                    int read = Read(buffer, toRead);
                    if (read == 0)
                    {
                        break;
                    }

                    if (pstm.Write(buffer, read) != read)
                    {
                        throw EFail("Wrote an incorrect number of bytes");
                    }

                    written += read;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            if (pcbRead != null && pcbRead.Length > 0)
            {
                pcbRead[0] = written;
            }

            return written;
        }

        public virtual void LockRegion(long libOffset, long cb, int dwLockType)
        {
        }

        protected static ExternalException EFail(string msg)
        {
            throw new ExternalException(msg, SafeNativeMethods.E_FAIL);
        }

        protected static void NotImplemented()
        {
            throw new ExternalException(SR.Format(SR.NotImplemented), SafeNativeMethods.E_NOTIMPL);
        }

        public virtual int Read(IntPtr buf, int length)
        {
            byte[] buffer = new byte[length];
            int count = Read(buffer, length);
            Marshal.Copy(buffer, 0, buf, length);
            return count;
        }

        public virtual int Read(byte[] buffer, int length)
        {
            ActualizeVirtualPosition();
            return dataStream.Read(buffer, 0, length);
        }

        public virtual void Revert() => NotImplemented();

        public virtual long Seek(long offset, int origin)
        {
            long pos = _virtualPosition;
            if (_virtualPosition == -1)
            {
                pos = dataStream.Position;
            }

            long len = dataStream.Length;
            switch (origin)
            {
                case SafeNativeMethods.StreamConsts.STREAM_SEEK_SET:
                    if (offset <= len)
                    {
                        dataStream.Position = offset;
                        _virtualPosition = -1;
                    }
                    else
                    {
                        _virtualPosition = offset;
                    }
                    break;
                case SafeNativeMethods.StreamConsts.STREAM_SEEK_END:
                    if (offset <= 0)
                    {
                        dataStream.Position = len + offset;
                        _virtualPosition = -1;
                    }
                    else
                    {
                        _virtualPosition = len + offset;
                    }
                    break;
                case SafeNativeMethods.StreamConsts.STREAM_SEEK_CUR:
                    if (offset + pos <= len)
                    {
                        dataStream.Position = pos + offset;
                        _virtualPosition = -1;
                    }
                    else
                    {
                        _virtualPosition = offset + pos;
                    }
                    break;
            }

            if (_virtualPosition != -1)
            {
                return _virtualPosition;
            }
            else
            {
                return dataStream.Position;
            }
        }

        public virtual void SetSize(long value) => dataStream.SetLength(value);

        public void Stat(IntPtr pstatstg, int grfStatFlag)
        {
            var stats = new STATSTG { cbSize = dataStream.Length };
            Marshal.StructureToPtr(stats, pstatstg, true);
        }

        public virtual void UnlockRegion(long libOffset, long cb, int dwLockType)
        {
        }

        public virtual int Write(IntPtr buf, int length)
        {
            byte[] buffer = new byte[length];
            Marshal.Copy(buf, buffer, 0, length);
            return Write(buffer, length);
        }

        public virtual int Write(byte[] buffer, int length)
        {
            ActualizeVirtualPosition();
            dataStream.Write(buffer, 0, length);
            return length;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class STATSTG
        {
            public IntPtr pwcsName = IntPtr.Zero;
            public int type;
            [MarshalAs(UnmanagedType.I8)]
            public long cbSize;
            [MarshalAs(UnmanagedType.I8)]
            public long mtime;
            [MarshalAs(UnmanagedType.I8)]
            public long ctime;
            [MarshalAs(UnmanagedType.I8)]
            public long atime;
            [MarshalAs(UnmanagedType.I4)]
            public int grfMode;
            [MarshalAs(UnmanagedType.I4)]
            public int grfLocksSupported;

            public int clsid_data1;
            [MarshalAs(UnmanagedType.I2)]
            public short clsid_data2;
            [MarshalAs(UnmanagedType.I2)]
            public short clsid_data3;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b0;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b1;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b2;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b3;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b4;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b5;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b6;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b7;
            [MarshalAs(UnmanagedType.I4)]
            public int grfStateBits;
            [MarshalAs(UnmanagedType.I4)]
            public int reserved;
        }
    }
}
