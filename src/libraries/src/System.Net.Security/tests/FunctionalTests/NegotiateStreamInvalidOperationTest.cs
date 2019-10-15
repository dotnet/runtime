// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)] // NegotiateStream only supports client-side functionality on Unix
    public class NegotiateStreamInvalidOperationTest
    {
        private static readonly byte[] s_sampleMsg = Encoding.UTF8.GetBytes("Sample Test Message");
        private const string TargetName = "testTargetName";

        [Fact]
        public async Task NegotiateStream_StreamContractTest_Success()
        {
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                Assert.False(client.CanSeek);
                Assert.False(client.CanRead);
                Assert.False(client.CanTimeout);
                Assert.False(client.CanWrite);
                Assert.False(server.CanSeek);
                Assert.False(server.CanRead);
                Assert.False(server.CanTimeout);
                Assert.False(server.CanWrite);

                Assert.Throws<InvalidOperationException>(() => client.ReadTimeout);
                Assert.Throws<InvalidOperationException>(() => client.WriteTimeout);
                Assert.Throws<NotImplementedException>(() => client.Length);
                Assert.Throws<NotImplementedException>(() => client.Position);
                Assert.Throws<NotSupportedException>(() => client.Seek(0, new SeekOrigin()));

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(),
                    server.AuthenticateAsServerAsync());

                Assert.True(client.CanRead);
                Assert.True(client.CanWrite);
                Assert.True(server.CanRead);
                Assert.True(server.CanWrite);
            }
        }

        [Fact]
        public async Task NegotiateStream_EndReadEndWriteInvalidParameter_Throws()
        {
            byte[] recvBuf = new byte[s_sampleMsg.Length];
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, string.Empty),
                    server.AuthenticateAsServerAsync());

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    Task.Factory.FromAsync(client.BeginWrite,
                        (asyncResult) =>
                        {
                            NegotiateStream authStream = (NegotiateStream)asyncResult.AsyncState;
                            AssertExtensions.Throws<ArgumentNullException>(nameof(asyncResult), () => authStream.EndWrite(null));

                            IAsyncResult result = new MyAsyncResult();
                            AssertExtensions.Throws<ArgumentException>(nameof(asyncResult), () => authStream.EndWrite(result));
                        },
                        s_sampleMsg, 0, s_sampleMsg.Length, client),
                    Task.Factory.FromAsync(server.BeginRead,
                        (asyncResult) =>
                        {
                            NegotiateStream authStream = (NegotiateStream)asyncResult.AsyncState;
                            AssertExtensions.Throws<ArgumentNullException>(nameof(asyncResult), () => authStream.EndRead(null));

                            IAsyncResult result = new MyAsyncResult();
                            AssertExtensions.Throws<ArgumentException>(nameof(asyncResult), () => authStream.EndRead(result));
                        },
                        recvBuf, 0, s_sampleMsg.Length, server));
            }
        }

        [Fact]
        public async Task NegotiateStream_ConcurrentAsyncReadOrWrite_ThrowsNotSupportedException()
        {
            byte[] recvBuf = new byte[s_sampleMsg.Length];
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, string.Empty),
                    server.AuthenticateAsServerAsync());

                // Custom EndWrite/Read will not reset the variable which monitors concurrent write/read.
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    Task.Factory.FromAsync(client.BeginWrite, (ar) => { Assert.NotNull(ar); }, s_sampleMsg, 0, s_sampleMsg.Length, client),
                    Task.Factory.FromAsync(server.BeginRead, (ar) => { Assert.NotNull(ar); }, recvBuf, 0, s_sampleMsg.Length, server));

                Assert.Throws<NotSupportedException>(() => client.BeginWrite(s_sampleMsg, 0, s_sampleMsg.Length, (ar) => { Assert.Null(ar); }, null));
                Assert.Throws<NotSupportedException>(() => server.BeginRead(recvBuf, 0, s_sampleMsg.Length, (ar) => { Assert.Null(ar); }, null));
            }
        }

        [Fact]
        public async Task NegotiateStream_ConcurrentSyncReadOrWrite_ThrowsNotSupportedException()
        {
            byte[] recvBuf = new byte[s_sampleMsg.Length];
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, string.Empty),
                    server.AuthenticateAsServerAsync());

                // Custom EndWrite/Read will not reset the variable which monitors concurrent write/read.
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    Task.Factory.FromAsync(client.BeginWrite, (ar) => { Assert.NotNull(ar); }, s_sampleMsg, 0, s_sampleMsg.Length, client),
                    Task.Factory.FromAsync(server.BeginRead, (ar) => { Assert.NotNull(ar); }, recvBuf, 0, s_sampleMsg.Length, server));

                Assert.Throws<NotSupportedException>(() => client.Write(s_sampleMsg, 0, s_sampleMsg.Length));
                Assert.Throws<NotSupportedException>(() => server.Read(recvBuf, 0, s_sampleMsg.Length));
            }
        }

        [Fact]
        public async Task NegotiateStream_DisposeTooEarly_Throws()
        {
            byte[] recvBuf = new byte[s_sampleMsg.Length];
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(),
                    server.AuthenticateAsServerAsync());

                server.Dispose();
                Assert.Throws<IOException>(() => client.Write(s_sampleMsg, 0, s_sampleMsg.Length));
                Assert.Throws<IOException>(() => client.Read(recvBuf, 0, s_sampleMsg.Length));
            }
        }

        [Fact]
        public void NegotiateStream_InvalidPolicy_Throws()
        {
            var network = new VirtualNetwork();
            var policy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                // If ExtendedProtection is on, either CustomChannelBinding or CustomServiceNames must be set.
                AssertExtensions.Throws<ArgumentException>(nameof(policy), () => server.AuthenticateAsServer(policy));
            }
        }

        [Fact]
        public async Task NegotiateStream_TokenImpersonationLevelRequirmentNotMatch_Throws()
        {
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    Assert.ThrowsAsync<AuthenticationException>(() =>
                        client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, string.Empty)),
                    // We suppress the Delegation flag in NTLM case.
                    Assert.ThrowsAsync<AuthenticationException>(() =>
                        server.AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials,
                            null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Delegation)));
            }
        }

        [Fact]
        public async Task NegotiateStream_SPNRequirmentNotMeet_Throws()
        {
            var network = new VirtualNetwork();
            var snc = new List<string>
            {
                "serviceName"
            };
            // PolicyEnforcement.Always will force clientSpn check.
            var policy = new ExtendedProtectionPolicy(PolicyEnforcement.Always, ProtectionScenario.TransportSelected, new ServiceNameCollection(snc));

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    Assert.ThrowsAsync<AuthenticationException>(() => client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, string.Empty)),
                    Assert.ThrowsAsync<AuthenticationException>(() => server.AuthenticateAsServerAsync(policy)));
            }
        }

        [Fact]
        public void NegotiateStream_DisposedState_Throws()
        {
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                client.Dispose();
                Assert.Throws<ObjectDisposedException>(() => client.AuthenticateAsClient());
            }
        }

        [Fact]
        public async Task NegotiateStream_DoubleAuthentication_Throws()
        {
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(),
                    server.AuthenticateAsServerAsync());

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    Assert.ThrowsAsync<InvalidOperationException>(() => client.AuthenticateAsClientAsync()),
                    Assert.ThrowsAsync<InvalidOperationException>(() => server.AuthenticateAsServerAsync()));
            }
        }

        [Fact]
        public void NegotiateStream_NullCredential_Throws()
        {
            NetworkCredential credential = null;
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                AssertExtensions.Throws<ArgumentNullException>(nameof(credential), () => client.AuthenticateAsClient(null, TargetName));
            }
        }

        [Fact]
        public void NegotiateStream_NullServicePrincipalName_Throws()
        {
            string servicePrincipalName = null;
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                AssertExtensions.Throws<ArgumentNullException>(nameof(servicePrincipalName),
                    () => client.AuthenticateAsClient(CredentialCache.DefaultNetworkCredentials, servicePrincipalName));
            }
        }

        [Fact]
        public async Task NegotiateStream_SecurityRequirmentNotMeet_Throws()
        {
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                // ProtectionLevel not match.
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    Assert.ThrowsAsync<AuthenticationException>(() =>
                        client.AuthenticateAsClientAsync((NetworkCredential)CredentialCache.DefaultCredentials,
                            TargetName, ProtectionLevel.None, TokenImpersonationLevel.Identification)),
                    Assert.ThrowsAsync<AuthenticationException>(() =>
                        server.AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials,
                            ProtectionLevel.Sign, TokenImpersonationLevel.Identification)));

                Assert.Throws<AuthenticationException>(() => client.Write(s_sampleMsg, 0, s_sampleMsg.Length));
            }
        }

        [Fact]
        public async Task NegotiateStream_EndAuthenticateInvalidParameter_Throws()
        {
            var network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    Task.Factory.FromAsync(client.BeginAuthenticateAsClient, (asyncResult) =>
                    {
                        NegotiateStream authStream = (NegotiateStream)asyncResult.AsyncState;
                        AssertExtensions.Throws<ArgumentNullException>(nameof(asyncResult), () => authStream.EndAuthenticateAsClient(null));

                        IAsyncResult result = new MyAsyncResult();
                        AssertExtensions.Throws<ArgumentException>(nameof(asyncResult), () => authStream.EndAuthenticateAsClient(result));

                        authStream.EndAuthenticateAsClient(asyncResult);
                        Assert.Throws<InvalidOperationException>(() => authStream.EndAuthenticateAsClient(asyncResult));
                    }, CredentialCache.DefaultNetworkCredentials, string.Empty, client),

                    Task.Factory.FromAsync(server.BeginAuthenticateAsServer, (asyncResult) =>
                    {
                        NegotiateStream authStream = (NegotiateStream)asyncResult.AsyncState;
                        AssertExtensions.Throws<ArgumentNullException>(nameof(asyncResult), () => authStream.EndAuthenticateAsServer(null));

                        IAsyncResult result = new MyAsyncResult();
                        AssertExtensions.Throws<ArgumentException>(nameof(asyncResult), () => authStream.EndAuthenticateAsServer(result));

                        authStream.EndAuthenticateAsServer(asyncResult);
                        Assert.Throws<InvalidOperationException>(() => authStream.EndAuthenticateAsServer(asyncResult));
                    }, server));
            }
        }

        [Fact]
        public async Task NegotiateStream_InvalidParametersForReadWrite_Throws()
        {
            var network = new VirtualNetwork();
            byte[] buffer = s_sampleMsg;
            int offset = 0;
            int count = s_sampleMsg.Length;

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                // Need to do authentication first, because Read/Write operation
                // is only allowed using a successfully authenticated context.
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, string.Empty),
                    server.AuthenticateAsServerAsync());

                // Null buffer.
                AssertExtensions.Throws<ArgumentNullException>(nameof(buffer), () => client.Write(null, offset, count));

                // Negative offset.
                AssertExtensions.Throws<ArgumentOutOfRangeException>(nameof(offset), () => client.Write(buffer, -1, count));

                // Negative count.
                AssertExtensions.Throws<ArgumentOutOfRangeException>(nameof(count), () => client.Write(buffer, offset, -1));

                // Invalid offset and count combination.
                AssertExtensions.Throws<ArgumentOutOfRangeException>(nameof(count), () => client.Write(buffer, offset, count + count));

                // Null buffer.
                AssertExtensions.Throws<ArgumentNullException>(nameof(buffer), () => server.Read(null, offset, count));

                // Negative offset.
                AssertExtensions.Throws<ArgumentOutOfRangeException>(nameof(offset), () => server.Read(buffer, -1, count));

                // Negative count.
                AssertExtensions.Throws<ArgumentOutOfRangeException>(nameof(count), () => server.Read(buffer, offset, -1));

                // Invalid offset and count combination.
                AssertExtensions.Throws<ArgumentOutOfRangeException>(nameof(count), () => server.Read(buffer, offset, count + count));
            }
        }

        private class MyAsyncResult : IAsyncResult
        {
            public bool IsCompleted
            {
                get { return true; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { throw new NotImplementedException(); }
            }

            public object AsyncState
            {
                get { return null; }
            }

            public bool CompletedSynchronously
            {
                get { return true; }
            }
        }
    }
}
