// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Principal;
using Xunit;

namespace System.Security.Claims
{
    public class GenericPrincipalTest
    {
        [Fact]
        public void NullRoles()
        {
            GenericIdentity gi = new GenericIdentity("user");
            GenericPrincipal gp = new GenericPrincipal(gi, null);
            Assert.Equal("user", gp.Identity.Name);
            Assert.False(gp.IsInRole("role 1"));
        }

        [Fact]
        public void IsInRole()
        {
            GenericIdentity gi = new GenericIdentity("user");
            string[] roles = new string[5];
            roles[0] = "role 1";
            GenericPrincipal gp = new GenericPrincipal(gi, roles);
            roles[1] = "role 2";
            Assert.True(gp.IsInRole("role 1"));
            Assert.False(gp.IsInRole("role 2"));
        }

        [Fact]
        public void IsInRole_CaseInsensitive()
        {
            GenericIdentity gi = new GenericIdentity("user");
            GenericPrincipal gp = new GenericPrincipal(gi, new string[2] { "hello", "world" });
            Assert.True(gp.IsInRole("heLLo"));
            Assert.True(gp.IsInRole("wOrLd"));
        }

        [Fact]
        public void Ctor_ArgumentValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("identity", () => new GenericPrincipal(null, new string[5]));
        }
    }
}
