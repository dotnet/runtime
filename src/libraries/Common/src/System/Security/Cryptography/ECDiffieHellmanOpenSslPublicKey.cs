// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDiffieHellmanImplementation
    {
#endif
        internal sealed class ECDiffieHellmanOpenSslPublicKey : ECDiffieHellmanPublicKey
        {
            private readonly ECOpenSsl _key;

            internal ECDiffieHellmanOpenSslPublicKey(SafeEvpPKeyHandle pkeyHandle)
            {
                if (pkeyHandle == null)
                    throw new ArgumentNullException(nameof(pkeyHandle));
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

            public override string ToXmlString()
            {
                throw new PlatformNotSupportedException();
            }

            public override byte[] ToByteArray()
            {
                throw new PlatformNotSupportedException();
            }

            public override ECParameters ExportExplicitParameters() =>
                ECOpenSsl.ExportExplicitParameters(_key.Value, includePrivateParameters: false);

            public override ECParameters ExportParameters() =>
                ECOpenSsl.ExportParameters(_key.Value, includePrivateParameters: false);

            internal bool HasCurveName => Interop.Crypto.EcKeyHasCurveName(_key.Value);

            internal int KeySize => _key.KeySize;

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _key?.Dispose();
                }

                base.Dispose(disposing);
            }

            internal SafeEvpPKeyHandle DuplicateKeyHandle()
            {
                SafeEcKeyHandle currentKey = _key.Value;
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
        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
