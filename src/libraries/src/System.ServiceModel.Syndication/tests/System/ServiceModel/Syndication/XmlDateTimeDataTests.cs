// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class XmlDateTimeDataTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var data = new XmlDateTimeData();
            Assert.Null(data.DateTimeString);
            Assert.Null(data.ElementQualifiedName);
        }

        public static IEnumerable<object[]> Ctor_String_XmlQualifiedName_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { "", new XmlQualifiedName() };
            yield return new object[] { "date", new XmlQualifiedName("name") };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_XmlQualifiedName_TestData))]
        public void Ctor_String_XmlQualifiedName(string dateTimeString, XmlQualifiedName elementQualifiedName)
        {
            var data = new XmlDateTimeData(dateTimeString, elementQualifiedName);
            Assert.Equal(dateTimeString, data.DateTimeString);
            Assert.Equal(elementQualifiedName, data.ElementQualifiedName);
        }
    }
}
