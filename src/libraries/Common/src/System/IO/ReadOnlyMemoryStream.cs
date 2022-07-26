// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>Provides a <see cref="Stream"/> for the contents of a <see cref="ReadOnlyMemory{Byte}"/>.</summary>
    internal sealed class ReadOnlyMemoryStream : Stream
    {
        private ReadOnlyMemory<byte> _content;
        private int _position;
        private bool _isOpen;

        public ReadOnlyMemoryStream(ReadOnlyMemory<byte> content)
        {
            _content = content;
            _isOpen = true;
        }

        public override bool CanRead => _isOpen;
        public override bool CanSeek => _isOpen;
        public override bool CanWrite => false;

        private void EnsureNotClosed()
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
            }
        }

        public override long Length
        {
            get
            {
                EnsureNotClosed();
                return _content.Length;
            }
        }

        public override long Position
        {
            get
            {
                EnsureNotClosed();
                return _position;
            }
            set
            {
                EnsureNotClosed();
                if (value < 0 || value > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _position = (int)value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            EnsureNotClosed();

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
                throw new IOException(SR.IO_SeekBeforeBegin);
            }

            _position = (int)pos;
            return _position;
        }

        public override int ReadByte()
        {
            EnsureNotClosed();

            ReadOnlySpan<byte> s = _content.Span;
            return _position < s.Length ? s[_position++] : -1;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadBuffer(new Span<byte>(buffer, offset, count));
        }

#if !NETFRAMEWORK && !NETSTANDARD2_0
        public override int Read(Span<byte> buffer) => ReadBuffer(buffer);
#endif

        private int ReadBuffer(Span<byte> buffer)
        {
            EnsureNotClosed();

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
            ValidateBufferArguments(buffer, offset, count);
            EnsureNotClosed();
            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled<int>(cancellationToken) :
                Task.FromResult(ReadBuffer(new Span<byte>(buffer, offset, count)));
        }

#if !NETFRAMEWORK && !NETSTANDARD2_0
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureNotClosed();
            return cancellationToken.IsCancellationRequested ?
                ValueTask.FromCanceled<int>(cancellationToken) :
                new ValueTask<int>(ReadBuffer(buffer.Span));
        }
#endif

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);

        public override int EndRead(IAsyncResult asyncResult)
        {
            EnsureNotClosed();
            return TaskToApm.End<int>(asyncResult);
        }

#if !NETFRAMEWORK && !NETSTANDARD2_0
        public override void CopyTo(Stream destination, int bufferSize)
        {
            ValidateCopyToArguments(destination, bufferSize);
            EnsureNotClosed();
            if (_content.Length > _position)
            {
                destination.Write(_content.Span.Slice(_position));
                _position = _content.Length;
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ValidateCopyToArguments(destination, bufferSize);
            EnsureNotClosed();
            if (_content.Length > _position)
            {
                ReadOnlyMemory<byte> content = _content.Slice(_position);
                _position = _content.Length;
                return destination.WriteAsync(content, cancellationToken).AsTask();
            }
            else
            {
                return Task.CompletedTask;
            }
        }
#endif

        public override void Flush() { }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            _isOpen = false;
            _content = default;
            base.Dispose(disposing);
        }

#if NETFRAMEWORK || NETSTANDARD2_0
        private static void ValidateBufferArguments(byte[] buffer, int offset, int count)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if ((uint)count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.Argument_InvalidOffLen);
            }
        }
#endif
    }
}
