// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Microsoft.NET.HostModel.MachO;

internal static class StreamExtensions
{
    public static uint ReadUInt32BigEndian(this Stream stream)
    {
#if NET
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
#else
        byte[] buffer = new byte[sizeof(uint)];
#endif
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadUInt32BigEndian(buffer);
    }

    public static uint ReadUInt32BigEndian(this MemoryMappedViewAccessor accessor, long offset)
    {
        return accessor.ReadUInt32(offset).ConvertFromBigEndian();
    }

    public static void WriteUInt32BigEndian(this Stream stream, uint value)
    {
#if NET
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
#else
        byte[] buffer = new byte[sizeof(uint)];
#endif
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    public static void WriteUInt32BigEndian(this MemoryMappedViewAccessor accessor, long offset, uint value)
    {
        accessor.Write(offset, value.ConvertToBigEndian());
    }

    public static unsafe void Read<T>(this Stream stream, out T result) where T : unmanaged
    {
#if NET
        Span<byte> buffer = sizeof(T) < 256 ? stackalloc byte[sizeof(T)] : new byte[sizeof(T)];
#else
        byte[] buffer = new byte[sizeof(T)];
#endif
        stream.ReadExactly(buffer);
        result = MemoryMarshal.Read<T>(buffer);
    }

    public static unsafe void Write<T>(this Stream stream, ref T value) where T : unmanaged
    {
        byte[] buffer = new byte[sizeof(T)];
#pragma warning disable CS9191 // The 'ref' modifier for an argument corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
        MemoryMarshal.Write(buffer, ref value);
#pragma warning restore CS9191
        stream.Write(buffer, 0, buffer.Length);
    }

#if !NET
    /// <summary>
    /// Reads exactly the specified number of bytes from the stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="buffer">The buffer to read into.</param>
    /// <exception cref="EndOfStreamException">Thrown if the end of the stream is reached before reading the specified number of bytes.</exception>
    public static void ReadExactly(this Stream stream, byte[] buffer)
    {
        int totalBytesRead = 0;
        while (totalBytesRead < buffer.Length)
        {
            int bytesRead = stream.Read(buffer, totalBytesRead, buffer.Length - totalBytesRead);
            if (bytesRead == 0)
            {
                throw new EndOfStreamException("Reached end of stream before reading expected number of bytes.");
            }
            totalBytesRead += bytesRead;
        }
    }

    public static int Write(this Stream stream, byte[] buffer)
    {
        stream.Write(buffer, 0, buffer.Length);
        return buffer.Length;
    }
#endif
}
