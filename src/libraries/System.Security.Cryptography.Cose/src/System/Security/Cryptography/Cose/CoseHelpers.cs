// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Security.Cryptography.Cose
{
    internal static class CoseHelpers
    {
        private static readonly UTF8Encoding s_utf8EncodingStrict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        internal static int GetEncodedSize(ReadOnlySpan<byte> value)
        {
            return GetEncodedSize(value.Length) + value.Length;
        }

        internal static int GetEncodedSize(string value)
        {
            int strEncodedLength = s_utf8EncodingStrict.GetByteCount(value);
            return GetEncodedSize(strEncodedLength) + strEncodedLength;
        }

        internal static int GetEncodedSize(long value)
        {
            if (value < 0)
            {
                ulong unsignedRepresentation = (value == long.MinValue) ? (ulong)long.MaxValue : (ulong)(-value) - 1;
                return GetEncodedSize(unsignedRepresentation);
            }
            else
            {
                return GetEncodedSize((ulong)value);
            }
        }

        internal static int GetEncodedSize(ulong value)
        {
            if (value < 24)
            {
                return 1;
            }
            else if (value <= byte.MaxValue)
            {
                return 1 + sizeof(byte);
            }
            else if (value <= ushort.MaxValue)
            {
                return 1 + sizeof(ushort);
            }
            else if (value <= uint.MaxValue)
            {
                return 1 + sizeof(uint);
            }
            else
            {
                return 1 + sizeof(ulong);
            }
        }
    }
}
