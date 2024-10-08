// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    // Simple typed client for use in tests
    public class TestTypedClient : ITestTypedClient
    {
        public TestTypedClient(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public HttpClient HttpClient { get; }
    }

    // Simple typed client but the HttpClient parameter is missing
    public class TestTypedClientMissingConstructorParameter
    {
        // This is an error case - do not use Typed clients like this!
        public TestTypedClientMissingConstructorParameter(IHttpClientFactory httpClientFactory)
        {
            HttpClientFactory = httpClientFactory;
        }

        public IHttpClientFactory HttpClientFactory { get; }
    }
}
