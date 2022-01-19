// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

//
// This class is a port of the Mono managed implementation of the MD4 algorithm
// and required to support NTLM in Android only.
// It's an implementation detail and is not intended to be a public API.
// Assuming that NTLM would be System.Net.Security, it makes sense to put MD4 here as well.
//
namespace System.Net.Security
{
    internal sealed class MD4
    {
        private const int S11 = 3;
        private const int S12 = 7;
        private const int S13 = 11;
        private const int S14 = 19;
        private const int S21 = 3;
        private const int S22 = 5;
        private const int S23 = 9;
        private const int S24 = 13;
        private const int S31 = 3;
        private const int S32 = 9;
        private const int S33 = 11;
        private const int S34 = 15;

        internal static void HashData(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            Debug.Assert(destination.Length == 128 >> 3);
            Span<byte> buffer = stackalloc byte[64];
            Span<uint> state = stackalloc uint[4];
            Span<uint> count = stackalloc uint[2];

            // the initialize our context
            count[0] = 0;
            count[1] = 0;
            state[0] = 0x67452301;
            state[1] = 0xefcdab89;
            state[2] = 0x98badcfe;
            state[3] = 0x10325476;
            // Zeroize sensitive information
            buffer.Fill(0);

            HashCore(source, state, count, buffer);

            /* Save number of bits */
            Span<byte> bits = stackalloc byte[8];
            Encode(bits, count);

            /* Pad out to 56 mod 64. */
            uint index = ((count[0] >> 3) & 0x3f);
            int padLen = (int)((index < 56) ? (56 - index) : (120 - index));
            Span<byte> padding = stackalloc byte[padLen];
            padding[0] = 0x80;
            HashCore(padding, state, count, buffer);

            /* Append length (before padding) */
            HashCore(bits, state, count, buffer);

            /* Write state to destination */
            Encode(destination, state);
        }

        private static void HashCore(ReadOnlySpan<byte> input, Span<uint> state, Span<uint> count, Span<byte> buffer)
        {
            /* Compute number of bytes mod 64 */
            int index = (int)((count[0] >> 3) & 0x3F);
            /* Update number of bits */
            count[0] += (uint)(input.Length << 3);
            if (count[0] < (input.Length << 3))
                count[1]++;
            count[1] += (uint)(input.Length >> 29);

            int partLen = 64 - index;
            int i = 0;
            /* Transform as many times as possible. */
            if (input.Length >= partLen)
            {
                BlockCopy(input, 0, buffer, index, partLen);
                MD4Transform(state, buffer, 0);

                for (i = partLen; i + 63 < input.Length; i += 64)
                {
                    MD4Transform(state, input, i);
                }

                index = 0;
            }

            /* Buffer remaining input */
            input.Slice(i).CopyTo(buffer.Slice(index));
        }

        //--- private methods ---------------------------------------------------

        private static void BlockCopy(ReadOnlySpan<byte> source, int srcOffset, Span<byte> destination, int dstOffset, int count)
        {
            for (int srcIndex = srcOffset, dstIndex = dstOffset; srcIndex < srcOffset + count; srcIndex++, dstIndex++)
            {
                destination[dstIndex] = source[srcIndex];
            }
        }

        /* F, G and H are basic MD4 functions. */
        private static uint F(uint x, uint y, uint z)
        {
            return (uint)(((x) & (y)) | ((~x) & (z)));
        }

        private static uint G(uint x, uint y, uint z)
        {
            return (uint)(((x) & (y)) | ((x) & (z)) | ((y) & (z)));
        }

        private static uint H(uint x, uint y, uint z)
        {
            return (uint)((x) ^ (y) ^ (z));
        }

        /* FF, GG and HH are transformations for rounds 1, 2 and 3 */
        /* Rotation is separate from addition to prevent recomputation */
        private static void FF(ref uint a, uint b, uint c, uint d, uint x, byte s)
        {
            a += F(b, c, d) + x;
            a = BitOperations.RotateLeft(a, s);
        }

        private static void GG(ref uint a, uint b, uint c, uint d, uint x, byte s)
        {
            a += G(b, c, d) + x + 0x5a827999;
            a = BitOperations.RotateLeft(a, s);
        }

        private static void HH(ref uint a, uint b, uint c, uint d, uint x, byte s)
        {
            a += H(b, c, d) + x + 0x6ed9eba1;
            a = BitOperations.RotateLeft(a, s);
        }

