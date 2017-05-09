// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

#pragma warning disable CS0618 // obsolete warnings

namespace System.Net.Tests
{
    public class AuthorizationTest
    {
        [Fact]
        public void Ctor_Token_ExpectDefaultValues()
        {
            Authorization authorization = new Authorization("token");
            Assert.Equal("token", authorization.Message);
            Assert.True(authorization.Complete);
            Assert.Null(authorization.ConnectionGroupId);
            Assert.Null(authorization.ProtectionRealm);
            Assert.False(authorization.MutuallyAuthenticated);
            authorization.MutuallyAuthenticated = true;
            Assert.True(authorization.MutuallyAuthenticated);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Ctor_TokenNullOrEmpty_ExpectMessageNull(string token)
        {
            Authorization authorization = new Authorization(token);
            Assert.Equal(null, authorization.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Ctor_ConnectionGroupIdNullOrEmpty_ExpectConnectionGroupIdNull(string connectionGroupId)
        {
            Authorization authorization = new Authorization(null, true, connectionGroupId);
            Assert.Equal(null, authorization.ConnectionGroupId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new string[0] { } })]
        public void ProtectionRealm_SetNullOrEmptyGet_ExpectNullValue(string[] protectionRealm)
        {
            Authorization authorization = new Authorization(null);
            authorization.ProtectionRealm = protectionRealm;
            Assert.Equal(null, authorization.ProtectionRealm);
        }

        [Fact]
        public void ProtectionRealm_SetArray_ExpectEqualValues()
        {
            Authorization authorization = new Authorization(null);
            string[] protectionRealm = new string[] { "a" };
            authorization.ProtectionRealm = protectionRealm;
            Assert.Equal(protectionRealm, authorization.ProtectionRealm);
        }

        public static IEnumerable<object[]> GetMutuallyAuthenticatedValues()
        {
            yield return new object[] { new Authorization(null, false), false, false };
            yield return new object[] { new Authorization(null, true), false, false };
            yield return new object[] { new Authorization(null, true), true, true };
        }

        [Theory]
        [MemberData(nameof(GetMutuallyAuthenticatedValues))]
        public void MutuallyAuthenticated_Values_ExpectEqualValues(Authorization authorization, bool setValue, bool expectedValue)
        {
            authorization.MutuallyAuthenticated = setValue;
            Assert.Equal(expectedValue, authorization.MutuallyAuthenticated);
        }
    }
}
