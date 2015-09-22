// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Tests;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class HttpClientTest
    {
        [Fact]
        [OuterLoop]
        public void Timeout_SetTo60AndGetResponseFromServerWhichTakes40_Success()
        {
            // TODO: This server path will change once the final test infrastructure is in place (Issue #1477).
            const string SlowServer = "http://httpbin.org/drip?numbytes=1&duration=1&delay=40&code=200";
            
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                var response = client.GetAsync(SlowServer).GetAwaiter().GetResult();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
