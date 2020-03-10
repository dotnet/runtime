// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using System.Net.Test.Common;
using System.Text.Json;
using System.Linq;
using System.Text;
using System.IO;

namespace System.Net.Http.Json.Functional.Tests
{
    public class HttpClientJsonExtensionsTests
    {
        private void ValidateRequest(HttpRequestData requestData)
        {
            Assert.NotNull(requestData);
            HttpHeaderData contentType = requestData.Headers.Where(x => x.Name == "Content-Type").First();
            Assert.Equal("application/json; charset=utf-8", contentType.Value);
        }

        [Fact]
        public async Task TestGetFromJsonAsync()
        {
            const string json = @"{""Name"":""David"",""Age"":24}";

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Person obj = (Person)await client.GetFromJsonAsync(uri, typeof(Person));
                        Assert.NotNull(obj);
                        obj.Validate();
                    }
                },
                server => server.HandleRequestAsync(content: json));

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Person obj = await client.GetFromJsonAsync<Person>(uri);
                        Assert.NotNull(obj);
                        obj.Validate();
                    }
                },
                server => server.HandleRequestAsync(content: json));
        }

        //[Fact]
        //public async Task TestGetFromJsonAsyncNotJsonContent()
        //{
        //    HttpClient client = new HttpClient();
        //    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync("http://example.com", typeof(Person)));
        //    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync<Person>("http://example.com"));

        //    Uri uri = new Uri("http://example.com");
        //    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync(uri, typeof(Person)));
        //    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync<Person>(uri));
        //}

        // add tests with non-json content type. and stress content-type application/json and text/plain.
        [Fact]
        public async Task TestGetFromJsonAsyncUnsuccessfulResponse()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync(uri, typeof(Person)));
                        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFromJsonAsync<Person>(uri));
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync(statusCode: HttpStatusCode.InternalServerError);
                });
        }

        [Fact]
        public async Task TestGetFromJsonAsyncTextPlainUtf16()
        {
            const string json = @"{""Name"":""David"",""Age"":24}";
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Person per = Assert.IsType<Person>(await client.GetFromJsonAsync(uri, typeof(Person)));
                        per.Validate();
                    }
                },
                async server => {
                    byte[] nonUtf8Response = Encoding.Unicode.GetBytes(json);
                    var buffer = new MemoryStream();
                    buffer.Write(
                        Encoding.ASCII.GetBytes(
                            $"HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-16\r\nContent-Length: {nonUtf8Response.Length}\r\nConnection:close\r\n\r\n"));
                    buffer.Write(nonUtf8Response);

                    await server.AcceptConnectionSendCustomResponseAndCloseAsync(buffer.ToArray());
                });
        }

        [Fact]
        public async Task TestGetFromJsonAsyncGenericTextPlainUtf16()
        {
            const string json = @"{""Name"":""David"",""Age"":24}";
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Person per = await client.GetFromJsonAsync<Person>(uri);
                        per.Validate();
                    }
                },
                async server => {
                    byte[] nonUtf8Response = Encoding.Unicode.GetBytes(json);
                    var buffer = new MemoryStream();
                    buffer.Write(
                        Encoding.ASCII.GetBytes(
                            $"HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-16\r\nContent-Length: {nonUtf8Response.Length}\r\nConnection:close\r\n\r\n"));
                    buffer.Write(nonUtf8Response);

                    await server.AcceptConnectionSendCustomResponseAndCloseAsync(buffer.ToArray());
                });
        }

        [Fact]
        public async Task TestPostAsJsonAsync()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        HttpResponseMessage response = await client.PostAsJsonAsync(uri, typeof(Person), Person.Create());

                        Assert.True(response.StatusCode == HttpStatusCode.OK);
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync();
                    ValidateRequest(req);

                    Person obj = JsonSerializer.Deserialize<Person>(req.Body);
                    Assert.NotNull(obj);
                    obj.Validate();
                });

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        HttpResponseMessage response = await client.PostAsJsonAsync(uri, Person.Create());
                        Assert.True(response.StatusCode == HttpStatusCode.OK);
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync();
                    ValidateRequest(req);

                    Person obj = JsonSerializer.Deserialize<Person>(req.Body);
                    Assert.NotNull(obj);
                    obj.Validate();
                });
        }

        [Fact]
        public async Task TestPutAsJsonAsync()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Person obj = Person.Create();
                        HttpResponseMessage response = await client.PutAsJsonAsync(uri, typeof(Person), obj);

                        Assert.True(response.StatusCode == HttpStatusCode.OK);
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync();
                    ValidateRequest(req);

                    Person obj = JsonSerializer.Deserialize<Person>(req.Body);
                    Assert.NotNull(obj);
                    obj.Validate();
                });

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Person obj = Person.Create();
                        HttpResponseMessage response = await client.PutAsJsonAsync(uri, obj);
                        Assert.True(response.StatusCode == HttpStatusCode.OK);
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync();
                    ValidateRequest(req);

                    Person obj = JsonSerializer.Deserialize<Person>(req.Body);
                    Assert.NotNull(obj);
                    obj.Validate();
                });
        }
    }
}
