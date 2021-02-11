// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.Pkcs;

namespace System.Security.Cryptography.Asn1.Pkcs12
{
    internal partial struct PfxAsn
    {
        internal bool VerifyMac(
            ReadOnlySpan<char> macPassword,
            ReadOnlySpan<byte> authSafeContents)
        {
            Debug.Assert(MacData.HasValue);

            HashAlgorithmName hashAlgorithm;
            int expectedOutputSize;

            string algorithmValue = MacData.Value.Mac.DigestAlgorithm.Algorithm;

            switch (algorithmValue)
            {
                case Oids.Md5:
                    expectedOutputSize = 128 >> 3;
                    hashAlgorithm = HashAlgorithmName.MD5;
                    break;
                case Oids.Sha1:
                    expectedOutputSize = 160 >> 3;
                    hashAlgorithm = HashAlgorithmName.SHA1;
                    break;
                case Oids.Sha256:
                    expectedOutputSize = 256 >> 3;
                    hashAlgorithm = HashAlgorithmName.SHA256;
                    break;
                case Oids.Sha384:
                    expectedOutputSize = 384 >> 3;
                    hashAlgorithm = HashAlgorithmName.SHA384;
                    break;
                case Oids.Sha512:
                    expectedOutputSize = 512 >> 3;
                    hashAlgorithm = HashAlgorithmName.SHA512;
                    break;
                default:
                    throw new CryptographicException(
                        SR.Format(SR.Cryptography_UnknownHashAlgorithm, algorithmValue));
            }

            if (MacData.Value.Mac.Digest.Length != expectedOutputSize)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

#if NETFRAMEWORK || NETCOREAPP3_0 || NETSTANDARD
            byte[] derived = new byte[expectedOutputSize];
#else
            Debug.Assert(expectedOutputSize <= 64); // SHA512 is the largest digest size we know about
            Span<byte> derived = stackalloc byte[expectedOutputSize];
#endif


            int iterationCount =
                PasswordBasedEncryption.NormalizeIterationCount(MacData.Value.IterationCount);

            Pkcs12Kdf.DeriveMacKey(
                macPassword,
                hashAlgorithm,
                iterationCount,
                MacData.Value.MacSalt.Span,
                derived);

            using (IncrementalHash hmac = IncrementalHash.CreateHMAC(hashAlgorithm, derived))
            {
                hmac.AppendData(authSafeContents);

                if (!hmac.TryGetHashAndReset(derived, out int bytesWritten) || bytesWritten != expectedOutputSize)
                {
                    Debug.Fail($"TryGetHashAndReset wrote {bytesWritten} bytes when {expectedOutputSize} was expected");
                    throw new CryptographicException();
                }

                return CryptographicOperations.FixedTimeEquals(
                    derived,
                    MacData.Value.Mac.Digest.Span);
            }
        }
    }
}
