// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs12;
using System.Security.Cryptography.Pkcs;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal abstract class UnixExportProvider : IExportPal
    {
        private static readonly Asn1Tag s_contextSpecific0 =
            new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);

        internal static readonly PbeParameters s_windowsPbe =
            new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 2000);

        protected ICertificatePalCore? _singleCertPal;
        protected X509Certificate2Collection? _certs;

        internal UnixExportProvider(ICertificatePalCore singleCertPal)
        {
            _singleCertPal = singleCertPal;
        }

        internal UnixExportProvider(X509Certificate2Collection certs)
        {
            _certs = certs;
        }

        public void Dispose()
        {
            // Don't dispose any of the resources, they're still owned by the caller.
            _singleCertPal = null;
            _certs = null;
        }

        protected abstract byte[] ExportPkcs7();

        protected abstract byte[] ExportPkcs8(ICertificatePalCore certificatePal, ReadOnlySpan<char> password);

        public byte[]? Export(X509ContentType contentType, SafePasswordHandle password)
        {
            Debug.Assert(password != null);
            switch (contentType)
            {
                case X509ContentType.Cert:
                    return ExportX509Der();
                case X509ContentType.Pfx:
                    return ExportPfx(password);
                case X509ContentType.Pkcs7:
                    return ExportPkcs7();
                case X509ContentType.SerializedCert:
                case X509ContentType.SerializedStore:
                    throw new PlatformNotSupportedException(SR.Cryptography_Unix_X509_SerializedExport);
                default:
                    throw new CryptographicException(SR.Cryptography_X509_InvalidContentType);
            }
        }

        private byte[]? ExportX509Der()
        {
            if (_singleCertPal != null)
            {
                return _singleCertPal.RawData;
            }

            // Windows/Desktop compatibility: Exporting a collection (or store) as
            // X509ContentType.Cert returns the equivalent of FirstOrDefault(),
            // so anything past _certs[0] is ignored, and an empty collection is
            // null (not an Exception)
            if (_certs!.Count == 0)
            {
                return null;
            }

            return _certs[0].RawData;
        }

        private byte[] ExportPfx(SafePasswordHandle password)
        {
            int certCount = 1;

            if (_singleCertPal == null)
            {
                Debug.Assert(_certs != null);
                certCount = _certs.Count;
            }

            CertBagAsn[] certBags = ArrayPool<CertBagAsn>.Shared.Rent(certCount);
            SafeBagAsn[] keyBags = ArrayPool<SafeBagAsn>.Shared.Rent(certCount);
            AttributeAsn[] certAttrs = ArrayPool<AttributeAsn>.Shared.Rent(certCount);
            certAttrs.AsSpan(0, certCount).Clear();

            AsnWriter tmpWriter = new AsnWriter(AsnEncodingRules.DER);
            ArraySegment<byte> encodedAuthSafe = default;

            bool gotRef = false;

            try
            {
                password.DangerousAddRef(ref gotRef);
                ReadOnlySpan<char> passwordSpan = password.DangerousGetSpan();

                int keyIdx = 0;
                int certIdx = 0;

                if (_singleCertPal != null)
                {
                    BuildBags(
                        _singleCertPal,
                        passwordSpan,
                        tmpWriter,
                        certBags,
                        certAttrs,
                        keyBags,
                        ref certIdx,
                        ref keyIdx);
                }
                else
                {
                    foreach (X509Certificate2 cert in _certs!)
                    {
                        BuildBags(
                            cert.Pal,
                            passwordSpan,
                            tmpWriter,
                            certBags,
                            certAttrs,
                            keyBags,
                            ref certIdx,
                            ref keyIdx);
                    }
                }

                encodedAuthSafe = EncodeAuthSafe(
                    tmpWriter,
                    keyBags,
                    keyIdx,
                    certBags,
                    certAttrs,
                    certIdx,
                    passwordSpan);

                return MacAndEncode(tmpWriter, encodedAuthSafe, passwordSpan);
            }
            finally
            {
                if (gotRef)
                {
                    password.DangerousRelease();
                }

                certAttrs.AsSpan(0, certCount).Clear();
                certBags.AsSpan(0, certCount).Clear();
                keyBags.AsSpan(0, certCount).Clear();
                ArrayPool<AttributeAsn>.Shared.Return(certAttrs);
                ArrayPool<CertBagAsn>.Shared.Return(certBags);
                ArrayPool<SafeBagAsn>.Shared.Return(keyBags);

                if (encodedAuthSafe.Array != null)
                {
                    CryptoPool.Return(encodedAuthSafe);
                }
            }
        }

        private void BuildBags(
            ICertificatePalCore certPal,
            ReadOnlySpan<char> passwordSpan,
            AsnWriter tmpWriter,
            CertBagAsn[] certBags,
            AttributeAsn[] certAttrs,
            SafeBagAsn[] keyBags,
            ref int certIdx,
            ref int keyIdx)
        {
            tmpWriter.WriteOctetString(certPal.RawData);

            certBags[certIdx] = new CertBagAsn
            {
                CertId = Oids.Pkcs12X509CertBagType,
                CertValue = tmpWriter.Encode(),
            };

            tmpWriter.Reset();

            if (certPal.HasPrivateKey)
            {
                byte[] attrBytes = new byte[6];
                attrBytes[0] = (byte)UniversalTagNumber.OctetString;
                attrBytes[1] = sizeof(int);
                MemoryMarshal.Write(attrBytes.AsSpan(2), in keyIdx);

                AttributeAsn attribute = new AttributeAsn
                {
                    AttrType = Oids.LocalKeyId,
                    AttrValues = new ReadOnlyMemory<byte>[]
                    {
                        attrBytes,
                    }
                };
                keyBags[keyIdx] = new SafeBagAsn
                {
                    BagId = Oids.Pkcs12ShroudedKeyBag,
                    BagValue = ExportPkcs8(certPal, passwordSpan),
                    BagAttributes = new[] { attribute }
                };

                // Reuse the attribute between the cert and the key.
                certAttrs[certIdx] = attribute;
                keyIdx++;
            }

            certIdx++;
        }

        private static unsafe ArraySegment<byte> EncodeAuthSafe(
            AsnWriter tmpWriter,
            SafeBagAsn[] keyBags,
            int keyCount,
            CertBagAsn[] certBags,
            AttributeAsn[] certAttrs,
            int certIdx,
            ReadOnlySpan<char> passwordSpan)
        {
            string? encryptionAlgorithmOid = null;
            bool certsIsPkcs12Encryption = false;
            string? certsHmacOid = null;

            ArraySegment<byte> encodedKeyContents = default;
            ArraySegment<byte> encodedCertContents = default;

            try
            {
                if (keyCount > 0)
                {
                    encodedKeyContents = EncodeKeys(tmpWriter, keyBags, keyCount);
                }

                Span<byte> salt = stackalloc byte[16];
                RandomNumberGenerator.Fill(salt);
                Span<byte> certContentsIv = stackalloc byte[8];

                if (certIdx > 0)
                {
                    encodedCertContents = EncodeCerts(
                        tmpWriter,
                        certBags,
                        certAttrs,
                        certIdx,
                        salt,
                        passwordSpan,
                        certContentsIv,
                        out certsHmacOid,
                        out encryptionAlgorithmOid,
                        out certsIsPkcs12Encryption);
                }

                return EncodeAuthSafe(
                    tmpWriter,
                    encodedKeyContents,
                    encodedCertContents,
                    certsIsPkcs12Encryption,
                    certsHmacOid!,
                    encryptionAlgorithmOid!,
                    salt,
                    certContentsIv);
            }
            finally
            {
                if (encodedCertContents.Array != null)
                {
                    CryptoPool.Return(encodedCertContents);
                }

                if (encodedKeyContents.Array != null)
                {
                    CryptoPool.Return(encodedKeyContents);
                }
            }
        }

        private static ArraySegment<byte> EncodeKeys(AsnWriter tmpWriter, SafeBagAsn[] keyBags, int keyCount)
        {
            Debug.Assert(tmpWriter.GetEncodedLength() == 0);

            using (tmpWriter.PushSequence())
            {
                for (int i = 0; i < keyCount; i++)
                {
                    keyBags[i].Encode(tmpWriter);
                }
            }

            int length = tmpWriter.GetEncodedLength();
            byte[] keyBuf = CryptoPool.Rent(length);

            if (!tmpWriter.TryEncode(keyBuf, out length))
            {
                Debug.Fail("TryEncode failed with a pre-allocated buffer");
                throw new InvalidOperationException();
            }

            // Explicitly clear the internal buffer before it goes out of scope.
            tmpWriter.Reset();

            return new ArraySegment<byte>(keyBuf, 0, length);
        }

        private static ArraySegment<byte> EncodeCerts(
            AsnWriter tmpWriter,
            CertBagAsn[] certBags,
            AttributeAsn[] certAttrs,
            int certCount,
            Span<byte> salt,
            ReadOnlySpan<char> passwordSpan,
            Span<byte> certContentsIv,
            out string hmacOid,
            out string encryptionAlgorithmOid,
            out bool isPkcs12)
        {
            Debug.Assert(tmpWriter.GetEncodedLength() == 0);
            tmpWriter.PushSequence();

            PasswordBasedEncryption.InitiateEncryption(
                s_windowsPbe,
                out SymmetricAlgorithm cipher,
                out hmacOid,
                out encryptionAlgorithmOid,
                out isPkcs12);

            using (cipher)
            {
                Debug.Assert(certContentsIv.Length * 8 == cipher.BlockSize);

                for (int i = certCount - 1; i >= 0; --i)
                {
                    // Manually write the SafeBagAsn
                    // https://tools.ietf.org/html/rfc7292#section-4.2
                    //
                    // SafeBag ::= SEQUENCE {
                    //   bagId          BAG-TYPE.&id ({PKCS12BagSet})
                    //   bagValue       [0] EXPLICIT BAG-TYPE.&Type({PKCS12BagSet}{@bagId}),
                    //   bagAttributes  SET OF PKCS12Attribute OPTIONAL
                    // }
                    tmpWriter.PushSequence();

                    tmpWriter.WriteObjectIdentifierForCrypto(Oids.Pkcs12CertBag);

                    tmpWriter.PushSequence(s_contextSpecific0);
                    certBags[i].Encode(tmpWriter);
                    tmpWriter.PopSequence(s_contextSpecific0);

                    if (certAttrs[i].AttrType != null)
                    {
                        tmpWriter.PushSetOf();
                        certAttrs[i].Encode(tmpWriter);
                        tmpWriter.PopSetOf();
                    }

                    tmpWriter.PopSequence();
                }

                tmpWriter.PopSequence();

                // The padding applied will add at most a block to the output,
                // so ask for contentsSpan.Length + the number of bytes in a cipher block.
                int cipherBlockBytes = cipher.BlockSize >> 3;
                int requestedSize = checked(tmpWriter.GetEncodedLength() + cipherBlockBytes);
                byte[] certContents = CryptoPool.Rent(requestedSize);

                int encryptedLength = PasswordBasedEncryption.Encrypt(
                    passwordSpan,
                    ReadOnlySpan<byte>.Empty,
                    cipher,
                    isPkcs12,
                    tmpWriter,
                    s_windowsPbe,
                    salt,
                    certContents,
                    certContentsIv);

                Debug.Assert(encryptedLength <= requestedSize);
                tmpWriter.Reset();

                return new ArraySegment<byte>(certContents, 0, encryptedLength);
            }
        }

        private static ArraySegment<byte> EncodeAuthSafe(
            AsnWriter tmpWriter,
            ReadOnlyMemory<byte> encodedKeyContents,
            ReadOnlyMemory<byte> encodedCertContents,
            bool isPkcs12,
            string hmacOid,
            string encryptionAlgorithmOid,
            Span<byte> salt,
            Span<byte> certContentsIv)
        {
            Debug.Assert(tmpWriter.GetEncodedLength() == 0);

            tmpWriter.PushSequence();

            if (!encodedKeyContents.IsEmpty)
            {
                tmpWriter.PushSequence();
                tmpWriter.WriteObjectIdentifier(Oids.Pkcs7Data);
                tmpWriter.PushSequence(s_contextSpecific0);

                ReadOnlySpan<byte> keyContents = encodedKeyContents.Span;
                tmpWriter.WriteOctetString(keyContents);

                tmpWriter.PopSequence(s_contextSpecific0);
                tmpWriter.PopSequence();
            }

            if (!encodedCertContents.IsEmpty)
            {
                tmpWriter.PushSequence();

                {
                    tmpWriter.WriteObjectIdentifier(Oids.Pkcs7Encrypted);

                    tmpWriter.PushSequence(s_contextSpecific0);
                    tmpWriter.PushSequence();

                    {
                        // No unprotected attributes: version 0 data
                        tmpWriter.WriteInteger(0);

                        tmpWriter.PushSequence();

                        {
                            tmpWriter.WriteObjectIdentifier(Oids.Pkcs7Data);

                            PasswordBasedEncryption.WritePbeAlgorithmIdentifier(
                                tmpWriter,
                                isPkcs12,
                                encryptionAlgorithmOid,
                                salt,
                                s_windowsPbe.IterationCount,
                                hmacOid,
                                certContentsIv);

                            tmpWriter.WriteOctetString(encodedCertContents.Span, s_contextSpecific0);
                            tmpWriter.PopSequence();
                        }

                        tmpWriter.PopSequence();
                        tmpWriter.PopSequence(s_contextSpecific0);
                    }

                    tmpWriter.PopSequence();
                }
            }

            tmpWriter.PopSequence();

            int authSafeLength = tmpWriter.GetEncodedLength();
            byte[] authSafe = CryptoPool.Rent(authSafeLength);

            if (!tmpWriter.TryEncode(authSafe, out authSafeLength))
            {
                Debug.Fail("TryEncode failed with a pre-allocated buffer");
                throw new InvalidOperationException();
            }

            tmpWriter.Reset();

            return new ArraySegment<byte>(authSafe, 0, authSafeLength);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "HMACSHA1 is required for compat with other platforms")]
        private static unsafe byte[] MacAndEncode(
            AsnWriter tmpWriter,
            ReadOnlyMemory<byte> encodedAuthSafe,
            ReadOnlySpan<char> passwordSpan)
        {
            Span<byte> macKey = stackalloc byte[HMACSHA1.HashSizeInBytes];
            Span<byte> macSalt = stackalloc byte[HMACSHA1.HashSizeInBytes];
            Span<byte> macSpan = stackalloc byte[HMACSHA1.HashSizeInBytes];
            RandomNumberGenerator.Fill(macSalt);

            Pkcs12Kdf.DeriveMacKey(
                passwordSpan,
                HashAlgorithmName.SHA1,
                s_windowsPbe.IterationCount,
                macSalt,
                macKey);

            int bytesWritten = HMACSHA1.HashData(macKey, encodedAuthSafe.Span, macSpan);

            if (bytesWritten != HMACSHA1.HashSizeInBytes)
            {
                Debug.Fail($"HMACSHA1.HashData wrote {bytesWritten} of {HMACSHA1.HashSizeInBytes} bytes");
                throw new CryptographicException();
            }

            CryptographicOperations.ZeroMemory(macKey);

            // https://tools.ietf.org/html/rfc7292#section-4
            //
            // PFX ::= SEQUENCE {
            //   version    INTEGER {v3(3)}(v3,...),
            //   authSafe   ContentInfo,
            //   macData    MacData OPTIONAL
            // }
            Debug.Assert(tmpWriter.GetEncodedLength() == 0);
            tmpWriter.PushSequence();

            tmpWriter.WriteInteger(3);

            tmpWriter.PushSequence();
            {
                tmpWriter.WriteObjectIdentifier(Oids.Pkcs7Data);

                tmpWriter.PushSequence(s_contextSpecific0);
                {
                    tmpWriter.WriteOctetString(encodedAuthSafe.Span);
                    tmpWriter.PopSequence(s_contextSpecific0);
                }

                tmpWriter.PopSequence();
            }

            // https://tools.ietf.org/html/rfc7292#section-4
            //
            // MacData ::= SEQUENCE {
            //   mac        DigestInfo,
            //   macSalt    OCTET STRING,
            //   iterations INTEGER DEFAULT 1
            //   -- Note: The default is for historical reasons and its use is
            //   -- deprecated.
            // }
            tmpWriter.PushSequence();
            {
                tmpWriter.PushSequence();
                {
                    tmpWriter.PushSequence();
                    {
                        tmpWriter.WriteObjectIdentifier(Oids.Sha1);
                        tmpWriter.PopSequence();
                    }

                    tmpWriter.WriteOctetString(macSpan);
                    tmpWriter.PopSequence();
                }

                tmpWriter.WriteOctetString(macSalt);
                tmpWriter.WriteInteger(s_windowsPbe.IterationCount);

                tmpWriter.PopSequence();
            }

            tmpWriter.PopSequence();
            return tmpWriter.Encode();
        }
    }
}
