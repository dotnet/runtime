// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net.Security.Kerberos;
using System.Net.Test.Common;
using Kerberos.NET.Entities.Pac;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    [ConditionalClass(typeof(KerberosExecutor), nameof(KerberosExecutor.IsSupported))]
    public partial class NegotiateAuthenticationKerberosTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private static bool IsGssGetNameAttributeSupported =>
            OperatingSystem.IsLinux() &&
            KerberosExecutor.IsSupported &&
            Capability.IsGssGetNameAttributeSupported();

        public NegotiateAuthenticationKerberosTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
    
        [Theory]
        [InlineData("Negotiate", "Negotiate")]
        [InlineData("Kerberos", "Kerberos")]
        [InlineData("Kerberos", "Negotiate")]
        public async Task Loopback_Success(string clientPackage, string serverPackage)
        {
            using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");

            kerberosExecutor.AddService("HTTP/linux.contoso.com");
            kerberosExecutor.AddUser("user");

            await kerberosExecutor.Invoke(LoopbackSuccess, clientPackage, serverPackage);
        }

        private static void LoopbackSuccess(string clientPackage, string serverPackage)
        {
            // Do a loopback authentication
            NegotiateAuthenticationClientOptions clientOptions = new()
            {
                Package = clientPackage,
                Credential = new NetworkCredential("user", KerberosExecutor.DefaultUserPassword, "LINUX.CONTOSO.COM"),
                TargetName = "HTTP/linux.contoso.com"
            };
            NegotiateAuthenticationServerOptions serverOptions = new() { Package = serverPackage };
            using NegotiateAuthentication clientNegotiateAuthentication = new(clientOptions);
            using NegotiateAuthentication serverNegotiateAuthentication = new(serverOptions);

            byte[]? serverBlob = null;
            byte[]? clientBlob = null;
            bool shouldContinue = true;
            do
            {
                clientBlob = clientNegotiateAuthentication.GetOutgoingBlob(serverBlob, out NegotiateAuthenticationStatusCode statusCode);
                shouldContinue = statusCode == NegotiateAuthenticationStatusCode.ContinueNeeded;
                Assert.True(statusCode <= NegotiateAuthenticationStatusCode.ContinueNeeded, $"Client authentication failed with {statusCode}");
                if (clientBlob != null)
                {
                    serverBlob = serverNegotiateAuthentication.GetOutgoingBlob(clientBlob, out statusCode);
                    Assert.True(statusCode <= NegotiateAuthenticationStatusCode.ContinueNeeded, $"Server authentication failed with {statusCode}");
                }
            }
            while (serverBlob != null && shouldContinue);

            Assert.Equal("Kerberos", clientNegotiateAuthentication.Package);
            Assert.Equal("Kerberos", serverNegotiateAuthentication.Package);
            Assert.True(clientNegotiateAuthentication.IsAuthenticated);
            Assert.True(serverNegotiateAuthentication.IsAuthenticated);
        }

        [ConditionalTheory(typeof(Capability), nameof(Capability.IsNtlmServerAvailable))]
        [InlineData("Kerberos", "4e544c4d535350000100000033828ae2000000000000000000000000000000000601b01d0000000f")]
        [InlineData("Kerberos", "604806062b0601050502a03e303ca00e300c060a2b06010401823702020aa22a04284e544c4d535350000100000033828ae2000000000000000000000000000000000601b01d0000000f")]
        [InlineData("kErBeRoS", "4e544c4d535350000100000033828ae2000000000000000000000000000000000601b01d0000000f")]
        public async Task ServerPackage_Kerberos_RejectsNtlm(string serverPackage, string tokenHex)
        {
            using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");
            kerberosExecutor.AddService("HTTP/linux.contoso.com");

            await kerberosExecutor.Invoke(static (package, token) =>
            {
                byte[] clientBlob = Convert.FromHexString(token);
                using NegotiateAuthentication negotiateServer = new(new NegotiateAuthenticationServerOptions());
                byte[]? challenge = negotiateServer.GetOutgoingBlob(clientBlob, out NegotiateAuthenticationStatusCode statusCode);
                Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
                Assert.NotNull(challenge);

                using NegotiateAuthentication kerberosServer = new(new NegotiateAuthenticationServerOptions { Package = package });
                byte[]? serverBlob = kerberosServer.GetOutgoingBlob(clientBlob, out statusCode);
                Assert.True(statusCode >= NegotiateAuthenticationStatusCode.GenericFailure, $"Server accepted NTLM with {statusCode}");
                Assert.Null(serverBlob);
            }, serverPackage, tokenHex);
        }

        [ConditionalFact(typeof(Capability), nameof(Capability.IsNtlmServerAvailable))]
        public async Task ServerPackage_Ntlm_RejectsKerberos()
        {
            using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");
            kerberosExecutor.AddService("HTTP/linux.contoso.com");
            kerberosExecutor.AddUser("user");

            await kerberosExecutor.Invoke(static () =>
            {
                using NegotiateAuthentication client = new(new NegotiateAuthenticationClientOptions
                {
                    Package = "Kerberos",
                    Credential = new NetworkCredential("user", KerberosExecutor.DefaultUserPassword, "LINUX.CONTOSO.COM"),
                    TargetName = "HTTP/linux.contoso.com",
                    RequireMutualAuthentication = true
                });
                byte[]? clientBlob = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out NegotiateAuthenticationStatusCode statusCode);
                Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
                Assert.NotNull(clientBlob);

                using NegotiateAuthentication ntlmControl = new(new NegotiateAuthenticationServerOptions { Package = "NTLM" });
                byte[] ntlmBlob = Convert.FromHexString("4e544c4d535350000100000033828ae2000000000000000000000000000000000601b01d0000000f");
                byte[]? ntlmChallenge = ntlmControl.GetOutgoingBlob(ntlmBlob, out statusCode);
                Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
                Assert.NotNull(ntlmChallenge);

                using NegotiateAuthentication server = new(new NegotiateAuthenticationServerOptions { Package = "NTLM" });
                byte[]? serverBlob = server.GetOutgoingBlob(clientBlob, out statusCode);
                Assert.True(statusCode >= NegotiateAuthenticationStatusCode.GenericFailure, $"Server accepted Kerberos with {statusCode}");
                Assert.Null(serverBlob);
            });
        }

        [ConditionalFact(nameof(IsGssGetNameAttributeSupported))]
        public async Task RemoteIdentity_WithPac_HasSidClaims()
        {
            using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");

            kerberosExecutor.AddService("HTTP/linux.contoso.com");
            kerberosExecutor.AddUserWithPac(
                "user",
                userId: 1104,
                groupIds: new uint[] { FakeKerberosPrincipal.DomainUsersGroupId, 1105 },
                extraGroupSids: new[]
                {
                    new SecurityIdentifier(IdentifierAuthority.NTAuthority, new uint[] { 21, 111, 222, 333, 1201 }, SidAttributes.SE_GROUP_ENABLED),
                    new SecurityIdentifier(IdentifierAuthority.NTAuthority, new uint[] { 21, 111, 222, 333, 1202 }, SidAttributes.SE_GROUP_USE_FOR_DENY_ONLY),
                });

            await kerberosExecutor.Invoke(() =>
            {
                const string DomainSid = "S-1-5-21-2127521184-1604012920-1887927527";
                using NegotiateAuthentication serverNegotiateAuthentication = AuthenticateLoopback();
                var identity = Assert.IsAssignableFrom<ClaimsIdentity>(serverNegotiateAuthentication.RemoteIdentity);

                Assert.Equal($"{DomainSid}-1104", identity.FindFirst(ClaimTypes.PrimarySid)?.Value);
                Assert.Equal($"{DomainSid}-513", identity.FindFirst(ClaimTypes.PrimaryGroupSid)?.Value);
                Assert.Equal(
                    new[] { $"{DomainSid}-513", $"{DomainSid}-1105", "S-1-5-21-111-222-333-1201" },
                    identity.FindAll(ClaimTypes.GroupSid).Select(claim => claim.Value).ToArray());
            });
        }

        [Fact]
        public async Task RemoteIdentity_WithoutPac_HasNoSidClaims()
        {
            using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");

            kerberosExecutor.AddService("HTTP/linux.contoso.com");
            kerberosExecutor.AddUser("user");

            await kerberosExecutor.Invoke(() =>
            {
                using NegotiateAuthentication serverNegotiateAuthentication = AuthenticateLoopback();
                var identity = Assert.IsAssignableFrom<ClaimsIdentity>(serverNegotiateAuthentication.RemoteIdentity);

                Assert.Null(identity.FindFirst(ClaimTypes.PrimarySid));
                Assert.Null(identity.FindFirst(ClaimTypes.PrimaryGroupSid));
                Assert.Empty(identity.FindAll(ClaimTypes.GroupSid));
            });
        }

        private static NegotiateAuthentication AuthenticateLoopback()
        {
            NegotiateAuthenticationClientOptions clientOptions = new()
            {
                Credential = new NetworkCredential("user", KerberosExecutor.DefaultUserPassword, "LINUX.CONTOSO.COM"),
                TargetName = "HTTP/linux.contoso.com"
            };
            using NegotiateAuthentication clientNegotiateAuthentication = new(clientOptions);
            NegotiateAuthentication serverNegotiateAuthentication = new(new NegotiateAuthenticationServerOptions { });

            byte[]? serverBlob = null;
            byte[]? clientBlob;
            bool shouldContinue;
            do
            {
                clientBlob = clientNegotiateAuthentication.GetOutgoingBlob(serverBlob, out NegotiateAuthenticationStatusCode statusCode);
                shouldContinue = statusCode == NegotiateAuthenticationStatusCode.ContinueNeeded;
                Assert.True(statusCode <= NegotiateAuthenticationStatusCode.ContinueNeeded, $"Client authentication failed with {statusCode}");
                if (clientBlob != null)
                {
                    serverBlob = serverNegotiateAuthentication.GetOutgoingBlob(clientBlob, out statusCode);
                    Assert.True(statusCode <= NegotiateAuthenticationStatusCode.ContinueNeeded, $"Server authentication failed with {statusCode}");
                }
            }
            while (serverBlob != null && shouldContinue);

            Assert.True(serverNegotiateAuthentication.IsAuthenticated);
            return serverNegotiateAuthentication;
        }

        [Fact]
        public async Task Invalid_Token()
        {
            using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");
            // Force a non-empty keytab to make macOS happy
            kerberosExecutor.AddService("HTTP/linux.contoso.com");
            await kerberosExecutor.Invoke(() =>
            {
                NegotiateAuthentication ntAuth = new NegotiateAuthentication(new NegotiateAuthenticationServerOptions { });
                // Ask for NegHints
                byte[] blob = ntAuth.GetOutgoingBlob((ReadOnlySpan<byte>)default, out NegotiateAuthenticationStatusCode statusCode);
                Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
                Assert.NotNull(blob);
                // Send garbage token
                blob = ntAuth.GetOutgoingBlob(new byte[3], out statusCode);
                Assert.True(statusCode >= NegotiateAuthenticationStatusCode.GenericFailure);
                Assert.Null(blob);
            });
        }

        [Fact]
        public async Task Client_UnknownSPN_ReturnsTargetUnknown()
        {
            using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");
            // Add a registered service to get a non-empty keytab (required on macOS)
            kerberosExecutor.AddService("HTTP/linux.contoso.com");
            kerberosExecutor.AddUser("user");

            await kerberosExecutor.Invoke(() =>
            {
                // Use an SPN that is not registered in the KDC
                using NegotiateAuthentication client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
                {
                    Package = "Kerberos",
                    Credential = new NetworkCredential("user", KerberosExecutor.DefaultUserPassword, "LINUX.CONTOSO.COM"),
                    TargetName = "HTTP/nonexistent.linux.contoso.com"
                });

                client.GetOutgoingBlob((byte[])null, out NegotiateAuthenticationStatusCode statusCode);
                Assert.Equal(NegotiateAuthenticationStatusCode.TargetUnknown, statusCode);
            });
        }
    }
}
