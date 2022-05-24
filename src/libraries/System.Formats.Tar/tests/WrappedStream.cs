// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Formats.Tar
{
    public class WrappedStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly EventHandler _onClosed;
        private bool _canRead, _canWrite, _canSeek;

        public WrappedStream(Stream baseStream, bool canRead, bool canWrite, bool canSeek, EventHandler onClosed = null)
        {
            _baseStream = baseStream;
            _onClosed = onClosed;
            _canRead = canRead;
            _canSeek = canSeek;
            _canWrite = canWrite;
        }

        public override void Flush() => _baseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (CanRead)
            {
                try
                {
                    return _baseStream.Read(buffer, offset, count);
                }
                catch (ObjectDisposedException ex)
                {
                    throw new InvalidOperationException("This stream does not support reading", ex);
                }
            }
            else throw new InvalidOperationException("This stream does not support reading");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (CanSeek)
            {
                try
                {
                    return _baseStream.Seek(offset, origin);
                }
                catch (ObjectDisposedException ex)
                {
                    throw new InvalidOperationException("This stream does not support seeking", ex);
                }
            }
            else throw new InvalidOperationException("This stream does not support seeking");
        }

        public override void SetLength(long value) { _baseStream.SetLength(value); }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (CanWrite)
            {
                try
                {
                    _baseStream.Write(buffer, offset, count);
                }
                catch (ObjectDisposedException ex)
                {
                    throw new InvalidOperationException("This stream does not support writing", ex);
                }
            }
            else throw new InvalidOperationException("This stream does not support writing");
        }

        public override bool CanRead => _canRead && _baseStream.CanRead;

        public override bool CanSeek => _canSeek && _baseStream.CanSeek;

        public override bool CanWrite => _canWrite && _baseStream.CanWrite;

        public override long Length
        {
            get
            {
                if (!CanSeek)
                {
                    throw new InvalidOperationException("This stream does not support seeking.");
                }
                return _baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                if (!CanSeek)
                {
                    throw new InvalidOperationException("This stream does not support seeking");
                }
                return _baseStream.Position;
            }
            set
            {
                if (CanSeek)
                {
                    try
                    {
                        _baseStream.Position = value;
                    }
                    catch (ObjectDisposedException ex)
                    {
                        throw new InvalidOperationException("This stream does not support seeking", ex);
                    }
                }
                else throw new InvalidOperationException("This stream does not support seeking");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _onClosed?.Invoke(this, null);
                _canRead = false;
                _canWrite = false;
                _canSeek = false;
            }
            base.Dispose(disposing);
        }
    }
}