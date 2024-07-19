// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class ECDiffieHellmanOpenSsl : ECDiffieHellman
    {
        private Lazy<SafeEvpPKeyHandle>? _key;

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDiffieHellmanOpenSsl(ECCurve curve)
        {
            ThrowIfNotSupported();
            _key = new Lazy<SafeEvpPKeyHandle>(ECOpenSsl.GenerateECKey(curve, out int keySize));
            KeySizeValue = keySize;
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDiffieHellmanOpenSsl()
            : this(521)
        {
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDiffieHellmanOpenSsl(int keySize)
        {
            ThrowIfNotSupported();
            base.KeySize = keySize;
            _key = new Lazy<SafeEvpPKeyHandle>(() => ECOpenSsl.GenerateECKey(keySize));
        }

        public override KeySizes[] LegalKeySizes => s_defaultKeySizes.CloneKeySizesArray();

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
                {
                    return;
                }

                // Set the KeySize before FreeKey so that an invalid value doesn't throw away the key
                base.KeySize = value;

                ThrowIfDisposed();
                FreeKey();
                _key = new Lazy<SafeEvpPKeyHandle>(ECOpenSsl.GenerateECKey(value));
            }
        }

        public override void GenerateKey(ECCurve curve)
        {
            ThrowIfDisposed();

            FreeKey();
            _key = new Lazy<SafeEvpPKeyHandle>(ECOpenSsl.GenerateECKey(curve, out int keySizeValue));
            KeySizeValue = keySizeValue;
        }

        public override ECDiffieHellmanPublicKey PublicKey
        {
            get
            {
                ThrowIfDisposed();

                // This may generate the key
                return new ECDiffieHellmanOpenSslPublicKey(_key.Value);
            }
        }

        public override void ImportParameters(ECParameters parameters)
        {
            ThrowIfDisposed();
            FreeKey();
            _key = new Lazy<SafeEvpPKeyHandle>(ECOpenSsl.ImportECKey(parameters, out int keySize));
            KeySizeValue = keySize;
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
