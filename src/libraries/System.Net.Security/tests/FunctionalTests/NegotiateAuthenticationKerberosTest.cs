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

        [Fact]
        public async Task Incorrect_Credentials()
        {
            using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");
            kerberosExecutor.AddUser("user");
            await kerberosExecutor.Invoke(() =>
            {
                // Do a loopback authentication
                NegotiateAuthenticationClientOptions clientOptions = new()
                {
                    Credential = new NetworkCredential("user", "PLACEHOLDERwrong", "LINUX.CONTOSO.COM"),
                    TargetName = $"HTTP/linux.contoso.com",
                    Package = "Kerberos",
                };
                NegotiateAuthentication clientNegotiateAuthentication = new(clientOptions);
                byte[]? clientBlob = clientNegotiateAuthentication.GetOutgoingBlob((ReadOnlySpan<byte>)default, out NegotiateAuthenticationStatusCode statusCode);
                Assert.True(statusCode >= NegotiateAuthenticationStatusCode.GenericFailure, $"Client authentication succeeded with {statusCode}");
                Assert.Null(clientBlob);
            });
        }
    }
}
