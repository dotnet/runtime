// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.Apple;

namespace System.Security.Cryptography
{
    internal static partial class ECDiffieHellmanImplementation
    {
        public sealed partial class ECDiffieHellmanSecurityTransforms : ECDiffieHellman
        {
            private readonly EccSecurityTransforms _ecc = new EccSecurityTransforms(nameof(ECDiffieHellman));

            public ECDiffieHellmanSecurityTransforms()
            {
                base.KeySize = 521;
            }

            internal ECDiffieHellmanSecurityTransforms(SafeSecKeyRefHandle publicKey)
            {
                KeySizeValue = _ecc.SetKeyAndGetSize(SecKeyPair.PublicOnly(publicKey));
            }

            internal ECDiffieHellmanSecurityTransforms(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle privateKey)
            {
                KeySizeValue = _ecc.SetKeyAndGetSize(SecKeyPair.PublicPrivatePair(publicKey, privateKey));
            }

            public override KeySizes[] LegalKeySizes
            {
                get
                {
                    // Return the three sizes that can be explicitly set (for backwards compatibility)
                    return new[]
                    {
                        new KeySizes(minSize: 256, maxSize: 384, skipSize: 128),
                        new KeySizes(minSize: 521, maxSize: 521, skipSize: 0),
                    };
                }
            }

            public override int KeySize
            {
                get { return base.KeySize; }
                set
                {
                    if (KeySize == value)
                        return;

                    // Set the KeySize before freeing the key so that an invalid value doesn't throw away the key
                    base.KeySize = value;
                    _ecc.DisposeKey();
                }
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

            public override byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey) =>
                DeriveKeyFromHash(otherPartyPublicKey, HashAlgorithmName.SHA256, null, null);

            public override byte[] DeriveKeyFromHash(
                ECDiffieHellmanPublicKey otherPartyPublicKey!!,
                HashAlgorithmName hashAlgorithm,
                byte[]? secretPrepend,
                byte[]? secretAppend)
            {
                ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

                ThrowIfDisposed();

                return ECDiffieHellmanDerivation.DeriveKeyFromHash(
                    otherPartyPublicKey,
                    hashAlgorithm,
                    secretPrepend,
                    secretAppend,
                    (pubKey, hasher) => DeriveSecretAgreement(pubKey, hasher));
            }

            public override byte[] DeriveKeyFromHmac(
                ECDiffieHellmanPublicKey otherPartyPublicKey!!,
                HashAlgorithmName hashAlgorithm,
                byte[]? hmacKey,
                byte[]? secretPrepend,
                byte[]? secretAppend)
            {
                ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

                ThrowIfDisposed();

                return ECDiffieHellmanDerivation.DeriveKeyFromHmac(
                    otherPartyPublicKey,
                    hashAlgorithm,
                    hmacKey,
                    secretPrepend,
                    secretAppend,
                    (pubKey, hasher) => DeriveSecretAgreement(pubKey, hasher));
            }

            public override byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey!!, byte[] prfLabel!!, byte[] prfSeed!!)
            {
                ThrowIfDisposed();

                return ECDiffieHellmanDerivation.DeriveKeyTls(
                    otherPartyPublicKey,
                    prfLabel,
                    prfSeed,
                    (pubKey, hasher) => DeriveSecretAgreement(pubKey, hasher));
            }

            private byte[]? DeriveSecretAgreement(ECDiffieHellmanPublicKey otherPartyPublicKey, IncrementalHash? hasher)
            {
                if (!(otherPartyPublicKey is ECDiffieHellmanSecurityTransformsPublicKey secTransPubKey))
                {
                    secTransPubKey =
                        new ECDiffieHellmanSecurityTransformsPublicKey(otherPartyPublicKey.ExportParameters());
                }

                try
                {
                    SafeSecKeyRefHandle otherPublic = secTransPubKey.KeyHandle;

                    if (Interop.AppleCrypto.EccGetKeySizeInBits(otherPublic) != KeySize)
                    {
                        throw new ArgumentException(
                            SR.Cryptography_ArgECDHKeySizeMismatch,
                            nameof(otherPartyPublicKey));
                    }

                    SafeSecKeyRefHandle? thisPrivate = GetKeys().PrivateKey;

                    if (thisPrivate == null)
                    {
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }

                    // Since Apple only supports secp256r1, secp384r1, and secp521r1; and 521 fits in
                    // 66 bytes ((521 + 7) / 8), the Span path will always succeed.
                    Span<byte> secretSpan = stackalloc byte[66];

                    byte[]? secret = Interop.AppleCrypto.EcdhKeyAgree(
                        thisPrivate,
                        otherPublic,
                        secretSpan,
                        out int bytesWritten);

                    // Either we wrote to the span or we returned an array, but not both, and not neither.
                    // ("neither" would have thrown)
                    Debug.Assert(
                        (bytesWritten == 0) != (secret == null),
                        $"bytesWritten={bytesWritten}, (secret==null)={secret == null}");

                    if (hasher == null)
                    {
                        return secret ?? secretSpan.Slice(0, bytesWritten).ToArray();
                    }

                    if (secret == null)
                    {
                        hasher.AppendData(secretSpan.Slice(0, bytesWritten));
                    }
                    else
                    {
                        hasher.AppendData(secret);
                        Array.Clear(secret);
                    }

                    return null;
                }
                finally
                {
                    if (!ReferenceEquals(otherPartyPublicKey, secTransPubKey))
                    {
                        secTransPubKey.Dispose();
                    }
                }
            }

            public override ECDiffieHellmanPublicKey PublicKey =>
                new ECDiffieHellmanSecurityTransformsPublicKey(ExportParameters(false));

            private sealed class ECDiffieHellmanSecurityTransformsPublicKey : ECDiffieHellmanPublicKey
            {
                private EccSecurityTransforms _ecc;

                public ECDiffieHellmanSecurityTransformsPublicKey(ECParameters ecParameters)
                {
                    Debug.Assert(ecParameters.D == null);
                    _ecc = new EccSecurityTransforms(nameof(ECDiffieHellmanPublicKey));
                    _ecc.ImportParameters(ecParameters);
                }

#pragma warning disable 0672 // Member overrides an obsolete member.
                public override string ToXmlString()
#pragma warning restore 0672
                {
                    throw new PlatformNotSupportedException();
                }

                /// <summary>
                /// There is no key blob format for OpenSSL ECDH like there is for Cng ECDH. Instead of allowing
                /// this to return a potentially confusing empty byte array, we opt to throw instead.
                /// </summary>
                public override byte[] ToByteArray()
                {
                    throw new PlatformNotSupportedException();
                }

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        _ecc.Dispose();
                    }

                    base.Dispose(disposing);
                }

                public override ECParameters ExportExplicitParameters() =>
                    throw new PlatformNotSupportedException(SR.Cryptography_ECC_NamedCurvesOnly);

                public override ECParameters ExportParameters() =>
                    _ecc.ExportParameters(includePrivateParameters: false, keySizeInBits: -1);

                internal SafeSecKeyRefHandle KeyHandle =>
                    _ecc.GetOrGenerateKeys(-1).PublicKey;
            }
        }
    }
}
