// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace LibObjectFile.Utils
{
    /// <summary>
    /// Internal helper class for throwing exceptions.
    /// </summary>
    internal static class ThrowHelper
    {
        public static InvalidOperationException InvalidEnum(object v)
        {
            return new InvalidOperationException($"Invalid Enum {v.GetType()}.{v}");
        }
    }
    
    public static class StreamExtensions
    {
        /// <summary>
        /// Reads a null terminated UTF8 string from the stream.
        /// </summary>
        /// <returns><c>true</c> if the string was successfully read from the stream, false otherwise</returns>
        public static string ReadStringUTF8NullTerminated(this Stream stream)
        {
            if (!TryReadStringUTF8NullTerminated(stream, out var text))
            {
                throw new EndOfStreamException();
            }
            return text;
        }

        /// <summary>
        /// Reads a null terminated UTF8 string from the stream.
        /// </summary>
        /// <returns><c>true</c> if the string was successfully read from the stream, false otherwise</returns>
        public static bool TryReadStringUTF8NullTerminated(this Stream stream, out string text)
        {
            text = null;
            var buffer = ArrayPool<byte>.Shared.Rent((int)128);
            int textLength = 0;
            try
            {
                while (true)
                {
                    // TODO: not efficient to read byte by byte
                    int nextByte = stream.ReadByte();
                    if (nextByte < 0)
                    {
                        return false;
                    }

                    if (nextByte == 0)
                    {
                        break;
                    }

                    if (textLength > buffer.Length)
                    {
                        var newBuffer = ArrayPool<byte>.Shared.Rent((int)textLength * 2);
                        Array.Copy(buffer, 0, newBuffer, 0, buffer.Length);
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = newBuffer;
                    }

                    buffer[textLength++] = (byte)nextByte;
                }

                text = Encoding.UTF8.GetString(buffer, 0, textLength);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return true;
        }
    }
}