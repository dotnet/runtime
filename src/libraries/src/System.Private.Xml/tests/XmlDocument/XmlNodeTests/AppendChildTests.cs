// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class AppendChildTests
    {
        [Fact]
        public static void AppendElementToElementWithChildren()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><b/></a>");

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);

            var element = xmlDocument.CreateElement("comment");

            xmlDocument.DocumentElement.AppendChild(element);

            Assert.Equal(2, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Same(element, xmlDocument.DocumentElement.ChildNodes[1]);
        }

        [Fact]
        public static void AppendAttributeToAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a attr1='test' />");

            var newAttribute = xmlDocument.CreateAttribute("attr2");
            var attr1 = xmlDocument.DocumentElement.Attributes[0];

            Assert.Throws<InvalidOperationException>(() => attr1.AppendChild(newAttribute));
        }

        [Fact]
        public static void AppendAnElementFromOneTreeToAnother()
        {
            var xmlDocument1 = new XmlDocument();
            xmlDocument1.LoadXml("<a attr1='test' />");

            var xmlDocument2 = new XmlDocument();
            xmlDocument2.LoadXml("<b attr2='test2' />");

            AssertExtensions.Throws<ArgumentException>(null, () => xmlDocument2.DocumentElement.AppendChild(xmlDocument1.DocumentElement));
        }

        [Fact]
        public static void CallAppendChildOnCommentNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><!-- comment --></a>");

            var text = xmlDocument.CreateTextNode("text");

            Assert.Throws<InvalidOperationException>(() => xmlDocument.DocumentElement.FirstChild.AppendChild(text));
        }

        [Fact]
        public static void CallAppendChildOnPINode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><?PI pi_info?></a>");

            var text = xmlDocument.CreateTextNode("text");

            Assert.Throws<InvalidOperationException>(() => xmlDocument.DocumentElement.FirstChild.AppendChild(text));
        }

        [Fact]
        public static void CallAppendChildOnTextNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a>text node</a>");

            var text = xmlDocument.CreateTextNode("text");

            Assert.Throws<InvalidOperationException>(() => xmlDocument.DocumentElement.FirstChild.AppendChild(text));
        }
    }
}
