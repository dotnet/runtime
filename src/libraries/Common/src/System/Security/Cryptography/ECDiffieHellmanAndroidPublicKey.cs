// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal static partial class ECDiffieHellmanImplementation
    {
        internal sealed class ECDiffieHellmanAndroidPublicKey : ECDiffieHellmanPublicKey
        {
            private ECAndroid? _key;

            internal ECDiffieHellmanAndroidPublicKey(SafeEcKeyHandle ecKeyHandle)
            {
                ArgumentNullException.ThrowIfNull(ecKeyHandle);

                if (ecKeyHandle.IsInvalid)
                    throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(ecKeyHandle));

                _key = new ECAndroid(ecKeyHandle.DuplicateHandle());
            }

            internal ECDiffieHellmanAndroidPublicKey(ECParameters parameters)
            {
                _key = new ECAndroid(parameters);
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
                    _key = null;
                }

                base.Dispose(disposing);
            }

            internal SafeEcKeyHandle DuplicateKeyHandle()
            {
                return GetKey().DuplicateHandle();
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
        }
    }
}
