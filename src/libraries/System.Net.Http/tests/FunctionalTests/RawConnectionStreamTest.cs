// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class RawConnectionStreamTest 
    {
        [Fact]
        public async Task RawConnectionStream_Dispose()
        {
            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    Stream responseStream = await response.Content.ReadAsStreamAsync();

                    Assert.True(responseStream.CanRead);
                    Assert.True(responseStream.CanWrite);

                    responseStream.Dispose();

                    Assert.False(responseStream.CanRead);
                    Assert.False(responseStream.CanWrite);
                }
            },
            async server =>
            {
                // --> used to return System.Net.Http.HttpConnection+RawConnectionStream
                await server.HandleRequestAsync(HttpStatusCode.SwitchingProtocols); 
            });
        }
    }
}
