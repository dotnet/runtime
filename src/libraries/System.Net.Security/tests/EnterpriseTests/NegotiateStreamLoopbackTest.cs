// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Enterprise.Tests
{
    [ConditionalClass(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
    public class NegotiateStreamLoopbackTest
    {
        private const int TimeoutMilliseconds = 4 * 60 * 1000;

        private static Task WhenAllOrAnyFailedWithTimeout(params Task[] tasks) =>
            tasks.WhenAllOrAnyFailed(TimeoutMilliseconds);

        private const string TargetName = "HOST/linuxclient.linux.contoso.com";
        private const int PartialBytesToRead = 5;
        private static readonly byte[] s_sampleMsg = "Sample Test Message"u8.ToArray();

        private const int MaxWriteDataSize = 63 * 1024; // NegoState.MaxWriteDataSize
        private static string s_longString = new string('A', MaxWriteDataSize) + 'Z';
        private static readonly byte[] s_longMsg = Encoding.ASCII.GetBytes(s_longString);

        public static readonly object[][] SuccessCasesMemberData =
        {
            new object[] { EnterpriseTestConfiguration.ValidNetworkCredentials, "HOST/localhost" },
            new object[] { EnterpriseTestConfiguration.ValidNetworkCredentials, "HOST/linuxclient.linux.contoso.com" }
        };

        [Theory]
        [MemberData(nameof(SuccessCasesMemberData))]
        public async Task StreamToStream_ValidAuthentication_Success(NetworkCredential creds, string target)
        {
            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                Assert.False(client.IsAuthenticated);
                Assert.False(server.IsAuthenticated);

                Task[] auth = new Task[2];
                auth[0] = client.AuthenticateAsClientAsync(creds, target);
                auth[1] = server.AuthenticateAsServerAsync();

                await WhenAllOrAnyFailedWithTimeout(auth);

                VerifyStreamProperties(client, isServer: false, target);

                string remoteName = creds.UserName + "@" + EnterpriseTestConfiguration.Realm;
                VerifyStreamProperties(server, isServer: true, remoteName);
            }
        }

        public static readonly object[][] FailureCasesMemberData =
        {
            // Using a valid credential but trying to connect to the server using
            // the 'NEWSERVICE/localhost' SPN is not valid. That SPN, while registered in the overall
            // Kerberos realm, is not registered on this particular server's keytab. So, this test case verifies
            // that SPNEGO won't fallback from Kerberos to NTLM. Instead, it causes an AuthenticationException.
            new object[] { EnterpriseTestConfiguration.ValidNetworkCredentials, "NEWSERVICE/localhost" },

            // Invalid Kerberos credential password.
            new object[] { EnterpriseTestConfiguration.InvalidNetworkCredentials, "HOST/localhost" },
        };

        [Theory]
        [MemberData(nameof(FailureCasesMemberData))]
        public async Task StreamToStream_InvalidAuthentication_Failure(NetworkCredential creds, string target)
        {
            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                Assert.False(client.IsAuthenticated);
                Assert.False(server.IsAuthenticated);

                Task clientTask = client.AuthenticateAsClientAsync(creds, target);

                await Assert.ThrowsAsync<AuthenticationException>(() => server.AuthenticateAsServerAsync());
            }
        }

        [Fact]
        public async Task NegotiateStream_StreamToStream_Successive_ClientWrite_Sync_Success()
        {
            byte[] recvBuf = new byte[s_sampleMsg.Length];
            int bytesRead = 0;

            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                Assert.False(client.IsAuthenticated);
                Assert.False(server.IsAuthenticated);

                Task[] auth = new Task[2];
                auth[0] = client.AuthenticateAsClientAsync(EnterpriseTestConfiguration.ValidNetworkCredentials, TargetName);
                auth[1] = server.AuthenticateAsServerAsync();

                await WhenAllOrAnyFailedWithTimeout(auth);

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

        [Fact]
        public async Task NegotiateStream_StreamToStream_Successive_ClientWrite_Async_Success()
        {
            byte[] recvBuf = new byte[s_sampleMsg.Length];
            int bytesRead = 0;

            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional();
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                Assert.False(client.IsAuthenticated);
                Assert.False(server.IsAuthenticated);

                Task[] auth = new Task[2];
                auth[0] = client.AuthenticateAsClientAsync(EnterpriseTestConfiguration.ValidNetworkCredentials, TargetName);
                auth[1] = server.AuthenticateAsServerAsync();

                await WhenAllOrAnyFailedWithTimeout(auth);

                auth[0] = client.WriteAsync(s_sampleMsg, 0, s_sampleMsg.Length);
                auth[1] = server.ReadAsync(recvBuf, 0, s_sampleMsg.Length);
                await WhenAllOrAnyFailedWithTimeout(auth);
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

        [Fact]
        public async Task NegotiateStream_ReadWriteLongMsgSync_Success()
        {
            byte[] recvBuf = new byte[s_longMsg.Length];
            int bytesRead = 0;

            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional(4096, int.MaxValue);
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                await WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(EnterpriseTestConfiguration.ValidNetworkCredentials, TargetName),
                    server.AuthenticateAsServerAsync());

                client.Write(s_longMsg, 0, s_longMsg.Length);

                while (bytesRead < s_longMsg.Length)
                {
                    bytesRead += server.Read(recvBuf, bytesRead, s_longMsg.Length - bytesRead);
                }

                Assert.True(s_longMsg.SequenceEqual(recvBuf));
            }
        }

        [Fact]
        public async Task NegotiateStream_ReadWriteLongMsgAsync_Success()
        {
            byte[] recvBuf = new byte[s_longMsg.Length];
            int bytesRead = 0;

            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional(4096, int.MaxValue);
            using (var client = new NegotiateStream(stream1))
            using (var server = new NegotiateStream(stream2))
            {
                await WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(EnterpriseTestConfiguration.ValidNetworkCredentials, TargetName),
                    server.AuthenticateAsServerAsync());

                await client.WriteAsync(s_longMsg, 0, s_longMsg.Length);

                while (bytesRead < s_longMsg.Length)
                {
                    bytesRead += await server.ReadAsync(recvBuf, bytesRead, s_longMsg.Length - bytesRead);
                }

                Assert.True(s_longMsg.SequenceEqual(recvBuf));
            }
        }

        private void VerifyStreamProperties(NegotiateStream stream, bool isServer, string remoteName)
        {
            Assert.True(stream.IsAuthenticated);
            Assert.Equal(TokenImpersonationLevel.Identification, stream.ImpersonationLevel);
            Assert.True(stream.IsEncrypted);
            Assert.True(stream.IsMutuallyAuthenticated);
            Assert.Equal(isServer, stream.IsServer);
            Assert.True(stream.IsSigned);
            Assert.False(stream.LeaveInnerStreamOpen);

            IIdentity remoteIdentity = stream.RemoteIdentity;
            Assert.Equal("Kerberos", remoteIdentity.AuthenticationType);
            Assert.True(remoteIdentity.IsAuthenticated);
            Assert.Equal(remoteName, remoteIdentity.Name);
        }
    }
}
