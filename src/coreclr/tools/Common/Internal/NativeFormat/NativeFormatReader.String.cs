// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ---------------------------------------------------------------------------
// Native Format Reader
//
// UTF8 string reading methods
// ---------------------------------------------------------------------------

using System.Text;

namespace Internal.NativeFormat
{
    internal partial struct NativeParser
    {
        public string GetString()
        {
            string value;
            _offset = _reader.DecodeString(_offset, out value);
            return value;
        }

        public void SkipString()
        {
            _offset = _reader.SkipString(_offset);
        }
    }

    internal partial class NativeReader
    {
        public string ReadString(uint offset)
        {
            string value;
            DecodeString(offset, out value);
            return value;
        }

        public unsafe uint DecodeString(uint offset, out string value)
        {
            uint numBytes;
            offset = DecodeUnsigned(offset, out numBytes);

            if (numBytes == 0)
            {
                value = string.Empty;
                return offset;
            }

            uint endOffset = offset + numBytes;
            if (endOffset < numBytes || endOffset > _size)
                ThrowBadImageFormatException();

#if NETFX_45
            byte[] bytes = new byte[numBytes];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = *(_base + offset + i);

            value = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
#else
            value = Encoding.UTF8.GetString(_base + offset, (int)numBytes);
#endif

            return endOffset;
        }

        // Decode a string, but just skip it instead of returning it
        public uint SkipString(uint offset)
        {
            uint numBytes;
            offset = DecodeUnsigned(offset, out numBytes);

            if (numBytes == 0)
            {
                return offset;
            }

            uint endOffset = offset + numBytes;
            if (endOffset < numBytes || endOffset > _size)
                ThrowBadImageFormatException();

            return endOffset;
        }

        public unsafe bool StringEquals(uint offset, string value)
        {
            uint originalOffset = offset;

            uint numBytes;
            offset = DecodeUnsigned(offset, out numBytes);

            uint endOffset = offset + numBytes;
            if (endOffset < numBytes || offset > _size)
                ThrowBadImageFormatException();

            if (numBytes < value.Length)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                int ch = *(_base + offset + i);
                if (ch > 0x7F)
                    return ReadString(originalOffset) == value;

                // We are assuming here that valid UTF8 encoded byte > 0x7F cannot map to a character with code point <= 0x7F
                if (ch != value[i])
                    return false;
            }

            return numBytes == value.Length; // All char ANSI, all matching
        }
    }
}
