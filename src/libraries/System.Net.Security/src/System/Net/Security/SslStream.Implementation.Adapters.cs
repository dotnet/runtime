// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security
{
    // This contains adapters to allow a single code path for sync/async logic
    public partial class SslStream
    {
        private interface ISslWriteAdapter
        {
            Task LockAsync();
            ValueTask WriteAsync(byte[] buffer, int offset, int count);
        }

        private interface ISslReadAdapter
        {
            ValueTask<int> ReadAsync(byte[] buffer, int offset, int count);
            ValueTask<int> LockAsync(Memory<byte> buffer);
        }

        private readonly struct SslReadAsync : ISslReadAdapter
        {
            private readonly SslStream _sslStream;
            private readonly CancellationToken _cancellationToken;

            public SslReadAsync(SslStream sslStream, CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                _sslStream = sslStream;
            }

            public ValueTask<int> ReadAsync(byte[] buffer, int offset, int count) => _sslStream.InnerStream.ReadAsync(new Memory<byte>(buffer, offset, count), _cancellationToken);

            public ValueTask<int> LockAsync(Memory<byte> buffer) => _sslStream.CheckEnqueueReadAsync(buffer);
        }

        private readonly struct SslReadSync : ISslReadAdapter
        {
            private readonly SslStream _sslStream;

            public SslReadSync(SslStream sslStream) => _sslStream = sslStream;

            public ValueTask<int> ReadAsync(byte[] buffer, int offset, int count) => new ValueTask<int>(_sslStream.InnerStream.Read(buffer, offset, count));

            public ValueTask<int> LockAsync(Memory<byte> buffer) => new ValueTask<int>(_sslStream.CheckEnqueueRead(buffer));
        }

        private readonly struct SslWriteAsync : ISslWriteAdapter
        {
            private readonly SslStream _sslStream;
            private readonly CancellationToken _cancellationToken;

            public SslWriteAsync(SslStream sslStream, CancellationToken cancellationToken)
            {
                _sslStream = sslStream;
                _cancellationToken = cancellationToken;
            }

            public Task LockAsync() => _sslStream.CheckEnqueueWriteAsync();

            public ValueTask WriteAsync(byte[] buffer, int offset, int count) => _sslStream.InnerStream.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), _cancellationToken);
        }

        private readonly struct SslWriteSync : ISslWriteAdapter
        {
            private readonly SslStream _sslStream;

            public SslWriteSync(SslStream sslStream) => _sslStream = sslStream;

            public Task LockAsync()
            {
                _sslStream.CheckEnqueueWrite();
                return Task.CompletedTask;
            }

            public ValueTask WriteAsync(byte[] buffer, int offset, int count)
            {
                _sslStream.InnerStream.Write(buffer, offset, count);
                return default;
            }
        }
    }
}
