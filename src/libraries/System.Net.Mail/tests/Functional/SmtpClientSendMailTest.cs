// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Mail.Tests;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Mail.Tests
{
    public abstract class SmtpClientSendMailTest<T> : LoopbackServerTestBase<T> where T : ISendMethodProvider
    {
        public SmtpClientSendMailTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Message_Null()
        {
            await SendMail<ArgumentNullException>(null);
        }

        [Fact]
        public async Task Network_Host_Whitespace()
        {
            Smtp.Host = " \r\n ";
            await SendMail<InvalidOperationException>(new MailMessage("mono@novell.com", "everyone@novell.com", "introduction", "hello"));
        }

        [Fact]
        public async Task ServerDoesntExist_Throws()
        {
            Smtp.Host = Guid.NewGuid().ToString("N");
            await SendMail<SmtpException>(new MailMessage("mono@novell.com", "everyone@novell.com", "introduction", "hello"));
        }

        [Theory]
        [InlineData("howdydoo")]
        [InlineData("")]
        [InlineData(null)]
        [SkipOnCoreClr("System.Net.Tests are flaky and/or long running: https://github.com/dotnet/runtime/issues/131", ~RuntimeConfiguration.Release)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/131", TestRuntimes.Mono)] // System.Net.Tests are flaky and/or long running
        public async Task MailDelivery(string body)
        {
            Smtp.Credentials = new NetworkCredential("foo", "bar");
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", body);

            await SendMail(msg).WaitAsync(TimeSpan.FromSeconds(30));

            Assert.Equal("<foo@example.com>", Server.MailFrom);
            Assert.Equal("<bar@example.com>", Server.MailTo);
            Assert.Equal("hello", Server.Message.Subject);
            Assert.Equal(body ?? "", Server.Message.Body);
            Assert.Equal(GetClientDomain(), Server.ClientDomain);
            Assert.Equal("foo", Server.Username);
            Assert.Equal("bar", Server.Password);
            Assert.Equal("LOGIN", Server.AuthMethodUsed, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)] // Received subjectText.
        [InlineData(true, false)]
        [InlineData(true, true)] // Received subjectBase64. If subjectText is received, the test fails, and the results are inconsistent with those of synchronous methods.
        public async Task SendMail_DeliveryFormat_SubjectEncoded(bool useSevenBit, bool useSmtpUTF8)
        {
            // If the server support `SMTPUTF8` and use `SmtpDeliveryFormat.International`, the server should received this subject.
            const string subjectText = "Test \u6d4b\u8bd5 Contain \u5305\u542b UTF8";

            // If the server does not support `SMTPUTF8` or use `SmtpDeliveryFormat.SevenBit`, the server should received this subject.
            const string subjectBase64 = "=?utf-8?B?VGVzdCDmtYvor5UgQ29udGFpbiDljIXlkKsgVVRGOA==?=";

            // Setting up Server Support for `SMTPUTF8`.
            Server.SupportSmtpUTF8 = useSmtpUTF8;

            if (useSevenBit)
            {
                // Subject will be encoded by Base64.
                Smtp.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
            }
            else
            {
                // If the server supports `SMTPUTF8`, subject will not be encoded. Otherwise, subject will be encoded by Base64.
                Smtp.DeliveryFormat = SmtpDeliveryFormat.International;
            }

            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", subjectText, "hello \u9ad8\u575a\u679c");
            msg.HeadersEncoding = msg.BodyEncoding = msg.SubjectEncoding = System.Text.Encoding.UTF8;

            await SendMail(msg);

            if (useSevenBit || !useSmtpUTF8)
            {
                Assert.Equal(subjectBase64, Server.Message.Subject);
            }
            else
            {
                Assert.Equal(subjectText, Server.Message.Subject);
            }
        }

        [Fact]
        public async Task SendQUITOnDispose()
        {
            bool quitMessageReceived = false;
            using ManualResetEventSlim quitReceived = new ManualResetEventSlim();
            Server.OnQuitReceived += _ =>
            {
                quitMessageReceived = true;
                quitReceived.Set();
            };

            Smtp.Credentials = new NetworkCredential("Foo", "Bar");
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");
            await SendMail(msg);
            Assert.False(quitMessageReceived, "QUIT received");
            Smtp.Dispose();

            // There is a latency between send/receive.
            quitReceived.Wait(TimeSpan.FromSeconds(30));
            Assert.True(quitMessageReceived, "QUIT message not received");
        }

        [Fact]
        public async Task TestMultipleMailDelivery()
        {
            Smtp.Timeout = 10000;
            Smtp.Credentials = new NetworkCredential("foo", "bar");
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            for (var i = 0; i < 5; i++)
            {
                await SendMail(msg);

                Assert.Equal("<foo@example.com>", Server.MailFrom);
                Assert.Equal("<bar@example.com>", Server.MailTo);
                Assert.Equal("hello", Server.Message.Subject);
                Assert.Equal("howdydoo", Server.Message.Body);
                Assert.Equal(GetClientDomain(), Server.ClientDomain);
                Assert.Equal("foo", Server.Username);
                Assert.Equal("bar", Server.Password);
                Assert.Equal("LOGIN", Server.AuthMethodUsed, StringComparer.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [MemberData(nameof(SendMail_MultiLineDomainLiterals_Data))]
        public async Task MultiLineDomainLiterals_Disabled_Throws(string from, string to)
        {
            Smtp.Credentials = new NetworkCredential("Foo", "Bar");

            using var msg = new MailMessage(@from, @to, "subject", "body");

            await SendMail<SmtpException>(msg);
        }

        public static IEnumerable<object[]> SendMail_MultiLineDomainLiterals_Data()
        {
            foreach (string address in new[] { "foo@[\r\n bar]", "foo@[bar\r\n ]", "foo@[bar\r\n baz]" })
            {
                yield return new object[] { address, "foo@example.com" };
                yield return new object[] { "foo@example.com", address };
            }
        }
    }

    public class SmtpClientSendMailTest_Send : SmtpClientSendMailTest<SyncSendMethod>
    {
        public SmtpClientSendMailTest_Send(ITestOutputHelper output) : base(output) { }
    }

    public class SmtpClientSendMailTest_SendAsync : SmtpClientSendMailTest<AsyncSendMethod>
    {
        public SmtpClientSendMailTest_SendAsync(ITestOutputHelper output) : base(output) { }
    }

    public class SmtpClientSendMailTest_SendMailAsync : SmtpClientSendMailTest<SendMailAsyncMethod>
    {
        public SmtpClientSendMailTest_SendMailAsync(ITestOutputHelper output) : base(output) { }
    }
}