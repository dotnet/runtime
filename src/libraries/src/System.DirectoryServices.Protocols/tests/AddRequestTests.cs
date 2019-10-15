// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class AddRequestTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var request = new AddRequest();
            Assert.Empty(request.Attributes);
            Assert.Empty(request.Controls);
            Assert.Null(request.DistinguishedName);
            Assert.Null(request.RequestId);
        }

        public static IEnumerable<object[]> Ctor_DistinguishedName_Attributes_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, new DirectoryAttribute[0] };
            yield return new object[] { "DistinguishedName", new DirectoryAttribute[] { new DirectoryAttribute("name", "value") } };
        }

        [Theory]
        [MemberData(nameof(Ctor_DistinguishedName_Attributes_TestData))]
        public void Ctor_DistinguishedString_Attributes(string distinguishedName, DirectoryAttribute[] attributes)
        {
            var request = new AddRequest(distinguishedName, attributes);
            Assert.Equal(attributes ?? Enumerable.Empty<DirectoryAttribute>(), request.Attributes.Cast<DirectoryAttribute>());
            Assert.Empty(request.Controls);
            Assert.Equal(distinguishedName, request.DistinguishedName);
            Assert.Null(request.RequestId);
        }

        [Fact]
        public void Ctor_NullObjectInAttributes_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new AddRequest("DistinguishedName", new DirectoryAttribute[] { null }));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("DistinguishedName", "ObjectClass")]
        public void Ctor_DistinguishedName_ObjectClass(string distinguishedName, string objectClass)
        {
            var request = new AddRequest(distinguishedName, objectClass);
            DirectoryAttribute attribute = (DirectoryAttribute)Assert.Single(request.Attributes);
            Assert.Equal("objectClass", attribute.Name);
            Assert.Equal(new string[] { objectClass }, attribute.GetValues(typeof(string)));

            Assert.Empty(request.Controls);
            Assert.Equal(distinguishedName, request.DistinguishedName);
            Assert.Null(request.RequestId);
        }

        [Fact]
        public void Ctor_NullObjectClass_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("objectClass", () => new AddRequest("DistinguishedName", (string)null));
        }

        [Fact]
        public void DistinguishedName_Set_GetReturnsExpected()
        {
            var request = new AddRequest { DistinguishedName = "Name" };
            Assert.Equal("Name", request.DistinguishedName);
        }

        [Fact]
        public void RequestId_Set_GetReturnsExpected()
        {
            var request = new AddRequest { RequestId = "Id" };
            Assert.Equal("Id", request.RequestId);
        }
    }
}
