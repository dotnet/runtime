// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.PrivateUri.Tests
{
    public static class UriTests
    {
        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public static void TestCtor_String_Boolean(bool dontEscape)
        {
#pragma warning disable 0618
#pragma warning disable 0612
            Uri uri = new Uri(@"http://foo/bar/baz#frag", dontEscape);
#pragma warning restore 0612
#pragma warning restore 0618

            int i;
            String s;
            bool b;
            UriHostNameType uriHostNameType;
            String[] ss;

            s = uri.ToString();
            Assert.Equal(s, @"http://foo/bar/baz#frag");

            s = uri.AbsolutePath;
            Assert.Equal<String>(s, @"/bar/baz");

            s = uri.AbsoluteUri;
            Assert.Equal<String>(s, @"http://foo/bar/baz#frag");

            s = uri.Authority;
            Assert.Equal<String>(s, @"foo");

            s = uri.DnsSafeHost;
            Assert.Equal<String>(s, @"foo");

            s = uri.Fragment;
            Assert.Equal<String>(s, @"#frag");

            s = uri.Host;
            Assert.Equal<String>(s, @"foo");

            uriHostNameType = uri.HostNameType;
            Assert.Equal<UriHostNameType>(uriHostNameType, UriHostNameType.Dns);

            b = uri.IsAbsoluteUri;
            Assert.True(b);

            b = uri.IsDefaultPort;
            Assert.True(b);

            b = uri.IsFile;
            Assert.False(b);

            b = uri.IsLoopback;
            Assert.False(b);

            b = uri.IsUnc;
            Assert.False(b);

            s = uri.LocalPath;
            Assert.Equal<String>(s, @"/bar/baz");

            s = uri.OriginalString;
            Assert.Equal<String>(s, @"http://foo/bar/baz#frag");

            s = uri.PathAndQuery;
            Assert.Equal<String>(s, @"/bar/baz");

            i = uri.Port;
            Assert.Equal<int>(i, 80);

            s = uri.Query;
            Assert.Equal<String>(s, @"");

            s = uri.Scheme;
            Assert.Equal<String>(s, @"http");

            ss = uri.Segments;
            Assert.Equal<int>(ss.Length, 3);
            Assert.Equal<String>(ss[0], @"/");
            Assert.Equal<String>(ss[1], @"bar/");
            Assert.Equal<String>(ss[2], @"baz");

            b = uri.UserEscaped;
            Assert.Equal(b, dontEscape);

            s = uri.UserInfo;
            Assert.Equal<String>(s, @"");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public static void TestCtor_Uri_String_Boolean(bool dontEscape)
        {
            Uri uri;

            uri = new Uri(@"http://www.contoso.com/");
#pragma warning disable 0618
            uri = new Uri(uri, "catalog/shownew.htm?date=today", dontEscape);
#pragma warning restore 0618

            int i;
            String s;
            bool b;
            UriHostNameType uriHostNameType;
            String[] ss;

            s = uri.ToString();
            Assert.Equal(s, @"http://www.contoso.com/catalog/shownew.htm?date=today");

            s = uri.AbsolutePath;
            Assert.Equal<String>(s, @"/catalog/shownew.htm");

            s = uri.AbsoluteUri;
            Assert.Equal<String>(s, @"http://www.contoso.com/catalog/shownew.htm?date=today");

            s = uri.Authority;
            Assert.Equal<String>(s, @"www.contoso.com");

            s = uri.DnsSafeHost;
            Assert.Equal<String>(s, @"www.contoso.com");

            s = uri.Fragment;
            Assert.Equal<String>(s, @"");

            s = uri.Host;
            Assert.Equal<String>(s, @"www.contoso.com");

            uriHostNameType = uri.HostNameType;
            Assert.Equal<UriHostNameType>(uriHostNameType, UriHostNameType.Dns);

            b = uri.IsAbsoluteUri;
            Assert.True(b);

            b = uri.IsDefaultPort;
            Assert.True(b);

            b = uri.IsFile;
            Assert.False(b);

            b = uri.IsLoopback;
            Assert.False(b);

            b = uri.IsUnc;
            Assert.False(b);

            s = uri.LocalPath;
            Assert.Equal<String>(s, @"/catalog/shownew.htm");

            s = uri.OriginalString;
            Assert.Equal<String>(s, @"http://www.contoso.com/catalog/shownew.htm?date=today");

            s = uri.PathAndQuery;
            Assert.Equal<String>(s, @"/catalog/shownew.htm?date=today");

            i = uri.Port;
            Assert.Equal<int>(i, 80);

            s = uri.Query;
            Assert.Equal<String>(s, @"?date=today");

            s = uri.Scheme;
            Assert.Equal<String>(s, @"http");

            ss = uri.Segments;
            Assert.Equal<int>(ss.Length, 3);
            Assert.Equal<String>(ss[0], @"/");
            Assert.Equal<String>(ss[1], @"catalog/");
            Assert.Equal<String>(ss[2], @"shownew.htm");

            b = uri.UserEscaped;
            Assert.Equal(b, dontEscape);

            s = uri.UserInfo;
            Assert.Equal<String>(s, @"");
        }

        [Fact]
        public static void TestMakeRelative_Invalid()
        {
            var baseUri = new Uri("http://www.domain.com/");
            var relativeUri = new Uri("/path/", UriKind.Relative);
#pragma warning disable 0618
            AssertExtensions.Throws<ArgumentNullException>("toUri", () => baseUri.MakeRelative(null)); // Uri is null

            Assert.Throws<InvalidOperationException>(() => relativeUri.MakeRelative(baseUri)); // Base uri is relative
            Assert.Throws<InvalidOperationException>(() => baseUri.MakeRelative(relativeUri)); // Uri is relative
#pragma warning restore 0618
        }

        [Fact]
        public static void TestMakeRelative()
        {
            // Create a base Uri.
            Uri address1 = new Uri("http://www.contoso.com/");
            Uri address2 = new Uri("http://www.contoso.com:8000/");
            Uri address3 = new Uri("http://username@www.contoso.com/");

            // Create a new Uri from a string.
            Uri address4 = new Uri("http://www.contoso.com/index.htm?date=today");
#pragma warning disable 0618
            // Determine the relative Uri.
            string uriStr1 = address1.MakeRelative(address4);
            string uriStr2 = address2.MakeRelative(address4);
            string uriStr3 = address3.MakeRelative(address4);
#pragma warning restore 0618

            Assert.Equal(uriStr1, @"index.htm");
            Assert.Equal(uriStr2, @"http://www.contoso.com/index.htm?date=today");
            Assert.Equal(uriStr3, @"index.htm");
        }

        [Fact]
        public static void TestHexMethods()
        {
            char  testChar = 'e';
            Assert.True(Uri.IsHexDigit(testChar));
            Assert.Equal(14, Uri.FromHex(testChar));

            string hexString = Uri.HexEscape(testChar);
            Assert.Equal(hexString, "%65");

            int index = 0;
            Assert.True(Uri.IsHexEncoding(hexString, index));
            Assert.Equal(testChar, Uri.HexUnescape(hexString, ref index));
        }

        [Fact]
        public static void TestHexMethods_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Uri.FromHex('?'));
            Assert.Throws<ArgumentOutOfRangeException>(() => Uri.HexEscape('\x100'));
            int index = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => Uri.HexUnescape("%75", ref index));
            index = 0;
            Uri.HexUnescape("%75", ref index);
            Assert.Throws<ArgumentOutOfRangeException>(() => Uri.HexUnescape("%75", ref index));
        }
	}
}
