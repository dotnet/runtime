// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class NegotiateStreamKerberosTest
    {
        public static bool IsServerAndDomainAvailable => 
            Capability.IsDomainAvailable() && Capability.IsNegotiateServerAvailable();
        
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
        public async Task NegotiateStream_AuthenticationRemote_Success(object credentialObject)
        {
            var credential = (NetworkCredential)credentialObject;
            await VerifyAuthentication(credential);
        }

        [OuterLoop]
        [ConditionalTheory(nameof(IsServerAndDomainAvailable))]
        [MemberData(nameof(BadCredentialsData))]
        public async Task NegotiateStream_AuthenticationRemote_Fails(object credentialObject)
        {
            var credential = (NetworkCredential)credentialObject;
            await Assert.ThrowsAsync<AuthenticationException>(() => VerifyAuthentication(credential));
        }

        private async Task VerifyAuthentication(NetworkCredential credential)
        {
            string serverName = Configuration.Security.NegotiateServer.Host;
            int port = Configuration.Security.NegotiateServer.Port;
            string serverSPN = "HOST/" + serverName;
            bool isLocalhost = await IsLocalHost(serverName);
            
            string expectedAuthenticationType = "Kerberos";
            bool mutualAuthenitcated = true;

            if (credential == CredentialCache.DefaultNetworkCredentials && isLocalhost)
            {
                expectedAuthenticationType = "NTLM";
            }
            else if (credential != CredentialCache.DefaultNetworkCredentials && 
                (string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password)))
            {
                // Anonymous authentication.
                expectedAuthenticationType = "NTLM";
                mutualAuthenitcated = false;
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

                    Assert.Equal(true, auth.IsAuthenticated);
                    Assert.Equal(true, auth.IsEncrypted);
                    Assert.Equal(mutualAuthenitcated, auth.IsMutuallyAuthenticated);
                    Assert.Equal(true, auth.IsSigned);

                    // Send a message to the server. Encode the test data into a byte array.
                    byte[] message = Encoding.UTF8.GetBytes("Hello from the client.");
                    await auth.WriteAsync(message, 0, message.Length);
                }
            }
        }

        private async static Task<bool> IsLocalHost(string hostname)
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
