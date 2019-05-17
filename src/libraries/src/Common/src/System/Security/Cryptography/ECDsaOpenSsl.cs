// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDsaImplementation
    {
#endif
        public sealed partial class ECDsaOpenSsl : ECDsa
        {
            private ECOpenSsl _key;

            /// <summary>
            /// Create an ECDsaOpenSsl algorithm with a named curve.
            /// </summary>
            /// <param name="curve">The <see cref="ECCurve"/> representing the curve.</param>
            /// <exception cref="ArgumentNullException">if <paramref name="curve" /> is null.</exception>
            public ECDsaOpenSsl(ECCurve curve)
            {
                _key = new ECOpenSsl(curve);
                ForceSetKeySize(_key.KeySize);
            }

            /// <summary>
            ///     Create an ECDsaOpenSsl algorithm with a random 521 bit key pair.
            /// </summary>
            public ECDsaOpenSsl()
                : this(521)
            {
            }

            /// <summary>
            ///     Creates a new ECDsaOpenSsl object that will use a randomly generated key of the specified size.
            /// </summary>
            /// <param name="keySize">Size of the key to generate, in bits.</param>
            public ECDsaOpenSsl(int keySize)
            {
                KeySize = keySize;
                // Setting KeySize wakes up _key.
                Debug.Assert(_key != null);
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

            public override byte[] SignHash(byte[] hash)
            {
                if (hash == null)
                    throw new ArgumentNullException(nameof(hash));

                SafeEcKeyHandle key = _key.Value;
                int signatureLength = Interop.Crypto.EcDsaSize(key);
                byte[] signature = new byte[signatureLength];
                if (!Interop.Crypto.EcDsaSign(hash, signature, ref signatureLength, key))
                    throw Interop.Crypto.CreateOpenSslCryptographicException();

                byte[] converted = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(signature, 0, signatureLength, KeySize);

                return converted;
            }

            public override bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
            {
                SafeEcKeyHandle key = _key.Value;

                byte[] converted;
                int signatureLength = Interop.Crypto.EcDsaSize(key);
                byte[] signature = CryptoPool.Rent(signatureLength);
                try
                {
                    if (!Interop.Crypto.EcDsaSign(hash, new Span<byte>(signature, 0, signatureLength), ref signatureLength, key))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    converted = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(signature, 0, signatureLength, KeySize);
                }
                finally
                {
                    CryptoPool.Return(signature, signatureLength);
                }

                if (converted.Length <= destination.Length)
                {
                    new ReadOnlySpan<byte>(converted).CopyTo(destination);
                    bytesWritten = converted.Length;
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

                byte[] openSslFormat = AsymmetricAlgorithmHelpers.ConvertIeee1363ToDer(signature);

                SafeEcKeyHandle key = _key.Value;
                int verifyResult = Interop.Crypto.EcDsaVerify(hash, openSslFormat, key);
                return verifyResult == 1;
            }

            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                AsymmetricAlgorithmHelpers.HashData(data, offset, count, hashAlgorithm);

            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
                AsymmetricAlgorithmHelpers.HashData(data, hashAlgorithm);

            protected override bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                AsymmetricAlgorithmHelpers.TryHashData(data, destination, hashAlgorithm, out bytesWritten);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _key.Dispose();
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

                    // This is the only place where _key can be null, because it's called by the constructor
                    // which sets KeySize.
                    _key?.Dispose();
                    _key = new ECOpenSsl(this);
                }
            }

            public override void GenerateKey(ECCurve curve)
            {
                _key.GenerateKey(curve);

                // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
                // with the already loaded key.
                ForceSetKeySize(_key.KeySize);
            }

            public override void ImportParameters(ECParameters parameters)
            {
                _key.ImportParameters(parameters);
                ForceSetKeySize(_key.KeySize);
            }

            public override ECParameters ExportExplicitParameters(bool includePrivateParameters) =>
                ECOpenSsl.ExportExplicitParameters(_key.Value, includePrivateParameters);

            public override ECParameters ExportParameters(bool includePrivateParameters) =>
                ECOpenSsl.ExportParameters(_key.Value, includePrivateParameters);

        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
