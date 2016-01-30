// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Packaging
{
    internal class ZipWrappingStream : Stream
    {
        private Stream _baseStream;
        private ZipArchiveEntry _zipArchiveEntry;
        private FileMode _packageFileMode;
        private FileAccess _packageFileAccess;
        private bool _canRead;
        private bool _canWrite;

        public ZipWrappingStream(ZipArchiveEntry zipArchiveEntry, Stream stream, FileMode packageFileMode, FileAccess packageFileAccess, bool canRead, bool canWrite)
        {
            _zipArchiveEntry = zipArchiveEntry;
            _baseStream = stream;
            _packageFileMode = packageFileMode;
            _packageFileAccess = packageFileAccess;
            _canRead = canRead;
            _canWrite = canWrite;
        }

        public override bool CanRead
        {
            get
            {
                if (_canRead == false)
                    return false;
                return _baseStream.CanRead;
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (_canWrite == false)
                    return false;
                return _baseStream.CanWrite;
            }
        }

        public override void Write(
	        byte[] buffer,
	        int offset,
	        int count
        )
        {
            _baseStream.Write(buffer, offset, count);
        }

        public override int Read(
	        byte[] buffer,
	        int offset,
	        int count
        )
        {
            return _baseStream.Read(buffer, offset, count);
        }

        public override void SetLength(
	        long value
        )
        {
            _baseStream.SetLength(value);
        }

        public override long Seek(
	        long offset,
	        SeekOrigin origin
        )
        {
            return _baseStream.Seek(offset, origin);
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override long Position
        {
            get
            {
                return _baseStream.Position;
            }
            set
            {
                _baseStream.Position = value;
            }
        }

        // For some strange reason, if the package FileAccess == Read,
        // then can't get length from the stream.  Not supported.
        // The workaround - write the stream to a memory stream, then return the length of the
        // memory stream.
        public override long Length
        {
            get {
                if (_packageFileAccess == FileAccess.Read)
                {
                    using (MemoryStream ms = new MemoryStream())
                    using (Stream zs = _zipArchiveEntry.Open())
                    {
                        CopyStream(zs, ms);
                        return ms.Length;
                    }
                }
                else
                    return _baseStream.Length;
            }
        }

        private static void CopyStream(Stream source, Stream target)
        {
            const int BufSize = 0x4096;
            byte[] buf = new byte[BufSize];
            int bytesRead = 0;
            while ((bytesRead = source.Read(buf, 0, BufSize)) > 0)
                target.Write(buf, 0, bytesRead);
        }

        public override bool CanSeek
        {
            get { return _baseStream.CanSeek; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _baseStream.Dispose();
        }
    }
}
