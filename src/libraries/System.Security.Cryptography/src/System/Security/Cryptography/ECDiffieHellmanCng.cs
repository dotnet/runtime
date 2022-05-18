// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Wrapper for CNG's implementation of elliptic curve Diffie-Hellman key exchange
    /// </summary>
    public sealed partial class ECDiffieHellmanCng : ECDiffieHellman
    {
        private CngAlgorithmCore _core = new CngAlgorithmCore(nameof(ECDiffieHellmanCng)) { DefaultKeyType = CngAlgorithm.ECDiffieHellman };
        private CngAlgorithm _hashAlgorithm = CngAlgorithm.Sha256;
        private ECDiffieHellmanKeyDerivationFunction _kdf = ECDiffieHellmanKeyDerivationFunction.Hash;
        private byte[]? _hmacKey;
        private byte[]? _label;
        private byte[]? _secretAppend;
        private byte[]? _secretPrepend;
        private byte[]? _seed;

        [SupportedOSPlatform("windows")]
        public ECDiffieHellmanCng(CngKey key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (key.AlgorithmGroup != CngAlgorithmGroup.ECDiffieHellman)
                throw new ArgumentException(SR.Cryptography_ArgECDHRequiresECDHKey, nameof(key));

            Key = CngAlgorithmCore.Duplicate(key);
        }

        /// <summary>
        ///     Hash algorithm used with the Hash and HMAC KDFs
        /// </summary>
        public CngAlgorithm HashAlgorithm
        {
            get
            {
                return _hashAlgorithm;
            }

            set
            {
                ArgumentNullException.ThrowIfNull(value, nameof(value));

                _hashAlgorithm = value;
            }
        }

        /// <summary>
        ///     KDF used to transform the secret agreement into key material
        /// </summary>
        public ECDiffieHellmanKeyDerivationFunction KeyDerivationFunction
        {
            get
            {
                return _kdf;
            }

            set
            {
                if (value < ECDiffieHellmanKeyDerivationFunction.Hash || value > ECDiffieHellmanKeyDerivationFunction.Tls)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _kdf = value;
            }
        }

        /// <summary>
        ///     Key used with the HMAC KDF
        /// </summary>
        public byte[]? HmacKey
        {
            get { return _hmacKey; }
            set { _hmacKey = value; }
        }

        /// <summary>
        ///     Label bytes used for the TLS KDF
        /// </summary>
        public byte[]? Label
        {
            get { return _label; }
            set { _label = value; }
        }

        /// <summary>
        ///     Bytes to append to the raw secret agreement before processing by the KDF
        /// </summary>
        public byte[]? SecretAppend
        {
            get { return _secretAppend; }
            set { _secretAppend = value; }
        }

        /// <summary>
        ///     Bytes to prepend to the raw secret agreement before processing by the KDF
        /// </summary>
        public byte[]? SecretPrepend
        {
            get { return _secretPrepend; }
            set { _secretPrepend = value; }
        }

        /// <summary>
        ///     Seed bytes used for the TLS KDF
        /// </summary>
        public byte[]? Seed
        {
            get { return _seed; }
            set { _seed = value; }
        }

        /// <summary>
        ///     Use the secret agreement as the HMAC key rather than supplying a separate one
        /// </summary>
        public bool UseSecretAgreementAsHmacKey
        {
            get { return HmacKey == null; }
        }

        protected override void Dispose(bool disposing)
        {
            _core.Dispose();
        }

        private void ThrowIfDisposed()
        {
            _core.ThrowIfDisposed();
        }

        private void DisposeKey()
        {
            _core.DisposeKey();
        }

        internal string? GetCurveName(out string? oidValue)
        {
            return Key.GetCurveName(out oidValue);
        }

        private void ImportFullKeyBlob(byte[] ecfullKeyBlob, bool includePrivateParameters)
        {
            Key = ECCng.ImportFullKeyBlob(ecfullKeyBlob, includePrivateParameters);
        }

        private void ImportKeyBlob(byte[] ecfullKeyBlob, string curveName, bool includePrivateParameters)
        {
            Key = ECCng.ImportKeyBlob(ecfullKeyBlob, curveName, includePrivateParameters);
        }

        private byte[] ExportKeyBlob(bool includePrivateParameters)
        {
            return ECCng.ExportKeyBlob(Key, includePrivateParameters);
        }

        private byte[] ExportFullKeyBlob(bool includePrivateParameters)
        {
            return ECCng.ExportFullKeyBlob(Key, includePrivateParameters);
        }

        private void AcceptImport(CngPkcs8.Pkcs8Response response)
        {
            Key = response.Key;
        }

        public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            return Key.TryExportKeyBlob(
                Interop.NCrypt.NCRYPT_PKCS8_PRIVATE_KEY_BLOB,
                destination,
                out bytesWritten);
        }

        private byte[] ExportEncryptedPkcs8(ReadOnlySpan<char> pkcs8Password, int kdfCount)
        {
            return Key.ExportPkcs8KeyBlob(pkcs8Password, kdfCount);
        }

        private bool TryExportEncryptedPkcs8(
            ReadOnlySpan<char> pkcs8Password,
            int kdfCount,
            Span<byte> destination,
            out int bytesWritten)
        {
            return Key.TryExportPkcs8KeyBlob(
                pkcs8Password,
                kdfCount,
                destination,
                out bytesWritten);
        }
    }
}
