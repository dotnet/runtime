// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

internal static class StreamExtensions
{
    private static byte[] _readBuffer = new byte[sizeof(uint)];
    public static uint ReadUInt32BigEndian(this Stream stream)
    {
        stream.ReadExactly(_readBuffer);
        return BinaryPrimitives.ReadUInt32BigEndian(_readBuffer);
    }

    public static uint ReadUInt32BigEndian(this MemoryMappedViewAccessor accessor, long offset)
    {
        int bytesRead = accessor.ReadArray(offset, _readBuffer, 0, sizeof(uint));
        if (bytesRead < sizeof(uint))
        {
            throw new ArgumentOutOfRangeException("Not enough bytes to read a UInt32.");
        }
        return BinaryPrimitives.ReadUInt32BigEndian(_readBuffer);
    }

    public static byte[] _writeBuffer = new byte[sizeof(uint)];
    public static void WriteUInt32BigEndian(this Stream stream, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(_writeBuffer, value);
        stream.Write(_writeBuffer);
    }
    public static void WriteUInt32BigEndian(this MemoryMappedViewAccessor accessor, long offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(_writeBuffer, value);
        accessor.WriteArray(offset, _writeBuffer, 0, _writeBuffer.Length);
    }

    public static unsafe void Read<T>(this Stream stream, out T result) where T : unmanaged
    {
        byte[] buffer = new byte[sizeof(T)];
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
