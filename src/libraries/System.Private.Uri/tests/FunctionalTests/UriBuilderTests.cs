// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Xunit;

namespace System.PrivateUri.Tests
{
    public class UriBuilderTests
    {
        //This test tests a case where the .NET Core implementation of UriBuilder differs from .NET Framework UriBuilder.
        //The Query property will not longer prepend a ? character if the string being assigned is already prepended.
        [Fact]
        public static void TestQuery()
        {
            var uriBuilder = new UriBuilder(@"http://foo/bar/baz?date=today");

            Assert.Equal("?date=today", uriBuilder.Query);

            uriBuilder.Query = "?date=yesterday";
            Assert.Equal(@"?date=yesterday", uriBuilder.Query);

            uriBuilder.Query = "date=tomorrow";
            Assert.Equal("?date=tomorrow", uriBuilder.Query);

            uriBuilder.Query += @"&place=redmond";
            Assert.Equal("?date=tomorrow&place=redmond", uriBuilder.Query);

            uriBuilder.Query = null;
            Assert.Equal("", uriBuilder.Query);

            uriBuilder.Query = "";
            Assert.Equal("", uriBuilder.Query);

            uriBuilder.Query = "?";
            Assert.Equal("?", uriBuilder.Query);
        }

        [Fact]
        public void Ctor_Empty()
        {
            var uriBuilder = new UriBuilder();
            VerifyUriBuilder(uriBuilder, scheme: "http", userName: "", password: "", host: "localhost", port: -1, path: "/", query: "", fragment: "");
        }

        [Theory]
        [InlineData("http://host/", true, "http", "", "", "host", 80, "/", "", "")]
        [InlineData("http://username@host/", true, "http", "username", "", "host", 80, "/", "", "")]
        [InlineData("http://username:password@host/", true, "http", "username", "password", "host", 80, "/", "", "")]
        [InlineData("http://host:90/", true, "http", "", "", "host", 90, "/", "", "")]
        [InlineData("http://host/path", true, "http", "", "", "host", 80, "/path", "", "")]
        [InlineData("http://host/?query", true, "http", "", "", "host", 80, "/", "?query", "")]
        [InlineData("http://host/#fragment", true, "http", "", "", "host", 80, "/", "", "#fragment")]
        [InlineData("http://username:password@host:90/path1/path2?query#fragment", true, "http", "username", "password", "host", 90, "/path1/path2", "?query", "#fragment")]
        [InlineData("www.host.com", false, "http", "", "", "www.host.com", 80, "/", "", "")] // Relative
        [InlineData("unknownscheme:", true, "unknownscheme", "", "", "", -1, "", "", "")] // No authority
        public void Ctor_String(string uriString, bool createUri, string scheme, string username, string password, string host, int port, string path, string query, string fragment)
        {
            var uriBuilder = new UriBuilder(uriString);
            VerifyUriBuilder(uriBuilder, scheme, username, password, host, port, path, query, fragment);

            if (createUri)
            {
                uriBuilder = new UriBuilder(new Uri(uriString, UriKind.RelativeOrAbsolute));
                VerifyUriBuilder(uriBuilder, scheme, username, password, host, port, path, query, fragment);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => new UriBuilder(new Uri(uriString, UriKind.RelativeOrAbsolute)));
            }
        }

