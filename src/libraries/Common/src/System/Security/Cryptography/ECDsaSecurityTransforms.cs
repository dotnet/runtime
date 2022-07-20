// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class ECDsaImplementation
    {
        public sealed partial class ECDsaSecurityTransforms : ECDsa, IRuntimeAlgorithm
        {
            private readonly EccSecurityTransforms _ecc = new EccSecurityTransforms(nameof(ECDsa));

            public ECDsaSecurityTransforms()
            {
                base.KeySize = 521;
            }

            internal ECDsaSecurityTransforms(SafeSecKeyRefHandle publicKey)
            {
                KeySizeValue = _ecc.SetKeyAndGetSize(SecKeyPair.PublicOnly(publicKey));
            }

            internal ECDsaSecurityTransforms(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle privateKey)
            {
                KeySizeValue = _ecc.SetKeyAndGetSize(SecKeyPair.PublicPrivatePair(publicKey, privateKey));
            }

            public override KeySizes[] LegalKeySizes
            {
                get
                {
                    // Return the three sizes that can be explicitly set (for backwards compatibility)
                    return new[] {
                        new KeySizes(minSize: 256, maxSize: 384, skipSize: 128),
                        new KeySizes(minSize: 521, maxSize: 521, skipSize: 0),
                    };
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
                    _ecc.DisposeKey();
                }
            }

            public override byte[] SignHash(byte[] hash)
            {
                ArgumentNullException.ThrowIfNull(hash);

                SecKeyPair keys = GetKeys();

                if (keys.PrivateKey == null)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }

                byte[] derFormatSignature = Interop.AppleCrypto.CreateSignature(
                    keys.PrivateKey,
                    hash,
                    Interop.AppleCrypto.PAL_HashAlgorithm.Unknown,
                    Interop.AppleCrypto.PAL_SignatureAlgorithm.EC);
                byte[] ieeeFormatSignature = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(
                    derFormatSignature.AsSpan(0, derFormatSignature.Length),
                    KeySize);

                return ieeeFormatSignature;
            }

            public override bool TrySignHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
            {
                SecKeyPair keys = GetKeys();
                if (keys.PrivateKey == null)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }

                byte[] derFormatSignature = Interop.AppleCrypto.CreateSignature(
                    keys.PrivateKey,
                    source,
                    Interop.AppleCrypto.PAL_HashAlgorithm.Unknown,
                    Interop.AppleCrypto.PAL_SignatureAlgorithm.EC);
                byte[] ieeeFormatSignature = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(
                    derFormatSignature.AsSpan(0, derFormatSignature.Length),
                    KeySize);

                if (ieeeFormatSignature.Length <= destination.Length)
                {
                    new ReadOnlySpan<byte>(ieeeFormatSignature).CopyTo(destination);
                    bytesWritten = ieeeFormatSignature.Length;
                    return true;
                }
                else
                {
                    bytesWritten = 0;
                    return false;
                }
            }

            public override bool VerifyHash(byte[] hash, byte[] signature)
            {
                ArgumentNullException.ThrowIfNull(hash);
                ArgumentNullException.ThrowIfNull(signature);

                return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature);
            }

            public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
            {
                ThrowIfDisposed();

                // The signature format for .NET is r.Concat(s). Each of r and s are of length BitsToBytes(KeySize), even
                // when they would have leading zeroes.  If it's the correct size, then we need to encode it from
                // r.Concat(s) to SEQUENCE(INTEGER(r), INTEGER(s)), because that's the format that OpenSSL expects.
                int expectedBytes = 2 * AsymmetricAlgorithmHelpers.BitsToBytes(KeySize);
                if (signature.Length != expectedBytes)
                {
                    // The input isn't of the right length, so we can't sensibly re-encode it.
                    return false;
                }

                return Interop.AppleCrypto.VerifySignature(
                    GetKeys().PublicKey,
                    hash,
                    AsymmetricAlgorithmHelpers.ConvertIeee1363ToDer(signature),
                    Interop.AppleCrypto.PAL_HashAlgorithm.Unknown,
                    Interop.AppleCrypto.PAL_SignatureAlgorithm.EC);
            }

            private void ThrowIfDisposed()
            {
                _ecc.ThrowIfDisposed();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _ecc.Dispose();
                }

                base.Dispose(disposing);
            }

            public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            public override ECParameters ExportParameters(bool includePrivateParameters)
            {
                return _ecc.ExportParameters(includePrivateParameters, KeySize);
            }

            internal bool TryExportDataKeyParameters(bool includePrivateParameters, ref ECParameters ecParameters)
            {
                return _ecc.TryExportDataKeyParameters(includePrivateParameters, KeySize, ref ecParameters);
            }

            public override void ImportParameters(ECParameters parameters)
            {
                KeySizeValue = _ecc.ImportParameters(parameters);
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

            public override void GenerateKey(ECCurve curve)
            {
                KeySizeValue = _ecc.GenerateKey(curve);
            }

            internal SecKeyPair GetKeys()
            {
                return _ecc.GetOrGenerateKeys(KeySize);
            }
        }
    }
}
