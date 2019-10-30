// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class ModifyRequestTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var request = new ModifyRequest();
            Assert.Empty(request.Controls);
            Assert.Null(request.DistinguishedName);
            Assert.Empty(request.Modifications);
            Assert.Null(request.RequestId);
        }

        public static IEnumerable<object[]> Ctor_DistinguishedName_Modifications_TestData()
        {
            yield return new object[] { string.Empty, new DirectoryAttributeModification[0] };
            yield return new object[] { "DistinguishedName", new DirectoryAttributeModification[] { new DirectoryAttributeModification() } };
        }

        [Theory]
        [MemberData(nameof(Ctor_DistinguishedName_Modifications_TestData))]
        public void Ctor_DistinguishedString_Modifications(string distinguishedName, DirectoryAttributeModification[] modifications)
        {
            var request = new ModifyRequest(distinguishedName, modifications);
            Assert.Empty(request.Controls);
            Assert.Equal(distinguishedName, request.DistinguishedName);
            Assert.Equal(modifications ?? Enumerable.Empty<DirectoryAttributeModification>(), request.Modifications.Cast<DirectoryAttributeModification>());
            Assert.Null(request.RequestId);
        }

        [Fact]
        public void Ctor_NullModifications_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("attributes", () => new ModifyRequest("DistinguishedName", null));
        }

        [Fact]
        public void Ctor_NullObjectInAttributes_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new ModifyRequest("DistinguishedName", new DirectoryAttributeModification[] { null }));
        }

        public static IEnumerable<object[]> Ctor_DistinguishedName_Operation_AttributeName_Values_TestData()
        {
            yield return new object[] { null, null, "AttributeName", null };
            yield return new object[] { string.Empty, null, "AttributeName", new object[0] };
            yield return new object[] { "DistinguishedName", new DirectoryAttributeOperation(), "AttributeName", new object[] { "1", "2" } };
        }

        [Theory]
        [MemberData(nameof(Ctor_DistinguishedName_Operation_AttributeName_Values_TestData))]
        public void Ctor_DistinguishedName_Operation_AttributeName_Values(string distinguishedName, DirectoryAttributeOperation operation, string attributeName, object[] values)
        {
            var request = new ModifyRequest(distinguishedName, operation, attributeName, values);
            Assert.Empty(request.Controls);
            DirectoryAttributeModification modification = (DirectoryAttributeModification)Assert.Single(request.Modifications);
            Assert.Equal(attributeName, modification.Name);
            Assert.Equal(operation, modification.Operation);
            Assert.Equal(values ?? Enumerable.Empty<object>(), modification.Cast<object>());

            Assert.Equal(distinguishedName, request.DistinguishedName);
            Assert.Null(request.RequestId);
        }

        [Fact]
        public void Ctor_NullAttributeName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("attributeName", () => new ModifyRequest("DistinguishedName", new DirectoryAttributeOperation(), null, new object[0]));
        }

        [Fact]
        public void Ctor_NullObjectInValues_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new ModifyRequest("DistinguishedName", new DirectoryAttributeOperation(), "AttributeName", new object[] { null }));
        }

        [Fact]
        public void Ctor_InvalidObjectInValues_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("value", () => new ModifyRequest("DistinguishedName", new DirectoryAttributeOperation(), "AttributeName", new object[] { 1 }));
        }

        [Fact]
        public void DistinguishedName_Set_GetReturnsExpected()
        {
            var request = new ModifyRequest { DistinguishedName = "Name" };
            Assert.Equal("Name", request.DistinguishedName);
        }

        [Fact]
        public void RequestId_Set_GetReturnsExpected()
        {
            var request = new ModifyRequest { RequestId = "Id" };
            Assert.Equal("Id", request.RequestId);
        }
    }
}
