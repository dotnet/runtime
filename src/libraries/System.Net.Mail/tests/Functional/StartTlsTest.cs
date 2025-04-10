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
    // Common test setup to share across test cases.
    public class CertificateSetup : IDisposable
    {
        public readonly X509Certificate2 serverCert;
        public readonly X509Certificate2Collection serverChain;
        public readonly SslStreamCertificateContext serverCertContext;

        public CertificateSetup()
        {
            (serverCert, serverChain) = System.Net.Test.Common.Configuration.Certificates.GenerateCertificates("localhost", nameof(SmtpClientStartTlsTest<>));
            serverCertContext = SslStreamCertificateContext.Create(serverCert, serverChain);
        }

        public void Dispose()
        {
            serverCert.Dispose();
            foreach (var c in serverChain)
            {
                c.Dispose();
            }
        }
    }

    public abstract class SmtpClientStartTlsTest<TSendMethod> : LoopbackServerTestBase<TSendMethod>
        where TSendMethod : ISendMethodProvider
    {
        private CertificateSetup _certificateSetup;
        private Func<X509Certificate2, X509Chain, SslPolicyErrors, bool>? _serverCertValidationCallback;

        public SmtpClientStartTlsTest(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output)
        {
            _certificateSetup = certificateSetup;
            Server.SslOptions = new SslServerAuthenticationOptions
            {
                ServerCertificateContext = _certificateSetup.serverCertContext,
                ClientCertificateRequired = false,
            };

#pragma warning disable SYSLIB0014 // ServicePointManager is obsolete
            ServicePointManager.ServerCertificateValidationCallback = ServerCertValidationCallback;
#pragma warning restore SYSLIB0014 // ServicePointManager is obsolete
        }

        [Fact]
        public async Task EnableSslServerSupports_UsesTls()
        {
            using SmtpClient client = Server.CreateClient();
            _serverCertValidationCallback = (cert, chain, errors) =>
            {
                return true;
            };

            client.Credentials = new NetworkCredential("foo", "bar");
            client.EnableSsl = true;
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail(client, msg);
            Assert.True(Server.IsEncrypted, "TLS was not negotiated.");
            Assert.Equal(client.Host, Server.TlsHostName);
        }

        [Fact]
        public async Task EnableSsl_NoServerSupport_NoTls()
        {
            using SmtpClient client = Server.CreateClient();
            Server.SslOptions = null;

            client.Credentials = new NetworkCredential("foo", "bar");
            client.EnableSsl = true;
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail<SmtpException>(client, msg);
        }

        [Fact]
        public async Task DisableSslServerSupport_NoTls()
        {
            using SmtpClient client = Server.CreateClient();

            client.Credentials = new NetworkCredential("foo", "bar");
            client.EnableSsl = false;
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail(client, msg);
            Assert.False(Server.IsEncrypted, "TLS was negotiated when it should not have been.");
        }

        [Fact]
        public async Task AuthenticationException_Propagates()
        {
            using SmtpClient client = Server.CreateClient();
            _serverCertValidationCallback = (cert, chain, errors) =>
            {
                return false; // force auth errors
            };

            client.Credentials = new NetworkCredential("foo", "bar");
            client.EnableSsl = true;
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail<AuthenticationException>(client, msg);
        }

        [Fact]
        public async Task ClientCertificateRequired_Sent()
        {
            Server.SslOptions.ClientCertificateRequired = true;
            X509Certificate2 clientCert = _certificateSetup.serverCert; // use the server cert as a client cert for testing
            X509Certificate2? receivedClientCert = null;
            Server.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                receivedClientCert = cert as X509Certificate2;
                return true;
            };

            using SmtpClient client = Server.CreateClient();
            _serverCertValidationCallback = (cert, chain, errors) =>
            {
                return true;
            };

            client.Credentials = new NetworkCredential("foo", "bar");
            client.EnableSsl = true;
            client.ClientCertificates.Add(clientCert);

            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail(client, msg);
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
    public class StartTlsTest_Send : SmtpClientStartTlsTest<SyncSendMethod>, IClassFixture<CertificateSetup>
    {
        public StartTlsTest_Send(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output, certificateSetup) { }
    }

    [Collection(nameof(DisableParallelization))]
    public class StartTlsTest_SendAsync : SmtpClientStartTlsTest<AsyncSendMethod>, IClassFixture<CertificateSetup>
    {
        public StartTlsTest_SendAsync(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output, certificateSetup) { }
    }

    [Collection(nameof(DisableParallelization))]
    public class StartTlsTest_SendMailAsync : SmtpClientStartTlsTest<SendMailAsyncMethod>, IClassFixture<CertificateSetup>
    {
        public StartTlsTest_SendMailAsync(ITestOutputHelper output, CertificateSetup certificateSetup) : base(output, certificateSetup) { }
    }
}