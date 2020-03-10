// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborReader
    {
        private static readonly System.Text.Encoding s_utf8Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        // Implements major type 2 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public byte[] ReadByteString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);
            int length = checked((int)ReadUnsignedInteger(header, out int additionalBytes));
            EnsureBuffer(1 + additionalBytes + length);
            byte[] result = new byte[length];
            _buffer.Slice(1 + additionalBytes, length).CopyTo(result);
            AdvanceBuffer(1 + additionalBytes + length);
            _remainingDataItems--;
            return result;
        }

        public bool TryReadByteString(Span<byte> destination, out int bytesWritten)
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);
            int length = checked((int)ReadUnsignedInteger(header, out int additionalBytes));
            EnsureBuffer(1 + additionalBytes + length);

            if (length > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            _buffer.Span.Slice(1 + additionalBytes, length).CopyTo(destination);
            AdvanceBuffer(1 + additionalBytes + length);
            _remainingDataItems--;

            bytesWritten = length;
            return true;
        }

        // Implements major type 3 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public string ReadTextString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.TextString);
            int length = checked((int)ReadUnsignedInteger(header, out int additionalBytes));
            EnsureBuffer(1 + additionalBytes + length);
            ReadOnlySpan<byte> encodedString = _buffer.Span.Slice(1 + additionalBytes, length);
            string result = s_utf8Encoding.GetString(encodedString);
            AdvanceBuffer(1 + additionalBytes + length);
            _remainingDataItems--;
            return result;
        }

        public bool TryReadTextString(Span<char> destination, out int charsWritten)
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.TextString);
            int byteLength = checked((int)ReadUnsignedInteger(header, out int additionalBytes));
            EnsureBuffer(1 + additionalBytes + byteLength);

            ReadOnlySpan<byte> encodedSlice = _buffer.Span.Slice(1 + additionalBytes, byteLength);
            int charLength = s_utf8Encoding.GetCharCount(encodedSlice);
            if (charLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            s_utf8Encoding.GetChars(encodedSlice, destination);
            AdvanceBuffer(1 + additionalBytes + byteLength);
            _remainingDataItems--;
            charsWritten = charLength;
            return true;
        }
    }
}
