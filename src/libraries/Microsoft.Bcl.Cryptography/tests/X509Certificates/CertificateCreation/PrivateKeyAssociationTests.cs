// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public static partial class PrivateKeyAssociationTests
    {
        private static partial Func<X509Certificate2, SlhDsa, X509Certificate2> CopyWithPrivateKey_SlhDsa =>
            X509CertificateKeyAccessors.CopyWithPrivateKey;

        private static partial Func<X509Certificate2, SlhDsa> GetSlhDsaPublicKey =>
            X509CertificateKeyAccessors.GetSlhDsaPublicKey;

        private static partial Func<X509Certificate2, SlhDsa> GetSlhDsaPrivateKey =>
            X509CertificateKeyAccessors.GetSlhDsaPrivateKey;

        private static partial void CheckCopyWithPrivateKey<TKey>(
            X509Certificate2 cert,
            X509Certificate2 wrongAlgorithmCert,
            TKey correctPrivateKey,
            IEnumerable<Func<TKey>> incorrectKeys,
            Func<X509Certificate2, TKey, X509Certificate2> copyWithPrivateKey,
            Func<X509Certificate2, TKey> getPublicKey,
            Func<X509Certificate2, TKey> getPrivateKey,
            Action<TKey, TKey> keyProver)
            where TKey : class, IDisposable;
    }
}
