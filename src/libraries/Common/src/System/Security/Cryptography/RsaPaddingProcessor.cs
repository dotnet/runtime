// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static class RsaPaddingProcessor
    {
        // DigestInfo header values taken from https://tools.ietf.org/html/rfc3447#section-9.2, Note 1.
        private static ReadOnlySpan<byte> DigestInfoMD5 => new byte[]
            {
                0x30, 0x20, 0x30, 0x0C, 0x06, 0x08, 0x2A, 0x86,
                0x48, 0x86, 0xF7, 0x0D, 0x02, 0x05, 0x05, 0x00,
                0x04, 0x10,
            };

        private static ReadOnlySpan<byte> DigestInfoSha1 => new byte[]
            {
                0x30, 0x21, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03,
                0x02, 0x1A, 0x05, 0x00, 0x04, 0x14,
            };

        private static ReadOnlySpan<byte> DigestInfoSha256 => new byte[]
            {
                0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, 0x04,
                0x20,
            };

        private static ReadOnlySpan<byte> DigestInfoSha384 => new byte[]
            {
                0x30, 0x41, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x02, 0x05, 0x00, 0x04,
                0x30,
            };

        private static ReadOnlySpan<byte> DigestInfoSha512 => new byte[]
            {
                0x30, 0x51, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x03, 0x05, 0x00, 0x04,
                0x40,
            };

        private static ReadOnlySpan<byte> DigestInfoSha3_256 => new byte[]
            {
                0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x08, 0x05, 0x00, 0x04,
                0x20,
            };

        private static ReadOnlySpan<byte> DigestInfoSha3_384 => new byte[]
            {
                0x30, 0x41, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x09, 0x05, 0x00, 0x04,
                0x30,
            };

        private static ReadOnlySpan<byte> DigestInfoSha3_512 => new byte[]
            {
                0x30, 0x51, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x0A, 0x05, 0x00, 0x04,
                0x40,
            };

        private static ReadOnlySpan<byte> EightZeros => new byte[8];

        private static ReadOnlySpan<byte> GetDigestInfoForAlgorithm(
            HashAlgorithmName hashAlgorithmName,
            out int digestLengthInBytes)
        {
            switch (hashAlgorithmName.Name)
            {
                case HashAlgorithmNames.MD5:
                    digestLengthInBytes = MD5.HashSizeInBytes;
                    return DigestInfoMD5;
                case HashAlgorithmNames.SHA1:
                    digestLengthInBytes = SHA1.HashSizeInBytes;
                    return DigestInfoSha1;
                case HashAlgorithmNames.SHA256:
                    digestLengthInBytes = SHA256.HashSizeInBytes;
                    return DigestInfoSha256;
                case HashAlgorithmNames.SHA384:
                    digestLengthInBytes = SHA384.HashSizeInBytes;
                    return DigestInfoSha384;
                case HashAlgorithmNames.SHA512:
                    digestLengthInBytes = SHA512.HashSizeInBytes;
                    return DigestInfoSha512;
                case HashAlgorithmNames.SHA3_256:
                    digestLengthInBytes = SHA3_256.HashSizeInBytes;
                    return DigestInfoSha3_256;
                case HashAlgorithmNames.SHA3_384:
                    digestLengthInBytes = SHA3_384.HashSizeInBytes;
                    return DigestInfoSha3_384;
                case HashAlgorithmNames.SHA3_512:
                    digestLengthInBytes = SHA3_512.HashSizeInBytes;
                    return DigestInfoSha3_512;
                default:
                    Debug.Fail("Unknown digest algorithm");
                    throw new CryptographicException();
            }
        }

        internal static int BytesRequiredForBitCount(int keySizeInBits)
        {
            return (int)(((uint)keySizeInBits + 7) / 8);
        }

        internal static int HashLength(HashAlgorithmName hashAlgorithmName)
        {
            GetDigestInfoForAlgorithm(hashAlgorithmName, out int hLen);
            return hLen;
        }

        internal static void PadPkcs1Encryption(
            ReadOnlySpan<byte> source,
            Span<byte> destination)
        {
            // https://tools.ietf.org/html/rfc3447#section-7.2.1

            int mLen = source.Length;
            int k = destination.Length;

            // 1. If mLen > k - 11, fail
            if (mLen > k - 11)
            {
                throw new CryptographicException(SR.Cryptography_KeyTooSmall);
            }

            // 2(b). EM is composed of 00 02 [PS] 00 [M]
            Span<byte> mInEM = destination.Slice(destination.Length - source.Length);
            Span<byte> ps = destination.Slice(2, destination.Length - source.Length - 3);
            destination[0] = 0;
            destination[1] = 2;
            destination[ps.Length + 2] = 0;

            // 2(a). Fill PS with random data from a CSPRNG, but no zero-values.
            RandomNumberGeneratorImplementation.FillNonZeroBytes(ps);

            source.CopyTo(mInEM);
        }

        internal static OperationStatus DepadPkcs1Encryption(
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            int primitive = DepadPkcs1Encryption(source);
            int primitiveSign = SignStretch(primitive);

            // Primitive is a positive length, or ~length to indicate
            // an error, so flip ~length to length if the high bit is set.
            int len = Choose(primitiveSign, ~primitive, primitive);
            int spaceRemain = destination.Length - len;
            int spaceRemainSign = SignStretch(spaceRemain);

            // len = clampHigh(len, destination.Length);
            len = Choose(spaceRemainSign, destination.Length, len);

            // ret = spaceRemain < 0 ? DestinationTooSmall : Done
            int ret = Choose(
                spaceRemainSign,
                (int)OperationStatus.DestinationTooSmall,
                (int)OperationStatus.Done);

            // ret = primitive < 0 ? InvalidData : ret;
            ret = Choose(primitiveSign, (int)OperationStatus.InvalidData, ret);

            // Write some number of bytes, regardless of the final return.
            source[^len..].CopyTo(destination);

            // bytesWritten = ret == Done ? len : 0;
            bytesWritten = Choose(CheckZero(ret), len, 0);
            return (OperationStatus)ret;
        }

        private static int DepadPkcs1Encryption(ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length > 11);
            ReadOnlySpan<byte> afterPadding = source.Slice(10);
            ReadOnlySpan<byte> noZeros = source.Slice(2, 8);

            // Find the first zero in noZeros, or -1 for no zeros.
            int zeroPos = BlindFindFirstZero(noZeros);

            // If zeroPos is negative, valid is -1, otherwise 0.
            int valid = SignStretch(zeroPos);

            // If there are no zeros in afterPadding then zeroPos is negative,
            // so negating the sign stretch is 0, which makes hasPos 0.
            // If there -was- a zero, sign stretching is 0, so negating it makes hasPos -1.
            zeroPos = BlindFindFirstZero(afterPadding);
            int hasLen = ~SignStretch(zeroPos);
            valid &= hasLen;

            // Check that the first two bytes are { 00 02 }
            valid &= CheckZero(source[0] | (source[1] ^ 0x02));

            int lenIfGood = afterPadding.Length - zeroPos - 1;
            // If there were no zeros, use the full after-min-padding segment.
            int lenIfBad = ~Choose(hasLen, lenIfGood, source.Length - 11);

            Debug.Assert(lenIfBad < 0);
            return Choose(valid, lenIfGood, lenIfBad);
        }

        private static int BlindFindFirstZero(ReadOnlySpan<byte> source)
        {
            // Any vectorization of this routine needs to use non-early termination,
            // and instructions that do not vary their completion time on the input.

            int pos = -1;

            for (int i = source.Length - 1; i >= 0; i--)
            {
                // pos = source[i] == 0 ? i : pos;
                int local = CheckZero(source[i]);
                pos = Choose(local, i, pos);
            }

            return pos;
        }

        private static int SignStretch(int value)
        {
            return value >> 31;
        }

        private static int Choose(int selector, int yes, int no)
        {
            Debug.Assert((selector | (selector - 1)) == -1);
            return (selector & yes) | (~selector & no);
        }

        private static int CheckZero(int value)
        {
            // For zero, ~value and value-1 are both all bits set (negative).
            // For positive values, ~value is negative and value-1 is positive.
            // For negative values except MinValue, ~value is positive and value-1 is negative.
            // For MinValue, ~value is positive and value-1 is also positive.
            // All together, the only thing that has negative & negative is 0, so stretch the sign bit.
            int mask = ~value & (value - 1);
            return SignStretch(mask);
        }

        internal static void PadPkcs1Signature(
            HashAlgorithmName hashAlgorithmName,
            ReadOnlySpan<byte> source,
            Span<byte> destination)
        {
            // https://tools.ietf.org/html/rfc3447#section-9.2

            // 1. H = Hash(M)
            // Done by the caller.

            // 2. Encode the DigestInfo value
            ReadOnlySpan<byte> digestInfoPrefix = GetDigestInfoForAlgorithm(hashAlgorithmName, out _);
            int expectedLength = digestInfoPrefix[^1];

            if (source.Length != expectedLength)
            {
                throw new CryptographicException(SR.Cryptography_SignHash_WrongSize);
            }

            int tLen = digestInfoPrefix.Length + expectedLength;

            // 3. If emLen < tLen + 11, fail
            if (destination.Length - 11 < tLen)
            {
                throw new CryptographicException(SR.Cryptography_KeyTooSmall);
            }

            // 4. Generate emLen - tLen - 3 bytes of 0xFF as "PS"
            int paddingLength = destination.Length - tLen - 3;

            // 5. EM = 0x00 || 0x01 || PS || 0x00 || T
            destination[0] = 0;
            destination[1] = 1;
            destination.Slice(2, paddingLength).Fill(0xFF);
            destination[paddingLength + 2] = 0;
            digestInfoPrefix.CopyTo(destination.Slice(paddingLength + 3));
            source.CopyTo(destination.Slice(paddingLength + 3 + digestInfoPrefix.Length));
        }

        internal static void PadOaep(
            HashAlgorithmName hashAlgorithmName,
            ReadOnlySpan<byte> source,
            Span<byte> destination)
        {
            // https://tools.ietf.org/html/rfc3447#section-7.1.1

            int hLen = HashLength(hashAlgorithmName);
            byte[]? dbMask = null;
            Span<byte> dbMaskSpan = Span<byte>.Empty;

            try
            {
                // Since the biggest known _hLen is 512/8 (64) and destination.Length is 0 or more,
                // this shouldn't underflow without something having severely gone wrong.
                int maxInput = checked(destination.Length - hLen - hLen - 2);

                // 1(a) does not apply, we do not allow custom label values.

                // 1(b)
                if (source.Length > maxInput)
                {
                    throw new CryptographicException(
                        SR.Format(SR.Cryptography_Encryption_MessageTooLong, maxInput));
                }

                // The final message (step 2(i)) will be
                // 0x00 || maskedSeed (hLen long) || maskedDB (rest of the buffer)
                Span<byte> seed = destination.Slice(1, hLen);
                Span<byte> db = destination.Slice(1 + hLen);

                using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithmName))
                {
                    Debug.Assert(hasher.HashLengthInBytes == hLen);

                    // DB = lHash || PS || 0x01 || M
                    Span<byte> lHash = db.Slice(0, hLen);
                    Span<byte> mDest = db.Slice(db.Length - source.Length);
                    Span<byte> ps = db.Slice(hLen, db.Length - hLen - 1 - mDest.Length);
                    Span<byte> psEnd = db.Slice(hLen + ps.Length, 1);

                    // 2(a) lHash = Hash(L), where L is the empty string.
                    if (!hasher.TryGetHashAndReset(lHash, out int hLen2) || hLen2 != hLen)
                    {
                        Debug.Fail("TryGetHashAndReset failed with exact-size destination");
                        throw new CryptographicException();
                    }

                    // 2(b) generate a padding string of all zeros equal to the amount of unused space.
                    ps.Clear();

                    // 2(c)
                    psEnd[0] = 0x01;

                    // still 2(c)
                    source.CopyTo(mDest);

                    // 2(d)
                    RandomNumberGenerator.Fill(seed);

                    // 2(e)
                    dbMask = CryptoPool.Rent(db.Length);
                    dbMaskSpan = new Span<byte>(dbMask, 0, db.Length);
                    Mgf1(hasher, seed, dbMaskSpan);

                    // 2(f)
                    Xor(db, dbMaskSpan);

                    // 2(g)
                    Span<byte> seedMask = stackalloc byte[hLen];
                    Mgf1(hasher, db, seedMask);

                    // 2(h)
                    Xor(seed, seedMask);

                    // 2(i)
                    destination[0] = 0;
                }
            }
            catch (Exception e) when (!(e is CryptographicException))
            {
                Debug.Fail("Bad exception produced from OAEP padding: " + e);
                throw new CryptographicException();
            }
            finally
            {
                if (dbMask != null)
                {
                    CryptographicOperations.ZeroMemory(dbMaskSpan);
                    CryptoPool.Return(dbMask, clearSize: 0);
                }
            }
        }

        internal static void EncodePss(HashAlgorithmName hashAlgorithmName, ReadOnlySpan<byte> mHash, Span<byte> destination, int keySize)
        {
            int hLen = HashLength(hashAlgorithmName);

            // https://tools.ietf.org/html/rfc3447#section-9.1.1
            int emBits = keySize - 1;
            int emLen = BytesRequiredForBitCount(emBits);

            if (mHash.Length != hLen)
            {
                throw new CryptographicException(SR.Cryptography_SignHash_WrongSize);
            }

            // In this implementation, sLen is restricted to the length of the input hash.
            int sLen = hLen;

            // 3.  if emLen < hLen + sLen + 2, encoding error.
            //
            // sLen = hLen in this implementation.

            if (emLen < 2 + hLen + sLen)
            {
                throw new CryptographicException(SR.Cryptography_KeyTooSmall);
            }

            // Set any leading bytes to zero, since that will be required for the pending
            // RSA operation.
            destination.Slice(0, destination.Length - emLen).Clear();

            // 12. Let EM = maskedDB || H || 0xbc (H has length hLen)
            Span<byte> em = destination.Slice(destination.Length - emLen, emLen);

            int dbLen = emLen - hLen - 1;

            Span<byte> db = em.Slice(0, dbLen);
            Span<byte> hDest = em.Slice(dbLen, hLen);
            em[emLen - 1] = 0xBC;

            byte[] dbMaskRented = CryptoPool.Rent(dbLen);
            Span<byte> dbMask = new Span<byte>(dbMaskRented, 0, dbLen);

            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithmName))
            {
                Debug.Assert(hasher.HashLengthInBytes == hLen);
                // 4. Generate a random salt of length sLen
                Span<byte> salt = stackalloc byte[sLen];
                RandomNumberGenerator.Fill(salt);

                // 5. Let M' = an octet string of 8 zeros concat mHash concat salt
                // 6. Let H = Hash(M')

                hasher.AppendData(EightZeros);
                hasher.AppendData(mHash);
                hasher.AppendData(salt);

                if (!hasher.TryGetHashAndReset(hDest, out int hLen2) || hLen2 != hLen)
                {
                    Debug.Fail("TryGetHashAndReset failed with exact-size destination");
                    throw new CryptographicException();
                }

                // 7. Generate PS as zero-valued bytes of length emLen - sLen - hLen - 2.
                // 8. Let DB = PS || 0x01 || salt
                int psLen = emLen - sLen - hLen - 2;
                db.Slice(0, psLen).Clear();
                db[psLen] = 0x01;
                salt.CopyTo(db.Slice(psLen + 1));

                // 9. Let dbMask = MGF(H, emLen - hLen - 1)
                Mgf1(hasher, hDest, dbMask);

                // 10. Let maskedDB = DB XOR dbMask
                Xor(db, dbMask);

                // 11. Set the "unused" bits in the leftmost byte of maskedDB to 0.
                int unusedBits = 8 * emLen - emBits;

                if (unusedBits != 0)
                {
                    byte mask = (byte)(0xFF >> unusedBits);
                    db[0] &= mask;
                }
            }

            CryptographicOperations.ZeroMemory(dbMask);
            CryptoPool.Return(dbMaskRented, clearSize: 0);
        }

        internal static bool VerifyPss(HashAlgorithmName hashAlgorithmName, ReadOnlySpan<byte> mHash, ReadOnlySpan<byte> em, int keySize)
        {
            int hLen = HashLength(hashAlgorithmName);

            // https://tools.ietf.org/html/rfc3447#section-9.1.2

            int emBits = keySize - 1;
            int emLen = BytesRequiredForBitCount(emBits);

            if (mHash.Length != hLen)
            {
                return false;
            }

            Debug.Assert(em.Length >= emLen);

            // In this implementation, sLen is restricted to hLen.
            int sLen = hLen;

            // 3. If emLen < hLen + sLen + 2, output "inconsistent" and stop.
            if (emLen < hLen + sLen + 2)
            {
                return false;
            }

            // 4. If the last byte is not 0xBC, output "inconsistent" and stop.
            if (em[em.Length - 1] != 0xBC)
            {
                return false;
            }

            // 5. maskedDB is the leftmost emLen - hLen -1 bytes, H is the next hLen bytes.
            int dbLen = emLen - hLen - 1;

            ReadOnlySpan<byte> maskedDb = em.Slice(0, dbLen);
            ReadOnlySpan<byte> h = em.Slice(dbLen, hLen);

            // 6. If the unused bits aren't zero, output "inconsistent" and stop.
            int unusedBits = 8 * emLen - emBits;
            byte usedBitsMask = (byte)(0xFF >> unusedBits);

            if ((maskedDb[0] & usedBitsMask) != maskedDb[0])
            {
                return false;
            }

            // 7. dbMask = MGF(H, emLen - hLen - 1)
            byte[] dbMaskRented = CryptoPool.Rent(maskedDb.Length);
            Span<byte> dbMask = new Span<byte>(dbMaskRented, 0, maskedDb.Length);

            try
            {
                using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithmName))
                {
                    Debug.Assert(hasher.HashLengthInBytes == hLen);

                    Mgf1(hasher, h, dbMask);

                    // 8. DB = maskedDB XOR dbMask
                    Xor(dbMask, maskedDb);

                    // 9. Set the unused bits of DB to 0
                    dbMask[0] &= usedBitsMask;

                    // 10 ("a"): If the emLen - hLen - sLen - 2 leftmost bytes are not 0,
                    // output "inconsistent" and stop.
                    //
                    // Since signature verification is a public key operation there's no need to
                    // use fixed time equality checking here.
                    for (int i = emLen - hLen - sLen - 2 - 1; i >= 0; --i)
                    {
                        if (dbMask[i] != 0)
                        {
                            return false;
                        }
                    }

                    // 10 ("b") If the octet at position emLen - hLen - sLen - 1 (under a 1-indexed scheme)
                    // is not 0x01, output "inconsistent" and stop.
                    if (dbMask[emLen - hLen - sLen - 2] != 0x01)
                    {
                        return false;
                    }

                    // 11. Let salt be the last sLen octets of DB.
                    ReadOnlySpan<byte> salt = dbMask.Slice(dbMask.Length - sLen);

                    // 12/13. Let H' = Hash(eight zeros || mHash || salt)
                    hasher.AppendData(EightZeros);
                    hasher.AppendData(mHash);
                    hasher.AppendData(salt);

                    Span<byte> hPrime = stackalloc byte[hLen];

                    if (!hasher.TryGetHashAndReset(hPrime, out int hLen2) || hLen2 != hLen)
                    {
                        Debug.Fail("TryGetHashAndReset failed with exact-size destination");
                        throw new CryptographicException();
                    }

                    // 14. If H = H' output "consistent". Otherwise, output "inconsistent"
                    //
                    // Since this is a public key operation, no need to provide fixed time
                    // checking.
                    return h.SequenceEqual(hPrime);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dbMask);
                CryptoPool.Return(dbMaskRented, clearSize: 0);
            }
        }

        // https://tools.ietf.org/html/rfc3447#appendix-B.2.1
        private static void Mgf1(IncrementalHash hasher, ReadOnlySpan<byte> mgfSeed, Span<byte> mask)
        {
            int hLen = hasher.HashLengthInBytes;
            Span<byte> writePtr = mask;
            int count = 0;
            Span<byte> bigEndianCount = stackalloc byte[sizeof(int)];

            while (writePtr.Length > 0)
            {
                hasher.AppendData(mgfSeed);
                BinaryPrimitives.WriteInt32BigEndian(bigEndianCount, count);
                hasher.AppendData(bigEndianCount);

                if (writePtr.Length >= hLen)
                {
                    if (!hasher.TryGetHashAndReset(writePtr, out int bytesWritten))
                    {
                        Debug.Fail($"TryGetHashAndReset failed with sufficient space");
                        throw new CryptographicException();
                    }

                    Debug.Assert(bytesWritten == hLen);
                    writePtr = writePtr.Slice(bytesWritten);
                }
                else
                {
                    Span<byte> tmp = stackalloc byte[hLen];

                    if (!hasher.TryGetHashAndReset(tmp, out int bytesWritten))
                    {
                        Debug.Fail($"TryGetHashAndReset failed with sufficient space");
                        throw new CryptographicException();
                    }

                    Debug.Assert(bytesWritten == hLen);
                    tmp.Slice(0, writePtr.Length).CopyTo(writePtr);
                    break;
                }

                count++;
            }
        }

        /// <summary>
        /// Bitwise XOR of <paramref name="b"/> into <paramref name="a"/>.
        /// </summary>
        private static void Xor(Span<byte> a, ReadOnlySpan<byte> b)
        {
            if (a.Length != b.Length)
            {
                throw new InvalidOperationException();
            }

            for (int i = 0; i < b.Length; i++)
            {
                a[i] ^= b[i];
            }
        }
    }
}
