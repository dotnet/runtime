// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpClientHandler_SslProtocols_Test
    {
        [Fact]
        public void DefaultProtocols_MatchesExpected()
        {
            using (var handler = new HttpClientHandler())
            {
                const SslProtocols expectedDefault = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
                Assert.Equal(expectedDefault, handler.SslProtocols);
            }
        }

        [Theory]
        [InlineData(SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls11 | SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12)]
        public void SetGetProtocols_Roundtrips(SslProtocols protocols)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.SslProtocols = protocols;
                Assert.Equal(protocols, handler.SslProtocols);
            }
        }

        [ConditionalFact(nameof(BackendSupportsSslConfiguration))]
        public async Task SetProtcols_AfterRequest_ThrowsException()
        {
            using (var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = LoopbackServer.AllowAllCertificates })
            using (var client = new HttpClient(handler))
            {
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await WhenAllCompletedOrAnyFailed(
                        LoopbackServer.ReadRequestAndSendResponseAsync(server),
                        client.GetAsync(url));
                });
                Assert.Throws<InvalidOperationException>(() => handler.SslProtocols = SslProtocols.Tls12);
            }
        }

        [Theory]
        [InlineData(SslProtocols.None)]
        [InlineData(~SslProtocols.None)]
#pragma warning disable 0618 // obsolete warning
        [InlineData(SslProtocols.Ssl2)]
        [InlineData(SslProtocols.Ssl3)]
        [InlineData(SslProtocols.Ssl2 | SslProtocols.Ssl3)]
        [InlineData(SslProtocols.Ssl2 | SslProtocols.Ssl3 | SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12)]
#pragma warning restore 0618
        public void DisabledProtocols_SetSslProtocols_ThrowsException(SslProtocols disabledProtocols)
        {
            using (var handler = new HttpClientHandler())
            {
                Assert.Throws<NotSupportedException>(() => handler.SslProtocols = disabledProtocols);
            }
        }

        [ActiveIssue(8444, PlatformID.AnyUnix)]
        [ConditionalTheory(nameof(BackendSupportsSslConfiguration))]
        [InlineData(SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls12)]
        public async Task GetAsync_AllowedSSLVersion_Succeeds(SslProtocols acceptedProtocol)
        {
            using (var handler = new HttpClientHandler() { SslProtocols = acceptedProtocol, ServerCertificateCustomValidationCallback = LoopbackServer.AllowAllCertificates })
            using (var client = new HttpClient(handler))
            {
                var options = new LoopbackServer.Options { UseSsl = true, SslProtocols = acceptedProtocol };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await WhenAllCompletedOrAnyFailed(
                        LoopbackServer.ReadRequestAndSendResponseAsync(server, options: options),
                        client.GetAsync(url));
                }, options);
            }
        }

        [ConditionalTheory(nameof(BackendSupportsSslConfiguration))]
        [InlineData(SslProtocols.Tls11, SslProtocols.Tls, typeof(IOException))]
        [InlineData(SslProtocols.Tls12, SslProtocols.Tls11, typeof(IOException))]
        [InlineData(SslProtocols.Tls, SslProtocols.Tls12, typeof(AuthenticationException))]
        public async Task GetAsync_AllowedSSLVersionDiffersFromServer_ThrowsException(
            SslProtocols allowedProtocol, SslProtocols acceptedProtocol, Type exceptedServerException)
        {
            using (var handler = new HttpClientHandler() { SslProtocols = allowedProtocol, ServerCertificateCustomValidationCallback = LoopbackServer.AllowAllCertificates })
            using (var client = new HttpClient(handler))
            {
                var options = new LoopbackServer.Options { UseSsl = true, SslProtocols = acceptedProtocol };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await WhenAllCompletedOrAnyFailed(
                        Assert.ThrowsAsync(exceptedServerException, () => LoopbackServer.ReadRequestAndSendResponseAsync(server, options: options)),
                        Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url)));
                }, options);
            }
        }

        [Fact]
        public async Task GetAsync_DisallowTls10_AllowTls11_AllowTls12()
        {
            using (var handler = new HttpClientHandler() { SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12, ServerCertificateCustomValidationCallback = LoopbackServer.AllowAllCertificates })
            using (var client = new HttpClient(handler))
            {
                if (BackendSupportsSslConfiguration)
                {
                    LoopbackServer.Options options = new LoopbackServer.Options { UseSsl = true };

                    options.SslProtocols = SslProtocols.Tls;
                    await LoopbackServer.CreateServerAsync(async (server, url) =>
                    {
                        await WhenAllCompletedOrAnyFailed(
                            Assert.ThrowsAsync<IOException>(() => LoopbackServer.ReadRequestAndSendResponseAsync(server, options: options)),
                            Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url)));
                    }, options);

                    foreach (var prot in new[] { SslProtocols.Tls11, SslProtocols.Tls12 })
                    {
                        options.SslProtocols = prot;
                        await LoopbackServer.CreateServerAsync(async (server, url) =>
                        {
                            await WhenAllCompletedOrAnyFailed(
                                LoopbackServer.ReadRequestAndSendResponseAsync(server, options: options),
                                client.GetAsync(url));
                        }, options);
                    }
                }
                else
                {
                    await Assert.ThrowsAnyAsync<NotSupportedException>(() => client.GetAsync($"http://{Guid.NewGuid().ToString()}/"));
                }
            }
        }

        private static Task WhenAllCompletedOrAnyFailed(params Task[] tasks)
        {
            var tcs = new TaskCompletionSource<bool>();

            int remaining = tasks.Length;
            foreach (var task in tasks)
            {
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted) tcs.SetException(t.Exception.InnerExceptions);
                    else if (t.IsCanceled) tcs.SetCanceled();
                    else if (Interlocked.Decrement(ref remaining) == 0) tcs.SetResult(true);
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            return tcs.Task;
        }

        private static bool BackendSupportsSslConfiguration =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            (CurlSslVersionDescription()?.StartsWith("OpenSSL") ?? false);

        [DllImport("System.Net.Http.Native", EntryPoint = "HttpNative_GetSslVersionDescription")]
        private static extern string CurlSslVersionDescription();
    }
}
