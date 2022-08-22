// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class DSAImplementation
    {
        public sealed partial class DSASecurityTransforms : DSA, IRuntimeAlgorithm
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

            public override byte[] CreateSignature(byte[] rgbHash)
            {
                ArgumentNullException.ThrowIfNull(rgbHash);

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
                    fieldSizeBits: SHA1.HashSizeInBits);

                return ieeeFormatSignature;
            }

            public override bool VerifySignature(byte[] hash, byte[] signature)
            {
                ArgumentNullException.ThrowIfNull(hash);
                ArgumentNullException.ThrowIfNull(signature);

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

                return HashOneShotHelpers.HashData(hashAlgorithm, new ReadOnlySpan<byte>(data, offset, count));
            }

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

                // macOS declares DSA invalid for key generation.
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
}
