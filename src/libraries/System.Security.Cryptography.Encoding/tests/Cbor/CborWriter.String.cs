// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers.Binary;
using System.Text;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter
    {
        private static readonly System.Text.Encoding s_utf8Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        // Implements major type 2 encoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public void WriteByteString(ReadOnlySpan<byte> value)
        {
            WriteUnsignedInteger(CborMajorType.ByteString, (ulong)value.Length);
            EnsureWriteCapacity(value.Length);
            value.CopyTo(_buffer.AsSpan(_offset));
            _offset += value.Length;
            AdvanceDataItemCounters();
        }

        // Implements major type 3 encoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public void WriteTextString(ReadOnlySpan<char> value)
        {
            int length;
            try
            {
                length = s_utf8Encoding.GetByteCount(value);
            }
            catch (EncoderFallbackException e)
            {
                throw new ArgumentException("Provided text string is not valid UTF8.", e);
            }

            WriteUnsignedInteger(CborMajorType.TextString, (ulong)length);
            EnsureWriteCapacity(length);
            s_utf8Encoding.GetBytes(value, _buffer.AsSpan(_offset));
            _offset += length;
            AdvanceDataItemCounters();
        }

        public void WriteStartByteStringIndefiniteLength()
        {
            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.ByteString, CborAdditionalInfo.IndefiniteLength));
            AdvanceDataItemCounters();
            PushDataItem(CborMajorType.ByteString, expectedNestedItems: null);
        }

        public void WriteEndByteStringIndefiniteLength()
        {
            PopDataItem(CborMajorType.ByteString);
            // append break byte
            EnsureWriteCapacity(1);
            _buffer[_offset++] = CborInitialByte.IndefiniteLengthBreakByte;
        }

        public void WriteStartTextStringIndefiniteLength()
        {
            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.TextString, CborAdditionalInfo.IndefiniteLength));
            AdvanceDataItemCounters();
            PushDataItem(CborMajorType.TextString, expectedNestedItems: null);
        }

        public void WriteEndTextStringIndefiniteLength()
        {
            PopDataItem(CborMajorType.TextString);
            // append break byte
            EnsureWriteCapacity(1);
            _buffer[_offset++] = CborInitialByte.IndefiniteLengthBreakByte;
        }
    }
}
