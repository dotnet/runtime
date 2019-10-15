// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class XmlUriDataTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var data = new XmlUriData();
            Assert.Null(data.UriString);
            Assert.Equal(UriKind.RelativeOrAbsolute, data.UriKind);
            Assert.Null(data.ElementQualifiedName);
        }

        public static IEnumerable<object[]> Ctor_String_UriKind_XmlQualifiedName_TestData()
        {
            yield return new object[] { null, UriKind.Absolute, null };
            yield return new object[] { "", UriKind.RelativeOrAbsolute, new XmlQualifiedName() };
            yield return new object[] { "htp://microsoft.com", UriKind.Absolute, new XmlQualifiedName("name") };
            yield return new object[] { "/relative", UriKind.Relative, new XmlQualifiedName("name") };
            yield return new object[] { "/relative", UriKind.RelativeOrAbsolute - 1, new XmlQualifiedName("name") };
            yield return new object[] { "/relative", UriKind.Relative + 1, new XmlQualifiedName("name") };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_UriKind_XmlQualifiedName_TestData))]
        public void Ctor_String_UriKind_XmlQualifiedName(string uriString, UriKind uriKind, XmlQualifiedName elementQualifiedName)
        {
            var data = new XmlUriData(uriString, uriKind, elementQualifiedName);
            Assert.Equal(uriString, data.UriString);
            Assert.Equal(uriKind, data.UriKind);
            Assert.Equal(elementQualifiedName, data.ElementQualifiedName);
        }
    }
}
