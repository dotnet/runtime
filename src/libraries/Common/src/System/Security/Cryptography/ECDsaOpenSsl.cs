// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaOpenSsl : ECDsa, IRuntimeAlgorithm
    {
        // secp521r1 maxes out at 139 bytes, so 256 should always be enough
        private const int SignatureStackBufSize = 256;

        private Lazy<SafeEvpPKeyHandle>? _key;

        /// <summary>
        /// Create an ECDsaOpenSsl algorithm with a named curve.
        /// </summary>
        /// <param name="curve">The <see cref="ECCurve"/> representing the curve.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="curve" /> is null.</exception>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDsaOpenSsl(ECCurve curve)
        {
            ThrowIfNotSupported();
            _key = new Lazy<SafeEvpPKeyHandle>(SafeEvpPKeyHandle.GenerateECKey(curve, out int keySize));
            ForceSetKeySize(keySize);
        }

        /// <summary>
        ///     Create an ECDsaOpenSsl algorithm with a random 521 bit key pair.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDsaOpenSsl()
            : this(521)
        {
        }

        /// <summary>
        ///     Creates a new ECDsaOpenSsl object that will use a randomly generated key of the specified size.
        /// </summary>
        /// <param name="keySize">Size of the key to generate, in bits.</param>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDsaOpenSsl(int keySize)
        {
            ThrowIfNotSupported();
            base.KeySize = keySize;
            _key = new Lazy<SafeEvpPKeyHandle>(GenerateKeyFromSize);
        }

        /// <summary>
        /// Set the KeySize without validating against LegalKeySizes.
        /// </summary>
        /// <param name="newKeySize">The value to set the KeySize to.</param>
        private void ForceSetKeySize(int newKeySize)
        {
            // In the event that a key was loaded via ImportParameters, curve name, or an IntPtr/SafeHandle
            // it could be outside of the bounds that we currently represent as "legal key sizes".
            // Since that is our view into the underlying component it can be detached from the
            // component's understanding.  If it said it has opened a key, and this is the size, trust it.
            KeySizeValue = newKeySize;
        }

        // Return the three sizes that can be explicitly set (for backwards compatibility)
        public override KeySizes[] LegalKeySizes => s_defaultKeySizes.CloneKeySizesArray();

        public override byte[] SignHash(byte[] hash)
        {
            ArgumentNullException.ThrowIfNull(hash);
            ThrowIfDisposed();

            // We need to duplicate key handle in case it's being used by multiple threads and one of them disposes it
            using (SafeEvpPKeyHandle key = _key.Value.DuplicateHandle())
            using (SafeEvpPKeyCtxHandle ctx = Interop.Crypto.EvpPKeyCtxCreate(key))
            {
                Interop.Crypto.EvpPKeyCtxConfigureForECDSASign(ctx);

                if (!Interop.Crypto.TryEvpPKeyCtxSignatureSize(ctx, hash, out int sufficientDerSignatureSize))
                {
                    throw new CryptographicException();
                }

                Span<byte> derSignature = sufficientDerSignatureSize <= SignatureStackBufSize ? stackalloc byte[sufficientDerSignatureSize] : new byte[sufficientDerSignatureSize];
                if (!Interop.Crypto.TryEvpPKeyCtxSignHash(ctx, hash, derSignature, out int bytesWritten))
                {
                    throw new CryptographicException();
                }

                if (bytesWritten > derSignature.Length)
                {
                    Debug.Fail("TrySignHashCore wrote more bytes than it claimed it would write");
                    throw new CryptographicException();
                }

                derSignature = derSignature.Slice(0, bytesWritten);

                byte[] converted = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(derSignature, KeySize);
                return converted;
            }
        }

        public override bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
        {
            return TrySignHashCore(
                hash,
                destination,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation,
                out bytesWritten);
        }

        protected override bool TrySignHashCore(
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            DSASignatureFormat signatureFormat,
            out int bytesWritten)
        {
            ThrowIfDisposed();

            if (signatureFormat == DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            {
                int encodedSize = 2 * AsymmetricAlgorithmHelpers.BitsToBytes(KeySize);

                if (destination.Length < encodedSize)
                {
                    bytesWritten = 0;
                    return false;
                }

                // We need to duplicate key handle in case it's being used by multiple threads and one of them disposes it
                using (SafeEvpPKeyHandle key = _key.Value.DuplicateHandle())
                using (SafeEvpPKeyCtxHandle ctx = Interop.Crypto.EvpPKeyCtxCreate(key))
                {
                    Interop.Crypto.EvpPKeyCtxConfigureForECDSASign(ctx);

                    if (!Interop.Crypto.TryEvpPKeyCtxSignatureSize(ctx, hash, out int sufficientSignatureSizeInBytes))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    Span<byte> derSignatureDestination = sufficientSignatureSizeInBytes <= SignatureStackBufSize ? stackalloc byte[sufficientSignatureSizeInBytes] : new byte[sufficientSignatureSizeInBytes];
                    if (!Interop.Crypto.TryEvpPKeyCtxSignHash(ctx, hash, derSignatureDestination, out int derSignatureBytesWritten))
                    {
                        // this is unrelated to sufficient size reason
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    derSignatureDestination = derSignatureDestination.Slice(0, derSignatureBytesWritten);
                    bytesWritten = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(derSignatureDestination, KeySize, destination);
                    Debug.Assert(bytesWritten == encodedSize);
                }

                return true;
            }
            else if (signatureFormat == DSASignatureFormat.Rfc3279DerSequence)
            {
                // We need to duplicate key handle in case it's being used by multiple threads and one of them disposes it
                using (SafeEvpPKeyHandle key = _key.Value.DuplicateHandle())
                using (SafeEvpPKeyCtxHandle ctx = Interop.Crypto.EvpPKeyCtxCreate(key))
                {
                    Interop.Crypto.EvpPKeyCtxConfigureForECDSASign(ctx);

                    // We could theoretically pass this through but we need to distinguish between "not enough space" and "failed"
                    // We could check for presence of private key but that won't work when it's an external key.
                    if (!Interop.Crypto.TryEvpPKeyCtxSignatureSize(ctx, hash, out int sufficientSignatureSizeInBytes))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    if (destination.Length >= sufficientSignatureSizeInBytes)
                    {
                        // The only reason this could fail won't be related to buffer size
                        if (!Interop.Crypto.TryEvpPKeyCtxSignHash(ctx, hash, destination, out bytesWritten))
                        {
                            throw Interop.Crypto.CreateOpenSslCryptographicException();
                        }

                        return true;
                    }

                    // Since sufficientSignatureSizeInBytes can be more than what's actually needed
                    // we need temporary buffer of sufficient size and see if operation can succeed with that
                    Span<byte> derSignatureDestination = sufficientSignatureSizeInBytes <= SignatureStackBufSize ? stackalloc byte[sufficientSignatureSizeInBytes] : new byte[sufficientSignatureSizeInBytes];
                    if (!Interop.Crypto.TryEvpPKeyCtxSignHash(ctx, hash, derSignatureDestination, out int bytesWrittenToTemporaryBuffer))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    if (bytesWrittenToTemporaryBuffer > destination.Length)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    derSignatureDestination.Slice(0, bytesWrittenToTemporaryBuffer).CopyTo(destination);
                    bytesWritten = bytesWrittenToTemporaryBuffer;
                    return true;
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(signatureFormat));
            }
        }

        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            ArgumentNullException.ThrowIfNull(hash);
            ArgumentNullException.ThrowIfNull(signature);

            return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature);
        }

        public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature) =>
            VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        protected override bool VerifyHashCore(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            DSASignatureFormat signatureFormat)
        {
            ThrowIfDisposed();

            Span<byte> derSignature = stackalloc byte[SignatureStackBufSize];
            ReadOnlySpan<byte> toVerify = derSignature;

            if (signatureFormat == DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
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

                if (AsymmetricAlgorithmHelpers.TryConvertIeee1363ToDer(signature, derSignature, out int derSize))
                {
                    toVerify = derSignature.Slice(0, derSize);
                }
                else
                {
                    toVerify = AsymmetricAlgorithmHelpers.ConvertIeee1363ToDer(signature);
                }
            }
            else if (signatureFormat == DSASignatureFormat.Rfc3279DerSequence)
            {
                toVerify = signature;
            }
            else
            {
                Debug.Fail($"Missing internal implementation handler for signature format {signatureFormat}");
                throw new CryptographicException(
                    SR.Cryptography_UnknownSignatureFormat,
                    signatureFormat.ToString());
            }

            // We need to duplicate key handle in case it's being used by multiple threads and one of them disposes it
            using (SafeEvpPKeyHandle key = _key.Value.DuplicateHandle())
            using (SafeEvpPKeyCtxHandle ctx = Interop.Crypto.EvpPKeyCtxCreate(key))
            {
                Interop.Crypto.EvpPKeyCtxConfigureForECDSAVerify(ctx);
                return Interop.Crypto.EvpPKeyCtxVerifyHash(ctx, hash, toVerify);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                FreeKey();
                _key = null;
            }

            base.Dispose(disposing);
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

                // Set the KeySize before FreeKey so that an invalid value doesn't throw away the key
                base.KeySize = value;

                ThrowIfDisposed();

                FreeKey();
                _key = new Lazy<SafeEvpPKeyHandle>(GenerateKeyFromSize);
            }
        }

        public override void GenerateKey(ECCurve curve)
        {
            ThrowIfDisposed();

            FreeKey();
            _key = new Lazy<SafeEvpPKeyHandle>(SafeEvpPKeyHandle.GenerateECKey(curve, out int keySize));

            // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
            // with the already loaded key.
            ForceSetKeySize(keySize);
        }

        public override void ImportParameters(ECParameters parameters)
        {
            ThrowIfDisposed();

            FreeKey();
            _key = new Lazy<SafeEvpPKeyHandle>(SafeEvpPKeyHandle.GenerateECKey(parameters, out int keySize));

            // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
            // with the already loaded key.
            ForceSetKeySize(keySize);
        }

        private SafeEvpPKeyHandle GenerateKeyFromSize()
        {
            return SafeEvpPKeyHandle.GenerateECKey(KeySize);
        }

        public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
        {
            ThrowIfDisposed();

            using (SafeEcKeyHandle ecKey = Interop.Crypto.EvpPkeyGetEcKey(_key.Value))
            {
                return ECOpenSsl.ExportExplicitParameters(ecKey, includePrivateParameters);
            }
        }

        public override ECParameters ExportParameters(bool includePrivateParameters)
        {
            ThrowIfDisposed();

            using (SafeEcKeyHandle ecKey = Interop.Crypto.EvpPkeyGetEcKey(_key.Value))
            {
                return ECOpenSsl.ExportParameters(ecKey, includePrivateParameters);
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

        private void FreeKey()
        {
            if (_key != null && _key.IsValueCreated)
            {
                SafeEvpPKeyHandle handle = _key.Value;
                handle?.Dispose();
            }
        }

        [MemberNotNull(nameof(_key))]
        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_key is null, this);
        }

        static partial void ThrowIfNotSupported();
    }
}
