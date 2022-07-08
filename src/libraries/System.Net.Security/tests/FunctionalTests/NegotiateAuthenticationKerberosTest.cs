// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using System.Net.Test.Common;
using System.Net.Security.Kerberos;
using Xunit;

namespace System.Net.Security.Tests
{
    [ConditionalClass(typeof(KerberosExecutor), nameof(KerberosExecutor.IsSupported))]
    public class NegotiateAuthenticationKerberosTest
    {
        [Fact]
        public async Task Loopback_Kerberos_Authentication()
        {
            using var kerberosExecutor = new KerberosExecutor("LINUX.CONTOSO.COM");

            kerberosExecutor.AddService("HTTP/linux.contoso.com");

            await kerberosExecutor.Invoke(() =>
            {
                // Do a loopback authentication
                NegotiateAuthenticationClientOptions clientOptions = new()
                {
                    Credential = new NetworkCredential("user", KerberosExecutor.FakePassword, "LINUX.CONTOSO.COM"),
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
    }
}
