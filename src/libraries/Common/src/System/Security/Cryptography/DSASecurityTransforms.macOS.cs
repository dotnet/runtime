// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class DSAImplementation
    {
        public sealed partial class DSASecurityTransforms : DSA
        {
            public override DSAParameters ExportParameters(bool includePrivateParameters)
            {
                // Apple requires all private keys to be exported encrypted, but since we're trying to export
                // as parsed structures we will need to decrypt it for the user.
                const string ExportPassword = "DotnetExportPassphrase";
                SecKeyPair keys = GetKeys();

                if (includePrivateParameters && keys.PrivateKey == null)
                {
                    throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
                }

                byte[] keyBlob = Interop.AppleCrypto.SecKeyExport(
                    includePrivateParameters ? keys.PrivateKey : keys.PublicKey,
                    exportPrivate: includePrivateParameters,
                    password: ExportPassword);

                try
                {
                    if (!includePrivateParameters)
                    {
                        DSAKeyFormatHelper.ReadSubjectPublicKeyInfo(
                            keyBlob,
                            out int localRead,
                            out DSAParameters key);
                        Debug.Assert(localRead == keyBlob.Length);
                        return key;
                    }
                    else
                    {
                        DSAKeyFormatHelper.ReadEncryptedPkcs8(
                            keyBlob,
                            ExportPassword,
                            out int localRead,
                            out DSAParameters key);
                        Debug.Assert(localRead == keyBlob.Length);
                        return key;
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyBlob);
                }
            }

            public override void ImportParameters(DSAParameters parameters)
            {
                if (parameters.P == null || parameters.Q == null || parameters.G == null || parameters.Y == null)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MissingFields);

                // J is not required and is not even used on CNG blobs.
                // It should, however, be less than P (J == (P-1) / Q).
                // This validation check is just to maintain parity with DSACng and DSACryptoServiceProvider,
                // which also perform this check.
                if (parameters.J != null && parameters.J.Length >= parameters.P.Length)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedPJ);

                int keySize = parameters.P.Length;
                bool hasPrivateKey = parameters.X != null;

                if (parameters.G.Length != keySize || parameters.Y.Length != keySize)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedPGY);

                if (hasPrivateKey && parameters.X!.Length != parameters.Q.Length)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedQX);

                if (!(8 * parameters.P.Length).IsLegalSize(LegalKeySizes))
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);

                if (parameters.Q.Length != 20)
                    throw new CryptographicException(SR.Cryptography_InvalidDsaParameters_QRestriction_ShortKey);

                ThrowIfDisposed();

                if (hasPrivateKey)
                {
                    SafeSecKeyRefHandle privateKey = ImportKey(parameters);

                    DSAParameters publicOnly = parameters;
                    publicOnly.X = null;

                    SafeSecKeyRefHandle publicKey;
                    try
                    {
                        publicKey = ImportKey(publicOnly);
                    }
                    catch
                    {
                        privateKey.Dispose();
                        throw;
                    }

                    SetKey(SecKeyPair.PublicPrivatePair(publicKey, privateKey));
                }
                else
                {
                    SafeSecKeyRefHandle publicKey = ImportKey(parameters);
                    SetKey(SecKeyPair.PublicOnly(publicKey));
                }
            }

            public override void ImportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<byte> passwordBytes,
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ThrowIfDisposed();
                base.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead);
            }

            public override void ImportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ThrowIfDisposed();
                base.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead);
            }

            private static SafeSecKeyRefHandle ImportKey(DSAParameters parameters)
            {
                AsnWriter keyWriter;
                bool hasPrivateKey;

                if (parameters.X != null)
                {
                    // DSAPrivateKey ::= SEQUENCE(
                    //   version INTEGER,
                    //   p INTEGER,
                    //   q INTEGER,
                    //   g INTEGER,
                    //   y INTEGER,
                    //   x INTEGER,
                    // )

                    keyWriter = new AsnWriter(AsnEncodingRules.DER);

                    using (keyWriter.PushSequence())
                    {
                        keyWriter.WriteInteger(0);
                        keyWriter.WriteKeyParameterInteger(parameters.P);
                        keyWriter.WriteKeyParameterInteger(parameters.Q);
                        keyWriter.WriteKeyParameterInteger(parameters.G);
                        keyWriter.WriteKeyParameterInteger(parameters.Y);
                        keyWriter.WriteKeyParameterInteger(parameters.X);
                    }

                    hasPrivateKey = true;
                }
                else
                {
                    keyWriter = DSAKeyFormatHelper.WriteSubjectPublicKeyInfo(parameters);
                    hasPrivateKey = false;
                }

                byte[] rented = CryptoPool.Rent(keyWriter.GetEncodedLength());

                if (!keyWriter.TryEncode(rented, out int written))
                {
                    Debug.Fail("TryEncode failed with a pre-allocated buffer");
                    throw new InvalidOperationException();
                }

                // Explicitly clear the inner buffer
                keyWriter.Reset();

                try
                {
                    return Interop.AppleCrypto.ImportEphemeralKey(rented.AsSpan(0, written), hasPrivateKey);
                }
                finally
                {
                    CryptoPool.Return(rented, written);
                }
            }

            public override unsafe void ImportSubjectPublicKeyInfo(
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ThrowIfDisposed();

                fixed (byte* ptr = &MemoryMarshal.GetReference(source))
                {
                    using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                    {
                        // Validate the DER value and get the number of bytes.
                        DSAKeyFormatHelper.ReadSubjectPublicKeyInfo(
                            manager.Memory,
                            out int localRead);

                        SafeSecKeyRefHandle publicKey = Interop.AppleCrypto.ImportEphemeralKey(source.Slice(0, localRead), false);
                        SetKey(SecKeyPair.PublicOnly(publicKey));

                        bytesRead = localRead;
                    }
                }
            }
        }
    }
}
