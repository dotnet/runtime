// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics;

/// <summary>Provides downlevel polyfills for instance methods on <see cref="BigInteger"/>.</summary>
internal static class BigIntegerPolyfills
{
    public static BigInteger Create(ReadOnlySpan<byte> value, bool isUnsigned, bool isBigEndian)
    {
        if (value.IsEmpty)
            return BigInteger.Zero;

        byte[] bytes = value.ToArray();

        if (isBigEndian)
            Array.Reverse(bytes);

        // BigInteger(byte[]) expects little-endian signed (two's complement).
        // If the caller says unsigned and the high bit is set, append a 0x00
        // so BigInteger doesn't interpret it as negative.
        if (isUnsigned && (bytes[bytes.Length - 1] & 0x80) != 0)
        {
            byte[] extended = new byte[bytes.Length + 1];
            Array.Copy(bytes, extended, bytes.Length);
            bytes = extended;
        }

        return new BigInteger(bytes);
    }

    extension(BigInteger self)
    {
        public byte[] ToByteArray(bool isUnsigned, bool isBigEndian)
        {
            if (isUnsigned && self.Sign < 0)
                throw new OverflowException();

            byte[] littleEndianBytes = self.ToByteArray();

            if (!isUnsigned && !isBigEndian)
                return littleEndianBytes;

            int length = littleEndianBytes.Length;

            // For unsigned, trim a single most-significant 0x00 sign-extension byte
            // from the end of the little-endian array.
            if (isUnsigned && length > 1 && littleEndianBytes[length - 1] == 0x00)
                length--;

            if (isBigEndian)
            {
                byte[] result = new byte[length];

                for (int i = 0; i < length; i++)
                {
                    result[i] = littleEndianBytes[length - 1 - i];
                }

                return result;
            }

            if (length == littleEndianBytes.Length)
                return littleEndianBytes;

            byte[] trimmed = new byte[length];
            Array.Copy(littleEndianBytes, trimmed, length);

            return trimmed;
        }
    }
}
