// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// AlternateViewTest.cs - Unit Test Cases for System.Net.MailAddress.AlternateView
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
        [Fact]
        public void ArgumentNullExceptionTest()
        {
            Assert.Throws<ArgumentNullException>(() => new AlternateView((string)null));
        }

        [Fact]
        public void ArgumentNullException2Test()
        {
            Assert.Throws<ArgumentNullException>(() => new AlternateView((Stream)null));
        }

        [Fact]
        public void ContentTypeTest()
        {
            AlternateView av = AlternateView.CreateAlternateViewFromString("test", new ContentType("text/plain"));
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
            AlternateView av = AlternateView.CreateAlternateViewFromString("test", new ContentType("text/plain"));
            Assert.NotNull(av.ContentStream);
            Assert.Equal(4, av.ContentStream.Length);
        }

        [Fact]
        public void TransferEncodingTest()
        {
            AlternateView av = AlternateView.CreateAlternateViewFromString("test", new ContentType("text/plain"));
            Assert.Equal(TransferEncoding.QuotedPrintable, av.TransferEncoding);

            MemoryStream ms = new MemoryStream(new byte[] { 1, 2, 3 });
            Assert.Equal(TransferEncoding.Base64, new AlternateView(ms).TransferEncoding);
            Assert.Equal(TransferEncoding.Base64, new AlternateView(ms, "text/plain").TransferEncoding);
        }

        [Fact]
        public void CreateAlternateViewFromStringEncodingNull()
        {
            AlternateView av = AlternateView.CreateAlternateViewFromString("<p>test message</p>", null, "text/html");
            Assert.Equal(Text.Encoding.ASCII.BodyName, av.ContentType.CharSet);
        }

        [Theory]
        [InlineData("", UriKind.Relative)]
        [InlineData("aaaa", UriKind.Relative)]
        [InlineData("aaaň", UriKind.Relative)]
        [InlineData("aa-aa", UriKind.Relative)]
        [InlineData("aa_aa", UriKind.Relative)]
        [InlineData("aa\"aa", UriKind.Relative)]
        [InlineData("aa\\aa", UriKind.Relative)]
        [InlineData("aa/aa", UriKind.Relative)]
        [InlineData("aa@aa", UriKind.Relative)]
        [InlineData("aa; bb=cc", UriKind.Relative)]
        [InlineData("C:\\foo.txt", UriKind.Absolute)]
        public void ContentLocation_Validation_Valid(string uri, UriKind kind)
        {
            AlternateView av = AlternateView.CreateAlternateViewFromString("foo");
            av.BaseUri = new Uri(uri, kind);
        }

        [Theory]
        [InlineData("aa\raa", UriKind.Relative)]
        [InlineData("aa\naa", UriKind.Relative)]
        [InlineData("aa\r\naa", UriKind.Relative)]
        [InlineData("aa\r\n aa", UriKind.Relative)]
        public void ContentLocation_Validation_Invalid(string uri, UriKind kind)
        {
            AlternateView av = AlternateView.CreateAlternateViewFromString("foo");
            Uri u = new Uri(uri, kind);
            Assert.Throws<ArgumentException>(() =>
            {
                av.BaseUri = u;
            });
        }
    }
}
