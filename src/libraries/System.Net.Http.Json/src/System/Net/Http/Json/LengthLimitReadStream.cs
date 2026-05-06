// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    internal sealed class LengthLimitReadStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly int _lengthLimit;
        private int _remainingLength;

        public LengthLimitReadStream(Stream innerStream, int lengthLimit)
        {
            _innerStream = innerStream;
            _lengthLimit = _remainingLength = lengthLimit;
        }

        private void CheckLengthLimit(int read)
        {
            _remainingLength -= read;
            if (_remainingLength < 0)
            {
                ThrowExceededBufferLimit(_lengthLimit);
            }
        }

        internal static void ThrowExceededBufferLimit(int limit)
        {
            throw new HttpRequestException(SR.Format(SR.net_http_content_buffersize_exceeded, limit));
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => false;

#if NET
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ValueTask<int> readTask = _innerStream.ReadAsync(buffer, cancellationToken);

            if (buffer.IsEmpty)
            {
                return readTask;
            }

            if (readTask.IsCompletedSuccessfully)
            {
                int read = readTask.Result;
                CheckLengthLimit(read);
                return new ValueTask<int>(read);
            }

            return Core(readTask);

            async ValueTask<int> Core(ValueTask<int> readTask)
            {
                int read = await readTask.ConfigureAwait(false);
                CheckLengthLimit(read);
                return read;
            }
        }
#else
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            CheckLengthLimit(read);
            return read;
        }
#endif

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _innerStream.Read(buffer, offset, count);
            CheckLengthLimit(read);
            return read;
        }

        public override void Flush() => _innerStream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
        public override long Length => _innerStream.Length;
        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
