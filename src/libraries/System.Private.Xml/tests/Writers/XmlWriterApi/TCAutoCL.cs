// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.XmlWriterApiTests
{
    public class TCAutoCL
    {
        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void auto_1(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Auto);

            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Auto, "Error");
            w.WriteStartDocument();

            // PROLOG
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
            w.WriteStartElement("root");
            w.WriteEndElement();

            // Try writing another root element, should error
            try
            {
                w.WriteStartElement("root");
            }
            catch (InvalidOperationException)
            {
                return;
            }
            finally
            {
                w.Dispose();
            }
            CError.WriteLine("Conformance level = Document did not take effect");
            Assert.Fail();
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom, true)]
        [XmlWriterInlineData(WriterType.AllButCustom, false)]
        public void auto_2(XmlWriterUtils utils, bool writeStartDocument)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Auto);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Auto, "Error");
            w.WriteStartDocument(writeStartDocument);

            // PROLOG
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
            w.WriteStartElement("root");
            // ELEMENT CONTENT
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
            // Inside Attribute
            w.WriteStartAttribute("attr");
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
            w.WriteEndElement();

            // Try writing another root element, should error
            try
            {
                w.WriteStartElement("root");
            }
            catch (InvalidOperationException)
            {
                return;
            }
            finally
            {
                w.Dispose();
            }
            CError.WriteLine("Conformance level = Document did not take effect");
            Assert.Fail();
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void auto_3(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Auto);
            w.WriteDocType("ROOT", "publicid", "sysid", "<!ENTITY e 'abc'>");
            // PROLOG
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Should switch to Document from Auto when you write top level DTD");
            w.WriteStartElement("Root");
            w.WriteEndElement();

            // Try writing text at root level, should error
            try
            {
                w.WriteString("text");
            }
            catch (InvalidOperationException)
            {
                return;
            }
            finally
            {
                w.Dispose();
            }
            CError.WriteLine("Conformance level = Document did not take effect");
            Assert.Fail();
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void auto_4(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Auto);
            w.WriteStartElement("root");
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
            w.WriteEndElement();
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();
            return;
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom, "String")]
        [XmlWriterInlineData(WriterType.AllButCustom, "CData")]
        [XmlWriterInlineData(WriterType.AllButCustom, "EntityRef")]
        [XmlWriterInlineData(WriterType.AllButCustom, "CharEntity")]
        [XmlWriterInlineData(WriterType.AllButCustom, "SurrogateCharEntity")]
        [XmlWriterInlineData(WriterType.AllButCustom, "Chars")]
        [XmlWriterInlineData(WriterType.AllButCustom, "Raw")]
        [XmlWriterInlineData(WriterType.AllButCustom, "Base64")]
        [XmlWriterInlineData(WriterType.AllButCustom, "BinHex")]
        public void auto_5(XmlWriterUtils utils, string tokenType)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Auto);
            byte[] buffer = new byte[10];

            switch (tokenType)
            {
                case "String":
                    w.WriteString("text");
                    break;
                case "CData":
                    w.WriteCData("cdata text");
                    break;
                case "EntityRef":
                    w.WriteEntityRef("ent");
                    break;
                case "CharEntity":
                    w.WriteCharEntity('\uD23E');
                    break;
                case "SurrogateCharEntity":
                    w.WriteSurrogateCharEntity('\uDF41', '\uD920');
                    break;
                case "Chars":
                    string s = "test";
                    char[] buf = s.ToCharArray();
                    w.WriteChars(buf, 0, 1);
                    break;
                case "Raw":
                    w.WriteRaw("<Root />");
                    break;
                case "BinHex":
                    w.WriteBinHex(buffer, 0, 1);
                    break;
                case "Base64":
                    w.WriteBase64(buffer, 0, 1);
                    break;
                default:
                    CError.Compare(false, "Invalid param in testcase");
                    break;
            }
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
            w.Dispose();
            return;
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom, "PI")]
        [XmlWriterInlineData(WriterType.AllButCustom, "Comment")]
        [XmlWriterInlineData(WriterType.AllButCustom, "WS")]
        public void auto_6(XmlWriterUtils utils, string tokenType)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Auto);
            switch (tokenType)
            {
                case "PI":
                    w.WriteProcessingInstruction("pi", "text");
                    break;
                case "Comment":
                    w.WriteComment("text");
                    break;
                case "WS":
                    w.WriteWhitespace("   ");
                    break;
                default:
                    CError.Compare(false, "Invalid param in testcase");
                    break;
            }
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Auto, "Error");
            w.WriteDocType("ROOT", "publicid", "sysid", "<!ENTITY e 'abc'>");
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
            w.Dispose();
            return;
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void auto_7(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Auto);
            w.WriteProcessingInstruction("pi", "text");
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Auto, "Error");
            w.WriteString("text");
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
            w.Dispose();
            return;
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void auto_8(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Auto);

            string strxml = "<!DOCTYPE test [<!ENTITY e 'abc'>]><Root />";
            XmlReaderSettings rSettings = new XmlReaderSettings();
            rSettings.CloseInput = true;
            rSettings.DtdProcessing = DtdProcessing.Parse;
            XmlReader xr = ReaderHelper.Create(new StringReader(strxml), rSettings, (string)null);

            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Auto, "Error");
            xr.Read();
            CError.Compare(xr.NodeType.ToString(), "DocumentType", "Error");
            w.WriteNode(xr, false);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
            w.Dispose();
            xr.Dispose();
            return;
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void auto_10(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Auto);

            string strxml = "<Root>text</Root>";
            XmlReader xr = ReaderHelper.Create(new StringReader(strxml));

            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Auto, "Error");
            xr.Read(); xr.Read();
            CError.Compare(xr.NodeType.ToString(), "Text", "Error");
            w.WriteNode(xr, false);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
            w.Dispose();
            xr.Dispose();
            return;
        }
    }
}
