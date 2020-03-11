// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    public class JsonContentTests
    {

        private class Foo { }
        private class Bar { }

        [Fact]
        public void JsonContentObjectType()
        {
            Type fooType = typeof(Foo);
            Foo foo = new Foo();

            JsonContent content = new JsonContent(fooType, foo);
            Assert.Equal(fooType, content.ObjectType);
            Assert.Same(foo, content.Value);

            content = JsonContent.Create(foo);
            Assert.Equal(fooType, content.ObjectType);
            Assert.Same(foo, content.Value);

            object fooBoxed = foo;

            // ObjectType is the specified type when using the .ctor.
            content = new JsonContent(fooType, fooBoxed);
            Assert.Equal(fooType, content.ObjectType);
            Assert.Same(fooBoxed, content.Value);

            // ObjectType is the declared type when using the factory method.
            content = JsonContent.Create(fooBoxed);
            Assert.Equal(typeof(object), content.ObjectType);
            Assert.Same(fooBoxed, content.Value);
        }

        [Fact]
        public void JsonContentMediaType()
        {
            MediaTypeHeaderValue mediaType = MediaTypeHeaderValue.Parse("foo/bar; charset=utf-16");
            Type fooType = typeof(Foo);
            Foo foo = new Foo();

            // Use the default content-type if none is provided.
            JsonContent content = new JsonContent(fooType, foo);
            Assert.Equal("application/json", content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            content = JsonContent.Create(foo);
            Assert.Equal("application/json", content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            // Use the specified MediaTypeHeaderValue if provided.
            content = new JsonContent(fooType, foo, mediaType: mediaType);
            Assert.Same(mediaType, content.Headers.ContentType);

            content = JsonContent.Create(foo, mediaType: mediaType);
            Assert.Same(mediaType, content.Headers.ContentType);

            // Use the specified mediaType string but use the default charset if not provided.
            string mediaTypeAsString = "foo/bar";
            content = new JsonContent(fooType, foo, mediaType: mediaTypeAsString);
            Assert.Equal(mediaTypeAsString, content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            content = JsonContent.Create(foo, mediaType: mediaTypeAsString);
            Assert.Equal(mediaTypeAsString, content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            // Use the specifed mediaType and charset.
            string mediaTypeAndCharSetAsString = "foo/bar; charset=utf-16";
            content = new JsonContent(fooType, foo, mediaType: mediaTypeAndCharSetAsString);
            Assert.Equal("foo/bar", content.Headers.ContentType.MediaType);
            Assert.Equal("utf-16", content.Headers.ContentType.CharSet);

            content = JsonContent.Create(foo, mediaType: mediaTypeAndCharSetAsString);
            Assert.Equal("foo/bar", content.Headers.ContentType.MediaType);
            Assert.Equal("utf-16", content.Headers.ContentType.CharSet);
        }

        [Fact]
        public async Task SendJsonContentMediaTypeValidateOnServerAsync()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Post, uri);
                        request.Content = JsonContent.Create(Person.Create(), mediaType: "foo/bar");
                        await client.SendAsync(request);

                        request = new HttpRequestMessage(HttpMethod.Post, uri);
                        request.Content = JsonContent.Create(Person.Create(), mediaType: "foo/bar; charset=utf-16");
                        await client.SendAsync(request);

                        request = new HttpRequestMessage(HttpMethod.Post, uri);
                        MediaTypeHeaderValue mediaType = MediaTypeHeaderValue.Parse("foo/bar");
                        request.Content = JsonContent.Create(Person.Create(), mediaType: mediaType);
                        await client.SendAsync(request);

                        request = new HttpRequestMessage(HttpMethod.Post, uri);
                        mediaType = MediaTypeHeaderValue.Parse("foo/bar; charset=baz");
                        request.Content = JsonContent.Create(Person.Create(), mediaType: mediaType);
                        await client.SendAsync(request);
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync();
                    Assert.Equal("foo/bar; charset=utf-8", req.GetSingleHeaderValue("Content-Type"));

                    req = await server.HandleRequestAsync();
                    Assert.Equal("foo/bar; charset=utf-16", req.GetSingleHeaderValue("Content-Type"));

                    req = await server.HandleRequestAsync();
                    Assert.Equal("foo/bar", req.GetSingleHeaderValue("Content-Type"));

                    req = await server.HandleRequestAsync();
                    Assert.Equal("foo/bar; charset=baz", req.GetSingleHeaderValue("Content-Type"));
                });
        }

        [Fact]
        public void JsonContentMediaTypeIsNull()
        {
            Type fooType = typeof(Foo);
            Foo foo = null;

            ArgumentNullException ex;
            ex = Assert.Throws<ArgumentNullException>(() => new JsonContent(fooType, foo, mediaType: (string)null));
            Assert.Equal("mediaType", ex.ParamName);
            ex = Assert.Throws<ArgumentNullException>(() => new JsonContent(fooType, foo, mediaType: (MediaTypeHeaderValue)null));
            Assert.Equal("mediaType", ex.ParamName);
            ex = Assert.Throws<ArgumentNullException>(() => JsonContent.Create(foo, mediaType: (string)null));
            Assert.Equal("mediaType", ex.ParamName);
            ex = Assert.Throws<ArgumentNullException>(() => JsonContent.Create(foo, mediaType: (MediaTypeHeaderValue)null));
            Assert.Equal("mediaType", ex.ParamName);
        }

        [Fact]
        public void JsonContentTypeIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonContent(null, null));
            Assert.Throws<ArgumentNullException>(() => new JsonContent(null, null, MediaTypeHeaderValue.Parse("foo/bar; charset=utf-16")));
        }

        [Fact]
        public void JsonContentThrowsOnIncompatibleType()
        {
            var foo = new Foo();
            Assert.Throws<ArgumentException>(() => new JsonContent(typeof(Bar), foo));
            Assert.Throws<ArgumentException>(() => new JsonContent(typeof(Bar), foo, MediaTypeHeaderValue.Parse("foo/bar; charset=utf-16")));
        }
    }
}
