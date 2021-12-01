// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal static partial class ECDiffieHellmanImplementation
    {
        internal sealed class ECDiffieHellmanAndroidPublicKey : ECDiffieHellmanPublicKey
        {
            private ECAndroid _key;

            internal ECDiffieHellmanAndroidPublicKey(SafeEcKeyHandle ecKeyHandle)
            {
                if (ecKeyHandle == null)
                    throw new ArgumentNullException(nameof(ecKeyHandle));
                if (ecKeyHandle.IsInvalid)
                    throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(ecKeyHandle));

                _key = new ECAndroid(ecKeyHandle.DuplicateHandle());
            }

            internal ECDiffieHellmanAndroidPublicKey(ECParameters parameters)
            {
                _key = new ECAndroid(parameters);
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
                ECAndroid.ExportExplicitParameters(GetKey(), includePrivateParameters: false);

            public override ECParameters ExportParameters() =>
                ECAndroid.ExportParameters(GetKey(), includePrivateParameters: false);

            internal bool HasCurveName => Interop.AndroidCrypto.EcKeyHasCurveName(GetKey());

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

            internal SafeEcKeyHandle DuplicateKeyHandle()
            {
                return GetKey().DuplicateHandle();
            }

            private void ThrowIfDisposed()
            {
                if (_key == null)
                {
                    throw new ObjectDisposedException(nameof(ECDiffieHellmanPublicKey));
                }
            }

            private SafeEcKeyHandle GetKey()
            {
                ThrowIfDisposed();
                return _key.Value;
            }
        }
    }
}
