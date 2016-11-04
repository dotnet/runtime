// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// LinkedResourceTest.cs - NUnit Test Cases for System.Net.MailAddress.LinkedResource
//
// Authors:
//   John Luke (john.luke@gmail.com)
//
// (C) 2005 John Luke
//

using System.IO;
using System.Net.Mime;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class LinkedResourceTest
    {
        LinkedResource lr;

        public LinkedResourceTest()
        {
            lr = LinkedResource.CreateLinkedResourceFromString("test", new ContentType("text/plain"));
        }

        [Fact]
        public void ThrowsOnNullString()
        {
            string s = null;
            Assert.Throws<ArgumentNullException>(() => new LinkedResource(s));
        }

        [Fact]
        public void ThrowsOnNullStream()
        {
            Stream s = null;
            Assert.Throws<ArgumentNullException>(() => new LinkedResource(s));
        }

        [Fact]
        public void TransferEncodingTest()
        {
            Assert.Equal(TransferEncoding.QuotedPrintable, lr.TransferEncoding);
        }
    }
}
