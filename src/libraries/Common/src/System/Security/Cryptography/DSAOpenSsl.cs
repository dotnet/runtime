// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    public partial class DSA : AsymmetricAlgorithm
    {
        private static DSA CreateCore()
        {
            return new DSAImplementation.DSAOpenSsl();
        }
    }

    internal static partial class DSAImplementation
    {
#endif
        public sealed partial class DSAOpenSsl : DSA
        {
            // The biggest key allowed by FIPS 186-4 has N=256 (bit), which
            // maximally produces a 72-byte DER signature.
            // If a future version of the standard continues to enhance DSA,
            // we may want to bump this limit to allow the max-1 (expected size)
            // TryCreateSignature to pass.
            // Future updates seem unlikely, though, as FIPS 186-5 October 2019 draft has
            // DSA as a no longer supported/updated algorithm.
            private const int SignatureStackBufSize = 72;
            private const int BitsPerByte = 8;

            private Lazy<SafeEvpPKeyHandle> _key = null!;

            public DSAOpenSsl()
                : this(2048)
            {
            }

            public DSAOpenSsl(int keySize)
            {
                LegalKeySizesValue = s_legalKeySizes;
                base.KeySize = keySize;
                _key = new Lazy<SafeEvpPKeyHandle>(GenerateKey);
            }

            public override int KeySize
            {
                set
                {
                    if (KeySize == value)
                    {
                        return;
                    }

                    // Set the KeySize before FreeKey so that an invalid value doesn't throw away the key
                    base.KeySize = value;

                    ThrowIfDisposed();
                    FreeKey();
                    _key = new Lazy<SafeEvpPKeyHandle>(GenerateKey);
                }
            }

            private void ForceSetKeySize(int newKeySize)
            {
                // In the event that a key was loaded via ImportParameters or an IntPtr/SafeHandle
                // it could be outside of the bounds that we currently represent as "legal key sizes".
                // Since that is our view into the underlying component it can be detached from the
                // component's understanding.  If it said it has opened a key, and this is the size, trust it.
                KeySizeValue = newKeySize;
            }

            public override KeySizes[] LegalKeySizes
            {
                get
                {
                    return base.LegalKeySizes;
                }
            }

            public override DSAParameters ExportParameters(bool includePrivateParameters)
            {
                // It's entirely possible that this line will cause the key to be generated in the first place.
                SafeEvpPKeyHandle key = GetKey();
                DSAParameters ret;

                if (includePrivateParameters)
                {
                    ArraySegment<byte> pkcs8 = Interop.Crypto.RentEncodePkcs8PrivateKey(key);

                    try
                    {
                        DSAKeyFormatHelper.ReadPkcs8(pkcs8, out int read, out ret);
                        Debug.Assert(read == pkcs8.Count);
                    }
                    catch (CryptographicException)
                    {
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }
                    finally
                    {
                        CryptoPool.Return(pkcs8);
                    }
                }
                else
                {
                    ArraySegment<byte> spki = Interop.Crypto.RentEncodeSubjectPublicKeyInfo(key);

                    try
                    {
                        DSAKeyFormatHelper.ReadSubjectPublicKeyInfo(spki, out int read, out ret);
                        Debug.Assert(read == spki.Count);
                    }
                    finally
                    {
                        CryptoPool.Return(spki);
                    }
                }

                return ret;
            }

            public override void ImportParameters(DSAParameters parameters)
            {
                if (parameters.P == null || parameters.Q == null || parameters.G == null || parameters.Y == null)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MissingFields);

                // J is not required and is not even used on CNG blobs. It should however be less than P (J == (P-1) / Q). This validation check
                // is just to maintain parity with DSACNG and DSACryptoServiceProvider, which also perform this check.
                if (parameters.J != null && parameters.J.Length >= parameters.P.Length)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedPJ);

                bool hasPrivateKey = parameters.X != null;

                int keySize = parameters.P.Length;
                if (parameters.G.Length != keySize || parameters.Y.Length != keySize)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedPGY);

                if (hasPrivateKey && parameters.X!.Length != parameters.Q.Length)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedQX);

                ThrowIfDisposed();

                if (hasPrivateKey)
                {
                    AsnWriter writer = DSAKeyFormatHelper.WritePkcs8(parameters);
                    ArraySegment<byte> pkcs8 = writer.RentAndEncode();

                    try
                    {
                        ImportPkcs8PrivateKey(pkcs8, checkAlgorithm: false, out _);
                    }
                    finally
                    {
                        CryptoPool.Return(pkcs8);
                    }
                }
                else
                {
                    AsnWriter writer = DSAKeyFormatHelper.WriteSubjectPublicKeyInfo(parameters);
                    ArraySegment<byte> spki = writer.RentAndEncode();

                    try
                    {
                        ImportSubjectPublicKeyInfo(spki, checkAlgorithm: false, out _);
                    }
                    finally
                    {
                        CryptoPool.Return(spki);
                    }
                }
            }

            public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
            {
                ThrowIfDisposed();

                ImportSubjectPublicKeyInfo(source, checkAlgorithm: true, out bytesRead);
            }

            private void ImportSubjectPublicKeyInfo(
                ReadOnlySpan<byte> source,
                bool checkAlgorithm,
                out int bytesRead)
            {
                int read;

                if (checkAlgorithm)
                {
                    read = DSAKeyFormatHelper.CheckSubjectPublicKeyInfo(source);
                }
                else
                {
                    read = source.Length;
                }

                SafeEvpPKeyHandle newKey = Interop.Crypto.DecodeSubjectPublicKeyInfo(
                    source.Slice(0, read),
                    Interop.Crypto.EvpAlgorithmId.DSA);

                Debug.Assert(!newKey.IsInvalid);
                SetKey(newKey);
                bytesRead = read;
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

            public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
            {
                ThrowIfDisposed();

                ImportPkcs8PrivateKey(source, checkAlgorithm: true, out bytesRead);
            }

            private void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, bool checkAlgorithm, out int bytesRead)
            {
                int read;

                if (checkAlgorithm)
                {
                    read = DSAKeyFormatHelper.CheckPkcs8(source);
                }
                else
                {
                    read = source.Length;
                }

                SafeEvpPKeyHandle newKey = Interop.Crypto.DecodePkcs8PrivateKey(
                    source.Slice(0, read),
                    Interop.Crypto.EvpAlgorithmId.DSA);

                Debug.Assert(!newKey.IsInvalid);
                SetKey(newKey);
                bytesRead = read;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    FreeKey();
                    _key = null!;
                }

                base.Dispose(disposing);
            }

            private void FreeKey()
            {
                if (_key != null && _key.IsValueCreated)
                {
                    SafeEvpPKeyHandle handle = _key.Value;

                    if (handle != null)
                    {
                        handle.Dispose();
                    }
                }
            }

            private static void CheckInvalidKey(SafeEvpPKeyHandle key)
            {
                if (key == null || key.IsInvalid)
                {
                    throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
                }
            }

            private SafeEvpPKeyHandle GenerateKey()
            {
                return Interop.Crypto.DsaGenerateKey(KeySize);
            }

            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
            {
                // we're sealed and the base should have checked this already
                Debug.Assert(data != null);
                Debug.Assert(offset >= 0 && offset <= data.Length);
                Debug.Assert(count >= 0 && count <= data.Length);
                Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

                return AsymmetricAlgorithmHelpers.HashData(data, offset, count, hashAlgorithm);
            }

            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
                AsymmetricAlgorithmHelpers.HashData(data, hashAlgorithm);

            protected override bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                AsymmetricAlgorithmHelpers.TryHashData(data, destination, hashAlgorithm, out bytesWritten);

            public override byte[] CreateSignature(byte[] rgbHash)
            {
                if (rgbHash == null)
                    throw new ArgumentNullException(nameof(rgbHash));

                SafeEvpPKeyHandle key = GetKey();

                int signatureSize = Interop.Crypto.EvpPKeySize(key);
                int signatureFieldSize = Interop.Crypto.DsaSignatureFieldSize(key) * BitsPerByte;
                Debug.Assert(signatureSize <= SignatureStackBufSize);
                Span<byte> signDestination = stackalloc byte[SignatureStackBufSize];

                ReadOnlySpan<byte> derSignature = SignHash(key, rgbHash, signDestination);
                return AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(derSignature, signatureFieldSize);
            }

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
            public override bool TryCreateSignature(
                ReadOnlySpan<byte> hash,
                Span<byte> destination,
                out int bytesWritten)
            {
                return TryCreateSignatureCore(
                    hash,
                    destination,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation,
                    out bytesWritten);
            }

            protected override bool TryCreateSignatureCore(
                ReadOnlySpan<byte> hash,
                Span<byte> destination,
                DSASignatureFormat signatureFormat,
                out int bytesWritten)
