// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public static partial class PrivateKeyAssociationTests
    {
        private static partial Func<X509Certificate2, MLKem, X509Certificate2> CopyWithPrivateKey_MLKem =>
            X509CertificateKeyAccessors.CopyWithPrivateKey;

        private static partial Func<X509Certificate2, MLKem> GetMLKemPublicKey =>
            X509CertificateKeyAccessors.GetMLKemPublicKey;

        private static partial Func<X509Certificate2, MLKem> GetMLKemPrivateKey =>
            X509CertificateKeyAccessors.GetMLKemPrivateKey;

        private static partial Func<X509Certificate2, SlhDsa, X509Certificate2> CopyWithPrivateKey_SlhDsa =>
            X509CertificateKeyAccessors.CopyWithPrivateKey;

        private static partial Func<X509Certificate2, SlhDsa> GetSlhDsaPublicKey =>
            X509CertificateKeyAccessors.GetSlhDsaPublicKey;

        private static partial Func<X509Certificate2, SlhDsa> GetSlhDsaPrivateKey =>
            X509CertificateKeyAccessors.GetSlhDsaPrivateKey;

        private static partial Func<X509Certificate2, MLDsa, X509Certificate2> CopyWithPrivateKey_MLDsa =>
            X509CertificateKeyAccessors.CopyWithPrivateKey;

        private static partial Func<X509Certificate2, MLDsa> GetMLDsaPublicKey =>
            X509CertificateKeyAccessors.GetMLDsaPublicKey;

        private static partial Func<X509Certificate2, MLDsa> GetMLDsaPrivateKey =>
            X509CertificateKeyAccessors.GetMLDsaPrivateKey;

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

        [Fact]
        public static void ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>("certificate", () => X509CertificateKeyAccessors.CopyWithPrivateKey(null, default(MLKem)));
            Assert.Throws<ArgumentNullException>("certificate", () => X509CertificateKeyAccessors.CopyWithPrivateKey(null, default(MLDsa)));
            Assert.Throws<ArgumentNullException>("certificate", () => X509CertificateKeyAccessors.CopyWithPrivateKey(null, default(SlhDsa)));
            Assert.Throws<ArgumentNullException>("certificate", () => X509CertificateKeyAccessors.GetMLKemPublicKey(null));
            Assert.Throws<ArgumentNullException>("certificate", () => X509CertificateKeyAccessors.GetMLDsaPublicKey(null));
            Assert.Throws<ArgumentNullException>("certificate", () => X509CertificateKeyAccessors.GetSlhDsaPublicKey(null));

#pragma warning disable SYSLIB0026 // X509Certificate and X509Certificate2 are immutable
            // This constructor is deprecated, but we use it here for test purposes.
            using (X509Certificate2 cert = new X509Certificate2())
#pragma warning restore SYSLIB0026 // X509Certificate and X509Certificate2 are immutable
            {
                Assert.Throws<ArgumentNullException>("privateKey", () => X509CertificateKeyAccessors.CopyWithPrivateKey(cert, default(MLKem)));
                Assert.Throws<ArgumentNullException>("privateKey", () => X509CertificateKeyAccessors.CopyWithPrivateKey(cert, default(MLDsa)));
                Assert.Throws<ArgumentNullException>("privateKey", () => X509CertificateKeyAccessors.CopyWithPrivateKey(cert, default(SlhDsa)));
            }
        }
    }
}
