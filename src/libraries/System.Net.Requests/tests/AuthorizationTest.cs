// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Net.Security;
using Xunit;

#pragma warning disable CS0618 // obsolete warnings

namespace System.Net.Tests
{
    public class AuthorizationTest
    {
        [Fact]
        public static void Ctor()
        {
            var a = new Authorization("token");
            Assert.Equal("token", a.Message);
            Assert.True(a.Complete);
            Assert.Null(a.ConnectionGroupId);
            Assert.Null(a.ProtectionRealm);
            Assert.False(a.MutuallyAuthenticated);
        }
    }
}
