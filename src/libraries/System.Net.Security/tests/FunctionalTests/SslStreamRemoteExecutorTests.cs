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
        private readonly ITestOutputHelper _output;

        public SslStreamRemoteExecutorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // SSLKEYLOGFILE is only supported on Linux for SslStream
        [InlineData(true)]
        [InlineData(false)]
        //[ActiveIssue("https://github.com/dotnet/runtime/issues/116473")]
        public async Task SslKeyLogFile_IsCreatedAndFilled(bool enabledBySwitch)
        {
            if (PlatformDetection.IsDebugLibrary(typeof(SslStream).Assembly) && !enabledBySwitch)
            {
                // AppCtxSwitch is not checked for SSLKEYLOGFILE in Debug builds, the same code path
                // will be tested by the enabledBySwitch = true case. Skip it here.
                return;
            }

            if (PlatformDetection.IsOpenSsl3_5 && !enabledBySwitch)
            {
                // OpenSSL 3.5 and later versions log into file in SSLKEYLOGFILE environment variable by default,
                // regardless of AppContext switch.
                return;
            }

            var psi = new ProcessStartInfo();
            var tempFile = Path.GetTempFileName();
            psi.Environment.Add("SSLKEYLOGFILE", tempFile);

            await RemoteExecutor.Invoke(async (enabledBySwitch) =>
            {
                if (bool.Parse(enabledBySwitch))
                {
                    AppContext.SetSwitch("System.Net.EnableSslKeyLogging", true);
                }

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
            }, enabledBySwitch.ToString(), new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();

            if (enabledBySwitch)
            {
                Assert.True(File.ReadAllText(tempFile).Length > 0);
            }
            else
            {
                Assert.True(File.ReadAllText(tempFile).Length == 0);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // OpenSSL configuration is only used on Linux
        public async Task MalformedOpenSslConfig_DoesNotCrash()
        {
            // This test verifies that when OpenSSL has a malformed configuration,
            // the SSL initialization gracefully falls back to defaults instead of crashing.

            // Create a malformed OpenSSL configuration file
            var tempConfigFile = Path.GetTempFileName();
            try
            {
                // Write a malformed config that references a non-existent provider section
                File.WriteAllText(tempConfigFile, @"openssl_conf = openssl_init

[openssl_init]
providers = provider_sect

[provider_sect]
default = default_sect
legacy = legacy_sect

[default_sect]
activate = 1

# The legacy_sect section is intentionally missing to cause a configuration error
");

                var psi = new ProcessStartInfo
                {
                    Environment = { { "OPENSSL_CONF", tempConfigFile } }
                };

                // The test should complete successfully without crashing
                await RemoteExecutor.Invoke(async () =>
                {
                    // Create an SSL stream and perform a handshake
                    // This will trigger SSL initialization which should gracefully handle the malformed config
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

                        // This should not crash even with malformed OpenSSL config
                        await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                            client.AuthenticateAsClientAsync(clientOptions),
                            server.AuthenticateAsServerAsync(serverOptions));

                        await TestHelper.PingPong(client, server);
                    }
                }, new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();

                // If we get here without exception, the test passed
                _output.WriteLine("Successfully handled malformed OpenSSL configuration without crashing");
            }
            finally
            {
                // Clean up the temporary config file
                if (File.Exists(tempConfigFile))
                {
                    File.Delete(tempConfigFile);
                }
            }
        }
    }
}
