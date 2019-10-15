// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates.Tests
{
    internal static class TestDataGenerator
    {
        internal static void MakeTestChain3(
            out X509Certificate2 endEntityCert,
            out X509Certificate2 intermediateCert,
            out X509Certificate2 rootCert)
        {
            using (RSA rootKey = RSA.Create())
            using (RSA intermediateKey = RSA.Create())
            using (RSA endEntityKey = RSA.Create())
            {
                ReadOnlySpan<RSA> keys = new[]
                {
                    rootKey,
                    intermediateKey,
                    endEntityKey,
                };

                Span<X509Certificate2> certs = new X509Certificate2[keys.Length];
                MakeTestChain(keys, certs);

                endEntityCert = certs[0];
                intermediateCert = certs[1];
                rootCert = certs[2];
            }
        }

        internal static void MakeTestChain(
            ReadOnlySpan<RSA> keys,
            Span<X509Certificate2> certs,
            string eeName = "CN=End-Entity",
            OidCollection eeEku = null)
        {
            if (keys.Length < 2)
                throw new ArgumentException(nameof(keys));
            if (keys.Length != certs.Length)
                throw new ArgumentException(nameof(certs));
            if (string.IsNullOrEmpty(eeName))
                throw new ArgumentOutOfRangeException(nameof(eeName));

            var caUnlimited = new X509BasicConstraintsExtension(true, false, 0, true);
            var eeConstraint = new X509BasicConstraintsExtension(false, false, 0, true);

            var caUsage = new X509KeyUsageExtension(
                X509KeyUsageFlags.CrlSign |
                    X509KeyUsageFlags.KeyCertSign |
                    X509KeyUsageFlags.DigitalSignature,
                false);

            var eeUsage = new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature |
                    X509KeyUsageFlags.NonRepudiation |
                    X509KeyUsageFlags.KeyEncipherment,
                false);

            TimeSpan notBeforeInterval = TimeSpan.FromDays(30);
            TimeSpan notAfterInterval = TimeSpan.FromDays(90);
            DateTimeOffset eeStart = DateTimeOffset.UtcNow.AddDays(-7);
            DateTimeOffset eeEnd = eeStart.AddDays(45);
            byte[] serialBuf = new byte[16];

            int rootIndex = keys.Length - 1;

            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;
            RSASignaturePadding signaturePadding = RSASignaturePadding.Pkcs1;

            CertificateRequest rootReq = new CertificateRequest(
                "CN=Test Root",
                keys[rootIndex],
                hashAlgorithm,
                signaturePadding);

            rootReq.CertificateExtensions.Add(caUnlimited);
            rootReq.CertificateExtensions.Add(caUsage);

            X509Certificate2 lastWithKey = rootReq.CreateSelfSigned(
                eeStart - (notBeforeInterval * rootIndex),
                eeEnd + (notAfterInterval * rootIndex));

            certs[rootIndex] = new X509Certificate2(lastWithKey.RawData);

            int presentationNumber = 0;

            for (int i = rootIndex - 1; i > 0; i--)
            {
                presentationNumber++;

                CertificateRequest intermediateReq = new CertificateRequest(
                    $"CN=Intermediate Layer {presentationNumber}",
                    keys[i],
                    hashAlgorithm,
                    signaturePadding);

                intermediateReq.CertificateExtensions.Add(caUnlimited);
                intermediateReq.CertificateExtensions.Add(caUsage);

                // Leave serialBuf[0] as 0 to avoid a realloc in the signer
                RandomNumberGenerator.Fill(serialBuf.AsSpan(1));

                certs[i] = intermediateReq.Create(
                    lastWithKey,
                    eeStart - (notBeforeInterval * i),
                    eeEnd + (notAfterInterval * i),
                    serialBuf);

                lastWithKey.Dispose();
                lastWithKey = certs[i].CopyWithPrivateKey(keys[i]);
            }

            CertificateRequest eeReq = new CertificateRequest(
                eeName,
                keys[0],
                hashAlgorithm,
                signaturePadding);

            eeReq.CertificateExtensions.Add(eeConstraint);
            eeReq.CertificateExtensions.Add(eeUsage);

            if (eeEku != null)
            {
                eeReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eeEku, false));
            }

            // Leave serialBuf[0] as 0 to avoid a realloc in the signer
            RandomNumberGenerator.Fill(serialBuf.AsSpan(1));

            certs[0] = eeReq.Create(lastWithKey, eeStart, eeEnd, serialBuf);
            lastWithKey.Dispose();
        }
    }
}
