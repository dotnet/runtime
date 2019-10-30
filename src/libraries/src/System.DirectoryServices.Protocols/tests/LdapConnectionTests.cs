// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class LdapConnectionTests
    {
        [Theory]
        [InlineData(null, new string[0])]
        [InlineData("server", new string[] { "server" })]
        public void Ctor_String(string server, string[] expectedServer)
        {
            var connection = new LdapConnection(server);
            Assert.Equal(AuthType.Negotiate, connection.AuthType);
            Assert.True(connection.AutoBind);
            Assert.Equal(expectedServer, ((LdapDirectoryIdentifier)connection.Directory).Servers);
            Assert.Equal(new TimeSpan(0, 0, 30), connection.Timeout);
        }

        [Fact]
        public void Ctor_ServerHasSpaceInName_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new LdapConnection("se rver"));
        }

        public static IEnumerable<object[]> Ctor_Identifier_TestData()
        {
            yield return new object[] { new LdapDirectoryIdentifier("server") };
            yield return new object[] { new LdapDirectoryIdentifier(new string[] { "server", null, "server" }, false, false) };
            yield return new object[] { new LdapDirectoryIdentifier(new string[] { null }, false, false) };
        }

        [Theory]
        [MemberData(nameof(Ctor_Identifier_TestData))]
        public void Ctor_Identifier(LdapDirectoryIdentifier identifier)
        {
            var connection = new LdapConnection(identifier);
            Assert.Equal(AuthType.Negotiate, connection.AuthType);
            Assert.True(connection.AutoBind);
            Assert.Equal(identifier, connection.Directory);
            Assert.Equal(new TimeSpan(0, 0, 30), connection.Timeout);
        }

        public static IEnumerable<object[]> Ctor_Identifier_NetworkCredential_TestData()
        {
            yield return new object[] { new LdapDirectoryIdentifier("server"), null };
            yield return new object[] { new LdapDirectoryIdentifier("server"), new NetworkCredential("username", "password") };
        }

        [Theory]
        [MemberData(nameof(Ctor_Identifier_NetworkCredential_TestData))]
        public void Ctor_Identifier_AuthType(LdapDirectoryIdentifier identifier, NetworkCredential credential)
        {
            var connection = new LdapConnection(identifier, credential);
            Assert.Equal(AuthType.Negotiate, connection.AuthType);
            Assert.True(connection.AutoBind);
            Assert.Equal(identifier, connection.Directory);
            Assert.Equal(new TimeSpan(0, 0, 30), connection.Timeout);
        }

        public static IEnumerable<object[]> Ctor_Identifier_NetworkCredential_AuthType_TestData()
        {
            yield return new object[] { new LdapDirectoryIdentifier("server"), null, AuthType.Anonymous };
            yield return new object[] { new LdapDirectoryIdentifier("server"), new NetworkCredential(), AuthType.Anonymous };
            yield return new object[] { new LdapDirectoryIdentifier("server"), new NetworkCredential("username", "password"), AuthType.Kerberos };
        }

        [Theory]
        [MemberData(nameof(Ctor_Identifier_NetworkCredential_AuthType_TestData))]
        public void Ctor_Identifier_AuthType(LdapDirectoryIdentifier identifier, NetworkCredential credential, AuthType authType)
        {
            var connection = new LdapConnection(identifier, credential, authType);
            Assert.Equal(authType, connection.AuthType);
            Assert.True(connection.AutoBind);
            Assert.Equal(identifier, connection.Directory);
            Assert.Equal(new TimeSpan(0, 0, 30), connection.Timeout);
        }

        [Fact]
        public void Ctor_NullIdentifier_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new LdapConnection((LdapDirectoryIdentifier)null));
            Assert.Throws<NullReferenceException>(() => new LdapConnection(null, new NetworkCredential()));
            Assert.Throws<NullReferenceException>(() => new LdapConnection(null, new NetworkCredential(), AuthType.Dpa));
        }

        [Theory]
        [InlineData(AuthType.Anonymous - 1)]
        [InlineData(AuthType.Kerberos + 1)]
        public void Ctor_InvalidAuthType_ThrowsInvalidEnumArgumentException(AuthType authType)
        {
            AssertExtensions.Throws<InvalidEnumArgumentException>("authType", () => new LdapConnection(new LdapDirectoryIdentifier("server"), new NetworkCredential(), authType));
        }

        [Fact]
        public void Ctor_InvalidAuthTypeWithCredentials_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new LdapConnection(new LdapDirectoryIdentifier("server"), new NetworkCredential("username", "password"), AuthType.Anonymous));
        }

        [Fact]
        public void AuthType_SetValid_GetReturnsExpected()
        {
            var connection = new LdapConnection("server") { AuthType = AuthType.Basic };
            Assert.Equal(AuthType.Basic, connection.AuthType);
        }

        [Theory]
        [InlineData(AuthType.Anonymous - 1)]
        [InlineData(AuthType.Kerberos + 1)]
        public void AuthType_SetInvalid_ThrowsInvalidEnumArgumentException(AuthType authType)
        {
            var connection = new LdapConnection("server");
            AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => connection.AuthType = authType);
        }

        [Fact]
        public void AutoBind_Set_GetReturnsExpected()
        {
            var connection = new LdapConnection("server") { AutoBind = false };
            Assert.False(connection.AutoBind);
        }

        [Fact]
        public void Timeout_SetValid_GetReturnsExpected()
        {
            var connection = new LdapConnection("server") { Timeout = TimeSpan.Zero };
            Assert.Equal(TimeSpan.Zero, connection.Timeout);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData((long)int.MaxValue + 1)]
        public void Timeout_SetInvalid_ThrowsArgumentException(long totalSeconds)
        {
            var connection = new LdapConnection("server");
            AssertExtensions.Throws<ArgumentException>("value", () => connection.Timeout = TimeSpan.FromSeconds(totalSeconds));
        }

        [Fact]
        public void Bind_Disposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.Bind());
            Assert.Throws<ObjectDisposedException>(() => connection.Bind(null));
        }

        [Fact]
        public void Bind_AnonymouseAuthenticationAndNetworkCredentials_ThrowsInvalidOperationException()
        {
            var connection = new LdapConnection("server") { AuthType = AuthType.Anonymous };
            Assert.Throws<InvalidOperationException>(() => connection.Bind(new NetworkCredential("name", "password")));

            connection.Credential = new NetworkCredential("name", "password");
            Assert.Throws<InvalidOperationException>(() => connection.Bind());
        }

        [Fact]
        public void SendRequest_Disposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.SendRequest(new AddRequest()));
            Assert.Throws<ObjectDisposedException>(() => connection.BeginSendRequest(new AddRequest(), PartialResultProcessing.NoPartialResultSupport, null, null));
        }

        [Fact]
        public void SendRequest_NullRequest_ThrowsArgumentNullException()
        {
            var connection = new LdapConnection("server");
            AssertExtensions.Throws<ArgumentNullException>("request", () => connection.SendRequest(null));
            AssertExtensions.Throws<ArgumentNullException>("request", () => connection.BeginSendRequest(null, PartialResultProcessing.NoPartialResultSupport, null, null));
        }

        [Fact]
        public void SendRequest_DsmlAuthRequest_ThrowsNotSupportedException()
        {
            var connection = new LdapConnection("server");
            Assert.Throws<NotSupportedException>(() => connection.SendRequest(new DsmlAuthRequest()));
        }

        [Theory]
        [InlineData(PartialResultProcessing.NoPartialResultSupport - 1)]
        [InlineData(PartialResultProcessing.ReturnPartialResultsAndNotifyCallback + 1)]
        public void BeginSendRequest_InvalidPartialMode_ThrowsInvalidEnumArgumentException(PartialResultProcessing partialMode)
        {
            var connection = new LdapConnection("server");
            AssertExtensions.Throws<InvalidEnumArgumentException>("partialMode", () => connection.BeginSendRequest(new AddRequest(), partialMode, null, null));
        }

        [Theory]
        [InlineData(PartialResultProcessing.ReturnPartialResults)]
        [InlineData(PartialResultProcessing.ReturnPartialResultsAndNotifyCallback)]
        public void BeginSendRequest_ReturnModeAndSearchRequest_ThrowsInvalidNotSupportedException(PartialResultProcessing partialMode)
        {
            var connection = new LdapConnection("server");
            Assert.Throws<NotSupportedException>(() => connection.BeginSendRequest(new AddRequest(), partialMode, null, null));
        }

        [Fact]
        public void BeginSendRequest_NotifyCallbackAndNullCallback_ThrowsArgumentException()
        {
            var connection = new LdapConnection("server");
            AssertExtensions.Throws<ArgumentException>("callback", () => connection.BeginSendRequest(new SearchRequest(), PartialResultProcessing.ReturnPartialResultsAndNotifyCallback, null, null));
        }

        [Fact]
        public void EndSendRequest_Disposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.EndSendRequest(null));
        }

        [Fact]
        public void EndSendRequest_NullAsyncResult_ThrowsArgumentNullException()
        {
            var connection = new LdapConnection("server");
            AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => connection.EndSendRequest(null));
        }

        [Fact]
        public void EndSendRequest_InvalidAsyncResult_ThrowsArgumentNullException()
        {
            var connection = new LdapConnection("server");
            AssertExtensions.Throws<ArgumentException>(null, () => connection.EndSendRequest(new CustomAsyncResult()));
        }

        [Fact]
        public void GetPartialResults_Disposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.GetPartialResults(null));
        }

        [Fact]
        public void GetPartialResults_NullAsyncResult_ThrowsArgumentNullException()
        {
            var connection = new LdapConnection("server");
            AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => connection.GetPartialResults(null));
        }

        [Fact]
        public void GetPartialResults_InvalidAsyncResult_ThrowsArgumentNullException()
        {
            var connection = new LdapConnection("server");
            AssertExtensions.Throws<ArgumentException>(null, () => connection.GetPartialResults(new CustomAsyncResult()));
        }

        [Fact]
        public void Abort_Disposed_ThrowsObjectDisposedException()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.Abort(null));
        }

        [Fact]
        public void Abort_NullAsyncResult_ThrowsArgumentNullException()
        {
            var connection = new LdapConnection("server");
            AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => connection.Abort(null));
        }

        [Fact]
        public void Abort_InvalidAsyncResult_ThrowsArgumentNullException()
        {
            var connection = new LdapConnection("server");
            AssertExtensions.Throws<ArgumentException>(null, () => connection.Abort(new CustomAsyncResult()));
        }

        [Fact]
        public void Dispose_MultipleTimes_Nop()
        {
            var connection = new LdapConnection("server");
            connection.Dispose();
            connection.Dispose();
        }

        public class CustomAsyncResult : IAsyncResult
        {
            public object AsyncState => throw new NotImplementedException();
            public WaitHandle AsyncWaitHandle => throw new NotImplementedException();
            public bool CompletedSynchronously => throw new NotImplementedException();
            public bool IsCompleted => throw new NotImplementedException();
        }
    }
}
