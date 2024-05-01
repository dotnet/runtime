// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

#if BUILDING_PKCS
using Helpers = Internal.Cryptography.PkcsHelpers;
#endif

namespace System.Security.Cryptography.Asn1.Pkcs12
{
    internal partial struct PfxAsn
    {
        private const int MaxIterationWork = 300_000;
        private static ReadOnlySpan<char> EmptyPassword => ""; // don't use ReadOnlySpan<byte>.Empty because it will get confused with default.
        private static ReadOnlySpan<char> NullPassword => default;

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

#if NET
            Debug.Assert(expectedOutputSize <= 64); // SHA512 is the largest digest size we know about
            Span<byte> derived = stackalloc byte[expectedOutputSize];
#else
            byte[] derived = new byte[expectedOutputSize];
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

        internal ulong CountTotalIterations()
        {
            checked
            {
                ulong count = 0;

                // RFC 7292 section 4.1:
                //  the contentType field of authSafe shall be of type data
                //  or signedData.  The content field of the authSafe shall, either
                //  directly (data case) or indirectly (signedData case), contain a BER-
                //  encoded value of type AuthenticatedSafe.
                // We don't support authSafe that is signedData, so enforce that it's just data.
                if (AuthSafe.ContentType != Oids.Pkcs7Data)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                ReadOnlyMemory<byte> authSafeContents = Helpers.DecodeOctetStringAsMemory(AuthSafe.Content);
                AsnValueReader outerAuthSafe = new AsnValueReader(authSafeContents.Span, AsnEncodingRules.BER); // RFC 7292 PDU says BER
                AsnValueReader authSafeReader = outerAuthSafe.ReadSequence();
                outerAuthSafe.ThrowIfNotEmpty();

                bool hasSeenEncryptedInfo = false;

                while (authSafeReader.HasData)
                {
                    ContentInfoAsn.Decode(ref authSafeReader, authSafeContents, out ContentInfoAsn contentInfo);

                    ReadOnlyMemory<byte> contentData;
                    ArraySegment<byte>? rentedData = null;

                    try
                    {
                        if (contentInfo.ContentType != Oids.Pkcs7Data)
                        {
                            if (contentInfo.ContentType == Oids.Pkcs7Encrypted)
                            {
                                if (hasSeenEncryptedInfo)
                                {
                                    // We will process at most one encryptedData ContentInfo. This is the most typical scenario where
                                    // certificates are stored in an encryptedData ContentInfo, and keys are shrouded in a data ContentInfo.
                                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                                }

                                ArraySegment<byte> content = DecryptContentInfo(contentInfo, out uint iterations);
                                contentData = content;
                                rentedData = content;
                                hasSeenEncryptedInfo = true;
                                count += iterations;
                            }
                            else
                            {
                                // Not a common scenario. It's not data or encryptedData, so they need to go through the
                                // regular PKCS12 loader.
                                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                            }
                        }
                        else
                        {
                            contentData = Helpers.DecodeOctetStringAsMemory(contentInfo.Content);
                        }

                        AsnValueReader outerSafeBag = new AsnValueReader(contentData.Span, AsnEncodingRules.BER);
                        AsnValueReader safeBagReader = outerSafeBag.ReadSequence();
                        outerSafeBag.ThrowIfNotEmpty();

                        while (safeBagReader.HasData)
                        {
                            SafeBagAsn.Decode(ref safeBagReader, contentData, out SafeBagAsn bag);

                            // We only need to count iterations on PKCS8ShroudedKeyBag.
                            // * KeyBag is PKCS#8 PrivateKeyInfo and doesn't do iterations.
                            // * CertBag, either for x509Certificate or sdsiCertificate don't do iterations.
                            // * CRLBag doesn't do iterations.
                            // * SecretBag doesn't do iteations.
                            // * Nested SafeContents _can_ do iterations, but Windows ignores it. So we will ignore it too.
                            if (bag.BagId == Oids.Pkcs12ShroudedKeyBag)
                            {
                                AsnValueReader pkcs8ShroudedKeyReader = new AsnValueReader(bag.BagValue.Span, AsnEncodingRules.BER);
                                EncryptedPrivateKeyInfoAsn.Decode(
                                    ref pkcs8ShroudedKeyReader,
                                    bag.BagValue,
                                    out EncryptedPrivateKeyInfoAsn epki);

                                count += IterationsFromParameters(epki.EncryptionAlgorithm);
                            }
                        }
                    }
                    finally
                    {
                        if (rentedData.HasValue)
                        {
                            CryptoPool.Return(rentedData.Value);
                        }
                    }
                }

                if (MacData.HasValue)
                {
                    if (MacData.Value.IterationCount < 0)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    count += (uint)MacData.Value.IterationCount;
                }

                return count;
            }
        }

