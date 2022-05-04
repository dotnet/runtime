// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal sealed partial class ManagedPkcsPal : PkcsPal
    {
        internal sealed class ManagedDecryptorPal : DecryptorPal
        {
            private readonly byte[] _dataCopy;
            private EnvelopedDataAsn _envelopedData;

            public ManagedDecryptorPal(
                byte[] dataCopy,
                EnvelopedDataAsn envelopedDataAsn,
                RecipientInfoCollection recipientInfos)
                : base(recipientInfos)
            {
                _dataCopy = dataCopy;
                _envelopedData = envelopedDataAsn;
            }

            public override unsafe ContentInfo? TryDecrypt(
                RecipientInfo recipientInfo,
                X509Certificate2? cert,
                AsymmetricAlgorithm? privateKey,
                X509Certificate2Collection originatorCerts,
                X509Certificate2Collection extraStore,
                out Exception? exception)
            {
                // When encryptedContent is null Windows seems to decrypt the CEK first,
                // then return a 0 byte answer.

                Debug.Assert((cert != null) ^ (privateKey != null));

                if (recipientInfo.Pal is ManagedKeyTransPal ktri)
                {
                    RSA? key = privateKey as RSA;

                    if (privateKey != null && key == null)
                    {
                        exception = new CryptographicException(SR.Cryptography_Cms_Ktri_RSARequired);
                        return null;
                    }

                    byte[]? cek = ktri.DecryptCek(cert, key, out exception);
                    // Pin CEK to prevent it from getting copied during heap compaction.
                    fixed (byte* pinnedCek = cek)
                    {
                        try
                        {
                            if (exception != null)
                            {
                                return null;
                            }

                            return TryDecryptCore(
                                cek!,
                                _envelopedData.EncryptedContentInfo.ContentType,
                                _envelopedData.EncryptedContentInfo.EncryptedContent,
                                _envelopedData.EncryptedContentInfo.ContentEncryptionAlgorithm,
                                out exception);
                        }
                        finally
                        {
                            if (cek != null)
                            {
                                Array.Clear(cek, 0, cek.Length);
                            }
                        }
                    }
                }
                else
                {
                    exception = new CryptographicException(
                        SR.Cryptography_Cms_RecipientType_NotSupported,
                        recipientInfo.Type.ToString());

                    return null;
                }
            }

            public static unsafe ContentInfo? TryDecryptCore(
                byte[] cek,
                string contentType,
                ReadOnlyMemory<byte>? content,
                AlgorithmIdentifierAsn contentEncryptionAlgorithm,
                out Exception? exception)
            {
                if (content == null)
                {
                    exception = null;

                    return new ContentInfo(
                        new Oid(contentType),
                        Array.Empty<byte>());
                }

                byte[]? decrypted = DecryptContent(content.Value, cek, contentEncryptionAlgorithm, out exception);

                if (exception != null)
                {
                    return null;
                }

                if (contentType != Oids.Pkcs7Data)
                {
                    decrypted = GetAsnSequenceWithContentNoValidation(decrypted);
                }

                exception = null;
                return new ContentInfo(
                    new Oid(contentType),
                    decrypted!);
            }

            private static byte[] GetAsnSequenceWithContentNoValidation(ReadOnlySpan<byte> content)
            {
                AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);

                // Content may be invalid ASN.1 data.
                // We will encode it as octet string to bypass validation
                writer.WriteOctetString(content);
                byte[] encoded = writer.Encode();

                // and replace octet string tag (0x04) with sequence tag (0x30 or constructed 0x10)
                Debug.Assert(encoded[0] == 0x04);
                encoded[0] = 0x30;

                return encoded;
            }

            private static byte[]? DecryptContent(
                ReadOnlyMemory<byte> encryptedContent,
                byte[] cek,
                AlgorithmIdentifierAsn contentEncryptionAlgorithm,
                out Exception? exception)
            {
                exception = null;
                int encryptedContentLength = encryptedContent.Length;
                byte[]? encryptedContentArray = CryptoPool.Rent(encryptedContentLength);

                try
                {
                    encryptedContent.CopyTo(encryptedContentArray);

                    using (SymmetricAlgorithm alg = OpenAlgorithm(contentEncryptionAlgorithm))
                    {
                        ICryptoTransform decryptor;

                        try
                        {
                            decryptor = alg.CreateDecryptor(cek, alg.IV);
                        }
                        catch (ArgumentException ae)
                        {
                            // Decrypting or deriving the symmetric key with the wrong key may still succeed
                            // but produce a symmetric key that is not the correct length.
                            throw new CryptographicException(SR.Cryptography_Cms_InvalidSymmetricKey, ae);
                        }

                        using (decryptor)
                        {
                            // If we extend this library to accept additional algorithm providers
                            // then a different array pool needs to be used.
                            Debug.Assert(alg.GetType().Assembly == typeof(Aes).Assembly);

                            return decryptor.OneShot(
                                encryptedContentArray,
                                0,
                                encryptedContentLength);
                        }
                    }
                }
                catch (CryptographicException e)
                {
                    exception = e;
                    return null;
                }
                finally
                {
                    CryptoPool.Return(encryptedContentArray, encryptedContentLength);
                }
            }

            public override void Dispose()
            {
            }
        }
    }
}
