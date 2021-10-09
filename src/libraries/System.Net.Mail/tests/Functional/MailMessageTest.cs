// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// MailMessageTest.cs - Unit Test Cases for System.Net.MailAddress.MailMessage
//
// Authors:
//   John Luke (john.luke@gmail.com)
//
// (C) 2005, 2006 John Luke
//

using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class MailMessageTest
    {
        MailMessage messageWithSubjectAndBody;
        MailMessage emptyMessage;

        public MailMessageTest()
        {
            messageWithSubjectAndBody = new MailMessage("from@example.com", "to@example.com");
            messageWithSubjectAndBody.Subject = "the subject";
            messageWithSubjectAndBody.Body = "hello";
            messageWithSubjectAndBody.AlternateViews.Add(AlternateView.CreateAlternateViewFromString("<html><body>hello</body></html>", null, "text/html"));
            Attachment a = Attachment.CreateAttachmentFromString("blah blah", "AttachmentName");
            messageWithSubjectAndBody.Attachments.Add(a);

            emptyMessage = new MailMessage("from@example.com", "r1@t1.com, r2@t1.com");
        }

        [Fact]
        public void TestRecipients()
        {
            Assert.Equal(2, emptyMessage.To.Count);
            Assert.Equal("r1@t1.com", emptyMessage.To[0].Address);
            Assert.Equal("r2@t1.com", emptyMessage.To[1].Address);
        }

        [Fact]
        public void TestForNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MailMessage("from@example.com", null));
            Assert.Throws<ArgumentNullException>(() => new MailMessage(null, new MailAddress("to@example.com")));
            Assert.Throws<ArgumentNullException>(() => new MailMessage(new MailAddress("from@example.com"), null));
            Assert.Throws<ArgumentNullException>(() => new MailMessage(null, "to@example.com"));
        }

        [Fact]
        public void AlternateViewTest()
        {
            Assert.Equal(1, messageWithSubjectAndBody.AlternateViews.Count);
            AlternateView av = messageWithSubjectAndBody.AlternateViews[0];
            Assert.Equal(0, av.LinkedResources.Count);
            Assert.Equal("text/html; charset=us-ascii", av.ContentType.ToString());
        }

        [Fact]
        public void AttachmentTest()
        {
            Assert.Equal(1, messageWithSubjectAndBody.Attachments.Count);
            Attachment at = messageWithSubjectAndBody.Attachments[0];
            Assert.Equal("text/plain", at.ContentType.MediaType);
            Assert.Equal("AttachmentName", at.ContentType.Name);
            Assert.Equal("AttachmentName", at.Name);
        }

        [Fact]
        public void BodyTest()
        {
            Assert.Equal("hello", messageWithSubjectAndBody.Body);
        }

        [Fact]
        public void BodyEncodingTest()
        {
            Assert.Equal(messageWithSubjectAndBody.BodyEncoding, Encoding.ASCII);
        }

        [Fact]
        public void FromTest()
        {
            Assert.Equal("from@example.com", messageWithSubjectAndBody.From.Address);
        }

        [Fact]
        public void IsBodyHtmlTest()
        {
            Assert.False(messageWithSubjectAndBody.IsBodyHtml);
        }

        [Fact]
        public void PriorityTest()
        {
            Assert.Equal(MailPriority.Normal, messageWithSubjectAndBody.Priority);
        }

        [Fact]
        public void SubjectTest()
        {
            Assert.Equal("the subject", messageWithSubjectAndBody.Subject);
        }

        [Fact]
        public void ToTest()
        {
            Assert.Equal(1, messageWithSubjectAndBody.To.Count);
            Assert.Equal("to@example.com", messageWithSubjectAndBody.To[0].Address);

            messageWithSubjectAndBody = new MailMessage();
            messageWithSubjectAndBody.To.Add("to@example.com");
            messageWithSubjectAndBody.To.Add("you@nowhere.com");
            Assert.Equal(2, messageWithSubjectAndBody.To.Count);
            Assert.Equal("to@example.com", messageWithSubjectAndBody.To[0].Address);
            Assert.Equal("you@nowhere.com", messageWithSubjectAndBody.To[1].Address);
        }

        [Fact]
        public void BodyAndEncodingTest()
        {
            MailMessage msg = new MailMessage("from@example.com", "to@example.com");
            Assert.Null(msg.BodyEncoding);
            msg.Body = "test";
            Assert.Equal(Encoding.ASCII, msg.BodyEncoding);
            msg.Body = "test\u3067\u3059";
            Assert.Equal(Encoding.ASCII, msg.BodyEncoding);
            msg.BodyEncoding = null;
            msg.Body = "test\u3067\u3059";
            Assert.Equal(Encoding.UTF8.CodePage, msg.BodyEncoding.CodePage);
        }

        [Fact]
        public void SubjectAndEncodingTest()
        {
            MailMessage msg = new MailMessage("from@example.com", "to@example.com");
            Assert.Null(msg.SubjectEncoding);
            msg.Subject = "test";
            Assert.Null(msg.SubjectEncoding);
            msg.Subject = "test\u3067\u3059";
            Assert.Equal(Encoding.UTF8.CodePage, msg.SubjectEncoding.CodePage);
            msg.SubjectEncoding = null;
            msg.Subject = "test\u3067\u3059";
            Assert.Equal(Encoding.UTF8.CodePage, msg.SubjectEncoding.CodePage);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not passing as internal System.Net.Mail.MailWriter stripped from build")]
        public void SendMailMessageTest()
        {
            string expected = @"X-Sender: from@example.com
X-Receiver: to@example.com
MIME-Version: 1.0
From: from@example.com
To: to@example.com
Date: DATE
Subject: the subject
Content-Type: multipart/mixed;
 boundary=--boundary_1_GUID


----boundary_1_GUID
Content-Type: multipart/alternative;
 boundary=--boundary_0_GUID


----boundary_0_GUID
Content-Type: text/plain; charset=us-ascii
Content-Transfer-Encoding: quoted-printable

hello
----boundary_0_GUID
Content-Type: text/html; charset=us-ascii
Content-Transfer-Encoding: quoted-printable

<html><body>hello</body></html>
----boundary_0_GUID--

----boundary_1_GUID
Content-Type: text/plain; charset=us-ascii
Content-Disposition: attachment
Content-Transfer-Encoding: quoted-printable

blah blah
----boundary_1_GUID--
";
            expected = expected.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");

            string sent = DecodeSentMailMessage(messageWithSubjectAndBody).Raw;
            sent = Regex.Replace(sent, "Date:.*?\r\n", "Date: DATE\r\n");
            sent = Regex.Replace(sent, @"_.{8}-.{4}-.{4}-.{4}-.{12}", "_GUID");

            // name and charset can appear in different order
            Assert.Contains("; name=AttachmentName", sent);
            sent = sent.Replace("; name=AttachmentName", string.Empty);

            Assert.Equal(expected, sent);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not passing as internal System.Net.Mail.MailWriter stripped from build")]
        public void SentSpecialLengthMailAttachment_Base64Decode_Success()
        {
            // The special length follows pattern: (3N - 1) * 0x4400 + 1
            // This length will trigger WriteState.Padding = 2 & count = 1 (byte to write)
            // The smallest number to match the pattern is 34817.
            int specialLength = 34817;

            string stringLength34817 = new string('A', specialLength - 1) + 'Z';
            byte[] toBytes = Encoding.ASCII.GetBytes(stringLength34817);

            using (var tempFile = TempFile.Create(toBytes))
            {
                var message = new MailMessage("sender@test.com", "user1@pop.local", "testSubject", "testBody");
                message.Attachments.Add(new Attachment(tempFile.Path));

                string attachment = DecodeSentMailMessage(message).Attachment;
                string decodedAttachment = Encoding.UTF8.GetString(Convert.FromBase64String(attachment));

                // Make sure last byte is not encoded twice.
                Assert.Equal(specialLength, decodedAttachment.Length);
                Assert.Equal("AAAAAAAAAAAAAAAAZ", decodedAttachment.Substring(34800));
            }
        }

        private static (string Raw, string Attachment) DecodeSentMailMessage(MailMessage mail)
        {
            // Create a MIME message that would be sent using System.Net.Mail.
            var stream = new MemoryStream();
            var mailWriterType = mail.GetType().Assembly.GetType("System.Net.Mail.MailWriter");
            var mailWriter = Activator.CreateInstance(
                                type: mailWriterType,
                                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
                                binder: null,
                                args: new object[] { stream, true },    // true to encode message for transport
                                culture: null,
                                activationAttributes: null);

            // Send the message.
            mail.GetType().InvokeMember(
                                name: "Send",
                                invokeAttr: BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                                binder: null,
                                target: mail,
                                args: new object[] { mailWriter, true, true });

            // Decode contents.
            string result = Encoding.UTF8.GetString(stream.ToArray());
            string attachment = result.Split(new[] { "attachment" }, StringSplitOptions.None)[1].Trim().Split('-')[0].Trim();

            return (result, attachment);
        }
    }
}
