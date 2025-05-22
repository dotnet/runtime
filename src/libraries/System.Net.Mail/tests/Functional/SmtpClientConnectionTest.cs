// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Mail.Tests;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Mail.Tests
{
    public abstract class SmtpClientConnectionTest<TSendMethod> : LoopbackServerTestBase<TSendMethod>
        where TSendMethod : ISendMethodProvider
    {
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
