// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Http.Enterprise.Tests
{
    [ConditionalClass(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
    public class HttpClientAuthenticationTest
    {
        private const string AppContextSettingName = "System.Net.Http.UsePortInSpn";

        [Theory]
        [InlineData(EnterpriseTestConfiguration.NegotiateAuthWebServer, false)]
        [InlineData(EnterpriseTestConfiguration.NegotiateAuthWebServerNotDefaultPort, false)]
        [InlineData(EnterpriseTestConfiguration.AlternativeService, false, true)]
        [InlineData(EnterpriseTestConfiguration.DigestAuthWebServer, true)]
        [InlineData(EnterpriseTestConfiguration.DigestAuthWebServer, false)]
        [InlineData(EnterpriseTestConfiguration.NtlmAuthWebServer, true)]
        public void HttpClient_ValidAuthentication_Success(string url, bool useDomain, bool useAltPort = false)
        {
            RemoteExecutor.Invoke((url, useAltPort, useDomain) =>
            {
                // This is safe as we have no parallel tests
		if (!string.IsNullOrEmpty(useAltPort))
		{
                    AppContext.SetSwitch(AppContextSettingName, true);
                }
                using var handler = new HttpClientHandler();
                handler.Credentials = string.IsNullOrEmpty(useDomain) ? EnterpriseTestConfiguration.ValidNetworkCredentials : EnterpriseTestConfiguration.ValidDomainNetworkCredentials;
                using var client = new HttpClient(handler);

                using HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }, url, useAltPort ? "true" : "" , useDomain ? "true" : "").Dispose();
        }

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
