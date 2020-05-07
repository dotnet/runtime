// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            Assert.Equal(@"http://foo/bar/baz#frag", uri.ToString());

            Assert.Equal(@"/bar/baz", uri.AbsolutePath);

            Assert.Equal(@"http://foo/bar/baz#frag", uri.AbsoluteUri);

            Assert.Equal(@"foo", uri.Authority);

            Assert.Equal(@"foo", uri.DnsSafeHost);

            Assert.Equal(@"#frag", uri.Fragment);

            Assert.Equal(@"foo", uri.Host);

            Assert.Equal(UriHostNameType.Dns, uri.HostNameType);

            Assert.True(uri.IsAbsoluteUri);

            Assert.True(uri.IsDefaultPort);

            Assert.False(uri.IsFile);

            Assert.False(uri.IsLoopback);

            Assert.False(uri.IsUnc);

            Assert.Equal(@"/bar/baz", uri.LocalPath);

            Assert.Equal(@"http://foo/bar/baz#frag", uri.OriginalString);

            Assert.Equal(@"/bar/baz", uri.PathAndQuery);

            Assert.Equal(80, uri.Port);

            Assert.Equal(@"", uri.Query);

            Assert.Equal(@"http", uri.Scheme);

            string[] ss = uri.Segments;
            Assert.Equal(3, ss.Length);
            Assert.Equal(@"/", ss[0]);
            Assert.Equal(@"bar/", ss[1]);
            Assert.Equal(@"baz", ss[2]);

            Assert.Equal(dontEscape, uri.UserEscaped);

            Assert.Equal(@"", uri.UserInfo);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public static void TestCtor_Uri_String_Boolean(bool dontEscape)
        {
            Uri uri = new Uri(@"http://www.contoso.com/");
#pragma warning disable 0618
            uri = new Uri(uri, "catalog/shownew.htm?date=today", dontEscape);
#pragma warning restore 0618

            Assert.Equal(@"http://www.contoso.com/catalog/shownew.htm?date=today", uri.ToString());

            Assert.Equal(@"/catalog/shownew.htm", uri.AbsolutePath);

            Assert.Equal(@"http://www.contoso.com/catalog/shownew.htm?date=today", uri.AbsoluteUri);

            Assert.Equal(@"www.contoso.com", uri.Authority);

            Assert.Equal(@"www.contoso.com", uri.DnsSafeHost);

            Assert.Equal(@"", uri.Fragment);

            Assert.Equal(@"www.contoso.com", uri.Host);

            Assert.Equal(UriHostNameType.Dns, uri.HostNameType);

            Assert.True(uri.IsAbsoluteUri);

            Assert.True(uri.IsDefaultPort);

            Assert.False(uri.IsFile);

            Assert.False(uri.IsLoopback);

            Assert.False(uri.IsUnc);

            Assert.Equal(@"/catalog/shownew.htm", uri.LocalPath);

            Assert.Equal(@"http://www.contoso.com/catalog/shownew.htm?date=today", uri.OriginalString);

            Assert.Equal(@"/catalog/shownew.htm?date=today", uri.PathAndQuery);

            Assert.Equal(80, uri.Port);

            Assert.Equal(@"?date=today", uri.Query);

            Assert.Equal(@"http", uri.Scheme);

            string[] ss = uri.Segments;
            Assert.Equal(3, ss.Length);
            Assert.Equal(@"/", ss[0]);
            Assert.Equal(@"catalog/", ss[1]);
            Assert.Equal(@"shownew.htm", ss[2]);

            Assert.Equal(dontEscape, uri.UserEscaped);

            Assert.Equal(@"", uri.UserInfo);
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
            char testChar = 'e';
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
            AssertExtensions.Throws<ArgumentException>("digit", () => Uri.FromHex('?'));
            Assert.Throws<ArgumentOutOfRangeException>(() => Uri.HexEscape('\x100'));
            int index = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => Uri.HexUnescape("%75", ref index));
            index = 0;
            Uri.HexUnescape("%75", ref index);
            Assert.Throws<ArgumentOutOfRangeException>(() => Uri.HexUnescape("%75", ref index));
        }
    }
}
