// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;

namespace System.Xml.Tests
{
    //[TestCase(Name = "XmlWriterSettings: WriteEndDocumentOnClose")]
    public partial class TCWriteEndDocumentOnCloseTest : XmlWriterTestCaseBase
    {
        //[Variation(Desc = "write element with text but without end element when the WriteEndDocumentOnClose = false", Pri = 1, Params = new object[] { false, "<root>text" })]
        //[Variation(Desc = "write element with text but without end element when the WriteEndDocumentOnClose = true", Pri = 1, Params = new object[] { true, "<root>text</root>" })]
        public int TestWriteEndDocumentOnCoseForOneElementwithText()
        {
            bool writeEndDocument = (bool)CurVariation.Params[0];
            string expected = (string)CurVariation.Params[1];
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

            return TEST_PASS;
        }


        //[Variation(Desc = "write start element and then close when the WriteEndDocumentOnClose = false", Pri = 1, Params = new object[] { false, "<root>" })]
        //[Variation(Desc = "write start element and then close when the WriteEndDocumentOnClose = true", Pri = 1, Params = new object[] { true, "<root />" })]
        public int TestWriteEndDocumentOnCoseForOneElement()
        {
            bool writeEndDocument = (bool)CurVariation.Params[0];
            string expected = (string)CurVariation.Params[1];
            StringWriter output = new StringWriter();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            ws.WriteEndDocumentOnClose = writeEndDocument;
            XmlWriter writer = XmlWriter.Create(output, ws);
            writer.WriteStartElement("root");
            writer.Dispose();

            string act = output.ToString();
            CError.Compare(act, expected, "FAILED: when one start element and WriteEndDocumentOnClose = " + ws.WriteEndDocumentOnClose + ", expected: " + expected + ", received: " + act);

            return TEST_PASS;
        }

        //[Variation(Desc = "write start element and with attribute then close when the WriteEndDocumentOnClose = false", Pri = 1, Params = new object[] { false, "<root att=\"\">" })]
        //[Variation(Desc = "write start element and with attribute then close when the WriteEndDocumentOnClose = true", Pri = 1, Params = new object[] { true, "<root att=\"\" />" })]
        public int TestWriteEndDocumentOnCoseForOneElementwithAttribute()
        {
            bool writeEndDocument = (bool)CurVariation.Params[0];
            string expected = (string)CurVariation.Params[1];
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

            return TEST_PASS;
        }

        //[Variation(Desc = "write start element and with attribute value then close when the WriteEndDocumentOnClose = false", Pri = 1, Params = new object[] { false, "<root att=\"value\">" })]
        //[Variation(Desc = "write start element and with attribute value then close when the WriteEndDocumentOnClose = true", Pri = 1, Params = new object[] { true, "<root att=\"value\" />" })]
        public int TestWriteEndDocumentOnCoseForOneElementwithAttributeValue()
        {
            bool writeEndDocument = (bool)CurVariation.Params[0];
            string expected = (string)CurVariation.Params[1];
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

            return TEST_PASS;
        }
        //[Variation(Desc = "write start element and with namespace then close when the WriteEndDocumentOnClose = false", Pri = 1, Params = new object[] { false, "<root xmlns=\"testns\">" })]
        //[Variation(Desc = "write start element and with namespace then close when the WriteEndDocumentOnClose = true", Pri = 1, Params = new object[] { true, "<root xmlns=\"testns\" />" })]
        public int TestWriteEndDocumentOnCoseForOneElementwithNamespace()
        {
            bool writeEndDocument = (bool)CurVariation.Params[0];
            string expected = (string)CurVariation.Params[1];
            StringWriter output = new StringWriter();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            ws.WriteEndDocumentOnClose = writeEndDocument;
            XmlWriter writer = XmlWriter.Create(output, ws);
            writer.WriteStartElement("root", "testns");
            writer.Dispose();

            string act = output.ToString();
            CError.Compare(act, expected, "FAILED: when one element with namespace and WriteEndDocumentOnClose = " + ws.WriteEndDocumentOnClose + ", expected: " + expected + ", received: " + act);

            return TEST_PASS;
        }


        //[Variation(Desc = "write multi start elements then close when the WriteEndDocumentOnClose = false", Pri = 1, Params = new object[] { false, "<root><sub1><sub2>" })]
        //[Variation(Desc = "write multi start elements then close when the WriteEndDocumentOnClose = true", Pri = 1, Params = new object[] { true, "<root><sub1><sub2 /></sub1></root>" })]
        public int TestWriteEndDocumentOnCoseForMultiElements()
        {
            bool writeEndDocument = (bool)CurVariation.Params[0];
            string expected = (string)CurVariation.Params[1];
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

            return TEST_PASS;
        }

        //[Variation(Desc = "Write two elements only one with end elment when the WriteEndDocumentOnClose = false", Pri = 1, Params = new object[] { false, "<root><sub1 />" })]
        //[Variation(Desc = "Write two elements only one with end elment when the WriteEndDocumentOnClose = true", Pri = 1, Params = new object[] { true, "<root><sub1 /></root>" })]
        public int TestWriteEndDocumentOnCoseForElementsWithOneEndElement()
        {
            bool writeEndDocument = (bool)CurVariation.Params[0];
            string expected = (string)CurVariation.Params[1];
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

            return TEST_PASS;
        }

        //[Variation(Desc = "Write one element with end elment when the WriteEndDocumentOnClose = false", Pri = 1, Params = new object[] { false, "<root />" })]
        //[Variation(Desc = "Write one element with end elment when the WriteEndDocumentOnClose = true", Pri = 1, Params = new object[] { true, "<root />" })]
        public int TestWriteEndDocumentOnCoseForOneElementWithEndElement()
        {
            bool writeEndDocument = (bool)CurVariation.Params[0];
            string expected = (string)CurVariation.Params[1];
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

            return TEST_PASS;
        }
    }
}