        private static ArraySegment<byte> DecryptContentInfo(ContentInfoAsn contentInfo, out uint iterations)
        {
            EncryptedDataAsn encryptedData = EncryptedDataAsn.Decode(contentInfo.Content, AsnEncodingRules.BER);

            if (encryptedData.Version != 0 && encryptedData.Version != 2)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // The encrypted contentInfo can only wrap a PKCS7 data.
            if (encryptedData.EncryptedContentInfo.ContentType != Oids.Pkcs7Data)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            if (!encryptedData.EncryptedContentInfo.EncryptedContent.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            iterations = IterationsFromParameters(encryptedData.EncryptedContentInfo.ContentEncryptionAlgorithm);

            // This encryptData is encrypted with more rounds than we are willing to process. Bail out of the whole thing.
            if (iterations > MaxIterationWork)
            {
                throw new X509IterationCountExceededException();
            }

            int encryptedValueLength = encryptedData.EncryptedContentInfo.EncryptedContent.Value.Length;
            byte[] destination = CryptoPool.Rent(encryptedValueLength);
            int written = 0;

            try
            {
                try
                {
                    written = PasswordBasedEncryption.Decrypt(
                        in encryptedData.EncryptedContentInfo.ContentEncryptionAlgorithm,
                        EmptyPassword,
                        default,
                        encryptedData.EncryptedContentInfo.EncryptedContent.Value.Span,
                        destination);

                    // When padding happens to be as expected (false-positive), we can detect gibberish and prevent unexpected failures later
                    // This extra check makes it so it's very unlikely we'll end up with false positive.
                    AsnValueReader outerSafeBag = new AsnValueReader(destination.AsSpan(0, written), AsnEncodingRules.BER);
                    AsnValueReader safeBagReader = outerSafeBag.ReadSequence();
                    outerSafeBag.ThrowIfNotEmpty();
                }
                catch
                {
                    // If empty password didn't work, try null password.
                    written = PasswordBasedEncryption.Decrypt(
                        in encryptedData.EncryptedContentInfo.ContentEncryptionAlgorithm,
                        NullPassword,
                        default,
                        encryptedData.EncryptedContentInfo.EncryptedContent.Value.Span,
                        destination);

                    AsnValueReader outerSafeBag = new AsnValueReader(destination.AsSpan(0, written), AsnEncodingRules.BER);
                    AsnValueReader safeBagReader = outerSafeBag.ReadSequence();
                    outerSafeBag.ThrowIfNotEmpty();
                }
            }
            finally
            {
                if (written == 0)
                {
                    // This means the decryption operation failed and destination could contain
                    // partial data. Clear it to be hygienic.
                    CryptographicOperations.ZeroMemory(destination);
                }
            }

            return new ArraySegment<byte>(destination, 0, written);
        }

        private static uint IterationsFromParameters(in AlgorithmIdentifierAsn algorithmIdentifier)
        {
            switch (algorithmIdentifier.Algorithm)
            {
                case Oids.PasswordBasedEncryptionScheme2:
                    if (!algorithmIdentifier.Parameters.HasValue)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    PBES2Params pbes2Params = PBES2Params.Decode(algorithmIdentifier.Parameters.Value, AsnEncodingRules.BER);

                    // PBES2 only defines PKBDF2 for now. See RFC 8018 A.4
                    if (pbes2Params.KeyDerivationFunc.Algorithm != Oids.Pbkdf2)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    if (!pbes2Params.KeyDerivationFunc.Parameters.HasValue)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    Pbkdf2Params pbkdf2Params = Pbkdf2Params.Decode(pbes2Params.KeyDerivationFunc.Parameters.Value, AsnEncodingRules.BER);

                    if (pbkdf2Params.IterationCount < 0)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    return (uint)pbkdf2Params.IterationCount;

                // PBES1
                case Oids.PbeWithMD5AndDESCBC:
                case Oids.PbeWithMD5AndRC2CBC:
                case Oids.PbeWithSha1AndDESCBC:
                case Oids.PbeWithSha1AndRC2CBC:
                case Oids.Pkcs12PbeWithShaAnd3Key3Des:
                case Oids.Pkcs12PbeWithShaAnd2Key3Des:
                case Oids.Pkcs12PbeWithShaAnd128BitRC2:
                case Oids.Pkcs12PbeWithShaAnd40BitRC2:
                    if (!algorithmIdentifier.Parameters.HasValue)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    PBEParameter pbeParameters = PBEParameter.Decode(
                        algorithmIdentifier.Parameters.Value,
                        AsnEncodingRules.BER);

                    if (pbeParameters.IterationCount < 0)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    return (uint)pbeParameters.IterationCount;

                default:
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }
    }
}
