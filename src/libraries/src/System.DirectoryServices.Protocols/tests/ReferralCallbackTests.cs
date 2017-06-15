// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class ReferralCallbackTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var callback = new ReferralCallback();
            Assert.Null(callback.DereferenceConnection);
            Assert.Null(callback.NotifyNewConnection);
            Assert.Null(callback.QueryForConnection);
        }

        [Fact]
        public void DereferenceConnection_Set_GetReturnsExpected()
        {
            var callback = new ReferralCallback { DereferenceConnection = DereferenceConnection };
            Assert.Equal(DereferenceConnection, callback.DereferenceConnection);
        }

        [Fact]
        public void NotifyNewConnection_Set_GetReturnsExpected()
        {
            var callback = new ReferralCallback { NotifyNewConnection = NotifyNewConnection };
            Assert.Equal(NotifyNewConnection, callback.NotifyNewConnection);
        }

        [Fact]
        public void QueryForConnection_Set_GetReturnsExpected()
        {
            var callback = new ReferralCallback { QueryForConnection = QueryForConnection };
            Assert.Equal(QueryForConnection, callback.QueryForConnection);
        }

        public static void DereferenceConnection(LdapConnection primaryConnection, LdapConnection connectionToDereference) { }
        public static bool NotifyNewConnection(LdapConnection primaryConnection, LdapConnection referralFromConnection, string newDistinguishedName, LdapDirectoryIdentifier identifier, LdapConnection newConnection, NetworkCredential credential, long currentUserToken, int errorCodeFromBind) => true;
        public static LdapConnection QueryForConnection(LdapConnection primaryConnection, LdapConnection referralFromConnection, string newDistinguishedName, LdapDirectoryIdentifier identifier, NetworkCredential credential, long currentUserToken) => null;
    }
}
