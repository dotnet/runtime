// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using System.Net.Test.Common;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace System.Net.Http.Json.Functional.Tests
{
    public class HttpClientJsonExtensionsTests
    {
        private static readonly JsonSerializerOptions s_defaultSerializerOptions
            = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        [Fact]
        public async Task TestGetFromJsonAsync()
        {
            const string json = @"{""Name"":""David"",""Age"":24}";
            const int NumRequests = 4;
            HttpHeaderData header = new HttpHeaderData("Content-Type", "application/json");
            List<HttpHeaderData> headers = new List<HttpHeaderData> { header };

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Person per = (Person)await client.GetFromJsonAsync(uri, typeof(Person));
                        per.Validate();

                        per = (Person)await client.GetFromJsonAsync(uri.ToString(), typeof(Person));
                        per.Validate();

                        per = await client.GetFromJsonAsync<Person>(uri);
                        per.Validate();

                        per = await client.GetFromJsonAsync<Person>(uri.ToString());
                        per.Validate();
                    }
                },
                async server =>
                {
                    for (int i = 0; i < NumRequests; i++)
                    {
                        await server.HandleRequestAsync(content: json, headers: headers);
                    }
                });
        }

        [Fact]
        public async Task TestGetFromJsonAsyncUnsuccessfulResponseAsync()
        {
            const int NumRequests = 2;
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync(uri, typeof(Person)));
                        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync<Person>(uri));
                    }
                },
                async server =>
                {
                    for (int i = 0; i < NumRequests; i++)
                    {
                        await server.HandleRequestAsync(statusCode: HttpStatusCode.InternalServerError);
                    }
                });
        }

        [Fact]
        public async Task TestPostAsJsonAsync()
        {
            const int NumRequests = 4;
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Person person = Person.Create();

                        using HttpResponseMessage response = await client.PostAsJsonAsync(uri.ToString(), person);
                        Assert.True(response.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response2 = await client.PostAsJsonAsync(uri, person);
                        Assert.True(response2.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response3 = await client.PostAsJsonAsync(uri.ToString(), person, CancellationToken.None);
                        Assert.True(response3.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response4 = await client.PostAsJsonAsync(uri, person, CancellationToken.None);
                        Assert.True(response4.StatusCode == HttpStatusCode.OK);
                    }
                },
                async server => {
                    for (int i = 0; i < NumRequests; i++)
                    {
                        HttpRequestData request = await server.HandleRequestAsync();
                        ValidateRequest(request);
                        Person per = JsonSerializer.Deserialize<Person>(request.Body, s_defaultSerializerOptions);
                        per.Validate();
                    }
                });
        }

        [Fact]
        public async Task TestPutAsJsonAsync()
        {
            const int NumRequests = 4;
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Person person = Person.Create();
                        Type typePerson = typeof(Person);

                        using HttpResponseMessage response = await client.PutAsJsonAsync(uri.ToString(), person);
                        Assert.True(response.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response2 = await client.PutAsJsonAsync(uri, person);
                        Assert.True(response2.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response3 = await client.PutAsJsonAsync(uri.ToString(), person, CancellationToken.None);
                        Assert.True(response3.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response4 = await client.PutAsJsonAsync(uri, person, CancellationToken.None);
                        Assert.True(response4.StatusCode == HttpStatusCode.OK);
                    }
                },
                async server => {
                    for (int i = 0; i < NumRequests; i++)
                    {
                        HttpRequestData request = await server.HandleRequestAsync();
                        ValidateRequest(request);
                        Person obj = JsonSerializer.Deserialize<Person>(request.Body, s_defaultSerializerOptions);
                        obj.Validate();
                    }
                });
        }

        [Fact]
        public void TestHttpClientIsNullAsync()
        {
            HttpClient client = null;
            string uriString = "http://example.com";
            Uri uri = new Uri(uriString);

            AssertExtensions.Throws<ArgumentNullException>("client", () => client.GetFromJsonAsync(uriString, typeof(Person)));
            AssertExtensions.Throws<ArgumentNullException>("client", () => client.GetFromJsonAsync(uri, typeof(Person)));
            AssertExtensions.Throws<ArgumentNullException>("client", () => client.GetFromJsonAsync<Person>(uriString));
            AssertExtensions.Throws<ArgumentNullException>("client", () => client.GetFromJsonAsync<Person>(uri));

            AssertExtensions.Throws<ArgumentNullException>("client", () => client.PostAsJsonAsync<Person>(uriString, null));
            AssertExtensions.Throws<ArgumentNullException>("client", () => client.PostAsJsonAsync<Person>(uri, null));

            AssertExtensions.Throws<ArgumentNullException>("client", () => client.PutAsJsonAsync<Person>(uriString, null));
            AssertExtensions.Throws<ArgumentNullException>("client", () => client.PutAsJsonAsync<Person>(uri, null));
        }

        private void ValidateRequest(HttpRequestData requestData)
        {
            HttpHeaderData contentType = requestData.Headers.Where(x => x.Name == "Content-Type").First();
            Assert.Equal("application/json; charset=utf-8", contentType.Value);
        }

        [Fact]
        public async Task AllowNullRequesturlAsync()
        {
            const int NumRequests = 4;
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.BaseAddress = uri;

                        Person per = Assert.IsType<Person>(await client.GetFromJsonAsync((string)null, typeof(Person)));
                        per = Assert.IsType<Person>(await client.GetFromJsonAsync((Uri)null, typeof(Person)));

                        per = await client.GetFromJsonAsync<Person>((string)null);
                        per = await client.GetFromJsonAsync<Person>((Uri)null);
                    }
                },
                async server => {
                    List<HttpHeaderData> headers = new List<HttpHeaderData> { new HttpHeaderData("Content-Type", "application/json") };
                    string json = Person.Create().Serialize();

                    for (int i = 0; i < NumRequests; i++)
                    {
                        await server.HandleRequestAsync(content: json, headers: headers);
                    }
                });
        }
    }
}
