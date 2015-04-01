// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public class GZipStream : Stream
    {
        private DeflateStream deflateStream;


        public GZipStream(Stream stream, CompressionMode mode)

            : this(stream, mode, false)
        {
        }


        public GZipStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            deflateStream = new DeflateStream(stream, mode, leaveOpen);
            SetDeflateStreamFileFormatter(mode);
        }


        // Implies mode = Compress
        public GZipStream(Stream stream, CompressionLevel compressionLevel)

            : this(stream, compressionLevel, false)
        {
        }


        // Implies mode = Compress
        public GZipStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
        {
            deflateStream = new DeflateStream(stream, compressionLevel, leaveOpen);
            SetDeflateStreamFileFormatter(CompressionMode.Compress);
        }


        private void SetDeflateStreamFileFormatter(CompressionMode mode)
        {
            if (mode == CompressionMode.Compress)
            {
                IFileFormatWriter writeCommand = new GZipFormatter();
                deflateStream.SetFileFormatWriter(writeCommand);
            }
            else
            {
                IFileFormatReader readCommand = new GZipDecoder();
                deflateStream.SetFileFormatReader(readCommand);
            }
        }


        public override bool CanRead
        {
            get
            {
                if (deflateStream == null)
                {
                    return false;
                }

                return deflateStream.CanRead;
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (deflateStream == null)
                {
                    return false;
                }

                return deflateStream.CanWrite;
            }
        }

        public override bool CanSeek
        {
            get
            {
                if (deflateStream == null)
                {
                    return false;
                }

                return deflateStream.CanSeek;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException(SR.NotSupported);
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException(SR.NotSupported);
            }

            set
            {
                throw new NotSupportedException(SR.NotSupported);
            }
        }

        public override void Flush()
        {
            CheckDeflateStream();
            deflateStream.Flush();
            return;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        public override int Read(byte[] array, int offset, int count)
        {
            CheckDeflateStream();
            return deflateStream.Read(array, offset, count);
        }

        public override void Write(byte[] array, int offset, int count)
        {
            CheckDeflateStream();
            deflateStream.Write(array, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && deflateStream != null)
                {
                    deflateStream.Dispose();
                }
                deflateStream = null;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public Stream BaseStream
        {
            get
            {
                if (deflateStream != null)
                {
                    return deflateStream.BaseStream;
                }
                else
                {
                    return null;
                }
            }
        }

        public override Task<int> ReadAsync(Byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDeflateStream();
            return deflateStream.ReadAsync(array, offset, count, cancellationToken);
        }

        public override Task WriteAsync(Byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDeflateStream();
            return deflateStream.WriteAsync(array, offset, count, cancellationToken);
        }

        private void CheckDeflateStream()
        {
            if (deflateStream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
            }
        }
    }
}
