// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Mail.Tests
{
    public class ReplyToListTest
    {
        private Message _message = new Message();

        [Fact]
        public void ReplyToList_WithNoReplyToSet_ShouldReturnEmptyList()
        {
            Assert.Equal(0, _message.ReplyToList.Count);
        }

        [Fact]
        public void ReplyTo_WithReplyToListEmpty_ShouldReturnNull()
        {
            Assert.Null(_message.ReplyTo);
        }

        [Fact]
        public void PrepareHeaders_WithReplyToSet_ShouldIgnoreReplyToList()
        {
            MailAddress m = new MailAddress("test@example.com");
            MailAddress m2 = new MailAddress("test2@example.com");
            MailAddress m3 = new MailAddress("test3@example.com");

            _message.ReplyToList.Add(m);
            _message.ReplyToList.Add(m2);

            _message.ReplyTo = m3;

            _message.From = new MailAddress("from@example.com");

            _message.PrepareHeaders(true, false);

            Assert.Equal(2, _message.ReplyToList.Count);

            string[] s = _message.Headers.GetValues("Reply-To");
            Assert.Equal(1, s.Length);

            Assert.False(s[0].Contains("test@example.com"));
            Assert.False(s[0].Contains("test2@example.com"));
            Assert.True(s[0].Contains("test3@example.com"));
        }

        [Fact]
        public void PrepareHeaders_WithReplyToNull_AndReplyToListSet_ShouldUseReplyToList()
        {
            MailAddress m = new MailAddress("test@example.com");
            MailAddress m2 = new MailAddress("test2@example.com");
            MailAddress m3 = new MailAddress("test3@example.com");

            _message.ReplyToList.Add(m);
            _message.ReplyToList.Add(m2);
            _message.ReplyToList.Add(m3);

            _message.From = new MailAddress("from@example.com");

            _message.PrepareHeaders(true, false);

            Assert.Equal(3, _message.ReplyToList.Count);

            string[] s = _message.Headers.GetValues("Reply-To");
            Assert.Equal(1, s.Length);

            Assert.True(s[0].Contains("test@example.com"));
            Assert.True(s[0].Contains("test2@example.com"));
            Assert.True(s[0].Contains("test3@example.com"));

            Assert.Null(_message.ReplyTo);
        }

        [Fact]
        public void PrepareHeaders_WithReplyToListSet_AndReplyToHeaderSetManually_ShouldEnforceReplyToListIsSingleton()
        {
            MailAddress m = new MailAddress("test@example.com");
            MailAddress m2 = new MailAddress("test2@example.com");
            MailAddress m3 = new MailAddress("test3@example.com");

            _message.ReplyToList.Add(m);
            _message.ReplyToList.Add(m2);
            _message.ReplyToList.Add(m3);

            _message.Headers.Add("Reply-To", "shouldnotbeset@example.com");

            _message.From = new MailAddress("from@example.com");

            _message.PrepareHeaders(true, false);

            Assert.True(_message.ReplyToList.Count == 3, "ReplyToList did not contain all email addresses");

            string[] s = _message.Headers.GetValues("Reply-To");
            Assert.Equal(1, s.Length);

            Assert.True(s[0].Contains("test@example.com"));
            Assert.True(s[0].Contains("test2@example.com"));
            Assert.True(s[0].Contains("test3@example.com"));
            Assert.False(s[0].Contains("shouldnotbeset@example.com"));

            Assert.Null(_message.ReplyTo);
        }
    }
}
