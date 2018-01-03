// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// MailMessageTest.cs - NUnit Test Cases for System.Net.MailAddress.MailMessage
//
// Authors:
//   John Luke (john.luke@gmail.com)
//
// (C) 2005, 2006 John Luke
//

using System.IO;
using System.Reflection;
using System.Text;
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
            Attachment a = Attachment.CreateAttachmentFromString("blah blah", "text/plain");
            messageWithSubjectAndBody.Attachments.Add(a);

            emptyMessage = new MailMessage("from@example.com", "r1@t1.com, r2@t1.com");
        }

        [Fact]
        public void TestRecipients()
        {
            Assert.Equal(emptyMessage.To.Count, 2);
            Assert.Equal(emptyMessage.To[0].Address, "r1@t1.com");
            Assert.Equal(emptyMessage.To[1].Address, "r2@t1.com");
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
            Assert.Equal(messageWithSubjectAndBody.AlternateViews.Count, 1);
            AlternateView av = messageWithSubjectAndBody.AlternateViews[0];
            Assert.Equal(0, av.LinkedResources.Count);
            Assert.Equal("text/html; charset=us-ascii", av.ContentType.ToString());
        }

        [Fact]
        public void AttachmentTest()
        {
            Assert.Equal(messageWithSubjectAndBody.Attachments.Count, 1);
            Attachment at = messageWithSubjectAndBody.Attachments[0];
            Assert.Equal("text/plain", at.ContentType.MediaType);
        }

        [Fact]
        public void BodyTest()
        {
            Assert.Equal(messageWithSubjectAndBody.Body, "hello");
        }

        [Fact]
        public void BodyEncodingTest()
        {
            Assert.Equal(messageWithSubjectAndBody.BodyEncoding, Encoding.ASCII);
        }

        [Fact]
        public void FromTest()
        {
            Assert.Equal(messageWithSubjectAndBody.From.Address, "from@example.com");
        }

        [Fact]
        public void IsBodyHtmlTest()
        {
            Assert.False(messageWithSubjectAndBody.IsBodyHtml);
        }

        [Fact]
        public void PriorityTest()
        {
            Assert.Equal(messageWithSubjectAndBody.Priority, MailPriority.Normal);
        }

        [Fact]
        public void SubjectTest()
        {
            Assert.Equal(messageWithSubjectAndBody.Subject, "the subject");
        }

        [Fact]
        public void ToTest()
        {
            Assert.Equal(messageWithSubjectAndBody.To.Count, 1);
            Assert.Equal(messageWithSubjectAndBody.To[0].Address, "to@example.com");

            messageWithSubjectAndBody = new MailMessage();
            messageWithSubjectAndBody.To.Add("to@example.com");
            messageWithSubjectAndBody.To.Add("you@nowhere.com");
            Assert.Equal(messageWithSubjectAndBody.To.Count, 2);
            Assert.Equal(messageWithSubjectAndBody.To[0].Address, "to@example.com");
            Assert.Equal(messageWithSubjectAndBody.To[1].Address, "you@nowhere.com");
        }

        [Fact]
        public void BodyAndEncodingTest()
        {
            MailMessage msg = new MailMessage("from@example.com", "to@example.com");
            Assert.Equal(null, msg.BodyEncoding);
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
            Assert.Equal(null, msg.SubjectEncoding);
            msg.Subject = "test";
            Assert.Equal(null, msg.SubjectEncoding);
            msg.Subject = "test\u3067\u3059";
            Assert.Equal(Encoding.UTF8.CodePage, msg.SubjectEncoding.CodePage);
            msg.SubjectEncoding = null;
            msg.Subject = "test\u3067\u3059";
            Assert.Equal(Encoding.UTF8.CodePage, msg.SubjectEncoding.CodePage);
        }
        
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Requires fix shipping in .NET 4.7.2")]
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
                string decodedAttachment = DecodeSentMailMessage(message);
                
                // Make sure last byte is not encoded twice.
                Assert.Equal(specialLength, decodedAttachment.Length);
                Assert.Equal("AAAAAAAAAAAAAAAAZ", decodedAttachment.Substring(34800));
            }
        }
        
        private static string DecodeSentMailMessage(MailMessage mail)
        {
            // Create a MIME message that would be sent using System.Net.Mail.
            var stream = new MemoryStream();
            var mailWriterType = mail.GetType().Assembly.GetType("System.Net.Mail.MailWriter");
            var mailWriter = Activator.CreateInstance(
                                type: mailWriterType,
                                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
                                binder: null,
                                args: new object[] { stream },
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
            string encodedAttachment = result.Split(new[] { "attachment" }, StringSplitOptions.None)[1].Trim().Split('-')[0].Trim();
            byte[] data = Convert.FromBase64String(encodedAttachment);
            string decodedString = Encoding.UTF8.GetString(data);

            return decodedString;
        }
    }
}
