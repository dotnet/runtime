// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Test.Common
{
    // Stolen from product code
    public static class HuffmanDecoder
    {
        private static readonly (int codeLength, int[] codes)[] s_decodingTable = new[]
        {
            (5, new[] { 48, 49, 50, 97, 99, 101, 105, 111, 115, 116 }),
            (6, new[] { 32, 37, 45, 46, 47, 51, 52, 53, 54, 55, 56, 57, 61, 65, 95, 98, 100, 102, 103, 104, 108, 109, 110, 112, 114, 117 }),
            (7, new[] { 58, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 89, 106, 107, 113, 118, 119, 120, 121, 122 }),
            (8, new[] { 38, 42, 44, 59, 88, 90 }),
            (10, new[] { 33, 34, 40, 41, 63 }),
            (11, new[] { 39, 43, 124 }),
            (12, new[] { 35, 62 }),
            (13, new[] { 0, 36, 64, 91, 93, 126 }),
            (14, new[] { 94, 125 }),
            (15, new[] { 60, 96, 123 }),
            (19, new[] { 92, 195, 208 }),
            (20, new[] { 128, 130, 131, 162, 184, 194, 224, 226 }),
            (21, new[] { 153, 161, 167, 172, 176, 177, 179, 209, 216, 217, 227, 229, 230 }),
            (22, new[] { 129, 132, 133, 134, 136, 146, 154, 156, 160, 163, 164, 169, 170, 173, 178, 181, 185, 186, 187, 189, 190, 196, 198, 228, 232, 233 }),
            (23, new[] { 1, 135, 137, 138, 139, 140, 141, 143, 147, 149, 150, 151, 152, 155, 157, 158, 165, 166, 168, 174, 175, 180, 182, 183, 188, 191, 197, 231, 239 }),
            (24, new[] { 9, 142, 144, 145, 148, 159, 171, 206, 215, 225, 236, 237 }),
            (25, new[] { 199, 207, 234, 235 }),
            (26, new[] { 192, 193, 200, 201, 202, 205, 210, 213, 218, 219, 238, 240, 242, 243, 255 }),
            (27, new[] { 203, 204, 211, 212, 214, 221, 222, 223, 241, 244, 245, 246, 247, 248, 250, 251, 252, 253, 254 }),
            (28, new[] { 2, 3, 4, 5, 6, 7, 8, 11, 12, 14, 15, 16, 17, 18, 19, 20, 21, 23, 24, 25, 26, 27, 28, 29, 30, 31, 127, 220, 249 }),
            (30, new[] { 10, 13, 22, 256 })
        };

        /// <summary>
        /// Decodes a Huffman encoded string from a byte array.
        /// </summary>
        /// <param name="src">The source byte array containing the encoded data.</param>
        /// <param name="dst">The destination byte array to store the decoded data.</param>
        /// <returns>The number of decoded symbols.</returns>
        public static int Decode(ReadOnlySpan<byte> src, Span<byte> dst)
        {
            int i = 0;
            int j = 0;
            int lastDecodedBits = 0;
            while (i < src.Length)
            {
                // Note that if lastDecodeBits is 3 or more, then we will only get 5 bits (or less)
                // from src[i]. Thus we need to read 5 bytes here to ensure that we always have 
                // at least 30 bits available for decoding.
                // TODO ISSUE 31751: Rework this as part of Huffman perf improvements
                uint next = (uint)(src[i] << 24 + lastDecodedBits);
                next |= (i + 1 < src.Length ? (uint)(src[i + 1] << 16 + lastDecodedBits) : 0);
                next |= (i + 2 < src.Length ? (uint)(src[i + 2] << 8 + lastDecodedBits) : 0);
                next |= (i + 3 < src.Length ? (uint)(src[i + 3] << lastDecodedBits) : 0);
                next |= (i + 4 < src.Length ? (uint)(src[i + 4] >> (8 - lastDecodedBits)) : 0);

                uint ones = (uint)(int.MinValue >> (8 - lastDecodedBits - 1));
                if (i == src.Length - 1 && lastDecodedBits > 0 && (next & ones) == ones)
                {
                    // The remaining 7 or less bits are all 1, which is padding.
                    // We specifically check that lastDecodedBits > 0 because padding
                    // longer than 7 bits should be treated as a decoding error.
                    // http://httpwg.org/specs/rfc7541.html#rfc.section.5.2
                    break;
                }

                // The longest possible symbol size is 30 bits. If we're at the last 4 bytes
                // of the input, we need to make sure we pass the correct number of valid bits
                // left, otherwise the trailing 0s in next may form a valid symbol.
                int validBits = Math.Min(30, (8 - lastDecodedBits) + (src.Length - i - 1) * 8);
                int ch = DecodeValue(next, validBits, out int decodedBits);

                if (ch == -1)
                {
                    // No valid symbol could be decoded with the bits in next
                    throw new Exception("huffman decoding error");
                }
                else if (ch == 256)
                {
                    // A Huffman-encoded string literal containing the EOS symbol MUST be treated as a decoding error.
                    // http://httpwg.org/specs/rfc7541.html#rfc.section.5.2
                    throw new Exception("huffman decoding error");
                }

                if (j == dst.Length)
                {
                    // Destination is too small.
                    throw new Exception("huffman decoding error");
                }

                dst[j++] = (byte)ch;

                // If we crossed a byte boundary, advance i so we start at the next byte that's not fully decoded.
                lastDecodedBits += decodedBits;
                i += lastDecodedBits / 8;

                // Modulo 8 since we only care about how many bits were decoded in the last byte that we processed.
                lastDecodedBits %= 8;
            }

            return j;
        }

        /// <summary>
        /// Decodes a single symbol from a 32-bit word.
        /// </summary>
        /// <param name="data">A 32-bit word containing a Huffman encoded symbol.</param>
        /// <param name="validBits">
        /// The number of bits in <paramref name="data"/> that may contain an encoded symbol.
        /// This is not the exact number of bits that encode the symbol. Instead, it prevents
        /// decoding the lower bits of <paramref name="data"/> if they don't contain any
        /// encoded data.
        /// </param>
        /// <param name="decodedBits">The number of bits decoded from <paramref name="data"/>.</param>
        /// <returns>The decoded symbol.</returns>
        private static int DecodeValue(uint data, int validBits, out int decodedBits)
        {
            // The code below implements the decoding logic for a canonical Huffman code.
            //
            // To decode a symbol, we scan the decoding table, which is sorted by ascending symbol bit length.
            // For each bit length b, we determine the maximum b-bit encoded value, plus one (that is codeMax).
            // This is done with the following logic:
            //
            // if we're at the first entry in the table,
            //    codeMax = the # of symbols encoded in b bits
            // else,
            //    left-shift codeMax by the difference between b and the previous entry's bit length,
            //    then increment codeMax by the # of symbols encoded in b bits
            //
            // Next, we look at the value v encoded in the highest b bits of data. If v is less than codeMax,
            // those bits correspond to a Huffman encoded symbol. We find the corresponding decoded
            // symbol in the list of values associated with bit length b in the decoding table by indexing it
            // with codeMax - v.

            int codeMax = 0;

            for (int i = 0; i < s_decodingTable.Length && s_decodingTable[i].codeLength <= validBits; i++)
            {
                (int codeLength, int[] codes) = s_decodingTable[i];

                if (i > 0)
                {
                    codeMax <<= codeLength - s_decodingTable[i - 1].codeLength;
                }

                codeMax += codes.Length;

                int mask = int.MinValue >> (codeLength - 1);
                long masked = (data & mask) >> (32 - codeLength);

                if (masked < codeMax)
                {
                    decodedBits = codeLength;
                    return codes[codes.Length - (codeMax - masked)];
                }
            }

            decodedBits = 0;
            return -1;
        }
    }
}