        private static void Encode(Span<byte> output, Span<uint> input)
        {
            for (int i = 0, j = 0; j < output.Length; i++, j += 4)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(j), input[i]);
            }
        }

        private static void Decode(Span<uint> output, ReadOnlySpan<byte> input, int index)
        {
            for (int i = 0, j = index; i < output.Length; i++, j += 4)
            {
                output[i] = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(j));
            }
        }

        private static void MD4Transform(Span<uint> state, ReadOnlySpan<byte> block, int index)
        {
            uint a = state[0];
            uint b = state[1];
            uint c = state[2];
            uint d = state[3];
            Span<uint> x = stackalloc uint[16];

            Decode(x, block, index);

            /* Round 1 */
            FF(ref a, b, c, d, x[0], S11); /* 1 */
            FF(ref d, a, b, c, x[1], S12); /* 2 */
            FF(ref c, d, a, b, x[2], S13); /* 3 */
            FF(ref b, c, d, a, x[3], S14); /* 4 */
            FF(ref a, b, c, d, x[4], S11); /* 5 */
            FF(ref d, a, b, c, x[5], S12); /* 6 */
            FF(ref c, d, a, b, x[6], S13); /* 7 */
            FF(ref b, c, d, a, x[7], S14); /* 8 */
            FF(ref a, b, c, d, x[8], S11); /* 9 */
            FF(ref d, a, b, c, x[9], S12); /* 10 */
            FF(ref c, d, a, b, x[10], S13); /* 11 */
            FF(ref b, c, d, a, x[11], S14); /* 12 */
            FF(ref a, b, c, d, x[12], S11); /* 13 */
            FF(ref d, a, b, c, x[13], S12); /* 14 */
            FF(ref c, d, a, b, x[14], S13); /* 15 */
            FF(ref b, c, d, a, x[15], S14); /* 16 */

            /* Round 2 */
            GG(ref a, b, c, d, x[0], S21); /* 17 */
            GG(ref d, a, b, c, x[4], S22); /* 18 */
            GG(ref c, d, a, b, x[8], S23); /* 19 */
            GG(ref b, c, d, a, x[12], S24); /* 20 */
            GG(ref a, b, c, d, x[1], S21); /* 21 */
            GG(ref d, a, b, c, x[5], S22); /* 22 */
            GG(ref c, d, a, b, x[9], S23); /* 23 */
            GG(ref b, c, d, a, x[13], S24); /* 24 */
            GG(ref a, b, c, d, x[2], S21); /* 25 */
            GG(ref d, a, b, c, x[6], S22); /* 26 */
            GG(ref c, d, a, b, x[10], S23); /* 27 */
            GG(ref b, c, d, a, x[14], S24); /* 28 */
            GG(ref a, b, c, d, x[3], S21); /* 29 */
            GG(ref d, a, b, c, x[7], S22); /* 30 */
            GG(ref c, d, a, b, x[11], S23); /* 31 */
            GG(ref b, c, d, a, x[15], S24); /* 32 */

            HH(ref a, b, c, d, x[0], S31); /* 33 */
            HH(ref d, a, b, c, x[8], S32); /* 34 */
            HH(ref c, d, a, b, x[4], S33); /* 35 */
            HH(ref b, c, d, a, x[12], S34); /* 36 */
            HH(ref a, b, c, d, x[2], S31); /* 37 */
            HH(ref d, a, b, c, x[10], S32); /* 38 */
            HH(ref c, d, a, b, x[6], S33); /* 39 */
            HH(ref b, c, d, a, x[14], S34); /* 40 */
            HH(ref a, b, c, d, x[1], S31); /* 41 */
            HH(ref d, a, b, c, x[9], S32); /* 42 */
            HH(ref c, d, a, b, x[5], S33); /* 43 */
            HH(ref b, c, d, a, x[13], S34); /* 44 */
            HH(ref a, b, c, d, x[3], S31); /* 45 */
            HH(ref d, a, b, c, x[11], S32); /* 46 */
            HH(ref c, d, a, b, x[7], S33); /* 47 */
            HH(ref b, c, d, a, x[15], S34); /* 48 */

            state[0] += a;
            state[1] += b;
            state[2] += c;
            state[3] += d;
        }
    }
}
