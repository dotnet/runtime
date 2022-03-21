// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    internal static class ECDiffieHellmanDerivation
    {
        /// <summary>
        /// Derive the raw ECDH value into <paramref name="hasher"/>, if present, otherwise returning the value.
        /// </summary>
        internal delegate byte[]? DeriveSecretAgreement(ECDiffieHellmanPublicKey otherPartyPublicKey, IncrementalHash? hasher);

        internal static byte[] DeriveKeyFromHash(
            ECDiffieHellmanPublicKey otherPartyPublicKey,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> secretPrepend,
            ReadOnlySpan<byte> secretAppend,
            DeriveSecretAgreement deriveSecretAgreement)
        {
            Debug.Assert(otherPartyPublicKey != null);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            using (IncrementalHash hash = IncrementalHash.CreateHash(hashAlgorithm))
            {
                hash.AppendData(secretPrepend);

                byte[]? secretAgreement = deriveSecretAgreement(otherPartyPublicKey, hash);
                // We want the side effect, and it should not have returned the answer.
                Debug.Assert(secretAgreement == null);

                hash.AppendData(secretAppend);

                return hash.GetHashAndReset();
            }
        }

        internal static unsafe byte[] DeriveKeyFromHmac(
            ECDiffieHellmanPublicKey otherPartyPublicKey,
            HashAlgorithmName hashAlgorithm,
            byte[]? hmacKey,
            ReadOnlySpan<byte> secretPrepend,
            ReadOnlySpan<byte> secretAppend,
            DeriveSecretAgreement deriveSecretAgreement)
        {
            Debug.Assert(otherPartyPublicKey != null);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            // If an hmac key is provided then calculate
            // HMAC(hmacKey, prepend || derived || append)
            //
            // Otherwise, calculate
            // HMAC(derived, prepend || derived || append)

            bool useSecretAsKey = hmacKey == null;

            if (useSecretAsKey)
            {
                hmacKey = deriveSecretAgreement(otherPartyPublicKey, null);
            }
            Debug.Assert(hmacKey != null);

            // Reduce the likelihood of the value getting copied during heap compaction.
            fixed (byte* pinnedHmacKey = hmacKey)
            {
                try
                {
                    using (IncrementalHash hash = IncrementalHash.CreateHMAC(hashAlgorithm, hmacKey))
                    {
                        hash.AppendData(secretPrepend);

                        if (useSecretAsKey)
                        {
                            hash.AppendData(hmacKey);
                        }
                        else
                        {
                            byte[]? secretAgreement = deriveSecretAgreement(otherPartyPublicKey, hash);
                            // We want the side effect, and it should not have returned the answer.
                            Debug.Assert(secretAgreement == null);
                        }

                        hash.AppendData(secretAppend);
                        return hash.GetHashAndReset();
                    }
                }
                finally
                {
                    // If useSecretAsKey is false then hmacKey is owned by the caller, not ours to clear.
                    if (useSecretAsKey)
                    {
                        Array.Clear(hmacKey);
                    }
                }
            }
        }

        internal static unsafe byte[] DeriveKeyTls(
            ECDiffieHellmanPublicKey otherPartyPublicKey,
            ReadOnlySpan<byte> prfLabel,
            ReadOnlySpan<byte> prfSeed,
            DeriveSecretAgreement deriveSecretAgreement)
        {
            Debug.Assert(otherPartyPublicKey != null);

            if (prfSeed.Length != 64)
            {
                throw new CryptographicException(SR.Cryptography_TlsRequires64ByteSeed);
            }

            // Windows produces a 48-byte output, so that's what we do, too.
            byte[] ret = new byte[48];

            const int Sha1Size = 20;
            const int Md5Size = 16;

            byte[]? secretAgreement = deriveSecretAgreement(otherPartyPublicKey, null);
            Debug.Assert(secretAgreement != null);

            // Reduce the likelihood of the value getting copied during heap compaction.
            fixed (byte* pinnedSecretAgreement = secretAgreement)
            {
                try
                {
                    // https://tools.ietf.org/html/rfc4346#section-5
                    //
                    //    S1 and S2 are the two halves of the secret, and each is the same
                    //    length.  S1 is taken from the first half of the secret, S2 from the
                    //    second half.  Their length is created by rounding up the length of
                    //    the overall secret, divided by two; thus, if the original secret is
                    //    an odd number of bytes long, the last byte of S1 will be the same as
                    //    the first byte of S2.
                    //
                    int half = secretAgreement.Length / 2;
                    int odd = secretAgreement.Length & 1;

                    // PRF(secret, label, seed) = P_MD5(S1, label + seed) XOR
                    //                            P_SHA-1(S2, label + seed);

                    PHash(
                        HashAlgorithmName.MD5,
                        new ReadOnlySpan<byte>(secretAgreement, 0, half + odd),
                        prfLabel,
                        prfSeed,
                        Md5Size,
                        ret);

                    Span<byte> part2 = stackalloc byte[ret.Length];

                    PHash(
                        HashAlgorithmName.SHA1,
                        new ReadOnlySpan<byte>(secretAgreement, half, half + odd),
                        prfLabel,
                        prfSeed,
                        Sha1Size,
                        part2);

                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] ^= part2[i];
                    }

                    return ret;
                }
                finally
                {
                    Array.Clear(secretAgreement);
                }
            }
        }

        private static unsafe void PHash(
            HashAlgorithmName algorithmName,
            ReadOnlySpan<byte> secret,
            ReadOnlySpan<byte> prfLabel,
            ReadOnlySpan<byte> prfSeed,
            int hashOutputSize,
            Span<byte> ret)
        {
            // https://tools.ietf.org/html/rfc4346#section-5
            //
            // P_hash(secret, seed) = HMAC_hash(secret, A(1) + seed) +
            //                        HMAC_hash(secret, A(2) + seed) +
            //                        HMAC_hash(secret, A(3) + seed) + ...
            //
            // A(0) = seed
            // A(i) = HMAC_hash(secret, A(i-1))
            //
            // This is called via PRF, which turns (label || seed) into seed.
            Span<byte> retSpan = ret;

            using (IncrementalHash hasher = IncrementalHash.CreateHMAC(algorithmName, secret))
            {
                Span<byte> a = stackalloc byte[hashOutputSize];
                Span<byte> p = stackalloc byte[hashOutputSize];

                // A(1)
                hasher.AppendData(prfLabel);
                hasher.AppendData(prfSeed);

                if (!hasher.TryGetHashAndReset(a, out int bytesWritten) || bytesWritten != hashOutputSize)
                {
                    throw new CryptographicException();
                }

                while (true)
                {
                    // HMAC_hash(secret, A(i) || seed) => p
                    hasher.AppendData(a);
                    hasher.AppendData(prfLabel);
                    hasher.AppendData(prfSeed);

                    if (!hasher.TryGetHashAndReset(p, out bytesWritten) || bytesWritten != hashOutputSize)
                    {
                        throw new CryptographicException();
                    }

                    int len = Math.Min(p.Length, retSpan.Length);

                    p.Slice(0, len).CopyTo(retSpan);
                    retSpan = retSpan.Slice(len);

                    if (retSpan.Length == 0)
                    {
                        return;
                    }

                    // Build the next A(i)
                    hasher.AppendData(a);

                    if (!hasher.TryGetHashAndReset(a, out bytesWritten) || bytesWritten != hashOutputSize)
                    {
                        throw new CryptographicException();
                    }
                }
            }
        }
    }
}
