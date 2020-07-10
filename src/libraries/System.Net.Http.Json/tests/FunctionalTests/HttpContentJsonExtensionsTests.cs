﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
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
        public void ThrowOnNull()
        {
            HttpContent content = null;
            AssertExtensions.Throws<ArgumentNullException>("content", () => content.ReadFromJsonAsync<Person>());
            AssertExtensions.Throws<ArgumentNullException>("content", () => content.ReadFromJsonAsync(typeof(Person)));
        }

        [Fact]
        public async Task HttpContentGetThenReadFromJsonAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = await client.SendAsync(request);
                        object obj = await response.Content.ReadFromJsonAsync(typeof(Person));
                        Person per = Assert.IsType<Person>(obj);
                        per.Validate();

                        request = new HttpRequestMessage(HttpMethod.Get, uri);
                        response = await client.SendAsync(request);
                        per = await response.Content.ReadFromJsonAsync<Person>();
                        per.Validate();
                    }
                },
                server => server.HandleRequestAsync(headers: _headers, content: Person.Create().Serialize()));
        }

        [Fact]
        public async Task HttpContentReturnValueIsNull()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = await client.SendAsync(request);
                        object obj = await response.Content.ReadFromJsonAsync(typeof(Person));
                        Assert.Null(obj);

                        request = new HttpRequestMessage(HttpMethod.Get, uri);
                        response = await client.SendAsync(request);
                        Person per = await response.Content.ReadFromJsonAsync<Person>();
                        Assert.Null(per);
                    }
                },
                server => server.HandleRequestAsync(headers: _headers, content: "null"));
        }

        [Fact]
        public async Task TestReadFromJsonNoMessageBodyAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = await client.SendAsync(request);

                        // As of now, we pass the message body to the serializer even when its empty which causes the serializer to throw.
                        JsonException ex = await Assert.ThrowsAsync<JsonException>(() => response.Content.ReadFromJsonAsync(typeof(Person)));
                        Assert.Contains("Path: $ | LineNumber: 0 | BytePositionInLine: 0", ex.Message);
                    }
                },
                server => server.HandleRequestAsync(headers: _headers));
        }

        [Fact]
        public async Task TestReadFromJsonNoContentTypeAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = await client.SendAsync(request);

                        await Assert.ThrowsAsync<NotSupportedException>(() => response.Content.ReadFromJsonAsync<Person>());
                    }
                },
                server => server.HandleRequestAsync(content: "{}"));
        }

        [Fact]
        public async Task TestGetFromJsonQuotedCharSetAsync()
        {
            List<HttpHeaderData> customHeaders = new List<HttpHeaderData>
            {
                new HttpHeaderData("Content-Type", "application/json; charset=\"utf-8\"")
            };

            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = await client.SendAsync(request);

                        Person person = await response.Content.ReadFromJsonAsync<Person>();
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
                new HttpHeaderData("Content-Type", "application/json; charset=\"foo-bar\"")
            };

            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = await client.SendAsync(request);

                        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => response.Content.ReadFromJsonAsync<Person>());
                        Assert.IsType<ArgumentException>(ex.InnerException);
                    }
                },
                server => server.HandleRequestAsync(headers: customHeaders, content: Person.Create().Serialize()));
        }

        [Fact]
        public async Task TestGetFromJsonAsyncTextPlainUtf16Async()
        {
            string json = Person.Create().Serialize();
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = await client.SendAsync(request);

                        Person per = Assert.IsType<Person>(await response.Content.ReadFromJsonAsync(typeof(Person)));
                        per.Validate();

                        request = new HttpRequestMessage(HttpMethod.Get, uri);
                        response = await client.SendAsync(request);

                        per = await response.Content.ReadFromJsonAsync<Person>();
                        per.Validate();
                    }
                },
                async server =>
                {
                    var headers = new List<HttpHeaderData> { new HttpHeaderData("Content-Type", "application/json; charset=utf-16") };
                    await server.HandleRequestAsync(statusCode: HttpStatusCode.OK, headers: headers, bytes: Encoding.Unicode.GetBytes(json));
                });
        }
        [Fact]
        public async Task EnsureDefaultJsonSerializerOptionsAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = await client.SendAsync(request);
                        await response.Content.ReadFromJsonAsync(typeof(EnsureDefaultOptions));
                    }
                },
                server => server.HandleRequestAsync(headers: _headers, content: "{}"));
        }

        [Theory]
        [InlineData("application/json")]
        [InlineData("Application/Json")]
        [InlineData("application/foo+json")] // Structured Syntax Json Suffix
        [InlineData("application/foo+Json")]
        [InlineData("appLiCaTiOn/a+JsOn")]
        public async Task TestValidMediaTypes(string mediaType)
        {
            List<HttpHeaderData> customHeaders = new List<HttpHeaderData>
            {
                new HttpHeaderData("Content-Type", $"{mediaType}; charset=\"utf-8\"")
            };

            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = await client.SendAsync(request);

                        Person person = await response.Content.ReadFromJsonAsync<Person>();
                        person.Validate();
                    }
                },
                server => server.HandleRequestAsync(headers: customHeaders, content: Person.Create().Serialize()));
        }

        [Theory]
        [InlineData("text/plain")]
        [InlineData("/")]
        [InlineData("application/")]
        [InlineData("application/+")]
        [InlineData("application/+json")] // empty subtype before suffix is invalid.
        [InlineData("application/problem+")] // no suffix after '+'.
        [InlineData("application/problem+foo+json")] // more than one '+' not allowed.
        public async Task TestInvalidMediaTypeAsync(string mediaType)
        {
            List<HttpHeaderData> customHeaders = new List<HttpHeaderData>
            {
                new HttpHeaderData("Content-Type", $"{mediaType}; charset=\"utf-8\"")
            };

            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = await client.SendAsync(request);

                        Exception ex = await Assert.ThrowsAsync<NotSupportedException>(() => response.Content.ReadFromJsonAsync<Person>());
                        Assert.Contains("application/json", ex.Message);
                        Assert.Contains("application/+json", ex.Message);
                    }
                },
                server => server.HandleRequestAsync(headers: customHeaders, content: Person.Create().Serialize()));
        }
    }
}
