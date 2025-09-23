// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Mail.Tests;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Mail.Tests
{
    public abstract class SmtpClientAuthTest<TSendMethod> : LoopbackServerTestBase<TSendMethod>
        where TSendMethod : ISendMethodProvider
    {
        public static bool IsNtlmInstalled => Capability.IsNtlmInstalled();

        public SmtpClientAuthTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // NTLM support required, see https://github.com/dotnet/runtime/issues/25827
        [SkipOnCoreClr("System.Net.Tests are flaky and/or long running: https://github.com/dotnet/runtime/issues/131", ~RuntimeConfiguration.Release)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/131", TestRuntimes.Mono)] // System.Net.Tests are flaky and/or long running
        public async Task TestCredentialsCopyInAsyncContext()
        {
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            CredentialCache cache = new CredentialCache();
            cache.Add("localhost", Server.Port, "NTLM", CredentialCache.DefaultNetworkCredentials);

            Smtp.Credentials = cache;

            // The mock server doesn't actually understand NTLM, but still advertises support for it
            Server.AdvertiseNtlmAuthSupport = true;
            await SendMail<SmtpException>(msg);

            Assert.Equal("NTLM", Server.AuthMethodUsed, StringComparer.OrdinalIgnoreCase);
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/65678", TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.MacCatalyst)]
        public async Task TestGssapiAuthentication()
        {
            Server.AdvertiseGssapiAuthSupport = true;
            Server.ExpectedGssapiCredential = new NetworkCredential("foo", "bar");
            Smtp.Credentials = Server.ExpectedGssapiCredential;
            MailMessage msg = new MailMessage("foo@example.com", "bar@example.com", "hello", "howdydoo");

            await SendMail(msg);

            Assert.Equal("GSSAPI", Server.AuthMethodUsed, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class SmtpClientAuthTest_Send : SmtpClientAuthTest<SyncSendMethod>
    {
        public SmtpClientAuthTest_Send(ITestOutputHelper output) : base(output) { }
    }

    public class SmtpClientAuthTest_SendAsync : SmtpClientAuthTest<AsyncSendMethod>
    {
        public SmtpClientAuthTest_SendAsync(ITestOutputHelper output) : base(output) { }
    }

    public class SmtpClientAuthTest_SendMailAsync : SmtpClientAuthTest<SendMailAsyncMethod>
    {
        public SmtpClientAuthTest_SendMailAsync(ITestOutputHelper output) : base(output) { }
    }
}
