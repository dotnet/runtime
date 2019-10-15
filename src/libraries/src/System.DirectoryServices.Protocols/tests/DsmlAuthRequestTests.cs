// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class DsmlAuthRequestTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var request = new DsmlAuthRequest();
            Assert.Empty(request.Controls);
            Assert.Empty(request.Principal);
            Assert.Null(request.RequestId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Principal")]
        public void Ctor_Principal(string principal)
        {
            var request = new DsmlAuthRequest(principal);
            Assert.Empty(request.Controls);
            Assert.Equal(principal, request.Principal);
            Assert.Null(request.RequestId);
        }

        [Fact]
        public void Principal_Set_GetReturnsExpected()
        {
            var request = new DsmlAuthRequest { Principal = "Principal" };
            Assert.Equal("Principal", request.Principal);
        }

        [Fact]
        public void RequestId_Set_GetReturnsExpected()
        {
            var request = new DsmlAuthRequest { RequestId = "Id" };
            Assert.Equal("Id", request.RequestId);
        }
    }
}
