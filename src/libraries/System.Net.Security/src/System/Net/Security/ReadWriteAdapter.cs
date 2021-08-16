// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        Task FlushAsync();

        CancellationToken CancellationToken { get; }
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

        public Task FlushAsync() => _stream.FlushAsync(CancellationToken);

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

        public Task FlushAsync()
        {
            _stream.Flush();
            return Task.CompletedTask;
        }

        public CancellationToken CancellationToken => default;
    }
}
