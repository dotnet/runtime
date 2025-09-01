// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Mail.Tests;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Mail.Tests
{
    public abstract class SmtpClientSpecifiedPickupDirectoryTest<T> : LoopbackServerTestBase<T> where T : ISendMethodProvider
    {
        class FileCleanupProvider : FileCleanupTestBase
        {
            // expose protected member
            public new string TestDirectory => base.TestDirectory;
        }

        FileCleanupProvider _fileCleanupProvider = new FileCleanupProvider();

        public SmtpClientSpecifiedPickupDirectoryTest(ITestOutputHelper output) : base(output)
        {

        }

        private string TempFolder
        {
            get
            {
                return _fileCleanupProvider.TestDirectory;
            }
        }

        public override void Dispose()
        {
            _fileCleanupProvider.Dispose();
        }

        [Fact]
        public async Task Send()
        {
            Smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            Smtp.PickupDirectoryLocation = TempFolder;
            await SendMail(new MailMessage("mono@novell.com", "everyone@novell.com", "introduction", "hello"));

            string[] files = Directory.GetFiles(TempFolder, "*");
            Assert.Equal(1, files.Length);
            Assert.Equal(".eml", Path.GetExtension(files[0]));
        }

        [Fact]
        public async Task Send_SpecifiedPickupDirectory_MessageBodyDoesNotEncodeForTransport()
        {
            // This test verifies that a line fold which results in a dot appearing as the first character of
            // a new line does not get dot-stuffed when the delivery method is pickup. To do so, it relies on
            // folding happening at a precise location. If folding implementation details change, this test will
            // likely fail and need to be updated accordingly.

            string padding = new string('a', 65);

            Smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            Smtp.PickupDirectoryLocation = TempFolder;

            await SendMail(new MailMessage("mono@novell.com", "everyone@novell.com", "introduction", padding + "."));

            string[] files = Directory.GetFiles(TempFolder, "*");
            Assert.Equal(1, files.Length);
            Assert.Equal(".eml", Path.GetExtension(files[0]));

            string message = File.ReadAllText(files[0]);
            Assert.EndsWith($"{padding}=\r\n.\r\n", message);
        }

        [Theory]
        [InlineData("some_path_not_exist")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("\0abc")]
        public async Task Send_SpecifiedPickupDirectoryInvalid(string? location)
        {
            Smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            Smtp.PickupDirectoryLocation = location;
            await SendMail<SmtpException>(new MailMessage("mono@novell.com", "everyone@novell.com", "introduction", "hello"), asyncDirectException: true);
        }
    }

    public class SmtpClientSpecifiedPickupDirectoryTest_Send : SmtpClientSpecifiedPickupDirectoryTest<SyncSendMethod>
    {
        public SmtpClientSpecifiedPickupDirectoryTest_Send(ITestOutputHelper output) : base(output) { }
    }

    public class SmtpClientSpecifiedPickupDirectoryTest_SendAsync : SmtpClientSpecifiedPickupDirectoryTest<AsyncSendMethod>
    {
        public SmtpClientSpecifiedPickupDirectoryTest_SendAsync(ITestOutputHelper output) : base(output) { }
    }

    public class SmtpClientSpecifiedPickupDirectoryTest_SendMailAsync : SmtpClientSpecifiedPickupDirectoryTest<SendMailAsyncMethod>
    {
        public SmtpClientSpecifiedPickupDirectoryTest_SendMailAsync(ITestOutputHelper output) : base(output) { }
    }
}