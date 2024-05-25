// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic;
using System.Net.Test.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandler_Finalization_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_Finalization_Test(ITestOutputHelper output) : base(output) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Task GetAndDropResponse(HttpClient client, Uri url)
        {
            return Task.Run(async () =>
            {
                // Get the response stream, but don't dispose it or return it. Just drop it.
                _ = await client.GetStreamAsync(url).ConfigureAwait(false);
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task IncompleteResponseStream_ResponseDropped_CancelsRequestToServer()
        {
            using (HttpClient client = CreateHttpClient())
            {
                bool stopGCs = false;
                await LoopbackServerFactory.CreateClientAndServerAsync(async url =>
                {
                    await GetAndDropResponse(client, url).ConfigureAwait(false);

                    while (!Volatile.Read(ref stopGCs))
                    {
                        await Task.Delay(10);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                },
                server => server.AcceptConnectionAsync(async connection =>
                {
                    try
                    {
                        HttpRequestData data = await connection.ReadRequestDataAsync(readBody: false).ConfigureAwait(false);
                        await connection.SendResponseHeadersAsync(headers: new HttpHeaderData[] { new HttpHeaderData("SomeHeaderName", "AndValue") }).ConfigureAwait(false);
                        await connection.WaitForCancellationAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        Volatile.Write(ref stopGCs, true);
                    }
                })).ConfigureAwait(false);
            }
        }
    }
}
