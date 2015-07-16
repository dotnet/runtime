// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;

using Xunit;
using Xunit.Abstractions;

namespace HttpTests
{
    public class HttpClientHandlerTest
    {
        readonly ITestOutputHelper _output;

        public HttpClientHandlerTest(ITestOutputHelper output)
        {
            _output = output;
        }
        
        [Fact]
        public void SendAsync_SimpleGet_Success()
        {
            var handler = new HttpClientHandler();
            var client = new HttpClient(handler);
            
            // TODO: This is a placeholder until GitHub Issue #2383 gets resolved.
            var response = client.GetAsync("http://httpbin.org").Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            _output.WriteLine(responseContent);
        }
    }
}
