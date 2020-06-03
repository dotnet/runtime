// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Security.Cryptography.Apple;

namespace System.Security.Cryptography
{
    internal sealed class SecKeyPair : IDisposable
    {
        internal SafeSecKeyRefHandle PublicKey { get; private set; }
        internal SafeSecKeyRefHandle? PrivateKey { get; private set; }
        internal SafeSecKeyRefHandle? PublicDataKey { get; private set; }
        internal SafeSecKeyRefHandle? PrivateDataKey { get; private set; }

        private SecKeyPair(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle? privateKey, SafeSecKeyRefHandle? publicDataKey, SafeSecKeyRefHandle? privateDataKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
            PublicDataKey = publicDataKey;
            PrivateDataKey = privateDataKey;
        }

        public void Dispose()
        {
            PrivateKey?.Dispose();
            PrivateKey = null;
            PublicKey?.Dispose();
            PublicKey = null!;
            PublicDataKey?.Dispose();
            PublicDataKey = null!;
            PrivateDataKey?.Dispose();
            PrivateDataKey = null!;
        }

        internal static SecKeyPair PublicPrivatePair(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle privateKey)
        {
            if (publicKey == null || publicKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(publicKey));
            if (privateKey == null || privateKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(privateKey));

            return new SecKeyPair(publicKey, privateKey, null, null);
        }

        internal static SecKeyPair PublicPrivatePair(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle privateKey, SafeSecKeyRefHandle publicDataKey, SafeSecKeyRefHandle privateDataKey)
        {
            if (publicKey == null || publicKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(publicKey));
            if (privateKey == null || privateKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(privateKey));
            if (publicDataKey == null || publicDataKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(publicDataKey));
            if (privateDataKey == null || privateDataKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(privateDataKey));

            return new SecKeyPair(publicKey, privateKey, publicDataKey, privateDataKey);
        }

        internal static SecKeyPair PublicOnly(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle? publicDataKey = null)
        {
            if (publicKey == null || publicKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(publicKey));
            if (publicDataKey != null && publicDataKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(publicDataKey));

            return new SecKeyPair(publicKey, null, publicDataKey, null);
        }
    }
}
