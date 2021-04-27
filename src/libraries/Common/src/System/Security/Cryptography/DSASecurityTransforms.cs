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
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS

    public partial class DSA : AsymmetricAlgorithm
    {
        public static new DSA Create()
        {
            return new DSAImplementation.DSASecurityTransforms();
        }
#endif

        internal static partial class DSAImplementation
        {
            public sealed partial class DSASecurityTransforms : DSA
            {
                private SecKeyPair? _keys;
                private bool _disposed;

                public DSASecurityTransforms()
                    : this(1024)
                {
                }

                public DSASecurityTransforms(int keySize)
                {
                    base.KeySize = keySize;
                }

                internal DSASecurityTransforms(SafeSecKeyRefHandle publicKey)
                {
                    SetKey(SecKeyPair.PublicOnly(publicKey));
                }

                internal DSASecurityTransforms(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle privateKey)
                {
                    SetKey(SecKeyPair.PublicPrivatePair(publicKey, privateKey));
                }

                public override KeySizes[] LegalKeySizes
                {
                    get
                    {
                        return new[] { new KeySizes(minSize: 512, maxSize: 1024, skipSize: 64) };
                    }
                }

                public override int KeySize
                {
                    get
                    {
                        return base.KeySize;
                    }
                    set
                    {
                        if (KeySize == value)
                            return;

                        // Set the KeySize before freeing the key so that an invalid value doesn't throw away the key
                        base.KeySize = value;

                        ThrowIfDisposed();

                        if (_keys != null)
                        {
                            _keys.Dispose();
                            _keys = null;
                        }
                    }
                }

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

                public override byte[] CreateSignature(byte[] rgbHash)
                {
                    if (rgbHash == null)
                        throw new ArgumentNullException(nameof(rgbHash));

                    SecKeyPair keys = GetKeys();

                    if (keys.PrivateKey == null)
                    {
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }

                    byte[] derFormatSignature = Interop.AppleCrypto.CreateSignature(
                        keys.PrivateKey,
                        rgbHash,
                        Interop.AppleCrypto.PAL_HashAlgorithm.Unknown,
                        Interop.AppleCrypto.PAL_SignatureAlgorithm.DSA);

                    // Since the AppleCrypto implementation is limited to FIPS 186-2, signature field sizes
                    // are always 160 bits / 20 bytes (the size of SHA-1, and the only legal length for Q).
                    byte[] ieeeFormatSignature = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(
                        derFormatSignature.AsSpan(0, derFormatSignature.Length),
                        fieldSizeBits: 160);

                    return ieeeFormatSignature;
                }

                public override bool VerifySignature(byte[] hash, byte[] signature)
                {
                    if (hash == null)
                        throw new ArgumentNullException(nameof(hash));
                    if (signature == null)
                        throw new ArgumentNullException(nameof(signature));

                    return VerifySignature((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature);
                }

                public override bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
                {
                    byte[] derFormatSignature = AsymmetricAlgorithmHelpers.ConvertIeee1363ToDer(signature);

                    return Interop.AppleCrypto.VerifySignature(
                        GetKeys().PublicKey,
                        hash,
                        derFormatSignature,
                        Interop.AppleCrypto.PAL_HashAlgorithm.Unknown,
                        Interop.AppleCrypto.PAL_SignatureAlgorithm.DSA);
                }

                protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
                {
                    if (hashAlgorithm != HashAlgorithmName.SHA1)
                    {
                        // Matching DSACryptoServiceProvider's "I only understand SHA-1/FIPS 186-2" exception
                        throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
                    }

                    return AsymmetricAlgorithmHelpers.HashData(data, offset, count, hashAlgorithm);
                }

                protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
                    AsymmetricAlgorithmHelpers.HashData(data, hashAlgorithm);

                protected override bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                    AsymmetricAlgorithmHelpers.TryHashData(data, destination, hashAlgorithm, out bytesWritten);

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        if (_keys != null)
                        {
                            _keys.Dispose();
                            _keys = null;
                        }

                        _disposed = true;
                    }

                    base.Dispose(disposing);
                }

                private void ThrowIfDisposed()
                {
                    // The other SecurityTransforms types use _keys.PublicKey == null,
                    // but since Apple doesn't provide DSA key generation we can't easily tell
                    // if a failed attempt to generate a key happened, or we're in a pristine state.
                    //
                    // So this type uses an explicit field, rather than inferred state.
                    if (_disposed)
                    {
                        throw new ObjectDisposedException(nameof(DSA));
                    }
                }

                internal SecKeyPair GetKeys()
                {
                    ThrowIfDisposed();

                    SecKeyPair? current = _keys;

                    if (current != null)
                    {
                        return current;
                    }

                    // macOS 10.11 and macOS 10.12 declare DSA invalid for key generation.
                    // Rather than write code which might or might not work, returning
                    // (OSStatus)-4 (errSecUnimplemented), just make the exception occur here.
                    //
                    // When the native code can be verified, then it can be added.
                    throw new PlatformNotSupportedException(SR.Cryptography_DSA_KeyGenNotSupported);
                }

                private void SetKey(SecKeyPair newKeyPair)
                {
                    ThrowIfDisposed();

                    SecKeyPair? current = _keys;
                    _keys = newKeyPair;
                    current?.Dispose();

                    if (newKeyPair != null)
                    {
                        int size = Interop.AppleCrypto.GetSimpleKeySizeInBits(newKeyPair.PublicKey);
                        KeySizeValue = size;
                    }
                }
            }
        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
