// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed class ECDiffieHellmanOpenSslPublicKey : ECDiffieHellmanPublicKey
    {
        private SafeEvpPKeyHandle? _key;

        internal ECDiffieHellmanOpenSslPublicKey(SafeEvpPKeyHandle pkeyHandle)
        {
            ArgumentNullException.ThrowIfNull(pkeyHandle);

            if (pkeyHandle.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(pkeyHandle));

            _key = pkeyHandle.DuplicateHandle();
        }

        internal ECDiffieHellmanOpenSslPublicKey(ECParameters parameters)
        {
            _key = ECOpenSsl.ImportECKey(parameters, out _);
        }

#pragma warning disable 0672 // Member overrides an obsolete member.
        public override string ToXmlString()
#pragma warning restore 0672
        {
            throw new PlatformNotSupportedException();
        }

#pragma warning disable 0672 // Member overrides an obsolete member.
        public override byte[] ToByteArray()
#pragma warning restore 0672
        {
            throw new PlatformNotSupportedException();
        }

        public override ECParameters ExportExplicitParameters() =>
            ECOpenSsl.ExportExplicitParameters(GetKey(), includePrivateParameters: false);

        public override ECParameters ExportParameters() =>
            ECOpenSsl.ExportParameters(GetKey(), includePrivateParameters: false);

        internal bool HasCurveName => Interop.Crypto.EvpPKeyHasCurveName(GetKey());

        internal bool HasExplicitEncoding
        {
            get
            {
                ThrowIfDisposed();
                return Interop.Crypto.EvpPKeyEcHasExplicitEncoding(GetKey());
            }
        }

        internal int KeySize
        {
            get
            {
                ThrowIfDisposed();

                int keySize = Interop.Crypto.EvpPKeyGetEcKeySize(_key);

                if (keySize == 0)
                {
                    throw new CryptographicException(SR.Cryptography_InvalidHandle);
                }

                return keySize;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key?.Dispose();
                _key = null;
            }

            base.Dispose(disposing);
        }

        internal SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            return GetKey().DuplicateHandle();
        }

        [MemberNotNull(nameof(_key))]
        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_key is null, this);
        }

        private SafeEvpPKeyHandle GetKey()
        {
            ThrowIfDisposed();
            return _key;
        }
    }
}
