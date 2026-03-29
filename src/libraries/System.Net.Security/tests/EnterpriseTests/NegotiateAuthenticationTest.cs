// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Net.Test.Common;
using System.Security.Principal;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Enterprise.Tests
{
    [ConditionalClass(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
    public class NegotiateAuthenticationTest
    {
        public static TheoryData<NetworkCredential, string> AuthenticationSuccessCases => new TheoryData<NetworkCredential, string>
        {
            { EnterpriseTestConfiguration.ValidNetworkCredentials, "HOST/localhost" },
            { EnterpriseTestConfiguration.ValidNetworkCredentials, "HOST/linuxclient.linux.contoso.com" },
            { EnterpriseTestConfiguration.ValidNetworkCredentials, "HTTP/apacheweb.linux.contoso.com" },
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

        [Theory]
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

            NegotiateAuthenticationStatusCode statusCode;
            byte[]? token = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out statusCode);

            if (token is null)
            {
                // No cached Kerberos TGT available (kinit not run); skip.
                return;
            }

            Assert.True(token.Length > 0);
            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
        }

        [Theory]
        [MemberData(nameof(LoopbackAuthenticationSuccessCases))]
        public async Task ClientServerAuthentication_Succeeds(NetworkCredential credential, string targetName)
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

            NegotiateAuthenticationStatusCode clientStatus, serverStatus;

            byte[]? clientToken = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out clientStatus);
            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, clientStatus);

            while (true)
            {
                byte[]? serverToken = server.GetOutgoingBlob(clientToken, out serverStatus);
                if (serverStatus == NegotiateAuthenticationStatusCode.Completed)
                {
                    if (serverToken is not null)
                    {
                        client.GetOutgoingBlob(serverToken, out clientStatus);
                    }
                    break;
                }
                Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, serverStatus);
                Assert.NotNull(serverToken);

                clientToken = client.GetOutgoingBlob(serverToken, out clientStatus);
                if (clientStatus == NegotiateAuthenticationStatusCode.Completed)
                {
                    break;
                }
                Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, clientStatus);
                Assert.NotNull(clientToken);
            }

            Assert.True(client.IsAuthenticated);
            Assert.True(server.IsAuthenticated);
        }

        [Theory]
        [MemberData(nameof(LoopbackAuthenticationSuccessCases))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/12345")]
        public async Task ClientServerAuthentication_WrapUnwrap_Succeeds(NetworkCredential credential, string targetName)
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

            NegotiateAuthenticationStatusCode clientStatus, serverStatus;
            byte[]? clientToken = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out clientStatus);

            while (true)
            {
                byte[]? serverToken = server.GetOutgoingBlob(clientToken, out serverStatus);
                if (serverStatus == NegotiateAuthenticationStatusCode.Completed)
                {
                    break;
                }

                clientToken = client.GetOutgoingBlob(serverToken, out clientStatus);
                if (clientStatus == NegotiateAuthenticationStatusCode.Completed)
                {
                    break;
                }
            }

            Assert.True(client.IsAuthenticated);
            Assert.True(server.IsAuthenticated);

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
                return;
            }

            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, clientStatus);
            Assert.NotNull(clientToken);

            bool authFailed = false;
            while (true)
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
        public async Task ClientServerAuthentication_ProtectionLevel_Succeeds(ProtectionLevel protectionLevel)
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

            NegotiateAuthenticationStatusCode clientStatus, serverStatus;
            byte[]? clientToken = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out clientStatus);

            while (true)
            {
                byte[]? serverToken = server.GetOutgoingBlob(clientToken, out serverStatus);
                if (serverStatus == NegotiateAuthenticationStatusCode.Completed)
                {
                    if (serverToken is not null)
                    {
                        client.GetOutgoingBlob(serverToken, out clientStatus);
                    }
                    break;
                }

                clientToken = client.GetOutgoingBlob(serverToken, out clientStatus);
                if (clientStatus == NegotiateAuthenticationStatusCode.Completed)
                {
                    break;
                }
            }

            Assert.True(client.IsAuthenticated);
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

        [Theory]
        [InlineData("HOST/linuxclient.linux.contoso.com@LINUX.CONTOSO.COM")]
        [InlineData("HTTP/apacheweb.linux.contoso.com@LINUX.CONTOSO.COM")]
        public void ClientAuthentication_TargetNameWithRealm_Succeeds(string targetName)
        {
            using var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
            {
                Package = "Negotiate",
                Credential = EnterpriseTestConfiguration.ValidNetworkCredentials,
                TargetName = targetName,
            });

            NegotiateAuthenticationStatusCode statusCode;
            byte[]? token = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out statusCode);

            Assert.NotNull(token);
            Assert.True(token.Length > 0);
            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
        }
    }
}
