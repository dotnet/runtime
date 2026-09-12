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
            KerberosExecutor.IsSupported && Capability.IsGssGetNameAttributeSupported();

        public NegotiateAuthenticationKerberosTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
    
        [Fact]
        public async Task Loopback_Success()
        {
            using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");

            kerberosExecutor.AddService("HTTP/linux.contoso.com");
            kerberosExecutor.AddUser("user");

            await kerberosExecutor.Invoke(() =>
            {
                // Do a loopback authentication
                NegotiateAuthenticationClientOptions clientOptions = new()
                {
                    Credential = new NetworkCredential("user", KerberosExecutor.DefaultUserPassword, "LINUX.CONTOSO.COM"),
                    TargetName = $"HTTP/linux.contoso.com"
                };
                NegotiateAuthenticationServerOptions serverOptions = new() { };
                NegotiateAuthentication clientNegotiateAuthentication = new(clientOptions);
                NegotiateAuthentication serverNegotiateAuthentication = new(serverOptions);

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

                Claim? primarySid = identity.FindFirst(ClaimTypes.PrimarySid);
                if (primarySid is null)
                {
                    throw new SkipTestException("The GSS mechanism does not expose the Kerberos PAC name attribute.");
                }

                Assert.Equal($"{DomainSid}-1104", primarySid.Value);
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

                Assert.Equal("user@LINUX.CONTOSO.COM", identity.Name);
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
