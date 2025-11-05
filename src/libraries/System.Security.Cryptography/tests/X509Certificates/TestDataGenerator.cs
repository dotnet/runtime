// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    internal static class TestDataGenerator
    {
        internal static void MakeTestChain3(
            out X509Certificate2 endEntityCert,
            out X509Certificate2 intermediateCert,
            out X509Certificate2 rootCert,
            IEnumerable<X509Extension> endEntityExtensions = null,
            IEnumerable<X509Extension> intermediateExtensions = null,
            IEnumerable<X509Extension> rootExtensions = null,
            [CallerMemberName] string testName = null)
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
                MakeTestChain(
                    keys,
                    certs,
                    endEntityExtensions,
                    intermediateExtensions,
                    rootExtensions,
                    testName);

                endEntityCert = certs[0];
                intermediateCert = certs[1];
                rootCert = certs[2];
            }
        }


        internal static void MakeTestChain4(
            out X509Certificate2 endEntityCert,
            out X509Certificate2 intermediateCert1,
            out X509Certificate2 intermediateCert2,
            out X509Certificate2 rootCert,
            IEnumerable<X509Extension> endEntityExtensions = null,
            IEnumerable<X509Extension> intermediateExtensions = null,
            IEnumerable<X509Extension> rootExtensions = null,
            [CallerMemberName] string testName = null)
        {
            using (RSA rootKey = RSA.Create())
            using (RSA intermediateKey = RSA.Create())
            using (RSA endEntityKey = RSA.Create())
            {
                ReadOnlySpan<RSA> keys = new[]
                {
                    rootKey,
                    intermediateKey,
                    intermediateKey,
                    endEntityKey,
                };

                Span<X509Certificate2> certs = new X509Certificate2[keys.Length];
                MakeTestChain(
                    keys,
                    certs,
                    endEntityExtensions,
                    intermediateExtensions,
                    rootExtensions,
                    testName);

                endEntityCert = certs[0];
                intermediateCert1 = certs[1];
                intermediateCert2 = certs[2];
                rootCert = certs[3];
            }
        }

        internal static void MakeTestChain4(
            out X509Certificate2 endEntityCert,
            out X509Certificate2 intermediateCert1,
            out X509Certificate2 intermediateCert2,
            out X509Certificate2 rootCert,
            IEnumerable<X509Extension> endEntityExtensions,
            IEnumerable<X509Extension> intermediate1Extensions,
            IEnumerable<X509Extension> intermediate2Extensions,
            IEnumerable<X509Extension> rootExtensions,
            [CallerMemberName] string testName = null)
        {
            using (RSA rootKey = RSA.Create())
            using (RSA intermediateKey1 = RSA.Create())
            using (RSA intermediateKey2 = RSA.Create())
            using (RSA endEntityKey = RSA.Create())
            {
                ReadOnlySpan<RSA> keys = new[]
                {
                    rootKey,
                    intermediateKey1,
                    intermediateKey2,
                    endEntityKey,
                };

                Span<X509Certificate2> certs = new X509Certificate2[keys.Length];

                // Build root
                HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;
                RSASignaturePadding signaturePadding = RSASignaturePadding.Pkcs1;
                int rootIndex = keys.Length - 1;
                DateTimeOffset eeStart = DateTimeOffset.UtcNow.AddDays(-7);
                DateTimeOffset eeEnd = eeStart.AddDays(45);
                TimeSpan notBeforeInterval = TimeSpan.FromDays(30);
                TimeSpan notAfterInterval = TimeSpan.FromDays(90);

                CertificateRequest rootReq = new CertificateRequest(
                    $"CN=Test Root, O=\"{testName}\"",
                    keys[rootIndex],
                    hashAlgorithm,
                    signaturePadding);

                rootExtensions ??= new X509Extension[] {
                    new X509BasicConstraintsExtension(true, false, 0, true),
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.CrlSign |
                            X509KeyUsageFlags.KeyCertSign |
                            X509KeyUsageFlags.DigitalSignature,
                        false)
                };

                foreach (X509Extension ext in rootExtensions)
                {
                    rootReq.CertificateExtensions.Add(ext);
                }

                X509SignatureGenerator lastGenerator = X509SignatureGenerator.CreateForRSA(keys[rootIndex], RSASignaturePadding.Pkcs1);
                X500DistinguishedName lastSubject = rootReq.SubjectName;

                certs[rootIndex] = rootReq.Create(
                    lastSubject,
                    lastGenerator,
                    eeStart - (notBeforeInterval * rootIndex),
                    eeEnd + (notAfterInterval * rootIndex),
                    CreateSerial());

                CertificateRequest int2Req = new CertificateRequest(
                    $"CN=Intermediate Layer 2, O=\"{testName}\"",
                    keys[2],
                    hashAlgorithm,
                    signaturePadding);
                intermediate2Extensions ??= new X509Extension[] {
                    new X509BasicConstraintsExtension(true, false, 0, true),
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.CrlSign |
                            X509KeyUsageFlags.KeyCertSign |
                            X509KeyUsageFlags.DigitalSignature,
                        false)
                };
                foreach (X509Extension ext in intermediate2Extensions) int2Req.CertificateExtensions.Add(ext);
                certs[2] = int2Req.Create(
                    lastSubject,
                    lastGenerator,
                    eeStart - (notBeforeInterval * 2),
                    eeEnd + (notAfterInterval * 2),
                    CreateSerial());
                lastSubject = int2Req.SubjectName;
                lastGenerator = X509SignatureGenerator.CreateForRSA(keys[2], RSASignaturePadding.Pkcs1);

                CertificateRequest int1Req = new CertificateRequest(
                    $"CN=Intermediate Layer 1, O=\"{testName}\"",
                    keys[1],
                    hashAlgorithm,
                    signaturePadding);
                intermediate1Extensions ??= new X509Extension[] {
                    new X509BasicConstraintsExtension(true, false, 0, true),
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.CrlSign |
                            X509KeyUsageFlags.KeyCertSign |
                            X509KeyUsageFlags.DigitalSignature,
                        false)
                };
                foreach (X509Extension ext in intermediate1Extensions) int1Req.CertificateExtensions.Add(ext);
                certs[1] = int1Req.Create(
                    lastSubject,
                    lastGenerator,
                    eeStart - (notBeforeInterval * 1),
                    eeEnd + (notAfterInterval * 1),
                    CreateSerial());
                lastSubject = int1Req.SubjectName;
                lastGenerator = X509SignatureGenerator.CreateForRSA(keys[1], RSASignaturePadding.Pkcs1);

                CertificateRequest eeReq = new CertificateRequest(
                    $"CN=End-Entity, O=\"{testName}\"",
                    keys[0],
                    hashAlgorithm,
                    signaturePadding);
                endEntityExtensions ??= new X509Extension[] {
                    new X509BasicConstraintsExtension(false, false, 0, true),
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature |
                            X509KeyUsageFlags.NonRepudiation |
                            X509KeyUsageFlags.KeyEncipherment,
                        false)
                };
                foreach (X509Extension ext in endEntityExtensions) eeReq.CertificateExtensions.Add(ext);
                certs[0] = eeReq.Create(lastSubject, lastGenerator, eeStart, eeEnd, CreateSerial());

                endEntityCert = certs[0];
                intermediateCert1 = certs[1];
                intermediateCert2 = certs[2];
                rootCert = certs[3];
            }
        }

        internal static void MakeTestChain(
            ReadOnlySpan<RSA> keys,
            Span<X509Certificate2> certs,
            IEnumerable<X509Extension> endEntityExtensions,
            IEnumerable<X509Extension> intermediateExtensions,
            IEnumerable<X509Extension> rootExtensions,
            string testName)
        {
            if (keys.Length < 2)
                throw new ArgumentException(nameof(keys));
            if (keys.Length != certs.Length)
                throw new ArgumentException(nameof(certs));

            rootExtensions ??= new X509Extension[] {
                new X509BasicConstraintsExtension(true, false, 0, true),
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.CrlSign |
                        X509KeyUsageFlags.KeyCertSign |
                        X509KeyUsageFlags.DigitalSignature,
                    false)
            };

            intermediateExtensions ??= new X509Extension[] {
                new X509BasicConstraintsExtension(true, false, 0, true),
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.CrlSign |
                        X509KeyUsageFlags.KeyCertSign |
                        X509KeyUsageFlags.DigitalSignature,
                    false)
            };

            endEntityExtensions ??= new X509Extension[] {
                new X509BasicConstraintsExtension(false, false, 0, true),
                new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature |
                    X509KeyUsageFlags.NonRepudiation |
                    X509KeyUsageFlags.KeyEncipherment,
                false)
            };

            TimeSpan notBeforeInterval = TimeSpan.FromDays(30);
            TimeSpan notAfterInterval = TimeSpan.FromDays(90);
            DateTimeOffset eeStart = DateTimeOffset.UtcNow.AddDays(-7);
            DateTimeOffset eeEnd = eeStart.AddDays(45);
            byte[] serialBuf = new byte[16];

            int rootIndex = keys.Length - 1;

            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;
            RSASignaturePadding signaturePadding = RSASignaturePadding.Pkcs1;

            CertificateRequest rootReq = new CertificateRequest(
                $"CN=Test Root, O=\"{testName}\"",
                keys[rootIndex],
                hashAlgorithm,
                signaturePadding);

            foreach (X509Extension extension in rootExtensions)
            {
                rootReq.CertificateExtensions.Add(extension);
            }

            X509SignatureGenerator lastGenerator = X509SignatureGenerator.CreateForRSA(keys[rootIndex], RSASignaturePadding.Pkcs1);
            X500DistinguishedName lastSubject = rootReq.SubjectName;

            certs[rootIndex] = rootReq.Create(
                lastSubject,
                lastGenerator,
                eeStart - (notBeforeInterval * rootIndex),
                eeEnd + (notAfterInterval * rootIndex),
                CreateSerial());

            int presentationNumber = 0;

            for (int i = rootIndex - 1; i > 0; i--)
            {
                presentationNumber++;

                CertificateRequest intermediateReq = new CertificateRequest(
                    $"CN=Intermediate Layer {presentationNumber}, O=\"{testName}\"",
                    keys[i],
                    hashAlgorithm,
                    signaturePadding);

                foreach (X509Extension extension in intermediateExtensions)
                {
                    intermediateReq.CertificateExtensions.Add(extension);
                }

                certs[i] = intermediateReq.Create(
                    lastSubject,
                    lastGenerator,
                    eeStart - (notBeforeInterval * i),
                    eeEnd + (notAfterInterval * i),
                    CreateSerial());

                lastSubject = intermediateReq.SubjectName;
                lastGenerator = X509SignatureGenerator.CreateForRSA(keys[i], RSASignaturePadding.Pkcs1);
            }

            CertificateRequest eeReq = new CertificateRequest(
                $"CN=End-Entity, O=\"{testName}\"",
                keys[0],
                hashAlgorithm,
                signaturePadding);

            foreach (X509Extension extension in endEntityExtensions)
            {
                eeReq.CertificateExtensions.Add(extension);
            }

            certs[0] = eeReq.Create(lastSubject, lastGenerator, eeStart, eeEnd, CreateSerial());
        }

        private static byte[] CreateSerial()
        {
            byte[] bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }
    }
}
