// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class SplitTextTests
    {
        [Fact]
        public static void SplitAtBeginningOfString()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc><elem1>This is a test</elem1></doc>");


            var node = xmlDocument.DocumentElement.FirstChild.FirstChild;
            var splitNode = ((XmlText)node).SplitText(0);

            Assert.Equal(String.Empty, node.Value);
            Assert.Equal("This is a test", splitNode.Value);
        }

        /// <summary>
        /// Using hard-coded offsets to SplitText may have unintended side effects since \r\n will 
        /// be converted to \n in accordance with the XML spec located at 
        /// http://www.w3.org/TR/2008/REC-xml-20081126/#sec-line-ends.  Better to calculate the offset 
        /// given to XmlText.SplitText using String.IndexOf or some other similar method.
        /// </summary>
        [Fact]
        public static void TextWithWhiteSpace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>\r\n  text node one\r\n  </root>");

            var initialTextNode = (XmlText)xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);

            var splitTextNode1 = initialTextNode.SplitText(initialTextNode.Value.IndexOf("text"));
            var splitTextNode2 = splitTextNode1.SplitText(splitTextNode1.Value.IndexOf(" one"));
            var splitTextNode3 = splitTextNode1.SplitText(splitTextNode1.Value.IndexOf(" "));

            Assert.Equal("text", splitTextNode1.Value);
            Assert.Equal(" node", splitTextNode3.Value);

            Assert.Equal(XmlNodeType.Text, splitTextNode2.NodeType);
            Assert.Equal(XmlNodeType.Text, splitTextNode3.NodeType);
            Assert.Equal(4, xmlDocument.DocumentElement.ChildNodes.Count);
        }

        [Fact]
        public static void SplitLongTextNodeAndNormalize()
        {
            var xml = @" <T>This is a very long text node that contains &lt;, and &amp;. We are going to use method SplitText.</T> ";

            var xmlDocument = new XmlDocument { PreserveWhitespace = true };
            xmlDocument.LoadXml(xml);

            var parent = xmlDocument.DocumentElement;

            Assert.Equal(parent.FirstChild, parent.LastChild);

            var text = (XmlText)parent.FirstChild;
            var original = parent.InnerXml;

            for (int i = 0; i < 6; i++)
            {
                XmlText newText = text.SplitText(text.OuterXml.Length / 2);

                Assert.Equal(text.ParentNode, parent);
                Assert.Equal(newText.ParentNode, parent);
                Assert.NotNull(text);
                Assert.NotNull(newText);
                Assert.Equal(newText, text.NextSibling);
                Assert.Equal(text, newText.PreviousSibling);
                Assert.Equal(parent.ChildNodes.Count, i + 2);
                Assert.Equal(original, parent.InnerXml);

                text = newText;
            }

            parent.Normalize();

            Assert.Equal(1, parent.ChildNodes.Count);
            Assert.Equal(original, parent.InnerXml);
        }
    }
}
