// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDiffieHellmanImplementation
    {
#endif
        public sealed partial class ECDiffieHellmanOpenSsl : ECDiffieHellman
        {
            private ECOpenSsl _key;

            public ECDiffieHellmanOpenSsl(ECCurve curve)
            {
                ThrowIfNotSupported();
                _key = new ECOpenSsl(curve);
                KeySizeValue = _key.KeySize;
            }

            public ECDiffieHellmanOpenSsl()
                : this(521)
            {
            }

            public ECDiffieHellmanOpenSsl(int keySize)
            {
                ThrowIfNotSupported();
                base.KeySize = keySize;
                _key = new ECOpenSsl(this);
            }

            public override KeySizes[] LegalKeySizes =>
                new[] {
                    new KeySizes(minSize: 256, maxSize: 384, skipSize: 128),
                    new KeySizes(minSize: 521, maxSize: 521, skipSize: 0)
                };

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
                    return new ECDiffieHellmanOpenSslPublicKey(_key.UpRefKeyHandle());
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

            private void ThrowIfDisposed()
            {
                if (_key == null)
                {
                    throw new ObjectDisposedException(
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
                        nameof(ECDiffieHellman)
#else
                        nameof(ECDiffieHellmanOpenSsl)
#endif
                    );
                }
            }

            private SafeEcKeyHandle GetKey()
            {
                ThrowIfDisposed();
                return _key.Value;
            }

            static partial void ThrowIfNotSupported();
        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
