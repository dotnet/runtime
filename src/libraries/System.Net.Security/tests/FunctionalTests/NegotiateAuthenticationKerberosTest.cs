// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Net.Security.Kerberos;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    [ConditionalClass(typeof(KerberosExecutor), nameof(KerberosExecutor.IsSupported))]
    public class NegotiateAuthenticationKerberosTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

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
    }
}
