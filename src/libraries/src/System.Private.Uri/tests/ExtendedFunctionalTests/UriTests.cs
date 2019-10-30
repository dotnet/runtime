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
            string s;
            bool b;
            UriHostNameType uriHostNameType;
            string[] ss;

            s = uri.ToString();
            Assert.Equal(@"http://foo/bar/baz#frag", s);

            s = uri.AbsolutePath;
            Assert.Equal(@"/bar/baz", s);

            s = uri.AbsoluteUri;
            Assert.Equal(@"http://foo/bar/baz#frag", s);

            s = uri.Authority;
            Assert.Equal(@"foo", s);

            s = uri.DnsSafeHost;
            Assert.Equal(@"foo", s);

            s = uri.Fragment;
            Assert.Equal(@"#frag", s);

            s = uri.Host;
            Assert.Equal(@"foo", s);

            uriHostNameType = uri.HostNameType;
            Assert.Equal<UriHostNameType>(UriHostNameType.Dns, uriHostNameType);

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
            Assert.Equal(@"/bar/baz", s);

            s = uri.OriginalString;
            Assert.Equal(@"http://foo/bar/baz#frag", s);

            s = uri.PathAndQuery;
            Assert.Equal(@"/bar/baz", s);

            i = uri.Port;
            Assert.Equal<int>(80, i);

            s = uri.Query;
            Assert.Equal(@"", s);

            s = uri.Scheme;
            Assert.Equal(@"http", s);

            ss = uri.Segments;
            Assert.Equal<int>(3, ss.Length);
            Assert.Equal(@"/", ss[0]);
            Assert.Equal(@"bar/", ss[1]);
            Assert.Equal(@"baz", ss[2]);

            b = uri.UserEscaped;
            Assert.Equal(b, dontEscape);

            s = uri.UserInfo;
            Assert.Equal(@"", s);
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
            string s;
            bool b;
            UriHostNameType uriHostNameType;
            string[] ss;

            s = uri.ToString();
            Assert.Equal(@"http://www.contoso.com/catalog/shownew.htm?date=today", s);

            s = uri.AbsolutePath;
            Assert.Equal(@"/catalog/shownew.htm", s);

            s = uri.AbsoluteUri;
            Assert.Equal(@"http://www.contoso.com/catalog/shownew.htm?date=today", s);

            s = uri.Authority;
            Assert.Equal(@"www.contoso.com", s);

            s = uri.DnsSafeHost;
            Assert.Equal(@"www.contoso.com", s);

            s = uri.Fragment;
            Assert.Equal(@"", s);

            s = uri.Host;
            Assert.Equal(@"www.contoso.com", s);

            uriHostNameType = uri.HostNameType;
            Assert.Equal<UriHostNameType>(UriHostNameType.Dns, uriHostNameType);

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
            Assert.Equal(@"/catalog/shownew.htm", s);

            s = uri.OriginalString;
            Assert.Equal(@"http://www.contoso.com/catalog/shownew.htm?date=today", s);

            s = uri.PathAndQuery;
            Assert.Equal(@"/catalog/shownew.htm?date=today", s);

            i = uri.Port;
            Assert.Equal<int>(80, i);

            s = uri.Query;
            Assert.Equal(@"?date=today", s);

            s = uri.Scheme;
            Assert.Equal(@"http", s);

            ss = uri.Segments;
            Assert.Equal<int>(3, ss.Length);
            Assert.Equal(@"/", ss[0]);
            Assert.Equal(@"catalog/", ss[1]);
            Assert.Equal(@"shownew.htm", ss[2]);

            b = uri.UserEscaped;
            Assert.Equal(b, dontEscape);

            s = uri.UserInfo;
            Assert.Equal(@"", s);
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

            Assert.Equal(@"index.htm", uriStr1);
            Assert.Equal(@"http://www.contoso.com/index.htm?date=today", uriStr2);
            Assert.Equal(@"index.htm", uriStr3);
        }

        [Fact]
        public static void TestHexMethods()
        {
            char  testChar = 'e';
            Assert.True(Uri.IsHexDigit(testChar));
            Assert.Equal(14, Uri.FromHex(testChar));

            string hexString = Uri.HexEscape(testChar);
            Assert.Equal("%65", hexString);

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
