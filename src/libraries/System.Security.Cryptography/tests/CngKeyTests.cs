// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class CngKeyTests
    {
        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsVbsAvailable))]
        [InlineData(CngKeyCreationOptions.PreferVbs)]
        [InlineData(CngKeyCreationOptions.RequireVbs)]
        [InlineData(CngKeyCreationOptions.UsePerBootKey)]
        public void CreateVbsKey_SignAndVerify(CngKeyCreationOptions creationOption)
        {
            using (CngKeyWrapper key = CngKeyWrapper.CreateMicrosoftSoftwareKeyStorageProvider(
                CngAlgorithm.ECDsaP256,
                creationOption,
                keySuffix: creationOption.ToString()))
            {
                SignAndVerifyECDsa(key.Key);
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsVbsAvailable))]
        [InlineData(CngKeyCreationOptions.PreferVbs)]
        [InlineData(CngKeyCreationOptions.RequireVbs)]
        [InlineData(CngKeyCreationOptions.UsePerBootKey)]
        public void CreateVbsKey_KeyIsNotExportable(CngKeyCreationOptions creationOption)
        {
            using (CngKeyWrapper key = CngKeyWrapper.CreateMicrosoftSoftwareKeyStorageProvider(
                CngAlgorithm.ECDsaP256,
                creationOption,
                keySuffix: creationOption.ToString()))
            {
                using (ECDsaCng ecdsa = new ECDsaCng(key.Key))
                {
                    Assert.ThrowsAny<CryptographicException>(() => ecdsa.ExportExplicitParameters(includePrivateParameters: true));
                }
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsVbsAvailable))]
        [InlineData(CngKeyCreationOptions.PreferVbs)]
        [InlineData(CngKeyCreationOptions.RequireVbs)]
        [InlineData(CngKeyCreationOptions.UsePerBootKey)]
        [InlineData(CngKeyCreationOptions.PreferVbs | CngKeyCreationOptions.UsePerBootKey)]
        [InlineData(CngKeyCreationOptions.RequireVbs | CngKeyCreationOptions.UsePerBootKey)]
        public void CreateVbsKey_SoftwareKeyStorageProviderFlagsOnWrongProvider(CngKeyCreationOptions creationOption)
        {
            Assert.ThrowsAny<CryptographicException>(() => CngKeyWrapper.CreateMicrosoftPlatformCryptoProvider(
                    CngAlgorithm.ECDsaP256,
                    creationOption: creationOption,
                    keySuffix: creationOption.ToString()));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void Handle_ConcurrentFirstAccess()
        {
            const int IterationCount = 10;
            const int ThreadCount = 8;
            TimeSpan timeout = TimeSpan.FromSeconds(30);

            for (int iteration = 0; iteration < IterationCount; iteration++)
            {
                using CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256);
                using Barrier barrier = new Barrier(ThreadCount);
                var handles = new SafeNCryptKeyHandle?[ThreadCount];
                var exceptions = new Exception?[ThreadCount];
                var threads = new Thread[ThreadCount];

                for (int i = 0; i < threads.Length; i++)
                {
                    int index = i;
                    threads[i] = new Thread(() =>
                    {
                        try
                        {
                            Assert.True(barrier.SignalAndWait(timeout), "Timed out waiting for concurrent handle access.");
                            handles[index] = key.Handle;
                        }
                        catch (Exception e)
                        {
                            exceptions[index] = e;
                        }
                    });
                    threads[i].IsBackground = true;
                    threads[i].Start();
                }

                foreach (Thread thread in threads)
                {
                    Assert.True(thread.Join(timeout), "Timed out waiting for handle access thread.");
                }

                Assert.All(exceptions, Assert.Null);
                Assert.All(handles, Assert.NotNull);

                key.Dispose();

                for (int i = 0; i < handles.Length - 1; i++)
                {
                    handles[i]!.Dispose();
                }

                using SafeNCryptKeyHandle remainingHandle = handles[^1]!;
                using CngKey remainingKey = CngKey.Open(
                    remainingHandle,
                    CngKeyHandleOpenOptions.EphemeralKey);
                Assert.Equal(CngAlgorithm.ECDsaP256, remainingKey.Algorithm);
            }
        }

        private static void SignAndVerifyECDsa(CngKey key)
        {
            using (ECDsaCng ecdsa = new ECDsaCng(key))
            {
                byte[] data = { 12, 11, 02, 08, 25, 14, 11, 18, 16 };

                // using key directly
                byte[] signature = ecdsa.SignData(data, HashAlgorithmName.SHA256);
                VerifyTests(ecdsa, data, signature);

                // through cert
                CertificateRequest req = new CertificateRequest("CN=potato", ecdsa, HashAlgorithmName.SHA256);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                using (X509Certificate2 cert = req.CreateSelfSigned(now, now.AddHours(1)))
                using (ECDsa certKey = cert.GetECDsaPrivateKey())
                using (ECDsa certPubKey = cert.GetECDsaPublicKey())
                {
                    Assert.NotNull(certKey);
                    Assert.NotNull(certPubKey);

                    VerifyTests(certPubKey, data, signature);
                    VerifyTests(certKey, data, signature);

                    Assert.ThrowsAny<CryptographicException>(() => certPubKey.SignData(data, HashAlgorithmName.SHA256));
                    signature = certKey.SignData(data, HashAlgorithmName.SHA256);

                    VerifyTests(ecdsa, data, signature);
                    VerifyTests(certPubKey, data, signature);
                    VerifyTests(certKey, data, signature);
                }

                // we can still sign/verify after disposing the cert
                signature = ecdsa.SignData(data, HashAlgorithmName.SHA256);
                VerifyTests(ecdsa, data, signature);
            }
        }

        private static void VerifyTests(ECDsa ecdsa, byte[] data, byte[] signature)
        {
            bool valid = ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            Assert.True(valid, "signature is not valid");

            signature[0] ^= 0xFF;
            valid = ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            Assert.False(valid, "tampered signature is valid");
            signature[0] ^= 0xFF;

            data[0] ^= 0xFF;
            valid = ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            Assert.False(valid, "tampered data is verified as valid");
            data[0] ^= 0xFF;

            // we call it second time and expect no issues with validation
            valid = ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            Assert.True(valid, "signature is not valid");
        }
    }
}
