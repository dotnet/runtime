// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// Author:
//  Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Xunit;

namespace System.PrivateUri.Tests
{
    #region Test class
    public sealed class TestUriParser : UriParser
    {
        private bool on_new_uri_called;
        private int default_Port;
        private string scheme_name;
        public TestUriParser() : base() { }
        public new string GetComponents(Uri uri, UriComponents components, UriFormat format) => base.GetComponents(uri, components, format);
        public new void InitializeAndValidate(Uri uri, out UriFormatException parsingError) => base.InitializeAndValidate(uri, out parsingError);
        public new bool IsBaseOf(Uri baseUri, Uri relativeUri) => base.IsBaseOf(baseUri, relativeUri);
        public new bool IsWellFormedOriginalString(Uri uri) => base.IsWellFormedOriginalString(uri);
        public new string Resolve(Uri baseUri, Uri relativeUri, out UriFormatException parsingError) => base.Resolve(baseUri, relativeUri, out parsingError);
        public new UriParser OnNewUri()
        {
            on_new_uri_called = true;
            return base.OnNewUri();
        }
        public bool OnNewUriCalled
        {
            get
            {
                return on_new_uri_called;
            }
        }
        protected override void OnRegister(string schemeName, int defaultPort)
        {
            scheme_name = schemeName;
            default_Port = defaultPort;
            base.OnRegister(schemeName, defaultPort);
        }
        public string SchemeName
        {
            get
            {
                return scheme_name;
            }
        }
        public int DefaultPort
        {
            get
            {
                return default_Port;
            }
        }

    }
    #endregion Test class

    public static class UriParserTest
    {
        #region UriParser tests

        private const string full_http = "http://www.mono-project.com/Main_Page#FAQ?Edit";
        private static string prefix;
        private static Uri http, ftp, ftp2;

