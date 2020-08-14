// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Tests
{
    public class UriCreateStringTests
    {
        private static readonly bool s_isWindowsSystem = PlatformDetection.IsWindows;
        public static readonly string s_longString = new string('a', 65520 + 1);

        public static IEnumerable<object[]> OriginalString_AbsoluteUri_ToString_TestData()
        {
            // Basic
            yield return new object[] { "http://host", "http://host/", "http://host/" };
            yield return new object[] { @"http:/\host", "http://host/", "http://host/" };
            yield return new object[] { @"http:\/host", "http://host/", "http://host/" };
            yield return new object[] { @"http:\\host", "http://host/", "http://host/" };
            yield return new object[] { @"http://host/path1\path2", "http://host/path1/path2", "http://host/path1/path2" };

            yield return new object[] { "http://userinfo@host:90/path?query#fragment", "http://userinfo@host:90/path?query#fragment", "http://userinfo@host:90/path?query#fragment" };
            yield return new object[] { "http://userinfo@host:80/path?query#fragment", "http://userinfo@host/path?query#fragment", "http://userinfo@host/path?query#fragment" };
            yield return new object[] { "http://userinfo@host:90/path?query#fragment", "http://userinfo@host:90/path?query#fragment", "http://userinfo@host:90/path?query#fragment" };

            // Escaped and non-ascii
            yield return new object[] { "http://userinfo%%%2F%3F%23%5B%5D%40%3B%26%2B%2C%5C%2g%2G@host", "http://userinfo%25%25%2F%3F%23%5B%5D%40%3B%26%2B%2C%5C%252g%252G@host/", "http://userinfo%%%2F%3F%23%5B%5D%40%3B%26%2B%2C%5C%2g%2G@host/" };
            yield return new object[] { "http://\u1234\u2345/\u1234\u2345?\u1234\u2345#\u1234\u2345", "http://\u1234\u2345/%E1%88%B4%E2%8D%85?%E1%88%B4%E2%8D%85#%E1%88%B4%E2%8D%85", "http://\u1234\u2345/\u1234\u2345?\u1234\u2345#\u1234\u2345" };

            // IP
            yield return new object[] { "http://192.168.0.1", "http://192.168.0.1/", "http://192.168.0.1/" };
            yield return new object[] { "http://192.168.0.1/", "http://192.168.0.1/", "http://192.168.0.1/" };
            yield return new object[] { "http://[::1]", "http://[::1]/", "http://[::1]/" };
            yield return new object[] { "http://[::1]/", "http://[::1]/", "http://[::1]/" };

            // Implicit UNC
            yield return new object[] { @"\\unchost", "file://unchost/", "file://unchost/" };
            yield return new object[] { @"\/unchost", "file://unchost/", "file://unchost/" };
            if (s_isWindowsSystem) // Unc can only start with '/' on Windows
            {
                yield return new object[] { @"/\unchost", "file://unchost/", "file://unchost/" };
                yield return new object[] { "//unchost", "file://unchost/", "file://unchost/" };
            }
            yield return new object[] { @"\\\/\/servername\sharename\path\filename", "file://servername/sharename/path/filename", "file://servername/sharename/path/filename" };

            // Explicit UNC
            yield return new object[] { @"file://unchost", "file://unchost/", "file://unchost/" };
            yield return new object[] { @"file://\/unchost", "file://unchost/", "file://unchost/" };
            yield return new object[] { @"file:///\unchost", "file://unchost/", "file://unchost/" };
            yield return new object[] { "file:////unchost", "file://unchost/", "file://unchost/" };

            // Implicit windows drive
            yield return new object[] { "C:/", "file:///C:/", "file:///C:/" };
            yield return new object[] { @"C:\", "file:///C:/", "file:///C:/" };
            yield return new object[] { "C|/", "file:///C:/", "file:///C:/" };
            yield return new object[] { @"C|\", "file:///C:/", "file:///C:/" };

            // Explicit windows drive
            yield return new object[] { "file:///C:/", "file:///C:/", "file:///C:/" };
            yield return new object[] { "file://C:/", "file:///C:/", "file:///C:/" };
            yield return new object[] { @"file:///C:\", "file:///C:/", "file:///C:/" };
            yield return new object[] { @"file://C:\", "file:///C:/", "file:///C:/" };
            yield return new object[] { "file:///C|/", "file:///C:/", "file:///C:/" };
            yield return new object[] { "file://C|/", "file:///C:/", "file:///C:/" };
            yield return new object[] { @"file:///C|\", "file:///C:/", "file:///C:/" };
            yield return new object[] { @"file://C|\", "file:///C:/", "file:///C:/" };

            // Unix path
            if (!s_isWindowsSystem)
            {
                // Implicit File
                yield return new object[] { "/", "file:///", "file:///" };
                yield return new object[] { "/path/filename", "file:///path/filename", "file:///path/filename" };
            }

            // Compressed
            yield return new object[] { "http://host/path1/../path2", "http://host/path2", "http://host/path2" };
            yield return new object[] { "http://host/../", "http://host/", "http://host/" };
        }

        [Theory]
        [MemberData(nameof(OriginalString_AbsoluteUri_ToString_TestData))]
        public void OriginalString_AbsoluteUri_ToString(string uriString, string absoluteUri, string toString)
        {
            PerformAction(uriString, UriKind.Absolute, uri =>
            {
                Assert.Equal(uriString, uri.OriginalString);
                Assert.Equal(absoluteUri, uri.AbsoluteUri);
                Assert.Equal(toString, uri.ToString());
            });
        }

        public static IEnumerable<object[]> Scheme_Authority_TestData()
        {
            // HTTP (Generic Uri Syntax)
            yield return new object[] { "   \t \r   http://host/", "http", "", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://userinfo@host:90", "http", "userinfo", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://@host:90", "http", "", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://@host", "http", "", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://userinfo@host", "http", "userinfo", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://USERINFO@host", "http", "USERINFO", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://host:90", "http", "", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://host", "http", "", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://userinfo@host:90/", "http", "userinfo", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://@host:90/", "http", "", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://@host/", "http", "", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://userinfo@host/", "http", "userinfo", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://host:90/", "http", "", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://host/", "http", "", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://host?query", "http", "", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://host:90?query", "http", "", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://@host:90?query", "http", "", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://userinfo@host:90?query", "http", "userinfo", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://userinfo@host?query", "http", "userinfo", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://host#fragment", "http", "", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://host:90#fragment", "http", "", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://@host:90#fragment", "http", "", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://userinfo@host:90#fragment", "http", "userinfo", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://userinfo@host#fragment", "http", "userinfo", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://user:password@host", "http", "user:password", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://user:80@host:90", "http", "user:80", "host", UriHostNameType.Dns, 90, false, false };
            yield return new object[] { "http://host:0", "http", "", "host", UriHostNameType.Dns, 0, false, false };
            yield return new object[] { "http://host:80", "http", "", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://host:65535", "http", "", "host", UriHostNameType.Dns, 65535, false, false };
            yield return new object[] { "http://part1-part2_part3-part4_part5/", "http", "", "part1-part2_part3-part4_part5", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "HTTP://USERINFO@HOST", "http", "USERINFO", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http.http2-http3+3http://host/", "http.http2-http3+3http", "", "host", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { @"http:\\host", "http", "", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { @"http:/\host", "http", "", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { @"http:\/host", "http", "", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "https://host/", "https", "", "host", UriHostNameType.Dns, 443, true, false };
            yield return new object[] { "http://_/", "http", "", "_", UriHostNameType.Basic, 80, true, false };
            yield return new object[] { "http://-/", "http", "", "-", UriHostNameType.Basic, 80, true, false };
            yield return new object[] { "http://_abc.efg1-hij2_345/path", "http", "", "_abc.efg1-hij2_345", UriHostNameType.Basic, 80, true, false };
            yield return new object[] { "http://_abc./path", "http", "", "_abc.", UriHostNameType.Basic, 80, true, false };
            yield return new object[] { "http://xn--abc", "http", "", "xn--abc", UriHostNameType.Dns, 80, true, false };

            // IPv4 host - decimal
            yield return new object[] { "http://4294967295/", "http", "", "255.255.255.255", UriHostNameType.IPv4, 80, true, false };
            yield return new object[] { "http://4294967296/", "http", "", "4294967296", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://192.168.0.1/", "http", "", "192.168.0.1", UriHostNameType.IPv4, 80, true, false };
            yield return new object[] { "http://192.168.0.1.1/", "http", "", "192.168.0.1.1", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://192.256.0.1/", "http", "", "192.256.0.1", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://192.168.256.1/", "http", "", "192.168.256.1", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://192.168.0.256/", "http", "", "192.168.0.256", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://userinfo@192.168.0.1:90/", "http", "userinfo", "192.168.0.1", UriHostNameType.IPv4, 90, false, false };
            yield return new object[] { "http://192.16777216", "http", "", "192.16777216", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://192.168.65536", "http", "", "192.168.65536", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://192.168.0.256", "http", "", "192.168.0.256", UriHostNameType.Dns, 80, true, false };

            // IPv4 host - hex
            yield return new object[] { "http://0x1a2B3c", "http", "", "0.26.43.60", UriHostNameType.IPv4, 80, true, false };
            yield return new object[] { "http://0x1a.0x2B3c", "http", "", "26.0.43.60", UriHostNameType.IPv4, 80, true, false };
            yield return new object[] { "http://0x1a.0x2B.0x3C4d", "http", "", "26.43.60.77", UriHostNameType.IPv4, 80, true, false };
            yield return new object[] { "http://0x1a.0x2B.0x3C.0x4d", "http", "", "26.43.60.77", UriHostNameType.IPv4, 80, true, false };
            yield return new object[] { "http://0xFFFFFFFF/", "http", "", "255.255.255.255", UriHostNameType.IPv4, 80, true, false };
            yield return new object[] { "http://0xFFFFFF/", "http", "", "0.255.255.255", UriHostNameType.IPv4, 80, true, false };
            yield return new object[] { "http://0xFF/", "http", "", "0.0.0.255", UriHostNameType.IPv4, 80, true, false };
            yield return new object[] { "http://0/", "http", "", "0.0.0.0", UriHostNameType.IPv4, 80, true, false };
            yield return new object[] { "http://0x100000000/", "http", "", "0x100000000", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://0x/", "http", "", "0x", UriHostNameType.Dns, 80, true, false };

            // IPv4 host - octet
            yield return new object[] { "http://192.0123.0.10", "http", "", "192.83.0.10", UriHostNameType.IPv4, 80, true, false };

            // IPv4 host - implicit UNC
            if (s_isWindowsSystem) // Unc can only start with '/' on Windows
            {
                yield return new object[] { "//192.168.0.1", "file", "", "192.168.0.1", UriHostNameType.IPv4, -1, true, false };
                yield return new object[] { @"/\192.168.0.1", "file", "", "192.168.0.1", UriHostNameType.IPv4, -1, true, false };
            }
            yield return new object[] { @"\\192.168.0.1", "file", "", "192.168.0.1", UriHostNameType.IPv4, -1, true, false };
            yield return new object[] { @"\/192.168.0.1", "file", "", "192.168.0.1", UriHostNameType.IPv4, -1, true, false };

            // IPv4 host - explicit UNC
            yield return new object[] { @"file://\\192.168.0.1", "file", "", "192.168.0.1", UriHostNameType.IPv4, -1, true, false };
            if (s_isWindowsSystem) // Unc can only start with '/' on Windows
            {
                yield return new object[] { "file:////192.168.0.1", "file", "", "192.168.0.1", UriHostNameType.IPv4, -1, true, false };
                yield return new object[] { @"file:///\192.168.0.1", "file", "", "192.168.0.1", UriHostNameType.IPv4, -1, true, false };
            }
            yield return new object[] { @"file://\/192.168.0.1", "file", "", "192.168.0.1", UriHostNameType.IPv4, -1, true, false };

            // IPv4 host - other
            yield return new object[] { "file://192.168.0.1", "file", "", "192.168.0.1", UriHostNameType.IPv4, -1, true, false };
            yield return new object[] { "ftp://192.168.0.1", "ftp", "", "192.168.0.1", UriHostNameType.IPv4, 21, true, false };
            yield return new object[] { "telnet://192.168.0.1", "telnet", "", "192.168.0.1", UriHostNameType.IPv4, 23, true, false };
            yield return new object[] { "unknown://192.168.0.1", "unknown", "", "192.168.0.1", UriHostNameType.IPv4, -1, true, false };

            // IPv6 host
            yield return new object[] { "http://[1111:1111:1111:1111:1111:1111:1111:1111]", "http", "", "[1111:1111:1111:1111:1111:1111:1111:1111]", UriHostNameType.IPv6, 80, true, false };
            yield return new object[] { "http://[2001:0db8:0000:0000:0000:ff00:0042:8329]/", "http", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, 80, true, false };
            yield return new object[] { "http://[2001:0db8:0000:0000:0000:ff00:0042:8329]:90/", "http", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, 90, false, false };
            yield return new object[] { "http://[1::]/", "http", "", "[1::]", UriHostNameType.IPv6, 80, true, false };
            yield return new object[] { "http://[1::1]/", "http", "", "[1::1]", UriHostNameType.IPv6, 80, true, false };
            yield return new object[] { "http://[::192.168.0.1]/", "http", "", "[::192.168.0.1]", UriHostNameType.IPv6, 80, true, false };
            yield return new object[] { "http://[::ffff:0:192.168.0.1]/", "http", "", "[::ffff:0:192.168.0.1]", UriHostNameType.IPv6, 80, true, false }; // SIIT
            yield return new object[] { "http://[::ffff:1:192.168.0.1]/", "http", "", "[::ffff:1:c0a8:1]", UriHostNameType.IPv6, 80, true, false }; // SIIT (invalid)
            yield return new object[] { "http://[fe80::0000:5efe:192.168.0.1]/", "http", "", "[fe80::5efe:192.168.0.1]", UriHostNameType.IPv6, 80, true, false }; // ISATAP
            yield return new object[] { "http://[1111:2222:3333::431/20]", "http", "", "[1111:2222:3333::431]", UriHostNameType.IPv6, 80, true, false }; // Prefix

            // IPv6 Host - implicit UNC
            if (s_isWindowsSystem) // Unc can only start with '/' on Windows
            {
                yield return new object[] { "//[2001:0db8:0000:0000:0000:ff00:0042:8329]", "file", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, -1, true, false };
                yield return new object[] { @"/\[2001:0db8:0000:0000:0000:ff00:0042:8329]", "file", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, -1, true, false };
            }
            yield return new object[] { @"\\[2001:0db8:0000:0000:0000:ff00:0042:8329]", "file", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, -1, true, false };
            yield return new object[] { @"\/[2001:0db8:0000:0000:0000:ff00:0042:8329]", "file", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, -1, true, false };
            yield return new object[] { @"file://\\[2001:0db8:0000:0000:0000:ff00:0042:8329]", "file", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, -1, true, false };

            // IPv6 host - explicit UNC
            yield return new object[] { "file:////[2001:0db8:0000:0000:0000:ff00:0042:8329]", "file", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, -1, true, false };
            yield return new object[] { @"file:///\[2001:0db8:0000:0000:0000:ff00:0042:8329]", "file", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, -1, true, false };
            yield return new object[] { @"file://\/[2001:0db8:0000:0000:0000:ff00:0042:8329]", "file", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, -1, true, false };

            // IPv6 Host - other
            yield return new object[] { "file://[2001:0db8:0000:0000:0000:ff00:0042:8329]", "file", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, -1, true, false };
            yield return new object[] { "ftp://[2001:0db8:0000:0000:0000:ff00:0042:8329]", "ftp", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, 21, true, false };
            yield return new object[] { "telnet://[2001:0db8:0000:0000:0000:ff00:0042:8329]", "telnet", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, 23, true, false };
            yield return new object[] { "unknown://[2001:0db8:0000:0000:0000:ff00:0042:8329]", "unknown", "", "[2001:db8::ff00:42:8329]", UriHostNameType.IPv6, -1, true, false };

            // File - empty path
            yield return new object[] { "file:///", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file://\", "file", "", "", UriHostNameType.Basic, -1, true, true };
            // File - host

            yield return new object[] { "file://path1/path2", "file", "", "path1", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { "file:///path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // File - explicit with windows drive with empty path
            yield return new object[] { "file://C:/", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "file://C|/", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file://C:\", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file://C|\", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // File - explicit with windows drive with path
            yield return new object[] { "file://C:/path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "file://C|/path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file://C:\path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file://C|\path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // File - '/' + windows drive with empty path
            yield return new object[] { "file:///C:/", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "file:///C|/", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file:///C:\", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file:///C|\", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // File - '/' + windows drive with path
            yield return new object[] { "file:///C:/path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "file:///C|/path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file:///C:\path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file:///C|\path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // File - implicit with empty path
            yield return new object[] { "C:/", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "C|/", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"C:\", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"C|\", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // File - implicit with path
            yield return new object[] { "C:/path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "C|/path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"C:\path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"C|\path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // UNC - implicit with empty path
            if (s_isWindowsSystem) // Unc can only start with '/' on Windows
            {
                yield return new object[] { "//unchost", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
                yield return new object[] { @"/\unchost", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
            }
            yield return new object[] { @"\\unchost", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { @"\/unchost", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };

            // UNC - implicit with path
            if (s_isWindowsSystem) // Unc can only start with '/' on Windows
            {
                yield return new object[] { "//unchost/path1/path2", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
                yield return new object[] { @"/\unchost/path1/path2", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
            }
            yield return new object[] { @"\\unchost/path1/path2", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { @"\/unchost/path1/path2", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { @"\\\/\/servername\sharename\path\filename", "file", "", "servername", UriHostNameType.Dns, -1, true, false };

            // UNC - explicit with empty host and empty path
            yield return new object[] { @"file://\\", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "file:////", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file:///\", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file://\/", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // UNC - explicit with empty host and non empty path
            yield return new object[] { @"file://\\/", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "file://///", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file:///\/", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file://\//", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // UNC - explicit with empty host and query
            yield return new object[] { @"file://\\?query", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "file:////?query", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file:///\?query", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file://\/?query", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "file://///?a", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "file://///#a", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // UNC - explicit with empty host and fragment
            yield return new object[] { @"file://\\#fragment", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "file:////#fragment", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file:///\#fragment", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file://\/#fragment", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // UNC - explicit with non empty host and empty path
            yield return new object[] { @"file://\\unchost", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { "file:////unchost", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { @"file:///\unchost", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { @"file://\/unchost", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };

            // UNC - explicit with path
            yield return new object[] { @"file://\\unchost/path1/path2", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { "file:////unchost/path1/path2", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { @"file:///\unchost/path1/path2", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { @"file://\/unchost/path1/path2", "file", "", "unchost", UriHostNameType.Dns, -1, true, false };

            // UNC - explicit with windows drive
            yield return new object[] { @"file://\\C:/path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "file:////C:/path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file:///\C:/path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { @"file://\/C:/path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };

            // Unix path
            if (!s_isWindowsSystem)
            {
                // Implicit with path
                yield return new object[] { "/path1/path2", "file", "", "", UriHostNameType.Basic, -1, true, true };
                yield return new object[] { "/", "file", "", "", UriHostNameType.Basic, -1, true, true };
            }

            // File - with host
            yield return new object[] { @"file://host/", "file", "", "host", UriHostNameType.Dns, -1, true, false };

            yield return new object[] { "unknown://h.a./", "unknown", "", "h.a.", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { "unknown://h.1./", "unknown", "", "h.1.", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { "unknown://h.-/", "unknown", "", "h.-", UriHostNameType.Basic, -1, true, false };
            yield return new object[] { "unknown://h._", "unknown", "", "h._", UriHostNameType.Basic, -1, true, false };
            yield return new object[] { "unknown://", "unknown", "", "", UriHostNameType.Basic, -1, true, true };

            // Mailto
            yield return new object[] { "mailto:", "mailto", "", "", UriHostNameType.Basic, 25, true, true };
            yield return new object[] { "mailto:someone@example.com", "mailto", "someone", "example.com", UriHostNameType.Dns, 25, true, false };
            yield return new object[] { "mailto://someone@example.com", "mailto", "", "", UriHostNameType.Basic, 25, true, true };
            yield return new object[] { "mailto:/someone@example.com", "mailto", "", "", UriHostNameType.Basic, 25, true, true };

            // FTP
            yield return new object[] { "ftp://host", "ftp", "", "host", UriHostNameType.Dns, 21, true, false };
            yield return new object[] { "ftp://userinfo@host", "ftp", "userinfo", "host", UriHostNameType.Dns, 21, true, false };
            yield return new object[] { "ftp://host?query#fragment", "ftp", "", "host", UriHostNameType.Dns, 21, true, false };

            // Telnet
            yield return new object[] { "telnet://host/", "telnet", "", "host", UriHostNameType.Dns, 23, true, false };
            yield return new object[] { "telnet://host:80", "telnet", "", "host", UriHostNameType.Dns, 80, false, false };
            yield return new object[] { "telnet://userinfo@host/", "telnet", "userinfo", "host", UriHostNameType.Dns, 23, true, false };
            yield return new object[] { "telnet://username:password@host/", "telnet", "username:password", "host", UriHostNameType.Dns, 23, true, false };
            yield return new object[] { "telnet://host?query#fragment", "telnet", "", "host", UriHostNameType.Dns, 23, true, false };
            yield return new object[] { "telnet://host#fragment", "telnet", "", "host", UriHostNameType.Dns, 23, true, false };
            yield return new object[] { "telnet://localhost/", "telnet", "", "localhost", UriHostNameType.Dns, 23, true, true };
            yield return new object[] { "telnet://loopback/", "telnet", "", "localhost", UriHostNameType.Dns, 23, true, true };

            // Unknown
            yield return new object[] { "urn:namespace:segment1:segment2:segment3", "urn", "", "", UriHostNameType.Unknown, -1, true, false };
            yield return new object[] { "unknown:", "unknown", "", "", UriHostNameType.Unknown, -1, true, false };
            yield return new object[] { "unknown:path", "unknown", "", "", UriHostNameType.Unknown, -1, true, false };
            yield return new object[] { "unknown://host", "unknown", "", "host", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { "unknown://userinfo@host", "unknown", "userinfo", "host", UriHostNameType.Dns, -1, true, false };
            yield return new object[] { "unknown://userinfo@host:80", "unknown", "userinfo", "host", UriHostNameType.Dns, 80, false, false };
            yield return new object[] { "unknown://./", "unknown", "", ".", UriHostNameType.Basic, -1, true, false };
            yield return new object[] { "unknown://../", "unknown", "", "..", UriHostNameType.Basic, -1, true, false };
            yield return new object[] { "unknown://////", "unknown", "", "", UriHostNameType.Basic, -1, true, true };
            yield return new object[] { "unknown:///C:/", "unknown", "", "", UriHostNameType.Basic, -1, true, true };

            // Loopback - HTTP
            yield return new object[] { "http://localhost/", "http", "", "localhost", UriHostNameType.Dns, 80, true, true };
            yield return new object[] { "http://loopback/", "http", "", "localhost", UriHostNameType.Dns, 80, true, true };

            // Loopback - implicit UNC with localhost
            if (s_isWindowsSystem) // Unc can only start with '/' on Windows
            {
                yield return new object[] { "//localhost", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
                yield return new object[] { @"/\localhost", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            }
            yield return new object[] { @"\\localhost", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            yield return new object[] { @"\/localhost", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            // Loopback - explicit UNC with localhost
            yield return new object[] { @"file://\\localhost", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            yield return new object[] { @"file:///\localhost", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            yield return new object[] { @"file://\/localhost", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            yield return new object[] { "file:////localhost", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            // Loopback - implicit UNC with loopback
            if (s_isWindowsSystem) // Unc can only start with '/' on Windows
            {
                yield return new object[] { "//loopback", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
                yield return new object[] { @"/\loopback", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            }
            yield return new object[] { @"\\loopback", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            yield return new object[] { @"\/loopback", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            // Loopback - explicit UNC with loopback
            yield return new object[] { @"file://\\loopback", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            yield return new object[] { "file:////loopback", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            yield return new object[] { @"file:///\loopback", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            yield return new object[] { @"file://\/loopback", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };
            // Loopback - IpV4
            yield return new object[] { "http://127.0.0.1/", "http", "", "127.0.0.1", UriHostNameType.IPv4, 80, true, true };
            // Loopback - IpV6
            yield return new object[] { "http://[::1]/", "http", "", "[::1]", UriHostNameType.IPv6, 80, true, true };
            yield return new object[] { "http://[::127.0.0.1]/", "http", "", "[::127.0.0.1]", UriHostNameType.IPv6, 80, true, true };
            // Loopback - File
            yield return new object[] { "file://loopback", "file", "", "localhost", UriHostNameType.Dns, -1, true, true };

            // RFC incompatability
            // We allow any non-unreserved, percent encoding or sub-delimeter in the userinfo
            yield return new object[] { "http://abc\u1234\u2345\u3456@host/", "http", "abc%E1%88%B4%E2%8D%85%E3%91%96", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://\u1234abc\u2345\u3456@host/", "http", "%E1%88%B4abc%E2%8D%85%E3%91%96", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://\u1234\u2345\u3456abc@host/", "http", "%E1%88%B4%E2%8D%85%E3%91%96abc", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://userinfo!~+-_*()[]:;&$=123USERINFO@host/", "http", "userinfo!~+-_*()[]:;&$=123USERINFO", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://%68%65%6C%6C%6F@host/", "http", "hello", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://\u00A3@host/", "http", "%C2%A3", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://\u1234@host/", "http", "%E1%88%B4", "host", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://userinfo%%%2F%3F%23%5B%5D%40%3B%26%2B%2C%5C%2g%2G@host", "http", "userinfo%25%25%2F%3F%23%5B%5D%40%3B%26%2B%2C%5C%252g%252G", "host", UriHostNameType.Dns, 80, true, false };
        }

        [Theory]
        [MemberData(nameof(Scheme_Authority_TestData))]
        public void Scheme_Authority_Basic(string uriString, string scheme, string userInfo, string host, UriHostNameType hostNameType, int port, bool isDefaultPort, bool isLoopback)
        {
            string idnHost = host;
            if (hostNameType == UriHostNameType.IPv6)
            {
                idnHost = host.Substring(1, host.Length - 2);
            }

            Scheme_Authority_IdnHost(uriString, scheme, userInfo, host, idnHost, idnHost, hostNameType, port, isDefaultPort, isLoopback);
        }

        public static IEnumerable<object[]> Scheme_Authority_IdnHost_TestData()
        {
            yield return new object[] { "http://\u043F\u0440\u0438\u0432\u0435\u0442/", "http", "", "\u043F\u0440\u0438\u0432\u0435\u0442", "xn--b1agh1afp", "\u043F\u0440\u0438\u0432\u0435\u0442", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://\u043F\u0440\u0438\u0432\u0435\u0442.ascii/", "http", "", "\u043F\u0440\u0438\u0432\u0435\u0442.ascii", "xn--b1agh1afp.ascii", "\u043F\u0440\u0438\u0432\u0435\u0442.ascii", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://ascii.\u043F\u0440\u0438\u0432\u0435\u0442/", "http", "", "ascii.\u043F\u0440\u0438\u0432\u0435\u0442", "ascii.xn--b1agh1afp", "ascii.\u043F\u0440\u0438\u0432\u0435\u0442", UriHostNameType.Dns, 80, true, false };
            yield return new object[] { "http://\u043F\u0440\u0438\u0432\u0435\u0442.\u03B2\u03AD\u03BB\u03B1\u03C3\u03BC\u03B1/", "http", "", "\u043F\u0440\u0438\u0432\u0435\u0442.\u03B2\u03AD\u03BB\u03B1\u03C3\u03BC\u03B1", "xn--b1agh1afp.xn--ixaiab0ch2c", "\u043F\u0440\u0438\u0432\u0435\u0442.\u03B2\u03AD\u03BB\u03B1\u03C3\u03BC\u03B1", UriHostNameType.Dns, 80, true, false };

            yield return new object[] { "http://[1111:2222:3333::431%16]:50/", "http", "", "[1111:2222:3333::431]", "1111:2222:3333::431%16", "1111:2222:3333::431%16", UriHostNameType.IPv6, 50, false, false }; // Scope ID
            yield return new object[] { "http://[1111:2222:3333::431%16/20]", "http", "", "[1111:2222:3333::431]", "1111:2222:3333::431%16", "1111:2222:3333::431%16", UriHostNameType.IPv6, 80, true, false }; // Scope ID and prefix

            yield return new object[] { "http://\u1234\u2345\u3456/", "http", "", "\u1234\u2345\u3456", "xn--ryd258fr0m", "\u1234\u2345\u3456", UriHostNameType.Dns, 80, true, false };
        }

        [Theory]
        [MemberData(nameof(Scheme_Authority_IdnHost_TestData))]
        public void Scheme_Authority_IdnHost(string uriString, string scheme, string userInfo, string host, string idnHost, string dnsSafeHost, UriHostNameType hostNameType, int port, bool isDefaultPort, bool isLoopback)
        {
            string authority = host;
            if (!isDefaultPort)
            {
                authority += ":" + port.ToString();
            }
            PerformAction(uriString, UriKind.Absolute, uri =>
            {
                Assert.Equal(scheme, uri.Scheme);
                Assert.Equal(authority, uri.Authority);
                Assert.Equal(userInfo, uri.UserInfo);
                Assert.Equal(host, uri.Host);
                Assert.Equal(idnHost, uri.IdnHost);
                Assert.Equal(dnsSafeHost, uri.DnsSafeHost);
                Assert.Equal(hostNameType, uri.HostNameType);
                Assert.Equal(port, uri.Port);
                Assert.Equal(isDefaultPort, uri.IsDefaultPort);
                Assert.Equal(isLoopback, uri.IsLoopback);
                Assert.True(uri.IsAbsoluteUri);
                Assert.False(uri.UserEscaped);
            });
        }

        public static IEnumerable<object[]> Path_Query_Fragment_TestData()
        {
            // Http
            yield return new object[] { "http://host", "/", "", "" };
            yield return new object[] { "http://host?query", "/", "?query", "" };
            yield return new object[] { "http://host#fragment", "/", "", "#fragment" };
            yield return new object[] { "http://host?query#fragment", "/", "?query", "#fragment" };
            yield return new object[] { "http://host/PATH?QUERY#FRAGMENT", "/PATH", "?QUERY", "#FRAGMENT" };
            yield return new object[] { "http://host/", "/", "", "" };
            yield return new object[] { "http://host/path1/path2", "/path1/path2", "", "" };
            yield return new object[] { "http://host/path1/path2/", "/path1/path2/", "", "" };
            yield return new object[] { "http://host/?query", "/", "?query", "" };
            yield return new object[] { "http://host/path1/path2/?query", "/path1/path2/", "?query", "" };
            yield return new object[] { "http://host/#fragment", "/", "", "#fragment" };
            yield return new object[] { "http://host/path1/path2/#fragment", "/path1/path2/", "", "#fragment" };
            yield return new object[] { "http://host/?query#fragment", "/", "?query", "#fragment" };
            yield return new object[] { "http://host/path1/path2/?query#fragment", "/path1/path2/", "?query", "#fragment" };
            yield return new object[] { "http://host/?#fragment", "/", "?", "#fragment" };
            yield return new object[] { "http://host/path1/path2/?#fragment", "/path1/path2/", "?", "#fragment" };
            yield return new object[] { "http://host/?query#", "/", "?query", "#" };
            yield return new object[] { "http://host/path1/path2/?query#", "/path1/path2/", "?query", "#" };
            yield return new object[] { "http://host/?", "/", "?", "" };
            yield return new object[] { "http://host/path1/path2/?", "/path1/path2/", "?", "" };
            yield return new object[] { "http://host/#", "/", "", "#" };
            yield return new object[] { "http://host/path1/path2/#", "/path1/path2/", "", "#" };
            yield return new object[] { "http://host/?#", "/", "?", "#" };
            yield return new object[] { "http://host/path1/path2/?#", "/path1/path2/", "?", "#" };
            yield return new object[] { "http://host/?query1?query2#fragment1#fragment2?query3", "/", "?query1?query2", "#fragment1#fragment2?query3" };
            yield return new object[] { "http://host/?query1=value&query2", "/", "?query1=value&query2", "" };
            yield return new object[] { "http://host/?:@?/", "/", "?:@?/", "" };
            yield return new object[] { @"http://host/path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
            yield return new object[] { @"http://host/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };
            yield return new object[] { "http://host/path    \t \r \n  \x0009 \x000A \x000D", "/path", "", "" };
            yield return new object[] { "http://host/path?query    \t \r \n  \x0009 \x000A \x000D", "/path", "?query", "" };
            yield return new object[] { "http://host/path#fragment    \t \r \n  \x0009 \x000A \x000D", "/path", "", "#fragment" };
            yield return new object[] { "http://192.168.0.1:50/path1/page?query#fragment", "/path1/page", "?query", "#fragment" };
            yield return new object[] { "http://192.168.0.1:80/\u1234\u2345/\u4567\u5678?query#fragment", "/%E1%88%B4%E2%8D%85/%E4%95%A7%E5%99%B8", "?query", "#fragment" };
            yield return new object[] { "http://[1111:2222:3333::431]/path1/page?query#fragment", "/path1/page", "?query", "#fragment" };
            yield return new object[] { "http://[1111:2222:3333::431]/\u1234\u2345/\u4567\u5678?query#fragment", "/%E1%88%B4%E2%8D%85/%E4%95%A7%E5%99%B8", "?query", "#fragment" };

            // File with empty path
            yield return new object[] { "file:///", "/", "", "" };
            yield return new object[] { @"file://\", "/", "", "" };
            // File with windows drive
            yield return new object[] { "file://C:/", "C:/", "", "" };
            yield return new object[] { "file://C|/", "C:/", "", "" };
            yield return new object[] { @"file://C:\", "C:/", "", "" };
            yield return new object[] { @"file://C|\", "C:/", "", "" };
            // File with windows drive with path
            yield return new object[] { "file://C:/path1/path2", "C:/path1/path2", "", "" };
            yield return new object[] { "file://C|/path1/path2", "C:/path1/path2", "", "" };
            yield return new object[] { @"file://C:\path1/path2", "C:/path1/path2", "", "" };
            yield return new object[] { @"file://C|\path1/path2", "C:/path1/path2", "", "" };
            // File with windows drive with backlash in path
            yield return new object[] { @"file://C:/path1\path2/path3\path4", "C:/path1/path2/path3/path4", "", "" };
            yield return new object[] { @"file://C|/path1\path2/path3\path4", "C:/path1/path2/path3/path4", "", "" };
            yield return new object[] { @"file://C:\path1\path2/path3\path4", "C:/path1/path2/path3/path4", "", "" };
            yield return new object[] { @"file://C|\path1\path2/path3\path4", "C:/path1/path2/path3/path4", "", "" };
            // File with windows drive ending with backslash
            yield return new object[] { @"file://C:/path1\path2/path3\path4\", "C:/path1/path2/path3/path4/", "", "" };
            yield return new object[] { @"file://C|/path1\path2/path3\path4\", "C:/path1/path2/path3/path4/", "", "" };
            yield return new object[] { @"file://C:\path1\path2/path3\path4\", "C:/path1/path2/path3/path4/", "", "" };
            yield return new object[] { @"file://C|\path1\path2/path3\path4\", "C:/path1/path2/path3/path4/", "", "" };
            // File with host
            yield return new object[] { "file://path1/path2", "/path2", "", "" };
            yield return new object[] { "file:///path1/path2", "/path1/path2", "", "" };
            if (s_isWindowsSystem)
            {
                yield return new object[] { @"file:///path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
                yield return new object[] { @"file:///path1\path2/path3%5Cpath4\", "/path1/path2/path3/path4/", "", "" };
                yield return new object[] { @"file://localhost/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };
                yield return new object[] { @"file://localhost/path1%5Cpath2", "/path1/path2", "", ""};
            }
            else // Unix paths preserve backslash
            {
                yield return new object[] { @"file:///path1\path2/path3\path4", @"/path1%5Cpath2/path3%5Cpath4", "", "" };
                yield return new object[] { @"file:///path1%5Cpath2\path3", @"/path1%5Cpath2%5Cpath3", "", ""};
                yield return new object[] { @"file://localhost/path1\path2/path3\path4\", @"/path1%5Cpath2/path3%5Cpath4%5C", "", "" };
                yield return new object[] { @"file://localhost/path1%5Cpath2\path3", @"/path1%5Cpath2%5Cpath3", "", ""};
            }
            // Implicit file with empty path
            yield return new object[] { "C:/", "C:/", "", "" };
            yield return new object[] { "C|/", "C:/", "", "" };
            yield return new object[] { @"C:\", "C:/", "", "" };
            yield return new object[] { @"C|\", "C:/", "", "" };
            // Implicit file with path
            yield return new object[] { "C:/path1/path2", "C:/path1/path2", "", "" };
            yield return new object[] { "C|/path1/path2", "C:/path1/path2", "", "" };
            yield return new object[] { @"C:\path1/path2", "C:/path1/path2", "", "" };
            yield return new object[] { @"C|\path1/path2", "C:/path1/path2", "", "" };
            // Implicit file with backslash in path
            yield return new object[] { @"C:/path1\path2/path3\path4", "C:/path1/path2/path3/path4", "", "" };
            yield return new object[] { @"C|/path1\path2/path3\path4", "C:/path1/path2/path3/path4", "", "" };
            yield return new object[] { @"C:\path1\path2/path3\path4", "C:/path1/path2/path3/path4", "", "" };
            yield return new object[] { @"C|\path1\path2/path3\path4", "C:/path1/path2/path3/path4", "", "" };
            // Implicit file ending with backlash
            yield return new object[] { @"C:/path1\path2/path3\path4\", "C:/path1/path2/path3/path4/", "", "" };
            yield return new object[] { @"C|/path1\path2/path3\path4\", "C:/path1/path2/path3/path4/", "", "" };
            yield return new object[] { @"C:\path1\path2/path3\path4\", "C:/path1/path2/path3/path4/", "", "" };
            yield return new object[] { @"C|\path1\path2/path3\path4\", "C:/path1/path2/path3/path4/", "", "" };

            // Implicit UNC with empty path
            if (s_isWindowsSystem) // Unix UNC paths must start with '\'
            {
                yield return new object[] { "//unchost", "/", "", "" };
                yield return new object[] { @"/\unchost", "/", "", "" };
            }
            yield return new object[] { @"\\unchost", "/", "", "" };
            yield return new object[] { @"\/unchost", "/", "", "" };
            // Implicit UNC with path
            if (s_isWindowsSystem) // Unix UNC paths must start with '\'
            {
                yield return new object[] { "//unchost/path1/path2", "/path1/path2", "", "" };
                yield return new object[] { @"/\unchost/path1/path2", "/path1/path2", "", "" };
            }
            yield return new object[] { @"\\unchost/path1/path2", "/path1/path2", "", "" };
            yield return new object[] { @"\/unchost/path1/path2", "/path1/path2", "", "" };

            // Implict UNC with backslash in path
            if (s_isWindowsSystem) // Unix UNC paths must start with '\'
            {
                yield return new object[] { @"//unchost/path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
                yield return new object[] { @"/\unchost/path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
            }
            yield return new object[] { @"\\unchost/path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
            yield return new object[] { @"\/unchost/path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
            yield return new object[] { @"\\\/\/servername\sharename\path\filename", "/sharename/path/filename", "", "" };
            // Implicit UNC ending with backslash
            if (s_isWindowsSystem) // Unix UNC paths must start with '\'
            {
                yield return new object[] { @"//unchost/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };
                yield return new object[] { @"/\unchost/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };
            }
            yield return new object[] { @"\\unchost/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };
            yield return new object[] { @"\/unchost/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };
            // Explicit UNC with empty path
            yield return new object[] { @"file://\\unchost", "/", "", "" };
            yield return new object[] { "file:////unchost", "/", "", "" };
            yield return new object[] { @"file:///\unchost", "/", "", "" };
            yield return new object[] { @"file://\/unchost", "/", "", "" };
            // Explicit UNC with empty host and empty path
            yield return new object[] { @"file://\\", "//", "", "" };
            yield return new object[] { "file:////", "//", "", "" };
            yield return new object[] { @"file:///\", "//", "", "" };
            yield return new object[] { @"file://\/", "//", "", "" };
            // Explicit UNC with empty host and non-empty path
            yield return new object[] { @"file://\\/", "///", "", "" };
            yield return new object[] { "file://///", "///", "", "" };
            yield return new object[] { @"file:///\/", "///", "", "" };
            yield return new object[] { @"file://\//", "///", "", "" };
            // Explicit UNC with empty host and query
            yield return new object[] { @"file://\\?query", "//", "?query", "" };
            yield return new object[] { "file:////?query", "//", "?query", "" };
            yield return new object[] { @"file:///\?query", "//", "?query", "" };
            yield return new object[] { @"file://\/?query", "//", "?query", "" };
            yield return new object[] { "file://///?query", "///", "?query", "" };
            // Explicit UNC with empty host and fragment
            yield return new object[] { @"file://\\#fragment", "//", "", "#fragment" };
            yield return new object[] { "file:////#fragment", "//", "", "#fragment" };
            yield return new object[] { @"file:///\#fragment", "//", "", "#fragment" };
            yield return new object[] { @"file://\/#fragment", "//", "", "#fragment" };
            yield return new object[] { "file://///#fragment", "///", "", "#fragment" };
            // Explicit UNC with path
            yield return new object[] { @"file://\\unchost/path1/path2", "/path1/path2", "", "" };
            yield return new object[] { "file:////unchost/path1/path2", "/path1/path2", "", "" };
            yield return new object[] { @"file:///\unchost/path1/path2", "/path1/path2", "", "" };
            yield return new object[] { @"file://\/unchost/path1/path2", "/path1/path2", "", "" };
            // Explicit UNC with path, query and fragment
            yield return new object[] { @"file://\\unchost/path1/path2?query#fragment", "/path1/path2", "?query", "#fragment" };
            yield return new object[] { "file:////unchost/path1/path2?query#fragment", "/path1/path2", "?query", "#fragment" };
            yield return new object[] { @"file:///\unchost/path1/path2?query#fragment", "/path1/path2", "?query", "#fragment" };
            yield return new object[] { @"file://\/unchost/path1/path2?query#fragment", "/path1/path2", "?query", "#fragment" };
            // Explicit UNC with a windows drive as host
            yield return new object[] { @"file://\\C:/path1/path2", "C:/path1/path2", "", "" };
            yield return new object[] { "file:////C:/path1/path2", "C:/path1/path2", "", "" };
            yield return new object[] { @"file:///\C:/path1/path2", "C:/path1/path2", "", "" };
            yield return new object[] { @"file://\/C:/path1/path2", "C:/path1/path2", "", "" };
            // Other
            yield return new object[] { "C|/path|path/path2", "C:/path%7Cpath/path2", "", "" };
            yield return new object[] { "file://host/path?query#fragment", "/path", "?query", "#fragment" };

            if (s_isWindowsSystem)
            {
                // Explicit UNC with backslash in path
                yield return new object[] { @"file://\\unchost/path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
                yield return new object[] { @"file:////unchost/path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
                yield return new object[] { @"file:///\unchost/path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
                yield return new object[] { @"file://\/unchost/path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
                // Explicit UNC ending with backslash
                yield return new object[] { @"file://\\unchost/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };
                yield return new object[] { @"file:////unchost/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };
                yield return new object[] { @"file:///\unchost/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };
                yield return new object[] { @"file://\/unchost/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };
            }
            else
            {
                // Implicit file with path
                yield return new object[] { "/", "/", "", "" };
                yield return new object[] { "/path1/path2", "/path1/path2", "", "" };
                // Implicit file with backslash in path
                yield return new object[] { @"/path1\path2/path3\path4", "/path1%5Cpath2/path3%5Cpath4", "", "" };
                // Implicit file ending with backlash
                yield return new object[] { @"/path1\path2/path3\path4\", "/path1%5Cpath2/path3%5Cpath4%5C", "", "" };
                // Explicit UNC with backslash in path
                yield return new object[] { @"file://\\unchost/path1\path2/path3\path4", @"/path1%5Cpath2/path3%5Cpath4", "", "" };
                yield return new object[] { @"file:////unchost/path1\path2/path3\path4", @"/path1%5Cpath2/path3%5Cpath4", "", "" };
                yield return new object[] { @"file:///\unchost/path1\path2/path3\path4", @"/path1%5Cpath2/path3%5Cpath4", "", "" };
                yield return new object[] { @"file://\/unchost/path1\path2/path3\path4", @"/path1%5Cpath2/path3%5Cpath4", "", "" };
                // Explicit UNC ending with backslash
                yield return new object[] { @"file://\\unchost/path1\path2/path3\path4\", @"/path1%5Cpath2/path3%5Cpath4%5C", "", "" };
                yield return new object[] { @"file:////unchost/path1\path2/path3\path4\", @"/path1%5Cpath2/path3%5Cpath4%5C", "", "" };
                yield return new object[] { @"file:///\unchost/path1\path2/path3\path4\", @"/path1%5Cpath2/path3%5Cpath4%5C", "", "" };
                yield return new object[] { @"file://\/unchost/path1\path2/path3\path4\", @"/path1%5Cpath2/path3%5Cpath4%5C", "", "" };
            }

            // Mailto
            yield return new object[] { "mailto:someone@example.com", "", "", "" };
            yield return new object[] { "mailto:someone@example.com?query#fragment", "", "?query", "#fragment" };
            yield return new object[] { "mailto:/someone@example.com", "/someone@example.com", "", "" };
            yield return new object[] { "mailto://someone@example.com", "//someone@example.com", "", "" };
            yield return new object[] { "mailto://someone@example.com?query#fragment", "//someone@example.com", "?query", "#fragment" };

            // Ftp
            yield return new object[] { "ftp://host/#fragment", "/", "", "#fragment" };
            yield return new object[] { "ftp://host/#fragment", "/", "", "#fragment" };
            yield return new object[] { "ftp://host/?query#fragment", "/%3Fquery", "", "#fragment" };
            yield return new object[] { "ftp://userinfo@host/?query#fragment", "/%3Fquery", "", "#fragment" };
            yield return new object[] { @"ftp://host/path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
            yield return new object[] { @"ftp://host/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };

            // Telnet
            yield return new object[] { "telnet://userinfo@host/", "/", "", "" };
            yield return new object[] { "telnet://userinfo@host?query#fragment", "/%3Fquery", "", "#fragment" };
            yield return new object[] { "telnet://userinfo@host/?query#fragment", "/%3Fquery", "", "#fragment" };
            yield return new object[] { @"telnet://host/path1\path2/path3\path4", "/path1%5Cpath2/path3%5Cpath4", "", "" };
            yield return new object[] { @"telnet://host/path1\path2/path3\path4\", "/path1%5Cpath2/path3%5Cpath4%5C", "", "" };

            // Unknown
            yield return new object[] { "urn:namespace:segment1:segment2:segment3", "namespace:segment1:segment2:segment3", "", "" };
            yield return new object[] { "unknown:", "", "", "" };
            yield return new object[] { "unknown:path", "path", "", "" };
            yield return new object[] { "unknown:path1:path2", "path1:path2", "", "" };
            yield return new object[] { "unknown:path?query#fragment", "path", "?query", "#fragment" };
            yield return new object[] { "unknown:?query#fragment", "", "?query", "#fragment" };
            yield return new object[] { "unknown://./", "/", "", "" };
            yield return new object[] { "unknown://../", "/", "", "" };
            yield return new object[] { "unknown://////", "////", "", "" };
            yield return new object[] { "unknown:///C:/", "C:/", "", "" };
            yield return new object[] { "unknown://host/path?query#fragment", "/path", "?query", "#fragment" };
            yield return new object[] { @"unknown://host/path1\path2/path3\path4", "/path1/path2/path3/path4", "", "" };
            yield return new object[] { @"unknown://host/path1\path2/path3\path4\", "/path1/path2/path3/path4/", "", "" };

            // Does not need to be escaped
            yield return new object[] { "http://host/path!~+-_*()[]@:;&$=123PATH", "/path!~+-_*()[]@:;&$=123PATH", "", "" };
            yield return new object[] { "http://host/?query!~+-_*()[]@:;&$=123QUERY", "/", "?query!~+-_*()[]@:;&$=123QUERY", "" };
            yield return new object[] { "http://host/#fragment!~+-_*()[]@:;&$=123FRAGMENT", "/", "", "#fragment!~+-_*()[]@:;&$=123FRAGMENT" };
            // Unescaped
            yield return new object[] { "http://host/\u1234\u2345\u3456", "/%E1%88%B4%E2%8D%85%E3%91%96", "", "" };
            yield return new object[] { "http://host/abc\u1234\u2345\u3456", "/abc%E1%88%B4%E2%8D%85%E3%91%96", "", "" };
            yield return new object[] { "http://host/\u1234abc\u2345\u3456", "/%E1%88%B4abc%E2%8D%85%E3%91%96", "", "" };
            yield return new object[] { "http://host/\u1234\u2345\u3456abc", "/%E1%88%B4%E2%8D%85%E3%91%96abc", "", "" };
            yield return new object[] { "http://host/?abc\u1234\u2345\u3456", "/", "?abc%E1%88%B4%E2%8D%85%E3%91%96", "" };
            yield return new object[] { "http://host/?\u1234abc\u2345\u3456", "/", "?%E1%88%B4abc%E2%8D%85%E3%91%96", "" };
            yield return new object[] { "http://host/?\u1234\u2345\u3456abc", "/", "?%E1%88%B4%E2%8D%85%E3%91%96abc", "" };
            yield return new object[] { "http://host/#abc\u1234\u2345\u3456", "/", "", "#abc%E1%88%B4%E2%8D%85%E3%91%96" };
            yield return new object[] { "http://host/#\u1234abc\u2345\u3456", "/", "", "#%E1%88%B4abc%E2%8D%85%E3%91%96" };
            yield return new object[] { "http://host/#\u1234\u2345\u3456abc", "/", "", "#%E1%88%B4%E2%8D%85%E3%91%96abc" };
            yield return new object[] { "http://host/\0?\0#\0", "/%00", "?%00", "#%00" };
            // Unnecessarily escaped (upper case hex letters)
            yield return new object[] { "http://host/%68%65%6C%6C%6F", "/hello", "", "" };
            yield return new object[] { "http://host/?%68%65%6C%6C%6F", "/", "?hello", "" };
            yield return new object[] { "http://host/#%68%65%6C%6C%6F", "/", "", "#hello" };
            // Unnecessarily escaped (lower case hex letters)
            yield return new object[] { "http://host/%68%65%6c%6c%6f", "/hello", "", "" };
            yield return new object[] { "http://host/?%68%65%6c%6c%6f", "/", "?hello", "" };
            yield return new object[] { "http://host/#%68%65%6c%6c%6f", "/", "", "#hello" };
            // Encoded generic delimeters should not be expanded
            yield return new object[] { "http://host/%3A?%3A#%3A", "/%3A", "?%3A", "#%3A" };
            yield return new object[] { "http://host/%2F?%2F#%2F", "/%2F", "?%2F", "#%2F" };
            yield return new object[] { "http://host/%3F?%3F#%3F", "/%3F", "?%3F", "#%3F" };
            yield return new object[] { "http://host/%23?%23#%23", "/%23", "?%23", "#%23" };
            yield return new object[] { "http://host/%5B?%5B#%5B", "/%5B", "?%5B", "#%5B" };
            yield return new object[] { "http://host/%5D?%5D#%5D", "/%5D", "?%5D", "#%5D" };
            yield return new object[] { "http://host/%40?%40#%40", "/%40", "?%40", "#%40" };
            // Encoded sub delimeters should not be expanded
            yield return new object[] { "http://host/%21?%21#%21", "/%21", "?%21", "#%21" };
            yield return new object[] { "http://host/%24?%24#%24", "/%24", "?%24", "#%24" };
            yield return new object[] { "http://host/%26?%26#%26", "/%26", "?%26", "#%26" };
            yield return new object[] { "http://host/%5C?%5C#%5C", "/%5C", "?%5C", "#%5C" };
            yield return new object[] { "http://host/%28?%28#%28", "/%28", "?%28", "#%28" };
            yield return new object[] { "http://host/%29?%29#%29", "/%29", "?%29", "#%29" };
            yield return new object[] { "http://host/%2A?%2A#%2A", "/%2A", "?%2A", "#%2A" };
            yield return new object[] { "http://host/%2B?%2B#%2B", "/%2B", "?%2B", "#%2B" };
            yield return new object[] { "http://host/%2C?%2C#%2C", "/%2C", "?%2C", "#%2C" };
            yield return new object[] { "http://host/%3B?%3B#%3B", "/%3B", "?%3B", "#%3B" };
            yield return new object[] { "http://host/%3D?%3D#%3D", "/%3D", "?%3D", "#%3D" };
            // Invalid unicode
            yield return new object[] { "http://host/%?%#%", "/%25", "?%25", "#%25" };
            yield return new object[] { "http://host/%3?%3#%3", "/%253", "?%253", "#%253" };
            yield return new object[] { "http://host/%G?%G#%G", "/%25G", "?%25G", "#%25G" };
            yield return new object[] { "http://host/%g?%g#%g", "/%25g", "?%25g", "#%25g" };
            yield return new object[] { "http://host/%G3?%G3#%G3", "/%25G3", "?%25G3", "#%25G3" };
            yield return new object[] { "http://host/%g3?%g3#%g3", "/%25g3", "?%25g3", "#%25g3" };
            yield return new object[] { "http://host/%3G?%3G#%3G", "/%253G", "?%253G", "#%253G" };
            yield return new object[] { "http://host/%3g?%3g#%3g", "/%253g", "?%253g", "#%253g" };

            // Compressed
            yield return new object[] { "http://host/%2E%2E/%2E%2E", "/", "", "" };
            yield return new object[] { "http://host/path1/../path2", "/path2", "", "" };
            yield return new object[] { "http://host/../", "/", "", "" };
            yield return new object[] { "http://host/path1/./path2", "/path1/path2", "", "" };
            yield return new object[] { "http://host/./", "/", "", "" };
            yield return new object[] { "http://host/..", "/", "", "" };
            yield return new object[] { "http://host/.../", "/.../", "", "" };
            yield return new object[] { "http://host/x../", "/x../", "", "" };
            yield return new object[] { "http://host/..x/", "/..x/", "", "" };
            yield return new object[] { "http://host/path//", "/path//", "", "" };
            yield return new object[] { "file://C:/abc/def/../ghi", "C:/abc/ghi", "", "" };
        }

        [Theory]
        [MemberData(nameof(Path_Query_Fragment_TestData))]
        public void Path_Query_Fragment(string uriString, string path, string query, string fragment)
        {
            IEnumerable<string> segments = null;
            string localPath = null;
            string segmentsPath = null;
            PerformAction(uriString, UriKind.Absolute, uri =>
            {
                if (segments == null)
                {
                    localPath = Uri.UnescapeDataString(path);
                    segmentsPath = path;
                    if (uri.IsUnc)
                    {
                        localPath = @"\\" + uri.Host + path;
                        localPath = localPath.Replace('/', '\\');
                        // Unescape '\\'
                        localPath = localPath.Replace("%5C", "\\");
                        if (path == "/")
                        {
                            localPath = localPath.Substring(0, localPath.Length - 1);
                        }
                    }
                    else if (path.Length > 2 && path[1] == ':' && path[2] == '/')
                    {
                        segmentsPath = '/' + segmentsPath;
                        localPath = localPath.Replace('/', '\\');
                    }
                    segments = Regex.Split(segmentsPath, @"(?<=/)").TakeWhile(s => s.Length != 0);
                }

                Assert.Equal(path, uri.AbsolutePath);
                Assert.Equal(localPath, uri.LocalPath);
                Assert.Equal(path + query, uri.PathAndQuery);
                Assert.Equal(segments, uri.Segments);
                Assert.Equal(query, uri.Query);
                Assert.Equal(fragment, uri.Fragment);
                Assert.True(uri.IsAbsoluteUri);
                Assert.False(uri.UserEscaped);
            });
        }

        public static IEnumerable<object[]> IsFile_IsUnc_TestData()
        { // Explicit file with windows drive with path
            yield return new object[] { "file://C:/path", true, false };
            yield return new object[] { "file://C|/path", true, false };
            yield return new object[] { @"file://C:\path", true, false };
            yield return new object[] { @"file://C|\path", true, false };
            yield return new object[] { "file:///C:/path", true, false };
            yield return new object[] { "file:///C|/path", true, false };
            yield return new object[] { @"file:///C:\path", true, false };
            yield return new object[] { @"file:///C|\path", true, false };

            // File with empty path
            yield return new object[] { "file:///", true, false };
            yield return new object[] { @"file://\", true, false };

            // File with host
            yield return new object[] { "file://host/path2", true, true };

            // Implicit file with windows drive with empty path
            yield return new object[] { "C:/", true, false };
            yield return new object[] { "C|/", true, false };
            yield return new object[] { @"C:\", true, false };
            yield return new object[] { @"C|/", true, false };

            // Implicit file with windows drive with path
            yield return new object[] { "C:/path", true, false };
            yield return new object[] { "C|/path", true, false };
            yield return new object[] { @"C:\path", true, false };
            yield return new object[] { @"C|\path", true, false };
            yield return new object[] { @"\\unchost", true, true };

            // Implicit UNC with empty path
            if (s_isWindowsSystem) // Unc can only start with '/' on Windows
            {
                yield return new object[] { "//unchost", true, true };
                yield return new object[] { @"/\unchost", true, true };
            }
            yield return new object[] { @"\\unchost", true, true };
            yield return new object[] { @"\/unchost", true, true };

            // Implicit UNC with path
            if (s_isWindowsSystem) // Unc can only start with '/' on Windows
            {
                yield return new object[] { "//unchost/path1/path2", true, true };
                yield return new object[] { @"/\unchost/path1/path2", true, true };
            }
            yield return new object[] { @"\\unchost/path1/path2", true, true };
            yield return new object[] { @"\/unchost/path1/path2", true, true };

            // Explicit UNC with empty path
            yield return new object[] { @"file://\\unchost", true, true };
            yield return new object[] { "file:////unchost", true, true };
            yield return new object[] { @"file:///\unchost", true, true };
            yield return new object[] { @"file://\/unchost", true, true };

            // Explicit UNC with empty host and empty path
            yield return new object[] { @"file://\\", true, false };
            yield return new object[] { "file:////", true, false };
            yield return new object[] { @"file:///\", true, false };
            yield return new object[] { @"file://\/", true, false };

            // Explicit UNC with empty host and non empty path
            yield return new object[] { @"file://\\/", true, false };
            yield return new object[] { "file://///", true, false };
            yield return new object[] { @"file:///\/", true, false };
            yield return new object[] { @"file://\//", true, false };

            // Explicit UNC with query
            yield return new object[] { @"file://\\?query", true, false };
            yield return new object[] { "file:////?query", true, false };
            yield return new object[] { @"file:///\?query", true, false };
            yield return new object[] { @"file://\/?query", true, false };

            // Explicit UNC with fragment
            yield return new object[] { @"file://\\#fragment", true, false };
            yield return new object[] { "file:////#fragment", true, false };
            yield return new object[] { @"file:///\#fragment", true, false };
            yield return new object[] { @"file://\/#fragment", true, false };

            // Explicit UNC with path
            yield return new object[] { @"file://\\unchost/path1/path2", true, true };
            yield return new object[] { "file:////unchost/path1/path2", true, true };
            yield return new object[] { @"file:///\unchost/path1/path2", true, true };
            yield return new object[] { @"file://\/unchost/path1/path2", true, true };

            // Explicit UNC with windows drive
            yield return new object[] { @"file://\\C:/", true, false };
            yield return new object[] { "file:////C:/", true, false };
            yield return new object[] { @"file:///\C:/", true, false };
            yield return new object[] { @"file://\/C:/", true, false };
            yield return new object[] { @"file://\\C|/", true, false };
            yield return new object[] { "file:////C|/", true, false };
            yield return new object[] { @"file:///\C|/", true, false };
            yield return new object[] { @"file://\/C|/", true, false };
            yield return new object[] { @"file://\\C:\", true, false };
            yield return new object[] { @"file:////C:\", true, false };
            yield return new object[] { @"file:///\C:\", true, false };
            yield return new object[] { @"file://\/C:\", true, false };
            yield return new object[] { @"file://\\C|\", true, false };
            yield return new object[] { @"file:////C|\", true, false };
            yield return new object[] { @"file:///\C|\", true, false };
            yield return new object[] { @"file://\/C|\", true, false };

            // Not a file
            yield return new object[] { "http://host/", false, false };
            yield return new object[] { "https://host/", false, false };
            yield return new object[] { "mailto:someone@example.com", false, false };
            yield return new object[] { "ftp://host/", false, false };
            yield return new object[] { "telnet://host/", false, false };
            yield return new object[] { "unknown:", false, false };
            yield return new object[] { "unknown:path", false, false };
            yield return new object[] { "unknown://host/", false, false };
        }

        [Theory]
        [MemberData(nameof(IsFile_IsUnc_TestData))]
        public void IsFile_IsUnc(string uriString, bool isFile, bool isUnc)
        {
            PerformAction(uriString, UriKind.Absolute, uri =>
            {
                Assert.Equal(isFile, uri.IsFile);
                Assert.Equal(isUnc, uri.IsUnc);
            });
        }

        public static IEnumerable<object[]> Relative_TestData()
        {
            yield return new object[] { "path1/page.htm?query1=value#fragment", true };
            yield return new object[] { "/", true };
            yield return new object[] { "?query", true };
            yield return new object[] { "#fragment", true };
            yield return new object[] { @"C:\abc", false };
            yield return new object[] { @"C|\abc", false };
            yield return new object[] { @"\\servername\sharename\path\filename", false };
        }

        [Theory]
        [MemberData(nameof(Relative_TestData))]
        public void Relative(string uriString, bool relativeOrAbsolute)
        {
            PerformAction(uriString, UriKind.Relative, uri =>
            {
                VerifyRelativeUri(uri, uriString, uriString);
            });
            PerformAction(uriString, UriKind.RelativeOrAbsolute, uri =>
            {
                if (relativeOrAbsolute)
                {
                    VerifyRelativeUri(uri, uriString, uriString);
                }
                else
                {
                    Assert.True(uri.IsAbsoluteUri);
                }
            });
        }

        [Fact]
        public void Create_String_Null_Throws_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("uriString", () => new Uri(null));
            AssertExtensions.Throws<ArgumentNullException>("uriString", () => new Uri(null, UriKind.Absolute));

            Uri uri;
            Assert.False(Uri.TryCreate(null, UriKind.Absolute, out uri));
            Assert.Null(uri);
        }

        [Fact]
        public void Create_String_InvalidUriKind_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Uri("http://host", UriKind.RelativeOrAbsolute - 1));
            AssertExtensions.Throws<ArgumentException>(null, () => new Uri("http://host", UriKind.Relative + 1));

            Uri uri = null;
            AssertExtensions.Throws<ArgumentException>(null, () => Uri.TryCreate("http://host", UriKind.RelativeOrAbsolute - 1, out uri));
            Assert.Null(uri);
            AssertExtensions.Throws<ArgumentException>(null, () => Uri.TryCreate("http://host", UriKind.Relative + 1, out uri));
            Assert.Null(uri);
        }

        public static IEnumerable<object[]> Create_String_Invalid_TestData()
        {
            yield return new object[] { s_longString, UriKind.Absolute }; // UriString is longer than 66520 characters

            // Invalid scheme
            yield return new object[] { "", UriKind.Absolute };
            yield return new object[] { "  \t \r \n  \x0009 \x000A \x000D ", UriKind.Absolute };
            yield return new object[] { "http", UriKind.Absolute };
            yield return new object[] { ":", UriKind.Absolute };
            yield return new object[] { "1http://host/", UriKind.Absolute };
            yield return new object[] { "http/://host/", UriKind.Absolute };
            yield return new object[] { "\u1234http://host/", UriKind.Absolute };
            yield return new object[] { "ht\u1234tp://host/", UriKind.Absolute };
            yield return new object[] { "ht%45tp://host/", UriKind.Absolute };
            yield return new object[] { "\x00a0 \x000B  \x000C \x0085http", UriKind.Absolute };
            yield return new object[] { "~", UriKind.Absolute };
            yield return new object[] { "http://", UriKind.Absolute };
            yield return new object[] { "http:/", UriKind.Absolute };
            yield return new object[] { "domain.com", UriKind.Absolute };
            yield return new object[] { "\u1234http://domain.com", UriKind.Absolute };
            yield return new object[] { "http\u1234://domain.com", UriKind.Absolute };
            yield return new object[] { "http~://domain.com", UriKind.Absolute };
            yield return new object[] { "http#://domain.com", UriKind.Absolute };
            yield return new object[] { new string('a', 1025) + "://domain.com", UriKind.Absolute }; // Scheme is longer than 1024 characters

            // Invalid userinfo
            yield return new object[] { @"http://use\rinfo@host", UriKind.Absolute };

            // Invalid characters in host
            yield return new object[] { "http://ho!st/", UriKind.Absolute };
            yield return new object[] { "http://ho&st/", UriKind.Absolute };
            yield return new object[] { "http://ho$st/", UriKind.Absolute };
            yield return new object[] { "http://ho(st/", UriKind.Absolute };
            yield return new object[] { "http://ho)st/", UriKind.Absolute };
            yield return new object[] { "http://ho*st", UriKind.Absolute };
            yield return new object[] { "http://ho+st", UriKind.Absolute };
            yield return new object[] { "http://ho,st", UriKind.Absolute };
            yield return new object[] { "http://ho;st/", UriKind.Absolute };
            yield return new object[] { "http://ho=st", UriKind.Absolute };
            yield return new object[] { "http://ho~st/", UriKind.Absolute };

            // Empty host
            yield return new object[] { "http://", UriKind.Absolute };
            yield return new object[] { "http:/", UriKind.Absolute };
            yield return new object[] { "http:/abc", UriKind.Absolute };
            yield return new object[] { "http://@", UriKind.Absolute };
            yield return new object[] { "http://userinfo@", UriKind.Absolute };
            yield return new object[] { "http://:", UriKind.Absolute };
            yield return new object[] { "http://:80", UriKind.Absolute };
            yield return new object[] { "http://@:", UriKind.Absolute };
            yield return new object[] { "http://@:80", UriKind.Absolute };
            yield return new object[] { "http://userinfo@:80", UriKind.Absolute };
            yield return new object[] { "http:///", UriKind.Absolute };
            yield return new object[] { "http://@/", UriKind.Absolute };
            yield return new object[] { "http://userinfo@/", UriKind.Absolute };
            yield return new object[] { "http://:/", UriKind.Absolute };
            yield return new object[] { "http://:80/", UriKind.Absolute };
            yield return new object[] { "http://@:/", UriKind.Absolute };
            yield return new object[] { "http://@:80/", UriKind.Absolute };
            yield return new object[] { "http://userinfo@:80/", UriKind.Absolute };
            yield return new object[] { "http://?query", UriKind.Absolute };
            yield return new object[] { "http://:?query", UriKind.Absolute };
            yield return new object[] { "http://@:?query", UriKind.Absolute };
            yield return new object[] { "http://userinfo@:?query", UriKind.Absolute };
            yield return new object[] { "http://#fragment", UriKind.Absolute };
            yield return new object[] { "http://:#fragment", UriKind.Absolute };
            yield return new object[] { "http://@:#fragment", UriKind.Absolute };
            yield return new object[] { "http://userinfo@:#fragment", UriKind.Absolute };
            yield return new object[] { @"http://host\", UriKind.Absolute };
            yield return new object[] { @"http://userinfo@host@host/", UriKind.Absolute };
            yield return new object[] { @"http://userinfo\@host/", UriKind.Absolute };
            yield return new object[] { "http://ho\0st/", UriKind.Absolute };
            yield return new object[] { "http://ho[st/", UriKind.Absolute };
            yield return new object[] { "http://ho]st/", UriKind.Absolute };
            yield return new object[] { @"http://ho\st/", UriKind.Absolute };
            yield return new object[] { "http://ho{st/", UriKind.Absolute };
            yield return new object[] { "http://ho}st/", UriKind.Absolute };

            // Invalid host
            yield return new object[] { @"http://domain\", UriKind.Absolute };
            yield return new object[] { @"unknownscheme://domain\", UriKind.Absolute };

            yield return new object[] { "unknown://h..9", UriKind.Absolute };
            yield return new object[] { "unknown://h..-", UriKind.Absolute };
            yield return new object[] { "unknown://h..", UriKind.Absolute };
            yield return new object[] { "unknown://h.a;./", UriKind.Absolute };

            // Invalid file
            yield return new object[] { "file:/a", UriKind.Absolute };
            yield return new object[] { "C:adomain.com", UriKind.Absolute };
            yield return new object[] { "C|adomain.com", UriKind.Absolute };
            yield return new object[] { "!://domain.com", UriKind.Absolute };
            yield return new object[] { "!|//domain.com", UriKind.Absolute };
            yield return new object[] { "\u1234://domain.com", UriKind.Absolute };
            yield return new object[] { "\u1234|//domain.com", UriKind.Absolute };
            yield return new object[] { ".://domain.com", UriKind.Absolute };

            // File is not rooted
            yield return new object[] { "file://a:a", UriKind.Absolute };
            yield return new object[] { "file://a:", UriKind.Absolute };

            // Implicit UNC has an empty host
            yield return new object[] { @"\\", UriKind.Absolute };
            yield return new object[] { @"\\?query", UriKind.Absolute };
            yield return new object[] { @"\\#fragment", UriKind.Absolute };
            yield return new object[] { "\\\\?query\u1234", UriKind.Absolute };
            yield return new object[] { "\\\\#fragment\u1234", UriKind.Absolute };

            // Implicit UNC has port
            yield return new object[] { @"\\unchost:90", UriKind.Absolute };
            yield return new object[] { @"\\unchost:90/path1/path2", UriKind.Absolute };

            // Explicit UNC has port
            yield return new object[] { @"file://\\unchost:90", UriKind.Absolute };
            yield return new object[] { @"file://\\unchost:90/path1/path2", UriKind.Absolute };

            // File with host has port
            yield return new object[] { @"file://host:90", UriKind.Absolute };
            yield return new object[] { @"file://host:90/path1/path2", UriKind.Absolute };

            // Implicit UNC has userinfo
            yield return new object[] { @"\\userinfo@host", UriKind.Absolute };
            yield return new object[] { @"\\userinfo@host/path1/path2", UriKind.Absolute };

            // Explicit UNC has userinfo
            yield return new object[] { @"file://\\userinfo@host", UriKind.Absolute };
            yield return new object[] { @"file://\\userinfo@host/path1/path2", UriKind.Absolute };

            // File with host has userinfo
            yield return new object[] { @"file://userinfo@host", UriKind.Absolute };
            yield return new object[] { @"file://userinfo@host/path1/path2", UriKind.Absolute };

            // Implicit UNC with windows drive
            yield return new object[] { @"\\C:/", UriKind.Absolute };
            yield return new object[] { @"\\C|/", UriKind.Absolute };
            if (s_isWindowsSystem) // Valid Unix path
            {
                yield return new object[] { "//C:/", UriKind.Absolute };
                yield return new object[] { "//C|/", UriKind.Absolute };
            }
            yield return new object[] { @"\/C:/", UriKind.Absolute };
            yield return new object[] { @"\/C|/", UriKind.Absolute };
            if (s_isWindowsSystem) // Valid Unix path
            {
                yield return new object[] { @"/\C:/", UriKind.Absolute };
                yield return new object[] { @"/\C|/", UriKind.Absolute };
            }

            // Explicit UNC with invalid windows drive
            yield return new object[] { @"file://\\1:/", UriKind.Absolute };
            yield return new object[] { @"file://\\ab:/", UriKind.Absolute };

            // Unc host is invalid
            yield return new object[] { @"\\.", UriKind.Absolute };
            yield return new object[] { @"\\server..", UriKind.Absolute };

            // Domain name host is invalid
            yield return new object[] { "http://./", UriKind.Absolute };
            yield return new object[] { "http://_a..a/", UriKind.Absolute };
            yield return new object[] { "http://a..a/", UriKind.Absolute };
            yield return new object[] { "unknownscheme://..a/", UriKind.Absolute };
            yield return new object[] { "http://host" + (char)0, UriKind.Absolute };
            yield return new object[] { "http://\u043F\u0440\u0438\u0432\u0435\u0442" + (char)0, UriKind.Absolute };
            yield return new object[] { "http://%", UriKind.Absolute };
            yield return new object[] { "http://@", UriKind.Absolute };

            // Invalid IPv4 address
            yield return new object[] { "http://192..0.1", UriKind.Absolute };
            yield return new object[] { "http://192.0.0.1;", UriKind.Absolute };

            // Invalid IPv6 address
            yield return new object[] { "http://[", UriKind.Absolute };
            yield return new object[] { "http://[?", UriKind.Absolute };
            yield return new object[] { "http://[#", UriKind.Absolute };
            yield return new object[] { "http://[/", UriKind.Absolute };
            yield return new object[] { @"http://[\", UriKind.Absolute };
            yield return new object[] { "http://[]", UriKind.Absolute };
            yield return new object[] { "http://[a]", UriKind.Absolute };
            yield return new object[] { "http://[1111:2222:3333::431", UriKind.Absolute };
            yield return new object[] { "http://[1111:2222:3333::431%", UriKind.Absolute };
            yield return new object[] { "http://[::1::1]", UriKind.Absolute };
            yield return new object[] { "http://[11111:2222:3333::431]", UriKind.Absolute };
            yield return new object[] { "http://[/12]", UriKind.Absolute };
            yield return new object[] { "http://[1111:2222:3333::431/12/12]", UriKind.Absolute };
            yield return new object[] { "http://[1111:2222:3333::431%16/]", UriKind.Absolute };
            yield return new object[] { "http://[1111:2222:3333::431/123]", UriKind.Absolute };

            yield return new object[] { "http://[192.168.0.9/192.168.0.9]", UriKind.Absolute };
            yield return new object[] { "http://[192.168.0.9%192.168.0.9]", UriKind.Absolute };
            yield return new object[] { "http://[001.168.0.9]", UriKind.Absolute };
            yield return new object[] { "http://[a92.168.0.9]", UriKind.Absolute };
            yield return new object[] { "http://[192.168.0]", UriKind.Absolute };
            yield return new object[] { "http://[256.168.0.9]", UriKind.Absolute };
            yield return new object[] { "http://[01.168.0.9]", UriKind.Absolute };

            // Invalid port
            yield return new object[] { "http://domain:a", UriKind.Absolute };
            yield return new object[] { "http://domain:-1", UriKind.Absolute };
            yield return new object[] { "http://domain:65536", UriKind.Absolute };
            yield return new object[] { "http://host:2147483648", UriKind.Absolute };
            yield return new object[] { "http://host:80:80", UriKind.Absolute };
            yield return new object[] { "uri://domain:a", UriKind.Absolute };
            yield return new object[] { "uri://domain:65536", UriKind.Absolute };
            yield return new object[] { "uri://a:a", UriKind.Absolute };
            yield return new object[] { "uri://a:65536", UriKind.Absolute };
            yield return new object[] { "uri://a:2147483648", UriKind.Absolute };
            yield return new object[] { "uri://a:80:80", UriKind.Absolute };

            if (PlatformDetection.IsNotInvariantGlobalization)
            {
                // Invalid unicode
                yield return new object[] { "http://\uD800", UriKind.Absolute };
                yield return new object[] { "http://\uDC00", UriKind.Absolute };
            }
        }

        [Theory]
        [MemberData(nameof(Create_String_Invalid_TestData))]
        public void Create_String_Invalid(string uriString, UriKind uriKind)
        {
            if (uriKind == UriKind.Absolute)
            {
                Assert.Throws<UriFormatException>(() => new Uri(uriString));
            }
            Assert.Throws<UriFormatException>(() => new Uri(uriString, uriKind));

            Uri uri;
            Assert.False(Uri.TryCreate(uriString, uriKind, out uri));
            Assert.Null(uri);
        }

        private static void PerformAction(string uriString, UriKind uriKind, Action<Uri> action)
        {
            if (uriKind == UriKind.Absolute)
            {
                Uri uri = new Uri(uriString);
                action(uri);
            }

            Uri uri1 = new Uri(uriString, uriKind);
            action(uri1);

            Uri result = null;
            Assert.True(Uri.TryCreate(uriString, uriKind, out result));
            action(result);
        }

        internal static void VerifyRelativeUri(Uri uri, string originalString, string toString)
        {
            Assert.Equal(originalString, uri.OriginalString);
            Assert.Equal(toString, uri.ToString());
            Assert.False(uri.IsAbsoluteUri);
            Assert.False(uri.UserEscaped);
            Assert.Throws<InvalidOperationException>(() => uri.AbsoluteUri);
            Assert.Throws<InvalidOperationException>(() => uri.Scheme);
            Assert.Throws<InvalidOperationException>(() => uri.HostNameType);
            Assert.Throws<InvalidOperationException>(() => uri.Authority);
            Assert.Throws<InvalidOperationException>(() => uri.Host);
            Assert.Throws<InvalidOperationException>(() => uri.IdnHost);
            Assert.Throws<InvalidOperationException>(() => uri.DnsSafeHost);
            Assert.Throws<InvalidOperationException>(() => uri.Port);
            Assert.Throws<InvalidOperationException>(() => uri.AbsolutePath);
            Assert.Throws<InvalidOperationException>(() => uri.LocalPath);
            Assert.Throws<InvalidOperationException>(() => uri.PathAndQuery);
            Assert.Throws<InvalidOperationException>(() => uri.Segments);
            Assert.Throws<InvalidOperationException>(() => uri.Fragment);
            Assert.Throws<InvalidOperationException>(() => uri.Query);
            Assert.Throws<InvalidOperationException>(() => uri.UserInfo);
            Assert.Throws<InvalidOperationException>(() => uri.IsDefaultPort);
            Assert.Throws<InvalidOperationException>(() => uri.IsFile);
            Assert.Throws<InvalidOperationException>(() => uri.IsLoopback);
            Assert.Throws<InvalidOperationException>(() => uri.IsUnc);
        }
    }
}
