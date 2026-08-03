// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace System.Net.Security.Tests
{
    public class SslAuthenticationOptionsTests
    {
        private readonly SslClientAuthenticationOptions _clientOptions = new SslClientAuthenticationOptions();
        private readonly SslServerAuthenticationOptions _serverOptions = new SslServerAuthenticationOptions();

        [Fact]
        public void AllowRenegotiation_Get_Set_Succeeds()
        {
            Assert.True(_clientOptions.AllowRenegotiation);
            Assert.False(_serverOptions.AllowRenegotiation);

            _clientOptions.AllowRenegotiation = true;
            _serverOptions.AllowRenegotiation = true;

            Assert.True(_clientOptions.AllowRenegotiation);
            Assert.True(_serverOptions.AllowRenegotiation);
        }

        [Fact]
        public void ClientCertificateRequired_Get_Set_Succeeds()
        {
            Assert.False(_serverOptions.ClientCertificateRequired);

            _serverOptions.ClientCertificateRequired = true;
            Assert.True(_serverOptions.ClientCertificateRequired);
        }

        [Fact]
        public void ApplicationProtocols_Get_Set_Succeeds()
        {
            Assert.Null(_clientOptions.ApplicationProtocols);
            Assert.Null(_serverOptions.ApplicationProtocols);

            List<SslApplicationProtocol> applnProtos = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3, SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 };
            _clientOptions.ApplicationProtocols = applnProtos;
            _serverOptions.ApplicationProtocols = applnProtos;

            Assert.Equal(applnProtos, _clientOptions.ApplicationProtocols);
            Assert.Equal(applnProtos, _serverOptions.ApplicationProtocols);
        }

        [Fact]
        public void RemoteCertificateValidationCallback_Get_Set_Succeeds()
        {
            Assert.Null(_clientOptions.RemoteCertificateValidationCallback);
            Assert.Null(_serverOptions.RemoteCertificateValidationCallback);

            RemoteCertificateValidationCallback callback = (sender, certificate, chain, errors) => { return true; };
            _clientOptions.RemoteCertificateValidationCallback = callback;
            _serverOptions.RemoteCertificateValidationCallback = callback;

            Assert.Equal(callback, _clientOptions.RemoteCertificateValidationCallback);
            Assert.Equal(callback, _serverOptions.RemoteCertificateValidationCallback);
        }

        [Fact]
        public void LocalCertificateSelectionCallback_Get_Set_Succeeds()
        {
            Assert.Null(_clientOptions.LocalCertificateSelectionCallback);

            LocalCertificateSelectionCallback callback = (sender, host, localCertificates, remoteCertificate, issuers) => default;
            _clientOptions.LocalCertificateSelectionCallback = callback;

            Assert.Equal(callback, _clientOptions.LocalCertificateSelectionCallback);
        }

        [Theory]
        [InlineData("")]
        [InlineData("\u0bee")]
        [InlineData("hello")]
        [InlineData(" \t")]
        [InlineData(null)]
        public void TargetHost_Get_Set_Succeeds(string? expected)
        {
            Assert.Null(_clientOptions.TargetHost);
            _clientOptions.TargetHost = expected;
            Assert.Equal(expected, _clientOptions.TargetHost);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/38559")]
        public void ClientCertificates_Get_Set_Succeeds()
        {
            Assert.Null(_clientOptions.ClientCertificates);

            _clientOptions.ClientCertificates = null;
            Assert.Null(_clientOptions.ClientCertificates);

            X509CertificateCollection expected = new X509CertificateCollection();
            _clientOptions.ClientCertificates = expected;
            Assert.Equal(expected, _clientOptions.ClientCertificates);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/38559")]
        public void ServerCertificate_Get_Set_Succeeds()
        {
            Assert.Null(_serverOptions.ServerCertificate);
            _serverOptions.ServerCertificate = null;

            Assert.Null(_serverOptions.ServerCertificate);
#pragma warning disable SYSLIB0057
            X509Certificate cert = new X509Certificate2(stackalloc byte[0]);
#pragma warning restore SYSLIB0057
            _serverOptions.ServerCertificate = cert;

            Assert.Equal(cert, _serverOptions.ServerCertificate);
        }

        [Fact]
        public void EnabledSslProtocols_Get_Set_Succeeds()
        {
            Assert.Equal(SslProtocols.None, _clientOptions.EnabledSslProtocols);
            Assert.Equal(SslProtocols.None, _serverOptions.EnabledSslProtocols);

            _clientOptions.EnabledSslProtocols = SslProtocols.Tls12;
            _serverOptions.EnabledSslProtocols = SslProtocols.Tls12;

            Assert.Equal(SslProtocols.Tls12, _clientOptions.EnabledSslProtocols);
            Assert.Equal(SslProtocols.Tls12, _serverOptions.EnabledSslProtocols);
        }

        [Fact]
        public void CheckCertificateRevocation_Get_Set_Succeeds()
        {
            Assert.Equal(X509RevocationMode.NoCheck, _clientOptions.CertificateRevocationCheckMode);
            Assert.Equal(X509RevocationMode.NoCheck, _serverOptions.CertificateRevocationCheckMode);

            _clientOptions.CertificateRevocationCheckMode = X509RevocationMode.Online;
            _serverOptions.CertificateRevocationCheckMode = X509RevocationMode.Offline;

            Assert.Equal(X509RevocationMode.Online, _clientOptions.CertificateRevocationCheckMode);
            Assert.Equal(X509RevocationMode.Offline, _serverOptions.CertificateRevocationCheckMode);

            Assert.Throws<ArgumentException>(() => _clientOptions.CertificateRevocationCheckMode = (X509RevocationMode)3);
            Assert.Throws<ArgumentException>(() => _serverOptions.CertificateRevocationCheckMode = (X509RevocationMode)3);
        }

        [Fact]
        public void EncryptionPolicy_Get_Set_Succeeds()
        {
            Assert.Equal(EncryptionPolicy.RequireEncryption, _clientOptions.EncryptionPolicy);
            Assert.Equal(EncryptionPolicy.RequireEncryption, _serverOptions.EncryptionPolicy);

#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
            _clientOptions.EncryptionPolicy = EncryptionPolicy.AllowNoEncryption;
            _serverOptions.EncryptionPolicy = EncryptionPolicy.NoEncryption;

            Assert.Equal(EncryptionPolicy.AllowNoEncryption, _clientOptions.EncryptionPolicy);
            Assert.Equal(EncryptionPolicy.NoEncryption, _serverOptions.EncryptionPolicy);
#pragma warning restore SYSLIB0040

            Assert.Throws<ArgumentException>(() => _clientOptions.EncryptionPolicy = (EncryptionPolicy)3);
            Assert.Throws<ArgumentException>(() => _serverOptions.EncryptionPolicy = (EncryptionPolicy)3);
        }

        [Fact]
        public void UpdateOptions_ServerCertificateContextProvided_DoesNotDisposeCallerContext()
        {
            // Build a certificate chain: root → intermediate → leaf.
            // Use ECDSA P-256 keys: fast and accepted on FIPS-mode platforms.
            DateTimeOffset now = DateTimeOffset.UtcNow;

            using ECDsa rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var rootReq = new CertificateRequest("CN=TestRoot", rootKey, HashAlgorithmName.SHA256);
            rootReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            using X509Certificate2 rootCert = rootReq.CreateSelfSigned(now.AddDays(-1), now.AddDays(365));

            using ECDsa intermediateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var intermediateReq = new CertificateRequest("CN=TestIntermediate", intermediateKey, HashAlgorithmName.SHA256);
            intermediateReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            using X509Certificate2 intermediatePub = intermediateReq.Create(rootCert, now.AddDays(-1), now.AddDays(365), new byte[] { 1 });
            using X509Certificate2 intermediateWithKey = intermediatePub.CopyWithPrivateKey(intermediateKey);

            using ECDsa leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var leafReq = new CertificateRequest("CN=TestLeaf", leafKey, HashAlgorithmName.SHA256);
            using X509Certificate2 leafPub = leafReq.Create(intermediateWithKey, now.AddDays(-1), now.AddDays(365), new byte[] { 2 });
            using X509Certificate2 leafWithKey = leafPub.CopyWithPrivateKey(leafKey);

            // Create a caller-owned context with an intermediate certificate
            SslStreamCertificateContext callerContext = SslStreamCertificateContext.Create(
                leafWithKey,
                new X509Certificate2Collection { intermediateWithKey },
                offline: true);

            // Simulate first UpdateOptions call: bare ServerCertificate creates and owns a context
            var options = new SslAuthenticationOptions();
            SslStreamCertificateContext ownedContext = SslStreamCertificateContext.Create(
                leafWithKey,
                new X509Certificate2Collection { intermediateWithKey },
                offline: true);
            // Capture the internal intermediate cert object before it is released
            Assert.NotEmpty(ownedContext.IntermediateCertificates);
            X509Certificate2 ownedIntermediate = ownedContext.IntermediateCertificates[0];
            options.CertificateContext = ownedContext;
            options.OwnsCertificateContext = true;

            // Simulate second UpdateOptions call: caller provides a ServerCertificateContext.
            // Before the fix, OwnsCertificateContext was not reset to false here, causing Dispose()
            // to incorrectly call ReleaseResources() on the caller-owned context.
            options.UpdateOptions(new SslServerAuthenticationOptions
            {
                ServerCertificateContext = callerContext,
            });

            Assert.False(options.OwnsCertificateContext);
            Assert.Same(callerContext, options.CertificateContext);

            // UpdateOptions should have released the previously-owned context's resources.
            // After X509Certificate2.Dispose(), Handle returns IntPtr.Zero.
            Assert.NotNull(ownedIntermediate);
            Assert.Equal(IntPtr.Zero, ownedIntermediate.Handle);

            // Dispose should NOT release the caller's context
            options.Dispose();

            // Verify that the caller's intermediate certificates were not disposed
            Assert.Equal(1, callerContext.IntermediateCertificates.Count);
            // Export() requires the native certificate handle; a CryptographicException would indicate the certificate was incorrectly disposed.
            foreach (X509Certificate2 cert in callerContext.IntermediateCertificates)
            {
                Assert.Null(Record.Exception(() => cert.Export(X509ContentType.Cert)));
            }
        }
    }
}
