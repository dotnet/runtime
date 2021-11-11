// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using static System.Numerics.BitOperations;

namespace Internal.Cryptography
{
    internal sealed class SHAHashProvider : HashProvider
    {
        private int hashSizeInBytes;
        private SHAManagedImplementationBase impl;
        private MemoryStream buffer;

        public SHAHashProvider(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                    impl = new SHA1ManagedImplementation();
                    hashSizeInBytes = 20;
                    break;
                case HashAlgorithmNames.SHA256:
                    impl = new SHA256ManagedImplementation();
                    hashSizeInBytes = 32;
                    break;
                case HashAlgorithmNames.SHA384:
                    impl = new SHA384ManagedImplementation();
                    hashSizeInBytes = 48;
                    break;
                case HashAlgorithmNames.SHA512:
                    impl = new SHA512ManagedImplementation();
                    hashSizeInBytes = 64;
                    break;
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
            }
        }

        public override void AppendHashData(ReadOnlySpan<byte> data)
        {
            if (buffer == null)
            {
                buffer = new MemoryStream(1000);
            }

            buffer.Write(data);
        }

        public override int FinalizeHashAndReset(Span<byte> destination)
        {
            GetCurrentHash(destination);
            buffer = null;

            return hashSizeInBytes;
        }

        public override int GetCurrentHash(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= hashSizeInBytes);

            impl.Initialize();
            if (buffer != null)
            {
                impl.HashCore(buffer.GetBuffer(), 0, (int)buffer.Length);
            }
            impl.HashFinal().CopyTo(destination);

