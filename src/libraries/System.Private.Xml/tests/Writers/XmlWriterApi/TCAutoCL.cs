// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public class TCAutoCL
    {
        //[Variation(id=1, Desc = "Change to CL Document after WriteStartDocument()", Pri = 0)]
        [Fact]
        public void auto_1()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Auto);

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
            Assert.True(false);
        }

        //[Variation(id=2, Desc="Change to CL Document after WriteStartDocument(standalone = true)", Pri=0, Param="true")]
        //[Variation(id=3, Desc="Change to CL Document after WriteStartDocument(standalone = false)", Pri=0, Param="false")]
        [Fact]
        public void auto_2()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Auto);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Auto, "Error");
            switch (CurVariation.Param.ToString())
            {
                case "true":
                    w.WriteStartDocument(true);
                    break;
                case "false":
                    w.WriteStartDocument(true);
                    break;
            }
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
            Assert.True(false);
        }

        //[Variation(id=4, Desc="Change to CL Document when you write DocType decl", Pri=0)]
        [Fact]
        public void auto_3()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Auto);
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
            Assert.True(false);
        }

        //[Variation(id=5, Desc="Change to CL Fragment when you write a root element", Pri=1)]
        [Fact]
        public void auto_4()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Auto);
            w.WriteStartElement("root");
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
            w.WriteEndElement();
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();
            return;
        }

        //[Variation(id=6, Desc="Change to CL Fragment for WriteString at top level", Pri=1, Param="String")]
        //[Variation(id=7, Desc="Change to CL Fragment for WriteCData at top level", Pri=1, Param="CData")]
        //[Variation(id=8, Desc="Change to CL Fragment for WriteEntityRef at top level", Pri=1, Param="EntityRef")]		
        //[Variation(id=9, Desc="Change to CL Fragment for WriteCharEntity at top level", Pri=1, Param="CharEntity")]		
        //[Variation(id=10, Desc="Change to CL Fragment for WriteSurrogateCharEntity at top level", Pri=1, Param="SurrogateCharEntity")]		
        //[Variation(id=11, Desc="Change to CL Fragment for WriteChars at top level", Pri=1, Param="Chars")]				
        //[Variation(id=12, Desc="Change to CL Fragment for WriteRaw at top level", Pri=1, Param="Raw")]		
        //[Variation(id=13, Desc="Change to CL Fragment for WriteBase64 at top level", Pri=1, Param="Base64")]		
        //[Variation(id=14, Desc="Change to CL Fragment for WriteBinHex at top level", Pri=1, Param="BinHex")]		
        [Fact]
        public void auto_5()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Auto);
            byte[] buffer = new byte[10];

            switch (CurVariation.Param.ToString())
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

        //[Variation(id=15, Desc="WritePI at top level, followed by DTD, expected CL = Document", Pri=2, Param="PI")]		
        //[Variation(id=16, Desc="WriteComment at top level, followed by DTD, expected CL = Document", Pri=2, Param="Comment")]		
        //[Variation(id=17, Desc="WriteWhitespace at top level, followed by DTD, expected CL = Document", Pri=2, Param="WS")]				
        [Fact]
        public void auto_6()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Auto);
            switch (CurVariation.Param.ToString())
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

        //[Variation(id=18, Desc="WritePI at top level, followed by text, expected CL = Fragment", Pri=2, Param="PI")]		
        //[Variation(id=19, Desc="WriteComment at top level, followed by text, expected CL = Fragment", Pri=2, Param="Comment")]		
        //[Variation(id=20, Desc="WriteWhitespace at top level, followed by text, expected CL = Fragment", Pri=2, Param="WS")]				
        [Fact]
        public void auto_7()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Auto);
            w.WriteProcessingInstruction("pi", "text");
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Auto, "Error");
            w.WriteString("text");
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
            w.Dispose();
            return;
        }

        //[Variation(id=21, Desc="WriteNode(XmlReader) when reader positioned on DocType node, expected CL = Document", Pri=2)]
        [Fact]
        public void auto_8()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Auto);

            string strxml = "<!DOCTYPE test [<!ENTITY e 'abc'>]><Root />";
            XmlReaderSettings rSettings = new XmlReaderSettings();
            rSettings.CloseInput = true;
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

        //[Variation(id=22, Desc="WriteNode(XmlReader) when reader positioned on text node, expected CL = Fragment", Pri=2)]
        [Fact]
        public void auto_10()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Auto);

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
