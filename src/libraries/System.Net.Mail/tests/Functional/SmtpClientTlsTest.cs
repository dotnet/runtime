// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Mail.Tests;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Mail.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    // Common test setup to share across test cases.
    public class CertificateSetup : IDisposable
    {
        public X509Certificate2 ServerCert => _pkiHolder.EndEntity;
        public X509Certificate2Collection ServerChain => _pkiHolder.IssuerChain;

        private readonly Configuration.Certificates.PkiHolder _pkiHolder;

        public CertificateSetup()
        {
            _pkiHolder = Configuration.Certificates.GenerateCertificates("localhost", nameof(SmtpClientTlsTest<>), longChain: true);
        }

        public SslStreamCertificateContext CreateSslStreamCertificateContext() => _pkiHolder.CreateSslStreamCertificateContext();

        public void Dispose()
        {
            _pkiHolder.Dispose();
        }
    }

    public abstract class SmtpClientTlsTest<TSendMethod> : LoopbackServerTestBase<TSendMethod>
        where TSendMethod : ISendMethodProvider
    {
        private CertificateSetup _certificateSetup;

        public SmtpClientTlsTest(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output)
        {
            _certificateSetup = certificateSetup;
            Server.SslOptions = new SslServerAuthenticationOptions
            {
                ServerCertificateContext = _certificateSetup.CreateSslStreamCertificateContext(),
                ClientCertificateRequired = false,
                AllowTlsResume = false,
            };

            Smtp.SslOptions.AllowTlsResume = false;
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/120959", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot), nameof(PlatformDetection.IsAndroid))]
        [Fact]
        public async Task EnableSsl_ServerSupports_UsesTls()
        {
            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;

            Smtp.Credentials = new NetworkCredential("foo", "bar");
            Smtp.EnableSsl = true;
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail(msg);
            Assert.True(Server.IsEncrypted, "TLS was not negotiated.");
            Assert.Equal(Smtp.Host, Server.TlsHostName);
        }

        [Theory]
        [InlineData("500 T'was just a jest.")]
        [InlineData("300 I don't know what I am doing.")]
        [InlineData("I don't know what I am doing.")]
        public async Task EnableSsl_ServerError(string reply)
        {
            Smtp.EnableSsl = true;
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            Server.OnCommandReceived = (command, parameter) =>
            {
                if (string.Equals(command, "STARTTLS", StringComparison.OrdinalIgnoreCase))
                    return reply;

                return null;
            };

            await SendMail<SmtpException>(msg);
        }


        [Fact]
        public async Task EnableSsl_NoServerSupport_NoTls()
        {
            Server.SslOptions = null;

            Smtp.Credentials = new NetworkCredential("foo", "bar");
            Smtp.EnableSsl = true;
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail<SmtpException>(msg);
        }

        [Fact]
        public async Task EnableSsl_NoExtendedHello_NoTls()
        {
            Smtp.Credentials = new NetworkCredential("foo", "bar");
            Smtp.EnableSsl = true;

            Server.OnCommandReceived = (command, arg) =>
            {
                if (string.Equals(command, "EHLO", StringComparison.OrdinalIgnoreCase))
                {
                    return "502 Not implemented";
                }

                return null;
            };

            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail<SmtpException>(msg);
        }

        [Fact]
        public async Task DisableSslServerSupport_NoTls()
        {

            Smtp.Credentials = new NetworkCredential("foo", "bar");
            Smtp.EnableSsl = false;
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail(msg);
            Assert.False(Server.IsEncrypted, "TLS was negotiated when it should not have been.");
        }

        [Fact]
        public async Task AuthenticationException_Propagates()
        {
            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => false;

            Smtp.Credentials = new NetworkCredential("foo", "bar");
            Smtp.EnableSsl = true;
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail<AuthenticationException>(msg);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/120959", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot), nameof(PlatformDetection.IsAndroid))]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ClientCertificateRequired_Sent(bool useSslOptions)
        {
            Server.SslOptions.ClientCertificateRequired = true;
            X509Certificate2 clientCert = _certificateSetup.ServerCert; // use the server cert as a client cert for testing
            X509Certificate2? receivedClientCert = null;
            Server.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                receivedClientCert = cert as X509Certificate2;
                return true;
            };

            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;

            Smtp.Credentials = new NetworkCredential("foo", "bar");
            Smtp.EnableSsl = true;
            if (useSslOptions)
            {
                Smtp.SslOptions.ClientCertificates = new X509CertificateCollection { clientCert };
            }
            else
            {
                Smtp.ClientCertificates.Add(clientCert);
            }

            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail(msg);
            Assert.True(Server.IsEncrypted, "TLS was not negotiated.");
            Assert.Equal(clientCert, receivedClientCert);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/120959", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot), nameof(PlatformDetection.IsAndroid))]
        [Fact]
        public async Task EnableSsl_ChangedAfterConnect_EstablishesNewEncryptedConnection()
        {
            Server.ReceiveMultipleConnections = true;
            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;

            // First send happens over a plaintext connection.
            await SendMail(new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo"));
            Assert.Equal(1, Server.ConnectionCount);
            Assert.False(Server.IsEncrypted, "First connection should not be encrypted.");

            // Enabling SSL must invalidate the cached plaintext connection so the next send
            // establishes a new, encrypted connection instead of reusing the old one.
            Smtp.EnableSsl = true;

            await SendMail(new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo"));
            Assert.Equal(2, Server.ConnectionCount);
            Assert.True(Server.IsEncrypted, "Second connection should be encrypted after enabling SSL.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("smtp.example.com")]
        public async Task SslOptions_TargetHost(string? targetHost)
        {
            Smtp.EnableSsl = true;
            Smtp.SslOptions.TargetHost = targetHost;
            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;
            using var message = new MailMessage("from@example.com", "to@example.com", "subject", "body");

            await SendMail(message);

            Assert.Equal(targetHost ?? Smtp.Host, Server.TlsHostName);
            Assert.Equal(targetHost, Smtp.SslOptions.TargetHost);
        }

        [Fact]
        public async Task SslOptions_DefaultTargetHostFollowsHostChange()
        {
            Server.ReceiveMultipleConnections = true;
            Smtp.EnableSsl = true;
            string? validatedHost = null;
            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                validatedHost = Assert.IsType<SslStream>(sender).TargetHostName;
                return true;
            };
            using var message = new MailMessage("from@example.com", "to@example.com", "subject", "body");

            await SendMail(message);
            Assert.Equal("localhost", validatedHost);

            Smtp.Host = "127.0.0.1";
            await SendMail(message);
            Assert.Equal("127.0.0.1", validatedHost);
            Assert.Equal(2, Server.ConnectionCount);
            Assert.Null(Smtp.SslOptions.TargetHost);
        }

        [Fact]
        public async Task SslOptions_ProtocolSelection()
        {
            Smtp.EnableSsl = true;
            Smtp.SslOptions.EnabledSslProtocols = SslProtocols.Tls12;
            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;
            using var message = new MailMessage("from@example.com", "to@example.com", "subject", "body");

            await SendMail(message);

            Assert.Equal(SslProtocols.Tls12, Server.TlsProtocol);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task SslOptions_ApplicationProtocols()
        {
            var protocol = new SslApplicationProtocol("smtp-test");
            Server.SslOptions.ApplicationProtocols = new() { protocol };
            Smtp.EnableSsl = true;
            Smtp.SslOptions.ApplicationProtocols = new() { protocol };
            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;
            using var message = new MailMessage("from@example.com", "to@example.com", "subject", "body");

            await SendMail(message);

            Assert.Equal(protocol, Server.ApplicationProtocol);
        }

        [Fact]
        public async Task SslOptions_DisabledSslDoesNotUseOptions()
        {
            bool callbackCalled = false;
            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                callbackCalled = true;
                return false;
            };
            using var message = new MailMessage("from@example.com", "to@example.com", "subject", "body");

            await SendMail(message);

            Assert.False(Server.IsEncrypted);
            Assert.False(callbackCalled);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SslOptions_AssignmentInvalidatesConnection(bool sameInstance)
        {
            Server.ReceiveMultipleConnections = true;
            Smtp.EnableSsl = true;
            X509CertificateCollection certificates = Smtp.ClientCertificates;
            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;
            using var message = new MailMessage("from@example.com", "to@example.com", "subject", "body");
            await SendMail(message);
            await SendMail(message);
            Assert.Equal(1, Server.ConnectionCount);

            SslClientAuthenticationOptions options = sameInstance ? Smtp.SslOptions : new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
            };
            options.TargetHost = "smtp.example.com";
            if (sameInstance)
            {
                certificates.Add(_certificateSetup.ServerCert);
                Assert.Same(certificates, options.ClientCertificates);
                await SendMail(message);
                Assert.Equal(1, Server.ConnectionCount);
                Assert.Equal("localhost", Server.TlsHostName);
            }

            Smtp.SslOptions = options;
            await SendMail(message);
            Assert.Equal(2, Server.ConnectionCount);
            Assert.Equal("smtp.example.com", Server.TlsHostName);
        }

        [Fact]
        public async Task SslOptions_InPlaceChangeAppliesAfterReconnect()
        {
            Server.ReceiveMultipleConnections = true;
            Smtp.EnableSsl = true;
            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;
            using var message = new MailMessage("from@example.com", "to@example.com", "subject", "body");
            await SendMail(message);

            Smtp.SslOptions.TargetHost = "smtp.example.com";
            Smtp.TargetName = "SMTPSVC/another-name";
            await SendMail(message);

            Assert.Equal(2, Server.ConnectionCount);
            Assert.Equal("smtp.example.com", Server.TlsHostName);
        }

        [Fact]
        public async Task SslOptions_AssignmentDuringSendThrows()
        {
            Smtp.EnableSsl = true;
            SslClientAuthenticationOptions original = Smtp.SslOptions;
            Exception? replacementAssignmentException = null;
            Exception? sameInstanceAssignmentException = null;
            Server.OnCommandReceived = (command, parameter) =>
            {
                if (string.Equals(command, "STARTTLS", StringComparison.OrdinalIgnoreCase))
                {
                    replacementAssignmentException = Record.Exception(() => Smtp.SslOptions = new SslClientAuthenticationOptions());
                    sameInstanceAssignmentException = Record.Exception(() => Smtp.SslOptions = original);
                }

                return null;
            };
            Smtp.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;
            using var message = new MailMessage("from@example.com", "to@example.com", "subject", "body");

            await SendMail(message);

            Assert.IsType<InvalidOperationException>(replacementAssignmentException);
            Assert.IsType<InvalidOperationException>(sameInstanceAssignmentException);
            Assert.Same(original, Smtp.SslOptions);
        }
    }

    public class SmtpClientTlsTest_Send : SmtpClientTlsTest<SyncSendMethod>, IClassFixture<CertificateSetup>
    {
        public SmtpClientTlsTest_Send(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output, certificateSetup) { }
    }

    public class SmtpClientTlsTest_SendAsync : SmtpClientTlsTest<AsyncSendMethod>, IClassFixture<CertificateSetup>
    {
        public SmtpClientTlsTest_SendAsync(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output, certificateSetup) { }
    }

    public class SmtpClientTlsTest_SendMailAsync : SmtpClientTlsTest<SendMailAsyncMethod>, IClassFixture<CertificateSetup>
    {
        public SmtpClientTlsTest_SendMailAsync(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output, certificateSetup) { }
    }
}
