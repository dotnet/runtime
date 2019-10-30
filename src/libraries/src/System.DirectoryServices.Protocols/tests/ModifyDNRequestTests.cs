// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class ModifyDNRequestTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var request = new ModifyDNRequest();
            Assert.Empty(request.Controls);
            Assert.Null(request.DistinguishedName);
            Assert.Null(request.NewName);
            Assert.Null(request.NewParentDistinguishedName);
            Assert.True(request.DeleteOldRdn);
            Assert.Null(request.RequestId);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("DistinguishedName", "NewParentDistinguishedName", "NewName")]
        public void Ctor_DistinguishedName_NewParentDistinguishedName_NewName(string distinguishedName, string newParentDistinguishedName, string newName)
        {
            var request = new ModifyDNRequest(distinguishedName, newParentDistinguishedName, newName);
            Assert.Empty(request.Controls);
            Assert.Equal(distinguishedName, request.DistinguishedName);
            Assert.Equal(newName, request.NewName);
            Assert.Equal(newParentDistinguishedName, request.NewParentDistinguishedName);
            Assert.True(request.DeleteOldRdn);
            Assert.Null(request.RequestId);
        }

        [Fact]
        public void DistinguishedName_Set_GetReturnsExpected()
        {
            var request = new ModifyDNRequest { DistinguishedName = "Name" };
            Assert.Equal("Name", request.DistinguishedName);
        }

        [Fact]
        public void NewParentDistinguishedName_Set_GetReturnsExpected()
        {
            var request = new ModifyDNRequest { NewParentDistinguishedName = "NewParentDistinguishedName" };
            Assert.Equal("NewParentDistinguishedName", request.NewParentDistinguishedName);
        }

        [Fact]
        public void NewName_Set_GetReturnsExpected()
        {
            var request = new ModifyDNRequest { NewName = "NewName" };
            Assert.Equal("NewName", request.NewName);
        }

        [Fact]
        public void DeleteOldRdn_Set_GetReturnsExpected()
        {
            var request = new ModifyDNRequest { DeleteOldRdn = false };
            Assert.False(request.DeleteOldRdn);
        }

        [Fact]
        public void RequestId_Set_GetReturnsExpected()
        {
            var request = new ModifyDNRequest { RequestId = "Id" };
            Assert.Equal("Id", request.RequestId);
        }
    }
}
