// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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

            int i;
            string s;
            bool b;
            UriHostNameType uriHostNameType;
            string[] ss;

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
            Assert.False(b);

            s = uri.UserInfo;
            Assert.Equal<String>(s, @"");
        }

        [Fact]
        public static void TestCtor_Uri_String()
        {
            Uri uri;

            uri = new Uri(@"http://www.contoso.com/");
            uri = new Uri(uri, "catalog/shownew.htm?date=today");

            int i;
            string s;
            bool b;
            UriHostNameType uriHostNameType;
            string[] ss;

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
            Assert.False(b);

            s = uri.UserInfo;
            Assert.Equal<String>(s, @"");
        }

        [Fact]
        public static void TestCtor_String_UriKind()
        {
            Uri uri = new Uri("catalog/shownew.htm?date=today", UriKind.Relative);

            string s;
            bool b;

            s = uri.ToString();
            Assert.Equal(s, @"catalog/shownew.htm?date=today");

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.AbsolutePath; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.AbsoluteUri; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Authority; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.DnsSafeHost; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Fragment; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Host; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.HostNameType; });

            Assert.False(uri.IsAbsoluteUri);

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.IsDefaultPort; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.IsFile; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.IsLoopback; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.IsUnc; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.LocalPath; });

            s = uri.OriginalString;
            Assert.Equal<String>(s, @"catalog/shownew.htm?date=today");

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.PathAndQuery; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Port; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Query; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Scheme; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Segments; });

            b = uri.UserEscaped;
            Assert.False(b);

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.UserInfo; });
        }

        [Fact]
        public static void TestCtor_Uri_Uri()
        {
            Uri absoluteUri = new Uri("http://www.contoso.com/");

            // Create a relative Uri from a string.  allowRelative = true to allow for 
            // creating a relative Uri.
            Uri relativeUri = new Uri("/catalog/shownew.htm?date=today", UriKind.Relative);

            // Create a new Uri from an absolute Uri and a relative Uri.
            Uri uri = new Uri(absoluteUri, relativeUri);

            int i;
            string s;
            bool b;
            UriHostNameType uriHostNameType;
            string[] ss;

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
            Assert.False(b);

            s = uri.UserInfo;
            Assert.Equal<String>(s, @"");
        }

        [Fact]
        public static void TestTryCreate_String_UriKind()
        {
            Uri uri;
            bool b = Uri.TryCreate("http://www.contoso.com/catalog/shownew.htm?date=today", UriKind.Absolute, out uri);
            Assert.True(b);

            int i;
            string s;
            UriHostNameType uriHostNameType;
            string[] ss;

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
            Assert.False(b);

            s = uri.UserInfo;
            Assert.Equal<String>(s, @"");
        }

        [Fact]
        public static void TestTryCreate_Uri_String()
        {
            Uri uri;
            Uri baseUri = new Uri("http://www.contoso.com/", UriKind.Absolute);
            bool b = Uri.TryCreate(baseUri, "catalog/shownew.htm?date=today", out uri);
            Assert.True(b);

            int i;
            string s;
            UriHostNameType uriHostNameType;
            string[] ss;

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
            Assert.False(b);

            s = uri.UserInfo;
            Assert.Equal<String>(s, @"");
        }

        [Fact]
        public static void TestTryCreate_Uri_Uri()
        {
            Uri uri;
            Uri baseUri = new Uri("http://www.contoso.com/", UriKind.Absolute);
            Uri relativeUri = new Uri("catalog/shownew.htm?date=today", UriKind.Relative);
            bool b = Uri.TryCreate(baseUri, relativeUri, out uri);
            Assert.True(b);

            int i;
            string s;
            UriHostNameType uriHostNameType;
            string[] ss;

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
            Assert.False(b);

            s = uri.UserInfo;
            Assert.Equal<String>(s, @"");
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

            string s;
            bool b;

            s = uri.ToString();
            Assert.Equal(s, @"index.htm?date=today");

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.AbsolutePath; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.AbsoluteUri; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Authority; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.DnsSafeHost; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Fragment; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Host; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.HostNameType; });

            b = uri.IsAbsoluteUri;
            Assert.False(b);

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.IsDefaultPort; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.IsFile; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.IsLoopback; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.IsUnc; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.LocalPath; });

            s = uri.OriginalString;
            Assert.Equal<String>(s, @"index.htm?date=today");

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.PathAndQuery; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Port; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Query; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Scheme; });

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.Segments; });

            b = uri.UserEscaped;
            Assert.False(b);

            Assert.Throws<System.InvalidOperationException>(() => { object o = uri.UserInfo; });
        }

        [Fact]
        public static void TestCheckHostName()
        {
            UriHostNameType u;

            u = Uri.CheckHostName("www.contoso.com");
            Assert.Equal(u, UriHostNameType.Dns);
            u = Uri.CheckHostName("1.2.3.4");
            Assert.Equal(u, UriHostNameType.IPv4);
            u = Uri.CheckHostName(null);
            Assert.Equal(u, UriHostNameType.Unknown);
            u = Uri.CheckHostName("!@*(@#&*#$&*#");
            Assert.Equal(u, UriHostNameType.Unknown);
        }

        [Fact]
        public static void TestCheckSchemeName()
        {
            bool b;

            b = Uri.CheckSchemeName("http");
            Assert.True(b);

            b = Uri.CheckSchemeName(null);
            Assert.False(b);

            b = Uri.CheckSchemeName("!");
            Assert.False(b);
        }

        [Fact]
        public static void TestIsBaseOf()
        {
            bool b;
            Uri uri, uri2;

            uri = new Uri("http://host/path/path/file?query");

            uri2 = new Uri(@"http://host/path/path/file/");
            b = uri.IsBaseOf(uri2);
            Assert.True(b);

            uri2 = new Uri(@"http://host/path/path/#fragment");
            b = uri.IsBaseOf(uri2);
            Assert.True(b);

            uri2 = new Uri("http://host/path/path/MoreDir/\"");
            b = uri.IsBaseOf(uri2);
            Assert.True(b);

            uri2 = new Uri(@"http://host/path/path/OtherFile?Query");
            b = uri.IsBaseOf(uri2);
            Assert.True(b);

            uri2 = new Uri(@"http://host/path/path/");
            b = uri.IsBaseOf(uri2);
            Assert.True(b);

            uri2 = new Uri(@"http://host/path/path/file");
            b = uri.IsBaseOf(uri2);
            Assert.True(b);

            uri2 = new Uri(@"http://host/path/path");
            b = uri.IsBaseOf(uri2);
            Assert.False(b);

            uri2 = new Uri(@"http://host/path/path?query");
            b = uri.IsBaseOf(uri2);
            Assert.False(b);

            uri2 = new Uri(@"http://host/path/path#Fragment");
            b = uri.IsBaseOf(uri2);
            Assert.False(b);

            uri2 = new Uri(@"http://host/path/path2/");
            b = uri.IsBaseOf(uri2);
            Assert.False(b);

            uri2 = new Uri(@"http://host/path/path2/MoreDir");
            b = uri.IsBaseOf(uri2);
            Assert.False(b);

            uri2 = new Uri(@"http://host/path/File");
            b = uri.IsBaseOf(uri2);
            Assert.False(b);
        }

        [Fact]
        public static void TestIsWellFormedOriginalString()
        {
            Uri uri;
            bool b;

            uri = new Uri("http://www.contoso.com/path?name");
            b = uri.IsWellFormedOriginalString();
            Assert.True(b);

            uri = new Uri("http://www.contoso.com/path???/file name");
            b = uri.IsWellFormedOriginalString();
            Assert.False(b);

            uri = new Uri(@"c:\\directory\filename");
            b = uri.IsWellFormedOriginalString();
            Assert.False(b);

            uri = new Uri(@"file://c:/directory/filename");
            b = uri.IsWellFormedOriginalString();
            Assert.False(b);

            uri = new Uri(@"http:\\host/path/file");
            b = uri.IsWellFormedOriginalString();
            Assert.False(b);
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
            Assert.Equal(i, 0);

            i = Uri.Compare(uri1, uri2, UriComponents.Query | UriComponents.Fragment, UriFormat.UriEscaped, StringComparison.CurrentCulture);
            Assert.Equal(i, -1);

            bool b;

            b = uri1.Equals(uri2);
            Assert.False(b);

            b = uri1 == uri2;
            Assert.False(b);

            b = uri1 != uri2;
            Assert.True(b);

            b = uri2.Equals(uri2a);
            Assert.True(b);

            b = uri2 == uri2a;
            Assert.True(b);

            b = uri2 != uri2a;
            Assert.False(b);

            int h2 = uri2.GetHashCode();
            int h2a = uri2a.GetHashCode();
            Assert.Equal(h2, h2a);
        }

        [Fact]
        public static void TestEscapeDataString()
        {
            string s;

            s = Uri.EscapeDataString("Hello");
            Assert.Equal(s, "Hello");

            s = Uri.EscapeDataString(@"He\l/lo");
            Assert.Equal(s, "He%5Cl%2Flo");
        }

        [Fact]
        public static void TestUnescapeDataString()
        {
            string s;

            s = Uri.UnescapeDataString("Hello");
            Assert.Equal(s, "Hello");

            s = Uri.UnescapeDataString("He%5Cl%2Flo");
            Assert.Equal(s, @"He\l/lo");
        }

        [Fact]
        public static void TestEscapeUriString()
        {
            string s;

            s = Uri.EscapeUriString("Hello");
            Assert.Equal(s, "Hello");

            s = Uri.EscapeUriString(@"He\l/lo");
            Assert.Equal(s, @"He%5Cl/lo");
        }

        [Fact]
        public static void TestGetComponentParts()
        {
            Uri uri = new Uri("http://www.contoso.com/path?name#frag");
            string s;

            s = uri.GetComponents(UriComponents.Fragment, UriFormat.UriEscaped);
            Assert.Equal(s, "frag");

            s = uri.GetComponents(UriComponents.Host, UriFormat.UriEscaped);
            Assert.Equal(s, "www.contoso.com");
        }

        [Fact]
        public static void TestCasingWhenCombiningAbsoluteAndRelativeUris()
        {
            Uri u = new Uri(new Uri("http://example.com/", UriKind.Absolute), new Uri("C(B:G", UriKind.Relative));
            Assert.Equal("http://example.com/C(B:G", u.ToString());
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "This test depends on a fix that has not yet made it to .NET Framework.")]
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void Uri_PortTrailingSpaces_SpacesTrimmed(string portString)
        {
            Uri u = new Uri($"http://www.contoso.com:{portString}     ");

            int port = Int32.Parse(portString);
            Assert.Equal($"http://www.contoso.com:{port}/", u.AbsoluteUri);
            Assert.Equal(port, u.Port);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void Uri_EmptyPortTrailingSpaces_UsesDefaultPortSpacesTrimmed()
        {
            Uri u = new Uri($"http://www.contoso.com:     ");

            Assert.Equal($"http://www.contoso.com/", u.AbsoluteUri);
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
            Assert.Throws<UriFormatException>( () =>
            {
                Uri u = new Uri($"http://www.contoso.com:{portString}");
            });
        }

        [Fact]
        public static void Uri_EmptyPort_UsesDefaultPort()
        {
            Uri u = new Uri($"http://www.contoso.com:");

            Assert.Equal($"http://www.contoso.com/", u.AbsoluteUri);
            Assert.Equal(80, u.Port);
        }
    }
}
