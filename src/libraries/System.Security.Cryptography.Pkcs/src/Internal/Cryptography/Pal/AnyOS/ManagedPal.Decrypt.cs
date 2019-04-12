// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
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
            private byte[] _dataCopy;
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

            public override unsafe ContentInfo TryDecrypt(
                RecipientInfo recipientInfo,
                X509Certificate2 cert,
                AsymmetricAlgorithm privateKey,
                X509Certificate2Collection originatorCerts,
                X509Certificate2Collection extraStore,
                out Exception exception)
            {
                // When encryptedContent is null Windows seems to decrypt the CEK first,
                // then return a 0 byte answer.

                Debug.Assert((cert != null) ^ (privateKey != null));

                if (recipientInfo.Pal is ManagedKeyTransPal ktri)
                {
                    RSA key = privateKey as RSA;

                    if (privateKey != null && key == null)
                    {
                        exception = new CryptographicException(SR.Cryptography_Cms_Ktri_RSARequired);
                        return null;
                    }

                    byte[] cek = ktri.DecryptCek(cert, key, out exception);
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
                                cek,
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

            public static unsafe ContentInfo TryDecryptCore(
                byte[] cek,
                string contentType,
                ReadOnlyMemory<byte>? content,
                AlgorithmIdentifierAsn contentEncryptionAlgorithm,
                out Exception exception)
            {
                if (content == null)
                {
                    exception = null;

                    return new ContentInfo(
                        new Oid(contentType),
                        Array.Empty<byte>());
                }

                byte[] decrypted = DecryptContent(content.Value, cek, contentEncryptionAlgorithm, out exception);

                if (exception != null)
                {
                    return null;
                }

                // Compat: Previous versions of the managed PAL encryptor would wrap the contents in an octet stream
                // which is not correct and is incompatible with other CMS readers. To maintain compatibility with
                // existing CMS that have the incorrect wrapping, we attempt to remove it. 
                if (contentType == Oids.Pkcs7Data)
                {
                    byte[] tmp = null;

                    try
                    {
                        AsnReader reader = new AsnReader(decrypted, AsnEncodingRules.BER);

                        if (reader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> contents))
                        {
                            decrypted = contents.ToArray();
                        }
                        else
                        {
                            tmp = ArrayPool<byte>.Shared.Rent(decrypted.Length);

                            if (reader.TryCopyOctetStringBytes(tmp, out int written))
                            {
                                Span<byte> innerContents = new Span<byte>(tmp, 0, written);
                                decrypted = innerContents.ToArray();
                                innerContents.Clear();
                            }
                            else
                            {
                                Debug.Fail("Octet string grew during copy");
                                // If this happens (which requires decrypted was overwritten, which
                                // shouldn't be possible), just leave decrypted alone.
                            }
                        }
                    }
                    catch (CryptographicException)
                    {
                    }
                    finally
                    {
                        if (tmp != null)
                        {
                            // Already cleared
                            ArrayPool<byte>.Shared.Return(tmp);
                        }
                    }
                }
                else
                {
                    decrypted = GetAsnSequenceWithContentNoValidation(decrypted);
                }

                exception = null;
                return new ContentInfo(
                    new Oid(contentType),
                    decrypted);
            }

            private static byte[] GetAsnSequenceWithContentNoValidation(ReadOnlySpan<byte> content)
            {
                using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
                {
                    // Content may be invalid ASN.1 data.
                    // We will encode it as octet string to bypass validation
                    writer.WriteOctetString(content);
                    byte[] encoded = writer.Encode();

                    // and replace octet string tag (0x04) with sequence tag (0x30 or constructed 0x10)
                    Debug.Assert(encoded[0] == 0x04);
                    encoded[0] = 0x30;

                    return encoded;
                }
            }

            private static byte[] DecryptContent(
                ReadOnlyMemory<byte> encryptedContent,
                byte[] cek,
                AlgorithmIdentifierAsn contentEncryptionAlgorithm,
                out Exception exception)
            {
                exception = null;
                int encryptedContentLength = encryptedContent.Length;
                byte[] encryptedContentArray = ArrayPool<byte>.Shared.Rent(encryptedContentLength);

                try
                {
                    encryptedContent.CopyTo(encryptedContentArray);

                    using (SymmetricAlgorithm alg = OpenAlgorithm(contentEncryptionAlgorithm))
                    using (ICryptoTransform decryptor = alg.CreateDecryptor(cek, alg.IV))
                    {
                        return decryptor.OneShot(
                            encryptedContentArray,
                            0,
                            encryptedContentLength);
                    }
                }
                catch (CryptographicException e)
                {
                    exception = e;
                    return null;
                }
                finally
                {
                    Array.Clear(encryptedContentArray, 0, encryptedContentLength);
                    ArrayPool<byte>.Shared.Return(encryptedContentArray);
                    encryptedContentArray = null;
                }
            }

            public override void Dispose()
            {
            }
        }
    }
}
