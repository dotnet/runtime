// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// AttachmentCollectionTest.cs - NUnit Test Cases for System.Net.MailAddress.AttachmentCollection
//
// Authors:
//   John Luke (john.luke@gmail.com)
//
// (C) 2005 John Luke
//

using Xunit;
using System.Net.Mime;

namespace System.Net.Mail.Tests
{
    public class AttachmentCollectionTest
    {
        AttachmentCollection ac;
        Attachment a;

        public AttachmentCollectionTest()
        {
            ac = new MailMessage("foo@bar.com", "foo@bar.com").Attachments;
            a = Attachment.CreateAttachmentFromString("test", new ContentType("text/plain"));
        }

        [Fact]
        public void InitialCount()
        {
            Assert.Equal(0, ac.Count);
        }

        [Fact]
        public void AddCount()
        {
            ac.Add(a);
            Assert.Equal(1, ac.Count);
        }

        [Fact]
        public void RemoveCount()
        {
            ac.Remove(a);
            Assert.Equal(0, ac.Count);
        }

        [Fact]
        public void TestDispose()
        {
            ac.Add(a);
            ac.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ac.Add(a));
            Assert.Throws<ObjectDisposedException>(() => ac.Clear());

            // No throw on second dispose
            ac.Dispose();
        }
    }
}