#else
            public override bool TryCreateSignature(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
#endif
            {
                SafeEvpPKeyHandle key = GetKey();

                int maxSignatureSize = Interop.Crypto.EvpPKeySize(key);
                Debug.Assert(maxSignatureSize <= SignatureStackBufSize);
                Span<byte> signDestination = stackalloc byte[SignatureStackBufSize];

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
                if (signatureFormat == DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
#endif
                {
                    int fieldSizeBytes = Interop.Crypto.DsaSignatureFieldSize(key);
                    int p1363SignatureSize = 2 * fieldSizeBytes;

                    if (destination.Length < p1363SignatureSize)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    int fieldSizeBits = fieldSizeBytes * 8;

                    ReadOnlySpan<byte> derSignature = SignHash(key, hash, signDestination);
                    bytesWritten = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(derSignature, fieldSizeBits, destination);
                    Debug.Assert(bytesWritten == p1363SignatureSize);
                    return true;
                }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
                else if (signatureFormat == DSASignatureFormat.Rfc3279DerSequence)
                {
                    if (destination.Length >= maxSignatureSize)
                    {
                        signDestination = destination;
                    }
                    else if (maxSignatureSize > signDestination.Length)
                    {
                        Debug.Fail($"Stack-based signDestination is insufficient ({maxSignatureSize} needed)");
                        bytesWritten = 0;
                        return false;
                    }

                    ReadOnlySpan<byte> derSignature = SignHash(key, hash, signDestination);

                    if (destination == signDestination)
                    {
                        bytesWritten = derSignature.Length;
                        return true;
                    }

                    return Helpers.TryCopyToDestination(derSignature, destination, out bytesWritten);
                }
                else
                {
                    Debug.Fail($"Missing internal implementation handler for signature format {signatureFormat}");
                    throw new CryptographicException(
                        SR.Cryptography_UnknownSignatureFormat,
                        signatureFormat.ToString());
                }
#endif
            }

            private static ReadOnlySpan<byte> SignHash(
                SafeEvpPKeyHandle key,
                ReadOnlySpan<byte> hash,
                Span<byte> destination)
            {
                int actualLength = Interop.Crypto.DsaSignHash(key, hash, destination);
                return destination.Slice(0, actualLength);
            }

            public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
            {
                if (rgbHash == null)
                    throw new ArgumentNullException(nameof(rgbHash));
                if (rgbSignature == null)
                    throw new ArgumentNullException(nameof(rgbSignature));

                return VerifySignature((ReadOnlySpan<byte>)rgbHash, (ReadOnlySpan<byte>)rgbSignature);
            }

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS

            public override bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature) =>
                VerifySignatureCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            protected override bool VerifySignatureCore(
                ReadOnlySpan<byte> hash,
                ReadOnlySpan<byte> signature,
                DSASignatureFormat signatureFormat)
#else
            public override bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
#endif
            {
                SafeEvpPKeyHandle key = GetKey();

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
                if (signatureFormat == DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
                {
#endif
                    int expectedSignatureBytes = Interop.Crypto.DsaSignatureFieldSize(key) * 2;
                    if (signature.Length != expectedSignatureBytes)
                    {
                        // The input isn't of the right length (assuming no DER), so we can't sensibly re-encode it with DER.
                        return false;
                    }

                    signature = AsymmetricAlgorithmHelpers.ConvertIeee1363ToDer(signature);
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
                }
                else if (signatureFormat != DSASignatureFormat.Rfc3279DerSequence)
                {
                    Debug.Fail($"Missing internal implementation handler for signature format {signatureFormat}");
                    throw new CryptographicException(
                        SR.Cryptography_UnknownSignatureFormat,
                        signatureFormat.ToString());
                }
                else
                {
                    // Ensure that the signature is a valid DER SEQUENCE(INTEGER,INTEGER), otherwise
                    // OpenSSL will return -1 without setting an error.
                    try
                    {
                        AsnValueReader reader = new AsnValueReader(signature, AsnEncodingRules.DER);
                        AsnValueReader payload = reader.ReadSequence();

                        if (reader.HasData)
                        {
                            return false;
                        }

                        payload.ReadIntegerBytes();
                        payload.ReadIntegerBytes();

                        if (payload.HasData)
                        {
                            return false;
                        }
                    }
                    catch (AsnContentException)
                    {
                        return false;
                    }
                }
#endif
                return Interop.Crypto.SimpleVerifyHash(key, hash, signature);
            }

            private void ThrowIfDisposed()
            {
                if (_key == null)
                {
                    throw new ObjectDisposedException(
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
                        nameof(DSA)
#else
                        nameof(DSAOpenSsl)
#endif
                    );
                }
            }

            private SafeEvpPKeyHandle GetKey()
            {
                ThrowIfDisposed();

                SafeEvpPKeyHandle key = _key.Value;
                CheckInvalidKey(key);

                return key;
            }

            [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(_key))]
            private void SetKey(SafeEvpPKeyHandle newKey)
            {
                Debug.Assert(!newKey.IsInvalid);
                // Do not call ThrowIfDisposed here, as it breaks the SafeEvpPKey ctor
                FreeKey();
                _key = new Lazy<SafeEvpPKeyHandle>(newKey);

                // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
                // with the already loaded key.
                ForceSetKeySize(Interop.Crypto.EvpPKeyBits(newKey));
            }

            private static readonly KeySizes[] s_legalKeySizes = new KeySizes[] { new KeySizes(minSize: 512, maxSize: 3072, skipSize: 64) };
        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
