// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Security.Enterprise.Tests
{
    [ConditionalClass(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
    public class NegotiateAuthenticationTest
    {
        static NegotiateAuthenticationTest()
        {
            // Obtain a Kerberos TGT so that DefaultNetworkCredentials tests can work.
            // Other tests pass explicit credentials but DefaultCredentials needs a cached ticket.
            try
            {
                NetworkCredential creds = EnterpriseTestConfiguration.ValidNetworkCredentials;
                using var process = new Process();
                process.StartInfo.FileName = "kinit";
                process.StartInfo.Arguments = $"{creds.UserName}@{EnterpriseTestConfiguration.Realm}";
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.StandardInput.WriteLine(creds.Password);
                process.StandardInput.Close();
                process.WaitForExit(10_000);
            }
            catch
            {
                // kinit may not be available; the test will skip gracefully.
            }
        }

        public static TheoryData<NetworkCredential, string> AuthenticationSuccessCases => new TheoryData<NetworkCredential, string>
        {
            { EnterpriseTestConfiguration.ValidNetworkCredentials, "HOST/localhost" },
            { EnterpriseTestConfiguration.ValidNetworkCredentials, "HOST/linuxclient.linux.contoso.com" },
            { EnterpriseTestConfiguration.ValidNetworkCredentials, "HTTP/apacheweb.linux.contoso.com" },
            { EnterpriseTestConfiguration.ValidNetworkCredentials, "HTTP/apacheweb.linux.contoso.com@LINUX.CONTOSO.COM" },
        };

        public static TheoryData<NetworkCredential, string> LoopbackAuthenticationSuccessCases => new TheoryData<NetworkCredential, string>
        {
            { EnterpriseTestConfiguration.ValidNetworkCredentials, "HOST/localhost" },
            { EnterpriseTestConfiguration.ValidNetworkCredentials, "HOST/linuxclient.linux.contoso.com" },
        };

        [Theory]
        [MemberData(nameof(AuthenticationSuccessCases))]
        public void ClientAuthentication_ValidCredentials_Succeeds(NetworkCredential credential, string targetName)
        {
            using var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
            {
                Package = "Negotiate",
                Credential = credential,
                TargetName = targetName,
            });

            NegotiateAuthenticationStatusCode statusCode;
            byte[]? token = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out statusCode);

            Assert.NotNull(token);
            Assert.True(token.Length > 0);
            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
            Assert.Equal("Negotiate", client.Package);
        }

        [ConditionalTheory]
        [InlineData("HOST/localhost")]
        [InlineData("HOST/linuxclient.linux.contoso.com")]
        public void ClientAuthentication_DefaultCredentials_Succeeds(string targetName)
        {
            using var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
            {
                Package = "Negotiate",
                Credential = CredentialCache.DefaultNetworkCredentials,
                TargetName = targetName,
            });

            using var server = new NegotiateAuthentication(new NegotiateAuthenticationServerOptions
            {
                Package = "Negotiate",
            });

            NegotiateAuthenticationStatusCode clientStatus;
            byte[]? clientToken = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out clientStatus);

            if (clientToken is null)
            {
                throw new SkipTestException("Kerberos TGT is not available (kinit not run).");
            }

            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, clientStatus);

            AssertMutualAuthenticationCompleted(client, server, clientToken);
        }

        [Theory]
        [MemberData(nameof(LoopbackAuthenticationSuccessCases))]
        public void ClientServerAuthentication_Succeeds(NetworkCredential credential, string targetName)
        {
            using var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
            {
                Package = "Negotiate",
                Credential = credential,
                TargetName = targetName,
            });

            using var server = new NegotiateAuthentication(new NegotiateAuthenticationServerOptions
            {
                Package = "Negotiate",
            });

            AssertMutualAuthenticationCompleted(client, server);
        }

        [Theory]
        [MemberData(nameof(LoopbackAuthenticationSuccessCases))]
        public void ClientServerAuthentication_WrapUnwrap_Succeeds(NetworkCredential credential, string targetName)
        {
            using var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
            {
                Package = "Negotiate",
                Credential = credential,
                TargetName = targetName,
                RequiredProtectionLevel = ProtectionLevel.EncryptAndSign,
            });

            using var server = new NegotiateAuthentication(new NegotiateAuthenticationServerOptions
            {
                Package = "Negotiate",
                RequiredProtectionLevel = ProtectionLevel.EncryptAndSign,
            });

            AssertMutualAuthenticationCompleted(client, server, assertContinueNeeded: false);

            byte[] message = "Hello from client"u8.ToArray();
            ArrayBufferWriter<byte> wrappedWriter = new ArrayBufferWriter<byte>();
            Assert.Equal(NegotiateAuthenticationStatusCode.Completed, client.Wrap(message, wrappedWriter, true, out bool isEncrypted));
            Assert.True(isEncrypted);

            ArrayBufferWriter<byte> unwrappedWriter = new ArrayBufferWriter<byte>();
            Assert.Equal(NegotiateAuthenticationStatusCode.Completed, server.Unwrap(wrappedWriter.WrittenSpan, unwrappedWriter, out bool wasEncrypted));
            Assert.True(wasEncrypted);
            Assert.Equal(message, unwrappedWriter.WrittenSpan.ToArray());
        }

        [Fact]
        public void ClientAuthentication_InvalidCredentials_Fails()
        {
            using var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
            {
                Package = "Negotiate",
                Credential = EnterpriseTestConfiguration.InvalidNetworkCredentials,
                TargetName = "HOST/localhost",
            });

            using var server = new NegotiateAuthentication(new NegotiateAuthenticationServerOptions
            {
                Package = "Negotiate",
            });

            NegotiateAuthenticationStatusCode clientStatus, serverStatus;
            byte[]? clientToken = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out clientStatus);

            if (clientStatus >= NegotiateAuthenticationStatusCode.GenericFailure)
            {
                // Authentication failed at the first step, which is acceptable.
                return;
            }

            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, clientStatus);
            Assert.NotNull(clientToken);

            bool authFailed = false;
            const int MaxIterations = 20;
            for (int i = 0; i < MaxIterations; i++)
            {
                byte[]? serverToken = server.GetOutgoingBlob(clientToken, out serverStatus);
                if (serverStatus >= NegotiateAuthenticationStatusCode.GenericFailure)
                {
                    authFailed = true;
                    break;
                }
                if (serverStatus == NegotiateAuthenticationStatusCode.Completed)
                {
                    break;
                }

                clientToken = client.GetOutgoingBlob(serverToken!, out clientStatus);
                if (clientStatus >= NegotiateAuthenticationStatusCode.GenericFailure)
                {
                    authFailed = true;
                    break;
                }
                if (clientStatus == NegotiateAuthenticationStatusCode.Completed)
                {
                    break;
                }
            }

            Assert.True(authFailed || !server.IsAuthenticated,
                "Authentication should have failed with invalid credentials");
        }

        [Theory]
        [InlineData(ProtectionLevel.Sign)]
        [InlineData(ProtectionLevel.EncryptAndSign)]
        public void ClientServerAuthentication_ProtectionLevel_Succeeds(ProtectionLevel protectionLevel)
        {
            using var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
            {
                Package = "Negotiate",
                Credential = EnterpriseTestConfiguration.ValidNetworkCredentials,
                TargetName = "HOST/linuxclient.linux.contoso.com",
                RequiredProtectionLevel = protectionLevel,
            });

            using var server = new NegotiateAuthentication(new NegotiateAuthenticationServerOptions
            {
                Package = "Negotiate",
            });

            AssertMutualAuthenticationCompleted(client, server, assertContinueNeeded: false);

            Assert.True(client.IsSigned);
            if (protectionLevel == ProtectionLevel.EncryptAndSign)
            {
                Assert.True(client.IsEncrypted);
            }
        }

        [Theory]
        [InlineData("HOST/linuxclient.linux.contoso.com")]
        [InlineData("HTTP/apacheweb.linux.contoso.com")]
        public void ClientAuthentication_TargetName_ReturnsCorrectSPN(string targetName)
        {
            using var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
            {
                Package = "Negotiate",
                Credential = EnterpriseTestConfiguration.ValidNetworkCredentials,
                TargetName = targetName,
            });

            Assert.Equal(targetName, client.TargetName);
        }

        [Fact]
        public async Task ClientServerAuthentication_AgainstWebServer_Succeeds()
        {
            string targetName = "HTTP/apacheweb.linux.contoso.com";
            using var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
            {
                Package = "Negotiate",
                Credential = EnterpriseTestConfiguration.ValidNetworkCredentials,
                TargetName = targetName,
            });

            byte[]? clientToken = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out NegotiateAuthenticationStatusCode clientStatus);
            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, clientStatus);
            byte[] nonNullClientToken = Assert.IsType<byte[]>(clientToken);

            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, EnterpriseTestConfiguration.NegotiateAuthWebServer);
            request.Headers.Authorization = new AuthenticationHeaderValue("Negotiate", Convert.ToBase64String(nonNullClientToken));

            using HttpResponseMessage response = await httpClient.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string? serverAuthHeader = response.Headers.WwwAuthenticate.ToString();
            if (!string.IsNullOrEmpty(serverAuthHeader) && serverAuthHeader.StartsWith("Negotiate ", StringComparison.OrdinalIgnoreCase))
            {
                byte[] serverToken = Convert.FromBase64String(serverAuthHeader.Substring("Negotiate ".Length));
                client.GetOutgoingBlob(serverToken, out clientStatus);
            }

            Assert.True(client.IsAuthenticated);
        }

        [Fact]
        public async Task ClientServerAuthentication_AgainstWebServer_WithRealmHint_Succeeds()
        {
            string targetName = "HTTP/apacheweb.linux.contoso.com@LINUX.CONTOSO.COM";
            using var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
            {
                Package = "Negotiate",
                Credential = EnterpriseTestConfiguration.ValidNetworkCredentials,
                TargetName = targetName,
            });

            byte[]? clientToken = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out NegotiateAuthenticationStatusCode clientStatus);
            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, clientStatus);
            byte[] nonNullClientToken = Assert.IsType<byte[]>(clientToken);

            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, EnterpriseTestConfiguration.NegotiateAuthWebServer);
            request.Headers.Authorization = new AuthenticationHeaderValue("Negotiate", Convert.ToBase64String(nonNullClientToken));

            using HttpResponseMessage response = await httpClient.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string? serverAuthHeader = response.Headers.WwwAuthenticate.ToString();
            if (!string.IsNullOrEmpty(serverAuthHeader) && serverAuthHeader.StartsWith("Negotiate ", StringComparison.OrdinalIgnoreCase))
            {
                byte[] serverToken = Convert.FromBase64String(serverAuthHeader.Substring("Negotiate ".Length));
                client.GetOutgoingBlob(serverToken, out clientStatus);
            }

            Assert.True(client.IsAuthenticated);
        }

        private static void AssertMutualAuthenticationCompleted(
            NegotiateAuthentication client,
            NegotiateAuthentication server,
            byte[]? initialClientToken = null,
            bool assertContinueNeeded = true)
        {
            NegotiateAuthenticationStatusCode clientStatus;
            byte[]? clientToken = initialClientToken;

            if (clientToken is null)
            {
                clientToken = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out clientStatus);
                if (assertContinueNeeded)
                {
                    Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, clientStatus);
                    Assert.NotNull(clientToken);
                }
            }

            NegotiateAuthenticationStatusCode serverStatus;
            const int MaxIterations = 20;

            for (int i = 0; i < MaxIterations; i++)
            {
                byte[]? serverToken = server.GetOutgoingBlob(clientToken!, out serverStatus);
                if (serverStatus == NegotiateAuthenticationStatusCode.Completed)
                {
                    if (serverToken is not null)
                    {
                        client.GetOutgoingBlob(serverToken, out clientStatus);
                    }
                    break;
                }

                if (assertContinueNeeded)
                {
                    Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, serverStatus);
                    Assert.NotNull(serverToken);
                }

                clientToken = client.GetOutgoingBlob(serverToken!, out clientStatus);
                if (clientStatus == NegotiateAuthenticationStatusCode.Completed)
                {
                    break;
                }

                if (assertContinueNeeded)
                {
                    Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, clientStatus);
                    Assert.NotNull(clientToken);
                }
            }

            Assert.True(client.IsAuthenticated);
            Assert.True(server.IsAuthenticated);
        }
    }
}
