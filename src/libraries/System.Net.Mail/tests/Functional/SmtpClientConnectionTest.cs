// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Mail.Tests;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Mail.Tests
{
    public enum ConnectionAffectingProperty
    {
        Host,
        Credentials,
        TargetName,
    }

    public abstract class SmtpClientConnectionTest<TSendMethod> : LoopbackServerTestBase<TSendMethod>
        where TSendMethod : ISendMethodProvider
    {
        private const int MaxReplyLineLength = 16 * 1024;
        private const int MaxReplyLength = 256 * 1024;

        public SmtpClientConnectionTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task SocketClosed()
        {
            Server.OnConnected = socket => socket.Close();
            await SendMail<SmtpException>(new MailMessage("mono@novell.com", "everyone@novell.com", "introduction", "hello"));
        }

        [Fact]
        public async Task UnrecognizedReply_Throws()
        {
            Server.OnCommandReceived = (command, arg) =>
            {
                return "Go away";
            };

            await SendMail<SmtpException>(new MailMessage("mono@novell.com", "everyone@novell.com", "introduction", "hello"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task OversizedReply_Throws(bool multiline)
        {
            string reply;
            if (multiline)
            {
                const int ContinuationLineLength = 8 * 1024;
                string continuationLine = $"250-{new string('a', ContinuationLineLength - 6)}\r\n";
                StringBuilder builder = new StringBuilder(MaxReplyLength + ContinuationLineLength);
                while (builder.Length <= MaxReplyLength)
                {
                    builder.Append(continuationLine);
                }
                builder.Append("250 OK");
                reply = builder.ToString();
            }
            else
            {
                reply = $"250 {new string('a', MaxReplyLineLength)}";
            }

            Server.OnCommandReceived = (command, _) =>
                command.Equals("EHLO", StringComparison.OrdinalIgnoreCase) ? reply : null;

            await SendMail<SmtpException>(new MailMessage("mono@novell.com", "everyone@novell.com", "introduction", "hello"));
        }

        [Fact]
        public async Task EHelloNotRecognized_RestartWithHello()
        {
            bool helloReceived = false;
            Server.OnCommandReceived = (command, arg) =>
            {
                helloReceived |= string.Equals(command, "HELO", StringComparison.OrdinalIgnoreCase);
                if (string.Equals(command, "EHLO", StringComparison.OrdinalIgnoreCase))
                {
                    return "502 Not implemented";
                }

                return null;
            };

            await SendMail(new MailMessage("mono@novell.com", "everyone@novell.com", "introduction", "hello"));
            Assert.True(helloReceived, "HELO command was not received.");
        }

        [Fact]
        public async Task ChangingPort_DoesNotReuseConnectionToPreviousServer()
        {
            using LoopbackSmtpServer server2 = new LoopbackSmtpServer(Output);

            await SendMail(new MailMessage("first@example.com", "everyone@novell.com", "introduction", "hello"));
            Assert.Equal("<first@example.com>", Server.MailFrom);

            // Point the client at a different server. The cached connection to the original
            // server must be dropped so the next message is delivered to the new server.
            Smtp.Port = server2.Port;

            await SendMail(new MailMessage("second@example.com", "everyone@novell.com", "introduction", "hello"));

            Assert.Equal("<second@example.com>", server2.MailFrom);
            Assert.Equal(1, server2.ConnectionCount);

            // The original server must not have received the second message.
            Assert.Equal("<first@example.com>", Server.MailFrom);
        }

        [Theory]
        [InlineData(ConnectionAffectingProperty.Host)]
        [InlineData(ConnectionAffectingProperty.Credentials)]
        [InlineData(ConnectionAffectingProperty.TargetName)]
        public async Task ChangingConnectionProperty_EstablishesNewConnection(ConnectionAffectingProperty property)
        {
            Server.ReceiveMultipleConnections = true;

            await SendMail(new MailMessage("first@example.com", "everyone@novell.com", "introduction", "hello"));
            Assert.Equal(1, Server.ConnectionCount);
            Assert.Equal("<first@example.com>", Server.MailFrom);

            // Change a property that affects how the connection is established. The cached
            // connection must be invalidated and a new one established on the next send.
            switch (property)
            {
                case ConnectionAffectingProperty.Host:
                    // A different value that still resolves to the loopback server. The default
                    // TargetName should follow the host so authentication uses the correct SPN.
                    Assert.Equal("SMTPSVC/localhost", Smtp.TargetName);
                    Smtp.Host = "127.0.0.1";
                    Assert.Equal("SMTPSVC/127.0.0.1", Smtp.TargetName);
                    break;
                case ConnectionAffectingProperty.Credentials:
                    Smtp.Credentials = new NetworkCredential("foo", "bar");
                    break;
                case ConnectionAffectingProperty.TargetName:
                    Smtp.TargetName = "SMTPSVC/example.com";
                    break;
            }

            await SendMail(new MailMessage("second@example.com", "everyone@novell.com", "introduction", "hello"));
            Assert.Equal(2, Server.ConnectionCount);
            Assert.Equal("<second@example.com>", Server.MailFrom);
        }

        [Fact]
        public async Task ChangingHost_PreservesExplicitlySetTargetName()
        {
            // A TargetName explicitly set by the caller must not be overwritten when the host
            // changes, even though the default (host-derived) TargetName does follow the host.
            Smtp.TargetName = "SMTPSVC/explicit.example.com";

            await SendMail(new MailMessage("first@example.com", "everyone@novell.com", "introduction", "hello"));
            Assert.Equal("SMTPSVC/explicit.example.com", Smtp.TargetName);

            Smtp.Host = "127.0.0.1";
            Assert.Equal("SMTPSVC/explicit.example.com", Smtp.TargetName);
        }
    }

    public class SmtpClientConnectionTest_Send : SmtpClientConnectionTest<SyncSendMethod>
    {
        public SmtpClientConnectionTest_Send(ITestOutputHelper output) : base(output) { }
    }

    public class SmtpClientConnectionTest_SendAsync : SmtpClientConnectionTest<AsyncSendMethod>
    {
        public SmtpClientConnectionTest_SendAsync(ITestOutputHelper output) : base(output) { }
    }

    public class SmtpClientConnectionTest_SendMailAsync : SmtpClientConnectionTest<SendMailAsyncMethod>
    {
        public SmtpClientConnectionTest_SendMailAsync(ITestOutputHelper output) : base(output) { }
    }
}
