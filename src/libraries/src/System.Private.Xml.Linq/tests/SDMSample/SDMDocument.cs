// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Xml.Linq;
using Xunit;

namespace XDocumentTests.SDMSample
{
    public class SDM_Document
    {
        /// <summary>
        /// Validate behavior of the default XDocument constructor.
        /// </summary>
        [Fact]
        public void CreateEmptyDocument()
        {
            XDocument doc = new XDocument();

            Assert.Null(doc.Parent);
            Assert.Null(doc.Root);
            Assert.Null(doc.Declaration);
            Assert.Empty(doc.Nodes());
        }

        /// <summary>
        /// Validate behavior of the XDocument constructor that takes content.
        /// </summary>
        [Fact]
        public void CreateDocumentWithContent()
        {
            XDeclaration declaration = new XDeclaration("1.0", "utf-8", "yes");
            XComment comment = new XComment("This is a document");
            XProcessingInstruction instruction = new XProcessingInstruction("doc-target", "doc-data");
            XElement element = new XElement("RootElement");

            XDocument doc = new XDocument(declaration, comment, instruction, element);

            Assert.Equal(new XNode[] { comment, instruction, element }, doc.Nodes());
        }

        /// <summary>
        /// Validate behavior of the XDocument copy/clone constructor.
        /// </summary>
        [Fact]
        public void CreateDocumentCopy()
        {
            Assert.Throws<ArgumentNullException>(() => new XDocument((XDocument)null));

            XDeclaration declaration = new XDeclaration("1.0", "utf-8", "yes");
            XComment comment = new XComment("This is a document");
            XProcessingInstruction instruction = new XProcessingInstruction("doc-target", "doc-data");
            XElement element = new XElement("RootElement");

            XDocument doc = new XDocument(declaration, comment, instruction, element);

            XDocument doc2 = new XDocument(doc);

            IEnumerator e = doc2.Nodes().GetEnumerator();

            // First node: declaration
            Assert.Equal(doc.Declaration.ToString(), doc2.Declaration.ToString());

            // Next node: comment
            Assert.True(e.MoveNext());
            Assert.IsType<XComment>(e.Current);
            Assert.NotSame(comment, e.Current);

            XComment comment2 = (XComment)e.Current;
            Assert.Equal(comment.Value, comment2.Value);

            // Next node: processing instruction
            Assert.True(e.MoveNext());
            Assert.IsType<XProcessingInstruction>(e.Current);
            Assert.NotSame(instruction, e.Current);

            XProcessingInstruction instruction2 = (XProcessingInstruction)e.Current;
            Assert.Equal(instruction.Target, instruction2.Target);
            Assert.Equal(instruction.Data, instruction2.Data);

            // Next node: element.
            Assert.True(e.MoveNext());
            Assert.IsType<XElement>(e.Current);
            Assert.NotSame(element, e.Current);

            XElement element2 = (XElement)e.Current;
            Assert.Equal(element.Name.ToString(), element2.Name.ToString());
            Assert.Empty(element2.Nodes());

            // Should be end.
            Assert.False(e.MoveNext());
        }

        /// <summary>
        /// Validate behavior of the XDocument XmlDeclaration property.
        /// </summary>
        [Fact]
        public void DocumentXmlDeclaration()
        {
            XDocument doc = new XDocument();
            Assert.Null(doc.Declaration);

            XDeclaration dec = new XDeclaration("1.0", "utf-16", "yes");
            XDocument doc2 = new XDocument(dec);
            Assert.Same(dec, doc2.Declaration);

            doc2.RemoveNodes();
            Assert.NotNull(doc2.Declaration);
        }

        /// <summary>
        /// Validate behavior of the XDocument Root property.
        /// </summary>
        [Fact]
        public void DocumentRoot()
        {
            XDocument doc = new XDocument();
            Assert.Null(doc.Root);

            XElement e = new XElement("element");
            doc.Add(e);
            Assert.Same(e, doc.Root);

            doc.RemoveNodes();
            doc.Add(new XComment("comment"));
            Assert.Null(doc.Root);
        }

        /// <summary>
        /// Validate behavior of adding string content to a document.
        /// </summary>
        [Fact]
        public void DocumentAddString()
        {
            XDocument doc = new XDocument();
            doc.Add("");
            Assert.Equal("", doc.ToString(SaveOptions.DisableFormatting));

            doc.Add(" \t" + Environment.NewLine);
            Assert.Equal(" \t" + Environment.NewLine, doc.ToString(SaveOptions.DisableFormatting));

            AssertExtensions.Throws<ArgumentException>(null, () => doc.Add("a"));
            AssertExtensions.Throws<ArgumentException>(null, () => doc.Add("\tab"));
        }
    }
}
