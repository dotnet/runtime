// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security
{
    internal interface IReadWriteAdapter
    {
        ValueTask<int> ReadAsync(Memory<byte> buffer);

        ValueTask WriteAsync(byte[] buffer, int offset, int count);

        Task WaitAsync(TaskCompletionSource<bool> waiter);

        CancellationToken CancellationToken { get; }

        public async ValueTask<int> ReadAllAsync(Memory<byte> buffer)
        {
            int length = buffer.Length;

            do
            {
                int bytes = await ReadAsync(buffer).ConfigureAwait(false);
                if (bytes == 0)
                {
                    if (!buffer.IsEmpty)
                    {
                        throw new IOException(SR.net_io_eof);
                    }
                    break;
                }

                buffer = buffer.Slice(bytes);
            }
            while (!buffer.IsEmpty);

            return length;
        }
    }

    internal readonly struct AsyncReadWriteAdapter : IReadWriteAdapter
    {
        private readonly Stream _stream;

        public AsyncReadWriteAdapter(Stream stream, CancellationToken cancellationToken)
        {
            _stream = stream;
            CancellationToken = cancellationToken;
        }

        public ValueTask<int> ReadAsync(Memory<byte> buffer) =>
            _stream.ReadAsync(buffer, CancellationToken);

        public ValueTask WriteAsync(byte[] buffer, int offset, int count) =>
            _stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken);

        public Task WaitAsync(TaskCompletionSource<bool> waiter) => waiter.Task;

        public CancellationToken CancellationToken { get; }
    }

    internal readonly struct SyncReadWriteAdapter : IReadWriteAdapter
    {
        private readonly Stream _stream;

        public SyncReadWriteAdapter(Stream stream) => _stream = stream;

        public ValueTask<int> ReadAsync(Memory<byte> buffer) =>
            new ValueTask<int>(_stream.Read(buffer.Span));

        public ValueTask WriteAsync(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
            return default;
        }

        public Task WaitAsync(TaskCompletionSource<bool> waiter)
        {
            waiter.Task.GetAwaiter().GetResult();
            return Task.CompletedTask;
        }

        public CancellationToken CancellationToken => default;
    }
}