        [Fact]
        public static void GetComponents_test()
        {
            http = new Uri(full_http);
            TestUriParser parser = new TestUriParser();
            Assert.Equal("http", parser.GetComponents(http, UriComponents.Scheme, UriFormat.SafeUnescaped));
            Assert.Equal(string.Empty, parser.GetComponents(http, UriComponents.UserInfo, UriFormat.SafeUnescaped));
            Assert.Equal("www.mono-project.com", parser.GetComponents(http, UriComponents.Host, UriFormat.SafeUnescaped));
            Assert.Equal(string.Empty, parser.GetComponents(http, UriComponents.Port, UriFormat.SafeUnescaped));
            Assert.Equal("Main_Page", parser.GetComponents(http, UriComponents.Path, UriFormat.SafeUnescaped));
            Assert.Equal(string.Empty, parser.GetComponents(http, UriComponents.Query, UriFormat.SafeUnescaped));
            Assert.Equal("FAQ?Edit", parser.GetComponents(http, UriComponents.Fragment, UriFormat.SafeUnescaped));
            Assert.Equal("80", parser.GetComponents(http, UriComponents.StrongPort, UriFormat.SafeUnescaped));
            Assert.Equal(string.Empty, parser.GetComponents(http, UriComponents.KeepDelimiter, UriFormat.SafeUnescaped));
            Assert.Equal("www.mono-project.com:80", parser.GetComponents(http, UriComponents.HostAndPort, UriFormat.SafeUnescaped));
            Assert.Equal("www.mono-project.com:80", parser.GetComponents(http, UriComponents.StrongAuthority, UriFormat.SafeUnescaped));
            Assert.Equal(full_http, parser.GetComponents(http, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped));
            Assert.Equal("/Main_Page", parser.GetComponents(http, UriComponents.PathAndQuery, UriFormat.SafeUnescaped));
            Assert.Equal("http://www.mono-project.com/Main_Page", parser.GetComponents(http, UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped));
            Assert.Equal("http://www.mono-project.com", parser.GetComponents(http, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
            Assert.Equal(full_http, parser.GetComponents(http, UriComponents.SerializationInfoString, UriFormat.SafeUnescaped));
            // strange mixup
            Assert.Equal("http://", parser.GetComponents(http, UriComponents.Scheme | UriComponents.Port, UriFormat.SafeUnescaped));
            Assert.Equal("www.mono-project.com#FAQ?Edit", parser.GetComponents(http, UriComponents.Host | UriComponents.Fragment, UriFormat.SafeUnescaped));
            Assert.Equal("/Main_Page", parser.GetComponents(http, UriComponents.Port | UriComponents.Path, UriFormat.SafeUnescaped));
            Assert.Equal(parser, parser.OnNewUri());
        }

        [Fact]
        public static void GetComponents_Ftp()
        {
            var full_ftp = "ftp://username:password@ftp.go-mono.com:21/with some spaces/mono.tgz";
            ftp = new Uri(full_ftp);
            TestUriParser parser = new TestUriParser();
            Assert.Equal("ftp", parser.GetComponents(ftp, UriComponents.Scheme, UriFormat.Unescaped));
            Assert.Equal("username:password", parser.GetComponents(ftp, UriComponents.UserInfo, UriFormat.Unescaped));
            Assert.Equal("ftp.go-mono.com", parser.GetComponents(ftp, UriComponents.Host, UriFormat.Unescaped));
            Assert.Equal(string.Empty, parser.GetComponents(ftp, UriComponents.Port, UriFormat.Unescaped));
            Assert.Equal("with some spaces/mono.tgz", parser.GetComponents(ftp, UriComponents.Path, UriFormat.Unescaped));
            Assert.Equal("with%20some%20spaces/mono.tgz", parser.GetComponents(ftp, UriComponents.Path, UriFormat.UriEscaped));
            Assert.Equal("with some spaces/mono.tgz", parser.GetComponents(ftp, UriComponents.Path, UriFormat.SafeUnescaped));
            Assert.Equal(string.Empty, parser.GetComponents(ftp, UriComponents.Query, UriFormat.Unescaped));
            Assert.Equal(string.Empty, parser.GetComponents(ftp, UriComponents.Fragment, UriFormat.Unescaped));
            Assert.Equal("21", parser.GetComponents(ftp, UriComponents.StrongPort, UriFormat.Unescaped));
            Assert.Equal(string.Empty, parser.GetComponents(ftp, UriComponents.KeepDelimiter, UriFormat.Unescaped));
            Assert.Equal("ftp.go-mono.com:21", parser.GetComponents(ftp, UriComponents.HostAndPort, UriFormat.Unescaped));
            Assert.Equal("username:password@ftp.go-mono.com:21", parser.GetComponents(ftp, UriComponents.StrongAuthority, UriFormat.Unescaped));
            Assert.Equal("ftp://username:password@ftp.go-mono.com/with some spaces/mono.tgz", parser.GetComponents(ftp, UriComponents.AbsoluteUri, UriFormat.Unescaped));
            Assert.Equal("/with some spaces/mono.tgz", parser.GetComponents(ftp, UriComponents.PathAndQuery, UriFormat.Unescaped));
            Assert.Equal("ftp://ftp.go-mono.com/with some spaces/mono.tgz", parser.GetComponents(ftp, UriComponents.HttpRequestUrl, UriFormat.Unescaped));
            Assert.Equal("ftp://ftp.go-mono.com", parser.GetComponents(ftp, UriComponents.SchemeAndServer, UriFormat.Unescaped));
            Assert.Equal("ftp://username:password@ftp.go-mono.com/with some spaces/mono.tgz", parser.GetComponents(ftp, UriComponents.SerializationInfoString, UriFormat.Unescaped));
            Assert.Equal(parser, parser.OnNewUri());
            // strange mixup
            Assert.Equal("ftp://username:password@", parser.GetComponents(ftp, UriComponents.Scheme | UriComponents.UserInfo, UriFormat.Unescaped));
            Assert.Equal(":21/with some spaces/mono.tgz", parser.GetComponents(ftp, UriComponents.Path | UriComponents.StrongPort, UriFormat.Unescaped));
        }

        [Fact]
        public static void GetComponents_Ftp2()
        {
            var full_ftp = "ftp://%75sername%3a%70assword@ftp.go-mono.com:21/with some spaces/mono.tgz";
            ftp2 = new Uri(full_ftp);
            TestUriParser parser = new TestUriParser();
            Assert.Equal("ftp", parser.GetComponents(ftp2, UriComponents.Scheme, UriFormat.Unescaped));
            Assert.Equal("username:password", parser.GetComponents(ftp2, UriComponents.UserInfo, UriFormat.Unescaped));
            Assert.Equal("ftp.go-mono.com", parser.GetComponents(ftp2, UriComponents.Host, UriFormat.Unescaped));
            Assert.Equal(string.Empty, parser.GetComponents(ftp2, UriComponents.Port, UriFormat.Unescaped));
            Assert.Equal("with some spaces/mono.tgz", parser.GetComponents(ftp2, UriComponents.Path, UriFormat.Unescaped));
            Assert.Equal("with%20some%20spaces/mono.tgz", parser.GetComponents(ftp2, UriComponents.Path, UriFormat.UriEscaped));
            Assert.Equal("with some spaces/mono.tgz", parser.GetComponents(ftp2, UriComponents.Path, UriFormat.SafeUnescaped));
            Assert.Equal(string.Empty, parser.GetComponents(ftp2, UriComponents.Query, UriFormat.Unescaped));
            Assert.Equal(string.Empty, parser.GetComponents(ftp2, UriComponents.Fragment, UriFormat.Unescaped));
            Assert.Equal("21", parser.GetComponents(ftp2, UriComponents.StrongPort, UriFormat.Unescaped));
            Assert.Equal(string.Empty, parser.GetComponents(ftp2, UriComponents.KeepDelimiter, UriFormat.Unescaped));
            Assert.Equal("ftp.go-mono.com:21", parser.GetComponents(ftp2, UriComponents.HostAndPort, UriFormat.Unescaped));
            Assert.Equal("username:password@ftp.go-mono.com:21", parser.GetComponents(ftp2, UriComponents.StrongAuthority, UriFormat.Unescaped));
            Assert.Equal("ftp://username:password@ftp.go-mono.com/with some spaces/mono.tgz", parser.GetComponents(ftp2, UriComponents.AbsoluteUri, UriFormat.Unescaped));
            Assert.Equal("/with some spaces/mono.tgz", parser.GetComponents(ftp2, UriComponents.PathAndQuery, UriFormat.Unescaped));
            Assert.Equal("ftp://ftp.go-mono.com/with some spaces/mono.tgz", parser.GetComponents(ftp2, UriComponents.HttpRequestUrl, UriFormat.Unescaped));
            Assert.Equal("ftp://ftp.go-mono.com", parser.GetComponents(ftp2, UriComponents.SchemeAndServer, UriFormat.Unescaped));
            Assert.Equal("ftp://username:password@ftp.go-mono.com/with some spaces/mono.tgz", parser.GetComponents(ftp2, UriComponents.SerializationInfoString, UriFormat.Unescaped));
            Assert.Equal(parser, parser.OnNewUri());
            // strange mixup
            Assert.Equal("ftp://username:password@", parser.GetComponents(ftp2, UriComponents.Scheme | UriComponents.UserInfo, UriFormat.Unescaped));
            Assert.Equal(":21/with some spaces/mono.tgz", parser.GetComponents(ftp2, UriComponents.Path | UriComponents.StrongPort, UriFormat.Unescaped));
        }
        
        [Fact]
        public static void TestParseUserPath()
        {
            var u = new Uri("https://a.net/1@1.msg");
            var result = u.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.UriEscaped);
            Assert.Equal(result, "https://a.net/1@1.msg");
        }


        [Fact]
        public static void GetComponents_Null()
        {
            TestUriParser parser = new TestUriParser();
            Assert.Throws<System.NullReferenceException>(() => { parser.GetComponents(null, UriComponents.Host, UriFormat.SafeUnescaped); });
        }

        [Fact]
        public static void GetComponents_BadUriComponents()
        {
            http = new Uri(full_http);
            TestUriParser parser = new TestUriParser();
            Assert.Equal(full_http, parser.GetComponents(http, (UriComponents)int.MinValue, UriFormat.SafeUnescaped));
        }

        [Fact]
        public static void GetComponents_BadUriFormat()
        {
            http = new Uri(full_http);
            TestUriParser parser = new TestUriParser();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => { parser.GetComponents(http, UriComponents.Host, (UriFormat)int.MinValue); });
        }

