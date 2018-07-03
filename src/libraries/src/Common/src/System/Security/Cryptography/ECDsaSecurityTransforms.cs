// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    public partial class ECDsa : AsymmetricAlgorithm
    {
        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        public static new ECDsa Create()
        {
            return new ECDsaImplementation.ECDsaSecurityTransforms();
        }

        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        /// <param name="curve">
        /// The <see cref="ECCurve"/> representing the elliptic curve.
        /// </param>
        public static ECDsa Create(ECCurve curve)
        {
            ECDsa ecdsa = Create();
            ecdsa.GenerateKey(curve);
            return ecdsa;
        }

        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        /// <param name="parameters">
        /// The <see cref="ECParameters"/> representing the elliptic curve parameters.
        /// </param>
        public static ECDsa Create(ECParameters parameters)
        {
            ECDsa ecdsa = Create();
            ecdsa.ImportParameters(parameters);
            return ecdsa;
        }
#endif
        internal static partial class ECDsaImplementation
        {
            public sealed partial class ECDsaSecurityTransforms : ECDsa
            {
                private readonly EccSecurityTransforms _ecc = new EccSecurityTransforms();

                public ECDsaSecurityTransforms()
                {
                    KeySize = 521;
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
                        _ecc.Dispose();
                    }
                }

                public override byte[] SignHash(byte[] hash)
                {
                    if (hash == null)
                        throw new ArgumentNullException(nameof(hash));

                    SecKeyPair keys = GetKeys();

                    if (keys.PrivateKey == null)
                    {
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }

                    byte[] derFormatSignature = Interop.AppleCrypto.GenerateSignature(keys.PrivateKey, hash);
                    byte[] ieeeFormatSignature = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(
                        derFormatSignature,
                        0,
                        derFormatSignature.Length,
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

                    byte[] derFormatSignature = Interop.AppleCrypto.GenerateSignature(keys.PrivateKey, source);
                    byte[] ieeeFormatSignature = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(
                        derFormatSignature,
                        0,
                        derFormatSignature.Length,
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
                    if (hash == null)
                        throw new ArgumentNullException(nameof(hash));
                    if (signature == null)
                        throw new ArgumentNullException(nameof(signature));

                    return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature);
                }

                public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
                {
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
                        AsymmetricAlgorithmHelpers.ConvertIeee1363ToDer(signature));
                }

                protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                    AsymmetricAlgorithmHelpers.HashData(data, offset, count, hashAlgorithm);

                protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
                    AsymmetricAlgorithmHelpers.HashData(data, hashAlgorithm);

                protected override bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                    AsymmetricAlgorithmHelpers.TryHashData(source, destination, hashAlgorithm, out bytesWritten);

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

                public override void ImportParameters(ECParameters parameters)
                {
                    KeySizeValue = _ecc.ImportParameters(parameters);
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
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
