// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal ref partial struct CborReader
    {
        private static readonly System.Text.Encoding s_utf8Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        // Implements major type 2 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public byte[] ReadByteString()
        {
            ReadInitialByte(out CborDataItem header, expectedType: CborMajorType.ByteString);
            int length = checked((int)ReadUnsignedInteger(in header, out int additionalBytes));
            byte[] result = new byte[length];
            _buffer.Slice(1 + additionalBytes, length).CopyTo(result);
            AdvanceBuffer(1 + additionalBytes + length);
            return result;
        }

        // Implements major type 3 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public string ReadUtf8String()
        {
            ReadInitialByte(out CborDataItem header, expectedType: CborMajorType.Utf8String);
            int length = checked((int)ReadUnsignedInteger(in header, out int additionalBytes));
            ReadOnlySpan<byte> encodedString = _buffer.Slice(1 + additionalBytes, length);
            string result = s_utf8Encoding.GetString(encodedString);
            AdvanceBuffer(1 + additionalBytes + length);
            return result;
        }
    }
}
