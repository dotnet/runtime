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
            const int NumRequests = 2;
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        var response = await client.SendAsync(request);
                        object obj = await response.Content.ReadFromJsonAsync(typeof(Person));
                        Person per = Assert.IsType<Person>(obj);
                        per.Validate();

                        request = new HttpRequestMessage(HttpMethod.Get, uri);
                        response = await client.SendAsync(request);
                        per = await response.Content.ReadFromJsonAsync<Person>();
                        per.Validate();
                    }
                },
                async server => {
                    for (int i = 0; i < NumRequests; i++)
                    {
                        HttpRequestData req = await server.HandleRequestAsync(headers: _headers, content: Person.Create().Serialize());
                    }
                });
        }

        [Fact]
        public async Task HttpContentObjectIsNull()
        {
            const int NumRequests = 2;
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        var response = await client.SendAsync(request);
                        object obj = await response.Content.ReadFromJsonAsync(typeof(Person));
                        Assert.Null(obj);

                        request = new HttpRequestMessage(HttpMethod.Get, uri);
                        response = await client.SendAsync(request);
                        Person per = await response.Content.ReadFromJsonAsync<Person>();
                        Assert.Null(per);
                    }
                },
                async server => {
                    for (int i = 0; i < NumRequests; i++)
                    {
                        HttpRequestData req = await server.HandleRequestAsync(headers: _headers, content: "null");
                    }
                });
        }

        [Fact]
        public async Task TestGetFromJsonNoMessageBodyAsync()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        // As of now, we pass the message body to the serializer even when its empty which causes the serializer to throw.
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

        [Fact(Skip ="Disable temporarily until transcode support is added.")]
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
                    byte[] utf16Content = Encoding.Unicode.GetBytes(json);
                    byte[] bytes =
                        Encoding.ASCII.GetBytes(
                            $"HTTP/1.1 200 OK" +
                            $"\r\nContent-Type: text/plain; charset=utf-16\r\n" +
                            $"Content-Length: {utf16Content.Length}\r\n" +
                            $"Connection:close\r\n\r\n");


                    var buffer = new MemoryStream();
                    buffer.Write(bytes, 0, bytes.Length);
                    buffer.Write(utf16Content, bytes.Length - 1, utf16Content.Length);

                    for (int i = 0; i < NumRequests; i++)
                    {
                        await server.AcceptConnectionSendCustomResponseAndCloseAsync(buffer.ToArray());
                    }
                });
        }
    }
}
