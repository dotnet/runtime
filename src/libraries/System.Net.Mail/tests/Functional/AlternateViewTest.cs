// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// AlternateViewTest.cs - NUnit Test Cases for System.Net.MailAddress.AlternateView
//
// Authors:
//   John Luke (john.luke@gmail.com)
//
// (C) 2005 John Luke
//

using System;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class AlternateViewTest
    {
        AlternateView av;

        public AlternateViewTest()
        {
            av = AlternateView.CreateAlternateViewFromString("test", new ContentType("text/plain"));
        }

        [Fact]
        public void ArgumentNullExceptionTest()
        {
            string s = null;
            Assert.Throws<ArgumentNullException>(() => new AlternateView(s));
        }

        [Fact]
        public void ArgumentNullException2Test()
        {
            Stream s = null;
            Assert.Throws<ArgumentNullException>(() => new AlternateView(s));
        }

        [Fact]
        public void ContentTypeTest()
        {
            Assert.NotNull(av.ContentType);
            Assert.Equal("text/plain", av.ContentType.MediaType);
        }

        [Fact]
        public void ContentType2Test()
        {
            AlternateView av = new AlternateView(new MemoryStream());
            Assert.NotNull(av.ContentType);
            Assert.Equal("application/octet-stream", av.ContentType.MediaType);
        }

        [Fact]
        public void ContentStreamTest()
        {
            Assert.NotNull(av.ContentStream);
            Assert.Equal(4, av.ContentStream.Length);
        }

        [Fact]
        public void TransferEncodingTest()
        {
            Assert.Equal(TransferEncoding.QuotedPrintable, av.TransferEncoding);

            MemoryStream ms = new MemoryStream(new byte[] { 1, 2, 3 });
            Assert.Equal(TransferEncoding.Base64, new AlternateView(ms).TransferEncoding);
            Assert.Equal(TransferEncoding.Base64, new AlternateView(ms, "text/plain").TransferEncoding);
        }

        [Fact]
        public void CreateAlternateViewFromStringEncodingNull()
        {
            AlternateView.CreateAlternateViewFromString("<p>test message</p>", null, "text/html");
        }
    }
}
