// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class ItemTests
    {
        [Fact]
        public static void ValidItemTest()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two text node three </elem1>");

            var namedNodeMap = (XmlNamedNodeMap)xmlDocument.FirstChild.Attributes;

            for (int idx = 0; idx < namedNodeMap.Count; idx++)
                Assert.NotNull(namedNodeMap.Item(idx));

            Assert.Null(namedNodeMap.Item(namedNodeMap.Count));
        }

        [Fact]
        public static void InvalidItemTest()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two text node three </elem1>");

            var namedNodeMap = (XmlNamedNodeMap)xmlDocument.FirstChild.Attributes;

            Assert.Null(namedNodeMap.Item(-1));
            Assert.Null(namedNodeMap.Item(namedNodeMap.Count));
        }
    }
}
