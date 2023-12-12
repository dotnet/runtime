// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography
{
    internal sealed class SecKeyPair : IDisposable
    {
        internal SafeSecKeyRefHandle PublicKey { get; private set; }
        internal SafeSecKeyRefHandle? PrivateKey { get; private set; }
        private SafeSecCertificateHandle? _owningCertificate;

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

            // We don't dispose here. Callers that supply a certificate to the key pair are expected to pass
            // an existing certificate with that has been incremented.
            SafeSecCertificateHandle? handle = Interlocked.Exchange(ref _owningCertificate, null);
            handle?.DangerousRelease();
        }

        internal static SecKeyPair PublicPrivatePair(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle privateKey)
        {
            if (publicKey == null || publicKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(publicKey));
            if (privateKey == null || privateKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(privateKey));

            return new SecKeyPair(publicKey, privateKey);
        }

        internal static SecKeyPair PublicPrivatePair(
            SafeSecKeyRefHandle publicKey,
            SafeSecKeyRefHandle privateKey,
            SafeSecCertificateHandle owningCertificate)
        {
            if (owningCertificate is null || owningCertificate.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(owningCertificate));

            SecKeyPair pair = PublicPrivatePair(publicKey, privateKey);
            pair._owningCertificate = owningCertificate;
            return pair;
        }

        internal static SecKeyPair PublicOnly(SafeSecKeyRefHandle publicKey)
        {
            if (publicKey == null || publicKey.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(publicKey));

            return new SecKeyPair(publicKey, null);
        }
    }
}
