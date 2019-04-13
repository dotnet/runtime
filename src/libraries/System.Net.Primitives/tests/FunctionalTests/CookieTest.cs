// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class CookieTest
    {
        [Fact]
        public static void Ctor_Empty_Success()
        {
            Cookie c = new Cookie();
        }

        [Fact]
        public static void Ctor_NameValue_Success()
        {
            Cookie c = new Cookie("hello", "goodbye");
            Assert.Equal("hello", c.Name);
            Assert.Equal("goodbye", c.Value);
        }

        [Fact]
        public static void Ctor_NameValuePath_Success()
        {
            Cookie c = new Cookie("hello", "goodbye", "foo");
            Assert.Equal("hello", c.Name);
            Assert.Equal("goodbye", c.Value);
            Assert.Equal("foo", c.Path);
        }

        [Fact]
        public static void Ctor_NameValuePathDomain_Success()
        {
            Cookie c = new Cookie("hello", "goodbye", "foo", "bar");
            Assert.Equal("hello", c.Name);
            Assert.Equal("goodbye", c.Value);
            Assert.Equal("foo", c.Path);
            Assert.Equal("bar", c.Domain);
        }

        [Fact]
        public static void CookieException_Ctor_Success()
        {
            CookieException c = new CookieException();
        }

        [Fact]
        public static void Comment_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.Equal(string.Empty, c.Comment);

            c.Comment = "hello";
            Assert.Equal("hello", c.Comment);

            c.Comment = null;
            Assert.Equal(string.Empty, c.Comment);
        }

        [Fact]
        public static void CommentUri_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.Null(c.CommentUri);

            c.CommentUri = new Uri("http://hello.com");
            Assert.Equal(new Uri("http://hello.com"), c.CommentUri);
        }

        [Fact]
        public static void HttpOnly_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.False(c.HttpOnly);

            c.HttpOnly = true;
            Assert.True(c.HttpOnly);

            c.HttpOnly = false;
            Assert.False(c.HttpOnly);
        }

        [Fact]
        public static void Discard_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.False(c.Discard);

            c.Discard = true;
            Assert.True(c.Discard);

            c.Discard = false;
            Assert.False(c.Discard);
        }

        [Fact]
        public static void Domain_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.Equal(string.Empty, c.Domain);

            c.Domain = "hello";
            Assert.Equal("hello", c.Domain);

            c.Domain = null;
            Assert.Equal(string.Empty, c.Domain);
        }

        [Fact]
        public static void Expired_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.False(c.Expired);
            
            c.Expires = DateTime.Now.AddDays(-1);
            Assert.True(c.Expired);

            c.Expires = DateTime.Now.AddDays(1);
            Assert.False(c.Expired);

            c.Expired = true;
            Assert.True(c.Expired);

            c.Expired = true;
            Assert.True(c.Expired);
        }

        [Fact]
        public static void Expires_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.Equal(c.Expires, DateTime.MinValue);

            DateTime dt = DateTime.Now;
            c.Expires = dt;
            Assert.Equal(dt, c.Expires);
        }

        [Fact]
        public static void Name_GetSet_Success()
        {
            Cookie c1 = new Cookie();
            Assert.Equal(string.Empty, c1.Name);

            c1.Name = "hello";
            Assert.Equal("hello", c1.Name);

            if (!PlatformDetection.IsFullFramework)
            {
                Cookie c2 = new Cookie();
                c2.Name = "hello world";
                Assert.Equal("hello world", c2.Name);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("$hello")]
        [InlineData("hello ")]
        [InlineData(" hello")]
        [InlineData("hello\t")]
        [InlineData("hello\r")]
        [InlineData("hello\n")]
        [InlineData("hello=")]
        [InlineData("hello;")]
        [InlineData("hello,")]
        public static void Name_Set_Invalid(string name)
        {
            Cookie c = new Cookie();
            Assert.Throws<CookieException>(() => c.Name = name);
        }

        [Fact]
        public static void Path_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.Equal(string.Empty, c.Path);

            c.Path = "path";
            Assert.Equal("path", c.Path);

            c.Path = null;
            Assert.Equal(string.Empty, c.Path);
        }
        
        [Fact]
        public static void Port_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.Equal(string.Empty, c.Port);

            c.Port = "\"80 110, 1050, 1090 ,110\"";
            Assert.Equal("\"80 110, 1050, 1090 ,110\"", c.Port);

            c.Port = null;
            Assert.Equal(string.Empty, c.Port);
        }

        [Theory]
        [InlineData("80, 80 \"")] //No leading quotation mark
        [InlineData("\"80, 80")] //No trailing quotation mark
        [InlineData("\"80, hello\"")] //Invalid port
        [InlineData("\"-1, hello\"")] //Port out of range, < 0
        [InlineData("\"80, 65536\"")] //Port out of range, > 65535
        public static void Port_Set_Invalid(string port)
        {
            Cookie c = new Cookie();
            Assert.Throws<CookieException>(() => c.Port = port);
        }

        [Fact]
        public static void Secure_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.False(c.Secure);

            c.Secure = true;
            Assert.True(c.Secure);

            c.Secure = false;
            Assert.False(c.Secure);
        }

        [Fact]
        public static void Timestamp_GetSet_Success()
        {
            DateTime dt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0); //DateTime.Now changes as the test runs
            Cookie c = new Cookie();
            Assert.True(c.TimeStamp >= dt);
        }

        [Fact]
        public static void Value_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.Equal(string.Empty, c.Value);

            c.Value = "hello";
            Assert.Equal("hello", c.Value);

            c.Value = null;
            Assert.Equal(string.Empty, c.Value);
        }
        
        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)] // Cookie.Value returns null on full framework, empty string on .NETCore
        public static void Value_PassNullToCtor_GetReturnsEmptyString_net46()
        {
            var cookie = new Cookie("SomeName", null);
            // Cookie.Value returns null on full framework.
            Assert.Null(cookie.Value);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // Cookie.Value returns null on full framework, empty string on .NETCore
        public static void Value_PassNullToCtor_GetReturnsEmptyString()
        {
            var cookie = new Cookie("SomeName", null);
            // Cookie.Value returns empty string on netcore.
            Assert.Equal(string.Empty, cookie.Value);
        }

        [Fact]
        public static void Version_GetSet_Success()
        {
            Cookie c = new Cookie();
            Assert.Equal(0, c.Version);

            c.Version = 100;
            Assert.Equal(100, c.Version);
        }

        [Fact]
        public static void Version_Set_Invalid()
        {
            Cookie c = new Cookie();
            Assert.Throws<ArgumentOutOfRangeException>(() => c.Version = -1);
        }

        [Fact]
        public static void Equals_Compare_Success()
        {
            Cookie c1 = new Cookie();

            Cookie c2 = new Cookie("name", "value");
            Cookie c3 = new Cookie("Name", "value");
            Cookie c4 = new Cookie("different-name", "value");
            Cookie c5 = new Cookie("name", "different-value");

            Cookie c6 = new Cookie("name", "value", "path");
            Cookie c7 = new Cookie("name", "value", "path");
            Cookie c8 = new Cookie("name", "value", "different-path");

            Cookie c9 = new Cookie("name", "value", "path", "domain");
            Cookie c10 = new Cookie("name", "value", "path", "domain");
            Cookie c11 = new Cookie("name", "value", "path", "Domain");
            Cookie c12 = new Cookie("name", "value", "path", "different-domain");

            Cookie c13 = new Cookie("name", "value", "path", "domain") { Version = 5 };
            Cookie c14 = new Cookie("name", "value", "path", "domain") { Version = 5 };
            Cookie c15 = new Cookie("name", "value", "path", "domain") { Version = 100 };

            Assert.False(c2.Equals(null));
            Assert.False(c2.Equals(""));

            Assert.NotEqual(c1, c2);

            Assert.Equal(c2, c3);
            Assert.Equal(c3, c2);
            Assert.NotEqual(c2, c4);
            Assert.NotEqual(c2, c5);
            Assert.NotEqual(c2, c6);
            Assert.NotEqual(c2, c9);
            
            Assert.NotEqual(c2.GetHashCode(), c4.GetHashCode());
            Assert.NotEqual(c2.GetHashCode(), c5.GetHashCode());
            Assert.NotEqual(c2.GetHashCode(), c6.GetHashCode());
            Assert.NotEqual(c2.GetHashCode(), c9.GetHashCode());

            Assert.Equal(c6, c7);
            Assert.NotEqual(c6, c8);
            Assert.NotEqual(c6, c9);
            
            Assert.NotEqual(c6.GetHashCode(), c8.GetHashCode());
            Assert.NotEqual(c6.GetHashCode(), c9.GetHashCode());

            Assert.Equal(c9, c10);
            Assert.Equal(c9, c11);
            Assert.NotEqual(c9, c12);

            Assert.Equal(c9.GetHashCode(), c10.GetHashCode());
            Assert.NotEqual(c9.GetHashCode(), c12.GetHashCode());

            Assert.Equal(c13, c14);
            Assert.NotEqual(c13, c15);
            Assert.Equal(c13.GetHashCode(), c14.GetHashCode());
            Assert.NotEqual(c13.GetHashCode(), c15.GetHashCode());
        }

        [Fact]
        public static void ToString_Compare_Success()
        {
            Cookie c = new Cookie();
            Assert.Equal(string.Empty, c.ToString());

            c = new Cookie("name", "value");
            Assert.Equal("name=value", c.ToString());

            c = new Cookie("name", "value", "path", "domain");
            c.Port = "\"80\"";
            c.Version = 100;
            Assert.Equal("$Version=100; name=value; $Path=path; $Domain=domain; $Port=\"80\"", c.ToString());

            c.Version = 0;
            Assert.Equal("name=value; $Path=path; $Domain=domain; $Port=\"80\"", c.ToString());

            c.Port = "";
            Assert.Equal("name=value; $Path=path; $Domain=domain; $Port", c.ToString());
        }
    }
}
