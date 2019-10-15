// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class AttributesTests
    {
        [Fact]
        public static void GetAttributesOnAnElementWithAttributes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a attr1='test' attr2='test2' />");

            Assert.Equal(2, xmlDocument.DocumentElement.Attributes.Count);

            Assert.Equal("attr1", xmlDocument.DocumentElement.Attributes[0].Name);
            Assert.Equal("test", xmlDocument.DocumentElement.Attributes[0].Value);

            Assert.Equal("attr2", xmlDocument.DocumentElement.Attributes[1].Name);
            Assert.Equal("test2", xmlDocument.DocumentElement.Attributes[1].Value);
        }

        [Fact]
        public static void GetAttributesOnAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a attr1='test' />");

            Assert.Null(xmlDocument.DocumentElement.Attributes[0].Attributes);
        }

        [Fact]
        public static void GetAttributesOnProcessingInstruction()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><?PI pi_info?></a>");

            Assert.Null(xmlDocument.DocumentElement.FirstChild.Attributes);
        }

        [Fact]
        public static void GetAttributesOnCommentNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><!-- comment --></a>");

            Assert.Null(xmlDocument.DocumentElement.FirstChild.Attributes);
        }

        [Fact]
        public static void GetAttributesOnTextNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a>text node</a>");

            Assert.Null(xmlDocument.DocumentElement.FirstChild.Attributes);
        }

        [Fact]
        public static void GetAttributesOnCDataNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><![CDATA[test data]]></a>");

            Assert.Null(xmlDocument.DocumentElement.FirstChild.Attributes);
        }

        [Fact]
        public static void GetAttributesOnDocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            var documentFragment = xmlDocument.CreateDocumentFragment();

            Assert.Null(documentFragment.Attributes);
        }
    }
}