        [Fact]
        public void Ctor_String_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("uriString", () => new UriBuilder((string)null)); // UriString is null
            Assert.Throws<UriFormatException>(() => new UriBuilder(@"http://host\")); // UriString is invalid
        }

        [Fact]
        public void Ctor_Uri_Null()
        {
            AssertExtensions.Throws<ArgumentNullException>("uri", () => new UriBuilder((Uri)null)); // Uri is null
        }

        [Theory]
        [InlineData("http", "host", "http", "host")]
        [InlineData("HTTP", "host", "http", "host")]
        [InlineData("http", "[::1]", "http", "[::1]")]
        [InlineData("https", "::1]", "https", "[::1]]")]
        [InlineData("http", "::1", "http", "[::1]")]
        [InlineData("http1:http2", "host", "http1", "host")]
        [InlineData("http", "", "http", "")]
        [InlineData("", "host", "", "host")]
        [InlineData("", "", "", "")]
        [InlineData("http", null, "http", "")]
        [InlineData(null, "", "", "")]
        [InlineData(null, null, "", "")]
        public void Ctor_String_String(string schemeName, string hostName, string expectedScheme, string expectedHost)
        {
            var uriBuilder = new UriBuilder(schemeName, hostName);
            VerifyUriBuilder(uriBuilder, expectedScheme, "", "", expectedHost, -1, "/", "", "");
        }

        [Theory]
        [InlineData("http", "host", 0, "http", "host")]
        [InlineData("HTTP", "host", 20, "http", "host")]
        [InlineData("http", "[::1]", 40, "http", "[::1]")]
        [InlineData("https", "::1]", 60, "https", "[::1]]")]
        [InlineData("http", "::1", 80, "http", "[::1]")]
        [InlineData("http1:http2", "host", 100, "http1", "host")]
        [InlineData("http", "", 120, "http", "")]
        [InlineData("", "host", 140, "", "host")]
        [InlineData("", "", 160, "", "")]
        [InlineData("http", null, 180, "http", "")]
        [InlineData(null, "", -1, "", "")]
        [InlineData(null, null, 65535, "", "")]
        public void Ctor_String_String_Int(string scheme, string host, int port, string expectedScheme, string expectedHost)
        {
            var uriBuilder = new UriBuilder(scheme, host, port);
            VerifyUriBuilder(uriBuilder, expectedScheme, "", "", expectedHost, port, "/", "", "");
        }

        [Theory]
        [InlineData("http", "host", 0, "/path", "http", "host", "/path")]
        [InlineData("HTTP", "host", 20, "/path1/path2", "http", "host", "/path1/path2")]
        [InlineData("http", "[::1]", 40, "/", "http", "[::1]", "/")]
        [InlineData("https", "::1]", 60, "/path1/", "https", "[::1]]", "/path1/")]
        [InlineData("http", "::1", 80, null, "http", "[::1]", "/")]
        [InlineData("http1:http2", "host", 100, "path1", "http1", "host", "path1")]
        [InlineData("http", "", 120, "path1/path2", "http", "", "path1/path2")]
        [InlineData("", "host", 140, "path1/path2/path3/", "", "host", "path1/path2/path3/")]
        [InlineData("", "", 160, @"\path1\path2\path3", "", "", "/path1/path2/path3")]
        [InlineData("http", null, 180, @"path1\path2\", "http", "", "path1/path2/")]
        [InlineData(null, "", -1, "\u1234", "", "", "%E1%88%B4")]
        [InlineData(null, null, 65535, "\u1234\u2345", "", "", "%E1%88%B4%E2%8D%85")]
        public void Ctor_String_String_Int_String(string schemeName, string hostName, int port, string pathValue, string expectedScheme, string expectedHost, string expectedPath)
        {
            var uriBuilder = new UriBuilder(schemeName, hostName, port, pathValue);
            VerifyUriBuilder(uriBuilder, expectedScheme, "", "", expectedHost, port, expectedPath, "", "");
        }

        [Theory]
        [InlineData("http", "host", 0, "/path", "?query#fragment", "http", "host", "/path", "?query", "#fragment")]
        [InlineData("HTTP", "host", 20, "/path1/path2", "?query&query2=value#fragment", "http", "host", "/path1/path2", "?query&query2=value", "#fragment")]
        [InlineData("http", "[::1]", 40, "/", "#fragment?query", "http", "[::1]", "/", "", "#fragment?query")]
        [InlineData("https", "::1]", 60, "/path1/", "?query", "https", "[::1]]", "/path1/", "?query", "")]
        [InlineData("http", "::1", 80, null, "#fragment", "http", "[::1]", "/", "", "#fragment")]
        [InlineData("http", "", 120, "path1/path2", "?#", "http", "", "path1/path2", "", "")]
        [InlineData("", "host", 140, "path1/path2/path3/", "?", "", "host", "path1/path2/path3/", "", "")]
        [InlineData("", "", 160, @"\path1\path2\path3", "#", "", "", "/path1/path2/path3", "", "")]
        [InlineData("http", null, 180, @"path1\path2\", "?\u1234#\u2345", "http", "", "path1/path2/", "?\u1234", "#\u2345")]
        [InlineData(null, "", -1, "\u1234", "", "", "", "%E1%88%B4", "", "")]
        [InlineData(null, null, 65535, "\u1234\u2345", null, "", "", "%E1%88%B4%E2%8D%85", "", "")]
        public void Ctor_String_String_Int_String_String(string schemeName, string hostName, int port, string pathValue, string extraValue, string expectedScheme, string expectedHost, string expectedPath, string expectedQuery, string expectedFragment)
        {
            var uriBuilder = new UriBuilder(schemeName, hostName, port, pathValue, extraValue);
            VerifyUriBuilder(uriBuilder, expectedScheme, "", "", expectedHost, port, expectedPath, expectedQuery, expectedFragment);
        }

        [Theory]
        [InlineData("query#fragment")]
        [InlineData("fragment?fragment")]
        public void Ctor_InvalidExtraValue_ThrowsArgumentException(string extraValue)
        {
            AssertExtensions.Throws<ArgumentException>("extraValue", null, () => new UriBuilder("scheme", "host", 80, "path", extraValue));
        }

        [Theory]
        [InlineData("https", "https")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Scheme_Get_Set(string value, string expected)
        {
            var uriBuilder = new UriBuilder("http://userinfo@domain/path?query#fragment");
            uriBuilder.Scheme = value;
            Assert.Equal(expected, uriBuilder.Scheme);
        }

        [Theory]
        [InlineData("\u1234http")]
        [InlineData(".")]
        [InlineData("-")]
        public void InvalidScheme_ThrowsArgumentException(string schemeName)
        {
            AssertExtensions.Throws<ArgumentException>("value", null, () => new UriBuilder(schemeName, "host"));
            AssertExtensions.Throws<ArgumentException>("value", null, () => new UriBuilder(schemeName, "host", 80));
            AssertExtensions.Throws<ArgumentException>("value", null, () => new UriBuilder(schemeName, "host", 80, "path"));
            AssertExtensions.Throws<ArgumentException>("value", null, () => new UriBuilder(schemeName, "host", 80, "?query#fragment"));

            AssertExtensions.Throws<ArgumentException>("value", null, () => new UriBuilder().Scheme = schemeName);
        }

        [Theory]
        [InlineData("username", "username")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void UserName_Get_Set(string value, string expected)
        {
            var uriBuilder = new UriBuilder("http://userinfo@domain/path?query#fragment");
            uriBuilder.UserName = value;
            Assert.Equal(expected, uriBuilder.UserName);

            Uri oldUri = uriBuilder.Uri;
            uriBuilder.UserName = value;
            Assert.NotSame(uriBuilder.Uri, oldUri); // Should generate new uri
            Assert.Equal(uriBuilder.UserName, uriBuilder.Uri.UserInfo);
        }

        [Theory]
        [InlineData("password", "password")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Password_Get_Set(string value, string expected)
        {
            var uriBuilder = new UriBuilder("http://userinfo1:PLACEHOLDER@domain/path?query#fragment");
            uriBuilder.Password = value;
            Assert.Equal(expected, uriBuilder.Password);

            Uri oldUri = uriBuilder.Uri;
            uriBuilder.Password = value;
            Assert.NotSame(uriBuilder.Uri, oldUri);
        }

        [Theory]
        [InlineData("host", "host")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Host_Get_Set(string value, string expected)
        {
            var uriBuilder = new UriBuilder("http://userinfo@domain/path?query#fragment");
            uriBuilder.Host = value;
            Assert.Equal(expected, uriBuilder.Host);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(80)]
        [InlineData(180)]
        [InlineData(65535)]
        public void Port_Get_Set(int port)
        {
            var uriBuilder = new UriBuilder("http://userinfo@domain/path?query#fragment");
            uriBuilder.Port = port;
            Assert.Equal(port, uriBuilder.Port);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(65536)]
        public void InvalidPort_ThrowsArgumentOutOfRangeException(int portNumber)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UriBuilder("scheme", "host", portNumber));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UriBuilder("scheme", "host", portNumber, "path"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UriBuilder("scheme", "host", portNumber, "path", "?query#fragment"));

            Assert.Throws<ArgumentOutOfRangeException>(() => new UriBuilder().Port = portNumber);
        }

        [Theory]
        [InlineData("/path1/path2", "/path1/path2")]
        [InlineData(@"\path1\path2", "/path1/path2")]
        [InlineData("", "/")]
        [InlineData(null, "/")]
        public void Path_Get_Set(string value, string expected)
        {
            var uriBuilder = new UriBuilder("http://userinfo@domain/path?query#fragment");
            uriBuilder.Path = value;
            Assert.Equal(expected, uriBuilder.Path);
        }

        [Theory]
        [InlineData("query", "?query")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Query_Get_Set(string value, string expected)
        {
            var uriBuilder = new UriBuilder();
            uriBuilder.Query = value;
            Assert.Equal(expected, uriBuilder.Query);

            Uri oldUri = uriBuilder.Uri;
            uriBuilder.Query = value;
            Assert.NotSame(uriBuilder.Uri, oldUri); // Should generate new uri
            Assert.Equal(uriBuilder.Query, uriBuilder.Uri.Query);
        }

        [Theory]
        [InlineData("#fragment", "#fragment")]
        [InlineData("#", "#")]
        public void Fragment_Get_Set_StartsWithPound(string value, string expected)
        {
            var uriBuilder = new UriBuilder();
            uriBuilder.Fragment = value;
            Assert.Equal(expected, uriBuilder.Fragment);

            Uri oldUri = uriBuilder.Uri;
            uriBuilder.Fragment = value;
            Assert.NotSame(uriBuilder.Uri, oldUri); // Should generate new uri
            Assert.Equal(uriBuilder.Fragment, uriBuilder.Uri.Fragment);
        }

        [Theory]
        [InlineData("fragment", "#fragment")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Fragment_Get_Set(string value, string expected)
        {
            var uriBuilder = new UriBuilder();
            uriBuilder.Fragment = value;
            Assert.Equal(expected, uriBuilder.Fragment);

            Uri oldUri = uriBuilder.Uri;
            uriBuilder.Fragment = value;
            Assert.NotSame(uriBuilder.Uri, oldUri); // Should generate new uri
            Assert.Equal(uriBuilder.Fragment, uriBuilder.Uri.Fragment);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new UriBuilder(), new UriBuilder(), true };
            yield return new object[] { new UriBuilder(), null, false };

            yield return new object[] { new UriBuilder("http://username:password@domain.com:80/path/file?query#fragment"), new UriBuilder("http://username:password@domain.com:80/path/file?query#fragment"), true };
            yield return new object[] { new UriBuilder("http://username:password@domain.com:80/path/file?query#fragment"), new UriBuilder("http://domain.com:80/path/file?query#fragment"), true }; // Ignores userinfo
            yield return new object[] { new UriBuilder("http://username:password@domain.com:80/path/file?query#fragment"), new UriBuilder("http://username:password@domain.com:80/path/file?query#fragment2"), true }; // Ignores fragment
            yield return new object[] { new UriBuilder("http://username:password@domain.com:80/path/file?query#fragment"), new UriBuilder("http://username:password@host.com:80/path/file?query#fragment"), false };
            yield return new object[] { new UriBuilder("http://username:password@domain.com:80/path/file?query#fragment"), new UriBuilder("http://username:password@domain.com:90/path/file?query#fragment"), false };
            yield return new object[] { new UriBuilder("http://username:password@domain.com:80/path/file?query#fragment"), new UriBuilder("http://username:password@domain.com:80/path2/file?query#fragment"), false };
            yield return new object[] { new UriBuilder("http://username:password@domain.com:80/path/file?query#fragment"), new UriBuilder("http://username:password@domain.com:80/path/file?query2#fragment"), false };

            yield return new object[] { new UriBuilder("unknown:"), new UriBuilder("unknown:"), true };
            yield return new object[] { new UriBuilder("unknown:"), new UriBuilder("different:"), false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(UriBuilder uriBuilder1, UriBuilder uriBuilder2, bool expected)
        {
            Assert.Equal(expected, uriBuilder1.Equals(uriBuilder2));
            if (uriBuilder2 != null)
            {
                Assert.Equal(expected, uriBuilder1.GetHashCode().Equals(uriBuilder2.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new UriBuilder(), "http://localhost/" };
            yield return new object[] { new UriBuilder() { Scheme = "" }, "localhost/" };
            yield return new object[] { new UriBuilder() { Scheme = "unknown" }, "unknown://localhost/" };
            yield return new object[] { new UriBuilder() { Scheme = "unknown", Host = "" }, "unknown:/" };
            yield return new object[] { new UriBuilder() { Scheme = "unknown", Host = "", Path = "path1/path2" }, "unknown:path1/path2" };
            yield return new object[] { new UriBuilder() { UserName = "username" }, "http://username@localhost/" };
            yield return new object[] { new UriBuilder() { UserName = "username", Password = "password" }, "http://username:password@localhost/" };
            yield return new object[] { new UriBuilder() { Port = 80 }, "http://localhost:80/" };
            yield return new object[] { new UriBuilder() { Port = 0 }, "http://localhost:0/" };
            yield return new object[] { new UriBuilder() { Host = "", Port = 80 }, "http:///" };
            yield return new object[] { new UriBuilder() { Host = "host", Path = "" }, "http://host/" };
            yield return new object[] { new UriBuilder() { Host = "host", Path = "/" }, "http://host/" };
            yield return new object[] { new UriBuilder() { Host = "host", Path = @"\" }, "http://host/" };
            yield return new object[] { new UriBuilder() { Host = "host", Path = "path" }, "http://host/path" };
            yield return new object[] { new UriBuilder() { Host = "host", Path = "path", Query = "query" }, "http://host/path?query" };
            yield return new object[] { new UriBuilder() { Host = "host", Path = "path", Fragment = "fragment" }, "http://host/path#fragment" };
            yield return new object[] { new UriBuilder() { Host = "host", Path = "path", Query = "query", Fragment = "fragment" }, "http://host/path?query#fragment" };
            yield return new object[] { new UriBuilder() { Host = "host", Query = "query" }, "http://host/?query" };
            yield return new object[] { new UriBuilder() { Host = "host", Fragment = "fragment" }, "http://host/#fragment" };
            yield return new object[] { new UriBuilder() { Host = "host", Query = "query", Fragment = "fragment" }, "http://host/?query#fragment" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToStringTest(UriBuilder uriBuilder, string expected)
        {
            Assert.Equal(expected, uriBuilder.ToString());
        }

        [Fact]
        public void ToString_Invalid()
        {
            var uriBuilder = new UriBuilder();
            uriBuilder.Password = "password";
            Assert.Throws<UriFormatException>(() => uriBuilder.ToString()); // Uri has a password but no username
        }

        private static void VerifyUriBuilder(UriBuilder uriBuilder, string scheme, string userName, string password, string host, int port, string path, string query, string fragment)
        {
            Assert.Equal(scheme, uriBuilder.Scheme);
            Assert.Equal(userName, uriBuilder.UserName);
            Assert.Equal(password, uriBuilder.Password);
            Assert.Equal(host, uriBuilder.Host);
            Assert.Equal(port, uriBuilder.Port);
            Assert.Equal(path, uriBuilder.Path);
            Assert.Equal(query, uriBuilder.Query);
            Assert.Equal(fragment, uriBuilder.Fragment);
        }
    }
}
