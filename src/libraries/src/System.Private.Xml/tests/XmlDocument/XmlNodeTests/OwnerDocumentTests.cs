// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class OwnerDocumentTests
    {
        [Fact]
        public static void CheckOwnerOfDocumentNode()
        {
            var xmlDocument = new XmlDocument();

            Assert.Null(xmlDocument.OwnerDocument);
        }
        [Fact]
        public static void FromLoadedDocument()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><elem attr='test' /></root>");

            var element = xmlDocument.DocumentElement.FirstChild as XmlElement;
            Assert.NotNull(element);
            Assert.Equal(1, element.Attributes.Count);

            var attribute = element.Attributes[0];
            Assert.Equal(xmlDocument, attribute.OwnerDocument);
        }

        [Fact]
        public static void AttachingDetachingAttribute()
        {
            var xmlDocument = new XmlDocument();

            var attribute = xmlDocument.CreateAttribute("att");
            Assert.Equal(xmlDocument, attribute.OwnerDocument);

            var element = xmlDocument.CreateElement("elem");
            element.Attributes.Append(attribute);
            Assert.Equal(xmlDocument, attribute.OwnerDocument);

            element.RemoveAttribute("att");
            Assert.Equal(xmlDocument, attribute.OwnerDocument);
        }

        [Fact]
        public static void OwnerDocumentOnImportedTree()
        {
            var tempDoc = new XmlDocument();
            var nodeToImport = tempDoc.CreateDocumentFragment();

            nodeToImport.AppendChild(tempDoc.CreateElement("A1"));
            nodeToImport.AppendChild(tempDoc.CreateComment("comment"));
            nodeToImport.AppendChild(tempDoc.CreateProcessingInstruction("PI", "donothing"));

            var xmlDocument = new XmlDocument();
            var node = xmlDocument.ImportNode(nodeToImport, true);

            Assert.Equal(xmlDocument, node.OwnerDocument);

            foreach (XmlNode child in node.ChildNodes)
                Assert.Equal(xmlDocument, child.OwnerDocument);
        }

        [Fact]
        public static void VerifyAllTypes()
        {
            VerifyOwnerOfGivenType(XmlNodeType.Attribute);
            VerifyOwnerOfGivenType(XmlNodeType.CDATA);
            VerifyOwnerOfGivenType(XmlNodeType.Comment);
            VerifyOwnerOfGivenType(XmlNodeType.DocumentFragment);
            VerifyOwnerOfGivenType(XmlNodeType.DocumentType);
            VerifyOwnerOfGivenType(XmlNodeType.Element);
            VerifyOwnerOfGivenType(XmlNodeType.ProcessingInstruction);
            VerifyOwnerOfGivenType(XmlNodeType.SignificantWhitespace);
            VerifyOwnerOfGivenType(XmlNodeType.Text);
            VerifyOwnerOfGivenType(XmlNodeType.Whitespace);
            VerifyOwnerOfGivenType(XmlNodeType.XmlDeclaration);
        }

        #region Helper methods
        private static void VerifyOwnerOfGivenType(XmlNodeType nodeType)
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateNode(nodeType, "test", string.Empty);

            Assert.Equal(xmlDocument, node.OwnerDocument);
        }
        #endregion
    }
}