        [Fact]
        public static void InitializeAndValidate()
        {
            http = new Uri(full_http);
            UriFormatException error = null;
            TestUriParser parser = new TestUriParser();
            parser.InitializeAndValidate(http, out error);
            Assert.NotNull(error);
        }
        
        [Fact]
        public static void InitializeAndValidate_Null()
        {
            UriFormatException error = null;
            TestUriParser parser = new TestUriParser();
            Assert.Throws<System.NullReferenceException>(() => { parser.InitializeAndValidate(null, out error); }); 
        }
        
        [Fact]
        public static void IsBaseOf()
        {
            http = new Uri(full_http);
            TestUriParser parser = new TestUriParser();
            Assert.True(parser.IsBaseOf(http, http), "http-http");

            Uri u = new Uri("http://www.mono-project.com/Main_Page#FAQ");
            Assert.True(parser.IsBaseOf(u, http), "http-1a");
            Assert.True(parser.IsBaseOf(http, u), "http-1b");

            u = new Uri("http://www.mono-project.com/Main_Page");
            Assert.True(parser.IsBaseOf(u, http), "http-2a");
            Assert.True(parser.IsBaseOf(http, u), "http-2b");

            u = new Uri("http://www.mono-project.com/");
            Assert.True(parser.IsBaseOf(u, http), "http-3a");
            Assert.True(parser.IsBaseOf(http, u), "http-3b");

            u = new Uri("http://www.mono-project.com/Main_Page/");
            Assert.False(parser.IsBaseOf(u, http), "http-4a");
            Assert.True(parser.IsBaseOf(http, u), "http-4b");

            // docs says the UserInfo isn't evaluated, but...
            u = new Uri("http://username:password@www.mono-project.com/Main_Page");
            Assert.False(parser.IsBaseOf(u, http), "http-5a");
            Assert.False(parser.IsBaseOf(http, u), "http-5b");

            // scheme case sensitive ? no
            u = new Uri("HTTP://www.mono-project.com/Main_Page");
            Assert.True(parser.IsBaseOf(u, http), "http-6a");
            Assert.True(parser.IsBaseOf(http, u), "http-6b");

            // host case sensitive ? no
            u = new Uri("http://www.Mono-Project.com/Main_Page");
            Assert.True(parser.IsBaseOf(u, http), "http-7a");
            Assert.True(parser.IsBaseOf(http, u), "http-7b");

            // path case sensitive ? no
            u = new Uri("http://www.Mono-Project.com/MAIN_Page");
            Assert.True(parser.IsBaseOf(u, http), "http-8a");
            Assert.True(parser.IsBaseOf(http, u), "http-8b");

            // different scheme
            u = new Uri("ftp://www.mono-project.com/Main_Page");
            Assert.False(parser.IsBaseOf(u, http), "http-9a");
            Assert.False(parser.IsBaseOf(http, u), "http-9b");

            // different host
            u = new Uri("http://www.go-mono.com/Main_Page");
            Assert.False(parser.IsBaseOf(u, http), "http-10a");
            Assert.False(parser.IsBaseOf(http, u), "http-10b");

            // different port
            u = new Uri("http://www.mono-project.com:8080/");
            Assert.False(parser.IsBaseOf(u, http), "http-11a");
            Assert.False(parser.IsBaseOf(http, u), "http-11b");

            // specify default port
            u = new Uri("http://www.mono-project.com:80/");
            Assert.True(parser.IsBaseOf(u, http), "http-12a");
            Assert.True(parser.IsBaseOf(http, u), "http-12b");
        }

