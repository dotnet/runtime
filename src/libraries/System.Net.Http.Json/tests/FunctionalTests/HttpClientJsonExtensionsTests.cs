// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Test.Common;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    public class HttpClientJsonExtensionsTests
    {
        [Theory]
        [MemberData(nameof(ReadFromJsonTestData))]
        public async Task TestGetFromJsonAsync(string json, bool containsQuotedNumbers)
        {
            HttpHeaderData header = new HttpHeaderData("Content-Type", "application/json");
            List<HttpHeaderData> headers = new List<HttpHeaderData> { header };

            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        Person per = (Person)await client.GetFromJsonAsync(uri, typeof(Person));
                        per.Validate();

                        per = (Person)await client.GetFromJsonAsync(uri.ToString(), typeof(Person));
                        per.Validate();

                        per = await client.GetFromJsonAsync<Person>(uri);
                        per.Validate();

                        per = await client.GetFromJsonAsync<Person>(uri.ToString());
                        per.Validate();

                        if (!containsQuotedNumbers)
                        {
                            per = (Person)await client.GetFromJsonAsync(uri, typeof(Person), JsonContext.Default);
                            per.Validate();

                            per = (Person)await client.GetFromJsonAsync(uri.ToString(), typeof(Person), JsonContext.Default);
                            per.Validate();

                            per = await client.GetFromJsonAsync<Person>(uri, JsonContext.Default.Person);
                            per.Validate();

                            per = await client.GetFromJsonAsync<Person>(uri.ToString(), JsonContext.Default.Person);
                            per.Validate();
                        }
                    }
                },
                server => server.HandleRequestAsync(content: json, headers: headers));
        }

        public static IEnumerable<object[]> ReadFromJsonTestData()
        {
            Person per = Person.Create();
            yield return new object[] { per.Serialize(), false };
            yield return new object[] { per.SerializeWithNumbersAsStrings(), true };
        }

        [Fact]
        public async Task TestGetFromJsonAsyncUnsuccessfulResponseAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync(uri, typeof(Person)));
                        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync<Person>(uri));
                        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync(uri, typeof(Person), JsonContext.Default));
                        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync(uri, JsonContext.Default.Person));
                    }
                },
                server => server.HandleRequestAsync(statusCode: HttpStatusCode.InternalServerError));
        }

        [Fact]
        public async Task TestPostAsJsonAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
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

                        using HttpResponseMessage response5 = await client.PostAsJsonAsync(uri.ToString(), person, JsonContext.Default.Person);
                        Assert.True(response5.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response6 = await client.PostAsJsonAsync(uri, person, JsonContext.Default.Person);
                        Assert.True(response6.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response7 = await client.PostAsJsonAsync(uri.ToString(), person, JsonContext.Default.Person, CancellationToken.None);
                        Assert.True(response7.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response8 = await client.PostAsJsonAsync(uri, person, JsonContext.Default.Person, CancellationToken.None);
                        Assert.True(response8.StatusCode == HttpStatusCode.OK);
                    }
                },
                async server => {
                    HttpRequestData request = await server.HandleRequestAsync();
                    ValidateRequest(request);
                    Person per = JsonSerializer.Deserialize<Person>(request.Body, JsonOptions.DefaultSerializerOptions);
                    per.Validate();
                });
        }

        [Fact]
        public async Task TestPutAsJsonAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
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

                        using HttpResponseMessage response5 = await client.PutAsJsonAsync(uri.ToString(), person, JsonContext.Default.Person);
                        Assert.True(response5.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response6 = await client.PutAsJsonAsync(uri, person, JsonContext.Default.Person);
                        Assert.True(response6.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response7 = await client.PutAsJsonAsync(uri.ToString(), person, JsonContext.Default.Person, CancellationToken.None);
                        Assert.True(response7.StatusCode == HttpStatusCode.OK);

                        using HttpResponseMessage response8 = await client.PutAsJsonAsync(uri, person, JsonContext.Default.Person, CancellationToken.None);
                        Assert.True(response8.StatusCode == HttpStatusCode.OK);
                    }
                },
                async server => {
                    HttpRequestData request = await server.HandleRequestAsync();
                    ValidateRequest(request);

                    byte[] json = request.Body;

                    Person obj = JsonSerializer.Deserialize<Person>(json, JsonOptions.DefaultSerializerOptions);
                    obj.Validate();

                    // Assert numbers are not written as strings - JsonException would be thrown here if written as strings.
                    obj = JsonSerializer.Deserialize<Person>(json, JsonOptions.DefaultSerializerOptions_StrictNumberHandling);
                    obj.Validate();
                });
        }

        [Fact]
        public void TestHttpClientIsNullAsync()
        {
            const string uriString = "http://example.com";
            const string clientParamName = "client";

            HttpClient client = null;
            Uri uri = new Uri(uriString);

            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.GetFromJsonAsync(uriString, typeof(Person)));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.GetFromJsonAsync(uri, typeof(Person)));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.GetFromJsonAsync<Person>(uriString));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.GetFromJsonAsync<Person>(uri));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.GetFromJsonAsync(uriString, typeof(Person), JsonContext.Default));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.GetFromJsonAsync(uri, typeof(Person), JsonContext.Default));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.GetFromJsonAsync(uriString, JsonContext.Default.Person));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.GetFromJsonAsync(uri, JsonContext.Default.Person));

            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.PostAsJsonAsync<Person>(uriString, null));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.PostAsJsonAsync<Person>(uri, null));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.PostAsJsonAsync(uriString, null, JsonContext.Default.Person));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.PostAsJsonAsync(uri, null, JsonContext.Default.Person));

            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.PutAsJsonAsync<Person>(uriString, null));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.PutAsJsonAsync<Person>(uri, null));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.PutAsJsonAsync(uriString, null, JsonContext.Default.Person));
            AssertExtensions.Throws<ArgumentNullException>(clientParamName, () => client.PutAsJsonAsync(uri, null, JsonContext.Default.Person));
        }

        [Fact]
        public void TestTypeMetadataIsNull()
        {
            const string uriString = "http://example.com";
            const string jsonTypeInfoParamName = "client";
            const string contextParamName = "client";

            HttpClient client = null;
            Uri uri = new Uri(uriString);

            AssertExtensions.Throws<ArgumentNullException>(contextParamName, () => client.GetFromJsonAsync(uriString, typeof(Person), JsonContext.Default));
            AssertExtensions.Throws<ArgumentNullException>(contextParamName, () => client.GetFromJsonAsync(uri, typeof(Person), JsonContext.Default));
            AssertExtensions.Throws<ArgumentNullException>(jsonTypeInfoParamName, () => client.GetFromJsonAsync(uriString, JsonContext.Default.Person));
            AssertExtensions.Throws<ArgumentNullException>(jsonTypeInfoParamName, () => client.GetFromJsonAsync(uri, JsonContext.Default.Person));

            AssertExtensions.Throws<ArgumentNullException>(jsonTypeInfoParamName, () => client.PostAsJsonAsync(uriString, null, JsonContext.Default.Person));
            AssertExtensions.Throws<ArgumentNullException>(jsonTypeInfoParamName, () => client.PostAsJsonAsync(uri, null, JsonContext.Default.Person));

            AssertExtensions.Throws<ArgumentNullException>(jsonTypeInfoParamName, () => client.PutAsJsonAsync(uriString, null, JsonContext.Default.Person));
            AssertExtensions.Throws<ArgumentNullException>(jsonTypeInfoParamName, () => client.PutAsJsonAsync(uri, null, JsonContext.Default.Person));
        }

        private void ValidateRequest(HttpRequestData requestData)
        {
            HttpHeaderData contentType = requestData.Headers.Where(x => x.Name == "Content-Type").First();
            Assert.Equal("application/json; charset=utf-8", contentType.Value);
        }

        [Fact]
        public async Task AllowNullRequesturlAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.BaseAddress = uri;

                        Person per = Assert.IsType<Person>(await client.GetFromJsonAsync((string)null, typeof(Person)));
                        per = Assert.IsType<Person>(await client.GetFromJsonAsync((Uri)null, typeof(Person)));
                        per = Assert.IsType<Person>(await client.GetFromJsonAsync((string)null, typeof(Person), JsonContext.Default));
                        per = Assert.IsType<Person>(await client.GetFromJsonAsync((Uri)null, typeof(Person), JsonContext.Default));

                        per = await client.GetFromJsonAsync<Person>((string)null);
                        per = await client.GetFromJsonAsync<Person>((Uri)null);
                    }
                },
                async server => {
                    List<HttpHeaderData> headers = new List<HttpHeaderData> { new HttpHeaderData("Content-Type", "application/json") };
                    string json = Person.Create().Serialize();

                    await server.HandleRequestAsync(content: json, headers: headers);
                });
        }
    }
}
