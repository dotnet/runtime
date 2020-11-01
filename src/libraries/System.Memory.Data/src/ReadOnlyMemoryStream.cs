#pragma warning disable SA1636 // File header copyright text should match
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma warning restore SA1636 // File header copyright text should match

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>Provides a <see cref="Stream"/> for the contents of a <see cref="ReadOnlyMemory{Byte}"/>.</summary>
    internal sealed class ReadOnlyMemoryStream : Stream
    {
        private ReadOnlyMemory<byte> _content;
        private bool _isOpen;
        private int _position;

        public ReadOnlyMemoryStream(ReadOnlyMemory<byte> content)
        {
            _content = content;
            _isOpen = true;
        }

        public override bool CanRead => _isOpen;
        public override bool CanSeek => _isOpen;
        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                ValidateNotClosed();
                return _content.Length;
            }
        }

        public override long Position
        {
            get
            {
                ValidateNotClosed();
                return _position;
            }
            set
            {
                ValidateNotClosed();
                if (value < 0 || value > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _position = (int)value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ValidateNotClosed();
            long pos =
                origin == SeekOrigin.Begin ? offset :
                origin == SeekOrigin.Current ? _position + offset :
                origin == SeekOrigin.End ? _content.Length + offset :
                throw new ArgumentOutOfRangeException(nameof(origin));

            if (pos > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            else if (pos < 0)
            {
                throw new IOException("An attempt was made to move the position before the beginning of the stream.");
            }

            _position = (int)pos;
            return _position;
        }

        public override int ReadByte()
        {
            ValidateNotClosed();
            ReadOnlySpan<byte> s = _content.Span;
            return _position < s.Length ? s[_position++] : -1;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateNotClosed();
            ValidateReadArrayArguments(buffer, offset, count);
            return ReadBuffer(new Span<byte>(buffer, offset, count));
        }

        private int ReadBuffer(Span<byte> buffer)
        {
            int remaining = _content.Length - _position;

            if (remaining <= 0 || buffer.Length == 0)
            {
                return 0;
            }
            else if (remaining <= buffer.Length)
            {
                _content.Span.Slice(_position).CopyTo(buffer);
                _position = _content.Length;
                return remaining;
            }
            else
            {
                _content.Span.Slice(_position, buffer.Length).CopyTo(buffer);
                _position += buffer.Length;
                return buffer.Length;
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateNotClosed();
            ValidateReadArrayArguments(buffer, offset, count);
            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled<int>(cancellationToken) :
                Task.FromResult(ReadBuffer(new Span<byte>(buffer, offset, count)));
        }

        public override void Flush() { }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private static void ValidateReadArrayArguments(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || buffer.Length - offset < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        private void ValidateNotClosed()
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException(null, "Cannot access a closed Stream");
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _isOpen = false;
                    _content = default;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
