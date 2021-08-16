// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Apple;

namespace System.Security.Cryptography
{
    internal sealed class SecKeyPair : IDisposable
    {
        internal SafeSecKeyRefHandle PublicKey { get; private set; }
        internal SafeSecKeyRefHandle? PrivateKey { get; private set; }

        private SecKeyPair(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle? privateKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public void Dispose()
        {
            PrivateKey?.Dispose();
            PrivateKey = null;
            PublicKey?.Dispose();
            PublicKey = null!;
        }

        internal static SecKeyPair PublicPrivatePair(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle privateKey)
        {
            if (publicKey == null || publicKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(publicKey));
            if (privateKey == null || privateKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(privateKey));

            return new SecKeyPair(publicKey, privateKey);
        }

        internal static SecKeyPair PublicOnly(SafeSecKeyRefHandle publicKey)
        {
            if (publicKey == null || publicKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(publicKey));

            return new SecKeyPair(publicKey, null);
        }
    }
}