        [Fact]
        public static void IsWellFormedOriginalString()
        {
            http = new Uri(full_http);
            TestUriParser parser = new TestUriParser();
            Assert.True(parser.IsWellFormedOriginalString(http), "http");
        }
        
        [Fact]
        public static void IsWellFormedOriginalString_Null()
        {
            TestUriParser parser = new TestUriParser();
            Assert.Throws<System.NullReferenceException>(() => { parser.IsWellFormedOriginalString(null); } );
        }
        
        [Fact]
        public static void OnRegister()
        {
            prefix = "unit.test.";
            string scheme = prefix + "onregister";
            Assert.False(UriParser.IsKnownScheme(scheme), "IsKnownScheme-false");
            TestUriParser parser = new TestUriParser();
            try
            {
                UriParser.Register(parser, scheme, 2005);
            }
            catch (NotSupportedException)
            {
                // special case / ordering
            }
            // if true then the registration is done before calling OnRegister
            Assert.True(UriParser.IsKnownScheme(scheme), "IsKnownScheme-true");
        }
        
        [Fact]
        public static void OnRegister2()
        {
            prefix = "unit.test.";
            string scheme = prefix + "onregister2";
            Assert.False(UriParser.IsKnownScheme(scheme), "IsKnownScheme-false");
            TestUriParser parser = new TestUriParser();
            try
            {
                UriParser.Register(parser, scheme, 2005);
                Uri uri = new Uri(scheme + "://foobar:2005");
                Assert.Equal(scheme, uri.Scheme);
                Assert.Equal(2005, uri.Port);
                Assert.Equal("//foobar:2005", uri.LocalPath);
            }
            catch (NotSupportedException)
            {
                // special case / ordering
            }
            // if true then the registration is done before calling OnRegister
            Assert.True(UriParser.IsKnownScheme(scheme), "IsKnownScheme-true");
        }
        
