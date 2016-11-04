// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class MessageHeaderBehaviorTest
    {
        private Message _message = new Message();

        [Fact]
        public void Message_WithXSenderHeaderSet_ShouldNotOverwriteXSender()
        {
            _message.Headers.Add("X-Sender", "test@networking.com");
            _message.From = new MailAddress("test@example.com");
            _message.HeadersEncoding = Encoding.UTF8;

            _message.PrepareEnvelopeHeaders(true, false);
            _message.EncodeHeaders(_message.Headers, false);

            Assert.Equal("test@networking.com", _message.Headers["X-Sender"]);
        }

        [Fact]
        public void Message_WithNoXSender_ShouldSetXSenderToFromAddress()
        {
            _message.From = new MailAddress("test@example.com");
            _message.HeadersEncoding = Encoding.UTF8;

            _message.PrepareEnvelopeHeaders(true, false);
            _message.EncodeHeaders(_message.Headers, false);
            _message.EncodeHeaders(_message.EnvelopeHeaders, false);

            //if the X-Sender was not set, it will be set in EnvelopeHeaders instead of Headers.
            Assert.Equal("test@example.com", _message.EnvelopeHeaders["X-Sender"]);
        }

        [Fact]
        public void Message_WithXReceiverSetByUser_ShouldNotClearXReceiverHeader()
        {
            _message.Headers.Add("X-Receiver", "test@example.com");
            _message.From = new MailAddress("someaddress@example.com");
            _message.To.Add(new MailAddress("to@example.com"));
            _message.HeadersEncoding = Encoding.UTF8;

            _message.PrepareEnvelopeHeaders(true, false);
            Assert.NotNull(_message.EnvelopeHeaders.GetValues("X-Receiver"));
            _message.EncodeHeaders(_message.EnvelopeHeaders, false);
            _message.EncodeHeaders(_message.Headers, false);

            Assert.Equal(1, _message.EnvelopeHeaders.GetValues("X-Receiver").Length);
            Assert.Equal(1, _message.Headers.GetValues("X-Receiver").Length);
        }

        [Fact]
        public void Message_WithBccAddresses_ShouldSetXReceiverHeaderForBccAddresses()
        {
            _message.From = new MailAddress("someaddress@example.com");
            _message.To.Add(new MailAddress("to@example.com"));
            _message.HeadersEncoding = Encoding.UTF8;
            _message.Bcc.Add("shouldbeset@example.com");

            _message.PrepareEnvelopeHeaders(true, false);
            string[] xReceivers = _message.EnvelopeHeaders.GetValues("X-Receiver");
            Assert.True(xReceivers.Length == 2);
        }

        [Fact]
        public void Message_WithTo_WithCc_WithBcc_WithReplyToList_ShouldBeSingletons_ShouldDeleteUserSuppliedHeaders()
        {
            //all examples of headers that users may potentially try to set that we silently discard
            string fromHeader = "fromshouldberemoved@example.com";
            string toHeader = "toshouldberemoved@example.com";
            string ccHeader = "ccshouldberemoved@example.com";
            string bccHeader = "bccshouldberemoved@example.com";
            string replyToHeader = "replytoshouldberemoved@example.com";

            //values that will go to the properties- should replace headers with these
            string validFrom = "somefromaddress@example.com";
            string validTo = "validtoaddress@example.com";
            string validCc = "validccaddress@example.com";
            string validBcc = "validbccaddress@example.com";
            string validReplyTo = "validreplytoaddress@example.com";

            //add bad headers.  these are all on singleton header values and therefore should be removed
            //automatically by PrepareHeaders and replaced with correct values
            _message.Headers.Add("From", fromHeader);
            _message.Headers.Add("To", toHeader);
            _message.Headers.Add("Cc", ccHeader);
            _message.Headers.Add("Bcc", bccHeader);
            _message.Headers.Add("Reply-To", replyToHeader);

            //these values should overwrite all the headers above
            _message.From = new MailAddress(validFrom);
            _message.To.Add(new MailAddress(validTo));
            _message.CC.Add(new MailAddress(validCc));
            _message.Bcc.Add(new MailAddress(validBcc));
            _message.ReplyToList.Add(new MailAddress(validReplyTo));

            _message.PrepareHeaders(true, false);

            string[] fromHeaders = _message.Headers.GetValues("From");
            string[] toHeaders = _message.Headers.GetValues("To");
            string[] ccHeaders = _message.Headers.GetValues("Cc");
            string[] bccHeaders = _message.Headers.GetValues("Bcc");
            string[] replyToHeaders = _message.Headers.GetValues("Reply-To");

            //all headers supplied by the user should be gone
            Assert.Equal(1, fromHeaders.Length);
            Assert.Equal(1, toHeaders.Length);
            Assert.Equal(1, ccHeaders.Length);
            //there should be no bcc header
            Assert.Null(bccHeaders);
            Assert.Equal(1, replyToHeaders.Length);

            //all headers that were set via properties should remain
            Assert.True(fromHeaders[0].Contains(validFrom));
            Assert.True(toHeaders[0].Contains(validTo));
            Assert.True(ccHeaders[0].Contains(validCc));
            Assert.True(replyToHeaders[0].Contains(validReplyTo));
        }

        [Fact]
        public void MessagePriority_WithValueSetByHeader_AndImportanceNotSet_ShouldRespectHeader()
        {
            _message.From = new MailAddress("from@example.com");

            _message.Headers.Add("Priority", "non-urgent");
            _message.Headers.Add("Importance", "low");
            _message.Headers.Add("X-Priority", "5");

            _message.PrepareHeaders(true, false);

            Assert.Equal(MailPriority.Normal, _message.Priority);

            Assert.True(_message.Headers.GetValues("Importance").Length == 1, "importance was not set");
            Assert.True(_message.Headers.GetValues("X-Priority").Length == 1, "x-priority was not set");
            Assert.True(_message.Headers.GetValues("Priority").Length == 1, "priority was not set");

            Assert.True(_message.Headers.GetValues("Importance")[0].Contains("low"));
            Assert.True(_message.Headers.GetValues("X-Priority")[0].Contains("5"));
            Assert.True(_message.Headers.GetValues("Priority")[0].Contains("non-urgent"));
        }

        [Fact]
        public void MessageSubject_WhenSetViaHeaders_AndSubjectPropertyIsSet_ShouldUsePropertyValue()
        {
            _message.From = new MailAddress("from@example.com");
            _message.Headers.Add("Subject", "should be removed");
            _message.Subject = "correct subject";
            _message.PrepareHeaders(true, false);

            Assert.Equal("correct subject", _message.Subject);
            Assert.True(_message.Headers.GetValues("Subject").Length == 1);
            Assert.True(_message.Headers.GetValues("Subject")[0].Contains("correct subject"));
        }

        [Fact]
        public void MessageSubject_WhenEmpty_WithHeaderSet_ShouldDiscardHeaderAndLeaveSubjectEmpty()
        {
            _message.From = new MailAddress("from@example.com");
            _message.Headers.Add("Subject", "should be removed");
            _message.Subject = string.Empty;
            _message.PrepareHeaders(true, false);

            Assert.Equal(string.Empty, _message.Subject);
            Assert.Null(_message.Headers["Subject"]);
        }

        [Fact]
        public void MessageSubject_Unicode_Accepted()
        {
            _message.From = new MailAddress("from@example.com"); ;

            string input = "Hi \u00DC Bob";
            _message.Subject = input;

            Assert.Equal(Encoding.UTF8, _message.SubjectEncoding);

            _message.PrepareHeaders(true, false);

            Assert.Equal(input, _message.Subject);
            Assert.Equal("=?utf-8?B?SGkgw5wgQm9i?=", _message.Headers["Subject"]);
        }

        [Fact]
        public void MessageSubject_Utf8EncodedUnicode_Accepted()
        {
            _message.From = new MailAddress("from@example.com"); ;

            string input = "=?utf-8?B?SGkgw5wgQm9i?=";
            _message.Subject = input;

            Assert.Equal(Encoding.UTF8, _message.SubjectEncoding);

            _message.PrepareHeaders(true, false);

            Assert.Equal("Hi \u00DC Bob", _message.Subject);
            Assert.Equal(input, _message.Headers["Subject"]);
        }

        [Fact]
        public void MessageSubject_UnknownEncodingEncodedUnicode_Accepted()
        {
            _message.From = new MailAddress("from@example.com"); ;

            string input = "=?utf-99?B?SGkgw5wgQm9i?=";
            _message.Subject = input;

            Assert.Equal(null, _message.SubjectEncoding);

            _message.PrepareHeaders(true, false);

            Assert.Equal(input, _message.Subject);
            Assert.Equal(input, _message.Headers["Subject"]);
        }

        [Fact]
        public void MessageSubject_BadBase64EncodedUnicode_Accepted()
        {
            _message.From = new MailAddress("from@example.com"); ;

            string input = "=?utf-8?B?SGkgw5.wgQm9i?="; // Extra . in the middle
            _message.Subject = input;

            Assert.Equal(null, _message.SubjectEncoding);

            _message.PrepareHeaders(true, false);

            Assert.Equal(input, _message.Subject);
            Assert.Equal(input, _message.Headers["Subject"]);
        }

        [Fact]
        public void MessageSubject_MultiLineEncodedUnicode_Accepted()
        {
            _message.From = new MailAddress("from@example.com"); ;

            string input = "=?utf-8?B?SGkgw5wg?=\r\n =?utf-8?B?Qm9i?=";
            _message.Subject = input;

            Assert.Equal(Encoding.UTF8, _message.SubjectEncoding);

            _message.PrepareHeaders(true, false);

            Assert.Equal("Hi \u00DC Bob", _message.Subject);
            Assert.Equal("=?utf-8?B?SGkgw5wgQm9i?=", _message.Headers["Subject"]);
        }

        [Fact]
        public void MessageContentTypeHeader_WhenSetByUser_ShouldBeDiscarded()
        {
            _message.From = new MailAddress("from@example.com");
            _message.Headers.Add("Content-Type", "should be removed");
            _message.PrepareHeaders(true, false);

            // Content type header should be removed.
            Assert.Null(_message.Headers["Content-Type"]);
        }
    }
}
