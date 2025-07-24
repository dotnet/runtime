// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Mail.Tests;
using System.Threading.Tasks;
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
        private Func<X509Certificate2, X509Chain, SslPolicyErrors, bool>? _serverCertValidationCallback;

        public SmtpClientTlsTest(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output)
        {
            _certificateSetup = certificateSetup;
            Server.SslOptions = new SslServerAuthenticationOptions
            {
                ServerCertificateContext = _certificateSetup.CreateSslStreamCertificateContext(),
                ClientCertificateRequired = false,
            };

#pragma warning disable SYSLIB0014 // ServicePointManager is obsolete
            ServicePointManager.ServerCertificateValidationCallback = ServerCertValidationCallback;
#pragma warning restore SYSLIB0014 // ServicePointManager is obsolete
        }

        [Fact]
        public async Task EnableSsl_ServerSupports_UsesTls()
        {
            _serverCertValidationCallback = (cert, chain, errors) =>
            {
                return true;
            };

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
            _serverCertValidationCallback = (cert, chain, errors) =>
            {
                return false; // force auth errors
            };

            Smtp.Credentials = new NetworkCredential("foo", "bar");
            Smtp.EnableSsl = true;
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail<AuthenticationException>(msg);
        }

        [Fact]
        public async Task ClientCertificateRequired_Sent()
        {
            Server.SslOptions.ClientCertificateRequired = true;
            X509Certificate2 clientCert = _certificateSetup.ServerCert; // use the server cert as a client cert for testing
            X509Certificate2? receivedClientCert = null;
            Server.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                receivedClientCert = cert as X509Certificate2;
                return true;
            };

            _serverCertValidationCallback = (cert, chain, errors) =>
            {
                return true;
            };

            Smtp.Credentials = new NetworkCredential("foo", "bar");
            Smtp.EnableSsl = true;
            Smtp.ClientCertificates.Add(clientCert);

            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail(msg);
            Assert.True(Server.IsEncrypted, "TLS was not negotiated.");
            Assert.Equal(clientCert, receivedClientCert);
        }

        private bool ServerCertValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            if (_serverCertValidationCallback != null)
            {
                return _serverCertValidationCallback((X509Certificate2)certificate!, chain!, sslPolicyErrors);
            }

            // Default validation: check if the certificate is valid.
            return sslPolicyErrors == SslPolicyErrors.None;
        }
    }

    // since the tests change global state (ServicePointManager.ServerCertificateValidationCallback), we need to run them in isolation

    [Collection(nameof(DisableParallelization))]
    public class SmtpClientTlsTest_Send : SmtpClientTlsTest<SyncSendMethod>, IClassFixture<CertificateSetup>
    {
        public SmtpClientTlsTest_Send(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output, certificateSetup) { }
    }

    [Collection(nameof(DisableParallelization))]
    public class SmtpClientTlsTest_SendAsync : SmtpClientTlsTest<AsyncSendMethod>, IClassFixture<CertificateSetup>
    {
        public SmtpClientTlsTest_SendAsync(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output, certificateSetup) { }
    }

    [Collection(nameof(DisableParallelization))]
    public class SmtpClientTlsTest_SendMailAsync : SmtpClientTlsTest<SendMailAsyncMethod>, IClassFixture<CertificateSetup>
    {
        public SmtpClientTlsTest_SendMailAsync(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output, certificateSetup) { }
    }
}