        [Fact]
        public static void Resolve()
        {
            http = new Uri(full_http);
            UriFormatException error = null;
            TestUriParser parser = new TestUriParser();
            Assert.Equal(full_http, parser.Resolve(http, http, out error));
        }
        
        [Fact]
        public static void Resolve_UriNull()
        {
            http = new Uri(full_http);
            UriFormatException error = null;
            TestUriParser parser = new TestUriParser();
            Assert.Equal(full_http, parser.Resolve(http, null, out error));
        }
        
        [Fact]
        public static void Resolve_NullUri()
        {
            http = new Uri(full_http);
            UriFormatException error = null;
            TestUriParser parser = new TestUriParser();
            Assert.Throws<System.NullReferenceException>(() => { parser.Resolve(null, http, out error); } );
            parser.Resolve(http, null, out error);
        }
        
        [Fact]
        public static void IsKnownScheme_WellKnown()
        {
            Assert.True(UriParser.IsKnownScheme("file"), "file");
            Assert.True(UriParser.IsKnownScheme("ftp"), "ftp");
            Assert.True(UriParser.IsKnownScheme("gopher"), "gopher");
            Assert.True(UriParser.IsKnownScheme("http"), "http");
            Assert.True(UriParser.IsKnownScheme("https"), "https");
            Assert.True(UriParser.IsKnownScheme("mailto"), "mailto");
            Assert.True(UriParser.IsKnownScheme("net.pipe"), "net.pipe");
            Assert.True(UriParser.IsKnownScheme("net.tcp"), "net.tcp");
            Assert.True(UriParser.IsKnownScheme("news"), "news");
            Assert.True(UriParser.IsKnownScheme("nntp"), "nntp");
            Assert.True(UriParser.IsKnownScheme("ldap"), "ldap");
            Assert.False(UriParser.IsKnownScheme("ldaps"), "ldaps");
            Assert.False(UriParser.IsKnownScheme("unknown"), "unknown");
            Assert.True(UriParser.IsKnownScheme("FiLe"), "FiLe");
            Assert.True(UriParser.IsKnownScheme("FTP"), "ftp");
            Assert.False(UriParser.IsKnownScheme("tcp"), "tcp");
        }
        
