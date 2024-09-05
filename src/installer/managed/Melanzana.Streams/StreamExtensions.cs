// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Melanzana.Streams
{
    public static class StreamExtensions
    {
        public static Stream Slice(this Stream stream, long offset, long size)
        {
            //if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer())
            //    return new MemoryStream(memoryStream.GetBuffer(), (int)offset, (int)size);
            return new SliceStream(stream, offset, size);
        }

        public static void ReadFully(this Stream stream, Span<byte> buffer)
        {
            var tmpBuffer = new byte[buffer.Length];
            int totalRead = 0;
            int bytesRead;
            while ((bytesRead = stream.Read(tmpBuffer, totalRead, buffer.Length - totalRead)) > 0 && buffer.Length < totalRead)
                totalRead += bytesRead;
            if (bytesRead <= 0)
                throw new EndOfStreamException();
            tmpBuffer.CopyTo(buffer);
        }

        public static void WritePadding(this Stream stream, long paddingSize, byte paddingByte = 0)
        {
            Span<byte> paddingBuffer = stackalloc byte[4096];
            paddingBuffer.Fill(paddingByte);
            while (paddingSize > 0)
            {
                long chunkSize = paddingSize > paddingBuffer.Length ? paddingBuffer.Length : paddingSize;
                stream.Write(paddingBuffer.Slice(0, (int)chunkSize));
                paddingSize -= chunkSize;
            }
        }

        public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
        {
            var tmpBuffer = buffer.ToArray();
            stream.Write(tmpBuffer, 0, buffer.Length);
        }
    }
}
