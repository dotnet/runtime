// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Net.Test.Common;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)] // NegotiateStream client needs explicit credentials or SPNs on unix.
    public abstract class NegotiateStreamStreamToStreamTest
    {
        public static bool IsNtlmInstalled => Capability.IsNtlmInstalled();

        private const int PartialBytesToRead = 5;
        private static readonly byte[] s_sampleMsg = Encoding.UTF8.GetBytes("Sample Test Message");

        private const int MaxWriteDataSize = 63 * 1024; // NegoState.MaxWriteDataSize
        private static string s_longString = new string('A', MaxWriteDataSize) + 'Z';
        private static readonly byte[] s_longMsg = Encoding.ASCII.GetBytes(s_longString);

        protected abstract Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName);
        protected abstract Task AuthenticateAsServerAsync(NegotiateStream server);

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public async Task NegotiateStream_StreamToStream_Authentication_Success()
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
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

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

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public async Task NegotiateStream_StreamToStream_Authentication_TargetName_Success()
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
                Assert.False(client.IsMutuallyAuthenticated);
                Assert.False(server.IsMutuallyAuthenticated);

                Task[] auth = new Task[2];

                auth[0] = AuthenticateAsClientAsync(client, CredentialCache.DefaultNetworkCredentials, targetName);
                auth[1] = AuthenticateAsServerAsync(server);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

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

        [ConditionalFact(nameof(IsNtlmInstalled))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Core difference in behavior: https://github.com/dotnet/corefx/issues/5241")]
        public async Task NegotiateStream_StreamToStream_Authentication_EmptyCredentials_Fails()
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

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

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
                // On .NET Desktop: Assert.Equal(true, clientIdentity.IsAuthenticated);

                IdentityValidator.AssertHasName(clientIdentity, new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).Value);
            }
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public async Task NegotiateStream_StreamToStream_Successive_ClientWrite_Sync_Success()
        {
            byte[] recvBuf = new byte[s_sampleMsg.Length];
            VirtualNetwork network = new VirtualNetwork();
            int bytesRead = 0;

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

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                client.Write(s_sampleMsg, 0, s_sampleMsg.Length);
                server.Read(recvBuf, 0, s_sampleMsg.Length);

                Assert.True(s_sampleMsg.SequenceEqual(recvBuf));

                client.Write(s_sampleMsg, 0, s_sampleMsg.Length);

                // Test partial sync read.
                bytesRead = server.Read(recvBuf, 0, PartialBytesToRead);
                Assert.Equal(PartialBytesToRead, bytesRead);

                bytesRead = server.Read(recvBuf, PartialBytesToRead, s_sampleMsg.Length - PartialBytesToRead);
                Assert.Equal(s_sampleMsg.Length - PartialBytesToRead, bytesRead);

                Assert.True(s_sampleMsg.SequenceEqual(recvBuf));
            }
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public async Task NegotiateStream_StreamToStream_Successive_ClientWrite_Async_Success()
        {
            byte[] recvBuf = new byte[s_sampleMsg.Length];
            VirtualNetwork network = new VirtualNetwork();
            int bytesRead = 0;

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

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);

                auth[0] = client.WriteAsync(s_sampleMsg, 0, s_sampleMsg.Length);
                auth[1] = server.ReadAsync(recvBuf, 0, s_sampleMsg.Length);
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(auth);
                Assert.True(s_sampleMsg.SequenceEqual(recvBuf));

                await client.WriteAsync(s_sampleMsg, 0, s_sampleMsg.Length);

                // Test partial async read.
                bytesRead = await server.ReadAsync(recvBuf, 0, PartialBytesToRead);
                Assert.Equal(PartialBytesToRead, bytesRead);

                bytesRead = await server.ReadAsync(recvBuf, PartialBytesToRead, s_sampleMsg.Length - PartialBytesToRead);
                Assert.Equal(s_sampleMsg.Length - PartialBytesToRead, bytesRead);

                Assert.True(s_sampleMsg.SequenceEqual(recvBuf));
            }
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public async Task NegotiateStream_ReadWriteLongMsgSync_Success()
        {
            byte[] recvBuf = new byte[s_longMsg.Length];
            var network = new VirtualNetwork();
            int bytesRead = 0;

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, string.Empty),
                    server.AuthenticateAsServerAsync());

                client.Write(s_longMsg, 0, s_longMsg.Length);

                while (bytesRead < s_longMsg.Length)
                {
                    bytesRead += server.Read(recvBuf, bytesRead, s_longMsg.Length - bytesRead);
                }

                Assert.True(s_longMsg.SequenceEqual(recvBuf));
            }
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public async Task NegotiateStream_ReadWriteLongMsgAsync_Success()
        {
            byte[] recvBuf = new byte[s_longMsg.Length];
            var network = new VirtualNetwork();
            int bytesRead = 0;

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, string.Empty),
                    server.AuthenticateAsServerAsync());

                await client.WriteAsync(s_longMsg, 0, s_longMsg.Length);

                while (bytesRead < s_longMsg.Length)
                {
                    bytesRead += await server.ReadAsync(recvBuf, bytesRead, s_longMsg.Length - bytesRead);
                }

                Assert.True(s_longMsg.SequenceEqual(recvBuf));
            }
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public void NegotiateStream_StreamToStream_Flush_Propagated()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var stream = new VirtualNetworkStream(network, isServer: false))
            using (var negotiateStream = new NegotiateStream(stream))
            {
                Assert.False(stream.HasBeenSyncFlushed);
                negotiateStream.Flush();
                Assert.True(stream.HasBeenSyncFlushed);
            }
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Relies on FlushAsync override not available in desktop")]
        public void NegotiateStream_StreamToStream_FlushAsync_Propagated()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var stream = new VirtualNetworkStream(network, isServer: false))
            using (var negotiateStream = new NegotiateStream(stream))
            {
                Task task = negotiateStream.FlushAsync();

                Assert.False(task.IsCompleted);
                stream.CompleteAsyncFlush();
                Assert.True(task.IsCompleted);
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

    public sealed class NegotiateStreamStreamToStreamTest_Async_TestOverloadNullBinding : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            client.AuthenticateAsClientAsync(credential, null, targetName);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            server.AuthenticateAsServerAsync(null);
    }

    public sealed class NegotiateStreamStreamToStreamTest_Async_TestOverloadProtectionLevel : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            client.AuthenticateAsClientAsync(credential, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            server.AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
    }

    public sealed class NegotiateStreamStreamToStreamTest_Async_TestOverloadAllParameters : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            client.AuthenticateAsClientAsync(credential, null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            server.AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
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

    public sealed class NegotiateStreamStreamToStreamTest_Sync_TestOverloadNullBinding : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            Task.Run(() => client.AuthenticateAsClient(credential, null, targetName));

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            Task.Run(() => server.AuthenticateAsServer(null));
    }

    public sealed class NegotiateStreamStreamToStreamTest_Sync_TestOverloadAllParameters : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            Task.Run(() => client.AuthenticateAsClient(credential, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification));

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            Task.Run(() => server.AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification));
    }
}
