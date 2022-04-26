// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security
{
    internal interface IReadWriteAdapter
    {
        static abstract ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken);
        static abstract ValueTask WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        static abstract Task FlushAsync(Stream stream, CancellationToken cancellationToken);
        static abstract Task WaitAsync(TaskCompletionSource<bool> waiter);
    }

    internal readonly struct AsyncReadWriteAdapter : IReadWriteAdapter
    {
        public static ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken) =>
            stream.ReadAsync(buffer, cancellationToken);

        public static ValueTask WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);

        public static Task FlushAsync(Stream stream, CancellationToken cancellationToken) => stream.FlushAsync(cancellationToken);

        public static Task WaitAsync(TaskCompletionSource<bool> waiter) => waiter.Task;
    }

    internal readonly struct SyncReadWriteAdapter : IReadWriteAdapter
    {
        public static ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken) =>
            new ValueTask<int>(stream.Read(buffer.Span));

        public static ValueTask WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            stream.Write(buffer, offset, count);
            return default;
        }

        public static Task FlushAsync(Stream stream, CancellationToken cancellationToken)
        {
            stream.Flush();
            return Task.CompletedTask;
        }

        public static Task WaitAsync(TaskCompletionSource<bool> waiter)
        {
            waiter.Task.GetAwaiter().GetResult();
            return Task.CompletedTask;
        }
    }
}
