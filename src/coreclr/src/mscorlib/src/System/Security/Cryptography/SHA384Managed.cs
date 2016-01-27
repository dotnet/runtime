// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// C# implementation of the proposed SHA-384 hash algorithm
//

namespace System.Security.Cryptography {
    using System;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    public class SHA384Managed : SHA384
    {
        private byte[]   _buffer;
        private ulong    _count; // Number of bytes in the hashed message
        private UInt64[] _stateSHA384;
        private UInt64[] _W;

        //
        // public constructors
        //

        public SHA384Managed()
        {
            if (CryptoConfig.AllowOnlyFipsAlgorithms)
                throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm"));
            Contract.EndContractBlock();

            _stateSHA384 = new UInt64[8];
            _buffer = new byte[128];
            _W = new UInt64[80];

            InitializeState();
        }

        //
        // public methods
        //

        public override void Initialize() {
            InitializeState();

            // Zeroize potentially sensitive information.
            Array.Clear(_buffer, 0, _buffer.Length);
            Array.Clear(_W, 0, _W.Length);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override void HashCore(byte[] rgb, int ibStart, int cbSize) {
            _HashData(rgb, ibStart, cbSize);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override byte[] HashFinal() {
            return _EndHash();
        }

        //
        // private methods
        //

        private void InitializeState() {
            _count = 0;

            _stateSHA384[0] = 0xcbbb9d5dc1059ed8;
            _stateSHA384[1] = 0x629a292a367cd507;
            _stateSHA384[2] = 0x9159015a3070dd17;
            _stateSHA384[3] = 0x152fecd8f70e5939;
            _stateSHA384[4] = 0x67332667ffc00b31;
            _stateSHA384[5] = 0x8eb44a8768581511;
            _stateSHA384[6] = 0xdb0c2e0d64f98fa7;
            _stateSHA384[7] = 0x47b5481dbefa4fa4;
        }

        /* SHA384 block update operation. Continues an SHA message-digest
           operation, processing another message block, and updating the
           context.
           */

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe void _HashData(byte[] partIn, int ibStart, int cbSize)
        {
            int bufferLen;
            int partInLen = cbSize;
            int partInBase = ibStart;

            /* Compute length of buffer */
            bufferLen = (int) (_count & 0x7f);

            /* Update number of bytes */
            _count += (ulong) partInLen;

            fixed (UInt64* stateSHA384 = _stateSHA384) {
                fixed (byte* buffer = _buffer) {
                    fixed (UInt64* expandedBuffer = _W) {
                        if ((bufferLen > 0) && (bufferLen + partInLen >= 128)) {
                            Buffer.InternalBlockCopy(partIn, partInBase, _buffer, bufferLen, 128 - bufferLen);
                            partInBase += (128 - bufferLen);
                            partInLen -= (128 - bufferLen);
                            SHATransform(expandedBuffer, stateSHA384, buffer);
                            bufferLen = 0;
                        }

                        /* Copy input to temporary buffer and hash */
                        while (partInLen >= 128) {
                            Buffer.InternalBlockCopy(partIn, partInBase, _buffer, 0, 128);
                            partInBase += 128;
                            partInLen -= 128;
                            SHATransform(expandedBuffer, stateSHA384, buffer);
                        }

                        if (partInLen > 0) {
                            Buffer.InternalBlockCopy(partIn, partInBase, _buffer, bufferLen, partInLen);
                        }
                    }
                }
            }
        }

        /* SHA384 finalization. Ends an SHA384 message-digest operation, writing
           the message digest.
           */

        [System.Security.SecurityCritical]  // auto-generated
        private byte[] _EndHash()
        {
            byte[]         pad;
            int            padLen;
            ulong          bitCount;
            byte[]         hash = new byte[48]; // HashSizeValue = 384

            /* Compute padding: 80 00 00 ... 00 00 <bit count>
             */

            padLen = 128 - (int)(_count & 0x7f);
            if (padLen <= 16)
                padLen += 128;

            pad = new byte[padLen];
            pad[0] = 0x80;

            //  Convert count to bit count
            bitCount = _count * 8;

            // bitCount is at most 8 * 128 = 1024. Its representation as a 128-bit number has all bits set to zero
            // except eventually the 11 lower bits

            //pad[padLen-16] = (byte) ((bitCount >> 120) & 0xff);
            //pad[padLen-15] = (byte) ((bitCount >> 112) & 0xff);
            //pad[padLen-14] = (byte) ((bitCount >> 104) & 0xff);
            //pad[padLen-13] = (byte) ((bitCount >> 96) & 0xff);
            //pad[padLen-12] = (byte) ((bitCount >> 88) & 0xff);
            //pad[padLen-11] = (byte) ((bitCount >> 80) & 0xff);
            //pad[padLen-10] = (byte) ((bitCount >> 72) & 0xff);
            //pad[padLen-9] = (byte) ((bitCount >> 64) & 0xff);
            pad[padLen-8] = (byte) ((bitCount >> 56) & 0xff);
            pad[padLen-7] = (byte) ((bitCount >> 48) & 0xff);
            pad[padLen-6] = (byte) ((bitCount >> 40) & 0xff);
            pad[padLen-5] = (byte) ((bitCount >> 32) & 0xff);
            pad[padLen-4] = (byte) ((bitCount >> 24) & 0xff);
            pad[padLen-3] = (byte) ((bitCount >> 16) & 0xff);
            pad[padLen-2] = (byte) ((bitCount >> 8) & 0xff);
            pad[padLen-1] = (byte) ((bitCount >> 0) & 0xff);

            /* Digest padding */
            _HashData(pad, 0, pad.Length);

            /* Store digest */
            Utils.QuadWordToBigEndian (hash, _stateSHA384, 6);

            HashValue = hash;
            return hash;
        }

        private readonly static UInt64[] _K = {
            0x428a2f98d728ae22, 0x7137449123ef65cd, 0xb5c0fbcfec4d3b2f, 0xe9b5dba58189dbbc,
            0x3956c25bf348b538, 0x59f111f1b605d019, 0x923f82a4af194f9b, 0xab1c5ed5da6d8118,
            0xd807aa98a3030242, 0x12835b0145706fbe, 0x243185be4ee4b28c, 0x550c7dc3d5ffb4e2,
            0x72be5d74f27b896f, 0x80deb1fe3b1696b1, 0x9bdc06a725c71235, 0xc19bf174cf692694,
            0xe49b69c19ef14ad2, 0xefbe4786384f25e3, 0x0fc19dc68b8cd5b5, 0x240ca1cc77ac9c65,
            0x2de92c6f592b0275, 0x4a7484aa6ea6e483, 0x5cb0a9dcbd41fbd4, 0x76f988da831153b5,
            0x983e5152ee66dfab, 0xa831c66d2db43210, 0xb00327c898fb213f, 0xbf597fc7beef0ee4,
            0xc6e00bf33da88fc2, 0xd5a79147930aa725, 0x06ca6351e003826f, 0x142929670a0e6e70,
            0x27b70a8546d22ffc, 0x2e1b21385c26c926, 0x4d2c6dfc5ac42aed, 0x53380d139d95b3df,
            0x650a73548baf63de, 0x766a0abb3c77b2a8, 0x81c2c92e47edaee6, 0x92722c851482353b,
            0xa2bfe8a14cf10364, 0xa81a664bbc423001, 0xc24b8b70d0f89791, 0xc76c51a30654be30,
            0xd192e819d6ef5218, 0xd69906245565a910, 0xf40e35855771202a, 0x106aa07032bbd1b8,
            0x19a4c116b8d2d0c8, 0x1e376c085141ab53, 0x2748774cdf8eeb99, 0x34b0bcb5e19b48a8,
            0x391c0cb3c5c95a63, 0x4ed8aa4ae3418acb, 0x5b9cca4f7763e373, 0x682e6ff3d6b2b8a3,
            0x748f82ee5defb2fc, 0x78a5636f43172f60, 0x84c87814a1f0ab72, 0x8cc702081a6439ec,
            0x90befffa23631e28, 0xa4506cebde82bde9, 0xbef9a3f7b2c67915, 0xc67178f2e372532b,
            0xca273eceea26619c, 0xd186b8c721c0c207, 0xeada7dd6cde0eb1e, 0xf57d4f7fee6ed178,
            0x06f067aa72176fba, 0x0a637dc5a2c898a6, 0x113f9804bef90dae, 0x1b710b35131c471b,
            0x28db77f523047d84, 0x32caab7b40c72493, 0x3c9ebe0a15c9bebc, 0x431d67c49c100d4c,
            0x4cc5d4becb3e42b6, 0x597f299cfc657e2a, 0x5fcb6fab3ad6faec, 0x6c44198c4a475817,
        };

        [System.Security.SecurityCritical]  // auto-generated
        private static unsafe void SHATransform (UInt64* expandedBuffer, UInt64* state, byte* block)
        {
            UInt64 a, b, c, d, e, f, g, h;
            UInt64 aa, bb, cc, dd, ee, ff, hh, gg;
            UInt64 T1;

            a = state[0];
            b = state[1];
            c = state[2];
            d = state[3];
            e = state[4];
            f = state[5];
            g = state[6];
            h = state[7];

            // fill in the first 16 blocks of W.
            Utils.QuadWordFromBigEndian (expandedBuffer, 16, block);
            SHA384Expand (expandedBuffer);

            /* Apply the SHA384 compression function */
            // We are trying to be smart here and avoid as many copies as we can
            // The perf gain with this method over the straightforward modify and shift 
            // forward is >= 20%, so it's worth the pain
            for (int j=0; j<80; ) {
                T1 = h + Sigma_1(e) + Ch(e,f,g) + _K[j] + expandedBuffer[j];
                ee = d + T1;
                aa = T1 + Sigma_0(a) + Maj(a,b,c);
                j++;

                T1 = g + Sigma_1(ee) + Ch(ee,e,f) + _K[j] + expandedBuffer[j];
                ff = c + T1;
                bb = T1 + Sigma_0(aa) + Maj(aa,a,b);
                j++;

                T1 = f + Sigma_1(ff) + Ch(ff,ee,e) + _K[j] + expandedBuffer[j];
                gg = b + T1;
                cc = T1 + Sigma_0(bb) + Maj(bb,aa,a);
                j++;

                T1 = e + Sigma_1(gg) + Ch(gg,ff,ee) + _K[j] + expandedBuffer[j];
                hh = a + T1;
                dd = T1 + Sigma_0(cc) + Maj(cc,bb,aa);
                j++;

                T1 = ee + Sigma_1(hh) + Ch(hh,gg,ff) + _K[j] + expandedBuffer[j];
                h = aa + T1;
                d = T1 + Sigma_0(dd) + Maj(dd,cc,bb);
                j++;

                T1 = ff + Sigma_1(h) + Ch(h,hh,gg) + _K[j] + expandedBuffer[j];
                g = bb + T1;
                c = T1 + Sigma_0(d) + Maj(d,dd,cc);
                j++;

                T1 = gg + Sigma_1(g) + Ch(g,h,hh) + _K[j] + expandedBuffer[j];
                f = cc + T1;
                b = T1 + Sigma_0(c) + Maj(c,d,dd);
                j++;

                T1 = hh + Sigma_1(f) + Ch(f,g,h) + _K[j] + expandedBuffer[j];
                e = dd + T1;
                a = T1 + Sigma_0(b) + Maj(b,c,d);
                j++;
            }

            state[0] += a;
            state[1] += b;
            state[2] += c;
            state[3] += d;
            state[4] += e;
            state[5] += f;
            state[6] += g;
            state[7] += h;
        }

        private static UInt64 RotateRight(UInt64 x, int n) {
            return (((x) >> (n)) | ((x) << (64-(n))));
        }

        private static UInt64 Ch(UInt64 x, UInt64 y, UInt64 z) {
            return ((x & y) ^ ((x ^ 0xffffffffffffffff) & z));
        }

        private static UInt64 Maj(UInt64 x, UInt64 y, UInt64 z) {
            return ((x & y) ^ (x & z) ^ (y & z));
        }

        private static UInt64 Sigma_0(UInt64 x) {
            return (RotateRight(x,28) ^ RotateRight(x,34) ^ RotateRight(x,39));
        }

        private static UInt64 Sigma_1(UInt64 x) {
            return (RotateRight(x,14) ^ RotateRight(x,18) ^ RotateRight(x,41));
        }

        private static UInt64 sigma_0(UInt64 x) {
            return (RotateRight(x,1) ^ RotateRight(x,8) ^ (x >> 7));
        }

        private static UInt64 sigma_1(UInt64 x) {
            return (RotateRight(x,19) ^ RotateRight(x,61) ^ (x >> 6));
        }

        /* This function creates W_16,...,W_79 according to the formula
           W_j <- sigma_1(W_{j-2}) + W_{j-7} + sigma_0(W_{j-15}) + W_{j-16};
        */

        [System.Security.SecurityCritical]  // auto-generated
        private static unsafe void SHA384Expand (UInt64* x)
        {
            for (int i = 16; i < 80; i++) {
                x[i] = sigma_1(x[i-2]) + x[i-7] + sigma_0(x[i-15]) + x[i-16];
            }
        }
    }
}
