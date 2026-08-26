// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Mail.Tests;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Mail.Tests
{
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

        [Fact]
        public async Task ChangingHost_EstablishesNewConnection()
        {
            Server.ReceiveMultipleConnections = true;

            await SendMail(new MailMessage("first@example.com", "everyone@novell.com", "introduction", "hello"));
            Assert.Equal(1, Server.ConnectionCount);
            Assert.Equal("<first@example.com>", Server.MailFrom);

            // Change the host to another value that still resolves to the loopback server.
            // The cached connection must be dropped and a new one established.
            Smtp.Host = "127.0.0.1";

            await SendMail(new MailMessage("second@example.com", "everyone@novell.com", "introduction", "hello"));
            Assert.Equal(2, Server.ConnectionCount);
            Assert.Equal("<second@example.com>", Server.MailFrom);
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
