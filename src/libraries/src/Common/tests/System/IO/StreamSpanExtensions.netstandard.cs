// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Extensions to enable the tests to use the span-based Read/Write methods that only exist in netcoreapp.</summary>
internal static class StreamSpanExtensions
{
    // These implementations are inefficient and are just for testing purposes.

    public static int Read(this Stream stream, Span<byte> destination)
    {
        byte[] array = new byte[destination.Length];
        int bytesRead = stream.Read(array, 0, array.Length);
        new Span<byte>(array, 0, bytesRead).CopyTo(destination);
        return bytesRead;
    }

    public static void Write(this Stream stream, ReadOnlySpan<byte> source) =>
        stream.Write(source.ToArray(), 0, source.Length);

    public static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> destination, CancellationToken cancellationToken = default(CancellationToken))
    {
        byte[] array = new byte[destination.Length];
        return new ValueTask<int>(stream.ReadAsync(array, 0, array.Length, cancellationToken).ContinueWith(t =>
        {
            int bytesRead = t.GetAwaiter().GetResult();
            new Span<byte>(array, 0, bytesRead).CopyTo(destination.Span);
            return bytesRead;
        }));
    }

    public static Task WriteAsync(this Stream stream, ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default(CancellationToken)) =>
        stream.WriteAsync(source.ToArray(), 0, source.Length, cancellationToken);
}
