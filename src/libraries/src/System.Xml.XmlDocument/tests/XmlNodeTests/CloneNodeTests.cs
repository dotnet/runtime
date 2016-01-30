// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class CloneNodeTests
    {
        private const string ComplexDocument = "<?xml version=\"1.0\" standalone=\"no\"?><!-- comment1 the different between this one and domtest.xml is att in elem1 --><?PI1 processing instruction?><?xmlnamespace1 ns=\"something\" prefix=\"something\"?><?xmlnamespace2 ns=\"http://www.placeholder-name-here.com/schema/\" prefix=\"my\"?><?xmlnamespace3 ns=\"urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/\" prefix=\"dt\"?><!-- comment2 --><?PI2 processing instruction?><root xmlns:my=\"urn:http://www.placeholder-name-here.com/schema/\" xmlns:dt=\"urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/\">\r\n  text node one\r\n  <elem1 child1=\"\" child2=\"duu\" child3=\"blar\" child4=\"a1\" child5=\"goody\">\r\n     text node two \r\n     <ie att=\"sun\"></ie>\r\n     text node three\r\n  </elem1><!-- comment3 --><?PI3 processing instruction?>\r\n  something \r\n  <foo /><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]><elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"><a /></elem2><elem2> \r\n      elem2-text1\r\n      <a> \r\n          this-is-a    \r\n      </a> \r\n\r\n      elem2-text2\r\n      <!-- elem2-comment1-->\r\n      elem2-text3\r\n\r\n      <b> \r\n          this-is-b\r\n      </b>\r\n\r\n      elem2-text4\r\n      <?elem2_PI elem2-PI?>\r\n      elem2-text5\r\n\r\n  </elem2><my:book style=\"leather\" price=\"29.50\"><my:title>Who's Who in Trenton</my:title><my:author>Robert Bob</my:author></my:book><my:book price=\"99.95\"><my:book></my:book></my:book><my:book dt:style=\"string\" style=\"hard back\" price=\"19.99\"><my:title>Where in the world is Trenton?</my:title></my:book></root><!-- comment --><?PI processing instruction?>";

        private static void CheckChildren(XmlNode parent)
        {
            foreach (XmlNode child in parent.ChildNodes)
            {
                Assert.Same(parent, child.ParentNode);
                CheckChildren(child);
            }

            if (parent.Attributes == null)
                return;

            foreach (XmlNode attribute in parent.Attributes)
            {
                Assert.Same(attribute, attribute.FirstChild.ParentNode);
            }
        }

        [Fact]
        public static void CloneComplexDocumentTrue()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(ComplexDocument);
            var cloned = xmlDocument.CloneNode(true);

            Assert.NotSame(xmlDocument.ChildNodes, cloned.ChildNodes);
            Assert.Equal(xmlDocument.NodeType, cloned.NodeType);
            Assert.Equal(xmlDocument.ChildNodes.Count, cloned.ChildNodes.Count);
            Assert.Equal(xmlDocument.LastChild.Value, cloned.LastChild.Value);

            // Test XmlNode.LastChild
            {
                for (var i = 0; i < xmlDocument.ChildNodes.Count; i++)
                {
                    if (xmlDocument.ChildNodes[i].LastChild == null)
                    {
                        Assert.Equal(xmlDocument.ChildNodes[i].LastChild, cloned.ChildNodes[i].LastChild);
                    }
                    else
                    {
                        Assert.Equal(xmlDocument.ChildNodes[i].LastChild.Value, cloned.ChildNodes[i].LastChild.Value);
                    }
                }
            }

            // Test XmlNode.NextSibling
            {
                var count = cloned.ChildNodes.Count;
                var previousNode = cloned.ChildNodes[0];

                for (var idx = 1; idx < count; idx++)
                {
                    var currentNode = cloned.ChildNodes[idx];
                    Assert.Equal(currentNode, previousNode.NextSibling);
                    previousNode = currentNode;
                }

                Assert.Null(previousNode.NextSibling);
            }

            // Test XmlNode.PreviousSibling
            {
                var count = cloned.ChildNodes.Count;
                var nextNode = cloned.ChildNodes[count - 1];

                for (var idx = count - 2; idx >= 0; idx--)
                {
                    var currentNode = cloned.ChildNodes[idx];
                    Assert.Equal(currentNode, nextNode.PreviousSibling);
                    nextNode = currentNode;
                }

                Assert.Null(nextNode.PreviousSibling);
            }

            // Test XmlNode.ParentNode
            CheckChildren(cloned);
        }

        [Fact]
        public static void CloneComplexDocumentFalse()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(ComplexDocument);
            var cloned = xmlDocument.DocumentElement.CloneNode(false);

            Assert.NotSame(xmlDocument.ChildNodes, cloned.ChildNodes);
            Assert.Equal(12, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(0, cloned.ChildNodes.Count);
            Assert.Null(cloned.LastChild);
            Assert.Null(cloned.NextSibling);
        }

        [Fact]
        public static void CloneComplexDocumentTrueAndManipulate()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a>test</a>");
            var cloned = xmlDocument.CloneNode(true);

            cloned.FirstChild.FirstChild.Value = "replaced";

            Assert.Equal("test", xmlDocument.FirstChild.FirstChild.Value);
            Assert.Equal("replaced", cloned.FirstChild.FirstChild.Value);
        }

        [Fact]
        public static void CloneRemovedAttributeNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a attr='test'></a>");

            var element = (XmlElement)xmlDocument.FirstChild;
            var removedAttribute = element.RemoveAttributeNode(element.Attributes[0]);

            var clonedTrue = removedAttribute.CloneNode(true);
            Assert.Equal(XmlNodeType.Attribute, clonedTrue.NodeType);
            Assert.Equal("attr", clonedTrue.Name);
            Assert.Equal("test", clonedTrue.Value);

            var clonedFalse = removedAttribute.CloneNode(false);
            Assert.Equal(XmlNodeType.Attribute, clonedFalse.NodeType);
            Assert.Equal("attr", clonedFalse.Name);
            Assert.Equal("test", clonedFalse.Value);
        }
    }
}
