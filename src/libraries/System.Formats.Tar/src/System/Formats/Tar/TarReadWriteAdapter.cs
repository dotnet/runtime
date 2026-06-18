// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    // Static-abstract adapter that lets a single generic implementation serve both
    // synchronous and asynchronous Tar code paths. The sync entry point passes
    // SyncReadWriteAdapter and asserts the returned ValueTask is already completed;
    // the async entry point passes AsyncReadWriteAdapter. Mirrors the IReadWriteAdapter
    // pattern used by SslStream / SmtpClient (see src/libraries/Common/src/System/Net/ReadWriteAdapter.cs).
    internal interface IReadWriteAdapter
    {
        static abstract ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken);
        static abstract ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken);
        static abstract ValueTask ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken);
        static abstract ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
        static abstract ValueTask CopyToAsync(Stream source, Stream destination, CancellationToken cancellationToken);
        static abstract ValueTask AdvanceToEndAsync(SubReadStream stream, CancellationToken cancellationToken);
        static abstract ValueTask DisposeAsync(Stream stream);
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

        public static ValueTask AdvanceToEndAsync(SubReadStream stream, CancellationToken cancellationToken) =>
            stream.AdvanceToEndAsync(cancellationToken);

        public static ValueTask DisposeAsync(Stream stream) => stream.DisposeAsync();
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

        public static ValueTask AdvanceToEndAsync(SubReadStream stream, CancellationToken cancellationToken)
        {
            stream.AdvanceToEnd();
            return default;
        }

        public static ValueTask DisposeAsync(Stream stream)
        {
            stream.Dispose();
            return default;
        }
    }
}
