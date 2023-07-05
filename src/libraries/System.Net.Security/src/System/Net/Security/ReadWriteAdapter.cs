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
        static abstract ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken);
        static abstract ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
        static abstract Task FlushAsync(Stream stream, CancellationToken cancellationToken);
        static abstract Task WaitAsync(TaskCompletionSource<bool> waiter);
    }

    internal readonly struct AsyncReadWriteAdapter : IReadWriteAdapter
    {
        public static ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken) =>
            stream.ReadAsync(buffer, cancellationToken);

        public static ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken) =>
            stream.ReadAtLeastAsync(buffer, minimumBytes, throwOnEndOfStream, cancellationToken);

        public static ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
            stream.WriteAsync(buffer, cancellationToken);

        public static Task FlushAsync(Stream stream, CancellationToken cancellationToken) => stream.FlushAsync(cancellationToken);

        public static Task WaitAsync(TaskCompletionSource<bool> waiter) => waiter.Task;
    }

    internal readonly struct SyncReadWriteAdapter : IReadWriteAdapter
    {
        public static ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken) =>
            new ValueTask<int>(stream.Read(buffer.Span));

        public static ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken) =>
            new ValueTask<int>(stream.ReadAtLeast(buffer.Span, minimumBytes, throwOnEndOfStream));

        public static ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            stream.Write(buffer.Span);
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
