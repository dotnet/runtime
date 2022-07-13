// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        private static ReadOnlySpan<byte> EightZeros => new byte[8];

        private static ReadOnlySpan<byte> GetDigestInfoForAlgorithm(
            HashAlgorithmName hashAlgorithmName,
            out int digestLengthInBytes)
        {
            if (hashAlgorithmName == HashAlgorithmName.MD5)
            {
#pragma warning disable CA1416 // Unsupported on Browser. We just want the const here.
                digestLengthInBytes = MD5.HashSizeInBytes;
                return DigestInfoMD5;
#pragma warning restore CA1416
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA1)
            {
                digestLengthInBytes = SHA1.HashSizeInBytes;
                return DigestInfoSha1;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA256)
            {
                digestLengthInBytes = SHA256.HashSizeInBytes;
                return DigestInfoSha256;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA384)
            {
                digestLengthInBytes = SHA384.HashSizeInBytes;
                return DigestInfoSha384;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA512)
            {
                digestLengthInBytes = SHA512.HashSizeInBytes;
                return DigestInfoSha512;
            }
            else
            {
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
            FillNonZeroBytes(ps);

            source.CopyTo(mInEM);
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

        internal static bool DepadOaep(
            HashAlgorithmName hashAlgorithmName,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            int hLen = HashLength(hashAlgorithmName);

            // https://tools.ietf.org/html/rfc3447#section-7.1.2
            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithmName))
            {
                Debug.Assert(hasher.HashLengthInBytes == hLen);

                Span<byte> lHash = stackalloc byte[hLen];

                if (!hasher.TryGetHashAndReset(lHash, out int hLen2) || hLen2 != hLen)
                {
                    Debug.Fail("TryGetHashAndReset failed with exact-size destination");
                    throw new CryptographicException();
                }

                int y = source[0];
                ReadOnlySpan<byte> maskedSeed = source.Slice(1, hLen);
                ReadOnlySpan<byte> maskedDB = source.Slice(1 + hLen);

                Span<byte> seed = stackalloc byte[hLen];
                // seedMask = MGF(maskedDB, hLen)
                Mgf1(hasher, maskedDB, seed);

                // seed = seedMask XOR maskedSeed
                Xor(seed, maskedSeed);

                byte[] tmp = CryptoPool.Rent(source.Length);

                try
                {
                    Span<byte> dbMask = new Span<byte>(tmp, 0, maskedDB.Length);
                    // dbMask = MGF(seed, k - hLen - 1)
                    Mgf1(hasher, seed, dbMask);

                    // DB = dbMask XOR maskedDB
                    Xor(dbMask, maskedDB);

                    ReadOnlySpan<byte> lHashPrime = dbMask.Slice(0, hLen);

                    int separatorPos = int.MaxValue;

                    for (int i = dbMask.Length - 1; i >= hLen; i--)
                    {
                        // if dbMask[i] is 1, val is 0. otherwise val is [01,FF]
                        byte dbMinus1 = (byte)(dbMask[i] - 1);
                        int val = dbMinus1;

                        // if val is 0: FFFFFFFF & FFFFFFFF => FFFFFFFF
                        // if val is any other byte value, val-1 will be in the range 00000000 to 000000FE,
                        // and so the high bit will not be set.
                        val = (~val & (val - 1)) >> 31;

                        // if val is 0: separator = (0 & i) | (~0 & separator) => separator
                        // else: separator = (~0 & i) | (0 & separator) => i
                        //
                        // Net result: non-branching "if (dbMask[i] == 1) separatorPos = i;"
                        separatorPos = (val & i) | (~val & separatorPos);
                    }

                    bool lHashMatches = CryptographicOperations.FixedTimeEquals(lHash, lHashPrime);
                    bool yIsZero = y == 0;
                    bool separatorMadeSense = separatorPos < dbMask.Length;

                    // This intentionally uses non-short-circuiting operations to hide the timing
                    // differential between the three failure cases
                    bool shouldContinue = lHashMatches & yIsZero & separatorMadeSense;

                    if (!shouldContinue)
                    {
                        throw new CryptographicException(SR.Cryptography_OAEP_Decryption_Failed);
                    }

                    Span<byte> message = dbMask.Slice(separatorPos + 1);

                    if (message.Length <= destination.Length)
                    {
                        message.CopyTo(destination);
                        bytesWritten = message.Length;
                        return true;
                    }
                    else
                    {
                        bytesWritten = 0;
                        return false;
                    }
                }
                finally
                {
                    CryptoPool.Return(tmp, source.Length);
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

        // This is a copy of RandomNumberGeneratorImplementation.GetNonZeroBytes, but adapted
        // to the object-less RandomNumberGenerator.Fill.
        private static void FillNonZeroBytes(Span<byte> data)
        {
            while (data.Length > 0)
            {
                // Fill the remaining portion of the span with random bytes.
                RandomNumberGenerator.Fill(data);

                // Find the first zero in the remaining portion.
                int indexOfFirst0Byte = data.Length;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == 0)
                    {
                        indexOfFirst0Byte = i;
                        break;
                    }
                }

                // If there were any zeros, shift down all non-zeros.
                for (int i = indexOfFirst0Byte + 1; i < data.Length; i++)
                {
                    if (data[i] != 0)
                    {
                        data[indexOfFirst0Byte++] = data[i];
                    }
                }

                // Request new random bytes if necessary; dont re-use
                // existing bytes since they were shifted down.
                data = data.Slice(indexOfFirst0Byte);
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
