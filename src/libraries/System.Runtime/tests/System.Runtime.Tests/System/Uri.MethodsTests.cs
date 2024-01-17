// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class UriMethodTests
    {
        private static readonly bool s_isWindowsSystem = PlatformDetection.IsWindows;

        public static IEnumerable<object[]> MakeRelative_TestData()
        {
            // Path (forward x3)
            yield return new object[] { new Uri("http://www.domain.com/"), new Uri("http://www.domain.com/path1/path2/path3"), new Uri("path1/path2/path3", UriKind.Relative) };

            // Path (forward x2)
            yield return new object[] { new Uri("http://www.domain.com/path1/"), new Uri("http://www.domain.com/path1/path2/path3"), new Uri("path2/path3", UriKind.Relative) };

            // Path (forward x1)
            yield return new object[] { new Uri("http://www.domain.com/path1/path2/"), new Uri("http://www.domain.com/path1/path2/path3"), new Uri("path3", UriKind.Relative) };

            // Path same
            yield return new object[] { new Uri("http://www.domain.com/path1/path2/path3"), new Uri("http://www.domain.com/path1/path2/path3"), new Uri("", UriKind.Relative) };

            // Same path (backward x1)
            yield return new object[] { new Uri("http://www.domain.com/path1/path2/"), new Uri("http://www.domain.com/path1/"), new Uri("../", UriKind.Relative) };

            // Path (backward x1, forward x1)
            yield return new object[] { new Uri("http://www.domain.com/path1/path2/"), new Uri("http://www.domain.com/path1/path3"), new Uri("../path3", UriKind.Relative) };

            // Path (backward x2)
            yield return new object[] { new Uri("http://www.domain.com/path1/path2/path3/"), new Uri("http://www.domain.com/path1/"), new Uri("../../", UriKind.Relative) };

            // Path (backward x2, forward x2)
            yield return new object[] { new Uri("http://www.domain.com/path1/path2/path3/"), new Uri("http://www.domain.com/path1/path4/path5"), new Uri("../../path4/path5", UriKind.Relative) };

            // Path (backward x3)
            yield return new object[] { new Uri("http://www.domain.com/path1/path2/path3/"), new Uri("http://www.domain.com/"), new Uri("../../../", UriKind.Relative) };

            // Query
            yield return new object[] { new Uri("http://www.domain.com/"), new Uri("http://www.domain.com/?query"), new Uri("?query", UriKind.Relative) };

            // Fragment
            yield return new object[] { new Uri("http://www.domain.com/"), new Uri("http://www.domain.com/#fragment"), new Uri("#fragment", UriKind.Relative) };

            // Path, query, fragment
            yield return new object[] { new Uri("http://www.domain.com/"), new Uri("http://www.domain.com/index.htm?query1=value#fragment"), new Uri("index.htm?query1=value#fragment", UriKind.Relative) };

            // Different scheme
            yield return new object[] { new Uri("https://www.domain.com/"), new Uri("http://www.domain.com/index.htm?query1=value"), new Uri("http://www.domain.com/index.htm?query1=value") };

            // Different domain
            yield return new object[] { new Uri("http://www.abc.com/"), new Uri("http://www.xyz.com/index.htm?query1=value"), new Uri("http://www.xyz.com/index.htm?query1=value") };

            // Different port
            yield return new object[] { new Uri("http://www.abc:80/"), new Uri("http://www.abc:90/index.htm?query1=value"), new Uri("http://www.abc:90/index.htm?query1=value") };

            // Ignores userinfo
            yield return new object[] { new Uri("http://userinfo@domain.com/"), new Uri("http://domain.com/path1/path2"), new Uri("path1/path2", UriKind.Relative) };

            // Urn (relative uris cannot have semicolons)
            yield return new object[] { new Uri("urn:namespace:segment1:segment2"), new Uri("urn:namespace:segment1:segment2:segment3"), new Uri("./namespace:segment1:segment2:segment3", UriKind.Relative) };

            // Only UNC or DOS uris are case insensitive
            yield return new object[] { new Uri("http://domain.com/PATH1/path2/PATH3"), new Uri("http://domain.com/path1/path2/path3"), new Uri("../../path1/path2/path3", UriKind.Relative) };
            yield return new object[] { new Uri(@"\\servername\PATH1\path2\PATH3"), new Uri(@"\\servername\path1\path2\path3"), new Uri("", UriKind.Relative) };
            yield return new object[] { new Uri("file://C:/PATH1/path2/PATH3"), new Uri("file://C:/path1/path2/path3"), new Uri("", UriKind.Relative) };
            // Unix paths are case sensitive
            yield return new object[] { new Uri("file:///PATH1/path2/PATH3"), new Uri("file:///path1/path2/path3"), new Uri("../../path1/path2/path3", UriKind.Relative) };
            if (!s_isWindowsSystem) // Unix path
            {
                yield return new object[] { new Uri("/PATH1/path2/PATH3"), new Uri("/path1/path2/path3"), new Uri("../../path1/path2/path3", UriKind.Relative) };
            }

            // Same path, but uri1 ended with a filename, but uri2 didn't
            yield return new object[] { new Uri("http://domain.com/path1/file"), new Uri("http://domain.com/path1/"), new Uri("./", UriKind.Relative) };

            // Empty path
            yield return new object[] { new Uri("unknownscheme:"), new Uri("unknownscheme:path"), new Uri("path", UriKind.Relative) };
            yield return new object[] { new Uri("unknownscheme:path"), new Uri("unknownscheme:"), new Uri("", UriKind.Relative) };
        }

        [Theory]
        [MemberData(nameof(MakeRelative_TestData))]
        public void MakeRelative(Uri baseUri, Uri uri, Uri expected)
        {
            Uri relativeUri = baseUri.MakeRelativeUri(uri);
            Assert.Equal(expected, relativeUri);
            if (!expected.IsAbsoluteUri)
            {
                UriCreateStringTests.VerifyRelativeUri(relativeUri, expected.OriginalString, expected.ToString());
            }
        }

        [Fact]
        public void MakeRelative_Invalid()
        {
            var baseUri = new Uri("http://www.domain.com/");
            var relativeUri = new Uri("/path/", UriKind.Relative);
            AssertExtensions.Throws<ArgumentNullException>("uri", () => baseUri.MakeRelativeUri(null)); // Uri is null

            Assert.Throws<InvalidOperationException>(() => relativeUri.MakeRelativeUri(baseUri)); // Base uri is relative
            Assert.Throws<InvalidOperationException>(() => baseUri.MakeRelativeUri(relativeUri)); // Uri is relative
        }

        public static IEnumerable<object[]> CheckHostName_TestData()
        {
            // DNS
            yield return new object[] { "www.domain.com", UriHostNameType.Dns }; // Valid
            yield return new object[] { "\u1234\u1234", UriHostNameType.Dns }; // Valid - IRI
            yield return new object[] { "www.domain.com/", UriHostNameType.Unknown }; // Invalid
            yield return new object[] { @"www.domain.com\", UriHostNameType.Unknown }; // Invalid
            yield return new object[] { "domain" + (char)0x80, UriHostNameType.Unknown }; // Invalid - non ascii chars

            // IPv4
            yield return new object[] { "1.2.3.4", UriHostNameType.IPv4 }; // Valid
            yield return new object[] { "1.2.3.4.5", UriHostNameType.Dns }; // Invalid, parses as DNS

            // IPv6
            yield return new object[] { "[::1]", UriHostNameType.IPv6 }; // Valid
            yield return new object[] { "::1", UriHostNameType.IPv6 }; // Valid, even without being surrounded by []
            yield return new object[] { "[]", UriHostNameType.Unknown }; // Invalid
            yield return new object[] { "[1111:2222]", UriHostNameType.Unknown }; // Invalid
            yield return new object[] { "1111:2222", UriHostNameType.Unknown }; // Invalid

            // Invalid
            yield return new object[] { null, UriHostNameType.Unknown }; // Null
            yield return new object[] { "", UriHostNameType.Unknown }; // Empty
            yield return new object[] { "!@*(@#&*#$&*#", UriHostNameType.Unknown }; // Invalid chars
        }

        [Theory]
        [MemberData(nameof(CheckHostName_TestData))]
        public void CheckHostName(string name, UriHostNameType expected)
        {
            Assert.Equal(expected, Uri.CheckHostName(name));
        }

        [Theory]
        [InlineData("http", true)]
        [InlineData("https", true)]
        [InlineData("file", true)]
        [InlineData("ftp", true)]
        [InlineData("scheme+scheme", true)]
        [InlineData("scheme-scheme", true)]
        [InlineData("scheme.scheme", true)]
        [InlineData("scheme+", true)]
        [InlineData("scheme-", true)]
        [InlineData("scheme.", true)]
        [InlineData("\u1234http", false)] // Non ascii
        [InlineData("http\u1234", false)] // Non ascii
        [InlineData("http~", false)]
        [InlineData("+scheme", false)]
        [InlineData("-scheme", false)]
        [InlineData(".scheme", false)]
        [InlineData("!", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void CheckSchemeName(string schemeName, bool expected)
        {
            Assert.Equal(expected, Uri.CheckSchemeName(schemeName));
        }

        public static IEnumerable<object[]> IsBaseOfTestData()
        {
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path/file?query"), true };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path/file/"), true };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path/#fragment"), true };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path/MoreDir/\""), true };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path/OtherFile?Query"), true };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path/"), true };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path/file"), true };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path"), false };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path?query"), false };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path#fragment"), false };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path2/path"), false };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path2"), false };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/path2/path3"), false };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/path/File"), false };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/?query"), false };
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("http://host/#fragment"), false };

            yield return new object[] { new Uri("http://host"), new Uri("https://host"), false }; // Different scheme

            yield return new object[] { new Uri("host", UriKind.Relative), new Uri("http://host"), false }; // Uri1 is relative
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("path/path/file?query", UriKind.Relative), true }; // Uri2 is relative
            yield return new object[] { new Uri("http://host/path/path/file?query"), new Uri("path/path/file", UriKind.Relative), true }; // Uri2 is relative

            // Uri1 is a file
            yield return new object[] { new Uri("file://C:/path/path/file"), new Uri("file://C:/path/path/path"), true };
            yield return new object[] { new Uri("file://C:/path/path/file"), new Uri("file://D:/path/path/path"), false };
            yield return new object[] { new Uri("file://C:/path/path/file"), new Uri("http://host/path/path/file"), false };
            yield return new object[] { new Uri("file://C:/path/path/file"), new Uri("path/path/file", UriKind.Relative), true };

            yield return new object[] { new Uri("https://user@domain.com"), new Uri("https://domain.com"), true };
            yield return new object[] { new Uri("https://user@domain.com"), new Uri("http://domain.com"), false };
            yield return new object[] { new Uri("https://user1@domain.com"), new Uri("https://user2@domain.com"), true };
            yield return new object[] { new Uri("https://domain.com"), new Uri("https://user@domain.com"), true };
        }

        [Theory]
        [MemberData(nameof(IsBaseOfTestData))]
        public void IsBaseOf(Uri uri1, Uri uri2, bool expected)
        {
            Assert.Equal(expected, uri1.IsBaseOf(uri2));
        }

        [Fact]
        public void IsBaseOf_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("uri", () => new Uri("http://domain.com").IsBaseOf(null)); // Uri is null
        }

        public static IEnumerable<object[]> IsWellFormedOriginalString_TestData()
        {
            yield return new object[] { "http://www.domain.com/path?name", true  };
            yield return new object[] { "http://192.168.0.1:50/path1/page?query#fragment", true  };
            yield return new object[] { "http://[::1]:50/path1/page?query#fragment", true  };
            yield return new object[] { "http://[::1]/path1/page?query#fragment", true  };
            yield return new object[] { "unknownscheme:", true  };
            yield return new object[] { "http://www.domain.com/path???/file name", false  };
            yield return new object[] { @"http:\\host/path/file", false  };
            yield return new object[] { "file:////", true  };
            yield return new object[] { @"http:\\host/path/file", false  };
            yield return new object[] { "http://host/path\file", false  };
            yield return new object[] { @"c:\directory\filename", false  };
            yield return new object[] { @"\\unchost", false  };
            yield return new object[] { "file://C:/directory/filename", false  };
            yield return new object[] { "file:///c|/dir", false  };
            yield return new object[] { @"file:\\\c:\path", false  };
             // Unix path
            if (!s_isWindowsSystem)
            {
                yield return new object[] { "/directory/filename", false  };
            }
        }

        [Theory]
        [MemberData(nameof(IsWellFormedOriginalString_TestData))]
        public void IsWellFormedOriginalString(string uriString, bool expected)
        {
            Uri uri = new Uri(uriString);
            Assert.Equal(expected, uri.IsWellFormedOriginalString());
        }

        [Theory]
        [InlineData("http://www.domain.com/path?name", UriKind.Absolute, true)]
        [InlineData("http://www.domain.com/path?name", UriKind.RelativeOrAbsolute, true)]
        [InlineData("http://www.domain.com/path?name", UriKind.Relative, false)]
        [InlineData("/path1/path2", UriKind.Absolute, false)]
        [InlineData("/path1/path2", UriKind.RelativeOrAbsolute, true)]
        [InlineData("/path1/path2", UriKind.Relative, true)]
        [InlineData("http://192.168.0.1/path1/page?query#fragment", UriKind.Absolute, true)]
        [InlineData("http://192.168.0.1/path1/page?query#fragment", UriKind.RelativeOrAbsolute, true)]
        [InlineData("http://192.168.0.1/path1/page?query#fragment", UriKind.Relative, false)]
        [InlineData("http://192.168.0.1:50/path1/page?query#fragment", UriKind.Absolute, true)]
        [InlineData("http://192.168.0.1:50/path1/page?query#fragment", UriKind.RelativeOrAbsolute, true)]
        [InlineData("http://192.168.0.1:50/path1/page?query#fragment", UriKind.Relative, false)]
        [InlineData("http://[::1]/path1/page?query#fragment", UriKind.Absolute, true)]
        [InlineData("http://[::1]/path1/page?query#fragment", UriKind.RelativeOrAbsolute, true)]
        [InlineData("http://[::1]/path1/page?query#fragment", UriKind.Relative, false)]
        [InlineData("http://[::1]:50/path1/page?query#fragment", UriKind.Absolute, true)]
        [InlineData("http://[::1]:50/path1/page?query#fragment", UriKind.RelativeOrAbsolute, true)]
        [InlineData("http://[::1]:50/path1/page?query#fragment", UriKind.Relative, false)]
        [InlineData("http://www.domain.com/path???/file name", UriKind.RelativeOrAbsolute, false)]
        [InlineData("c:\\directory\filename", UriKind.RelativeOrAbsolute, false)]
        [InlineData("file://C:/directory/filename", UriKind.RelativeOrAbsolute, false)]
        [InlineData("http:\\host/path/file", UriKind.RelativeOrAbsolute, false)]
        [InlineData(null, UriKind.RelativeOrAbsolute, false)]
        public void IsWellFormedUriString(string uriString, UriKind uriKind, bool expected)
        {
            Assert.Equal(expected, Uri.IsWellFormedUriString(uriString, uriKind));
        }

        public static IEnumerable<object[]> Compare_TestData()
        {
            var absoluteUri1 = new Uri("http://www.domain.com/path?name#fragment");
            var absoluteUri2 = new Uri("http://www.domainoo.com/path?name#elagment");
            var relativeUri1 = new Uri("/path1/path2/", UriKind.Relative);
            var relativeUri2 = new Uri("/path2/path3", UriKind.Relative);
            var relativeUri3 = new Uri("/path0/path1", UriKind.Relative);

            yield return new object[] { absoluteUri1, absoluteUri2, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.CurrentCulture, -1 };
            yield return new object[] { absoluteUri1, absoluteUri2, UriComponents.Query, UriFormat.UriEscaped, StringComparison.CurrentCulture, 0 };
            yield return new object[] { absoluteUri1, absoluteUri2, UriComponents.Fragment, UriFormat.UriEscaped, StringComparison.CurrentCulture, 1 };
            yield return new object[] { absoluteUri1, absoluteUri2, UriComponents.Query | UriComponents.Fragment, UriFormat.UriEscaped, StringComparison.CurrentCulture, 1 };

            yield return new object[] { null, absoluteUri1, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.CurrentCulture, -1 };
            yield return new object[] { absoluteUri1, null, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.CurrentCulture, 1 };
            yield return new object[] { null, null, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.CurrentCulture, 0 };

            yield return new object[] { relativeUri1, relativeUri2, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.CurrentCulture, -1 };
            yield return new object[] { relativeUri1, relativeUri1, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.CurrentCulture, 0 };
            yield return new object[] { relativeUri1, relativeUri3, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.CurrentCulture, 1 };
            yield return new object[] { absoluteUri1, relativeUri1, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.CurrentCulture, 1 };
            yield return new object[] { relativeUri1, absoluteUri1, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.CurrentCulture, -1 };
        }

        [Theory]
        [MemberData(nameof(Compare_TestData))]
        public void Compare(Uri uri1, Uri uri2, UriComponents partsToCompare, UriFormat compareFormat, StringComparison comparisonType, int expected)
        {
            Assert.Equal(expected, Math.Sign(Uri.Compare(uri1, uri2, partsToCompare, compareFormat, comparisonType)));
        }

        [Fact]
        public void Compare_Invalid()
        {
            var uri = new Uri("http://domain.com");
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => Uri.Compare(uri, uri, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => Uri.Compare(uri, uri, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase + 1));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            // Urls
            yield return new object[] { new Uri("http://domain.com"), new Uri("http://domain.com"), true };
            yield return new object[] { new Uri("https://www.domain.com"), new Uri("http://www.domain.com"), false }; // Different scheme
            yield return new object[] { new Uri("http://domain.com"), new Uri("http://www.domain.com"), false };  // Different domain

            yield return new object[] { new Uri("http://www.domain.com/path?name#fragment"), new Uri("http://www.domain.com/path?name#fragment"), true };
            yield return new object[] { new Uri("http://www.domain.com/path?name"), new Uri("http://www.domain.com/path?name#fragment"), true }; // Fragments are ignored
            yield return new object[] { new Uri("http://www.domain.com/path?name#fragment"), new Uri("http://aaa.domain.com/path?bame#fragment"), false }; // Different domain
            yield return new object[] { new Uri("http://www.domain.com/path?name#fragment"), new Uri("http://www.domain.com/path?bame#fragment"), false }; // Different query
            yield return new object[] { new Uri("http://www.domain.com/page.html"), new Uri("http://www.username:password@domain.com/page.html"), false };

            yield return new object[] { new Uri("http://www.domain.com/path?name"), new Uri("http://www.domain.com/?name"), false }; // Different path
            yield return new object[] { new Uri("http://www.domain.com/path?name#frag"), new Uri("http://www.domain.com/path1?name#slag"), false }; // Different path
            yield return new object[] { new Uri("http://www.domain.com/path?name"), new Uri("http://www.domain.com/path/?name2"), false }; // Different query

            yield return new object[] { new Uri("http://www.domain.com:100/path?name#fragment"), new Uri("http://www.domain.com:100/path?name#fragment"), true };
            yield return new object[] { new Uri("http://www.domain.com:100/path?name#fragment"), new Uri("http://www.domain.com:800/path?name#fragment"), false }; // Different port
            yield return new object[] { new Uri("http://www.domain.com:100/path?name#fragment"), new Uri("http://www.domain.com:80/path?name#fragment"), false }; // Different port

            // File paths
            yield return new object[] { new Uri("file://C:/path1/path2/file"), new Uri("file://C:/path1/path2/file"), true };
            yield return new object[] { new Uri("file://C:/path1/path2/file"), new Uri("file://C:/path1/Path2/File"), true };
            yield return new object[] { new Uri("file://C:/path1/path2/file"), new Uri("file://D:/path1/path2/file"), false };
            yield return new object[] { new Uri("file://C:/path1/path2/file"), new Uri("file://C:/path2/path2/file"), false };
            yield return new object[] { new Uri("file://C:/path1/path2/file"), new Uri("file://C:/path1/path1/file"), false };
            yield return new object[] { new Uri("file://C:/path1/path2/file"), new Uri("file://C:/path1/path2/file!"), false };
            yield return new object[] { new Uri("file://C:/path1/path2/file"), new Uri("http://domain.com"), false };
            yield return new object[] { new Uri("file://C:/path1/path2/file"), new Uri(@"\\server\path1\path2\file"), false };

            // UNC share paths
            yield return new object[] { new Uri(@"\\server\sharepath\path\file"), new Uri(@"\\server\sharepath\path\file"), true };
            yield return new object[] { new Uri(@"\\server\sharepath\path\file"), new Uri(@"\\server1\sharepath\path\file"), false };
            yield return new object[] { new Uri(@"\\server\sharepath\path\file"), new Uri(@"\\server\sharepata\path\file"), false };
            yield return new object[] { new Uri(@"\\server\sharepath\path\file"), new Uri(@"\\server\sharepath\pata\file"), false };
            yield return new object[] { new Uri(@"\\server\sharepath\path\file"), new Uri(@"\\server\sharepath\path\file!"), false };

            // Unix path
            if (!s_isWindowsSystem)
            {
                // Implicit file
                yield return new object[] { new Uri("/sharepath/path/file"), new Uri("/sharepath/path/file"), true };
                yield return new object[] { new Uri("/sharepath/path/file"), new Uri("/sharepath/path/File"), false };
                yield return new object[] { new Uri("/sharepath/path/file"), new Uri("/sharepata/path/file"), false };
                yield return new object[] { new Uri("/sharepath/path/file"), new Uri("/sharepath/pata/file"), false };
                yield return new object[] { new Uri("/sharepath/path/file"), new Uri("/sharepath/path/file!"), false };
                yield return new object[] { new Uri(@"/shar\path/path/file"), new Uri("/shar/path/path/file"), false };
            }

            // Relative paths
            yield return new object[] { new Uri("/path1/path2/", UriKind.Relative), new Uri("/path1/path2/", UriKind.Relative), true };
            yield return new object[] { new Uri("/path1/path2/", UriKind.Relative), new Uri("/path1/path2", UriKind.Relative), false };
            yield return new object[] { new Uri("/path1/path2/", UriKind.Relative), new Uri("/path3/path4/", UriKind.Relative), false };
            yield return new object[] { new Uri("/domain/", UriKind.Relative), new Uri("http://domain/"), false }; // Relative and absolute uris are never equal

            Uri uri = new Uri("http://domain.com/");
            yield return new object[] { uri, uri, true }; // Uri1 and uri2 are the same reference

            yield return new object[] { null, new Uri("http://domain.com"), false }; // Uri1 is null
            yield return new object[] { new Uri("http://domain.com/"), null, false }; // Uri2 is null

            yield return new object[] { new Uri("", UriKind.Relative), new Uri("", UriKind.Relative), true };

            // A uri can equal a string
            yield return new object[] { new Uri("http://www.domain.com/path?name#frag"), "http://www.domain.com/path?name#frag", true };
            yield return new object[] { new Uri("http://www.domain.com/path?name#frag"), "http://www.domain.com/path?name#aaaa", true };
            yield return new object[] { new Uri("http://www.domain.com/path?name#frag"), "http://www.domain.com/aaaaaaaaaaaaaa", false };
            yield return new object[] { new Uri("http://www.domain.com/path?name#frag"), "http://www.domain.com/", false };
            yield return new object[] { new Uri("http://www.domain.com/path?name#frag"), @"http://www.domain.com\", false };
            yield return new object[] { new Uri("http://www.domain.com/path?name#frag"), 123, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(Uri uri1, object obj, bool expected)
        {
            Uri uri2 = obj as Uri;

            if (uri1 != null)
            {
                Assert.Equal(expected, uri1.Equals(obj));

                if (uri2 != null && expected)
                {
                    Assert.Equal(uri1.GetHashCode(), uri2.GetHashCode());
                }
            }

            if (!(obj is string))
            {
                Assert.Equal(expected, uri1 == uri2);
                Assert.Equal(!expected, uri1 != uri2);
            }
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("Hello", "Hello")]
        [InlineData("He\\l/lo", "He%5Cl%2Flo")]
        [InlineData("\uD800\uDC00", "%F0%90%80%80")] // With surrogate pair
        public void EscapeDataString(string stringToEscape, string expected)
        {
            Assert.Equal(expected, Uri.EscapeDataString(stringToEscape));
        }

        [Fact]
        public void EscapeDataString_InvalidSurrogatePairs()
        {
            EscapeDataString("\uD800", "%EF%BF%BD");
            EscapeDataString("abc\uD800", "abc%EF%BF%BD");
            EscapeDataString("abc\uD800\uD800abc", "abc%EF%BF%BD%EF%BF%BDabc");
            EscapeDataString("\xD800\xD800\xDFFF", "%EF%BF%BD%F0%90%8F%BF");
        }

        [Fact]
        public void EscapeDataString_Long_Success()
        {
            string s;
            const int LongCount = 65520 + 1;

            s = new string('a', LongCount);
            Assert.Equal(s, Uri.EscapeDataString(s));

            s = new string('/', LongCount);
            Assert.Equal(string.Concat(Enumerable.Repeat("%2F", LongCount)), Uri.EscapeDataString(s));
        }

        [Fact]
        public void EscapeDataString_NullArgument()
        {
            AssertExtensions.Throws<ArgumentNullException>("stringToEscape", () => Uri.EscapeDataString(null));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("Hello", "Hello")]
        [InlineData("He%5Cl/lo", "He\\l/lo")]
        [InlineData("%F0%90%80%80", "\uD800\uDC00")] // Surrogate pair
        public void UnescapeDataString(string stringToUnEscape, string expected)
        {
            Assert.Equal(expected, Uri.UnescapeDataString(stringToUnEscape));
        }

        [Fact]
        public void UnescapedDataString_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("stringToUnescape", () => Uri.UnescapeDataString(null)); // StringToUnescape is null
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("Hello", "Hello")]
        [InlineData("He\\l/lo", "He%5Cl/lo")]
        [InlineData("\uD800\uDC00", "%F0%90%80%80")] // With surrogate pair
        public void EscapeUriString(string stringToEscape, string expected)
        {
            Assert.Equal(expected, Uri.EscapeUriString(stringToEscape));
        }

        [Fact]
        public void EscapeUriString_InvalidSurrogatePairs()
        {
            EscapeUriString("\uD800", "%EF%BF%BD");
            EscapeUriString("abc\uD800", "abc%EF%BF%BD");
            EscapeUriString("abc\uD800\uD800abc", "abc%EF%BF%BD%EF%BF%BDabc");
            EscapeUriString("\xD800\xD800\xDFFF", "%EF%BF%BD%F0%90%8F%BF");
        }

        [Fact]
        public void EscapeUriString_Long_Success()
        {
            string s;
            const int LongCount = 65520 + 1;

            s = new string('a', LongCount);
            Assert.Equal(s, Uri.EscapeUriString(s));

            s = new string('<', LongCount);
            Assert.Equal(string.Concat(Enumerable.Repeat("%3C", LongCount)), Uri.EscapeUriString(s));
        }

        [Fact]
        public void EscapeUriString_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("stringToEscape", () => Uri.EscapeUriString(null)); // StringToEscape is null
        }

        public static IEnumerable<object[]> GetComponents_Basic_TestData()
        {
            Uri fullUri = new Uri("http://userinfo@www.domain.com/path?query=value&query2=value2#fragment");
            yield return new object[] { fullUri, UriComponents.AbsoluteUri, "http://userinfo@www.domain.com/path?query=value&query2=value2#fragment" };
            yield return new object[] { fullUri, UriComponents.Fragment, "fragment" };
            yield return new object[] { fullUri, UriComponents.Host, "www.domain.com" };
            yield return new object[] { fullUri, UriComponents.HostAndPort, "www.domain.com:80" };
            yield return new object[] { fullUri, UriComponents.HttpRequestUrl, "http://www.domain.com/path?query=value&query2=value2" };
            yield return new object[] { fullUri, UriComponents.NormalizedHost, "www.domain.com" };
            yield return new object[] { fullUri, UriComponents.Path, "path" };
            yield return new object[] { fullUri, UriComponents.PathAndQuery, "/path?query=value&query2=value2" };
            yield return new object[] { fullUri, UriComponents.Port, "" };
            yield return new object[] { fullUri, UriComponents.Query, "query=value&query2=value2" };
            yield return new object[] { fullUri, UriComponents.Scheme, "http" };
            yield return new object[] { fullUri, UriComponents.SchemeAndServer, "http://www.domain.com" };
            yield return new object[] { fullUri, UriComponents.SerializationInfoString, "http://userinfo@www.domain.com/path?query=value&query2=value2#fragment" };
            yield return new object[] { fullUri, UriComponents.StrongAuthority, "userinfo@www.domain.com:80" };
            yield return new object[] { fullUri, UriComponents.StrongPort, "80" };
            yield return new object[] { fullUri, UriComponents.UserInfo, "userinfo" };

            Uri unicodeHostUri = new Uri("http://\u043F\u0440\u0438\u0432\u0435\u0442.\u03B2\u03AD\u03BB\u03B1\u03C3\u03BC\u03B1");
            yield return new object[] { unicodeHostUri, UriComponents.Host, "\u043F\u0440\u0438\u0432\u0435\u0442.\u03B2\u03AD\u03BB\u03B1\u03C3\u03BC\u03B1" };
            yield return new object[] { unicodeHostUri, UriComponents.NormalizedHost, "\u043F\u0440\u0438\u0432\u0435\u0442.\u03B2\u03AD\u03BB\u03B1\u03C3\u03BC\u03B1" };

            // Punicode
            Uri punicodeUriWithoutAscii = new Uri("http://xn--b1agh1afp");
            yield return new object[] { punicodeUriWithoutAscii, UriComponents.Host, "xn--b1agh1afp" };
            yield return new object[] { punicodeUriWithoutAscii, UriComponents.NormalizedHost, "\u043F\u0440\u0438\u0432\u0435\u0442" };

            Uri punicodeUriWithAscii1 = new Uri("http://xn--b1agh1afp.xyza");
            yield return new object[] { punicodeUriWithAscii1, UriComponents.Host, "xn--b1agh1afp.xyza" };
            yield return new object[] { punicodeUriWithAscii1, UriComponents.NormalizedHost, "\u043F\u0440\u0438\u0432\u0435\u0442.xyza" };

            Uri punicodeUriWithAscii2 = new Uri("http://xn--b1agh1afp.ascii.xn--b1agh1afp");
            yield return new object[] { punicodeUriWithAscii2, UriComponents.Host, "xn--b1agh1afp.ascii.xn--b1agh1afp" };
            yield return new object[] { punicodeUriWithAscii2, UriComponents.NormalizedHost, "\u043F\u0440\u0438\u0432\u0435\u0442.ascii.\u043F\u0440\u0438\u0432\u0435\u0442" };

            Uri invalidPunicodeUri = new Uri("http://xn--\u1234pck.com");
            yield return new object[] { invalidPunicodeUri, UriComponents.Host, "xn--\u1234pck.com" };
            if (PlatformDetection.IsNlsGlobalization) // expected platform differences, see https://github.com/dotnet/runtime/issues/17190
            {
                yield return new object[] { invalidPunicodeUri, UriComponents.NormalizedHost, "xn--\u1234pck.com" };
            }

            // Custom port
            Uri customPortUri = new Uri("http://www.domain.com:50");
            yield return new object[] { customPortUri, UriComponents.AbsoluteUri, "http://www.domain.com:50/" };
            yield return new object[] { customPortUri, UriComponents.Fragment, "" };
            yield return new object[] { customPortUri, UriComponents.Host, "www.domain.com" };
            yield return new object[] { customPortUri, UriComponents.HostAndPort, "www.domain.com:50" };
            yield return new object[] { customPortUri, UriComponents.HttpRequestUrl, "http://www.domain.com:50/" };
            yield return new object[] { customPortUri, UriComponents.Path, "" };
            yield return new object[] { customPortUri, UriComponents.PathAndQuery, "/" };
            yield return new object[] { customPortUri, UriComponents.Port, "50" };
            yield return new object[] { customPortUri, UriComponents.Query, "" };
            yield return new object[] { customPortUri, UriComponents.Scheme, "http" };
            yield return new object[] { customPortUri, UriComponents.SchemeAndServer, "http://www.domain.com:50" };
            yield return new object[] { customPortUri, UriComponents.SerializationInfoString, "http://www.domain.com:50/" };
            yield return new object[] { customPortUri, UriComponents.StrongAuthority, "www.domain.com:50" };
            yield return new object[] { customPortUri, UriComponents.StrongPort, "50" };
            yield return new object[] { customPortUri, UriComponents.UserInfo, "" };

            // IPv6
            yield return new object[] { new Uri("http://[1111:2222:3333::431%16]"), UriComponents.SerializationInfoString, "http://[1111:2222:3333::431%16]/" }; // With scope id

            // File
            yield return new object[] { new Uri("file:///C|/path1/path2/file"), UriComponents.AbsoluteUri, "file:///C:/path1/path2/file" }; // Non canonical

            Uri uncUri = new Uri("\\\\\u1234\u2345");
            yield return new object[] { uncUri, UriComponents.Host, "\u1234\u2345" };
            yield return new object[] { uncUri, UriComponents.NormalizedHost, "\u1234\u2345" };

            // Unknown
            Uri unknownUri = new Uri("unknownscheme:");
            yield return new object[] { unknownUri, UriComponents.Fragment, "" };
            yield return new object[] { unknownUri, UriComponents.Host, "" };
            yield return new object[] { unknownUri, UriComponents.HostAndPort, "" };
            yield return new object[] { unknownUri, UriComponents.HttpRequestUrl,  "unknownscheme:" };
            yield return new object[] { unknownUri, UriComponents.Path, "" };
            yield return new object[] { unknownUri, UriComponents.PathAndQuery, "" };
            yield return new object[] { unknownUri, UriComponents.Port, "" };
            yield return new object[] { unknownUri, UriComponents.Query, "" };
            yield return new object[] { unknownUri, UriComponents.Scheme, "unknownscheme" };
            yield return new object[] { unknownUri, UriComponents.SchemeAndServer, "unknownscheme:" };
            yield return new object[] { unknownUri, UriComponents.SerializationInfoString, "unknownscheme:" };
            yield return new object[] { unknownUri, UriComponents.StrongAuthority, "" };
            yield return new object[] { unknownUri, UriComponents.StrongPort, "" };
            yield return new object[] { unknownUri, UriComponents.UserInfo, "" };

            Uri urnUri = new Uri("urn:namespace:identifier");
            yield return new object[] { urnUri, UriComponents.Host, "" };
            yield return new object[] { urnUri, UriComponents.Path, "namespace:identifier" };
            yield return new object[] { urnUri, UriComponents.SerializationInfoString, "urn:namespace:identifier" };

            // Relative
            yield return new object[] { new Uri("", UriKind.Relative), UriComponents.SerializationInfoString, "" };
        }

        [Theory]
        [MemberData(nameof(GetComponents_Basic_TestData))]
        public void GetComponents(Uri uri, UriComponents components, string expected)
        {
            GetComponents_Advanced(uri, components, UriFormat.SafeUnescaped, expected);
            GetComponents_Advanced(uri, components, UriFormat.Unescaped, expected);
            GetComponents_Advanced(uri, components, UriFormat.UriEscaped, expected);
        }

        public static IEnumerable<object[]> GetComponents_Advanced_TestData()
        {
            Uri unescapedUri = new Uri("http://ab\u1234\u2345\u3456cd@\u043F\u0440\u0438\u0432\u0435\u0442/\u4567/\u5678?\u6789#\u7890");
            yield return new object[] { unescapedUri, UriComponents.AbsoluteUri, UriFormat.UriEscaped, "http://ab%E1%88%B4%E2%8D%85%E3%91%96cd@\u043F\u0440\u0438\u0432\u0435\u0442/%E4%95%A7/%E5%99%B8?%E6%9E%89#%E7%A2%90" };
            yield return new object[] { unescapedUri, UriComponents.Fragment, UriFormat.UriEscaped, "%E7%A2%90" };
            yield return new object[] { unescapedUri, UriComponents.Host, UriFormat.UriEscaped, "\u043F\u0440\u0438\u0432\u0435\u0442" };
            yield return new object[] { unescapedUri, UriComponents.HostAndPort, UriFormat.UriEscaped, "\u043F\u0440\u0438\u0432\u0435\u0442:80" };
            yield return new object[] { unescapedUri, UriComponents.HttpRequestUrl, UriFormat.UriEscaped, "http://\u043F\u0440\u0438\u0432\u0435\u0442/%E4%95%A7/%E5%99%B8?%E6%9E%89" };
            yield return new object[] { unescapedUri, UriComponents.NormalizedHost, UriFormat.UriEscaped, "\u043F\u0440\u0438\u0432\u0435\u0442" };
            yield return new object[] { unescapedUri, UriComponents.Path, UriFormat.UriEscaped, "%E4%95%A7/%E5%99%B8" };
            yield return new object[] { unescapedUri, UriComponents.PathAndQuery, UriFormat.UriEscaped, "/%E4%95%A7/%E5%99%B8?%E6%9E%89" };
            yield return new object[] { unescapedUri, UriComponents.Port, UriFormat.UriEscaped, "" };
            yield return new object[] { unescapedUri, UriComponents.Query, UriFormat.UriEscaped, "%E6%9E%89" };
            yield return new object[] { unescapedUri, UriComponents.Scheme, UriFormat.UriEscaped, "http" };
            yield return new object[] { unescapedUri, UriComponents.SchemeAndServer, UriFormat.UriEscaped, "http://\u043F\u0440\u0438\u0432\u0435\u0442" };
            yield return new object[] { unescapedUri, UriComponents.SerializationInfoString, UriFormat.UriEscaped, "http://ab%E1%88%B4%E2%8D%85%E3%91%96cd@\u043F\u0440\u0438\u0432\u0435\u0442/%E4%95%A7/%E5%99%B8?%E6%9E%89#%E7%A2%90" };
            yield return new object[] { unescapedUri, UriComponents.StrongAuthority, UriFormat.UriEscaped, "ab%E1%88%B4%E2%8D%85%E3%91%96cd@\u043F\u0440\u0438\u0432\u0435\u0442:80" };
            yield return new object[] { unescapedUri, UriComponents.StrongPort, UriFormat.UriEscaped, "80" };
            yield return new object[] { unescapedUri, UriComponents.UserInfo, UriFormat.UriEscaped, "ab%E1%88%B4%E2%8D%85%E3%91%96cd" };

            yield return new object[] { unescapedUri, UriComponents.AbsoluteUri, UriFormat.Unescaped, "http://ab\u1234\u2345\u3456cd@\u043F\u0440\u0438\u0432\u0435\u0442/\u4567/\u5678?\u6789#\u7890" };
            yield return new object[] { unescapedUri, UriComponents.Fragment, UriFormat.Unescaped, "\u7890" };
            yield return new object[] { unescapedUri, UriComponents.Host, UriFormat.Unescaped, "\u043F\u0440\u0438\u0432\u0435\u0442" };
            yield return new object[] { unescapedUri, UriComponents.HostAndPort, UriFormat.Unescaped, "\u043F\u0440\u0438\u0432\u0435\u0442:80" };
            yield return new object[] { unescapedUri, UriComponents.HttpRequestUrl, UriFormat.Unescaped, "http://\u043F\u0440\u0438\u0432\u0435\u0442/\u4567/\u5678?\u6789" };
            yield return new object[] { unescapedUri, UriComponents.NormalizedHost, UriFormat.Unescaped, "\u043F\u0440\u0438\u0432\u0435\u0442" };
            yield return new object[] { unescapedUri, UriComponents.Path, UriFormat.Unescaped, "\u4567/\u5678" };
            yield return new object[] { unescapedUri, UriComponents.PathAndQuery, UriFormat.Unescaped, "/\u4567/\u5678?\u6789" };
            yield return new object[] { unescapedUri, UriComponents.Port, UriFormat.Unescaped, "" };
            yield return new object[] { unescapedUri, UriComponents.Query, UriFormat.Unescaped, "\u6789" };
            yield return new object[] { unescapedUri, UriComponents.Scheme, UriFormat.Unescaped, "http" };
            yield return new object[] { unescapedUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, "http://\u043F\u0440\u0438\u0432\u0435\u0442" };
            yield return new object[] { unescapedUri, UriComponents.SerializationInfoString, UriFormat.Unescaped, "http://ab\u1234\u2345\u3456cd@\u043F\u0440\u0438\u0432\u0435\u0442/\u4567/\u5678?\u6789#\u7890" };
            yield return new object[] { unescapedUri, UriComponents.StrongAuthority, UriFormat.Unescaped, "ab\u1234\u2345\u3456cd@\u043F\u0440\u0438\u0432\u0435\u0442:80" };
            yield return new object[] { unescapedUri, UriComponents.StrongPort, UriFormat.Unescaped, "80" };
            yield return new object[] { unescapedUri, UriComponents.UserInfo, UriFormat.Unescaped, "ab\u1234\u2345\u3456cd" };

            yield return new object[] { unescapedUri, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, "http://ab\u1234\u2345\u3456cd@\u043F\u0440\u0438\u0432\u0435\u0442/\u4567/\u5678?\u6789#\u7890" };
            yield return new object[] { unescapedUri, UriComponents.Fragment, UriFormat.SafeUnescaped, "\u7890" };
            yield return new object[] { unescapedUri, UriComponents.Host, UriFormat.SafeUnescaped, "\u043F\u0440\u0438\u0432\u0435\u0442" };
            yield return new object[] { unescapedUri, UriComponents.HostAndPort, UriFormat.SafeUnescaped, "\u043F\u0440\u0438\u0432\u0435\u0442:80" };
            yield return new object[] { unescapedUri, UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped, "http://\u043F\u0440\u0438\u0432\u0435\u0442/\u4567/\u5678?\u6789" };
            yield return new object[] { unescapedUri, UriComponents.NormalizedHost, UriFormat.SafeUnescaped, "\u043F\u0440\u0438\u0432\u0435\u0442" };
            yield return new object[] { unescapedUri, UriComponents.Path, UriFormat.SafeUnescaped, "\u4567/\u5678" };
            yield return new object[] { unescapedUri, UriComponents.PathAndQuery, UriFormat.SafeUnescaped, "/\u4567/\u5678?\u6789" };
            yield return new object[] { unescapedUri, UriComponents.Port, UriFormat.SafeUnescaped, "" };
            yield return new object[] { unescapedUri, UriComponents.Query, UriFormat.SafeUnescaped, "\u6789" };
            yield return new object[] { unescapedUri, UriComponents.Scheme, UriFormat.SafeUnescaped, "http" };
            yield return new object[] { unescapedUri, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, "http://\u043F\u0440\u0438\u0432\u0435\u0442" };
            yield return new object[] { unescapedUri, UriComponents.SerializationInfoString, UriFormat.SafeUnescaped, "http://ab\u1234\u2345\u3456cd@\u043F\u0440\u0438\u0432\u0435\u0442/\u4567/\u5678?\u6789#\u7890" };
            yield return new object[] { unescapedUri, UriComponents.StrongAuthority, UriFormat.SafeUnescaped, "ab\u1234\u2345\u3456cd@\u043F\u0440\u0438\u0432\u0435\u0442:80" };
            yield return new object[] { unescapedUri, UriComponents.StrongPort, UriFormat.SafeUnescaped, "80" };
            yield return new object[] { unescapedUri, UriComponents.UserInfo, UriFormat.SafeUnescaped, "ab\u1234\u2345\u3456cd" };

            // Relative
            yield return new object[] { new Uri("path/file?query1&query2=value#fragment", UriKind.Relative), UriComponents.SerializationInfoString, UriFormat.UriEscaped, "path/file?query1&query2=value#fragment" };
            yield return new object[] { new Uri("\u1234\u2345\u3456", UriKind.Relative), UriComponents.SerializationInfoString, UriFormat.UriEscaped, "%E1%88%B4%E2%8D%85%E3%91%96" };
            yield return new object[] { new Uri("\u1234\u2345\u3456", UriKind.Relative), UriComponents.SerializationInfoString, UriFormat.Unescaped, "\u1234\u2345\u3456" };
            yield return new object[] { new Uri("\u1234\u2345\u3456", UriKind.Relative), UriComponents.SerializationInfoString, UriFormat.SafeUnescaped, "\u1234\u2345\u3456" };
        }

        [Theory]
        [MemberData(nameof(GetComponents_Advanced_TestData))]
        public void GetComponents_Advanced(Uri uri, UriComponents components, UriFormat format, string expected)
        {
            Assert.Equal(expected, uri.GetComponents(components, format));
        }

        [Fact]
        public void GetComponents_Invalid()
        {
            var absoluteUri = new Uri("http://domain");
            var relativeUri = new Uri("path", UriKind.Relative);

            Assert.Throws<ArgumentOutOfRangeException>(() => absoluteUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped - 1)); // Format is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => absoluteUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped + 1)); // Format is invalid

            AssertExtensions.Throws<ArgumentOutOfRangeException>("components", () => absoluteUri.GetComponents(UriComponents.HostAndPort | ~UriComponents.KeepDelimiter, UriFormat.UriEscaped)); // Components is invalid

            Assert.Throws<InvalidOperationException>(() => relativeUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.Unescaped)); // Uri is relative
            Assert.Throws<ArgumentOutOfRangeException>(() => relativeUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped - 1)); // Uri is relative, format is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => relativeUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped + 1)); // Uri is relative, format is invalid
        }
    }
}
