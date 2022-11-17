// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)] // NegotiateStream client needs explicit credentials or SPNs on unix.
    public abstract class NegotiateStreamStreamToStreamTest
    {
        public static bool IsNtlmInstalled => Capability.IsNtlmInstalled();

        private const int PartialBytesToRead = 5;
        protected static readonly byte[] s_sampleMsg = "Sample Test Message"u8.ToArray();

        private const int MaxWriteDataSize = 63 * 1024; // NegoState.MaxWriteDataSize
        private static string s_longString = new string('A', MaxWriteDataSize) + 'Z';
        private static readonly byte[] s_longMsg = Encoding.ASCII.GetBytes(s_longString);

        protected abstract Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName);
        protected abstract Task AuthenticateAsServerAsync(NegotiateStream server);
        protected abstract Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
        protected abstract Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
        protected virtual bool SupportsCancelableReadsWrites => false;
        protected virtual bool IsEncryptedAndSigned => true;

        [ConditionalTheory(nameof(IsNtlmInstalled))]
        [InlineData(0)]
        [InlineData(1)]
        public async Task NegotiateStream_StreamToStream_Authentication_Success(int delay)
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();

            using (var clientStream = new DelayStream(stream1, delay))
            using (var serverStream = new DelayStream(stream2, delay))
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
                Assert.Equal(IsEncryptedAndSigned, client.IsEncrypted);
                Assert.False(client.IsMutuallyAuthenticated);
                Assert.False(client.IsServer);
                Assert.Equal(IsEncryptedAndSigned, client.IsSigned);
                Assert.False(client.LeaveInnerStreamOpen);

                IIdentity serverIdentity = client.RemoteIdentity;
                using (serverIdentity as IDisposable)
                {
                    Assert.Equal("NTLM", serverIdentity.AuthenticationType);
                    Assert.False(serverIdentity.IsAuthenticated);
                    Assert.Equal("", serverIdentity.Name);
                }

                // Expected Server property values:
                Assert.True(server.IsAuthenticated);
                Assert.Equal(TokenImpersonationLevel.Identification, server.ImpersonationLevel);
                Assert.Equal(IsEncryptedAndSigned, server.IsEncrypted);
                Assert.False(server.IsMutuallyAuthenticated);
                Assert.True(server.IsServer);
                Assert.Equal(IsEncryptedAndSigned, server.IsSigned);
                Assert.False(server.LeaveInnerStreamOpen);

                IIdentity clientIdentity = server.RemoteIdentity;
                using (clientIdentity as IDisposable)
                {
                    Assert.Equal("NTLM", clientIdentity.AuthenticationType);

                    Assert.True(clientIdentity.IsAuthenticated);

                    IdentityValidator.AssertIsCurrentIdentity(clientIdentity);
                }
            }
        }

        [ConditionalTheory(nameof(IsNtlmInstalled))]
        [InlineData(0)]
        [InlineData(1)]
        public async Task NegotiateStream_StreamToStream_Authenticated_DisposeAsync(int delay)
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            await using (var client = new NegotiateStream(new DelayStream(stream1, delay)))
            await using (var server = new NegotiateStream(new DelayStream(stream2, delay)))
            {
                Assert.False(client.IsServer);
                Assert.False(server.IsServer);

                Assert.False(client.IsAuthenticated);
                Assert.False(server.IsAuthenticated);

                Assert.False(client.IsMutuallyAuthenticated);
                Assert.False(server.IsMutuallyAuthenticated);

                Assert.False(client.IsEncrypted);
                Assert.False(server.IsEncrypted);

                Assert.False(client.IsSigned);
                Assert.False(server.IsSigned);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    AuthenticateAsClientAsync(client, CredentialCache.DefaultNetworkCredentials, string.Empty),
                    AuthenticateAsServerAsync(server));
            }
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public async Task NegotiateStream_StreamToStream_Unauthenticated_Dispose()
        {
            new NegotiateStream(new MemoryStream()).Dispose();
            await new NegotiateStream(new MemoryStream()).DisposeAsync();
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public async Task NegotiateStream_StreamToStream_Authentication_TargetName_Success()
        {
            string targetName = "testTargetName";

            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
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
                Assert.Equal(IsEncryptedAndSigned, client.IsEncrypted);
                Assert.False(client.IsMutuallyAuthenticated);
                Assert.False(client.IsServer);
                Assert.Equal(IsEncryptedAndSigned, client.IsSigned);
                Assert.False(client.LeaveInnerStreamOpen);

                IIdentity serverIdentity = client.RemoteIdentity;
                using (serverIdentity as IDisposable)
                {
                    Assert.Equal("NTLM", serverIdentity.AuthenticationType);
                    Assert.True(serverIdentity.IsAuthenticated);
                    Assert.Equal(targetName, serverIdentity.Name);
                }

                // Expected Server property values:
                Assert.True(server.IsAuthenticated);
                Assert.Equal(TokenImpersonationLevel.Identification, server.ImpersonationLevel);
                Assert.Equal(IsEncryptedAndSigned, server.IsEncrypted);
                Assert.False(server.IsMutuallyAuthenticated);
                Assert.True(server.IsServer);
                Assert.Equal(IsEncryptedAndSigned, server.IsSigned);
                Assert.False(server.LeaveInnerStreamOpen);

                IIdentity clientIdentity = server.RemoteIdentity;
                using (clientIdentity as IDisposable)
                {
                    Assert.Equal("NTLM", clientIdentity.AuthenticationType);

                    Assert.True(clientIdentity.IsAuthenticated);

                    IdentityValidator.AssertIsCurrentIdentity(clientIdentity);
                }
            }
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public async Task NegotiateStream_StreamToStream_Authentication_EmptyCredentials_Fails()
        {
            string targetName = "testTargetName";

            // Ensure there is no confusion between DefaultCredentials / DefaultNetworkCredentials and a
            // NetworkCredential object with empty user, password and domain.
            NetworkCredential emptyNetworkCredential = new NetworkCredential("", "", "");
            Assert.NotEqual(emptyNetworkCredential, CredentialCache.DefaultCredentials);
            Assert.NotEqual(emptyNetworkCredential, CredentialCache.DefaultNetworkCredentials);

            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
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
                Assert.Equal(IsEncryptedAndSigned, client.IsEncrypted);
                Assert.False(client.IsMutuallyAuthenticated);
                Assert.False(client.IsServer);
                Assert.Equal(IsEncryptedAndSigned, client.IsSigned);
                Assert.False(client.LeaveInnerStreamOpen);

                IIdentity serverIdentity = client.RemoteIdentity;
                using (serverIdentity as IDisposable)
                {
                    Assert.Equal("NTLM", serverIdentity.AuthenticationType);
                    Assert.True(serverIdentity.IsAuthenticated);
                    Assert.Equal(targetName, serverIdentity.Name);
                }

                // Expected Server property values:
                Assert.True(server.IsAuthenticated);
                Assert.Equal(TokenImpersonationLevel.Identification, server.ImpersonationLevel);
                Assert.Equal(IsEncryptedAndSigned, server.IsEncrypted);
                Assert.False(server.IsMutuallyAuthenticated);
                Assert.True(server.IsServer);
                Assert.Equal(IsEncryptedAndSigned, server.IsSigned);
                Assert.False(server.LeaveInnerStreamOpen);

                IIdentity clientIdentity = server.RemoteIdentity;
                using (clientIdentity as IDisposable)
                {
                    Assert.Equal("NTLM", clientIdentity.AuthenticationType);

                    Assert.False(clientIdentity.IsAuthenticated);
                    // On .NET Desktop: Assert.True(clientIdentity.IsAuthenticated);

                    IdentityValidator.AssertHasName(clientIdentity, new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).Value);
                }
            }
        }
    }

    public sealed class NegotiateStreamStreamToStreamTest_Async_Array : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            client.AuthenticateAsClientAsync(credential, targetName);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            server.AuthenticateAsServerAsync();

        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.ReadAsync(buffer, offset, count, cancellationToken);

        protected override Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.WriteAsync(buffer, offset, count, cancellationToken);

        protected override bool SupportsCancelableReadsWrites => true;
    }

    public class NegotiateStreamStreamToStreamTest_Async_Memory : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            client.AuthenticateAsClientAsync(credential, targetName);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            server.AuthenticateAsServerAsync();

        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        protected override Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        protected override bool SupportsCancelableReadsWrites => true;
    }

    public class NegotiateStreamStreamToStreamTest_Async_Memory_NotEncrypted : NegotiateStreamStreamToStreamTest_Async_Memory
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            client.AuthenticateAsClientAsync(credential, targetName, ProtectionLevel.None, TokenImpersonationLevel.Identification);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            server.AuthenticateAsServerAsync(CredentialCache.DefaultNetworkCredentials, ProtectionLevel.None, TokenImpersonationLevel.Identification);

        protected override bool IsEncryptedAndSigned => false;
    }

    public sealed class NegotiateStreamStreamToStreamTest_Async_TestOverloadNullBinding : NegotiateStreamStreamToStreamTest_Async_Memory
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            client.AuthenticateAsClientAsync(credential, null, targetName);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            server.AuthenticateAsServerAsync(null);
    }

    public sealed class NegotiateStreamStreamToStreamTest_Async_TestOverloadProtectionLevel : NegotiateStreamStreamToStreamTest_Async_Memory
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            client.AuthenticateAsClientAsync(credential, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            server.AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
    }

    public sealed class NegotiateStreamStreamToStreamTest_Async_TestOverloadAllParameters : NegotiateStreamStreamToStreamTest_Async_Memory
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            client.AuthenticateAsClientAsync(credential, null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            server.AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
    }

    public class NegotiateStreamStreamToStreamTest_BeginEnd : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            Task.Factory.FromAsync(client.BeginAuthenticateAsClient, client.EndAuthenticateAsClient, credential, targetName, null);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            Task.Factory.FromAsync(server.BeginAuthenticateAsServer, server.EndAuthenticateAsServer, null);

        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.Factory.FromAsync(stream.BeginRead, stream.EndRead, buffer, offset, count, null);

        protected override Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, buffer, offset, count, null);
    }

    public sealed class NegotiateStreamStreamToStreamTest_BeginEnd_TestOverloadNullBinding : NegotiateStreamStreamToStreamTest_BeginEnd
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            Task.Factory.FromAsync(client.BeginAuthenticateAsClient, client.EndAuthenticateAsClient, credential, (ChannelBinding)null, targetName, null);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            Task.Factory.FromAsync(server.BeginAuthenticateAsServer, server.EndAuthenticateAsServer, (ExtendedProtectionPolicy)null, null);
    }

    public sealed class NegotiateStreamStreamToStreamTest_BeginEnd_TestOverloadProtectionLevel : NegotiateStreamStreamToStreamTest_BeginEnd
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            Task.Factory.FromAsync(
                (callback, state) => client.BeginAuthenticateAsClient(credential, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, callback, state),
                client.EndAuthenticateAsClient, null);

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            Task.Factory.FromAsync(
                (callback, state) => server.BeginAuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, callback, state),
                server.EndAuthenticateAsServer, null);
    }

    public class NegotiateStreamStreamToStreamTest_Sync : NegotiateStreamStreamToStreamTest
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            Task.Run(() => client.AuthenticateAsClient(credential, targetName));

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            Task.Run(() => server.AuthenticateAsServer());

        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromResult(stream.Read(buffer, offset, count));

        protected override Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            stream.Write(buffer, offset, count);
            return Task.CompletedTask;
        }
    }

    public sealed class NegotiateStreamStreamToStreamTest_Sync_TestOverloadNullBinding : NegotiateStreamStreamToStreamTest_Sync
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            Task.Run(() => client.AuthenticateAsClient(credential, null, targetName));

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            Task.Run(() => server.AuthenticateAsServer(null));
    }

    public sealed class NegotiateStreamStreamToStreamTest_Sync_TestOverloadAllParameters : NegotiateStreamStreamToStreamTest_Sync
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            Task.Run(() => client.AuthenticateAsClient(credential, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification));

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            Task.Run(() => server.AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification));
    }

    public class NegotiateStreamStreamToStreamTest_Sync_NotEncrypted : NegotiateStreamStreamToStreamTest_Sync
    {
        protected override Task AuthenticateAsClientAsync(NegotiateStream client, NetworkCredential credential, string targetName) =>
            Task.Run(() => client.AuthenticateAsClient(credential, targetName, ProtectionLevel.None, TokenImpersonationLevel.Identification));

        protected override Task AuthenticateAsServerAsync(NegotiateStream server) =>
            Task.Run(() => server.AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, ProtectionLevel.None, TokenImpersonationLevel.Identification));

        protected override bool IsEncryptedAndSigned => false;
    }
}
