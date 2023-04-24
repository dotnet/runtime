// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class ECDiffieHellmanOpenSsl : ECDiffieHellman
    {
        private ECOpenSsl? _key;

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDiffieHellmanOpenSsl(ECCurve curve)
        {
            ThrowIfNotSupported();
            _key = new ECOpenSsl(curve);
            KeySizeValue = _key.KeySize;
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
            _key = new ECOpenSsl(this);
        }

        public override KeySizes[] LegalKeySizes => s_defaultKeySizes.CloneKeySizesArray();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key?.Dispose();
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
                _key.Dispose();
                _key = new ECOpenSsl(this);
            }
        }

        public override void GenerateKey(ECCurve curve)
        {
            ThrowIfDisposed();
            KeySizeValue = _key.GenerateKey(curve);
        }

        public override ECDiffieHellmanPublicKey PublicKey
        {
            get
            {
                ThrowIfDisposed();

                using (SafeEvpPKeyHandle handle = _key.UpRefKeyHandle())
                {
                    return new ECDiffieHellmanOpenSslPublicKey(handle);
                }
            }
        }

        public override void ImportParameters(ECParameters parameters)
        {
            ThrowIfDisposed();
            KeySizeValue = _key.ImportParameters(parameters);
        }

        public override ECParameters ExportExplicitParameters(bool includePrivateParameters) =>
            ECOpenSsl.ExportExplicitParameters(GetKey(), includePrivateParameters);

        public override ECParameters ExportParameters(bool includePrivateParameters) =>
            ECOpenSsl.ExportParameters(GetKey(), includePrivateParameters);

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

        private SafeEcKeyHandle GetKey()
        {
            ThrowIfDisposed();
            return _key.Value;
        }

        static partial void ThrowIfNotSupported();
    }
}