        [Fact]
        public static void IsKnownScheme_ExtraSpace()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => { UriParser.IsKnownScheme("ht tp"); });
        }
        
        [Fact]
        public static void IsKnownScheme_Null()
        {
            Assert.Throws<System.ArgumentNullException>(() => { UriParser.IsKnownScheme(null); });
        }
        
        [Fact]
        public static void IsKnownScheme_Empty()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => { UriParser.IsKnownScheme(string.Empty); });
        }
        
        [Fact]
        public static void Register()
        {
            prefix = "unit.test.";
            string scheme = prefix + "register.mono";
            Assert.False(UriParser.IsKnownScheme(scheme), "IsKnownScheme-false");

            TestUriParser parser = new TestUriParser();
            UriParser.Register(parser, scheme, 2005);
            Assert.Equal(scheme, parser.SchemeName);
            Assert.Equal(2005, parser.DefaultPort);

            Assert.True(UriParser.IsKnownScheme(scheme), "IsKnownScheme-true");
        }

        [Fact]
        public static void Register_NullParser()
        {
            prefix = "unit.test.";
            Assert.Throws<System.ArgumentNullException>(() => { UriParser.Register(null, prefix + "null.parser", 2006); } );
        }
        
        [Fact]
        public static void Register_NullScheme()
        {
            TestUriParser parser = new TestUriParser();
            Assert.Throws<System.ArgumentNullException>(() => { UriParser.Register(parser, null, 2006); });
        }

        [Fact]
        public static void Register_NegativePort()
        {
            prefix = "unit.test.";
            TestUriParser parser = new TestUriParser();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => { UriParser.Register(parser, prefix + "negative.port", -2); });
        }

        [Fact]
        public static void Register_Minus1Port()
        {
            prefix = "unit.test.";
            TestUriParser parser = new TestUriParser();
            UriParser.Register(parser, prefix + "minus1.port", -1);
        }

        [Fact]
        public static void Register_UInt16PortMinus1()
        {
            prefix = "unit.test.";
            TestUriParser parser = new TestUriParser();
            UriParser.Register(parser, prefix + "uint16.minus.1.port", ushort.MaxValue - 1);
        }
        
        [Fact]
        public static void Register_TooBigPort()
        {
            prefix = "unit.test.";
            TestUriParser parser = new TestUriParser();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => { UriParser.Register(parser, prefix + "too.big.port", ushort.MaxValue); });
        }

        [Fact]
        public static void ReRegister()
        {
            prefix = "unit.test.";
            string scheme = prefix + "re.register.mono";
            Assert.False(UriParser.IsKnownScheme(scheme), "IsKnownScheme-false");
            TestUriParser parser = new TestUriParser();
            UriParser.Register(parser, scheme, 2005);
            Assert.True(UriParser.IsKnownScheme(scheme), "IsKnownScheme-true");
            Assert.Throws<System.InvalidOperationException>(() => { UriParser.Register(parser, scheme, 2006); });
        }

        #endregion UriParser tests

        #region GenericUriParser tests
        [Fact]
        public static void GenericUriParser_ctor()
        {
            // Make sure that constructor doesn't throw when using different types of  Parser Options
            GenericUriParser parser; 
            
            parser = new GenericUriParser(GenericUriParserOptions.AllowEmptyAuthority);
            parser = new GenericUriParser(GenericUriParserOptions.Default);
            parser = new GenericUriParser(GenericUriParserOptions.DontCompressPath);
            parser = new GenericUriParser(GenericUriParserOptions.DontConvertPathBackslashes);
            parser = new GenericUriParser(GenericUriParserOptions.DontUnescapePathDotsAndSlashes);
            parser = new GenericUriParser(GenericUriParserOptions.GenericAuthority);
            parser = new GenericUriParser(GenericUriParserOptions.Idn);
            parser = new GenericUriParser(GenericUriParserOptions.IriParsing);
            parser = new GenericUriParser(GenericUriParserOptions.NoFragment);
            parser = new GenericUriParser(GenericUriParserOptions.NoPort);
            parser = new GenericUriParser(GenericUriParserOptions.NoQuery);
            parser = new GenericUriParser(GenericUriParserOptions.NoUserInfo);
        }
        #endregion GenericUriParser tests

        #region UriParser template tests
        //Make sure the constructor of the UriParser templates are callable

        [Fact]
        public static void HttpStyleUriParser_ctor()
        {
            HttpStyleUriParser httpParser = new HttpStyleUriParser();
        }

        [Fact]
        public static void FtpStyleUriParser_ctor()
        {
            FtpStyleUriParser httpParser = new FtpStyleUriParser();
        }
        [Fact]
        public static void FileStyleUriParser_ctor()
        {
            FileStyleUriParser httpParser = new FileStyleUriParser();
        }
        [Fact]
        public static void NewsStyleUriParser_ctor()
        {
            NewsStyleUriParser httpParser = new NewsStyleUriParser();
        }
        [Fact]
        public static void GopherStyleUriParser_ctor()
        {
            GopherStyleUriParser httpParser = new GopherStyleUriParser();
        }
        [Fact]
        public static void LdapStyleUriParser_ctor()
        {
            LdapStyleUriParser httpParser = new LdapStyleUriParser();
        }
        [Fact]
        public static void NetPipeStyleUriParser_ctor()
        {
            NetPipeStyleUriParser httpParser = new NetPipeStyleUriParser();
        }
        [Fact]
        public static void NetTcpStyleUriParser_ctor()
        {
            NetTcpStyleUriParser httpParser = new NetTcpStyleUriParser();
        }
        #endregion UriParser template tests
    }
}
