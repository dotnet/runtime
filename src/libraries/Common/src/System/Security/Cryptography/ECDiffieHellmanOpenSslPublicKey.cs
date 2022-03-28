// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed class ECDiffieHellmanOpenSslPublicKey : ECDiffieHellmanPublicKey
    {
        private ECOpenSsl _key;

        internal ECDiffieHellmanOpenSslPublicKey(SafeEvpPKeyHandle pkeyHandle!!)
        {
            if (pkeyHandle.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(pkeyHandle));

            // If ecKey is valid it has already been up-ref'd, so we can just use this handle as-is.
            SafeEcKeyHandle key = Interop.Crypto.EvpPkeyGetEcKey(pkeyHandle);

            if (key.IsInvalid)
            {
                key.Dispose();
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            _key = new ECOpenSsl(key);
        }

        internal ECDiffieHellmanOpenSslPublicKey(ECParameters parameters)
        {
            _key = new ECOpenSsl(parameters);
        }

#pragma warning disable 0672 // Member overrides an obsolete member.
        public override string ToXmlString()
#pragma warning restore 0672
        {
            throw new PlatformNotSupportedException();
        }

        public override byte[] ToByteArray()
        {
            throw new PlatformNotSupportedException();
        }

        public override ECParameters ExportExplicitParameters() =>
            ECOpenSsl.ExportExplicitParameters(GetKey(), includePrivateParameters: false);

        public override ECParameters ExportParameters() =>
            ECOpenSsl.ExportParameters(GetKey(), includePrivateParameters: false);

        internal bool HasCurveName => Interop.Crypto.EcKeyHasCurveName(GetKey());

        internal int KeySize
        {
            get
            {
                ThrowIfDisposed();
                return _key.KeySize;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key?.Dispose();
                _key = null!;
            }

            base.Dispose(disposing);
        }

        internal SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            SafeEcKeyHandle currentKey = GetKey();
            SafeEvpPKeyHandle pkeyHandle = Interop.Crypto.EvpPkeyCreate();

            try
            {
                // Wrapping our key in an EVP_PKEY will up_ref our key.
                // When the EVP_PKEY is Disposed it will down_ref the key.
                // So everything should be copacetic.
                if (!Interop.Crypto.EvpPkeySetEcKey(pkeyHandle, currentKey))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return pkeyHandle;
            }
            catch
            {
                pkeyHandle.Dispose();
                throw;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_key == null)
            {
                throw new ObjectDisposedException(nameof(ECDiffieHellmanOpenSslPublicKey));
            }
        }

        private SafeEcKeyHandle GetKey()
        {
            ThrowIfDisposed();
            return _key.Value;
        }
    }
}
