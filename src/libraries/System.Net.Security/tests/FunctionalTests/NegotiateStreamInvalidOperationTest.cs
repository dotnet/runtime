// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private static readonly byte[] s_sampleMsg = "Sample Test Message"u8.ToArray();
        private const string TargetName = "testTargetName";

        [Fact]
        public async Task NegotiateStream_StreamContractTest_Success()
        {
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (var client = new NegotiateStream(clientStream))
            using (var server = new NegotiateStream(serverStream))
            {
                Assert.False(client.CanSeek);
                Assert.False(client.CanRead);
                Assert.Equal(clientStream.CanTimeout, client.CanTimeout);
                Assert.False(client.CanWrite);
                Assert.False(server.CanSeek);
                Assert.False(server.CanRead);
                Assert.Equal(serverStream.CanTimeout, server.CanTimeout);
                Assert.False(server.CanWrite);

                if (!client.CanTimeout)
                {
                    Assert.Throws<InvalidOperationException>(() => client.ReadTimeout);
                }

                if (!server.CanTimeout)
                {
                    Assert.Throws<InvalidOperationException>(() => client.WriteTimeout);
                }

                Assert.Throws<NotSupportedException>(() => client.Length);
                Assert.Throws<NotSupportedException>(() => client.Position);
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
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
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
        public void NegotiateStream_InvalidPolicy_Throws()
        {
            var policy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                // If ExtendedProtection is on, either CustomChannelBinding or CustomServiceNames must be set.
                AssertExtensions.Throws<ArgumentException>(nameof(policy), () => server.AuthenticateAsServer(policy));
            }
        }

        [Fact]
        public async Task NegotiateStream_TokenImpersonationLevelRequirmentNotMatch_Throws()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
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
            var snc = new List<string>
            {
                "serviceName"
            };
            // PolicyEnforcement.Always will force clientSpn check.
            var policy = new ExtendedProtectionPolicy(PolicyEnforcement.Always, ProtectionScenario.TransportSelected, new ServiceNameCollection(snc));

            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    Assert.ThrowsAsync<AuthenticationException>(() => client.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, string.Empty)),
                    Assert.ThrowsAsync<AuthenticationException>(() => server.AuthenticateAsServerAsync(policy)));
            }
        }

        [Fact]
        public void NegotiateStream_DisposedState_Throws()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                client.Dispose();
                Assert.Throws<ObjectDisposedException>(() => client.AuthenticateAsClient());
            }
        }

        [Fact]
        public async Task NegotiateStream_DoubleAuthentication_Throws()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
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
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                AssertExtensions.Throws<ArgumentNullException>("credential", () => client.AuthenticateAsClient(null, TargetName));
            }
        }

        [Fact]
        public void NegotiateStream_NullServicePrincipalName_Throws()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                AssertExtensions.Throws<ArgumentNullException>("servicePrincipalName", () => client.AuthenticateAsClient(CredentialCache.DefaultNetworkCredentials, null));
            }
        }

        [Fact]
        public async Task NegotiateStream_SecurityRequirmentNotMeet_Throws()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
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
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    Task.Factory.FromAsync(client.BeginAuthenticateAsClient, (asyncResult) =>
                    {
                        NegotiateStream authStream = (NegotiateStream)asyncResult.AsyncState;
                        AssertExtensions.Throws<ArgumentNullException>(nameof(asyncResult), () => authStream.EndAuthenticateAsClient(null));

                        IAsyncResult result = new MyAsyncResult();
                        AssertExtensions.Throws<ArgumentException>(nameof(asyncResult), () => authStream.EndAuthenticateAsClient(result));

                        authStream.EndAuthenticateAsClient(asyncResult);
                    }, CredentialCache.DefaultNetworkCredentials, string.Empty, client),

                    Task.Factory.FromAsync(server.BeginAuthenticateAsServer, (asyncResult) =>
                    {
                        NegotiateStream authStream = (NegotiateStream)asyncResult.AsyncState;
                        AssertExtensions.Throws<ArgumentNullException>(nameof(asyncResult), () => authStream.EndAuthenticateAsServer(null));

                        IAsyncResult result = new MyAsyncResult();
                        AssertExtensions.Throws<ArgumentException>(nameof(asyncResult), () => authStream.EndAuthenticateAsServer(result));

                        authStream.EndAuthenticateAsServer(asyncResult);
                    }, server));
            }
        }

        [Fact]
        public async Task NegotiateStream_InvalidParametersForReadWrite_Throws()
        {
            byte[] buffer = s_sampleMsg;
            int offset = 0;
            int count = s_sampleMsg.Length;

            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
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
