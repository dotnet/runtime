// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Net.Mime;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Mail.Tests
{
    public abstract class SmtpClientAttachmentTest<T> : LoopbackServerTestBase<T> where T : ISendMethodProvider
    {
        public SmtpClientAttachmentTest(ITestOutputHelper output) : base(output)
        {
        }

        private class ThrowingStream : Stream
        {

            public override bool CanRead => throw new NotImplementedException();
            public override bool CanSeek => throw new NotImplementedException();
            public override bool CanWrite => throw new NotImplementedException();
            public override long Length => throw new NotImplementedException();
            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override void Flush() => throw new NotImplementedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Something wrong happened");
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long length) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }

        [Fact]
        public async Task AtachmentStreamThrows_Exception()
        {
            string attachmentFilename = "test.txt";
            byte[] attachmentContent = Encoding.UTF8.GetBytes("File Contents\r\n");

            string body = "This is a test mail.";

            using var msg = new MailMessage()
            {
                From = new MailAddress("foo@example.com"),
                To = { new MailAddress("baz@example.com") },
                Attachments = {
                    new Attachment(new ThrowingStream(), attachmentFilename, MediaTypeNames.Text.Plain)
                },
                Subject = "Test Subject",
                Body = body
            };

            await SendMail<SmtpException>(msg);
        }

        [Fact]
        public async Task TextFileAttachment()
        {
            string attachmentFilename = "test.txt";
            byte[] attachmentContent = Encoding.UTF8.GetBytes("File Contents\r\n");

            string body = "This is a test mail.";

            using var msg = new MailMessage()
            {
                From = new MailAddress("foo@example.com"),
                To = { new MailAddress("baz@example.com") },
                Attachments = {
                    new Attachment(new MemoryStream(attachmentContent), attachmentFilename, MediaTypeNames.Text.Plain)
                },
                Subject = "Test Subject",
                Body = body
            };

            await SendMail(msg);

            Assert.Equal(body, Server.Message.Body);
            Assert.Collection(Server.Message.Attachments,
                attachment =>
                {
                    Assert.Equal(attachmentFilename, attachment.ContentType.Name);
                    Assert.Equal(MediaTypeNames.Text.Plain, attachment.ContentType.MediaType);
                    Assert.Equal("base64", attachment.ContentTransferEncoding);
                    Assert.Equal(attachmentContent, Convert.FromBase64String(attachment.RawContent));
                });
        }
    }

    public class SmtpClientAttachmentTest_Send : SmtpClientAttachmentTest<SyncSendMethod>
    {
        public SmtpClientAttachmentTest_Send(ITestOutputHelper output) : base(output) { }
    }

    public class SmtpClientAttachmentTest_SendAsync : SmtpClientAttachmentTest<AsyncSendMethod>
    {
        public SmtpClientAttachmentTest_SendAsync(ITestOutputHelper output) : base(output) { }
    }

    public class SmtpClientAttachmentTest_SendMailAsync : SmtpClientAttachmentTest<SendMailAsyncMethod>
    {
        public SmtpClientAttachmentTest_SendMailAsync(ITestOutputHelper output) : base(output) { }
    }
}