            return hashSizeInBytes;
        }

        public override int HashSizeInBytes => hashSizeInBytes;

        public override void Reset()
        {
            buffer = null;
            impl.Initialize();
        }

        public override void Dispose(bool disposing)
        {
        }

        private abstract class SHAManagedImplementationBase
        {
            public abstract void Initialize();
            public abstract void HashCore(byte[] partIn, int ibStart, int cbSize);
            public abstract byte[] HashFinal();
        }

        private sealed class SHA1ManagedImplementation : SHAManagedImplementationBase
        {
            // It's ok to use a "non-secret purposes" hashing implementation here, as this is only
            // used in wasm scenarios, and as of the current release we don't make any security guarantees
            // about our crypto primitives in wasm environments.
            private Sha1ForNonSecretPurposes _state; // mutable struct - don't make readonly

            public override void Initialize()
            {
                _state = default;
                _state.Start();
            }

            public override void HashCore(byte[] partIn, int ibStart, int cbSize)
            {
                _state.Append(partIn.AsSpan(ibStart, cbSize));
            }

            public override byte[] HashFinal()
            {
                byte[] output = new byte[20];
                _state.Finish(output);
                return output;
            }
        }

        // ported from https://github.com/microsoft/referencesource/blob/a48449cb48a9a693903668a71449ac719b76867c/mscorlib/system/security/cryptography/sha256managed.cs
        private sealed class SHA256ManagedImplementation : SHAManagedImplementationBase
        {
            private byte[] _buffer;
            private long _count; // Number of bytes in the hashed message
            private uint[] _stateSHA256;
            private uint[] _W;

            public SHA256ManagedImplementation()
            {
                _stateSHA256 = new uint[8];
                _buffer = new byte[64];
                _W = new uint[64];

                InitializeState();
            }

            public override void Initialize()
            {
                InitializeState();

                // Zeroize potentially sensitive information.
                Array.Clear(_buffer, 0, _buffer.Length);
                Array.Clear(_W, 0, _W.Length);
            }

            private void InitializeState()
            {
                _count = 0;

                _stateSHA256[0] = 0x6a09e667;
                _stateSHA256[1] = 0xbb67ae85;
                _stateSHA256[2] = 0x3c6ef372;
                _stateSHA256[3] = 0xa54ff53a;
                _stateSHA256[4] = 0x510e527f;
                _stateSHA256[5] = 0x9b05688c;
                _stateSHA256[6] = 0x1f83d9ab;
                _stateSHA256[7] = 0x5be0cd19;
            }

            /* SHA256 block update operation. Continues an SHA message-digest
            operation, processing another message block, and updating the
            context.
            */
            public override unsafe void HashCore(byte[] partIn, int ibStart, int cbSize)
            {
                int bufferLen;
                int partInLen = cbSize;
                int partInBase = ibStart;

                /* Compute length of buffer */
                bufferLen = (int)(_count & 0x3f);

                /* Update number of bytes */
                _count += partInLen;

                fixed (uint* stateSHA256 = _stateSHA256)
                {
                    fixed (byte* buffer = _buffer)
                    {
                        fixed (uint* expandedBuffer = _W)
                        {
                            if ((bufferLen > 0) && (bufferLen + partInLen >= 64))
                            {
                                Buffer.BlockCopy(partIn, partInBase, _buffer, bufferLen, 64 - bufferLen);
                                partInBase += (64 - bufferLen);
                                partInLen -= (64 - bufferLen);
                                SHATransform(expandedBuffer, stateSHA256, buffer);
                                bufferLen = 0;
                            }

                            /* Copy input to temporary buffer and hash */
                            while (partInLen >= 64)
                            {
                                Buffer.BlockCopy(partIn, partInBase, _buffer, 0, 64);
                                partInBase += 64;
                                partInLen -= 64;
                                SHATransform(expandedBuffer, stateSHA256, buffer);
                            }

                            if (partInLen > 0)
                            {
                                Buffer.BlockCopy(partIn, partInBase, _buffer, bufferLen, partInLen);
                            }
                        }
                    }
                }
            }

            /* SHA256 finalization. Ends an SHA256 message-digest operation, writing
            the message digest.
            */
            public override byte[] HashFinal()
            {
                byte[] pad;
                int padLen;
                long bitCount;
                byte[] hash = new byte[32]; // HashSizeValue = 256

                /* Compute padding: 80 00 00 ... 00 00 <bit count>
                */

                padLen = 64 - (int)(_count & 0x3f);
                if (padLen <= 8)
                    padLen += 64;

                pad = new byte[padLen];
                pad[0] = 0x80;

                //  Convert count to bit count
                bitCount = _count * 8;

                pad[padLen - 8] = (byte)((bitCount >> 56) & 0xff);
                pad[padLen - 7] = (byte)((bitCount >> 48) & 0xff);
                pad[padLen - 6] = (byte)((bitCount >> 40) & 0xff);
                pad[padLen - 5] = (byte)((bitCount >> 32) & 0xff);
                pad[padLen - 4] = (byte)((bitCount >> 24) & 0xff);
                pad[padLen - 3] = (byte)((bitCount >> 16) & 0xff);
                pad[padLen - 2] = (byte)((bitCount >> 8) & 0xff);
                pad[padLen - 1] = (byte)((bitCount >> 0) & 0xff);

                /* Digest padding */
                HashCore(pad, 0, pad.Length);

                /* Store digest */
                SHAUtils.DWORDToBigEndian(hash, _stateSHA256, 8);

                return hash;
            }

            private static readonly uint[] _K = {
                0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5,
                0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
                0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3,
                0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
                0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc,
                0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
                0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7,
                0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
                0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13,
                0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
                0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3,
                0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
                0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5,
                0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
                0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208,
                0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
            };

            private static unsafe void SHATransform(uint* expandedBuffer, uint* state, byte* block)
            {
                uint a, b, c, d, e, f, h, g;
                uint aa, bb, cc, dd, ee, ff, hh, gg;
                uint T1;

                a = state[0];
                b = state[1];
                c = state[2];
                d = state[3];
                e = state[4];
                f = state[5];
                g = state[6];
                h = state[7];

                // fill in the first 16 bytes of W.
                SHAUtils.DWORDFromBigEndian(expandedBuffer, 16, block);
                SHA256Expand(expandedBuffer);

                /* Apply the SHA256 compression function */
                // We are trying to be smart here and avoid as many copies as we can
                // The perf gain with this method over the straightforward modify and shift
                // forward is >= 20%, so it's worth the pain
                for (int j = 0; j < 64;)
                {
                    T1 = h + Sigma_1(e) + Ch(e, f, g) + _K[j] + expandedBuffer[j];
                    ee = d + T1;
                    aa = T1 + Sigma_0(a) + Maj(a, b, c);
                    j++;

                    T1 = g + Sigma_1(ee) + Ch(ee, e, f) + _K[j] + expandedBuffer[j];
                    ff = c + T1;
                    bb = T1 + Sigma_0(aa) + Maj(aa, a, b);
                    j++;

                    T1 = f + Sigma_1(ff) + Ch(ff, ee, e) + _K[j] + expandedBuffer[j];
                    gg = b + T1;
                    cc = T1 + Sigma_0(bb) + Maj(bb, aa, a);
                    j++;

                    T1 = e + Sigma_1(gg) + Ch(gg, ff, ee) + _K[j] + expandedBuffer[j];
                    hh = a + T1;
                    dd = T1 + Sigma_0(cc) + Maj(cc, bb, aa);
                    j++;

                    T1 = ee + Sigma_1(hh) + Ch(hh, gg, ff) + _K[j] + expandedBuffer[j];
                    h = aa + T1;
                    d = T1 + Sigma_0(dd) + Maj(dd, cc, bb);
                    j++;

                    T1 = ff + Sigma_1(h) + Ch(h, hh, gg) + _K[j] + expandedBuffer[j];
                    g = bb + T1;
                    c = T1 + Sigma_0(d) + Maj(d, dd, cc);
                    j++;

                    T1 = gg + Sigma_1(g) + Ch(g, h, hh) + _K[j] + expandedBuffer[j];
                    f = cc + T1;
                    b = T1 + Sigma_0(c) + Maj(c, d, dd);
                    j++;

                    T1 = hh + Sigma_1(f) + Ch(f, g, h) + _K[j] + expandedBuffer[j];
                    e = dd + T1;
                    a = T1 + Sigma_0(b) + Maj(b, c, d);
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

            private static uint Ch(uint x, uint y, uint z)
            {
                return ((x & y) ^ ((x ^ 0xffffffff) & z));
            }

            private static uint Maj(uint x, uint y, uint z)
            {
                return ((x & y) ^ (x & z) ^ (y & z));
            }

            private static uint sigma_0(uint x)
            {
                return (RotateRight(x, 7) ^ RotateRight(x, 18) ^ (x >> 3));
            }

            private static uint sigma_1(uint x)
            {
                return (RotateRight(x, 17) ^ RotateRight(x, 19) ^ (x >> 10));
            }

            private static uint Sigma_0(uint x)
            {
                return (RotateRight(x, 2) ^ RotateRight(x, 13) ^ RotateRight(x, 22));
            }

            private static uint Sigma_1(uint x)
            {
                return (RotateRight(x, 6) ^ RotateRight(x, 11) ^ RotateRight(x, 25));
            }

            /* This function creates W_16,...,W_63 according to the formula
            W_j <- sigma_1(W_{j-2}) + W_{j-7} + sigma_0(W_{j-15}) + W_{j-16};
            */
            private static unsafe void SHA256Expand(uint* x)
            {
                for (int i = 16; i < 64; i++)
                {
                    x[i] = sigma_1(x[i - 2]) + x[i - 7] + sigma_0(x[i - 15]) + x[i - 16];
                }
            }
        }

        // ported from https://github.com/microsoft/referencesource/blob/a48449cb48a9a693903668a71449ac719b76867c/mscorlib/system/security/cryptography/sha384managed.cs
        private sealed class SHA384ManagedImplementation : SHAManagedImplementationBase
        {
            private byte[] _buffer;
            private ulong _count; // Number of bytes in the hashed message
            private ulong[] _stateSHA384;
            private ulong[] _W;

            public SHA384ManagedImplementation()
            {
                _stateSHA384 = new ulong[8];
                _buffer = new byte[128];
                _W = new ulong[80];

                InitializeState();
            }

            public override void Initialize()
            {
                InitializeState();

                // Zeroize potentially sensitive information.
                Array.Clear(_buffer, 0, _buffer.Length);
                Array.Clear(_W, 0, _W.Length);
            }

            private void InitializeState()
            {
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
            public override unsafe void HashCore(byte[] partIn, int ibStart, int cbSize)
            {
                int bufferLen;
                int partInLen = cbSize;
                int partInBase = ibStart;

                /* Compute length of buffer */
                bufferLen = (int)(_count & 0x7f);

                /* Update number of bytes */
                _count += (ulong)partInLen;

                fixed (ulong* stateSHA384 = _stateSHA384)
                {
                    fixed (byte* buffer = _buffer)
                    {
                        fixed (ulong* expandedBuffer = _W)
                        {
                            if ((bufferLen > 0) && (bufferLen + partInLen >= 128))
                            {
                                Buffer.BlockCopy(partIn, partInBase, _buffer, bufferLen, 128 - bufferLen);
                                partInBase += (128 - bufferLen);
                                partInLen -= (128 - bufferLen);
                                SHATransform(expandedBuffer, stateSHA384, buffer);
                                bufferLen = 0;
                            }

                            /* Copy input to temporary buffer and hash */
                            while (partInLen >= 128)
                            {
                                Buffer.BlockCopy(partIn, partInBase, _buffer, 0, 128);
                                partInBase += 128;
                                partInLen -= 128;
                                SHATransform(expandedBuffer, stateSHA384, buffer);
                            }

                            if (partInLen > 0)
                            {
                                Buffer.BlockCopy(partIn, partInBase, _buffer, bufferLen, partInLen);
                            }
                        }
                    }
                }
            }

            /* SHA384 finalization. Ends an SHA384 message-digest operation, writing
            the message digest.
            */
            public override byte[] HashFinal()
            {
                byte[] pad;
                int padLen;
                ulong bitCount;
                byte[] hash = new byte[48]; // HashSizeValue = 384

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
                pad[padLen - 8] = (byte)((bitCount >> 56) & 0xff);
                pad[padLen - 7] = (byte)((bitCount >> 48) & 0xff);
                pad[padLen - 6] = (byte)((bitCount >> 40) & 0xff);
                pad[padLen - 5] = (byte)((bitCount >> 32) & 0xff);
                pad[padLen - 4] = (byte)((bitCount >> 24) & 0xff);
                pad[padLen - 3] = (byte)((bitCount >> 16) & 0xff);
                pad[padLen - 2] = (byte)((bitCount >> 8) & 0xff);
                pad[padLen - 1] = (byte)((bitCount >> 0) & 0xff);

                /* Digest padding */
                HashCore(pad, 0, pad.Length);

                /* Store digest */
                SHAUtils.QuadWordToBigEndian(hash, _stateSHA384, 6);

                return hash;
            }

            private static readonly ulong[] _K = {
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

            private static unsafe void SHATransform(ulong* expandedBuffer, ulong* state, byte* block)
            {
                ulong a, b, c, d, e, f, g, h;
                ulong aa, bb, cc, dd, ee, ff, hh, gg;
                ulong T1;

                a = state[0];
                b = state[1];
                c = state[2];
                d = state[3];
                e = state[4];
                f = state[5];
                g = state[6];
                h = state[7];

                // fill in the first 16 blocks of W.
                SHAUtils.QuadWordFromBigEndian(expandedBuffer, 16, block);
                SHA384Expand(expandedBuffer);

                /* Apply the SHA384 compression function */
                // We are trying to be smart here and avoid as many copies as we can
                // The perf gain with this method over the straightforward modify and shift
                // forward is >= 20%, so it's worth the pain
                for (int j = 0; j < 80;)
                {
                    T1 = h + Sigma_1(e) + Ch(e, f, g) + _K[j] + expandedBuffer[j];
                    ee = d + T1;
                    aa = T1 + Sigma_0(a) + Maj(a, b, c);
                    j++;

                    T1 = g + Sigma_1(ee) + Ch(ee, e, f) + _K[j] + expandedBuffer[j];
                    ff = c + T1;
                    bb = T1 + Sigma_0(aa) + Maj(aa, a, b);
                    j++;

                    T1 = f + Sigma_1(ff) + Ch(ff, ee, e) + _K[j] + expandedBuffer[j];
                    gg = b + T1;
                    cc = T1 + Sigma_0(bb) + Maj(bb, aa, a);
                    j++;

                    T1 = e + Sigma_1(gg) + Ch(gg, ff, ee) + _K[j] + expandedBuffer[j];
                    hh = a + T1;
                    dd = T1 + Sigma_0(cc) + Maj(cc, bb, aa);
                    j++;

                    T1 = ee + Sigma_1(hh) + Ch(hh, gg, ff) + _K[j] + expandedBuffer[j];
                    h = aa + T1;
                    d = T1 + Sigma_0(dd) + Maj(dd, cc, bb);
                    j++;

                    T1 = ff + Sigma_1(h) + Ch(h, hh, gg) + _K[j] + expandedBuffer[j];
                    g = bb + T1;
                    c = T1 + Sigma_0(d) + Maj(d, dd, cc);
                    j++;

                    T1 = gg + Sigma_1(g) + Ch(g, h, hh) + _K[j] + expandedBuffer[j];
                    f = cc + T1;
                    b = T1 + Sigma_0(c) + Maj(c, d, dd);
                    j++;

                    T1 = hh + Sigma_1(f) + Ch(f, g, h) + _K[j] + expandedBuffer[j];
                    e = dd + T1;
                    a = T1 + Sigma_0(b) + Maj(b, c, d);
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

            private static ulong RotateRight(ulong x, int n)
            {
                return (((x) >> (n)) | ((x) << (64 - (n))));
            }

            private static ulong Ch(ulong x, ulong y, ulong z)
            {
                return ((x & y) ^ ((x ^ 0xffffffffffffffff) & z));
            }

            private static ulong Maj(ulong x, ulong y, ulong z)
            {
                return ((x & y) ^ (x & z) ^ (y & z));
            }

            private static ulong Sigma_0(ulong x)
            {
                return (RotateRight(x, 28) ^ RotateRight(x, 34) ^ RotateRight(x, 39));
            }

            private static ulong Sigma_1(ulong x)
            {
                return (RotateRight(x, 14) ^ RotateRight(x, 18) ^ RotateRight(x, 41));
            }

            private static ulong sigma_0(ulong x)
            {
                return (RotateRight(x, 1) ^ RotateRight(x, 8) ^ (x >> 7));
            }

            private static ulong sigma_1(ulong x)
            {
                return (RotateRight(x, 19) ^ RotateRight(x, 61) ^ (x >> 6));
            }

            /* This function creates W_16,...,W_79 according to the formula
            W_j <- sigma_1(W_{j-2}) + W_{j-7} + sigma_0(W_{j-15}) + W_{j-16};
            */
            private static unsafe void SHA384Expand(ulong* x)
            {
                for (int i = 16; i < 80; i++)
                {
                    x[i] = sigma_1(x[i - 2]) + x[i - 7] + sigma_0(x[i - 15]) + x[i - 16];
                }
            }
        }

        // ported from https://github.com/microsoft/referencesource/blob/a48449cb48a9a693903668a71449ac719b76867c/mscorlib/system/security/cryptography/sha512managed.cs
        private sealed class SHA512ManagedImplementation : SHAManagedImplementationBase
        {
            private byte[] _buffer;
            private ulong _count; // Number of bytes in the hashed message
            private ulong[] _stateSHA512;
            private ulong[] _W;

            public SHA512ManagedImplementation()
            {
                _stateSHA512 = new ulong[8];
                _buffer = new byte[128];
                _W = new ulong[80];

                InitializeState();
            }

            public override void Initialize()
            {
                InitializeState();

                // Zeroize potentially sensitive information.
                Array.Clear(_buffer, 0, _buffer.Length);
                Array.Clear(_W, 0, _W.Length);
            }

            private void InitializeState()
            {
                _count = 0;

                _stateSHA512[0] = 0x6a09e667f3bcc908;
                _stateSHA512[1] = 0xbb67ae8584caa73b;
                _stateSHA512[2] = 0x3c6ef372fe94f82b;
                _stateSHA512[3] = 0xa54ff53a5f1d36f1;
                _stateSHA512[4] = 0x510e527fade682d1;
                _stateSHA512[5] = 0x9b05688c2b3e6c1f;
                _stateSHA512[6] = 0x1f83d9abfb41bd6b;
                _stateSHA512[7] = 0x5be0cd19137e2179;
            }

            /* SHA512 block update operation. Continues an SHA message-digest
            operation, processing another message block, and updating the
            context.
            */
            public override unsafe void HashCore(byte[] partIn, int ibStart, int cbSize)
            {
                int bufferLen;
                int partInLen = cbSize;
                int partInBase = ibStart;

                /* Compute length of buffer */
                bufferLen = (int)(_count & 0x7f);

                /* Update number of bytes */
                _count += (ulong)partInLen;

                fixed (ulong* stateSHA512 = _stateSHA512)
                {
                    fixed (byte* buffer = _buffer)
                    {
                        fixed (ulong* expandedBuffer = _W)
                        {
                            if ((bufferLen > 0) && (bufferLen + partInLen >= 128))
                            {
                                Buffer.BlockCopy(partIn, partInBase, _buffer, bufferLen, 128 - bufferLen);
                                partInBase += (128 - bufferLen);
                                partInLen -= (128 - bufferLen);
                                SHATransform(expandedBuffer, stateSHA512, buffer);
                                bufferLen = 0;
                            }

                            /* Copy input to temporary buffer and hash */
                            while (partInLen >= 128)
                            {
                                Buffer.BlockCopy(partIn, partInBase, _buffer, 0, 128);
                                partInBase += 128;
                                partInLen -= 128;
                                SHATransform(expandedBuffer, stateSHA512, buffer);
                            }

                            if (partInLen > 0)
                            {
                                Buffer.BlockCopy(partIn, partInBase, _buffer, bufferLen, partInLen);
                            }
                        }
                    }
                }
            }

            /* SHA512 finalization. Ends an SHA512 message-digest operation, writing
            the message digest.
            */
            public override byte[] HashFinal()
            {
                byte[] pad;
                int padLen;
                ulong bitCount;
                byte[] hash = new byte[64]; // HashSizeValue = 512

                /* Compute padding: 80 00 00 ... 00 00 <bit count>
                */

                padLen = 128 - (int)(_count & 0x7f);
                if (padLen <= 16)
                    padLen += 128;

                pad = new byte[padLen];
                pad[0] = 0x80;

                //  Convert count to bit count
                bitCount = _count * 8;

                // If we ever have UInt128 for bitCount, then these need to be uncommented.
                // Note that C# only looks at the low 6 bits of the shift value for ulongs,
                // so >>0 and >>64 are equal!

                //pad[padLen-16] = (byte) ((bitCount >> 120) & 0xff);
                //pad[padLen-15] = (byte) ((bitCount >> 112) & 0xff);
                //pad[padLen-14] = (byte) ((bitCount >> 104) & 0xff);
                //pad[padLen-13] = (byte) ((bitCount >> 96) & 0xff);
                //pad[padLen-12] = (byte) ((bitCount >> 88) & 0xff);
                //pad[padLen-11] = (byte) ((bitCount >> 80) & 0xff);
                //pad[padLen-10] = (byte) ((bitCount >> 72) & 0xff);
                //pad[padLen-9] = (byte) ((bitCount >> 64) & 0xff);
                pad[padLen - 8] = (byte)((bitCount >> 56) & 0xff);
                pad[padLen - 7] = (byte)((bitCount >> 48) & 0xff);
                pad[padLen - 6] = (byte)((bitCount >> 40) & 0xff);
                pad[padLen - 5] = (byte)((bitCount >> 32) & 0xff);
                pad[padLen - 4] = (byte)((bitCount >> 24) & 0xff);
                pad[padLen - 3] = (byte)((bitCount >> 16) & 0xff);
                pad[padLen - 2] = (byte)((bitCount >> 8) & 0xff);
                pad[padLen - 1] = (byte)((bitCount >> 0) & 0xff);

                /* Digest padding */
                HashCore(pad, 0, pad.Length);

                /* Store digest */
                SHAUtils.QuadWordToBigEndian(hash, _stateSHA512, 8);

                return hash;
            }

            private static readonly ulong[] _K = {
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

            private static unsafe void SHATransform(ulong* expandedBuffer, ulong* state, byte* block)
            {
                ulong a, b, c, d, e, f, g, h;
                ulong aa, bb, cc, dd, ee, ff, hh, gg;
                ulong T1;

                a = state[0];
                b = state[1];
                c = state[2];
                d = state[3];
                e = state[4];
                f = state[5];
                g = state[6];
                h = state[7];

                // fill in the first 16 blocks of W.
                SHAUtils.QuadWordFromBigEndian(expandedBuffer, 16, block);
                SHA512Expand(expandedBuffer);

                /* Apply the SHA512 compression function */
                // We are trying to be smart here and avoid as many copies as we can
                // The perf gain with this method over the straightforward modify and shift
                // forward is >= 20%, so it's worth the pain
                for (int j = 0; j < 80;)
                {
                    T1 = h + Sigma_1(e) + Ch(e, f, g) + _K[j] + expandedBuffer[j];
                    ee = d + T1;
                    aa = T1 + Sigma_0(a) + Maj(a, b, c);
                    j++;

                    T1 = g + Sigma_1(ee) + Ch(ee, e, f) + _K[j] + expandedBuffer[j];
                    ff = c + T1;
                    bb = T1 + Sigma_0(aa) + Maj(aa, a, b);
                    j++;

                    T1 = f + Sigma_1(ff) + Ch(ff, ee, e) + _K[j] + expandedBuffer[j];
                    gg = b + T1;
                    cc = T1 + Sigma_0(bb) + Maj(bb, aa, a);
                    j++;

                    T1 = e + Sigma_1(gg) + Ch(gg, ff, ee) + _K[j] + expandedBuffer[j];
                    hh = a + T1;
                    dd = T1 + Sigma_0(cc) + Maj(cc, bb, aa);
                    j++;

                    T1 = ee + Sigma_1(hh) + Ch(hh, gg, ff) + _K[j] + expandedBuffer[j];
                    h = aa + T1;
                    d = T1 + Sigma_0(dd) + Maj(dd, cc, bb);
                    j++;

                    T1 = ff + Sigma_1(h) + Ch(h, hh, gg) + _K[j] + expandedBuffer[j];
                    g = bb + T1;
                    c = T1 + Sigma_0(d) + Maj(d, dd, cc);
                    j++;

                    T1 = gg + Sigma_1(g) + Ch(g, h, hh) + _K[j] + expandedBuffer[j];
                    f = cc + T1;
                    b = T1 + Sigma_0(c) + Maj(c, d, dd);
                    j++;

                    T1 = hh + Sigma_1(f) + Ch(f, g, h) + _K[j] + expandedBuffer[j];
                    e = dd + T1;
                    a = T1 + Sigma_0(b) + Maj(b, c, d);
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

            private static ulong Ch(ulong x, ulong y, ulong z)
            {
                return ((x & y) ^ ((x ^ 0xffffffffffffffff) & z));
            }

            private static ulong Maj(ulong x, ulong y, ulong z)
            {
                return ((x & y) ^ (x & z) ^ (y & z));
            }

            private static ulong Sigma_0(ulong x)
            {
                return (RotateRight(x, 28) ^ RotateRight(x, 34) ^ RotateRight(x, 39));
            }

            private static ulong Sigma_1(ulong x)
            {
                return (RotateRight(x, 14) ^ RotateRight(x, 18) ^ RotateRight(x, 41));
            }

            private static ulong sigma_0(ulong x)
            {
                return (RotateRight(x, 1) ^ RotateRight(x, 8) ^ (x >> 7));
            }

            private static ulong sigma_1(ulong x)
            {
                return (RotateRight(x, 19) ^ RotateRight(x, 61) ^ (x >> 6));
            }

            /* This function creates W_16,...,W_79 according to the formula
            W_j <- sigma_1(W_{j-2}) + W_{j-7} + sigma_0(W_{j-15}) + W_{j-16};
            */
            private static unsafe void SHA512Expand(ulong* x)
            {
                for (int i = 16; i < 80; i++)
                {
                    x[i] = sigma_1(x[i - 2]) + x[i - 7] + sigma_0(x[i - 15]) + x[i - 16];
                }
            }
        }

        // ported from https://github.com/microsoft/referencesource/blob/a48449cb48a9a693903668a71449ac719b76867c/mscorlib/system/security/cryptography/utils.cs
        private static class SHAUtils
        {
            // digits == number of DWORDs
            public static unsafe void DWORDFromBigEndian(uint* x, int digits, byte* block)
            {
                int i;
                int j;

                for (i = 0, j = 0; i < digits; i++, j += 4)
                    x[i] = (uint)((block[j] << 24) | (block[j + 1] << 16) | (block[j + 2] << 8) | block[j + 3]);
            }

            // encodes x (DWORD) into block (unsigned char), most significant byte first.
            // digits == number of DWORDs
            public static void DWORDToBigEndian(byte[] block, uint[] x, int digits)
            {
                int i;
                int j;

                for (i = 0, j = 0; i < digits; i++, j += 4)
                {
                    block[j] = (byte)((x[i] >> 24) & 0xff);
                    block[j + 1] = (byte)((x[i] >> 16) & 0xff);
                    block[j + 2] = (byte)((x[i] >> 8) & 0xff);
                    block[j + 3] = (byte)(x[i] & 0xff);
                }
            }

            // digits == number of QWORDs
            public static unsafe void QuadWordFromBigEndian(ulong* x, int digits, byte* block)
            {
                int i;
                int j;

                for (i = 0, j = 0; i < digits; i++, j += 8)
                    x[i] = (
                            (((ulong)block[j]) << 56) | (((ulong)block[j + 1]) << 48) |
                            (((ulong)block[j + 2]) << 40) | (((ulong)block[j + 3]) << 32) |
                            (((ulong)block[j + 4]) << 24) | (((ulong)block[j + 5]) << 16) |
                            (((ulong)block[j + 6]) << 8) | ((ulong)block[j + 7])
                            );
            }

            // encodes x (DWORD) into block (unsigned char), most significant byte first.
            // digits = number of QWORDS
            public static void QuadWordToBigEndian(byte[] block, ulong[] x, int digits)
            {
                int i;
                int j;

                for (i = 0, j = 0; i < digits; i++, j += 8)
                {
                    block[j] = (byte)((x[i] >> 56) & 0xff);
                    block[j + 1] = (byte)((x[i] >> 48) & 0xff);
                    block[j + 2] = (byte)((x[i] >> 40) & 0xff);
                    block[j + 3] = (byte)((x[i] >> 32) & 0xff);
                    block[j + 4] = (byte)((x[i] >> 24) & 0xff);
                    block[j + 5] = (byte)((x[i] >> 16) & 0xff);
                    block[j + 6] = (byte)((x[i] >> 8) & 0xff);
                    block[j + 7] = (byte)(x[i] & 0xff);
                }
            }
        }
    }
}
