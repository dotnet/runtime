// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Principal;
using Xunit;

namespace System.Security.Claims
{
    public class GenericIdentityTest
    {
        [Fact]
        public void Name()
        {
            GenericIdentity gi = new GenericIdentity("user");
            Assert.Equal("user", gi.Name);
            Assert.Equal(string.Empty, gi.AuthenticationType);
            Assert.True(gi.IsAuthenticated);
        }

        [Fact]
        public void NameAuthenticationType()
        {
            GenericIdentity gi = new GenericIdentity("user", "customType");
            Assert.Equal("user", gi.Name);
            Assert.Equal("customType", gi.AuthenticationType);
            Assert.True(gi.IsAuthenticated);
        }

        [Fact]
        public void EmptyName()
        {
            GenericIdentity gi = new GenericIdentity("");
            Assert.Equal(String.Empty, gi.Name);
            Assert.Equal(String.Empty, gi.AuthenticationType);
            Assert.False(gi.IsAuthenticated);
        }

        [Fact]
        public void Ctor_ArgumentValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => new GenericIdentity(null));
            AssertExtensions.Throws<ArgumentNullException>("type", () => new GenericIdentity("user", null));
        }
    }
}
