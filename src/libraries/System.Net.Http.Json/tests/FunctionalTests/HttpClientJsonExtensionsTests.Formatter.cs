// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    // Tests taken from https://github.com/aspnet/AspNetWebStack/blob/master/test/System.Net.Http.Formatting.Test/HttpClientExtensionsTest.cs
    public class HttpClientExtensionsTest
    {
        //private readonly MediaTypeFormatter _formatter = new MockMediaTypeFormatter { CallBase = true };
        private readonly HttpClient _client = new HttpClient();
        // TODO: Use this for JsonContent unit tests
        //private readonly MediaTypeHeaderValue _mediaTypeHeader = MediaTypeHeaderValue.Parse("foo/bar; charset=utf-16");

        //public HttpClientExtensionsTest()
        //{
        //    Mock<TestableHttpMessageHandler> handlerMock = new Mock<TestableHttpMessageHandler> { CallBase = true };
        //    handlerMock
        //        .Setup(h => h.SendAsyncPublic(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
        //        .Returns((HttpRequestMessage request, CancellationToken _) => Task.FromResult(new HttpResponseMessage() { RequestMessage = request }));

        //    _client = new HttpClient(handlerMock.Object);
        //}

        [Fact]
        public async Task PostAsJsonAsync_String_WhenClientIsNull_ThrowsException()
        {
            HttpClient client = null;

            ArgumentNullException ex =  await Assert.ThrowsAsync<ArgumentNullException>(() => client.PostAsJsonAsync("http://www.example.com", new object()));
            Assert.Equal("client", ex.ParamName);
        }

        [Fact]
        public void PostAsJsonAsync_String_WhenUriIsNull_ThrowsExceptionAsync()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.PostAsJsonAsync((string)null, new object()));
        }

        [Fact]
        public async Task PostAsJsonAsync_Uri_WhenUriIsNull_ThrowsException()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _client.PostAsJsonAsync((Uri)null, new object()));
        }

        [Fact]
        public async Task PostAsJsonAsync_String_UsesJsonMediaTypeFormatter()
        {
            var response = await _client.PostAsJsonAsync("http://example.com", new object());

            JsonContent content = Assert.IsType<JsonContent>(response.RequestMessage.Content);
            //Assert.IsType<JsonMediaTypeFormatter>(content.Formatter);
            //??
        }

        [Fact]
        public async Task PostAsync_String_WhenRequestUriIsSet_CreatesRequestWithAppropriateUri()
        {
            _client.BaseAddress = new Uri("http://example.com/");

            var response = await _client.PostAsJsonAsync("myapi/", new object());

            var request = response.RequestMessage;
            Assert.Equal("http://example.com/myapi/", request.RequestUri.ToString());
        }

        [Fact]
        public async Task PostAsJsonAsync_Uri_WhenClientIsNull_ThrowsExceptionAsync()
        {
            HttpClient client = null;

            ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.PostAsJsonAsync(new Uri("http://www.example.com"), new object()));
            Assert.Equal("client", ex.ParamName);
        }

        [Fact]
        public async Task PostAsJsonAsync_Uri_UsesJsonMediaTypeFormatter()
        {
            var response = await _client.PostAsJsonAsync(new Uri("http://example.com"), new object());

            var content = Assert.IsType<JsonContent>(response.RequestMessage.Content);
            //Assert.IsType<JsonMediaTypeFormatter>(content.Formatter);
        }
    }
}
