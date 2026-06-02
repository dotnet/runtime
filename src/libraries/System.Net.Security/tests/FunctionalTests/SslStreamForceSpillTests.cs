// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    // Exercises the spill path of the managed-span BIO used on Linux TLS.
    //
    // When DOTNET_OPENSSL_FORCE_BIO_SPILL=1 is set, the managed Interop.OpenSsl
    // helpers pass a zero-length write window to SslHandshake/SslEncrypt so
    // every byte written by SSL takes the spill (heap) path inside the BIO.
    // These tests run the same SslStream scenarios with that override turned
    // on so the spill path gets the same level of functional coverage as the
    // fast (window) path.
    //
    // The knob is compiled out of release builds of System.Net.Security, so
    // these tests only meaningfully exercise the spill path against Debug
    // builds and are skipped otherwise.
    [PlatformSpecific(TestPlatforms.Linux)]
    public class SslStreamForceSpillTests
    {
        public static bool IsSupported =>
            RemoteExecutor.IsSupported && PlatformDetection.IsDebugLibrary(typeof(SslStream).Assembly);

        private static ProcessStartInfo CreateForceSpillStartInfo()
        {
            var psi = new ProcessStartInfo();
            psi.Environment["DOTNET_OPENSSL_FORCE_BIO_SPILL"] = "1";
            return psi;
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task ForceSpill_PingPong_Succeeds()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
                using (clientStream)
                using (serverStream)
                using (var client = new SslStream(clientStream))
                using (var server = new SslStream(serverStream))
                using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
                {
                    SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = delegate { return true; },
                        TargetHost = "localhost",
                    };

                    SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                    {
                        ServerCertificate = certificate,
                    };

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));

                    await TestHelper.PingPong(client, server);
                }
            }, new RemoteInvokeOptions { StartInfo = CreateForceSpillStartInfo() }).DisposeAsync();
        }

        [ConditionalTheory(nameof(IsSupported))]
        [InlineData(1)]
        [InlineData(64 * 1024)]
        [InlineData(256 * 1024)]
        [InlineData(1024 * 1024)]
        public async Task ForceSpill_LargeTransfer_Succeeds(int payloadSize)
        {
            await RemoteExecutor.Invoke(static async (payloadSizeString) =>
            {
                int payloadSize = int.Parse(payloadSizeString);

                (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
                using (clientStream)
                using (serverStream)
                using (var client = new SslStream(clientStream))
                using (var server = new SslStream(serverStream))
                using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
                {
                    SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = delegate { return true; },
                        TargetHost = "localhost",
                    };

                    SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                    {
                        ServerCertificate = certificate,
                    };

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));

                    byte[] sendBuffer = new byte[payloadSize];
                    for (int i = 0; i < sendBuffer.Length; i++)
                    {
                        sendBuffer[i] = (byte)(i & 0xFF);
                    }

                    byte[] receiveBuffer = new byte[payloadSize];

                    using var cts = new CancellationTokenSource(TestConfiguration.PassingTestTimeout);

                    Task writeTask = client.WriteAsync(sendBuffer, cts.Token).AsTask();
                    Task readTask = ReadExactlyAsync(server, receiveBuffer, cts.Token);

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(writeTask, readTask);

                    Assert.Equal(sendBuffer, receiveBuffer);
                }
            }, payloadSize.ToString(), new RemoteInvokeOptions { StartInfo = CreateForceSpillStartInfo() }).DisposeAsync();
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task ForceSpill_BidirectionalStress_Succeeds()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                const int Iterations = 64;
                const int PayloadSize = 17 * 1024; // not a record-size multiple, crosses record boundaries

                (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
                using (clientStream)
                using (serverStream)
                using (var client = new SslStream(clientStream))
                using (var server = new SslStream(serverStream))
                using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
                {
                    SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = delegate { return true; },
                        TargetHost = "localhost",
                    };

                    SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                    {
                        ServerCertificate = certificate,
                    };

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));

                    byte[] payload = new byte[PayloadSize];
                    for (int i = 0; i < payload.Length; i++)
                    {
                        payload[i] = (byte)((i * 31) & 0xFF);
                    }

                    using var cts = new CancellationTokenSource(TestConfiguration.PassingTestTimeout);

                    Task clientLoop = RunPingPongAsync(client, payload, Iterations, cts.Token);
                    Task serverLoop = RunEchoAsync(server, payload.Length, Iterations, cts.Token);

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientLoop, serverLoop);
                }

                static async Task RunPingPongAsync(SslStream stream, byte[] payload, int iterations, CancellationToken ct)
                {
                    byte[] receive = new byte[payload.Length];
                    for (int i = 0; i < iterations; i++)
                    {
                        await stream.WriteAsync(payload, ct);
                        await ReadExactlyAsync(stream, receive, ct);
                        Assert.Equal(payload, receive);
                    }
                }

                static async Task RunEchoAsync(SslStream stream, int size, int iterations, CancellationToken ct)
                {
                    byte[] buffer = new byte[size];
                    for (int i = 0; i < iterations; i++)
                    {
                        await ReadExactlyAsync(stream, buffer, ct);
                        await stream.WriteAsync(buffer, ct);
                    }
                }
            }, new RemoteInvokeOptions { StartInfo = CreateForceSpillStartInfo() }).DisposeAsync();
        }

        private static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.Slice(total), cancellationToken);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }
                total += read;
            }
        }
    }
}
