// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class DeleteRequestTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var request = new DeleteRequest();
            Assert.Empty(request.Controls);
            Assert.Null(request.DistinguishedName);
            Assert.Null(request.RequestId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("DistinguishedName")]
        public void Ctor_DistinguishedName(string distinguishedName)
        {
            var request = new DeleteRequest(distinguishedName);
            Assert.Empty(request.Controls);
            Assert.Equal(distinguishedName, request.DistinguishedName);
            Assert.Null(request.RequestId);
        }

        [Fact]
        public void DistinguishedName_Set_GetReturnsExpected()
        {
            var request = new DeleteRequest { DistinguishedName = "Name" };
            Assert.Equal("Name", request.DistinguishedName);
        }

        [Fact]
        public void RequestId_Set_GetReturnsExpected()
        {
            var request = new DeleteRequest { RequestId = "Id" };
            Assert.Equal("Id", request.RequestId);
        }
    }
}
