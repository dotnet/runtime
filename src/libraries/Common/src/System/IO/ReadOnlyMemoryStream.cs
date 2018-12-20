// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>Provides a <see cref="Stream"/> for the contents of a <see cref="ReadOnlyMemory{Byte}"/>.</summary>
    internal sealed class ReadOnlyMemoryStream : Stream
    {
        private readonly ReadOnlyMemory<byte> _content;
        private int _position;

        public ReadOnlyMemoryStream(ReadOnlyMemory<byte> content)
        {
            _content = content;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override long Length => _content.Length;

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0 || value > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _position = (int)value;
            }
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
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
            ReadOnlySpan<byte> s = _content.Span;
            return _position < s.Length ? s[_position++] : -1;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateReadArrayArguments(buffer, offset, count);
            return Read(new Span<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> buffer)
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
            ValidateReadArrayArguments(buffer, offset, count);
            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled<int>(cancellationToken) :
                Task.FromResult(Read(new Span<byte>(buffer, offset, count)));
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken)) =>
            cancellationToken.IsCancellationRequested ?
                new ValueTask<int>(Task.FromCanceled<int>(cancellationToken)) :
                new ValueTask<int>(Read(buffer.Span));

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override void CopyTo(Stream destination, int bufferSize)
        {
            StreamHelpers.ValidateCopyToArgs(this, destination, bufferSize);
            if (_content.Length > _position)
            {
                destination.Write(_content.Span.Slice(_position));
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            StreamHelpers.ValidateCopyToArgs(this, destination, bufferSize);
            return _content.Length > _position ?
                destination.WriteAsync(_content.Slice(_position), cancellationToken).AsTask() :
                Task.CompletedTask;
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
    }
}
