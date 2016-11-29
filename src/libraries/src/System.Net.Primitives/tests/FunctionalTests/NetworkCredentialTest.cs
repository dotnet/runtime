// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static partial class NetworkCredentialTest
    {
        [Fact]
        public static void Ctor_Empty_Success()
        {
            NetworkCredential nc = new NetworkCredential();
            Assert.Equal(String.Empty, nc.UserName);
            Assert.Equal(String.Empty, nc.Password);
            Assert.Equal(String.Empty, nc.Domain);
        }

        [Fact]
        public static void Ctor_UserNamePassword_Success()
        {
            NetworkCredential nc = new NetworkCredential("username", "password");
            Assert.Equal("username", nc.UserName);
            Assert.Equal("password", nc.Password);
            Assert.Equal(String.Empty, nc.Domain);
        }

        [Fact]
        public static void Ctor_UserNamePasswordDomain_Success()
        {
            NetworkCredential nc = new NetworkCredential("username", "password", "domain");
            Assert.Equal("username", nc.UserName);
            Assert.Equal("password", nc.Password);
            Assert.Equal("domain", nc.Domain);
        }

        [Fact]
        public static void UserName_GetSet_Success()
        {
            NetworkCredential nc = new NetworkCredential();

            nc.UserName = "username";
            Assert.Equal("username", nc.UserName);

            nc.UserName = null;
            Assert.Equal(String.Empty, nc.UserName);
        }

        [Fact]
        public static void Password_GetSet_Success()
        {
            NetworkCredential nc = new NetworkCredential();

            nc.Password = "password";
            Assert.Equal("password", nc.Password);
        }

        [Fact]
        public static void Domain_GetSet_Success()
        {
            NetworkCredential nc = new NetworkCredential();

            nc.Domain = "domain";
            Assert.Equal("domain", nc.Domain);

            nc.Domain = null;
            Assert.Equal(String.Empty, nc.Domain);
        }

        [Fact]
        public static void GetCredential_UriAuthenticationType_Success()
        {
            NetworkCredential nc = new NetworkCredential();

            Assert.Equal(nc, nc.GetCredential(new Uri("http://microsoft.com"), "authenticationType"));
        }

        [Fact]
        public static void GetCredential_HostPortAuthenticationType_Success()
        {
            NetworkCredential nc = new NetworkCredential();

            Assert.Equal(nc, nc.GetCredential("host", 500, "authenticationType"));
        }
    }
}
