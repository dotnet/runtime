// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace XDocumentTests.SDMSample
{
    public class SDM_LoadSave
    {
        /// <summary>
        /// Test loading a document from an XmlReader.
        /// </summary>
        [Fact]
        public void DocumentLoadFromXmlReader()
        {
            // Null reader not allowed.
            Assert.Throws<ArgumentNullException>(() => XDocument.Load((XmlReader)null));

            // Extra content at end of reader.
            StringReader stringReader = new StringReader("<x/><y/>");
            using (XmlReader xmlReader = XmlReader.Create(stringReader))
            {
                Assert.Throws<XmlException>(() => XDocument.Load(xmlReader));
            }

            // Empty content.
            stringReader = new StringReader("");
            using (XmlReader xmlReader = XmlReader.Create(stringReader))
            {
                Assert.Throws<XmlException>(() => XDocument.Load(xmlReader));
            }

            // No root element.
            stringReader = new StringReader("<!-- comment -->");
            using (XmlReader xmlReader = XmlReader.Create(stringReader))
            {
                Assert.Throws<XmlException>(() => XDocument.Load(xmlReader));
            }

            // Reader mispositioned, so not at eof when done
            stringReader = new StringReader("<x></x>");
            using (XmlReader xmlReader = XmlReader.Create(stringReader))
            {
                // Position the reader on the end element.
                xmlReader.Read();
                xmlReader.Read();

                Assert.Throws<InvalidOperationException>(() => XDocument.Load(xmlReader));
            }

            // Reader mispositioned, so empty root when done
            stringReader = new StringReader("<x></x>");
            using (XmlReader xmlReader = XmlReader.Create(stringReader))
            {
                // Position the reader at eof.
                xmlReader.Read();
                xmlReader.Read();
                xmlReader.Read();

                Assert.Throws<InvalidOperationException>(() => XDocument.Load(xmlReader));
            }
        }

        /// <summary>
        /// Tests the Save overloads on document, that write to an XmlWriter.
        /// </summary>
        [Fact]
        public void DocumentSaveToXmlWriter()
        {
            XDocument ee = new XDocument();
            Assert.Throws<ArgumentNullException>(() => ee.Save((XmlWriter)null));
        }

        /// <summary>
        /// Tests WriteTo on document.
        /// </summary>
        [Fact]
        public void DocumentWriteTo()
        {
            XDocument ee = new XDocument();
            Assert.Throws<ArgumentNullException>(() => ee.WriteTo(null));
        }

        /// <summary>
        /// Test loading an element from an XmlReader.
        /// </summary>
        [Fact]
        public void ElementLoadFromXmlReader()
        {
            // Null reader not allowed.
            Assert.Throws<ArgumentNullException>(() => XElement.Load((XmlReader)null));

            // Extra stuff in xml after the element is not allowed
            using (StringReader reader = new StringReader("<abc><def/></abc>"))
            using (XmlReader xmlreader = XmlReader.Create(reader))
            {
                xmlreader.Read();
                xmlreader.Read(); // position on <def>

                Assert.Throws<InvalidOperationException>(() => XElement.Load(xmlreader));
            }
        }

        /// <summary>
        /// Tests the Save overloads on element, that write to an XmlWriter.
        /// </summary>
        [Fact]
        public void ElementSaveToXmlWriter()
        {
            XElement ee = new XElement("x");
            Assert.Throws<ArgumentNullException>(() => ee.Save((XmlWriter)null));
        }
    }
}
