// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    public sealed class DribbleStream : Stream
    {
        private readonly Stream _wrapped;

        public DribbleStream(Stream wrapped)
        {
            _wrapped = wrapped;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            for (int i = 0; i < count; i++)
            {
                await _wrapped.WriteAsync(buffer, offset + i, 1, cancellationToken);
                await _wrapped.FlushAsync(cancellationToken);
                await Task.Yield(); // introduce short delays, enough to send packets individually but not so long as to extend test duration significantly
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                _wrapped.Write(buffer, offset + i, 1);
                _wrapped.Flush();
            }
        }

        public override bool CanRead => _wrapped.CanRead;
        public override bool CanSeek => _wrapped.CanSeek;
        public override bool CanWrite => _wrapped.CanWrite;
        public override long Length => _wrapped.Length;
        public override long Position { get => _wrapped.Position; set => _wrapped.Position = value; }
        public override void Flush() => _wrapped.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _wrapped.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => _wrapped.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _wrapped.ReadAsync(buffer, offset, count, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => _wrapped.Seek(offset, origin);
        public override void SetLength(long value) => _wrapped.SetLength(value);
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => _wrapped.CopyToAsync(destination, bufferSize, cancellationToken);
        public override void Close() => _wrapped.Close();
        protected override void Dispose(bool disposing) => _wrapped.Dispose();
    }
}
