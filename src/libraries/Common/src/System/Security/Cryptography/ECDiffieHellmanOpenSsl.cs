// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class ECDiffieHellmanOpenSsl : ECDiffieHellman
    {
        private SafeEvpPKeyHandle _key;

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDiffieHellmanOpenSsl(ECCurve curve)
        {
            ThrowIfNotSupported();
            _key = SafeEvpPKeyHandle.GenerateECKey(curve, out int keySize);
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
            _key = SafeEvpPKeyHandle.GenerateECKey(keySize);
        }

        public override KeySizes[] LegalKeySizes => s_defaultKeySizes.CloneKeySizesArray();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key?.Dispose();
                _key = null!;
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
                _key.Dispose();
                _key = SafeEvpPKeyHandle.GenerateECKey(value);
            }
        }

        public override void GenerateKey(ECCurve curve)
        {
            ThrowIfDisposed();

            _key.Dispose();
            _key = SafeEvpPKeyHandle.GenerateECKey(curve, out int keySizeValue);
            KeySizeValue = keySizeValue;
        }

        public override ECDiffieHellmanPublicKey PublicKey
        {
            get
            {
                ThrowIfDisposed();
                return new ECDiffieHellmanOpenSslPublicKey(_key);
            }
        }

        public override void ImportParameters(ECParameters parameters)
        {
            ThrowIfDisposed();
            _key.Dispose();
            _key = SafeEvpPKeyHandle.GenerateECKey(parameters, out int keySize);
            KeySizeValue = keySize;
        }

        public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
        {
            ThrowIfDisposed();

            using (SafeEcKeyHandle ecKey = Interop.Crypto.EvpPkeyGetEcKey(_key))
            {
                return ECOpenSsl.ExportExplicitParameters(ecKey, includePrivateParameters);
            }
        }

        public override ECParameters ExportParameters(bool includePrivateParameters)
        {
            ThrowIfDisposed();

            using (SafeEcKeyHandle ecKey = Interop.Crypto.EvpPkeyGetEcKey(_key))
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

        [MemberNotNull(nameof(_key))]
        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_key is null, this);
        }

        static partial void ThrowIfNotSupported();
    }
}
