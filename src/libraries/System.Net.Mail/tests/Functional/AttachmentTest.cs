// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// AttachmentTest.cs - NUnit Test Cases for System.Net.MailAddress.Attachment
//
// Authors:
//   John Luke (john.luke@gmail.com)
//
// (C) 2005 John Luke
//

using System.IO;
using System.Text;
using System.Net.Mime;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class AttachmentTest
    {
        [Fact]
        public void TestNullStream()
        {
            Stream s = null;
            Assert.Throws<ArgumentNullException>(() => new Attachment(s, "application/octet-stream"));
        }

        [Fact]
        public void ConstructorNullName()
        {
            Attachment attach = new Attachment(new MemoryStream(), null, "application/octet-stream");
            Assert.Equal(null, attach.Name);
        }

        [Fact]
        public void CreateAttachmentFromStringNullName()
        {
            Attachment attach = Attachment.CreateAttachmentFromString("", null, Encoding.ASCII, "application/octet-stream");
            Assert.Null(attach.Name);
        }

        [Fact]
        public void ContentDisposition()
        {
            Attachment attach = Attachment.CreateAttachmentFromString("test", "attachment-name");
            Assert.NotNull(attach.ContentDisposition);
            Assert.Equal("attachment", attach.ContentDisposition.DispositionType);
        }

        [Fact]
        public void ContentType()
        {
            Attachment attach = Attachment.CreateAttachmentFromString("test", "attachment-name");
            Assert.NotNull(attach.ContentType);
            Assert.Equal("text/plain", attach.ContentType.MediaType);
            Attachment a2 = new Attachment(new MemoryStream(), "myname");
            Assert.NotNull(a2.ContentType);
            Assert.Equal("application/octet-stream", a2.ContentType.MediaType);
        }

        [Fact]
        public void NameEncoding()
        {
            Attachment attach = Attachment.CreateAttachmentFromString("test", "attachment-name");
            Assert.Null(attach.NameEncoding);
            Attachment a = new Attachment(new MemoryStream(), "myname");
            Assert.Null(a.NameEncoding);
            a = new Attachment(new MemoryStream(), "myname\u3067");
            Assert.Null(a.NameEncoding);
        }

        [Fact]
        public void ContentStream()
        {
            Attachment attach = Attachment.CreateAttachmentFromString("test", "attachment-name");
            Assert.NotNull(attach.ContentStream);
            Assert.Equal(4, attach.ContentStream.Length);
        }


        [Fact]
        public void Name()
        {
            Attachment attach = Attachment.CreateAttachmentFromString("test", "attachment-name");
            Assert.Equal("attachment-name", attach.Name);
            Attachment a2 = new Attachment(new MemoryStream(), new ContentType("image/jpeg"));
            Assert.Equal(null, a2.Name);
            a2.Name = null; // nullable
        }

        [Fact]
        public void TransferEncodingTest()
        {
            Attachment attach = Attachment.CreateAttachmentFromString("test", "attachment-name");
            Assert.Equal(TransferEncoding.QuotedPrintable, attach.TransferEncoding);
        }
    }
}
