// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// AttachmentTest.cs - Unit Test Cases for System.Net.MailAddress.Attachment
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
            Assert.Null(attach.Name);
        }

        [Fact]
        public void ConstructorPathName()
        {
            using (var tempFile = TempFile.Create(new byte[0]))
            {
                using (Attachment attach = new Attachment(tempFile.Path))
                {
                    Assert.Equal(Path.GetFileName(tempFile.Path), attach.Name);
                }
            }
        }

        [Fact]
        public void ConstructorPathNameMediaType()
        {
            using (var tempFile = TempFile.Create(new byte[0]))
            {
                const string mediaType = "application/octet-stream";
                string shortName = Path.GetFileName(tempFile.Path);
                using (Attachment attach = new Attachment(tempFile.Path, mediaType))
                {
                    Assert.Equal(shortName, attach.Name);
                    Assert.Equal(mediaType, attach.ContentType.MediaType);
                }
            }
        }

        [Fact]
        public void ConstructorPathNameContentType()
        {
            using (var tempFile = TempFile.Create(new byte[0]))
            {
                const string mediaType = "application/octet-stream";
                string shortName = Path.GetFileName(tempFile.Path);
                using (Attachment attach = new Attachment(tempFile.Path, new Mime.ContentType(mediaType)))
                {
                    Assert.Equal(shortName, attach.Name);
                    Assert.Equal(mediaType, attach.ContentType.MediaType);
                }
            }
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
            Attachment a;

            a = Attachment.CreateAttachmentFromString("test", "attachment-name");
            Assert.Null(a.NameEncoding);

            a = new Attachment(new MemoryStream(), "attachmentname");
            Assert.Null(a.NameEncoding);

            a = new Attachment(new MemoryStream(), "attachmentname\u3067");
            Assert.Null(a.NameEncoding);
        }

        [Fact]
        public void NameParsingAndEncodingDetection_Basics()
        {
            Attachment a;

            // smoke test
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?Q?attachmentname?=");
            Assert.Equal("attachmentname", a.Name);
            Assert.Equal(a.NameEncoding, Encoding.Latin1);

            // lower case charset
            a = new Attachment(new MemoryStream(), "=?iso-8859-1?Q?attachmentname?=");
            Assert.Equal("attachmentname", a.Name);
            Assert.Equal(a.NameEncoding, Encoding.Latin1);

            // Q encoding
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?Q?attachment=20name?=");
            Assert.Equal("attachment name", a.Name);
            Assert.Equal(a.NameEncoding, Encoding.Latin1);

            // Q encoding (lowercase)
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?q?attachment=20name?=");
            Assert.Equal("attachment name", a.Name);
            Assert.Equal(a.NameEncoding, Encoding.Latin1);

            // B encoding
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?B?YXR0YWNobWVudCBuYW1l?=");
            Assert.Equal("attachment name", a.Name);
            Assert.Equal(a.NameEncoding, Encoding.Latin1);

            // B encoding (lowercase)
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?b?YXR0YWNobWVudCBuYW1l?=");
            Assert.Equal("attachment name", a.Name);
            Assert.Equal(a.NameEncoding, Encoding.Latin1);

            // space alternate
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?q?attachment_name?=");
            Assert.Equal("attachment name", a.Name);
            Assert.Equal(a.NameEncoding, Encoding.Latin1);

            // question mark alternate
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?q?attachment=3Fname?=");
            Assert.Equal("attachment?name", a.Name);
            Assert.Equal(a.NameEncoding, Encoding.Latin1);

            // underscore alternate
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?q?attachment=5Fname?=");
            Assert.Equal("attachment_name", a.Name);
            Assert.Equal(a.NameEncoding, Encoding.Latin1);

            // multiple encoded-words
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?Q?attachment=20?= =?ISO-8859-1?Q?name?=");
            Assert.Equal("attachment name", a.Name);
            Assert.Equal(Encoding.Latin1, a.NameEncoding);

            // whitespace between encoded-words
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?Q?attachment=20?=    =?ISO-8859-1?Q?name?=");
            Assert.Equal("attachment name", a.Name);
            Assert.Equal(Encoding.Latin1, a.NameEncoding);

            // tab between encoded-words
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?Q?attachment=20?= \t   =?ISO-8859-1?Q?name?=");
            Assert.Equal("attachment name", a.Name);
            Assert.Equal(Encoding.Latin1, a.NameEncoding);

            // new-line whitespace between encoded-words
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?Q?attachment=20?=\r\n   =?ISO-8859-1?Q?name?=");
            Assert.Equal("attachment name", a.Name);
            Assert.Equal(Encoding.Latin1, a.NameEncoding);

            // multiple different encodings
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?Q?attachment=20?= =?UTF-8?Q?name?=");
            Assert.Equal("attachment name", a.Name);
            Assert.Equal(Encoding.Latin1, a.NameEncoding);
        }

        [Theory]
        [InlineData("=?Q?foo?=")] // missing charset
        [InlineData("=?ISO-8859-1?foo?=")] // missing encoding
        [InlineData("=?ISO-8859-1?qb?foo?=")] // two letter encoding
        [InlineData("=?ISO-8859-1?Q?foo?")] // missing end =
        [InlineData("=?ISO-8859-1?Q?foo")] // missing whole end
        [InlineData("=?ISO-8859-1?Q?foo_=?=")] // broken Q encoding, = at end
        [InlineData("=?ISO-8859-1?Q?foo_=A?=")] // broken Q encoding, single hex digit
        [InlineData("=?ISO-8859-1?Q?foo_?A?=")] // broken Q encoding, ? in text
        [InlineData("=?ISO-8859-1?Q?foo bar?=")] // broken Q encoding, space in text
        [InlineData("=?ISO-8859-1?Q?foo\tbar?=")] // broken Q encoding, tab in text
        [InlineData("=?ISO-8859-1?Q?foo\rbar?=")] // broken Q encoding, new line (CR) in text
        [InlineData("=?ISO-8859-1?Q?foo\nbar?=")] // broken Q encoding, new line (LF) in text
        [InlineData("=?ISO-8859-1?Q?foo\r\nbar?=")] // broken Q encoding, new line (CRLF) in text
        [InlineData("=?ISO?8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO(8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO<8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO@8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO,8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO;8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO:8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO/8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO[8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO.8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO=8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO\"8859-1?Q?foo_bar?=")] // prohibited char in charset
        [InlineData("=?ISO-8859-1?Q??foo_bar?=")] // prohibited char in encoding
        [InlineData("=?ISO-8859-1?Q(?foo_bar?=")] // prohibited char in encoding
        [InlineData("=?ISO-8859-1?Q<?foo_bar?=")] // prohibited char in encoding
        [InlineData("=?ISO-8859-1?Q@?foo_bar?=")] // prohibited char in encoding
        [InlineData("=?ISO-8859-1?Q,?foo_bar?=")] // prohibited char in encoding
        [InlineData("=?ISO-8859-1?Q;?foo_bar?=")] // prohibited char in encoding
        [InlineData("=?ISO-8859-1?Q:?foo_bar?=")] // prohibited char in encoding
        [InlineData("=?ISO-8859-1?Q/?foo_bar?=")] // prohibited char in encoding
        [InlineData("=?ISO-8859-1?Q[?foo_bar?=")] // prohibited char in encoding
        [InlineData("=?ISO-8859-1?Q.?foo_bar?=")] // prohibited char in encoding
        [InlineData("=?ISO-8859-1?Q=?foo_bar?=")] // prohibited char in encoding
        [InlineData("=?ISO-8859-1?Q\"?foo_bar?=")] // prohibited char in encoding
        public void NameParsingAndEncodingDetection_BadInputs(string attachmentName)
        {
            Attachment a = new Attachment(new MemoryStream(), attachmentName);
            Assert.Equal(attachmentName, a.Name);
            Assert.Null(a.NameEncoding);
        }

        [Fact]
        public void NameParsingAndEncodingDetection_BadInputsThrowing()
        {
            // Bad Base64 encoding
            Assert.Throws<FormatException>(() => new Attachment(new MemoryStream(), "=?ISO-8859-1?B?YXR0YWNobWV@@@@@@@@udCBuYW1l?="));

            // broken Q encoding, invalid hex value
            Assert.Throws<FormatException>(() => new Attachment(new MemoryStream(), "=?ISO-8859-1?Q?foo_=XY?="));

            // non existing encoding
            Assert.Throws<ArgumentException>(() => new Attachment(new MemoryStream(), "=?XXXX?Q?foo?="));
        }

        [Fact]
        public void NameParsingAndEncodingDetection_LengthValidation()
        {
            // 75 characters => decoded
            Attachment a;
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?Q?foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_fo?=");
            Assert.Equal("foo bar foo bar foo bar foo bar foo bar foo bar foo bar fo", a.Name);
            Assert.Equal(Encoding.Latin1, a.NameEncoding);

            // 76 charcters => RFC 2047 violation, not processed as encoded word
            a = new Attachment(new MemoryStream(), "=?ISO-8859-1?Q?foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo?=");
            Assert.Equal("=?ISO-8859-1?Q?foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo?=", a.Name);
            Assert.Null(a.NameEncoding);

            // 76 characters, different part of encoded word violating
            a = new Attachment(new MemoryStream(), "=?foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo?ISO-8859-1?Q?=");
            Assert.Equal("=?foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo?ISO-8859-1?Q?=", a.Name);
            Assert.Null(a.NameEncoding);

            // 76 characters, different part of encoded word violating
            a = new Attachment(new MemoryStream(), "=?Q?foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo?ISO-8859-1?=");
            Assert.Equal("=?Q?foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo_bar_foo?ISO-8859-1?=", a.Name);
            Assert.Null(a.NameEncoding);

            // over-length AND a non-existent charset: the length violation must be detected
            // before the charset is resolved, so this is treated as literal text rather
            // than throwing.
            string overLong = "=?XXXX?Q?" + new string('a', 70) + "?=";
            a = new Attachment(new MemoryStream(), overLong);
            Assert.Equal(overLong, a.Name);
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
            Assert.Null(a2.Name);
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
