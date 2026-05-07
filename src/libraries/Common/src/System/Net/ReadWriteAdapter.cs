// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal interface IReadWriteAdapter
    {
        static abstract ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken);
        static abstract ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken);
        static abstract ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
        static abstract Task FlushAsync(Stream stream, CancellationToken cancellationToken);
        static abstract Task WaitAsync(TaskCompletionSource<bool> waiter);
        static abstract Task WaitAsync(Task task);
        static abstract ValueTask<T> WaitAsync<T>(ValueTask<T> task);
    }

    internal readonly struct AsyncReadWriteAdapter : IReadWriteAdapter
    {
        public static async ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken) =>
            await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

        public static async ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken) =>
            await stream.ReadAtLeastAsync(buffer, minimumBytes, throwOnEndOfStream, cancellationToken).ConfigureAwait(false);

        public static async ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        public static async Task FlushAsync(Stream stream, CancellationToken cancellationToken) => await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

        public static Task WaitAsync(TaskCompletionSource<bool> waiter) => waiter.Task;
        public static Task WaitAsync(Task task) => task;
        public static ValueTask<T> WaitAsync<T>(ValueTask<T> task) => task;
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

        public static Task WaitAsync(Task task)
        {
            task.GetAwaiter().GetResult();
            return Task.CompletedTask;
        }

        public static async ValueTask<T> WaitAsync<T>(ValueTask<T> task)
        {
            return await task.ConfigureAwait(false);
        }
    }
}
