// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Net.Test.Common;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)] // NegotiateStream only supports client-side functionality on Unix
    public abstract class NegotiateStreamStreamToStreamTest
    {
        private readonly byte[] _sampleMsg = Encoding.UTF8.GetBytes("Sample Test Message");

        protected abstract Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName);
        protected abstract Task AuthenticateAsServerAsync(NegotiateStream server);

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void NegotiateStream_StreamToStream_Authentication_Success()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated);
                Assert.False(server.IsAuthenticated);

                Task[] auth = new Task[2];
                auth[0] = AuthenticateAsClientAsync(client, CredentialCache.DefaultNetworkCredentials, string.Empty);
                auth[1] = AuthenticateAsServerAsync(server);

                bool finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Handshake completed in the allotted time");

                // Expected Client property values:
                Assert.True(client.IsAuthenticated);
                Assert.Equal(TokenImpersonationLevel.Identification, client.ImpersonationLevel);
                Assert.Equal(true, client.IsEncrypted);
                Assert.Equal(false, client.IsMutuallyAuthenticated);
                Assert.Equal(false, client.IsServer);
                Assert.Equal(true, client.IsSigned);
                Assert.Equal(false, client.LeaveInnerStreamOpen);

                IIdentity serverIdentity = client.RemoteIdentity;
                Assert.Equal("NTLM", serverIdentity.AuthenticationType);
                Assert.Equal(false, serverIdentity.IsAuthenticated);
                Assert.Equal("", serverIdentity.Name);

                // Expected Server property values:
                Assert.True(server.IsAuthenticated);
                Assert.Equal(TokenImpersonationLevel.Identification, server.ImpersonationLevel);
                Assert.Equal(true, server.IsEncrypted);
                Assert.Equal(false, server.IsMutuallyAuthenticated);
                Assert.Equal(true, server.IsServer);
                Assert.Equal(true, server.IsSigned);
                Assert.Equal(false, server.LeaveInnerStreamOpen);

                IIdentity clientIdentity = server.RemoteIdentity;
                Assert.Equal("NTLM", clientIdentity.AuthenticationType);

                Assert.Equal(true, clientIdentity.IsAuthenticated);
                IdentityValidator.AssertIsCurrentIdentity(clientIdentity);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void NegotiateStream_StreamToStream_Authentication_TargetName_Success()
        {
            string targetName = "testTargetName";

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated);
                Assert.False(server.IsAuthenticated);

                Task[] auth = new Task[2];

                auth[0] = AuthenticateAsClientAsync(client, CredentialCache.DefaultNetworkCredentials, targetName);
                auth[1] = AuthenticateAsServerAsync(server);

                bool finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Handshake completed in the allotted time");

                // Expected Client property values:
                Assert.True(client.IsAuthenticated);
                Assert.Equal(TokenImpersonationLevel.Identification, client.ImpersonationLevel);
                Assert.Equal(true, client.IsEncrypted);
                Assert.Equal(false, client.IsMutuallyAuthenticated);
                Assert.Equal(false, client.IsServer);
                Assert.Equal(true, client.IsSigned);
                Assert.Equal(false, client.LeaveInnerStreamOpen);

                IIdentity serverIdentity = client.RemoteIdentity;
                Assert.Equal("NTLM", serverIdentity.AuthenticationType);
                Assert.Equal(true, serverIdentity.IsAuthenticated);
                Assert.Equal(targetName, serverIdentity.Name);

                // Expected Server property values:
                Assert.True(server.IsAuthenticated);
                Assert.Equal(TokenImpersonationLevel.Identification, server.ImpersonationLevel);
                Assert.Equal(true, server.IsEncrypted);
                Assert.Equal(false, server.IsMutuallyAuthenticated);
                Assert.Equal(true, server.IsServer);
                Assert.Equal(true, server.IsSigned);
                Assert.Equal(false, server.LeaveInnerStreamOpen);

                IIdentity clientIdentity = server.RemoteIdentity;
                Assert.Equal("NTLM", clientIdentity.AuthenticationType);

                Assert.Equal(true, clientIdentity.IsAuthenticated);
                IdentityValidator.AssertIsCurrentIdentity(clientIdentity);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void NegotiateStream_StreamToStream_Authentication_EmptyCredentials_Fails()
        {
            string targetName = "testTargetName";

            // Ensure there is no confusion between DefaultCredentials / DefaultNetworkCredentials and a
            // NetworkCredential object with empty user, password and domain.
            NetworkCredential emptyNetworkCredential = new NetworkCredential("", "", "");
            Assert.NotEqual(emptyNetworkCredential, CredentialCache.DefaultCredentials);
            Assert.NotEqual(emptyNetworkCredential, CredentialCache.DefaultNetworkCredentials);

            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated);
                Assert.False(server.IsAuthenticated);

                Task[] auth = new Task[2];

                auth[0] = AuthenticateAsClientAsync(client, emptyNetworkCredential, targetName);
                auth[1] = AuthenticateAsServerAsync(server);

                bool finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Handshake completed in the allotted time");

                // Expected Client property values:
                Assert.True(client.IsAuthenticated);
                Assert.Equal(TokenImpersonationLevel.Identification, client.ImpersonationLevel);
                Assert.Equal(true, client.IsEncrypted);
                Assert.Equal(false, client.IsMutuallyAuthenticated);
                Assert.Equal(false, client.IsServer);
                Assert.Equal(true, client.IsSigned);
                Assert.Equal(false, client.LeaveInnerStreamOpen);

                IIdentity serverIdentity = client.RemoteIdentity;
                Assert.Equal("NTLM", serverIdentity.AuthenticationType);
                Assert.Equal(true, serverIdentity.IsAuthenticated);
                Assert.Equal(targetName, serverIdentity.Name);

                // Expected Server property values:
                Assert.True(server.IsAuthenticated);
                Assert.Equal(TokenImpersonationLevel.Identification, server.ImpersonationLevel);
                Assert.Equal(true, server.IsEncrypted);
                Assert.Equal(false, server.IsMutuallyAuthenticated);
                Assert.Equal(true, server.IsServer);
                Assert.Equal(true, server.IsSigned);
                Assert.Equal(false, server.LeaveInnerStreamOpen);

                IIdentity clientIdentity = server.RemoteIdentity;
                Assert.Equal("NTLM", clientIdentity.AuthenticationType);

                // TODO #5241: Behavior difference:
                Assert.Equal(false, clientIdentity.IsAuthenticated);
                // On .Net Desktop: Assert.Equal(true, clientIdentity.IsAuthenticated);

                IdentityValidator.AssertHasName(clientIdentity, @"NT AUTHORITY\ANONYMOUS LOGON");
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void NegotiateStream_StreamToStream_Successive_ClientWrite_Sync_Success()
        {
            byte[] recvBuf = new byte[_sampleMsg.Length];
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated);
                Assert.False(server.IsAuthenticated);

                Task[] auth = new Task[2];
                auth[0] = AuthenticateAsClientAsync(client, CredentialCache.DefaultNetworkCredentials, string.Empty);
                auth[1] = AuthenticateAsServerAsync(server);

                bool finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Handshake completed in the allotted time");

                client.Write(_sampleMsg, 0, _sampleMsg.Length);
                server.Read(recvBuf, 0, _sampleMsg.Length);

                Assert.True(_sampleMsg.SequenceEqual(recvBuf));

                client.Write(_sampleMsg, 0, _sampleMsg.Length);
                server.Read(recvBuf, 0, _sampleMsg.Length);

                Assert.True(_sampleMsg.SequenceEqual(recvBuf));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void NegotiateStream_StreamToStream_Successive_ClientWrite_Async_Success()
        {
            byte[] recvBuf = new byte[_sampleMsg.Length];
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                Assert.False(client.IsAuthenticated);
                Assert.False(server.IsAuthenticated);

                Task[] auth = new Task[2];
                auth[0] = AuthenticateAsClientAsync(client, CredentialCache.DefaultNetworkCredentials, string.Empty);
                auth[1] = AuthenticateAsServerAsync(server);

                bool finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Handshake completed in the allotted time");

                auth[0] = client.WriteAsync(_sampleMsg, 0, _sampleMsg.Length);
                auth[1] = server.ReadAsync(recvBuf, 0, _sampleMsg.Length);
                finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Send/receive completed in the allotted time");
                Assert.True(_sampleMsg.SequenceEqual(recvBuf));

                auth[0] = client.WriteAsync(_sampleMsg, 0, _sampleMsg.Length);
                auth[1] = server.ReadAsync(recvBuf, 0, _sampleMsg.Length);
                finished = Task.WaitAll(auth, TestConfiguration.PassingTestTimeoutMilliseconds);
                Assert.True(finished, "Send/receive completed in the allotted time");
                Assert.True(_sampleMsg.SequenceEqual(recvBuf));
            }
        }
    }

    public sealed class NegotiateStreamStreamToStreamTest_Async : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            client.AuthenticateAsClientAsync(credential, targetName);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            server.AuthenticateAsServerAsync();
    }

    public sealed class NegotiateStreamStreamToStreamTest_BeginEnd : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            Task.Factory.FromAsync(client.BeginAuthenticateAsClient, client.EndAuthenticateAsClient, credential, targetName, null);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            Task.Factory.FromAsync(server.BeginAuthenticateAsServer, server.EndAuthenticateAsServer, null);
    }

    public sealed class NegotiateStreamStreamToStreamTest_Sync : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            Task.Run(() => client.AuthenticateAsClient(credential, targetName));

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            Task.Run(() => server.AuthenticateAsServer());
    }
}
