// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static partial class AsymmetricAlgorithmHelpers
    {
        internal static int BitsToBytes(int bitLength)
        {
            int byteLength = (bitLength + 7) / 8;
            return byteLength;
        }

        internal static int GetMaxDerSignatureSize(int fieldSizeBits)
        {
            // This encoding format is the DER-encoded representation of
            // SEQUENCE(INTEGER(r), INTEGER(s)).
            // Each of r and s are unsigned fieldSizeBits integers, and if byte-aligned
            // then they may gain a padding byte to avoid being a negative number.
            // The biggest single-byte length encoding for DER is 0x7F bytes, but we're
            // symmetric, so 0x7E (126).
            // 63 bytes per half allows for 61 content bytes (prefix 02 3D), which can
            // encode up to a ((61 * 8) - 1)-bit integer.
            // So, any fieldSizeBits <= 487 maximally needs 2 * fieldSizeBytes + 6 bytes,
            // because all lengths fit in one byte. (30 7E 02 3D ... 02 3D ...)

            // Add the padding bit because of unsigned -> signed.
            int paddedFieldSizeBytes = BitsToBytes(fieldSizeBits + 1);

            if (paddedFieldSizeBytes <= 61)
            {
                return 2 * paddedFieldSizeBytes + 6;
            }

            // Past this point the sequence length grows (30 81 xx) up until 0xFF payload.
            // Per our symmetry, that happens when the integers themselves max out, which is
            // when paddedFieldSizeBytes is 0x7F; which covers up to a 1015-bit (before padding) field.

            if (paddedFieldSizeBytes <= 0x7F)
            {
                return 2 * paddedFieldSizeBytes + 7;
            }

            // Beyond here, we'll just do math.
            int segmentSize = 2 + GetDerLengthLength(paddedFieldSizeBytes) + paddedFieldSizeBytes;
            int payloadSize = 2 * segmentSize;
            int sequenceSize = 2 + GetDerLengthLength(payloadSize) + payloadSize;
            return sequenceSize;

            static int GetDerLengthLength(int payloadLength)
            {
                Debug.Assert(payloadLength >= 0);

                if (payloadLength <= 0x7F)
                    return 0;

                if (payloadLength <= 0xFF)
                    return 1;

                if (payloadLength <= 0xFFFF)
                    return 2;

                if (payloadLength <= 0xFFFFFF)
                    return 3;

                return 4;
            }
        }
    }
}
