// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Http.Tests
{
    public class HttpRequestExceptionTests
    {
        [Fact]
        public void DefaultConstructors_HasNoStatusCode()
        {
            var exception = new HttpRequestException();
            Assert.Null(exception.StatusCode);
            
            exception = new HttpRequestException("message");
            Assert.Null(exception.StatusCode);
            
            exception = new HttpRequestException("message", new InvalidOperationException());
            Assert.Null(exception.StatusCode);
        }

        [Fact]
        public void StoresStatusCode()
        {
            var exception = new HttpRequestException("message", null, HttpStatusCode.InternalServerError);
            Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        }

        [Fact]
        public void StoresNonStandardStatusCode()
        {
            var statusCode = (HttpStatusCode)999;

            var exception = new HttpRequestException("message", null, statusCode);
            Assert.Equal(statusCode, exception.StatusCode);
        }
    }
}
