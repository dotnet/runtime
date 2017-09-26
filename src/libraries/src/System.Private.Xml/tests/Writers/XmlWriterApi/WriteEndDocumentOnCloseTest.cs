// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using Xunit;

namespace System.Xml.Tests
{
    public partial class TCWriteEndDocumentOnCloseTest
    {
        [Theory]
        [InlineData(false, "<root>text")]
        [InlineData(true, "<root>text</root>")]
        public void TestWriteEndDocumentOnCoseForOneElementwithText(bool writeEndDocument, string expected)
        {
            StringWriter output = new StringWriter();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            ws.WriteEndDocumentOnClose = writeEndDocument;
            XmlWriter writer = XmlWriter.Create(output, ws);
            writer.WriteStartElement("root");
            writer.WriteString("text");
            writer.Dispose();
            string act = output.ToString();
            CError.Compare(act, expected, "FAILED: when one element with text and WriteEndDocumentOnClose = " + ws.WriteEndDocumentOnClose + ", expected: " + expected + ", received: " + act);
        }

        [Theory]
        [InlineData(false, "<root>")]
        [InlineData(true, "<root />")]
        public void TestWriteEndDocumentOnCoseForOneElement(bool writeEndDocument, string expected)
        {
            StringWriter output = new StringWriter();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            ws.WriteEndDocumentOnClose = writeEndDocument;
            XmlWriter writer = XmlWriter.Create(output, ws);
            writer.WriteStartElement("root");
            writer.Dispose();

            string act = output.ToString();
            CError.Compare(act, expected, "FAILED: when one start element and WriteEndDocumentOnClose = " + ws.WriteEndDocumentOnClose + ", expected: " + expected + ", received: " + act);
        }

        [Theory]
        [InlineData(false, "<root att=\"\">")]
        [InlineData(true, "<root att=\"\" />")]
        public void TestWriteEndDocumentOnCoseForOneElementwithAttribute(bool writeEndDocument, string expected)
        {
            StringWriter output = new StringWriter();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            ws.WriteEndDocumentOnClose = writeEndDocument;
            XmlWriter writer = XmlWriter.Create(output, ws);
            writer.WriteStartElement("root");
            writer.WriteStartAttribute("att");
            writer.Dispose();

            string act = output.ToString();
            CError.Compare(act, expected, "FAILED: when one start element with attribute and WriteEndDocumentOnClose = " + ws.WriteEndDocumentOnClose + ", expected: " + expected + ", received: " + act);
        }

        [InlineData(false, "<root att=\"value\">")]
        [InlineData(true, "<root att=\"value\" />")]
        [Theory]
        public void TestWriteEndDocumentOnCoseForOneElementwithAttributeValue(bool writeEndDocument, string expected)
        {
            StringWriter output = new StringWriter();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            ws.WriteEndDocumentOnClose = writeEndDocument;
            XmlWriter writer = XmlWriter.Create(output, ws);
            writer.WriteStartElement("root");
            writer.WriteAttributeString("att", "value");
            writer.Dispose();

            string act = output.ToString();
            CError.Compare(act, expected, "FAILED: when one start element with attribute value and WriteEndDocumentOnClose = " + ws.WriteEndDocumentOnClose + ", expected: " + expected + ", received: " + act);
        }

        [Theory]
        [InlineData(false, "<root xmlns=\"testns\">")]
        [InlineData(true, "<root xmlns=\"testns\" />")]
        public void TestWriteEndDocumentOnCoseForOneElementwithNamespace(bool writeEndDocument, string expected)
        {
            StringWriter output = new StringWriter();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            ws.WriteEndDocumentOnClose = writeEndDocument;
            XmlWriter writer = XmlWriter.Create(output, ws);
            writer.WriteStartElement("root", "testns");
            writer.Dispose();

            string act = output.ToString();
            CError.Compare(act, expected, "FAILED: when one element with namespace and WriteEndDocumentOnClose = " + ws.WriteEndDocumentOnClose + ", expected: " + expected + ", received: " + act);
        }

        [Theory]
        [InlineData(false, "<root><sub1><sub2>")]
        [InlineData(true, "<root><sub1><sub2 /></sub1></root>")]
        public void TestWriteEndDocumentOnCoseForMultiElements(bool writeEndDocument, string expected)
        {
            StringWriter output = new StringWriter();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            ws.WriteEndDocumentOnClose = writeEndDocument;
            XmlWriter writer = XmlWriter.Create(output, ws);
            writer.WriteStartElement("root");
            writer.WriteStartElement("sub1");
            writer.WriteStartElement("sub2");
            writer.Dispose();

            string act = output.ToString();
            CError.Compare(act, expected, "FAILED: when multi element and WriteEndDocumentOnClose = " + ws.WriteEndDocumentOnClose + ", expected: " + expected + ", received: " + act);
        }

        [Theory]
        [InlineData(false, "<root><sub1 />")]
        [InlineData(true, "<root><sub1 /></root>")]
        public void TestWriteEndDocumentOnCoseForElementsWithOneEndElement(bool writeEndDocument, string expected)
        {
            StringWriter output = new StringWriter();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            ws.WriteEndDocumentOnClose = writeEndDocument;
            XmlWriter writer = XmlWriter.Create(output, ws);
            writer.WriteStartElement("root");
            writer.WriteStartElement("sub1");
            writer.WriteEndElement();
            writer.Dispose();

            string act = output.ToString();
            CError.Compare(act, expected, "FAILED: when two element with one end element and WriteEndDocumentOnClose = " + ws.WriteEndDocumentOnClose + ", expected: " + expected + ", received: " + act);
        }

        [Theory]
        [InlineData(false, "<root />")]
        [InlineData(true, "<root />")]
        public void TestWriteEndDocumentOnCoseForOneElementWithEndElement(bool writeEndDocument, string expected)
        {
            StringWriter output = new StringWriter();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            ws.WriteEndDocumentOnClose = writeEndDocument;
            XmlWriter writer = XmlWriter.Create(output, ws);
            writer.WriteStartElement("root");
            writer.WriteEndElement();
            writer.Dispose();

            string act = output.ToString();
            CError.Compare(act, expected, "FAILED: when one element with end element and WriteEndDocumentOnClose = " + ws.WriteEndDocumentOnClose + ", expected: " + expected + ", received: " + act);
        }
    }
}
