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

        public override long Length
        {
            get {
                // ZipArchiveEntry's read stream doesn't provide a length since it's a raw DeflateStream
                // Return length from the archive entry.
                if (_packageFileAccess == FileAccess.Read)
                    return _zipArchiveEntry.Length;
                else
                    return _baseStream.Length;
            }
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
