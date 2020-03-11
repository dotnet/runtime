// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    public class HttpContentJsonExtensionsTests
    {
        private readonly List<HttpHeaderData> _headers = new List<HttpHeaderData> { new HttpHeaderData("Content-Type", "application/json") };

        [Fact]
        public async Task HttpContentGetThenReadFromJsonAsync()
        {
            HttpContent content = null;
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        var response = await client.SendAsync(request);
                        Assert.True(response.StatusCode == HttpStatusCode.OK);

                        content = response.Content;
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync(headers: _headers, content: Person.Create().Serialize());
                });

            object obj = await content.ReadFromJsonAsync(typeof(Person));
            Person per = Assert.IsType<Person>(obj);
            per.Validate();

            per = await content.ReadFromJsonAsync<Person>();
            per.Validate();
        }

        [Fact]
        public async Task HttpContentObjectIsNull()
        {
            HttpContent content = null;
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        var response = await client.SendAsync(request);
                        Assert.True(response.StatusCode == HttpStatusCode.OK);

                         content = response.Content;
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync(headers: _headers, content: "null");
                });

            object obj = await content.ReadFromJsonAsync(typeof(Person));
            Assert.Null(obj);

            Person per = await content.ReadFromJsonAsync<Person>();
            Assert.Null(per);
        }

        [Fact]
        public async Task TestGetFromJsonNoMessageBodyAsync()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        JsonException ex = await Assert.ThrowsAsync<JsonException>(() => client.GetFromJsonAsync(uri, typeof(Person)));
                        Assert.Contains("Path: $ | LineNumber: 0 | BytePositionInLine: 0", ex.Message);
                    }
                },


                server => server.HandleRequestAsync(headers: _headers));
        }

        [Fact]
        public async Task TestGetFromJsonNoContentTypeAsync()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        await Assert.ThrowsAsync<NotSupportedException>(() => client.GetFromJsonAsync<Person>(uri));
                    }
                },
                server => server.HandleRequestAsync(content: "{}"));
        }

        [Fact]
        public async Task TestGetFromJsonQuotedCharSetAsync()
        {
            List<HttpHeaderData> customHeaders = new List<HttpHeaderData>
            {
                new HttpHeaderData("Content-Type", "text/plain; charset=\"utf-8\"")
            };

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Person person = await client.GetFromJsonAsync<Person>(uri);
                        person.Validate();
                    }
                },
                server => server.HandleRequestAsync(headers: customHeaders, content: Person.Create().Serialize()));
        }

        [Fact]
        public async Task TestGetFromJsonThrowOnInvalidCharSetAsync()
        {
            List<HttpHeaderData> customHeaders = new List<HttpHeaderData>
            {
                new HttpHeaderData("Content-Type", "text/plain; charset=\"foo-bar\"")
            };

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetFromJsonAsync<Person>(uri));
                        Assert.IsType<ArgumentException>(ex.InnerException);
                    }
                },
                server => server.HandleRequestAsync(headers: customHeaders, content: Person.Create().Serialize()));
        }

        [Fact]
        public async Task TestGetFromJsonAsyncTextPlainUtf16Async()
        {
            const string json = @"{""Name"":""David"",""Age"":24}";
            const int NumRequests = 2;
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Person per = Assert.IsType<Person>(await client.GetFromJsonAsync(uri, typeof(Person)));
                        per.Validate();

                        per = await client.GetFromJsonAsync<Person>(uri);
                        per.Validate();
                    }
                },
                async server => {
                    byte[] nonUtf8Response = Encoding.Unicode.GetBytes(json);
                    var buffer = new MemoryStream();
                    buffer.Write(
                        Encoding.ASCII.GetBytes(
                            $"HTTP/1.1 200 OK" +
                            $"\r\nContent-Type: text/plain; charset=utf-16\r\n" +
                            $"Content-Length: {nonUtf8Response.Length}\r\n" +
                            $"Connection:close\r\n\r\n"));
                    buffer.Write(nonUtf8Response);

                    for (int i = 0; i < NumRequests; i++)
                    {
                        await server.AcceptConnectionSendCustomResponseAndCloseAsync(buffer.ToArray());
                    }
                });
        }
    }
}
