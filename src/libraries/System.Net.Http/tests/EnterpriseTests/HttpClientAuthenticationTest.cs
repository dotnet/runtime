// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Enterprise.Tests
{
    [ConditionalClass(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
    public class HttpClientAuthenticationTest
    {
        [Theory]
        [InlineData(EnterpriseTestConfiguration.NegotiateAuthWebServer, false)]
        [InlineData(EnterpriseTestConfiguration.AlternativeService, false)]
        [InlineData(EnterpriseTestConfiguration.DigestAuthWebServer, true)]
        [InlineData(EnterpriseTestConfiguration.DigestAuthWebServer, false)]
        [InlineData(EnterpriseTestConfiguration.NtlmAuthWebServer, true)]
        public async Task HttpClient_ValidAuthentication_Success(string url, bool useDomain)
        {
            using var handler = new HttpClientHandler();
            handler.Credentials = useDomain ? EnterpriseTestConfiguration.ValidDomainNetworkCredentials : EnterpriseTestConfiguration.ValidNetworkCredentials;
            using var client = new HttpClient(handler);

            using HttpResponseMessage response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/416")]
        [Fact]
        public async Task HttpClient_InvalidAuthentication_Failure()
        {
            using var handler = new HttpClientHandler();
            handler.Credentials = EnterpriseTestConfiguration.InvalidNetworkCredentials;
            using var client = new HttpClient(handler);

            using HttpResponseMessage response = await client.GetAsync(EnterpriseTestConfiguration.NegotiateAuthWebServer);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
