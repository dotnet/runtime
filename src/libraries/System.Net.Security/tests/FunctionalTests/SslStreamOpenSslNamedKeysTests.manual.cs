// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Test.Cryptography;
using Xunit;

namespace System.Net.Security.Tests
{
    // See System.Security.Cryptography/tests/osslplugins/README.md for instructions on how to setup for TPM tests.
    public class SslStreamOpenSslNamedKeys
    {
        [ConditionalFact(typeof(OpenSslNamedKeysHelpers), nameof(OpenSslNamedKeysHelpers.ShouldRunProviderEcDsaTests))]
        public static async Task Provider_TPM2SslStream_ServerCertIsTpmEcDsa()
        {
            using X509Certificate2 serverCert = CreateSelfSignedEcDsaCertificate();
            CreateDefaultTlsOptions(serverCert, out SslServerAuthenticationOptions serverOptions, out SslClientAuthenticationOptions clientOptions);
            await TestTls(serverOptions, clientOptions);
        }

        [ConditionalTheory(typeof(OpenSslNamedKeysHelpers), nameof(OpenSslNamedKeysHelpers.ShouldRunProviderRsaTests))]
        [MemberData(nameof(OpenSslNamedKeysHelpers.RSASignaturePaddingValues), MemberType = typeof(OpenSslNamedKeysHelpers))]
        public static async void Provider_TPM2SslStream_ServerCertIsTpmRsa(RSASignaturePadding padding)
        {
            using X509Certificate2 serverCert = CreateSelfSignedRsaCertificate(padding);
            CreateTlsOptionsForRsa(serverCert, out SslServerAuthenticationOptions serverOptions, out SslClientAuthenticationOptions clientOptions);
            await TestTls(serverOptions, clientOptions);
        }

        private static async Task TestTls(SslServerAuthenticationOptions serverOptions, SslClientAuthenticationOptions clientOptions)
        {
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();

            using (clientStream)
            using (serverStream)
            {
                using SslStream server = new SslStream(serverStream, leaveInnerStreamOpen: false);
                using SslStream client = new SslStream(clientStream, leaveInnerStreamOpen: false);

                Exception failure = await TestHelper.WaitForSecureConnection(client, clientOptions, server, serverOptions).WaitAsync(TestConfiguration.PassingTestTimeoutMilliseconds);
                if (failure is not null)
                {
                    throw failure;
                }

                byte[] testData = [1, 2, 3];
                byte[] receivedData = new byte[testData.Length];
                Span<byte> receivedDataSpan = receivedData;

                // server can write to the client
                server.Write(testData);
                client.ReadExactly(receivedData);
                Assert.True(testData.SequenceEqual(receivedData));

                // client can write to the server
                receivedDataSpan.Fill(0);
                client.Write(testData);
                server.ReadExactly(receivedData);
                Assert.True(testData.SequenceEqual(receivedData));
            }
        }

        private static X509Certificate2 CreateSelfSignedEcDsaCertificate()
        {
            Assert.True(OpenSslNamedKeysHelpers.ShouldRunProviderEcDsaTests);

            // We will get rid of original handle and make sure X509Certificate2's duplicate is still working.
            X509Certificate2 serverCert;
            using (SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider(OpenSslNamedKeysHelpers.Tpm2ProviderName, OpenSslNamedKeysHelpers.TpmEcDsaKeyHandleUri))
            using (ECDsa ecdsaPri = new ECDsaOpenSsl(priKeyHandle))
            {
                serverCert = CreateSelfSignedCertificate(ecdsaPri);
            }

            return serverCert;
        }

        private static X509Certificate2 CreateSelfSignedRsaCertificate(RSASignaturePadding padding)
        {
            Assert.True(OpenSslNamedKeysHelpers.ShouldRunProviderRsaTests);

            // We will get rid of original handle and make sure X509Certificate2's duplicate is still working.
            X509Certificate2 serverCert;
            using (SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider(OpenSslNamedKeysHelpers.Tpm2ProviderName, OpenSslNamedKeysHelpers.TpmRsaKeyHandleUri))
            using (RSA rsaPri = new RSAOpenSsl(priKeyHandle))
            {
                serverCert = CreateSelfSignedCertificate(rsaPri, padding);
            }

            return serverCert;
        }

        private static X509Certificate2 CreateSelfSignedCertificate(ECDsa ecdsa)
        {
            var certReq = new CertificateRequest("CN=testservereku.contoso.com", ecdsa, HashAlgorithmName.SHA256);
            return FinishCertCreation(certReq);
        }

        private static X509Certificate2 CreateSelfSignedCertificate(RSA rsa, RSASignaturePadding padding)
        {
            var certReq = new CertificateRequest("CN=testservereku.contoso.com", rsa, HashAlgorithmName.SHA256, padding);
            return FinishCertCreation(certReq);
        }

        private static X509Certificate2 FinishCertCreation(CertificateRequest certificateRequest)
        {
            certificateRequest.CertificateExtensions.Add(X509BasicConstraintsExtension.CreateForEndEntity());
            certificateRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    // We need to allow KeyEncipherment for RSA ciphersuite which doesn't use PSS.
                    // PSS is causing issues with some TPMs (ignoring salt length)
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    critical: false)
            );

            certificateRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

            return certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(1));
        }

        private static RemoteCertificateValidationCallback CreateRemoteCertificateValidationCallback(byte[] expectedCert)
        {
            return (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) =>
            {
                X509Certificate2? cert = certificate as X509Certificate2;
                return cert != null && cert.RawData.SequenceEqual(expectedCert);
            };
        }

        private static void CreateTlsOptionsForRsa(X509Certificate2 serverCert, out SslServerAuthenticationOptions serverOptions, out SslClientAuthenticationOptions clientOptions)
        {
            CreateDefaultTlsOptions(serverCert, out serverOptions, out clientOptions);
            serverOptions.CipherSuitesPolicy = new CipherSuitesPolicy(new[]
            {
                // Some TPMs don't support PSS fully (i.e. may ignore salt length)
                // which will cause 'bad signature' when used inside of TLS.
                // This ciphersuite still allows PKCS1 padding for signing.
                TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
            });
        }

        private static void CreateDefaultTlsOptions(X509Certificate2 serverCert, out SslServerAuthenticationOptions serverOptions, out SslClientAuthenticationOptions clientOptions)
        {
            serverOptions = new SslServerAuthenticationOptions()
            {
                ServerCertificate = serverCert,
            };

            clientOptions = new SslClientAuthenticationOptions()
            {
                TargetHost = "test",
                RemoteCertificateValidationCallback = CreateRemoteCertificateValidationCallback(serverCert.RawData),
            };
        }
    }
}
