// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClient_SelectedSites_Test : HttpClientHandlerTestBase
    {
        public HttpClient_SelectedSites_Test(ITestOutputHelper output) : base(output) { }

        public static bool IsSelectedSitesTestEnabled() 
        {
            string envVar = Environment.GetEnvironmentVariable("CORFX_NET_HTTP_SELECTED_SITES");
            return envVar != null &&
                (envVar.Equals("true", StringComparison.OrdinalIgnoreCase) || envVar.Equals("1"));
        }

        [ConditionalTheory(nameof(IsSelectedSitesTestEnabled))]
        [Trait("SelectedSites", "true")]
        [MemberData(nameof(GetSelectedSites))]
        public async Task RetrieveSite_Succeeds(string site)
        {
            // Not doing this in bulk for platform handlers.
            if (!UseSocketsHttpHandler)
                return;

            int remainingAttempts = 2;
            while (remainingAttempts-- > 0)
            {
                try
                {
                    await VisitSite(site);
                    return;
                }
                catch
                {
                    if (remainingAttempts < 1)
                        throw;
                    await Task.Delay(1500);
                }
            }

            throw new Exception("Not expected to reach here");
        }

        [ConditionalTheory(nameof(IsSelectedSitesTestEnabled))]
        [Trait("SiteInvestigation", "true")]
        [InlineData("http://microsoft.com")]
        public async Task RetrieveSite_Debug_Helper(string site)
        {
            await VisitSite(site);
        }

        public static IEnumerable<string[]> GetSelectedSites()
        {
            const string resourceName = "SelectedSitesTest.txt";
            Assembly assembly = typeof(HttpClient_SelectedSites_Test).Assembly;
            Stream s = assembly.GetManifestResourceStream(resourceName);
            if (s == null)
            {
                throw new Exception("Couldn't find resource " + resourceName);
            }

            using (var reader = new StreamReader(s))
            {
                string site;
                while (null != (site = reader.ReadLine()))
                {
                    yield return new[] { site };
                }
            }
        }

        private async Task VisitSite(string site)
        {
            using (HttpClient httpClient = CreateHttpClientForSiteVisit())
            {
                await VisitSiteWithClient(site, httpClient);
            }
        }

        private async Task VisitSiteWithClient(string site, HttpClient httpClient)
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(site))
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Redirect:
                    case HttpStatusCode.OK:
                        if (response.Content.Headers.ContentLength > 0)
                            Assert.Equal(response.Content.Headers.ContentLength.Value, (await response.Content.ReadAsByteArrayAsync()).Length);
                        break;
                    case HttpStatusCode.BadGateway:
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.Moved:
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.ServiceUnavailable:
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.InternalServerError:
                        break;
                    default:
                        throw new Exception($"{site} returned: {response.StatusCode}");
                }
            }
        }

        private HttpClient CreateHttpClientForSiteVisit()
        {
            HttpClient httpClient = new HttpClient(CreateHttpClientHandler(UseSocketsHttpHandler));

            // Some extra headers since some sites only give proper responses when they are present.
            httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.186 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add(
                "Accept-Language",
                "en-US,en;q=0.9");
            httpClient.DefaultRequestHeaders.Add(
                "Accept-Encoding",
                "gzip, deflate, br");
            httpClient.DefaultRequestHeaders.Add(
                "Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");

            return httpClient;
        }
    }
}
