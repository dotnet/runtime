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
        private interface ISslIOAdapter
        {
            ValueTask<int> ReadAsync(Memory<byte> buffer);
            ValueTask<int> ReadLockAsync(Memory<byte> buffer);
            Task WriteLockAsync();
            ValueTask WriteAsync(byte[] buffer, int offset, int count);
            CancellationToken CancellationToken { get; }
        }

        private readonly struct AsyncSslIOAdapter : ISslIOAdapter
        {
            private readonly SslStream _sslStream;
            private readonly CancellationToken _cancellationToken;

            public AsyncSslIOAdapter(SslStream sslStream, CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                _sslStream = sslStream;
            }

            public ValueTask<int> ReadAsync(Memory<byte> buffer) => _sslStream.InnerStream.ReadAsync(buffer, _cancellationToken);

            public ValueTask<int> ReadLockAsync(Memory<byte> buffer) => _sslStream.CheckEnqueueReadAsync(buffer);

            public Task WriteLockAsync() => _sslStream.CheckEnqueueWriteAsync();

            public ValueTask WriteAsync(byte[] buffer, int offset, int count) => _sslStream.InnerStream.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), _cancellationToken);

            public CancellationToken CancellationToken => _cancellationToken;
        }

        private readonly struct SyncSslIOAdapter : ISslIOAdapter
        {
            private readonly SslStream _sslStream;

            public SyncSslIOAdapter(SslStream sslStream) => _sslStream = sslStream;

            public ValueTask<int> ReadAsync(Memory<byte> buffer) => new ValueTask<int>(_sslStream.InnerStream.Read(buffer.Span));

            public ValueTask<int> ReadLockAsync(Memory<byte> buffer) => new ValueTask<int>(_sslStream.CheckEnqueueRead(buffer));

            public ValueTask WriteAsync(byte[] buffer, int offset, int count)
            {
                _sslStream.InnerStream.Write(buffer, offset, count);
                return default;
            }

            public Task WriteLockAsync()
            {
                _sslStream.CheckEnqueueWrite();
                return Task.CompletedTask;
            }

            public CancellationToken CancellationToken => default;
        }
    }
}
