// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Resources
{
    internal static class BinaryStreamExtensions
    {
        public static bool TryReadInt32(this Stream stream, out int result)
        {
            result = 0;
            var b0 = stream.ReadByte();
            if (b0 < 0) return false;
            var b1 = stream.ReadByte();
            if (b1 < 0) return false;
            var b2 = stream.ReadByte();
            if (b2 < 0) return false;
            var b3 = stream.ReadByte();
            if (b3 < 0) return false;

            result = b3;
            result <<= 8;
            result |= b2;
            result <<= 8;
            result |= b1;
            result <<= 8;
            result |= b0;

            return true;
        }

        //blatantly copied over from BinaryReader
        public static bool TryReadInt327BitEncoded(this Stream stream, out int result)
        {
            // Read out an int 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            result = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per int, shift += 7
                    throw new FormatException(SR.Format_Bad7BitInt32);

                // ReadByte handles end of stream cases for us.
                int readByte = stream.ReadByte();
                if (readByte < 0)
                {
                    result = default(int);
                    return false;
                }
                b = (byte)readByte;
                result |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            return true;
        }

        // reads length (7-bit encoded int) + UTF8 buffer
        public static string ReadString(this Stream stream, bool utf16 = false)
        {
            var position = stream.Position;
            int stringLength;
            if (!stream.TryReadInt327BitEncoded(out stringLength))
            {
                throw new BadImageFormatException(SR.Resources_StreamNotValid);
            }

            if(stringLength == 0) { return string.Empty; }

            byte[] buffer = new byte[stringLength];
            int totalRead = 0;

            do
            {
                int justRead = stream.Read(buffer, totalRead, stringLength - totalRead);
                if(justRead == 0)
                {
                    throw new EndOfStreamException(SR.BadImageFormat_ResourceNameCorrupted_NameIndex + position);
                }
                totalRead += justRead;
            }
            while (totalRead != stringLength);

            if (utf16) {
                return Encoding.Unicode.GetString(buffer, 0, buffer.Length);
            }
            else {
                return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            }
        }

        internal static bool StartsWith(this Stream stream, byte[] value, bool advance = false)
        {
            long originalPosition = stream.Position;
            foreach(var b in value)
            {
                int read = stream.ReadByte();
                if(b != read) {
                    if (!advance) {
                        stream.Position = originalPosition;
                    }
                    return false;
                }
            }
            if (!advance) {
                stream.Position = originalPosition;
            }
            return true;
        }
    }
}
