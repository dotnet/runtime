// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Security
{
    internal sealed class MD4
    {
        private byte[]? HashValue;
        private uint[] state;
        private byte[] buffer;
        private uint[] count;
        private uint[] x;

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

        private byte[] digest;
        public int HashSize => 128;
        public int HashSizeBytes => HashSize / 8;

        public MD4()
        {
            // we allocate the context memory
            state = new uint[4];
            count = new uint[2];
            buffer = new byte[64];
            digest = new byte[16];
            // temporary buffer in MD4Transform that we don't want to keep allocate on each iteration
            x = new uint[16];
            // the initialize our context
            Initialize();
        }

        public int HashData(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < HashSizeBytes)
            {
                throw new ArgumentException("Destination is too short.", nameof(destination));
            }

            ComputeHash(source.ToArray(), 0, source.Length).CopyTo(destination);
            return destination.Length;
        }

        private void Initialize()
        {
            count[0] = 0;
            count[1] = 0;
            state[0] = 0x67452301;
            state[1] = 0xefcdab89;
            state[2] = 0x98badcfe;
            state[3] = 0x10325476;
            // Zeroize sensitive information
            Array.Clear(buffer, 0, 64);
            Array.Clear(x, 0, 16);
        }

        private byte[] ComputeHash(byte[] array, int offset, int count)
        {
            HashCore(array, offset, count);
            HashValue = HashFinal();
            byte[] result = (byte[]) HashValue.Clone();
            Initialize();
            return result;
        }

        private void HashCore(byte[] array, int ibStart, int cbSize)
        {
            /* Compute number of bytes mod 64 */
            int index = (int)((count[0] >> 3) & 0x3F);
            /* Update number of bits */
            count[0] += (uint)(cbSize << 3);
            if (count[0] < (cbSize << 3))
                count[1]++;
            count[1] += (uint)(cbSize >> 29);

            int partLen = 64 - index;
            int i = 0;
            /* Transform as many times as possible. */
            if (cbSize >= partLen)
            {
                Buffer.BlockCopy(array, ibStart, buffer, index, partLen);
                MD4Transform(state, buffer, 0);

                for (i = partLen; i + 63 < cbSize; i += 64)
                {
                    MD4Transform(state, array, ibStart + i);
                }

                index = 0;
            }

            /* Buffer remaining input */
            Buffer.BlockCopy(array, ibStart + i, buffer, index, (cbSize - i));
        }

        private byte[] HashFinal()
        {
            /* Save number of bits */
            byte[] bits = new byte[8];
            Encode(bits, count);

            /* Pad out to 56 mod 64. */
            uint index = ((count[0] >> 3) & 0x3f);
            int padLen = (int)((index < 56) ? (56 - index) : (120 - index));
            HashCore(Padding(padLen), 0, padLen);

            /* Append length (before padding) */
            HashCore(bits, 0, 8);

            /* Store state in digest */
            Encode(digest, state);

            // Zeroize sensitive information.
            Initialize();

            return digest;
        }

        //--- private methods ---------------------------------------------------

        private static byte[] Padding(int nLength)
        {
            byte[] padding = new byte[nLength];
            padding[0] = 0x80;
            return padding;
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

        /* ROTATE_LEFT rotates x left n bits. */
        private static uint ROL(uint x, byte n)
        {
            return (uint)(((x) << (n)) | ((x) >> (32 - (n))));
        }

        /* FF, GG and HH are transformations for rounds 1, 2 and 3 */
        /* Rotation is separate from addition to prevent recomputation */
        private static void FF(ref uint a, uint b, uint c, uint d, uint x, byte s)
        {
            a += F(b, c, d) + x;
            a = ROL(a, s);
        }

        private static void GG(ref uint a, uint b, uint c, uint d, uint x, byte s)
        {
            a += G(b, c, d) + x + 0x5a827999;
            a = ROL(a, s);
        }

        private static void HH(ref uint a, uint b, uint c, uint d, uint x, byte s)
        {
            a += H(b, c, d) + x + 0x6ed9eba1;
            a = ROL(a, s);
        }

        private static void Encode(byte[] output, uint[] input)
        {
            for (int i = 0, j = 0; j < output.Length; i++, j += 4)
            {
                output[j] = (byte)(input[i]);
                output[j + 1] = (byte)(input[i] >> 8);
                output[j + 2] = (byte)(input[i] >> 16);
                output[j + 3] = (byte)(input[i] >> 24);
            }
        }

        private static void Decode(uint[] output, byte[] input, int index)
        {
            for (int i = 0, j = index; i < output.Length; i++, j += 4)
            {
                output[i] = (uint)((input[j]) | (input[j + 1] << 8) | (input[j + 2] << 16) | (input[j + 3] << 24));
            }
        }

        private void MD4Transform(uint[] state, byte[] block, int index)
        {
            uint a = state[0];
            uint b = state[1];
            uint c = state[2];
            uint d = state[3];

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
