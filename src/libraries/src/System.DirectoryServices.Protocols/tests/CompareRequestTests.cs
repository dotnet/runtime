// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class CompareRequestTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var request = new CompareRequest();
            Assert.Empty(request.Assertion);
            Assert.Empty(request.Controls);
            Assert.Null(request.DistinguishedName);
            Assert.Null(request.RequestId);
        }

        [Theory]
        [InlineData(null, "", "")]
        [InlineData("DistinguishedName", "AttributeName", "value")]
        public void Ctor_DistinguishedName_AttributeName_StringValue(string distinguishedName, string attributeName, string value)
        {
            var request = new CompareRequest(distinguishedName, attributeName, value);
            Assert.Equal(attributeName, request.Assertion.Name);
            Assert.Equal(new string[] { value }, request.Assertion.GetValues(typeof(string)));

            Assert.Empty(request.Controls);
            Assert.Equal(distinguishedName, request.DistinguishedName);
            Assert.Null(request.RequestId);
        }

        [Theory]
        [InlineData(null, "", new byte[0])]
        [InlineData("DistinguishedName", "AttributeName", new byte[] { 1, 2, 3 })]
        public void Ctor_DistinguishedName_AttributeName_ByteArrayValue(string distinguishedName, string attributeName, byte[] value)
        {
            var request = new CompareRequest(distinguishedName, attributeName, value);
            Assert.Equal(attributeName, request.Assertion.Name);
            Assert.Equal(new byte[][] { value }, request.Assertion.GetValues(typeof(byte[])));

            Assert.Empty(request.Controls);
            Assert.Equal(distinguishedName, request.DistinguishedName);
            Assert.Null(request.RequestId);
        }

        [Fact]
        public void Ctor_DistinguishedName_AttributeName_Uri()
        {
            var uri = new Uri("http://microsoft.com");
            var request = new CompareRequest("DistinguishedName", "AttributeName", uri);

            Assert.Equal("AttributeName", request.Assertion.Name);
            Assert.Equal(uri, Assert.Single(request.Assertion));

            Assert.Empty(request.Controls);
            Assert.Equal("DistinguishedName", request.DistinguishedName);
            Assert.Null(request.RequestId);
        }

        [Fact]
        public void Ctor_NullAttributeName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("attributeName", () => new CompareRequest("DistinguishedName", null, "Value"));
            AssertExtensions.Throws<ArgumentNullException>("attributeName", () => new CompareRequest("DistinguishedName", null, new byte[0]));
            AssertExtensions.Throws<ArgumentNullException>("attributeName", () => new CompareRequest("DistinguishedName", null, new Uri("http://microsoft.com")));
        }

        [Fact]
        public void Ctor_NullValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new CompareRequest("DistinguishedName", "AttributeName", (string)null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => new CompareRequest("DistinguishedName", "AttributeName", (byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => new CompareRequest("DistinguishedName", "AttributeName", (Uri)null));
        }

        [Fact]
        public void Ctor_DistinguishedName_Assertion()
        {
            var assertion = new DirectoryAttribute { "value" };
            var request = new CompareRequest("DistinguishedName", assertion);

            Assert.NotSame(assertion, request.Assertion);
            Assert.Equal(assertion, request.Assertion);
            Assert.Empty(request.Controls);
            Assert.Equal("DistinguishedName", request.DistinguishedName);
            Assert.Null(request.RequestId);
        }

        [Fact]
        public void Ctor_NullAssertion_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("assertion", () => new CompareRequest("DistinguishedName", null));
        }

        public static IEnumerable<object[]> InvalidAssertion_TestData()
        {
            yield return new object[] { new DirectoryAttribute { "value1", "value2" } };
            yield return new object[] { new DirectoryAttribute() };
        }

        [Theory]
        [MemberData(nameof(InvalidAssertion_TestData))]
        public void Ctor_InvalidAssertion_ThrowsArgumentException(DirectoryAttribute assertion)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new CompareRequest("DistinguishedName", assertion));
        }

        [Fact]
        public void DistinguishedName_Set_GetReturnsExpected()
        {
            var request = new CompareRequest { DistinguishedName = "Name" };
            Assert.Equal("Name", request.DistinguishedName);
        }
    }
}
