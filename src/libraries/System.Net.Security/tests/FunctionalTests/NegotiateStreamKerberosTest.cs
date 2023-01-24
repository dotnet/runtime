// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class NegotiateStreamKerberosTest
    {
        public static bool IsServerAndDomainAvailable =>
            Capability.IsDomainAvailable() && Capability.IsNegotiateServerAvailable();

        public static bool IsClientAvailable =>
            Capability.IsNegotiateClientAvailable();

        public static IEnumerable<object[]> GoodCredentialsData
        {
            get
            {
                yield return new object[] { CredentialCache.DefaultNetworkCredentials };
                yield return new object[] { new NetworkCredential(
                    Configuration.Security.ActiveDirectoryUserName,
                    Configuration.Security.ActiveDirectoryUserPassword,
                    Configuration.Security.ActiveDirectoryName) };

                yield return new object[] { new NetworkCredential(
                    Configuration.Security.ActiveDirectoryUserName,
                    AsSecureString(Configuration.Security.ActiveDirectoryUserPassword),
                    Configuration.Security.ActiveDirectoryName) };


                // Anonymous (with domain name).
                yield return new object[] { new NetworkCredential(
                    Configuration.Security.ActiveDirectoryUserName,
                    (string)null,
                    Configuration.Security.ActiveDirectoryName) };

                yield return new object[] { new NetworkCredential(
                    Configuration.Security.ActiveDirectoryUserName,
                    (SecureString)null,
                    Configuration.Security.ActiveDirectoryName) };

                // Anonymous (without domain).
                yield return new object[] { new NetworkCredential(
                    Configuration.Security.ActiveDirectoryUserName,
                    (string)null,
                    null) };

                yield return new object[] { new NetworkCredential(
                    Configuration.Security.ActiveDirectoryUserName,
                    (SecureString)null,
                    null) };
            }
        }

        public static IEnumerable<object[]> BadCredentialsData
        {
            get
            {
                yield return new object[] { new NetworkCredential(null, (string)null, Configuration.Security.ActiveDirectoryName) };
                yield return new object[] { new NetworkCredential(null, (SecureString)null, Configuration.Security.ActiveDirectoryName) };

                yield return new object[] { new NetworkCredential(null, (string)null, null) };
                yield return new object[] { new NetworkCredential(null, (SecureString)null, null) };

                yield return new object[] { new NetworkCredential(
                    "baduser",
                    (string)null,
                    Configuration.Security.ActiveDirectoryName) };

                yield return new object[] { new NetworkCredential(
                    "baduser",
                    (SecureString)null,
                    Configuration.Security.ActiveDirectoryName) };

                yield return new object[] { new NetworkCredential(
                    "baduser",
                    AsSecureString("badpassword"),
                    Configuration.Security.ActiveDirectoryName) };
            }
        }

        [OuterLoop]
        [ConditionalTheory(nameof(IsServerAndDomainAvailable))]
        [MemberData(nameof(GoodCredentialsData))]
        public async Task NegotiateStream_ClientAuthenticationRemote_Success(object credentialObject)
        {
            var credential = (NetworkCredential)credentialObject;
            await VerifyClientAuthentication(credential);
        }

        [OuterLoop]
        [ConditionalTheory(nameof(IsServerAndDomainAvailable))]
        [MemberData(nameof(BadCredentialsData))]
        public async Task NegotiateStream_ClientAuthenticationRemote_Fails(object credentialObject)
        {
            var credential = (NetworkCredential)credentialObject;
            await Assert.ThrowsAsync<AuthenticationException>(() => VerifyClientAuthentication(credential));
        }

        private async Task VerifyClientAuthentication(NetworkCredential credential)
        {
            string serverName = Configuration.Security.NegotiateServer.Host;
            int port = Configuration.Security.NegotiateServer.Port;
            string serverSPN = "HOST/" + serverName;
            bool isLocalhost = await IsLocalHost(serverName);

            string expectedAuthenticationType = "Kerberos";
            bool mutuallyAuthenticated = true;

            if (credential == CredentialCache.DefaultNetworkCredentials && isLocalhost)
            {
                expectedAuthenticationType = "NTLM";
            }
            else if (credential != CredentialCache.DefaultNetworkCredentials &&
                (string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password)))
            {
                // Anonymous authentication.
                expectedAuthenticationType = "NTLM";
                mutuallyAuthenticated = false;
            }

            using (var client = new TcpClient())
            {
                await client.ConnectAsync(serverName, port);

                NetworkStream clientStream = client.GetStream();
                using (var auth = new NegotiateStream(clientStream, leaveInnerStreamOpen:false))
                {
                    await auth.AuthenticateAsClientAsync(
                        credential,
                        serverSPN,
                        ProtectionLevel.EncryptAndSign,
                        System.Security.Principal.TokenImpersonationLevel.Identification);

                    Assert.Equal(expectedAuthenticationType, auth.RemoteIdentity.AuthenticationType);
                    Assert.Equal(serverSPN, auth.RemoteIdentity.Name);
                    (auth.RemoteIdentity as IDisposable)?.Dispose();

                    Assert.True(auth.IsAuthenticated);
                    Assert.True(auth.IsEncrypted);
                    Assert.Equal(mutuallyAuthenticated, auth.IsMutuallyAuthenticated);
                    Assert.True(auth.IsSigned);

                    // Send a message to the server. Encode the test data into a byte array.
                    byte[] message = "Hello from the client."u8.ToArray();
                    await auth.WriteAsync(message, 0, message.Length);
                }
            }
        }

        [OuterLoop]
        [ConditionalFact(nameof(IsClientAvailable))]
        public async Task NegotiateStream_ServerAuthenticationRemote_Success()
        {
            string expectedUser = Configuration.Security.NegotiateClientUser;

            string expectedAuthenticationType = "Kerberos";
            bool mutuallyAuthenticated = true;

            using (var controlClient = new TcpClient())
            {
                string clientName = Configuration.Security.NegotiateClient.Host;
                int clientPort = Configuration.Security.NegotiateClient.Port;
                await controlClient.ConnectAsync(clientName, clientPort)
                    .WaitAsync(TimeSpan.FromSeconds(15));
                var serverStream = controlClient.GetStream();

                using (var serverAuth = new NegotiateStream(serverStream, leaveInnerStreamOpen: false))
                {
                    await serverAuth.AuthenticateAsServerAsync(
                        CredentialCache.DefaultNetworkCredentials,
                        ProtectionLevel.EncryptAndSign,
                        TokenImpersonationLevel.Identification)
                        .WaitAsync(TimeSpan.FromSeconds(15));

                    Assert.True(serverAuth.IsAuthenticated, "IsAuthenticated");
                    Assert.True(serverAuth.IsEncrypted, "IsEncrypted");
                    Assert.True(serverAuth.IsSigned, "IsSigned");
                    Assert.Equal(mutuallyAuthenticated, serverAuth.IsMutuallyAuthenticated);

                    Assert.Equal(expectedAuthenticationType, serverAuth.RemoteIdentity.AuthenticationType);
                    Assert.Equal(expectedUser, serverAuth.RemoteIdentity.Name);
                    (serverAuth.RemoteIdentity as IDisposable)?.Dispose();

                    // Receive a message from the client.
                    var message = "Hello from the client.";
                    using (var reader = new StreamReader(serverAuth))
                    {
                        var response = await reader.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(15));
                        Assert.Equal(message, response);
                    }
                }
            }
        }

        private static async Task<bool> IsLocalHost(string hostname)
        {
            IPAddress[] hostAddresses = await Dns.GetHostAddressesAsync(hostname);
            IPAddress[] localHostAddresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());

            // Note: This returns 127.0.0.1 and ::1 but 127.0.0.0/8 which most systems consider localhost.
            IPAddress[] loopbackAddresses = await Dns.GetHostAddressesAsync("localhost");
            int isLocalHost = hostAddresses.Intersect(localHostAddresses).Count();
            int isLoopback = hostAddresses.Intersect(loopbackAddresses).Count();

            return isLocalHost + isLoopback > 0;
        }

        private static SecureString AsSecureString(string str)
        {
            SecureString secureString = new SecureString();

            if (string.IsNullOrEmpty(str))
            {
                return secureString;
            }

            foreach (char ch in str)
            {
                secureString.AppendChar(ch);
            }

            return secureString;
        }
    }
}
