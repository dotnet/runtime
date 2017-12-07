// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security
{
    // This contains adapters to allow a single code path for sync/async logic
    internal partial class SslStreamInternal
    {
        private interface ISslWriteAdapter
        {
            Task LockAsync();
            Task WriteAsync(byte[] buffer, int offset, int count);
        }

        private interface ISslReadAdapter
        {
            ValueTask<int> ReadAsync(byte[] buffer, int offset, int count);
            ValueTask<int> LockAsync(Memory<byte> buffer);
        }

        private readonly struct SslReadAsync : ISslReadAdapter
        {
            private readonly SslState _sslState;
            private readonly CancellationToken _cancellationToken;

            public SslReadAsync(SslState sslState, CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                _sslState = sslState;
            }

            public ValueTask<int> ReadAsync(byte[] buffer, int offset, int count) => _sslState.InnerStream.ReadAsync(new Memory<byte>(buffer, offset, count), _cancellationToken);

            public ValueTask<int> LockAsync(Memory<byte> buffer) => _sslState.CheckEnqueueReadAsync(buffer);
        }

        private readonly struct SslReadSync : ISslReadAdapter
        {
            private readonly SslState _sslState;

            public SslReadSync(SslState sslState) => _sslState = sslState;

            public ValueTask<int> ReadAsync(byte[] buffer, int offset, int count) => new ValueTask<int>(_sslState.InnerStream.Read(buffer, offset, count));

            public ValueTask<int> LockAsync(Memory<byte> buffer) => new ValueTask<int>(_sslState.CheckEnqueueRead(buffer));
        }

        private readonly struct SslWriteAsync : ISslWriteAdapter
        {
            private readonly SslState _sslState;
            private readonly CancellationToken _cancellationToken;

            public SslWriteAsync(SslState sslState, CancellationToken cancellationToken)
            {
                _sslState = sslState;
                _cancellationToken = cancellationToken;
            }

            public Task LockAsync() => _sslState.CheckEnqueueWriteAsync();

            public Task WriteAsync(byte[] buffer, int offset, int count) => _sslState.InnerStream.WriteAsync(buffer, offset, count, _cancellationToken);
        }

        private readonly struct SslWriteSync : ISslWriteAdapter
        {
            private readonly SslState _sslState;

            public SslWriteSync(SslState sslState) => _sslState = sslState;

            public Task LockAsync()
            {
                _sslState.CheckEnqueueWrite();
                return Task.CompletedTask;
            }

            public Task WriteAsync(byte[] buffer, int offset, int count)
            {
                _sslState.InnerStream.Write(buffer, offset, count);
                return Task.CompletedTask;
            }
        }
    }
}
