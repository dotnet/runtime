// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public class UriCreateUriTests
    {
        private static readonly bool s_isWindowsSystem = PlatformDetection.IsWindows;

        public static IEnumerable<object[]> Create_Uri_TestData()
        {
            yield return new object[] { "http://host/?query#fragment", "path", UriKind.RelativeOrAbsolute, "http://host/path" };
            yield return new object[] { "http://hostold/path1", "//host", UriKind.RelativeOrAbsolute, "http://host/" };
            yield return new object[] { "http://host/", "", UriKind.RelativeOrAbsolute, "http://host/" };

            // Query
            yield return new object[] { "http://host", "?query", UriKind.RelativeOrAbsolute, "http://host/?query" };
            yield return new object[] { "http://host?queryold", "?query", UriKind.RelativeOrAbsolute, "http://host/?query" };
            yield return new object[] { "http://host#fragment", "?query", UriKind.RelativeOrAbsolute, "http://host/?query" };
            yield return new object[] { "http://host", " \t \r \n   ?query  \t \r \n  ", UriKind.RelativeOrAbsolute, "http://host/?query" };

            // Fragment
            yield return new object[] { "http://host", "#fragment", UriKind.RelativeOrAbsolute, "http://host/#fragment" };
            yield return new object[] { "http://host#fragmentold", "#fragment", UriKind.RelativeOrAbsolute, "http://host/#fragment" };
            yield return new object[] { "http://host?query", "#fragment", UriKind.RelativeOrAbsolute, "http://host/?query#fragment" };
            yield return new object[] { "http://host", " \t \r \n   #fragment  \t \r \n  ", UriKind.RelativeOrAbsolute, "http://host/#fragment" };

            // Path
            yield return new object[] { "http://host/", "path1/page?query=value#fragment", UriKind.RelativeOrAbsolute, "http://host/path1/page?query=value#fragment" };
            yield return new object[] { "http://host/", "C:/x", UriKind.Relative, "http://host/C:/x" };

            // Explicit windows drive file
            yield return new object[] { "file:///D:/abc", "C:/x", UriKind.Relative, "file:///C:/x" };
            yield return new object[] { "D:/abc", "C:/x", UriKind.Relative, "file:///C:/x" };
            yield return new object[] { "file:///C:/", "/path", UriKind.RelativeOrAbsolute, "file:///C:/path" };
            yield return new object[] { "file:///C:/", @"\path", UriKind.RelativeOrAbsolute, "file:///C:/path" };
            yield return new object[] { "file:///C:/pathold", "/path", UriKind.RelativeOrAbsolute, "file:///C:/path" };
            yield return new object[] { "file:///C:/pathold", @"\path", UriKind.RelativeOrAbsolute, "file:///C:/path" };
            yield return new object[] { "file:///C:/pathold", "path", UriKind.RelativeOrAbsolute, "file:///C:/path" };
            yield return new object[] { "file:///C:/", "/", UriKind.RelativeOrAbsolute, "file:///C:/" };
            yield return new object[] { "file:///C:/", @"\", UriKind.RelativeOrAbsolute, "file:///C:/" };

            // Implicit windows drive file
            yield return new object[] { "C:/", "/path", UriKind.RelativeOrAbsolute, "file:///C:/path" };
            yield return new object[] { "C:/", @"\path", UriKind.RelativeOrAbsolute, "file:///C:/path" };
            yield return new object[] { "C:/pathold", "/path", UriKind.RelativeOrAbsolute, "file:///C:/path" };
            yield return new object[] { "C:/pathold", @"\path", UriKind.RelativeOrAbsolute, "file:///C:/path" };
            yield return new object[] { "C:/pathold", "path", UriKind.RelativeOrAbsolute, "file:///C:/path" };
            yield return new object[] { "C:/", "/", UriKind.RelativeOrAbsolute, "file:///C:/" };
            yield return new object[] { "C:/", @"\", UriKind.RelativeOrAbsolute, "file:///C:/" };

            // Unix style path
            yield return new object[] { "file:///pathold/", "/path", UriKind.RelativeOrAbsolute, "file:///path" };
            yield return new object[] { "file:///pathold/", "path", UriKind.RelativeOrAbsolute, "file:///pathold/path" };
            yield return new object[] { "file:///", "/path", UriKind.RelativeOrAbsolute, "file:///path" };
            yield return new object[] { "file:///", "path", UriKind.RelativeOrAbsolute, "file:///path" };

            // UNC
            if (s_isWindowsSystem) // Unc can only start with '/' on Windows
            {
                yield return new object[] { @"\\servernameold\path1", "//servername", UriKind.Relative, @"\\servername" };
            }
            yield return new object[] { @"\\servernameold\path1", @"\\servername", UriKind.Relative, @"\\servername" };
            yield return new object[] { @"\\servername\path1", "/path", UriKind.RelativeOrAbsolute, @"\\servername\path1\path" };
            yield return new object[] { @"\\servername\path1", @"\path", UriKind.RelativeOrAbsolute, @"\\servername\path1\path" };
            yield return new object[] { @"\\servername\path1\path2", @"\path", UriKind.RelativeOrAbsolute, @"\\servername\path1\path" };
            yield return new object[] { @"\\servername\pathold", "path", UriKind.RelativeOrAbsolute, @"\\servername\path" };
            yield return new object[] { @"file://\\servername/path1", "/path", UriKind.RelativeOrAbsolute, "file://servername/path1/path" };
            yield return new object[] { @"\\servername\path1", "?query", UriKind.RelativeOrAbsolute, @"\\servername/?query" };

            // Unix path
            if (!s_isWindowsSystem)
            {
                // Implicit file
                yield return new object[] {"/path1", "/path", UriKind.RelativeOrAbsolute, "/path" };
                yield return new object[] {"/path1/path2", "/path", UriKind.RelativeOrAbsolute, "/path" };
                yield return new object[] {"/pathold", "path", UriKind.RelativeOrAbsolute, "/path" };
            }

            // IPv6
            yield return new object[] { "http://[::1]", "/path", UriKind.RelativeOrAbsolute, "http://[::1]/path" };
            yield return new object[] { "http://[::1]", @"\path", UriKind.RelativeOrAbsolute, "http://[::1]/path" };
            yield return new object[] { "http://[::1]",@"path", UriKind.RelativeOrAbsolute, "http://[::1]/path" };
            yield return new object[] { "http://[::1]:90", "/path", UriKind.RelativeOrAbsolute, "http://[::1]:90/path" };
            yield return new object[] { @"\\[::1]/", "path", UriKind.RelativeOrAbsolute, @"\\[::1]/path" };

            // Unknown
            yield return new object[] { "unknown:", "C:/x", UriKind.Relative, "unknown:/C:/x" };
            yield return new object[] { "unknown:", "//host/path?query#fragment", UriKind.RelativeOrAbsolute, "unknown://host/path?query#fragment" };
            yield return new object[] { "unknown:", "path", UriKind.RelativeOrAbsolute, "unknown:path" };
            yield return new object[] { "unknown:pathold", "path", UriKind.RelativeOrAbsolute, "unknown:path" };

            // Telnet
            yield return new object[] { "telnet://username:password@host:10", "path", UriKind.RelativeOrAbsolute, "telnet://username:password@host:10/path" };

            // Absolute Uri
            yield return new object[] { "http://host/", "C:/x", UriKind.Absolute, "file:///C:/x" };
            yield return new object[] { "http://hostold/", "http://host/path/page", UriKind.Absolute, "http://host/path/page" };
        }

        [Theory]
        [MemberData(nameof(Create_Uri_TestData))]
        public void Create_Uri(string uriString1, string uriString2, UriKind uriKind, string expectedUriString)
        {
            Uri baseUri = new Uri(uriString1, UriKind.Absolute);
            Uri relativeUri = new Uri(uriString2, uriKind);

            Uri expected = new Uri(expectedUriString);

            Uri uri = new Uri(baseUri, relativeUri);
            Assert.Equal(expected, uri);

            if (uriKind !=  UriKind.Relative)
            {
                uri = new Uri(baseUri, relativeUri.OriginalString);
                Assert.Equal(expected, uri);

                Assert.True(Uri.TryCreate(baseUri, relativeUri.OriginalString, out uri));
                Assert.Equal(expected, uri);
            }

            Assert.True(Uri.TryCreate(baseUri, relativeUri, out uri));
            Assert.Equal(expected, uri);
        }

        public static IEnumerable<object[]> Create_Uri_Invalid_TestData()
        {
            yield return new object[] { null, "host", typeof(ArgumentNullException), true }; // Base uri is null
            yield return new object[] { new Uri("host", UriKind.Relative), "host", typeof(ArgumentOutOfRangeException), true }; // Base uri is relative

            yield return new object[] { new Uri("http://host/"), @"http://host\", typeof(UriFormatException), false }; // Relative uri is invalid
        }

        [Theory]
        [MemberData(nameof(Create_Uri_Invalid_TestData))]
        public void Create_Uri_Invalid(Uri baseUri, string relativeUri, Type exceptionType, bool createUri)
        {
            Assert.Throws(exceptionType, () => new Uri(baseUri, relativeUri));
            if (createUri)
            {
                Assert.Throws(exceptionType, () => new Uri(baseUri, new Uri(relativeUri, UriKind.RelativeOrAbsolute)));
            }

            Uri result;
            Assert.False(Uri.TryCreate(baseUri, relativeUri, out result));
            Assert.Null(result);
            if (createUri)
            {
                Assert.False(Uri.TryCreate(baseUri, new Uri(relativeUri, UriKind.RelativeOrAbsolute), out result));
                Assert.Null(result);
            }
        }
    }
}
