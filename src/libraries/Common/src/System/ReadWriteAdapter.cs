// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    // Static-abstract adapter that lets a single generic implementation serve both
    // synchronous and asynchronous code paths. The sync entry point passes
    // SyncReadWriteAdapter and asserts the returned ValueTask is already completed;
    // the async entry point passes AsyncReadWriteAdapter.
    //
    // Originally used by SslStream and SmtpClient for networking I/O deduplication.
    // Extended for System.Formats.Tar (CopyToAsync, ReadExactlyAsync, DisposeAsync)
    // to unify TarReader/TarWriter/GnuSparseStream sync/async pairs.
    internal interface IReadWriteAdapter
    {
        static abstract ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken);
        static abstract ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken);
        static abstract ValueTask ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken);
        static abstract ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
        static abstract ValueTask CopyToAsync(Stream source, Stream destination, CancellationToken cancellationToken);
        static abstract ValueTask DisposeAsync(Stream stream);
        static abstract Task FlushAsync(Stream stream, CancellationToken cancellationToken);
        static abstract Task WaitAsync(TaskCompletionSource<bool> waiter);
        static abstract Task WaitAsync(Task task);
        static abstract ValueTask<T> WaitAsync<T>(ValueTask<T> task);
    }

    internal readonly struct AsyncReadWriteAdapter : IReadWriteAdapter
    {
        public static ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken) =>
            stream.ReadAsync(buffer, cancellationToken);

        public static ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken) =>
            stream.ReadAtLeastAsync(buffer, minimumBytes, throwOnEndOfStream, cancellationToken);

        public static ValueTask ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken) =>
            stream.ReadExactlyAsync(buffer, cancellationToken);

        public static ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
            stream.WriteAsync(buffer, cancellationToken);

        public static ValueTask CopyToAsync(Stream source, Stream destination, CancellationToken cancellationToken) =>
            new ValueTask(source.CopyToAsync(destination, cancellationToken));

        public static ValueTask DisposeAsync(Stream stream) => stream.DisposeAsync();

        public static Task FlushAsync(Stream stream, CancellationToken cancellationToken) => stream.FlushAsync(cancellationToken);

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

        public static ValueTask ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            stream.ReadExactly(buffer.Span);
            return default;
        }

        public static ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            stream.Write(buffer.Span);
            return default;
        }

        public static ValueTask CopyToAsync(Stream source, Stream destination, CancellationToken cancellationToken)
        {
            source.CopyTo(destination);
            return default;
        }

        public static ValueTask DisposeAsync(Stream stream)
        {
            stream.Dispose();
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

        public static ValueTask<T> WaitAsync<T>(ValueTask<T> task)
        {
            return ValueTask.FromResult(task.AsTask().GetAwaiter().GetResult());
        }
    }
}
