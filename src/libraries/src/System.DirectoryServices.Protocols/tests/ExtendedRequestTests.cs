// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class ExtendedRequestTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var request = new ExtendedRequest();
            Assert.Empty(request.Controls);
            Assert.Null(request.RequestId);
            Assert.Null(request.RequestName);
            Assert.Empty(request.RequestValue);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("RequestName")]
        public void Ctor_RequestName(string requestName)
        {
            var request = new ExtendedRequest(requestName);
            Assert.Empty(request.Controls);
            Assert.Null(request.RequestId);
            Assert.Equal(requestName, request.RequestName);
            Assert.Empty(request.RequestValue);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("RequestName", new byte[] { 1, 2, 3 })]
        public void Ctor_RequestName_RequestValue(string requestName, byte[] requestValue)
        {
            var request = new ExtendedRequest(requestName, requestValue);
            Assert.Empty(request.Controls);
            Assert.Null(request.RequestId);
            Assert.Equal(requestName, request.RequestName);
            Assert.NotSame(requestValue, request.RequestValue);
            Assert.Equal(requestValue ?? Array.Empty<byte>(), request.RequestValue);
        }

        [Fact]
        public void RequestName_Set_GetReturnsExpected()
        {
            var request = new ExtendedRequest { RequestName = "RequestName" };
            Assert.Equal("RequestName", request.RequestName);
        }

        [Fact]
        public void RequestValue_Set_GetReturnsExpected()
        {
            var request = new ExtendedRequest { RequestValue = new byte[] { 1, 2, 3 } };
            Assert.Equal(new byte[] { 1, 2, 3 }, request.RequestValue);
        }

        [Fact]
        public void RequestId_Set_GetReturnsExpected()
        {
            var request = new ExtendedRequest { RequestId = "Id" };
            Assert.Equal("Id", request.RequestId);
        }
    }
}
