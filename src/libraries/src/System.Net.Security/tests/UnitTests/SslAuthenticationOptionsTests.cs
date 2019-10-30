// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Authentication;
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
            Assert.True(_serverOptions.AllowRenegotiation);

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

            List<SslApplicationProtocol> applnProtos = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 };
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

            LocalCertificateSelectionCallback callback = (sender, host, localCertificates, remoteCertificate, issuers) => { return new X509Certificate(); };
            _clientOptions.LocalCertificateSelectionCallback = callback;

            Assert.Equal(callback, _clientOptions.LocalCertificateSelectionCallback);
        }

        [Theory]
        [InlineData("")]
        [InlineData("\u0bee")]
        [InlineData("hello")]
        [InlineData(" \t")]
        [InlineData(null)]
        public void TargetHost_Get_Set_Succeeds(string expected)
        {
            Assert.Null(_clientOptions.TargetHost);
            _clientOptions.TargetHost = expected;
            Assert.Equal(expected, _clientOptions.TargetHost);
        }

        [Fact]
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
        public void ServerCertificate_Get_Set_Succeeds()
        {
            Assert.Null(_serverOptions.ServerCertificate);
            _serverOptions.ServerCertificate = null;

            Assert.Null(_serverOptions.ServerCertificate);
            X509Certificate cert = new X509Certificate();
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

            _clientOptions.EncryptionPolicy = EncryptionPolicy.AllowNoEncryption;
            _serverOptions.EncryptionPolicy = EncryptionPolicy.NoEncryption;

            Assert.Equal(EncryptionPolicy.AllowNoEncryption, _clientOptions.EncryptionPolicy);
            Assert.Equal(EncryptionPolicy.NoEncryption, _serverOptions.EncryptionPolicy);

            Assert.Throws<ArgumentException>(() => _clientOptions.EncryptionPolicy = (EncryptionPolicy)3);
            Assert.Throws<ArgumentException>(() => _serverOptions.EncryptionPolicy = (EncryptionPolicy)3);
        }
    }
}
