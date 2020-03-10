// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    public class JsonContentTests
    {
        private const string JsonContentType = "foo/bar; charset=utf-16";
        private readonly MediaTypeHeaderValue s_mediaTypeHeader = MediaTypeHeaderValue.Parse(JsonContentType);

        private class Foo { }
        private class Bar { }

        [Fact]
        public void JsonContentObjectType()
        {
            Type fooType = typeof(Foo);
            Foo foo = new Foo();
            JsonContent content = new JsonContent(fooType, foo);
            Assert.Equal(fooType, content.ObjectType);

            content = JsonContent.Create(foo);
            Assert.Equal(fooType, content.ObjectType);

            object fooBoxed = foo;

            // ObjectType is the specified type when using the .ctor.
            content = new JsonContent(fooType, fooBoxed);
            Assert.Equal(fooType, content.ObjectType);

            // ObjectType is the declared type when using the factory method.
            content = JsonContent.Create(fooBoxed);
            Assert.Equal(typeof(object), content.ObjectType);
        }

        [Fact]
        public void JsonContentMediaType()
        {
            Type fooType = typeof(Foo);
            Foo foo = new Foo();

            JsonContent content = new JsonContent(fooType, foo, mediaType: s_mediaTypeHeader);
            Assert.Same(s_mediaTypeHeader, content.Headers.ContentType);

            content = JsonContent.Create(foo, mediaType: s_mediaTypeHeader);
            Assert.Same(s_mediaTypeHeader, content.Headers.ContentType);

            string mediaTypeAsString = s_mediaTypeHeader.MediaType;

            content = new JsonContent(fooType, foo, mediaType: mediaTypeAsString);
            Assert.Equal(mediaTypeAsString, content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            content = JsonContent.Create(foo, mediaType: mediaTypeAsString);
            Assert.Equal(mediaTypeAsString, content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            // JsonContentType define its own charset.
            content = new JsonContent(fooType, foo, mediaType: JsonContentType);
            Assert.Equal(s_mediaTypeHeader.MediaType, content.Headers.ContentType.MediaType);
            Assert.Equal(s_mediaTypeHeader.CharSet, content.Headers.ContentType.CharSet);

            content = JsonContent.Create(foo, mediaType: JsonContentType);
            Assert.Equal(s_mediaTypeHeader.MediaType, content.Headers.ContentType.MediaType);
            Assert.Equal(s_mediaTypeHeader.CharSet, content.Headers.ContentType.CharSet);
        }

        [Fact]
        public void JsonContentMediaTypeIsNull()
        {
            Type fooType = typeof(Foo);
            Foo foo = new Foo();

            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new JsonContent(fooType, foo, mediaType: (MediaTypeHeaderValue)null));
            Assert.Equal("mediaType", ex.ParamName);

            ex = Assert.Throws<ArgumentNullException>(() => JsonContent.Create(foo, mediaType: (MediaTypeHeaderValue)null));
            Assert.Equal("mediaType", ex.ParamName);

            string mediaTypeAsString = s_mediaTypeHeader.MediaType;
            ex = Assert.Throws<ArgumentNullException>(() => new JsonContent(fooType, foo, mediaType: (string)null));
            Assert.Equal("mediaType", ex.ParamName);

            ex = Assert.Throws<ArgumentNullException>(() => JsonContent.Create(foo, mediaType: (string)null));
            Assert.Equal("mediaType", ex.ParamName);
        }

        [Fact]
        public void JsonContentTypeIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonContent(null, null));
            Assert.Throws<ArgumentNullException>(() => new JsonContent(null, null, s_mediaTypeHeader));
        }

        [Fact]
        public void JsonContentThrowsOnIncompatibleType()
        {
            var foo = new Foo();
            Assert.Throws<ArgumentException>(() => new JsonContent(typeof(Bar), foo));
            Assert.Throws<ArgumentException>(() => new JsonContent(typeof(Bar), foo, s_mediaTypeHeader));
        }
    }
}
