// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamRemoteExecutorTests
    {
        public SslStreamRemoteExecutorTests()
        { }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/94843", ~TestPlatforms.Linux)]
        public void SslKeyLogFile_IsCreatedAndFilled()
        {
            if (PlatformDetection.IsReleaseLibrary(typeof(SslStream).Assembly))
            {
                throw new SkipTestException("Retrieving SSL secrets is not supported in Release mode.");
            }

            var psi = new ProcessStartInfo();
            var tempFile = Path.GetTempFileName();
            psi.Environment.Add("SSLKEYLOGFILE", tempFile);

            RemoteExecutor.Invoke(async () =>
            {
                (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
                using (clientStream)
                using (serverStream)
                using (var client = new SslStream(clientStream))
                using (var server = new SslStream(serverStream))
                using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
                {
                    SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions();
                    clientOptions.RemoteCertificateValidationCallback = delegate { return true; };

                    SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions();
                    serverOptions.ServerCertificate = certificate;

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));

                    await TestHelper.PingPong(client, server);
                }
            }, new RemoteInvokeOptions { StartInfo = psi }).Dispose();

            Assert.True(File.Exists(tempFile));
            Assert.True(File.ReadAllText(tempFile).Length > 0);
        }
    }
}