// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

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

    public static void Write(this Stream stream, ReadOnlySpan<byte> source)
    {
        byte[] array = source.ToArray();
        stream.Write(array, 0, array.Length);
    }
}
