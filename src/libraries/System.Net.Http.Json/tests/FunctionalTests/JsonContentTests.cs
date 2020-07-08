// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using System.Net.Test.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    public abstract class JsonContentTestsBase
    {
        protected abstract Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request);

        private class Foo { }
        private class Bar { }

        [Fact]
        public void JsonContentObjectType()
        {
            Type fooType = typeof(Foo);
            Foo foo = new Foo();

            JsonContent content = JsonContent.Create(foo, fooType);
            Assert.Equal(fooType, content.ObjectType);
            Assert.Same(foo, content.Value);

            content = JsonContent.Create(foo);
            Assert.Equal(fooType, content.ObjectType);
            Assert.Same(foo, content.Value);

            object fooBoxed = foo;

            // ObjectType is the specified type when using the .ctor.
            content = JsonContent.Create(fooBoxed, fooType);
            Assert.Equal(fooType, content.ObjectType);
            Assert.Same(fooBoxed, content.Value);

            // ObjectType is the declared type when using the factory method.
            content = JsonContent.Create(fooBoxed);
            Assert.Equal(typeof(object), content.ObjectType);
            Assert.Same(fooBoxed, content.Value);
        }

        [Fact]
        public void TestJsonContentMediaType()
        {
            Type fooType = typeof(Foo);
            Foo foo = new Foo();

            // Use the default content-type if none is provided.
            JsonContent content = JsonContent.Create(foo, fooType);
            Assert.Equal("application/json", content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            content = JsonContent.Create(foo);
            Assert.Equal("application/json", content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            // Use the specified MediaTypeHeaderValue if provided.
            MediaTypeHeaderValue mediaType = MediaTypeHeaderValue.Parse("foo/bar; charset=utf-8");
            content = JsonContent.Create(foo, fooType, mediaType);
            Assert.Same(mediaType, content.Headers.ContentType);

            content = JsonContent.Create(foo, mediaType: mediaType);
            Assert.Same(mediaType, content.Headers.ContentType);
        }

        [Fact]
        public async Task SendQuotedCharsetAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        JsonContent content = JsonContent.Create<Foo>(null);
                        content.Headers.ContentType.CharSet = "\"utf-8\"";

                        var request = new HttpRequestMessage(HttpMethod.Post, uri);
                        request.Content = content;
                        await SendAsync(client, request);
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync();
                    Assert.Equal("application/json; charset=\"utf-8\"", req.GetSingleHeaderValue("Content-Type"));
                });
        }

        [Fact]
        public void TestJsonContentContentTypeIsNotTheSameOnMultipleInstances()
        {
            JsonContent jsonContent1 = JsonContent.Create<object>(null);
            JsonContent jsonContent2 = JsonContent.Create<object>(null);

            jsonContent1.Headers.ContentType.CharSet = "foo-bar";

            Assert.NotEqual(jsonContent1.Headers.ContentType.CharSet, jsonContent2.Headers.ContentType.CharSet);
            Assert.NotSame(jsonContent1.Headers.ContentType, jsonContent2.Headers.ContentType);
        }

        [Fact]
        public async Task JsonContentMediaTypeValidateOnServerAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Post, uri);
                        MediaTypeHeaderValue mediaType = MediaTypeHeaderValue.Parse("foo/bar; charset=utf-8");
                        request.Content = JsonContent.Create(Person.Create(), mediaType: mediaType);
                        await SendAsync(client, request);
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync();
                    Assert.Equal("foo/bar; charset=utf-8", req.GetSingleHeaderValue("Content-Type"));
                });
        }

        [Fact]
        public void JsonContentMediaTypeDefaultIfNull()
        {
            Type fooType = typeof(Foo);
            Foo foo = null;

            JsonContent content = JsonContent.Create(foo, fooType, mediaType: null);
            Assert.Equal("application/json", content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            content = JsonContent.Create(foo, mediaType: null);
            Assert.Equal("application/json", content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);
        }

        [Fact]
        public void JsonContentInputTypeIsNull()
            => AssertExtensions.Throws<ArgumentNullException>("inputType", () => JsonContent.Create(null, inputType: null, mediaType: null));

        [Fact]
        public void JsonContentThrowsOnIncompatibleTypeAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                var foo = new Foo();
                Type typeOfBar = typeof(Bar);

                Exception ex = Assert.Throws<ArgumentException>(() => JsonContent.Create(foo, typeOfBar));

                string strTypeOfBar = typeOfBar.ToString();
                Assert.Contains(strTypeOfBar, ex.Message);

                string afterInputTypeMessage = ex.Message.Split(strTypeOfBar.ToCharArray())[1];
                Assert.Contains(afterInputTypeMessage, ex.Message);
            }
        }

        [Fact]
        public async Task ValidateUtf16IsTranscodedAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Post, uri);
                        MediaTypeHeaderValue mediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-16");
                        // Pass new options to avoid using the Default Web Options that use camelCase.
                        request.Content = JsonContent.Create(Person.Create(), mediaType: mediaType, options: new JsonSerializerOptions());
                        await SendAsync(client, request);
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync();
                    Assert.Equal("application/json; charset=utf-16", req.GetSingleHeaderValue("Content-Type"));
                    Person per = JsonSerializer.Deserialize<Person>(Encoding.Unicode.GetString(req.Body));
                    per.Validate();
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
                        // EnsureDefaultOptions uses a JsonConverter where we validate the JsonSerializerOptions when not provided to JsonContent.Create.
                        EnsureDefaultOptions dummyObj = new EnsureDefaultOptions();
                        var request = new HttpRequestMessage(HttpMethod.Post, uri);
                        request.Content = JsonContent.Create(dummyObj);
                        await SendAsync(client, request);
                    }
                },
                server => server.HandleRequestAsync());
        }

        [Fact]
        public async Task TestJsonContentNullContentTypeAsync()
        {
            await HttpMessageHandlerLoopbackServer.CreateClientAndServerAsync(
                async (handler, uri) =>
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Post, uri);
                        MediaTypeHeaderValue mediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-16");
                        JsonContent content = JsonContent.Create(Person.Create(), mediaType: mediaType);
                        content.Headers.ContentType = null;

                        request.Content = content;
                        await SendAsync(client, request);
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync();
                    Assert.Equal(0, req.GetHeaderValueCount("Content-Type"));
                });
        }
    }

    public class JsonContentTests_Async : JsonContentTestsBase
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request) => client.SendAsync(request);
    }
}
