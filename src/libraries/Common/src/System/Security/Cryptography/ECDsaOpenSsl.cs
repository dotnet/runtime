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
            _key = new Lazy<SafeEvpPKeyHandle>(ECOpenSsl.GenerateECKey(curve, out int keySize));
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

            SafeEvpPKeyHandle key = GetKey();

            Span<byte> derSignature = stackalloc byte[SignatureStackBufSize];
            int written = Interop.Crypto.EcDsaSignHash(key, hash, derSignature);
            derSignature = derSignature.Slice(0, written);

            byte[] converted = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(derSignature, KeySize);
            return converted;
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
                int encodedSize = GetMaxSignatureSize(DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

                // IeeeP1363FixedFieldConcatenation has a constant signature size therefore we can shortcut here
                if (destination.Length < encodedSize)
                {
                    bytesWritten = 0;
                    return false;
                }

                SafeEvpPKeyHandle key = GetKey();

                Span<byte> derSignature = stackalloc byte[SignatureStackBufSize];
                int written = Interop.Crypto.EcDsaSignHash(key, hash, derSignature);
                derSignature = derSignature.Slice(0, written);

                bytesWritten = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(derSignature, KeySize, destination);
                Debug.Assert(bytesWritten == encodedSize);

                return true;
            }
            else if (signatureFormat == DSASignatureFormat.Rfc3279DerSequence)
            {
                SafeEvpPKeyHandle key = GetKey();

                // We need to distinguish between the case where the destination buffer is too small and the case where something else went wrong
                // Since Rfc3279DerSequence signature size is not constant (DER is not constant size) we will use temporary buffer with max size.
                // If that succeeds we can copy to destination buffer if it's large enough.
                Span<byte> tmpDerSignature = stackalloc byte[SignatureStackBufSize];
                bytesWritten = Interop.Crypto.EcDsaSignHash(key, hash, tmpDerSignature);
                tmpDerSignature = tmpDerSignature.Slice(0, bytesWritten);

                return Helpers.TryCopyToDestination(tmpDerSignature, destination, out bytesWritten);
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

            SafeEvpPKeyHandle key = GetKey();

            return Interop.Crypto.EcDsaVerifyHash(
                key,
                hash,
                toVerify);
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
            _key = new Lazy<SafeEvpPKeyHandle>(ECOpenSsl.GenerateECKey(curve, out int keySize));

            // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
            // with the already loaded key.
            ForceSetKeySize(keySize);
        }

        public override void ImportParameters(ECParameters parameters)
        {
            ThrowIfDisposed();

            FreeKey();
            _key = new Lazy<SafeEvpPKeyHandle>(ECOpenSsl.GenerateECKey(parameters, out int keySize));

            // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
            // with the already loaded key.
            ForceSetKeySize(keySize);
        }

        private SafeEvpPKeyHandle GetKey()
        {
            ThrowIfDisposed();

            SafeEvpPKeyHandle key = _key.Value;

            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }

            return key;
        }

        private SafeEvpPKeyHandle GenerateKeyFromSize()
        {
            return ECOpenSsl.GenerateECKey(KeySize);
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
