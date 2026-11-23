// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml;

internal interface IXmlStreamIO
{
    static abstract ValueTask<int> Read(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken);
    static abstract ValueTask<int> ReadAtLeast(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken);
    static abstract ValueTask Write(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    static abstract ValueTask Flush(Stream stream, CancellationToken cancellationToken);
}

internal readonly struct SyncXmlStreamIO : IXmlStreamIO
{
    public static ValueTask<int> Read(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken) =>
        new ValueTask<int>(stream.Read(buffer.Span));

    public static ValueTask<int> ReadAtLeast(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken) =>
        new ValueTask<int>(stream.ReadAtLeast(buffer.Span, buffer.Length, throwOnEndOfStream: false));

    public static ValueTask Write(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        stream.Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    public static ValueTask Flush(Stream stream, CancellationToken cancellationToken)
    {
        stream.Flush();
        return ValueTask.CompletedTask;
    }
}

internal readonly struct AsyncXmlStreamIO : IXmlStreamIO
{
    public static ValueTask<int> Read(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken) =>
        stream.ReadAsync(buffer, cancellationToken);

    public static ValueTask<int> ReadAtLeast(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken) =>
        stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);

    public static ValueTask Write(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
        stream.WriteAsync(buffer, cancellationToken);

    public static ValueTask Flush(Stream stream, CancellationToken cancellationToken) =>
        new ValueTask(stream.FlushAsync(cancellationToken));
}
