// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.PrivateUri.Tests
{
    public static class UriTests
    {
        public static IEnumerable<object[]> Uri_TestData
        {
            get
            {
                if (PlatformDetection.IsWindows)
                {
                    yield return new object[] { @"file:///path1\path2/path3\path4", @"/path1/path2/path3/path4", @"/path1/path2/path3/path4", @"file:///path1/path2/path3/path4", "" };
                    yield return new object[] { @"file:///path1%5Cpath2\path3", @"/path1/path2/path3", @"/path1/path2/path3", @"file:///path1/path2/path3", ""};
                    yield return new object[] { @"file://localhost/path1\path2/path3\path4\", @"/path1/path2/path3/path4/", @"\\localhost\path1\path2\path3\path4\", @"file://localhost/path1/path2/path3/path4/", "localhost"};
                    yield return new object[] { @"file://randomhost/path1%5Cpath2\path3", @"/path1/path2/path3", @"\\randomhost\path1\path2\path3", @"file://randomhost/path1/path2/path3", "randomhost"};
                }
                else
                {
                    yield return new object[] { @"file:///path1\path2/path3\path4", @"/path1%5Cpath2/path3%5Cpath4", @"/path1\path2/path3\path4", @"file:///path1%5Cpath2/path3%5Cpath4", "" };
                    yield return new object[] { @"file:///path1%5Cpath2\path3", @"/path1%5Cpath2%5Cpath3", @"/path1\path2\path3", @"file:///path1%5Cpath2%5Cpath3", ""};
                    yield return new object[] { @"file://localhost/path1\path2/path3\path4\", @"/path1%5Cpath2/path3%5Cpath4%5C", @"\\localhost\path1\path2\path3\path4\", @"file://localhost/path1%5Cpath2/path3%5Cpath4%5C", "localhost"};
                    yield return new object[] { @"file://randomhost/path1%5Cpath2\path3", @"/path1%5Cpath2%5Cpath3", @"\\randomhost\path1\path2\path3", @"file://randomhost/path1%5Cpath2%5Cpath3", "randomhost"};
                }
            }
        }

        [Theory]
        [MemberData(nameof(Uri_TestData))]
        public static void TestCtor_BackwardSlashInPath(string uri, string expectedAbsolutePath, string expectedLocalPath, string expectedAbsoluteUri, string expectedHost)
        {
            Uri actualUri = new Uri(uri);
            Assert.Equal(expectedAbsolutePath, actualUri.AbsolutePath);
            Assert.Equal(expectedLocalPath, actualUri.LocalPath);
            Assert.Equal(expectedAbsoluteUri, actualUri.AbsoluteUri);
            Assert.Equal(expectedHost, actualUri.Host);
        }

        [Fact]
        public static void TestCtor_String()
        {
            Uri uri = new Uri(@"http://foo/bar/baz#frag");

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

            Assert.False(uri.UserEscaped);

            Assert.Equal(@"", uri.UserInfo);
        }

        [Fact]
        public static void TestCtor_Uri_String()
        {
            Uri uri = new Uri(@"http://www.contoso.com/");
            uri = new Uri(uri, "catalog/shownew.htm?date=today");

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

            Assert.False(uri.UserEscaped);

            Assert.Equal(@"", uri.UserInfo);
        }

        [Fact]
        public static void TestCtor_String_UriKind()
        {
            Uri uri = new Uri("catalog/shownew.htm?date=today", UriKind.Relative);

            Assert.Equal(@"catalog/shownew.htm?date=today", uri.ToString());

            Assert.Equal(@"catalog/shownew.htm?date=today", uri.OriginalString);

            Assert.False(uri.UserEscaped);

            Assert.False(uri.IsAbsoluteUri);

            Assert.Throws<InvalidOperationException>(() => uri.AbsolutePath);

            Assert.Throws<InvalidOperationException>(() => uri.AbsolutePath);

            Assert.Throws<InvalidOperationException>(() => uri.AbsoluteUri);

            Assert.Throws<InvalidOperationException>(() => uri.Authority);

            Assert.Throws<InvalidOperationException>(() => uri.DnsSafeHost);

            Assert.Throws<InvalidOperationException>(() => uri.Fragment);

            Assert.Throws<InvalidOperationException>(() => uri.Host);

            Assert.Throws<InvalidOperationException>(() => uri.IsDefaultPort);

            Assert.Throws<InvalidOperationException>(() => uri.IsFile);

            Assert.Throws<InvalidOperationException>(() => uri.IsLoopback);

            Assert.Throws<InvalidOperationException>(() => uri.IsUnc);

            Assert.Throws<InvalidOperationException>(() => uri.LocalPath);

            Assert.Throws<InvalidOperationException>(() => uri.PathAndQuery);

            Assert.Throws<InvalidOperationException>(() => uri.Port);

            Assert.Throws<InvalidOperationException>(() => uri.Query);

            Assert.Throws<InvalidOperationException>(() => uri.Scheme);

            Assert.Throws<InvalidOperationException>(() => uri.Segments);

            Assert.Throws<InvalidOperationException>(() => uri.UserInfo);
        }

        [Fact]
        public static void TestCtor_Uri_Uri()
        {
            Uri absoluteUri = new Uri("http://www.contoso.com/");

            Uri relativeUri = new Uri("/catalog/shownew.htm?date=today", UriKind.Relative);

            Uri uri = new Uri(absoluteUri, relativeUri);

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

            Assert.False(uri.UserEscaped);

            Assert.Equal(@"", uri.UserInfo);
        }

        [Fact]
        public static void TestTryCreate_String_UriKind()
        {
            Assert.True(Uri.TryCreate("http://www.contoso.com/catalog/shownew.htm?date=today", UriKind.Absolute, out Uri uri));

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

            Assert.False(uri.UserEscaped);

            Assert.Equal(@"", uri.UserInfo);
        }

        [Fact]
        public static void TestTryCreate_Uri_String()
        {
            Uri baseUri = new Uri("http://www.contoso.com/", UriKind.Absolute);

            Assert.True(Uri.TryCreate(baseUri, "catalog/shownew.htm?date=today", out Uri uri));

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

            Assert.False(uri.UserEscaped);

            Assert.Equal(@"", uri.UserInfo);
        }

        [Fact]
        public static void TestTryCreate_Uri_Uri()
        {
            Uri baseUri = new Uri("http://www.contoso.com/", UriKind.Absolute);
            Uri relativeUri = new Uri("catalog/shownew.htm?date=today", UriKind.Relative);

            Assert.True(Uri.TryCreate(baseUri, relativeUri, out Uri uri));

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

            Assert.False(uri.UserEscaped);

            Assert.Equal(@"", uri.UserInfo);
        }

        [Fact]
        public static void TestMakeRelative()
        {
            // Create a base Uri.
            Uri address1 = new Uri("http://www.contoso.com/");

            // Create a new Uri from a string.
            Uri address2 = new Uri("http://www.contoso.com/index.htm?date=today");

            // Determine the relative Uri.
            Uri uri = address1.MakeRelativeUri(address2);

            Assert.Equal(@"index.htm?date=today", uri.ToString());

            Assert.Equal(@"index.htm?date=today", uri.OriginalString);

            Assert.False(uri.IsAbsoluteUri);

            Assert.False(uri.UserEscaped);

            Assert.Throws<InvalidOperationException>(() => uri.AbsolutePath);

            Assert.Throws<InvalidOperationException>(() => uri.AbsoluteUri);

            Assert.Throws<InvalidOperationException>(() => uri.Authority);

            Assert.Throws<InvalidOperationException>(() => uri.DnsSafeHost);

            Assert.Throws<InvalidOperationException>(() => uri.Fragment);

            Assert.Throws<InvalidOperationException>(() => uri.Host);

            Assert.Throws<InvalidOperationException>(() => uri.HostNameType);

            Assert.Throws<InvalidOperationException>(() => uri.IsDefaultPort);

            Assert.Throws<InvalidOperationException>(() => uri.IsFile);

            Assert.Throws<InvalidOperationException>(() => uri.IsLoopback);

            Assert.Throws<InvalidOperationException>(() => uri.IsUnc);

            Assert.Throws<InvalidOperationException>(() => uri.LocalPath);

            Assert.Throws<InvalidOperationException>(() => uri.PathAndQuery);

            Assert.Throws<InvalidOperationException>(() => uri.Port);

            Assert.Throws<InvalidOperationException>(() => uri.Query);

            Assert.Throws<InvalidOperationException>(() => uri.Scheme);

            Assert.Throws<InvalidOperationException>(() => uri.Segments);

            Assert.Throws<InvalidOperationException>(() => uri.UserInfo);
        }

        [Theory]
        [InlineData("www.contoso.com", UriHostNameType.Dns)]
        [InlineData("1.2.3.4", UriHostNameType.IPv4)]
        [InlineData(null, UriHostNameType.Unknown)]
        [InlineData("!@*(@#&*#$&*#", UriHostNameType.Unknown)]
        public static void TestCheckHostName(string hostName, UriHostNameType expected)
        {
            Assert.Equal(expected, Uri.CheckHostName(hostName));
        }

        [Theory]
        [InlineData("http", true)]
        [InlineData(null, false)]
        [InlineData("!", false)]
        public static void TestCheckSchemeName(string scheme, bool expected)
        {
            Assert.Equal(expected, Uri.CheckSchemeName(scheme));
        }

        [Theory]
        [InlineData("http://host/path/path/file/", true)]
        [InlineData("http://host/path/path/#fragment", true)]
        [InlineData("http://host/path/path/MoreDir/\"", true)]
        [InlineData("http://host/path/path/OtherFile?Query", true)]
        [InlineData("http://host/path/path/", true)]
        [InlineData("http://host/path/path/file", true)]
        [InlineData("http://host/path/path", false)]
        [InlineData("http://host/path/path?query", false)]
        [InlineData("http://host/path/path#Fragment", false)]
        [InlineData("http://host/path/path2/", false)]
        [InlineData("http://host/path/path2/MoreDir", false)]
        [InlineData("http://host/path/File", false)]
        public static void TestIsBaseOf(string uriString, bool expected)
        {
            Uri uri = new Uri("http://host/path/path/file?query");
            Uri uri2 = new Uri(uriString);

            Assert.Equal(expected, uri.IsBaseOf(uri2));
        }

        [Theory]
        [InlineData("http://www.contoso.com/path?name", true)]
        [InlineData("http://www.contoso.com/path???/file name", false)]
        [InlineData(@"c:\\directory\filename", false)]
        [InlineData("file://c:/directory/filename", false)]
        [InlineData(@"http:\\host/path/file", false)]
        public static void TestIsWellFormedOriginalString(string uriString, bool expected)
        {
            Uri uri = new Uri(uriString);

            Assert.Equal(expected, uri.IsWellFormedOriginalString());
        }

        [Fact]
        public static void TestCompare()
        {
            Uri uri1 = new Uri("http://www.contoso.com/path?name#frag");
            Uri uri2 = new Uri("http://www.contosooo.com/path?name#slag");
            Uri uri2a = new Uri("http://www.contosooo.com/path?name#slag");

            int i;

            i = Uri.Compare(uri1, uri2, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.CurrentCulture);
            Assert.Equal(i, -1);

            i = Uri.Compare(uri1, uri2, UriComponents.Query, UriFormat.UriEscaped, StringComparison.CurrentCulture);
            Assert.Equal(0, i);

            i = Uri.Compare(uri1, uri2, UriComponents.Query | UriComponents.Fragment, UriFormat.UriEscaped, StringComparison.CurrentCulture);
            Assert.Equal(i, -1);

            Assert.False(uri1.Equals(uri2));

            Assert.False(uri1 == uri2);

            Assert.True(uri1 != uri2);

            Assert.True(uri2.Equals(uri2a));

            Assert.True(uri2 == uri2a);

            Assert.False(uri2 != uri2a);

            Assert.Equal(uri2.GetHashCode(), uri2a.GetHashCode());
        }

        [Fact]
        public static void TestEscapeDataString()
        {
            Assert.Equal("Hello", Uri.EscapeDataString("Hello"));

            Assert.Equal("He%5Cl%2Flo", Uri.EscapeDataString(@"He\l/lo"));
        }

        [Fact]
        public static void TestUnescapeDataString()
        {
            Assert.Equal("Hello", Uri.UnescapeDataString("Hello"));

            Assert.Equal(@"He\l/lo", Uri.UnescapeDataString("He%5Cl%2Flo"));
        }

        [Fact]
        public static void TestEscapeUriString()
        {
            Assert.Equal("Hello", Uri.EscapeUriString("Hello"));

            Assert.Equal(@"He%5Cl/lo", Uri.EscapeUriString(@"He\l/lo"));
        }

        [Fact]
        public static void TestGetComponentParts()
        {
            Uri uri = new Uri("http://www.contoso.com/path?name#frag");
            string s;

            s = uri.GetComponents(UriComponents.Fragment, UriFormat.UriEscaped);
            Assert.Equal("frag", s);

            s = uri.GetComponents(UriComponents.Host, UriFormat.UriEscaped);
            Assert.Equal("www.contoso.com", s);
        }

        [Fact]
        public static void TestCasingWhenCombiningAbsoluteAndRelativeUris()
        {
            Uri u = new Uri(new Uri("http://example.com/", UriKind.Absolute), new Uri("C(B:G", UriKind.Relative));
            Assert.Equal("http://example.com/C(B:G", u.ToString());
        }

        [Fact]
        public static void Uri_ColonInLongRelativeUri_SchemeSuccessfullyParsed()
        {
            Uri absolutePart = new Uri("http://www.contoso.com");
            string relativePart = "a/" + new string('a', 1024) + ":"; // 1024 is the maximum scheme length supported by System.Uri.
            Uri u = new Uri(absolutePart, relativePart); // On .NET Framework this will throw System.UriFormatException: Invalid URI: The Uri scheme is too long.
            Assert.Equal("http", u.Scheme);
        }

        [Fact]
        public static void Uri_ExtremelyLongScheme_ThrowsUriFormatException()
        {
            string largeString = new string('a', 1_000_000) + ":"; // 2MB is large enough to cause a stack overflow if we stackalloc the scheme buffer.
            Assert.Throws<UriFormatException>(() => new Uri(largeString));
        }

        [Fact]
        public static void Uri_HostTrailingSpaces_SpacesTrimmed()
        {
            string host = "www.contoso.com";
            Uri u = new Uri($"http://{host}     ");

            Assert.Equal($"http://{host}/", u.AbsoluteUri);
            Assert.Equal(host, u.Host);
        }

        [Theory]
        [InlineData("1234")]
        [InlineData("01234")]
        [InlineData("12340")]
        [InlineData("012340")]
        [InlineData("99")]
        [InlineData("09")]
        [InlineData("90")]
        [InlineData("0")]
        [InlineData("000")]
        [InlineData("65535")]
        public static void Uri_PortTrailingSpaces_SpacesTrimmed(string portString)
        {
            Uri u = new Uri($"http://www.contoso.com:{portString}     ");

            int port = int.Parse(portString);
            Assert.Equal($"http://www.contoso.com:{port}/", u.AbsoluteUri);
            Assert.Equal(port, u.Port);
        }

        [Fact]
        public static void Uri_EmptyPortTrailingSpaces_UsesDefaultPortSpacesTrimmed()
        {
            Uri u = new Uri("http://www.contoso.com:     ");

            Assert.Equal("http://www.contoso.com/", u.AbsoluteUri);
            Assert.Equal(80, u.Port);
        }

        [Fact]
        public static void Uri_PathTrailingSpaces_SpacesTrimmed()
        {
            string path = "/path/";
            Uri u = new Uri($"http://www.contoso.com{path}     ");

            Assert.Equal($"http://www.contoso.com{path}", u.AbsoluteUri);
            Assert.Equal(path, u.AbsolutePath);
        }

        [Fact]
        public static void Uri_QueryTrailingSpaces_SpacesTrimmed()
        {
            string query = "?query";
            Uri u = new Uri($"http://www.contoso.com/{query}     ");

            Assert.Equal($"http://www.contoso.com/{query}", u.AbsoluteUri);
            Assert.Equal(query, u.Query);
        }

        [Theory]
        [InlineData(" 80")]
        [InlineData("8 0")]
        [InlineData("80a")]
        [InlineData("65536")]
        [InlineData("100000")]
        [InlineData("10000000000")]
        public static void Uri_InvalidPort_ThrowsUriFormatException(string portString)
        {
            Assert.Throws<UriFormatException>(() => new Uri($"http://www.contoso.com:{portString}"));
        }

        [Fact]
        public static void Uri_EmptyPort_UsesDefaultPort()
        {
            Uri u = new Uri("http://www.contoso.com:");

            Assert.Equal("http://www.contoso.com/", u.AbsoluteUri);
            Assert.Equal(80, u.Port);
        }

        [Fact]
        public static void Uri_CombineUsesNewUriString()
        {
            // Tests that internal Uri fields were properly reset during a Combine operation
            // Otherwise, the wrong Uri string would be used if the relative Uri contains non-ascii characters
            // This will only affect parsers without the IriParsing flag - only custom parsers
            UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "combine-scheme", -1);

            const string BaseUriString = "combine-scheme://foo";
            const string RelativeUriString = "/relative/uri/with/non/ascii/\u00FC";
            const string Combined = BaseUriString + "/relative/uri/with/non/ascii/%C3%BC";

            var baseUri = new Uri(BaseUriString, UriKind.Absolute);
            var relativeUri = new Uri(RelativeUriString, UriKind.Relative);

            Assert.Equal(Combined, new Uri(baseUri, relativeUri).AbsoluteUri);
            Assert.Equal(Combined, new Uri(baseUri, RelativeUriString).AbsoluteUri);
        }

        [Fact]
        public static void Uri_CachesIdnHost()
        {
            var uri = new Uri("https://\u00FCnicode/foo");
            Assert.Same(uri.IdnHost, uri.IdnHost);
        }

        [Fact]
        public static void Uri_CachesPathAndQuery()
        {
            var uri = new Uri("https://foo/bar?one=two");
            Assert.Same(uri.PathAndQuery, uri.PathAndQuery);
        }

        [Fact]
        public static void Uri_CachesDnsSafeHost()
        {
            var uri = new Uri("https://[::]/bar");
            Assert.Same(uri.DnsSafeHost, uri.DnsSafeHost);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void Uri_DoesNotLockOnString()
        {
            // Don't intern the string we lock on
            string uriString = "*http://www.contoso.com".Substring(1);

            bool timedOut = false;

            var enteredLockMre = new ManualResetEvent(false);
            var finishedParsingMre = new ManualResetEvent(false);

            Task.Factory.StartNew(() =>
            {
                lock (uriString)
                {
                    enteredLockMre.Set();
                    timedOut = !finishedParsingMre.WaitOne(TimeSpan.FromSeconds(10));
                }
            }, TaskCreationOptions.LongRunning);

            enteredLockMre.WaitOne();
            int port = new Uri(uriString).Port;
            finishedParsingMre.Set();
            Assert.Equal(80, port);

            Assert.True(Monitor.TryEnter(uriString, TimeSpan.FromSeconds(10)));
            Assert.False(timedOut);
        }

        public static IEnumerable<object[]> FilePathHandlesNonAscii_TestData()
        {
            if (PlatformDetection.IsNotWindows)
            {
                // Unix absolute file path
                yield return new object[] { "/\u00FCri/", "file:///\u00FCri/", "/%C3%BCri/", "file:///%C3%BCri/", "/\u00FCri/" };
                yield return new object[] { "/a/b\uD83D\uDE1F/Foo.cs", "file:///a/b\uD83D\uDE1F/Foo.cs", "/a/b%F0%9F%98%9F/Foo.cs", "file:///a/b%F0%9F%98%9F/Foo.cs", "/a/b\uD83D\uDE1F/Foo.cs" };
            }

            // Absolute fie path
            yield return new object[] { "file:///\u00FCri/", "file:///\u00FCri/", "/%C3%BCri/", "file:///%C3%BCri/", "/\u00FCri/" };
            yield return new object[] { "file:///a/b\uD83D\uDE1F/Foo.cs", "file:///a/b\uD83D\uDE1F/Foo.cs", "/a/b%F0%9F%98%9F/Foo.cs", "file:///a/b%F0%9F%98%9F/Foo.cs", "/a/b\uD83D\uDE1F/Foo.cs" };

            // DOS
            yield return new object[] { "file://C:/\u00FCri/", "file:///C:/\u00FCri/", "C:/%C3%BCri/", "file:///C:/%C3%BCri/", "C:\\\u00FCri\\" };
            yield return new object[] { "file:///C:/\u00FCri/", "file:///C:/\u00FCri/", "C:/%C3%BCri/", "file:///C:/%C3%BCri/", "C:\\\u00FCri\\" };
            yield return new object[] { "C:/\u00FCri/", "file:///C:/\u00FCri/", "C:/%C3%BCri/", "file:///C:/%C3%BCri/", "C:\\\u00FCri\\" };

            // UNC
            yield return new object[] { "\\\\\u00FCri/", "file://\u00FCri/", "/", "file://\u00FCri/", "\\\\\u00FCri\\" };
            yield return new object[] { "file://\u00FCri/", "file://\u00FCri/", "/", "file://\u00FCri/", "\\\\\u00FCri\\" };

            // ? and # handling
            if (PlatformDetection.IsWindows)
            {
                yield return new object[] { "file:///a/?b/c\u00FC/", "file:///a/?b/c\u00FC/", "/a/", "file:///a/?b/c%C3%BC/", "/a/" };
                yield return new object[] { "file:///a/#b/c\u00FC/", "file:///a/#b/c\u00FC/", "/a/", "file:///a/#b/c%C3%BC/", "/a/" };
                yield return new object[] { "file:///a/?b/#c/d\u00FC/", "file:///a/?b/#c/d\u00FC/", "/a/", "file:///a/?b/#c/d%C3%BC/", "/a/" };
            }
            else
            {
                yield return new object[] { "/a/?b/c\u00FC/", "file:///a/%3Fb/c\u00FC/", "/a/%3Fb/c%C3%BC/", "file:///a/%3Fb/c%C3%BC/", "/a/?b/c\u00FC/" };
                yield return new object[] { "/a/#b/c\u00FC/", "file:///a/%23b/c\u00FC/", "/a/%23b/c%C3%BC/", "file:///a/%23b/c%C3%BC/", "/a/#b/c\u00FC/" };
                yield return new object[] { "/a/?b/#c/d\u00FC/", "file:///a/%3Fb/%23c/d\u00FC/", "/a/%3Fb/%23c/d%C3%BC/", "file:///a/%3Fb/%23c/d%C3%BC/", "/a/?b/#c/d\u00FC/" };

                yield return new object[] { "file:///a/?b/c\u00FC/", "file:///a/?b/c\u00FC/", "/a/", "file:///a/?b/c%C3%BC/", "/a/" };
                yield return new object[] { "file:///a/#b/c\u00FC/", "file:///a/#b/c\u00FC/", "/a/", "file:///a/#b/c%C3%BC/", "/a/" };
                yield return new object[] { "file:///a/?b/#c/d\u00FC/", "file:///a/?b/#c/d\u00FC/", "/a/", "file:///a/?b/#c/d%C3%BC/", "/a/" };
            }
        }

        [Theory]
        [MemberData(nameof(FilePathHandlesNonAscii_TestData))]
        public static void FilePathHandlesNonAscii(string uriString, string toString, string absolutePath, string absoluteUri, string localPath)
        {
            var uri = new Uri(uriString);

            Assert.Equal(toString, uri.ToString());
            Assert.Equal(absolutePath, uri.AbsolutePath);
            Assert.Equal(absoluteUri, uri.AbsoluteUri);
            Assert.Equal(localPath, uri.LocalPath);

            var uri2 = new Uri(uri.AbsoluteUri);

            Assert.Equal(toString, uri2.ToString());
            Assert.Equal(absolutePath, uri2.AbsolutePath);
            Assert.Equal(absoluteUri, uri2.AbsoluteUri);
            Assert.Equal(localPath, uri2.LocalPath);
        }

        public static IEnumerable<object[]> ZeroPortIsParsedForBothKnownAndUnknownSchemes_TestData()
        {
            yield return new object[] { "http://example.com:0", 0, false };
            yield return new object[] { "http://example.com", 80, true };
            yield return new object[] { "rtsp://example.com:0", 0, false };
            yield return new object[] { "rtsp://example.com", -1, true };
        }

        [Theory]
        [MemberData(nameof(ZeroPortIsParsedForBothKnownAndUnknownSchemes_TestData))]
        public static void ZeroPortIsParsedForBothKnownAndUnknownSchemes(string uriString, int port, bool isDefaultPort)
        {
            Uri.TryCreate(uriString, UriKind.Absolute, out var uri);
            Assert.Equal(port, uri.Port);
            Assert.Equal(isDefaultPort, uri.IsDefaultPort);
            Assert.Equal(uriString + "/", uri.ToString());
        }
    }
}
