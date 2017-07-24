// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Text;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    //[TestCase(Name="XmlWriterSettings: Default Values", Pri=0)]
    public partial class TCDefaultWriterSettings
    {
        //[Variation(id=1, Desc="Default value of Encoding")]
        [Fact]
        public void default_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Compare(wSettings.Encoding, Encoding.UTF8, "Incorrect default value of Encoding");

            XmlWriter w = CreateWriter();
            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                case WriterType.UTF8WriterIndent:
                case WriterType.CharCheckingWriter:
                case WriterType.WrappedWriter:
                    CError.Compare(w.Settings.Encoding.WebName, "utf-8", "Incorrect default value of Encoding");
                    break;
                case WriterType.UnicodeWriter:
                case WriterType.UnicodeWriterIndent:
                    CError.Compare(w.Settings.Encoding.WebName, "utf-16", "Incorrect default value of Encoding");
                    break;
            }
            w.Dispose();
            return;
        }

        //[Variation(id=2, Desc="Default value of OmitXmlDeclaration")]
        [Fact]
        public void default_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Compare(wSettings.OmitXmlDeclaration, false, "Incorrect default value of OmitXmlDeclaration");

            return;
        }

        //[Variation(id=3, Desc="Default value of NewLineHandling")]
        [Fact]
        public void default_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Compare(wSettings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value of NewLineHandling");

            XmlWriter w = CreateWriter();
            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                case WriterType.UnicodeWriter:
                    CError.Compare(w.Settings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value of NewLineHandling");
                    break;
            }
            w.Dispose();
            return;
        }

        //[Variation(id=4, Desc="Default value of NewLineChars")]
        [Fact]
        public void default_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.NewLineChars, Environment.NewLine, "Incorrect default value of NewLineChars");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.NewLineChars, Environment.NewLine, "Incorrect default value of NewLineChars");

            w.Dispose();
            return;
        }

        //[Variation(id=5, Desc="Default value of Indent")]
        [Fact]
        public void default_5()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.Indent, false, "Incorrect default value of wSettings.Indent");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.Indent, IsIndent() ? true : false, "Incorrect default value of w.Settings.Indent");
            w.Dispose();
            return;
        }

        //[Variation(id=6, Desc="Default value of IndentChars")]
        [Fact]
        public void default_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.IndentChars, "  ", "Incorrect default value of IndentChars");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.IndentChars, "  ", "Incorrect default value of IndentChars");

            return;
        }

        //[Variation(id=7, Desc="Default value of NewLineOnAttributes")]
        [Fact]
        public void default_7()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.NewLineOnAttributes, false, "Incorrect default value of NewLineOnAttributes");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.NewLineOnAttributes, false, "Incorrect default value of NewLineOnAttributes");

            return;
        }

        //[Variation(id=8, Desc="Default value of CloseOutput")]
        [Fact]
        public void default_8()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Compare(wSettings.CloseOutput, false, "Incorrect default value of CloseOutput");

            return;
        }

        //[Variation(id=10, Desc="Default value of CheckCharacters")]
        [Fact]
        public void default_10()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.CheckCharacters, true, "Incorrect default value of CheckCharacters");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.CheckCharacters, true, "Incorrect default value of CheckCharacters");

            w.Dispose();
            return;
        }

        //[Variation(id=11, Desc="Default value of ConformanceLevel")]
        [Fact]
        public void default_11()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.ConformanceLevel, ConformanceLevel.Document, "Incorrect default value of ConformanceLevel");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Incorrect default value of ConformanceLevel");

            w.Dispose();
            return;
        }

        //[Variation(id = 13, Desc = "Default value of WriteEndDocumentOnClose")]
        [Fact]
        public void default_13()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            CError.Equals(ws.WriteEndDocumentOnClose, true, "Incorrect default value of WriteEndDocumentOnClose");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.WriteEndDocumentOnClose, true, "Incorrect default value of WriteEndDocumentOnClose");

            w.Dispose();

            return;
        }
    }

    //[TestCase(Name="XmlWriterSettings: Reset/Clone")]
    public partial class TCWriterSettingsMisc
    {
        //[Variation(id=1, Desc="Test for Reset()", Pri=0)]
        [Fact]
        public void Reset_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Encoding = Encoding.UTF8;
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.None;
            wSettings.NewLineChars = "\n";
            wSettings.IndentChars = "\t\t";
            wSettings.NewLineOnAttributes = true;
            wSettings.CloseOutput = true;
            wSettings.CheckCharacters = false;
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.WriteEndDocumentOnClose = false;
            wSettings.Reset();

            CError.Equals(wSettings.Encoding, Encoding.UTF8, "Encoding");
            CError.Equals(wSettings.OmitXmlDeclaration, false, "OmitXmlDeclaration");
            CError.Equals(wSettings.NewLineHandling, NewLineHandling.Replace, "NewLineHandling");
            CError.Equals(wSettings.NewLineChars, Environment.NewLine, "NewLineChars");
            CError.Equals(wSettings.Indent, false, "Indent");
            CError.Equals(wSettings.IndentChars, "  ", "IndentChars");
            CError.Equals(wSettings.NewLineOnAttributes, false, "NewLineOnAttributes");
            CError.Equals(wSettings.CloseOutput, false, "CloseOutput");
            CError.Equals(wSettings.CheckCharacters, true, "CheckCharacters");
            CError.Equals(wSettings.ConformanceLevel, ConformanceLevel.Document, "ConformanceLevel");
            CError.Equals(wSettings.WriteEndDocumentOnClose, true, "WriteEndDocumentOnClose");

            return;
        }

        //[Variation(id=2, Desc="Test for Clone()", Pri=0)]
        [Fact]
        public void Clone_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Encoding = Encoding.UTF8;
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;
            wSettings.NewLineChars = "\n";
            wSettings.IndentChars = "                ";
            wSettings.NewLineOnAttributes = true;
            wSettings.CloseOutput = true;
            wSettings.CheckCharacters = false;
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.WriteEndDocumentOnClose = false;

            XmlWriterSettings newSettings = wSettings.Clone();

            CError.Equals(wSettings.Encoding, newSettings.Encoding, "Encoding");
            CError.Equals(wSettings.OmitXmlDeclaration, newSettings.OmitXmlDeclaration, "OmitXmlDeclaration");
            CError.Equals(wSettings.NewLineHandling, newSettings.NewLineHandling, "NewLineHandling");
            CError.Equals(wSettings.NewLineChars, newSettings.NewLineChars, "NewLineChars");
            CError.Equals(wSettings.Indent, newSettings.Indent, "Indent");
            CError.Equals(wSettings.IndentChars, newSettings.IndentChars, "IndentChars");
            CError.Equals(wSettings.NewLineOnAttributes, newSettings.NewLineOnAttributes, "NewLineOnAttributes");
            CError.Equals(wSettings.CloseOutput, newSettings.CloseOutput, "CloseOutput");
            CError.Equals(wSettings.CheckCharacters, newSettings.CheckCharacters, "CheckCharacters");
            CError.Equals(wSettings.ConformanceLevel, newSettings.ConformanceLevel, "ConformanceLevel");
            CError.Equals(wSettings.WriteEndDocumentOnClose, newSettings.WriteEndDocumentOnClose, "WriteEndDocumentOnClose");

            return;
        }
    }

    //[TestCase(Name="XmlWriterSettings: OmitXmlDeclaration")]
    public partial class TCOmitXmlDecl
    {
        //[Variation(id=1, Desc="Check when false", Pri=1)]
        [Fact]
        public void XmlDecl_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Mismatch in CL");
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            XmlReader xr = GetReader();
            // First node should be XmlDeclaration
            xr.Read();
            if (xr.NodeType != XmlNodeType.XmlDeclaration)
            {
                CError.WriteLine("Did not write XmlDecl when OmitXmlDecl was FALSE. NodeType = {0}", xr.NodeType.ToString());
                xr.Dispose();
                Assert.True(false);
            }
            else if (xr.NodeType == XmlNodeType.XmlDeclaration)
            {
                xr.Dispose();
                return;
            }
            else
            {
                xr.Dispose();
                Assert.True(false);
            }
        }

        //[Variation(id=2, Desc="Check when true", Pri=1)]
        [Fact]
        public void XmlDecl_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Mismatch in CL");
            CError.Compare(w.Settings.OmitXmlDeclaration, true, "Mismatch in OmitXmlDecl");
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            XmlReader xr = GetReader();
            // Should not read XmlDeclaration
            while (xr.Read())
            {
                if (xr.NodeType == XmlNodeType.XmlDeclaration)
                {
                    CError.WriteLine("Wrote XmlDecl when OmitXmlDecl was TRUE");
                    xr.Dispose();
                    Assert.True(false);
                }
            }
            xr.Dispose();
            return;
        }

        //[Variation(id=3, Desc="Set to true, write standalone attribute. Should not write XmlDecl", Pri=1)]
        [Fact]
        public void XmlDecl_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartDocument(true);
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.WriteEndDocument();
            w.Dispose();


            XmlReader xr = GetReader();
            // Should not read XmlDeclaration
            while (xr.Read())
            {
                if (xr.NodeType == XmlNodeType.XmlDeclaration)
                {
                    CError.WriteLine("Wrote XmlDecl when OmitXmlDecl was TRUE");
                    xr.Dispose();
                    Assert.True(false);
                }
            }
            xr.Dispose();
            return;
        }

        //[Variation(id=4, Desc="Set to false, write document fragment. Should not write XmlDecl", Pri=1)]
        [Fact]
        public void XmlDecl_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Fragment;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Mismatch in CL");
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();


            Assert.True(CompareReader("<root /><root />"));
        }

        //[Variation(id=5, Desc="WritePI with name = 'xml' text = 'version = 1.0' should work if WriteStartDocument is not called", Pri=1)]
        [Fact]
        public void XmlDecl_5()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = false;


            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.CloseOutput, false, "Mismatch in CloseOutput");
            w.WriteProcessingInstruction("xml", "version = \"1.0\"");
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.Dispose();


            Assert.True(CompareReader("<?xml version = \"1.0\"?><Root />"));
        }

        //[Variation(id=6, Desc="WriteNode(reader) positioned on XmlDecl, OmitXmlDecl = true", Pri=1)]
        [Fact]
        public void XmlDecl_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = false;
            wSettings.OmitXmlDeclaration = true;
            wSettings.ConformanceLevel = ConformanceLevel.Document;

            string strxml = "<?xml version=\"1.0\"?><root>blah</root>";
            XmlReader xr = ReaderHelper.Create(new StringReader(strxml));
            xr.Read();

            XmlWriter w = CreateWriter(wSettings);
            w.WriteNode(xr, false);
            w.WriteStartElement("root");
            w.WriteEndElement();
            xr.Dispose();
            w.Dispose();


            Assert.True(CompareReader("<root />"));
        }

        //[Variation(id=7, Desc="WriteNode(reader) positioned on XmlDecl, OmitXmlDecl = false", Pri=1)]
        [Fact]
        public void XmlDecl_7()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = false;
            wSettings.OmitXmlDeclaration = false;
            wSettings.ConformanceLevel = ConformanceLevel.Document;

            string strxml = "<?xml version=\"1.0\"?><root>blah</root>";
            XmlReader xr = ReaderHelper.Create(new StringReader(strxml));
            xr.Read();

            XmlWriter w = CreateWriter(wSettings);
            w.WriteNode(xr, false);
            w.WriteStartElement("root");
            w.WriteString("blah");
            w.WriteEndElement();
            xr.Dispose();
            w.Dispose();

            Assert.True(CompareReader(strxml));
        }
    }

    //[TestCase(Name = "XmlWriterSettings: CheckCharacters")]
    public partial class TCCheckChars
    {
        //[Variation(id=1, Desc = "CheckChars=true, invalid XML test WriteString", Pri = 1, Param ="String")]
        //[Variation(id=2, Desc = "CheckChars=true, invalid XML test WriteCData", Pri = 1, Param = "CData")]
        //[Variation(id=3, Desc = "CheckChars=true, invalid XML test WriteComment", Pri = 1, Param = "Comment")]
        //[Variation(id=4, Desc = "CheckChars=true, invalid XML test WriteCharEntity", Pri = 1, Param = "CharEntity")]
        //[Variation(id=5, Desc = "CheckChars=true, invalid XML test WriteEntityRef", Pri = 1, Param = "EntityRef")]
        //[Variation(id=6, Desc = "CheckChars=true, invalid XML test WriteSurrogateCharEntity", Pri = 1, Param = "SurrogateCharEntity")]
        //[Variation(id=7, Desc = "CheckChars=true, invalid XML test WritePI", Pri = 1, Param = "PI")]
        //[Variation(id=8, Desc = "CheckChars=true, invalid XML test WriteWhitespace", Pri = 1, Param = "Whitespace")]
        //[Variation(id=9, Desc = "CheckChars=true, invalid XML test WriteChars", Pri = 1, Param = "Chars")]
        //[Variation(id=10, Desc = "CheckChars=true, invalid XML test WriteRaw(String)", Pri = 1, Param = "RawString")]
        //[Variation(id=11, Desc = "CheckChars=true, invalid XML test WriteRaw(Chars)", Pri = 1, Param = "RawChars")]
        //[Variation(id=12, Desc = "CheckChars=true, invalid XML test WriteValue(string)", Pri = 1, Param = "WriteValue")]
        //[Variation(id=13, Desc = "CheckChars=true, invalid name chars WriteDocType(name)", Pri = 1, Param = "WriteDocTypeName")]
        //[Variation(id=14, Desc = "CheckChars=true, invalid name chars WriteDocType(sysid)", Pri = 1, Param = "WriteDocTypeSysid")]
        //[Variation(id=15, Desc = "CheckChars=true, invalid name chars WriteDocType(pubid)", Pri = 1, Param = "WriteDocTypePubid")]
        [Fact]
        public void checkChars_1()
        {
            char[] invalidXML = { '\u0000', '\u0008', '\u000B', '\u000C', '\u000E', '\u001F', '\uFFFE', '\uFFFF' };

            XmlWriter w = CreateWriter(ConformanceLevel.Auto);

            try
            {
                switch (CurVariation.Param.ToString())
                {
                    case "WriteDocTypeName":
                        w.WriteDocType(":badname", "sysid", "pubid", "subset");
                        break;

                    case "WriteDocTypeSysid":
                        w.WriteDocType("name", invalidXML[1].ToString(), "pubid", "subset");
                        break;

                    case "WriteDocTypePubid":
                        w.WriteDocType("name", "sysid", invalidXML[1].ToString(), "subset");
                        break;

                    case "String":
                        w.WriteString(invalidXML[0].ToString());
                        break;

                    case "CData":
                        w.WriteCData(invalidXML[1].ToString());
                        break;

                    case "Comment":
                        w.WriteComment(invalidXML[2].ToString());
                        break;

                    case "CharEntity":
                        w.WriteCharEntity(invalidXML[3]);
                        break;

                    case "EntityRef":
                        w.WriteEntityRef(invalidXML[4].ToString());
                        break;

                    case "SurrogateCharEntity":
                        w.WriteSurrogateCharEntity(invalidXML[5], invalidXML[1]);
                        break;

                    case "PI":
                        w.WriteProcessingInstruction("pi", invalidXML[6].ToString());
                        break;

                    case "Whitespace":
                        w.WriteWhitespace(invalidXML[7].ToString());
                        break;

                    case "Chars":
                        w.WriteChars(invalidXML, 1, 5);
                        break;

                    case "RawString":
                        w.WriteRaw(invalidXML[4].ToString());
                        break;

                    case "RawChars":
                        w.WriteRaw(invalidXML, 6, 2);
                        break;

                    case "WriteValue":
                        w.WriteValue(invalidXML[3].ToString());
                        break;

                    default:
                        CError.Compare(false, "Invalid param value");
                        break;
                }
            }
            catch (XmlException e1)
            {
                CError.WriteLineIgnore("Exception: " + e1.ToString());
                CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                return;
            }
            catch (ArgumentException e2)
            {
                CError.WriteLineIgnore("Exception: " + e2.ToString());
                CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                return;
            }
            finally
            {
                w.Dispose();
            }
            Assert.True(false);
        }

        //[Variation(id=20, Desc = "CheckChars=false, invalid XML characters should be escaped, WriteString", Pri = 1, Param = "String")]
        //[Variation(id=21, Desc = "CheckChars=false, invalid XML characters should be escaped, WriteCharEntity", Pri = 1, Param = "CharEntity")]
        //[Variation(id=22, Desc = "CheckChars=false, invalid XML characters should be escaped, WriteChars", Pri = 1, Param = "Chars")]
        //[Variation(id=23, Desc = "CheckChars=false, invalid XML characters should be escaped, WriteValue(string)", Pri = 1, Param = "WriteValue")]
        [Fact]
        public void checkChars_2()
        {
            char[] invalidXML = { '\u0000', '\u0008', '\u000B', '\u000C', '\u000E', '\u001F', '\uFFFE', '\uFFFF' };

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = false;
            wSettings.CloseOutput = false;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.CheckCharacters, false, "Mismatch in CheckCharacters");
            try
            {
                w.WriteStartElement("Root");
                switch (CurVariation.Param.ToString())
                {
                    case "String":
                        w.WriteString(invalidXML[0].ToString());
                        w.WriteEndElement();
                        w.Dispose();
                        Assert.True(CompareReader("<Root>&#x0;</Root>"));

                    case "CharEntity":
                        w.WriteCharEntity(invalidXML[3]);
                        w.WriteEndElement();
                        w.Dispose();
                        Assert.True(CompareReader("<Root>&#xC;</Root>"));

                    case "Chars":
                        w.WriteChars(invalidXML, 1, 5);
                        w.WriteEndElement();
                        w.Dispose();
                        Assert.True(CompareReader("<Root>&#x8;&#xB;&#xC;&#xE;&#x1F;</Root>"));

                    case "WriteValue":
                        w.WriteValue(invalidXML[3].ToString());
                        w.WriteEndElement();
                        w.Dispose();
                        Assert.True(CompareReader("<Root>&#xC;</Root>"));

                    default:
                        CError.Compare(false, "Invalid param value");
                        break;
                }
            }
            catch (ArgumentException e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                Assert.True(false);
            }
            finally
            {
                w.Dispose();
            }
            Assert.True(false);
        }

        //[Variation(id=30, Desc = "CheckChars=false, invalid XML characters in WriteComment are not escaped", Pri = 1, Param = "Comment")]
        //[Variation(id=32, Desc = "CheckChars=false, invalid XML characters in WritePI are not escaped", Pri = 1, Param = "PI")]
        //[Variation(id=33, Desc = "CheckChars=false, invalid XML characters in WriteCData are not escaped", Pri = 1, Param = "CData")]
        //[Variation(id=34, Desc = "CheckChars=false, invalid XML characters in WriteRaw(String) are not escaped", Pri = 1, Param = "RawString")]
        //[Variation(id=35, Desc = "CheckChars=false, invalid XML characters in WriteRaw(Chars) are not escaped", Pri = 1, Param = "RawChars")]
        [Fact]
        public void checkChars_3()
        {
            char[] invalidXML = { '\u0000', '\u0008', '\u000B', '\u000C', '\u000E', '\u001F', '\uFFFE', '\uFFFF' };

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = false;
            wSettings.CloseOutput = true;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.CheckCharacters, false, "Mismatch in CheckCharacters");
            w.WriteStartElement("Root");
            switch (CurVariation.Param.ToString())
            {
                case "Comment":
                    w.WriteComment(invalidXML[2].ToString());
                    w.WriteEndElement();
                    w.Dispose();
                    string exp = IsIndent() ? "<Root>" + Environment.NewLine + "  <!--\u000B-->" + Environment.NewLine + "</Root>" : "<Root><!--\u000B--></Root>";
                    Assert.True(CompareString(exp));

                case "PI":
                    w.WriteProcessingInstruction("pi", invalidXML[6].ToString());
                    w.WriteEndElement();
                    w.Dispose();
                    exp = IsIndent() ? "<Root>" + Environment.NewLine + "  <?pi \uFFFE?>" + Environment.NewLine + "</Root>" : "<Root><?pi \uFFFE?></Root>";
                    Assert.True(CompareString(exp));

                case "RawString":
                    w.WriteRaw(invalidXML[4].ToString());
                    w.WriteEndElement();
                    w.Dispose();
                    Assert.True(CompareString("<Root>\u000E</Root>"));

                case "RawChars":
                    w.WriteRaw(invalidXML, 6, 2);
                    w.WriteEndElement();
                    w.Dispose();
                    Assert.True(CompareString("<Root>\uFFFE\uFFFF</Root>"));

                case "CData":
                    w.WriteCData(invalidXML[1].ToString());
                    w.WriteEndElement();
                    w.Dispose();
                    Assert.True(CompareString("<Root><![CDATA[\u0008]]></Root>"));

                default:
                    CError.Compare(false, "Invalid param value");
                    Assert.True(false);
            }
        }

        //[Variation(id=40, Desc = "CheckChars=false, invalid XML characters in WriteWhitespace should error", Pri = 1, Params=new object[]{"Whitespace", false})]
        //[Variation(id=41, Desc = "CheckChars=false, invalid XML characters in WriteSurrogateCharEntity should error", Pri = 1, Params=new object[]{"Surrogate", false})]
        //[Variation(id=42, Desc = "CheckChars=false, invalid XML characters in WriteEntityRef should error", Pri = 1, Params=new object[]{"EntityRef", false})]
        //[Variation(id=43, Desc = "CheckChars=false, invalid XML characters in WriteName should error", Pri = 1, Params=new object[]{"Name", false})]
        //[Variation(id=44, Desc = "CheckChars=false, invalid XML characters in WriteNmToken should error", Pri = 1, Params=new object[]{"NmToken", false})]
        //[Variation(id=45, Desc = "CheckChars=false, invalid XML characters in WriteQualifiedName should error", Pri = 1, Params=new object[]{"QName", false})]

        //[Variation(id=46, Desc = "CheckChars=true, invalid XML characters in WriteWhitespace should error", Pri = 1, Params=new object[]{"Whitespace", true})]
        //[Variation(id=47, Desc = "CheckChars=true, invalid XML characters in WriteSurrogateCharEntity should error", Pri = 1, Params=new object[]{"Surrogate", true})]
        //[Variation(id=48, Desc = "CheckChars=true, invalid XML characters in WriteEntityRef should error", Pri = 1, Params=new object[]{"EntityRef", true})]
        //[Variation(id=49, Desc = "CheckChars=true, invalid XML characters in WriteName should error", Pri = 1, Params=new object[]{"Name", true})]
        //[Variation(id=50, Desc = "CheckChars=true, invalid XML characters in WriteNmToken should error", Pri = 1, Params=new object[]{"NmToken", true})]
        //[Variation(id=51, Desc = "CheckChars=true, invalid XML characters in WriteQualifiedName should error", Pri = 1, Params=new object[]{"QName", true})]		
        [Fact]
        public void checkChars_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = Boolean.Parse(CurVariation.Params[1].ToString());
            wSettings.CloseOutput = true;

            char[] invalidXML = { '\u0000', '\u0008', '\u000B', '\u000C', '\u000E', '\u001F', '\uFFFE', '\uFFFF' };
            XmlWriter w = CreateWriter(wSettings);
            try
            {
                w.WriteStartElement("Root");
                switch (CurVariation.Params[0].ToString())
                {
                    case "Whitespace":
                        w.WriteWhitespace(invalidXML[7].ToString());
                        break;
                    case "Surrogate":
                        w.WriteSurrogateCharEntity(invalidXML[7], invalidXML[0]);
                        break;
                    case "EntityRef":
                        w.WriteEntityRef(invalidXML[4].ToString());
                        break;
                    case "Name":
                        w.WriteName(invalidXML[6].ToString());
                        break;
                    case "NmToken":
                        w.WriteNmToken(invalidXML[5].ToString());
                        break;
                    case "QName":
                        w.WriteQualifiedName(invalidXML[3].ToString(), "");
                        break;
                    default:
                        CError.Compare(false, "Invalid param value");
                        break;
                }
            }
            catch (ArgumentException e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                //By design
                if ((WriterType == WriterType.UTF8Writer || WriterType == WriterType.UnicodeWriter || WriterType == WriterType.WrappedWriter || IsIndent()) &&
                    (CurVariation.Params[0].ToString() == "Name" || CurVariation.Params[0].ToString() == "NmToken"))
                {
                    CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                }
                else
                {
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                }
                return;
            }
            finally
            {
                w.Dispose();
            }
            Assert.True(false);
        }


        //[Variation(id=60, Desc = "CheckChars=true, write char in SHIFT-JIS codepage 932, WriteString", Pri = 1, Param = "String")]
        //[Variation(id=61, Desc = "CheckChars=true, write char in SHIFT-JIS codepage 932, WriteCharEntity", Pri = 1, Param = "CharEntity")]
        //[Variation(id=62, Desc = "CheckChars=true, write char in SHIFT-JIS codepage 932, WriteChars", Pri = 1, Param = "Chars")]
        //[Variation(id=63, Desc = "CheckChars=true, write char in SHIFT-JIS codepage 932, WriteValue(string)", Pri = 1, Param = "WriteValue")]
        [Fact]
        public void checkChars_5()
        {
            char[] shiftJIS = { '\uFF80', '\uFF61', '\uFF9F', '\uFF77' };

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = true;
            wSettings.CloseOutput = true;

            XmlWriter w = WriterHelper.Create("writer.out", wSettings);

            try
            {
                w.WriteStartElement("Root");
                switch (CurVariation.Param.ToString())
                {
                    case "String":
                        w.WriteString(shiftJIS[0].ToString());
                        w.WriteEndElement();

                        w.Dispose();
                        Assert.True(CompareReader("<Root>&#xFF80;</Root>"));

                    case "CharEntity":
                        w.WriteCharEntity(shiftJIS[2]);
                        w.WriteEndElement();

                        w.Dispose();
                        Assert.True(CompareReader("<Root>&#xFF9F;</Root>"));

                    case "Chars":
                        w.WriteChars(shiftJIS, 0, 2);
                        w.WriteEndElement();

                        w.Dispose();
                        Assert.True(CompareReader("<Root>&#xFF80;&#xFF61;</Root>"));

                    case "WriteValue":
                        w.WriteValue(shiftJIS[3].ToString());
                        w.WriteEndElement();

                        w.Dispose();
                        Assert.True(CompareReader("<Root>&#xFF77;</Root>"));

                    default:
                        CError.Compare(false, "Invalid param value");
                        break;
                }
            }
            catch (Exception e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                Assert.True(false);
            }
            finally
            {
                w.Dispose();
            }
            Assert.True(false);
        }

        //[Variation(id = 70, Desc = "CheckChars=true, write char in SHIFT-JIS codepage 932, WriteCData", Pri = 1, Param = "CData")]
        //[Variation(id = 71, Desc = "CheckChars=true, write char in SHIFT-JIS codepage 932, WriteComment", Pri = 1, Param = "Comment")]
        //[Variation(id = 72, Desc = "CheckChars=true, write char in SHIFT-JIS codepage 932, WritePI", Pri = 1, Param = "PI")]
        //[Variation(id = 73, Desc = "CheckChars=true, write char in SHIFT-JIS codepage 932, WriteRaw(String)", Pri = 1, Param = "RawString")]
        //[Variation(id = 74, Desc = "CheckChars=true, write char in SHIFT-JIS codepage 932, WriteRaw(Chars)", Pri = 1, Param = "RawChars")]
        [Fact]
        public void checkChars_6()
        {
            char[] shiftJIS = { '\uFF80', '\uFF61', '\uFF9F', '\uFF77' };

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = true;
            wSettings.CloseOutput = true;
            XmlWriter w = WriterHelper.Create("writer.out", wSettings);

            try
            {
                w.WriteStartElement("Root");
                switch (CurVariation.Param.ToString())
                {
                    case "CData":
                        w.WriteCData(shiftJIS[1].ToString());
                        w.WriteEndElement();
                        break;

                    case "Comment":
                        w.WriteComment(shiftJIS[1].ToString());
                        w.WriteEndElement();
                        break;

                    case "PI":
                        w.WriteProcessingInstruction("pi", shiftJIS[3].ToString());
                        w.WriteEndElement();
                        break;

                    case "RawString":
                        w.WriteRaw(shiftJIS[3].ToString());
                        w.WriteEndElement();
                        break;

                    case "RawChars":
                        w.WriteRaw(shiftJIS, 1, 2);
                        w.WriteEndElement();
                        break;

                    default:
                        CError.Compare(false, "Invalid param value");
                        break;
                }

                w.Flush();
            }
            catch (EncoderFallbackException)
            {
                return;
            }
            finally
            {
                w.Dispose();
            }

            CError.WriteLine("Did not throw!");
            Assert.True(false);
        }


        //[Variation(id = 75, Desc = "XmlWriter creates empty XML file when writing unencodeable characters (ByDesign)", Pri = 1)]
        [Fact]
        public void checkChars_6b()
        {
            char[] shiftJIS = { '\uFF80', '\uFF61', '\uFF9F', '\uFF77' };

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = true;
            wSettings.CloseOutput = true;

            using (XmlWriter w = WriterHelper.Create("writer.out", wSettings))
            {
                w.WriteStartElement("Root");
                w.WriteCData(shiftJIS[1].ToString());
                w.WriteEndElement();
                try
                {
                    w.Dispose();
                }
                catch (EncoderFallbackException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    return;
                }
            }

            CError.WriteLine("Did not throw exception!");
            Assert.True(false);
        }


        /*=============================================================================
        The writer contructor will throw XmlException when CheckCharacters=true and
            - IndentChars or NewLineChars contains non-whitespace character when NewLineOnAttributes=true 
        or
            - IndentChars or NewLineChars contains <, &, ]]> or an invalid surrogate character when NewLineOnAttributes=false
        ===============================================================================*/

        //[Variation(id=80, Desc = "CheckChars = true and IndentChars contains <", Pri = 1, Param="<")]
        //[Variation(id=81, Desc = "CheckChars = true and IndentChars contains &", Pri = 1, Param="&")]
        //[Variation(id=82, Desc = "CheckChars = true and IndentChars contains ]]>", Pri = 1, Param="]]>")]
        //[Variation(id=83, Desc = "CheckChars = true and IndentChars contains invalid surrogate char", Pri = 1, Param="\uDD12\uDD01")]
        //[Variation(id = 83, Desc = "CheckChars = true and IndentChars contains invalid surrogate char", Pri = 1, Param = "~surogate~")]
        [Fact]
        public void checkChars_7()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = true;
            wSettings.CloseOutput = true;
            wSettings.Indent = true;
            wSettings.IndentChars = CurVariation.Param.ToString();
            if (CurVariation.Param.ToString() == "~surogate~")
                wSettings.IndentChars = "\uDD12\uDD01";

            XmlWriter w = null;
            try
            {
                w = WriterHelper.Create("writer.out", wSettings);
            }
            catch (ArgumentException e)
            {
                CError.WriteLineIgnore(e.ToString());
                return;
            }
            finally
            {
                if (w != null)
                    w.Dispose();
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        //[Variation(id=90, Desc = "CheckChars = true and NewLineChars contains <", Pri = 1, Param="<")]
        //[Variation(id=91, Desc = "CheckChars = true and NewLineChars contains &", Pri = 1, Param="&")]
        //[Variation(id=92, Desc = "CheckChars = true and NewLineChars contains ]]>", Pri = 1, Param="]]>")]
        //[Variation(id=93, Desc = "CheckChars = true and NewLineChars contains invalid surrogate char", Pri = 1, Param="\uDD12\uDD01")]
        //[Variation(id = 93, Desc = "CheckChars = true and NewLineChars contains invalid surrogate char", Pri = 1, Param = "~surogate~")]
        [Fact]
        public void checkChars_8()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = true;
            wSettings.CloseOutput = true;
            wSettings.NewLineChars = CurVariation.Param.ToString();
            if (CurVariation.Param.ToString() == "~surogate~")
                wSettings.NewLineChars = "\uDD12\uDD01";

            XmlWriter w = null;
            try
            {
                w = WriterHelper.Create("writer.out", wSettings);
            }
            catch (ArgumentException e)
            {
                CError.WriteLineIgnore(e.ToString());
                return;
            }
            finally
            {
                if (w != null)
                    w.Dispose();
            }

            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        //[Variation(id=99, Desc = "CheckChars = true, NewLineOnAttributes = true and IndentChars contains non-whitespace char", Pri = 1)]
        [Fact]
        public void checkChars_9()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = true;
            wSettings.CloseOutput = true;
            wSettings.NewLineOnAttributes = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "foo";

            XmlWriter w = null;
            try
            {
                w = WriterHelper.Create("writer.out", wSettings);
            }
            catch (ArgumentException e)
            {
                CError.WriteLineIgnore(e.ToString());
                return;
            }
            finally
            {
                if (w != null)
                    w.Dispose();
            }

            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }
    }


    //[TestCase(Name="XmlWriterSettings: NewLineHandling")]
    public partial class TCNewLineHandling
    {
        //[Variation(id=1, Desc="Test for CR (xD) inside element when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_1()
        {
            XmlWriter w = CreateWriter();
            w.WriteStartElement("root");
            w.WriteString("\r");
            w.WriteEndElement();
            w.Dispose();
            return CompareString("<root>" + w.Settings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=2, Desc="Test for LF (xA) inside element when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_2()
        {
            XmlWriter w = CreateWriter();
            w.WriteStartElement("root");
            w.WriteString("\n");
            w.WriteEndElement();
            w.Dispose();
            return CompareString("<root>" + w.Settings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=3, Desc="Test for CR LF (xD xA) inside element when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_3()
        {
            XmlWriter w = CreateWriter();
            w.WriteStartElement("root");
            w.WriteString("\r\n");
            w.WriteEndElement();
            w.Dispose();
            return CompareString("<root>" + w.Settings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=4, Desc="Test for CR (xD) inside element when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineHandling, NewLineHandling.Entitize, "Mismatch in NewLineHandling");
            w.WriteStartElement("root");
            w.WriteString("\r");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>&#xD;</root>"));
        }

        //[Variation(id=5, Desc="Test for LF (xA) inside element when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_5()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteString("\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>\xA</root>"));
        }

        //[Variation(id=6, Desc="Test for CR LF (xD xA) inside element when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteString("\r\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>&#xD;\xA</root>"));
        }

        //[Variation(id=7, Desc="Test for CR (xD) inside attr when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_7()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r");
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xD;\" />"));
        }

        //[Variation(id=8, Desc="Test for LF (xA) inside attr when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_8()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xA;\" />"));
        }

        //[Variation(id=9, Desc="Test for CR LF (xD xA) inside attr when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_9()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xD;&#xA;\" />"));
        }

        //[Variation(id=10, Desc="Test for CR (xD) inside attr when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_10()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xD;\" />"));
        }

        //[Variation(id=11, Desc="Test for LF (xA) inside attr when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_11()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xA;\" />"));
        }

        //[Variation(id=12, Desc="Test for CR LF (xD xA) inside attr when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_12()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xD;&#xA;\" />"));
        }

        //[Variation(id=13, Desc="Factory-created writers do not entitize 0xD character in text content when NewLineHandling=Entitize")]
        [Fact]
        public void NewLineHandling_13()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("a");
            w.WriteString("A \r\n \r \n B");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<a>A &#xD;\xA &#xD; \xA B</a>"));
        }
    }

    //[TestCase(Name="XmlWriterSettings: NewLineChars")]
    public partial class TCNewLineChars
    {
        //[Variation(id=1, Desc="Set to tab char", Pri=0)]
        [Fact]
        public void NewLineChars_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\x9";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\x9", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>Test\x9NewLine</root>"));
        }

        //[Variation(id=2, Desc="Set to multiple whitespace chars", Pri=0)]
        [Fact]
        public void NewLineChars_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "     ";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "     ", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>Test     NewLine</root>"));
        }

        //[Variation(id=3, Desc="Set to 0xA", Pri=0)]
        [Fact]
        public void NewLineChars_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\xA";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\xA", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>Test\xANewLine</root>"));
        }

        //[Variation(id=4, Desc="Set to 0xD", Pri=0)]
        [Fact]
        public void NewLineChars_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\xD";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\xD", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>Test\xDNewLine</root>"));
        }

        //[Variation(id=5, Desc="Set to 0x20", Pri=0)]
        [Fact]
        public void NewLineChars_5()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\x20";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\x20", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>Test\x20NewLine</root>"));
        }

        //[Variation(id=6, Desc="Set to <", Pri=1, Param="<")]
        //[Variation(id=7, Desc="Set to &", Pri=1, Param="&")]
        //[Variation(id=8, Desc="Set to comment start tag", Pri=1, Param="<!--")]
        [Fact]
        public void NewLineChars_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = true;
            wSettings.NewLineChars = CurVariation.Param.ToString();

            XmlWriter w = null;

            try
            {
                w = CreateWriter(wSettings);
            }
            catch (ArgumentException e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                return;
            }

            CError.WriteLine("Did not throw ArgumentException");
            Assert.True((WriterType == WriterType.CharCheckingWriter));
        }
    }

    //[TestCase(Name="XmlWriterSettings: Indent")]
    public partial class TCIndent
    {
        //[Variation(id=1, Desc="Simple test when false", Pri=0)]
        [Fact]
        public void indent_1()
        {
            if (IsIndent()) return;
            XmlWriter w = CreateWriter();
            CError.Compare(w.Settings.Indent, false, "Mismatch in Indent");
            w.WriteStartElement("Root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root><child /></Root>"));
        }

        //[Variation(id=2, Desc="Simple test when true", Pri=0)]
        [Fact]
        public void indent_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            return CompareString("<Root>" + wSettings.NewLineChars + "  <child />" + wSettings.NewLineChars + "</Root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=3, Desc="Indent = false, element content is empty", Pri=0)]
        [Fact]
        public void indent_3()
        {
            XmlWriter w = CreateWriter();
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root></Root>"));
        }

        //[Variation(id=4, Desc="Indent = true, element content is empty", Pri=0)]
        [Fact]
        public void indent_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.Indent, true, "Mismatch in Indent");
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root></Root>"));
        }

        //[Variation(id=5, Desc="Indent = false, element content is empty, FullEndElement", Pri=0)]
        [Fact]
        public void indent_5()
        {
            XmlWriter w = CreateWriter();
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteFullEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root></Root>"));
        }

        //[Variation(id=6, Desc="Indent = true, element content is empty, FullEndElement", Pri=0)]
        [Fact]
        public void indent_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteFullEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root></Root>"));
        }

        //[Variation(id=7, Desc="Indent = true, mixed content", Pri=0)]
        [Fact]
        public void indent_7()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteString("test");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root>test<child /></Root>"));
        }

        //[Variation(id=8, Desc="Indent = true, mixed content, FullEndElement", Pri=0)]
        [Fact]
        public void indent_8()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteString("test");
            w.WriteStartElement("child");
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root>test<child></child></Root>"));
        }

        //[Variation(id = 9, Desc = "Other types of non-text nodes", Priority = 0)]
        [Fact]
        public void indent_9()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteProcessingInstruction("xml", "version=\"1.0\"");
            w.WriteDocType("root", null, null, "foo");
            w.WriteStartElement("root");
            w.WriteProcessingInstruction("pi", "pi");
            w.WriteComment("comment");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<?xml version=\"1.0\"?>" + wSettings.NewLineChars + "<!DOCTYPE root [foo]>" + wSettings.NewLineChars + "<root>" + wSettings.NewLineChars + "  <?pi pi?>" + wSettings.NewLineChars + "  <!--comment-->" + wSettings.NewLineChars + "  <foo />" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        //[Variation(id = 10, Desc = "Mixed content after child", Priority = 0)]
        [Fact]
        public void indent_10()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("master");
            w.WriteStartElement("root");
            w.WriteStartElement("foo");
            w.WriteString("text");
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteString("text");
            w.WriteStartElement("foo");
            w.WriteElementString("bar", "text2");
            w.Dispose();
            CError.Compare(CompareString("<master>" + wSettings.NewLineChars + "  <root>" + wSettings.NewLineChars + "    <foo>text</foo>" + wSettings.NewLineChars + "  </root>text<foo><bar>text2</bar></foo></master>"), "");
            return;
        }

        //[Variation(id = 11, Desc = "Mixed content - CData", Priority = 0)]
        [Fact]
        public void indent_11()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteCData("text");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<root><![CDATA[text]]><foo /></root>"), "");
            return;
        }

        //[Variation(id = 12, Desc = "Mixed content - Whitespace", Priority = 0)]
        [Fact]
        public void indent_12()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteWhitespace("  ");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<root>  <foo /></root>"), "");
            return;
        }

        //[Variation(id = 13, Desc = "Mixed content - Raw", Priority = 0)]
        [Fact]
        public void indent_13()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteRaw("text");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<root>text<foo /></root>"), "");
            return;
        }

        //[Variation(id = 14, Desc = "Mixed content - EntityRef", Priority = 0)]
        [Fact]
        public void indent_14()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteEntityRef("e");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<root>&e;<foo /></root>"), "");
            return;
        }

        //[Variation(id = 15, Desc = "Nested Elements - with EndDocument", Priority = 0)]
        [Fact]
        public void indent_15()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("e1");
            w.WriteStartElement("e2");
            w.WriteStartElement("e3");
            w.WriteStartElement("e4");
            w.WriteEndDocument();
            w.Dispose();
            CError.Compare(CompareString("<e1>" + wSettings.NewLineChars + "  <e2>" + wSettings.NewLineChars + "    <e3>" + wSettings.NewLineChars + "      <e4 />" + wSettings.NewLineChars + "    </e3>" + wSettings.NewLineChars + "  </e2>" + wSettings.NewLineChars + "</e1>"), "");
            return;
        }

        //[Variation(id = 16, Desc = "Nested Elements - with EndElement", Priority = 0)]
        [Fact]
        public void indent_16()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("e1");
            w.WriteStartElement("e2");
            w.WriteStartElement("e3");
            w.WriteStartElement("e4");
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<e1>" + wSettings.NewLineChars + "  <e2>" + wSettings.NewLineChars + "    <e3>" + wSettings.NewLineChars + "      <e4 />" + wSettings.NewLineChars + "    </e3>" + wSettings.NewLineChars + "  </e2>" + wSettings.NewLineChars + "</e1>"), "");
            return;
        }

        //[Variation(id = 17, Desc = "Nested Elements - with FullEndElement", Priority = 0)]
        [Fact]
        public void indent_17()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("e1");
            w.WriteStartElement("e2");
            w.WriteStartElement("e3");
            w.WriteStartElement("e4");
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.Dispose();
            CError.Compare(CompareString("<e1>" + wSettings.NewLineChars + "  <e2>" + wSettings.NewLineChars + "    <e3>" + wSettings.NewLineChars + "      <e4></e4>" + wSettings.NewLineChars + "    </e3>" + wSettings.NewLineChars + "  </e2>" + wSettings.NewLineChars + "</e1>"), "");
            return;
        }

        //[Variation(id = 18, Desc = "NewLines after root element", Priority = 0)]
        [Fact]
        public void indent_18()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteElementString("root", "");
            w.WriteComment("c");
            w.WriteProcessingInstruction("pi", "pi");
            w.WriteWhitespace("  ");
            w.WriteComment("c");
            w.WriteProcessingInstruction("pi", "pi");
            w.Dispose();
            CError.Compare(CompareString("<root />" + wSettings.NewLineChars + "<!--c-->" + wSettings.NewLineChars + "<?pi pi?>  <!--c--><?pi pi?>"), "");
            return;
        }

        //[Variation(id = 19, Desc = "Elements with attributes", Priority = 0)]
        [Fact]
        public void indent_19()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("a1", "value");
            w.WriteStartElement("foo");
            w.WriteAttributeString("a2", "value");
            w.WriteEndDocument();
            w.Dispose();
            CError.Compare(CompareString("<root a1=\"value\">" + wSettings.NewLineChars + "  <foo a2=\"value\" />" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        //[Variation(id = 20, Desc = "First PI with start document no xmldecl", Priority = 1)]
        [Fact]
        public void indent_20()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartDocument();
            w.WriteProcessingInstruction("pi", "value");
            w.WriteStartElement("root");
            w.Dispose();
            CError.Compare(CompareString("<?pi value?>" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        //[Variation(id = 21, Desc = "First comment with start document no xmldecl", Priority = 1)]
        [Fact]
        public void indent_21()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartDocument();
            w.WriteComment("value");
            w.WriteStartElement("root");
            w.Dispose();
            CError.Compare(CompareString("<!--value-->" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        //[Variation(id = 22, Desc = "PI in mixed content - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 23, Desc = "PI in mixed content - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        [Fact]
        public void indent_22()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteString("text");
                w.WriteStartElement("child");
                w.WriteProcessingInstruction("pi", "value");
            }
            CError.Compare(CompareString("<root>text<child><?pi value?></child></root>"), "");
            return;
        }

        //[Variation(id = 24, Desc = "Comment in mixed content - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 25, Desc = "Comment in mixed content - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        [Fact]
        public void indent_24()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteString("text");
                w.WriteStartElement("child");
                w.WriteComment("value");
            }
            CError.Compare(CompareString("<root>text<child><!--value--></child></root>"), "");
            return;
        }

        //[Variation(id = 26, Desc = "Mixed content after end element - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 27, Desc = "Mixed content after end element - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        [Fact]
        public void indent_26()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteStartElement("child");
                w.WriteStartElement("a");
                w.WriteEndElement();
                w.WriteString("text");
                w.WriteStartElement("a");
            }
            CError.Compare(CompareString("<root>" + wSettings.NewLineChars + "  <child>" + wSettings.NewLineChars + "    <a />text<a /></child>" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        //[Variation(id = 28, Desc = "First element - no decl", Priority = 1)]
        [Fact]
        public void indent_28()
        {
            // The output should be the same for ConformanceLevel.Document/Auto
            //   and with WriteStartDocument called or not
            int i;
            for (i = 0; i < 4; i++)
            {
                XmlWriterSettings wSettings = new XmlWriterSettings();
                wSettings.OmitXmlDeclaration = true;
                wSettings.Indent = true;
                wSettings.ConformanceLevel = (i % 2) == 0 ? ConformanceLevel.Auto : ConformanceLevel.Document;
                CError.WriteLine("ConformanceLevel: {0}", wSettings.ConformanceLevel.ToString());

                using (XmlWriter w = CreateWriter(wSettings))
                {
                    if (i > 1)
                    {
                        CError.WriteLine("WriteStartDocument called.");
                        w.WriteStartDocument();
                    }
                    else
                    {
                        CError.WriteLine("WriteStartDocument not called.");
                    }
                    w.WriteStartElement("root");
                }
                CError.Compare(CompareString("<root />"), "");
            }
            return;
        }

        //[Variation(id = 29, Desc = "First element - with decl", Priority = 1, Param = true)]
        [Fact]
        public void indent_29()
        {
            // The output should be the same for ConformanceLevel.Document/Auto
            int i;
            for (i = 0; i < 2; i++)
            {
                XmlWriterSettings wSettings = new XmlWriterSettings();
                wSettings.OmitXmlDeclaration = false;
                wSettings.Indent = true;
                wSettings.ConformanceLevel = (i % 2) == 0 ? ConformanceLevel.Auto : ConformanceLevel.Document;
                CError.WriteLine("ConformanceLevel: {0}", wSettings.ConformanceLevel.ToString());

                XmlWriter w = CreateWriter(wSettings);
                Encoding encoding = w.Settings.Encoding;
                if (wSettings.ConformanceLevel == ConformanceLevel.Auto)
                {
                    // Write the decl as PI - since WriteStartDocument would switch to Document mode
                    w.WriteProcessingInstruction("xml", string.Format("version=\"1.0\" encoding=\"{0}\"", encoding.WebName));
                }
                else
                {
                    w.WriteStartDocument();
                }
                w.WriteStartElement("root");
                w.Dispose();
                string expectedResult = string.Format("<?xml version=\"1.0\" encoding=\"{0}\"?>" + wSettings.NewLineChars + "<root />", encoding.WebName);
                CError.Compare(CompareString(expectedResult), "");
            }
            return;
        }

        //[Variation(id = 30, Desc = "Bad indentation of elements with mixed content data - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 31, Desc = "Bad indentation of elements with mixed content data - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        //[Variation(id = 32, Desc = "Bad indentation of elements with mixed content data - Fragment", Priority = 1, Param = ConformanceLevel.Fragment)]
        [Fact]
        public void indent_30()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteStartElement("e1");
                w.WriteStartElement("e2");
                w.WriteEndElement();
                w.WriteString("text");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            CError.Compare(CompareString("<root>" + wSettings.NewLineChars + "  <e1>" + wSettings.NewLineChars + "    <e2 />text</e1>" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        //[Variation(id = 33, Desc = "Indentation error - no new line after PI only if document contains no DocType node - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 34, Desc = "Indentation error - no new line after PI only if document contains no DocType node - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        [Fact]
        public void indent_33()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\"");
                w.WriteProcessingInstruction("piname1", "pitext1");
                w.WriteProcessingInstruction("piname2", "pitext2");
                w.WriteStartElement("root");
                w.WriteEndElement();
            }
            CError.Compare(CompareString("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + wSettings.NewLineChars + "<?piname1 pitext1?>" + wSettings.NewLineChars + "<?piname2 pitext2?>" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        //[Variation(id = 36, Desc = "Indentation error - no new line after PI only if document contains DocType node - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 37, Desc = "Indentation error - no new line after PI only if document contains DocType node - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        [Fact]
        public void indent_36()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\"");
                w.WriteDocType("name", "publicid", "systemid", "subset");
                w.WriteProcessingInstruction("piname1", "pitext1");
                w.WriteProcessingInstruction("piname2", "pitext2");
                w.WriteStartElement("root");
            }
            CError.Compare(CompareString("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + wSettings.NewLineChars + "<!DOCTYPE name PUBLIC \"publicid\" \"systemid\"[subset]>" + wSettings.NewLineChars + "<?piname1 pitext1?>" + wSettings.NewLineChars + "<?piname2 pitext2?>" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        //[Variation(id = 39, Desc = "Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 40, Desc = "Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        //[Variation(id = 41, Desc = "Fragment", Priority = 1, Param = ConformanceLevel.Fragment)]
        [Fact]
        public void indent_39()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteProcessingInstruction("piname1", "pitext1");
                w.WriteComment("comment1");
                w.WriteProcessingInstruction("piname2", "pitext2");
                w.WriteStartElement("root");
                w.WriteStartElement("e1");
                w.WriteStartElement("e2");
                w.WriteStartElement("e3");
                w.WriteStartElement("e4");
                w.WriteEndElement();
                w.WriteString("text1");
                w.WriteProcessingInstruction("piname3", "pitext3");
                w.WriteEndElement();
                w.WriteComment("comment2");
                w.WriteCData("cdata1");
                w.WriteString("text2");
                w.WriteProcessingInstruction("piname4", "pitext4");
                w.WriteCData("cdata2");
                w.WriteComment("comment3");
                w.WriteProcessingInstruction("piname5", "pitext5");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            CError.Compare(CompareString("<?piname1 pitext1?>" + wSettings.NewLineChars + "<!--comment1-->" + wSettings.NewLineChars + "<?piname2 pitext2?>" + wSettings.NewLineChars + "<root>" + wSettings.NewLineChars + "  <e1>" + wSettings.NewLineChars + "    <e2>" + wSettings.NewLineChars + "      <e3>" + wSettings.NewLineChars + "        <e4 />text1<?piname3 pitext3?></e3>" + wSettings.NewLineChars + "      <!--comment2--><![CDATA[cdata1]]>text2<?piname4 pitext4?><![CDATA[cdata2]]><!--comment3--><?piname5 pitext5?></e2>" + wSettings.NewLineChars + "  </e1>" + wSettings.NewLineChars + "</root>"), "");
            return;
        }
    }

    //[TestCase(Name="XmlWriterSettings: IndentChars")]
    public partial class TCIndentChars
    {
        //[Variation(id=1, Desc="Set to tab char", Pri=0)]
        [Fact]
        public void IndentChars_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\x9";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\x9", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root>" + wSettings.NewLineChars + "\x9<child />" + wSettings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=2, Desc="Set to multiple whitespace chars", Pri=0)]
        [Fact]
        public void IndentChars_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "     ";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "     ", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root>" + wSettings.NewLineChars + "     <child />" + wSettings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=3, Desc="Set to 0xA", Pri=0)]
        [Fact]
        public void IndentChars_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\xA";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\xA", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root>" + wSettings.NewLineChars + "\xA<child />" + wSettings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=4, Desc="Set to 0xD", Pri=0)]
        [Fact]
        public void IndentChars_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\xD";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\xD", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root>" + wSettings.NewLineChars + "\xD<child />" + wSettings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=5, Desc="Set to 0x20", Pri=0)]
        [Fact]
        public void IndentChars_5()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\x20";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\x20", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root>" + wSettings.NewLineChars + "\x20<child />" + wSettings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=6, Desc="Set to element start tag", Pri=1, Param="<")]
        //[Variation(id=7, Desc="Set to &", Pri=1, Param="&")]
        //[Variation(id=8, Desc="Set to comment start tag", Pri=1, Param="<!--")]
        [Fact]
        public void IndentChars_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = CurVariation.Param.ToString();

            XmlWriter w = null;
            try
            {
                w = CreateWriter(wSettings);
            }
            catch (ArgumentException e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                return;
            }
            Assert.True((WriterType == WriterType.CharCheckingWriter));
        }
    }

    //[TestCase(Name="XmlWriterSettings: NewLineOnAttributes")]
    public partial class TCNewLineOnAttributes
    {
        //[Variation(id=1, Desc="Make sure the setting has no effect when Indent is false", Pri=0)]
        [Fact]
        public void NewLineOnAttributes_1()
        {
            if (IsIndent()) return;
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineOnAttributes = true;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineOnAttributes, false, "Mismatch in NewLineOnAttributes");

            w.WriteStartElement("root");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr1=\"value1\" attr2=\"value2\" />"));
        }

        //[Variation(id=2, Desc="Sanity test when Indent is true", Pri=1)]
        [Fact]
        public void NewLineOnAttributes_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.NewLineOnAttributes = true;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineOnAttributes, true, "Mismatch in NewLineOnAttributes");

            w.WriteStartElement("root");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root" + wSettings.NewLineChars + "  attr1=\"value1\"" + wSettings.NewLineChars + "  attr2=\"value2\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=3, Desc="Attributes of nested elements", Pri=1)]
        [Fact]
        public void NewLineOnAttributes_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.NewLineOnAttributes = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("level1");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteStartElement("level2");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareBaseline("NewLineOnAttributes3.txt"));
        }
    }

    //[TestCase(Name="Standalone")]
    public partial class TCStandAlone
    {
        //[Variation(id=1, Desc="StartDocument(bool standalone = true)", Pri=0)]
        [Fact]
        public void standalone_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.CloseOutput = false;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartDocument(true);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.WriteEndDocument();
            w.Dispose();

            bool ret = false;

            if (WriterType == WriterType.UnicodeWriter)
                ret = CompareReader("<?xml version=\"1.0\" encoding=\"unicode\" standalone=\"yes\"?><Root />");
            else
                ret = CompareReader("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><Root />");

            Assert.True(ret);
        }

        //[Variation(id=2, Desc="StartDocument(bool standalone = false)", Pri=0)]
        [Fact]
        public void standalone_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.CloseOutput = false;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartDocument(false);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.WriteEndDocument();
            w.Dispose();

            bool ret = false;

            if (WriterType == WriterType.UnicodeWriter)
                ret = CompareReader("<?xml version=\"1.0\" encoding=\"unicode\" standalone=\"no\"?><Root />");
            else
                ret = CompareReader("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?><Root />");

            Assert.True(ret);
        }
    }

    //[TestCase(Name="XmlWriterSettings: CloseOutput")]
    public partial class TCCloseOutput
    {
        //[Variation(id=1, Desc="Check that underlying stream is NOT CLOSED when CloseOutput = FALSE and Create(Stream)", Pri=0, Param="Stream")]
        //[Variation(id=2, Desc="Check that underlying stream is NOT CLOSED when CloseOutput = FALSE and Create(TextWriter)", Pri=0, Param="Textwriter")]
        [Fact]
        public void CloseOutput_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            XmlWriter w = null;
            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }
            Stream writerStream = new MemoryStream();
            switch (CurVariation.Param.ToString())
            {
                case "Stream":
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case "Textwriter":
                    StreamWriter tw = new StreamWriter(writerStream, wSettings.Encoding);
                    w = WriterHelper.Create(tw, wSettings);
                    break;
            }

            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            if (writerStream.CanWrite)
            {
                writerStream.Dispose();
                return;
            }
            CError.WriteLine("Error: XmlWriter closed the stream when CloseOutput = false");
            Assert.True(false);
        }

        //[Variation(id=3, Desc="Check that underlying stream is CLOSED when CloseOutput = FALSE and Create(Uri)", Pri=0, Param="false")]
        //[Variation(id=4, Desc="Check that underlying stream is CLOSED when CloseOutput = TRUE and Create(Uri)", Pri=0, Param="true")]
        [Fact]
        public void CloseOutput_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }
            wSettings.CloseOutput = Boolean.Parse(CurVariation.Param.ToString());

            XmlWriter w = WriterHelper.Create("writer.out", wSettings);
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            // Check if you can open the file in ReadWrite mode
            Stream fs = null;
            try
            {
                fs = FilePathUtil.getStream("writer.out");/*new FileStream("writer.out", FileMode.Open, FileAccess.ReadWrite);*/
            }
            catch (Exception e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                CError.WriteLine("Uri stream is not closed by writer");
                Assert.True(false);
            }
            finally
            {
                fs.Dispose();
            }
            return;
        }

        //[Variation(id=5, Desc="Check that underlying stream is CLOSED when CloseOutput = TRUE and Create(Stream)", Pri=0, Param="Stream")]
        //[Variation(id=6, Desc="Check that underlying stream is CLOSED when CloseOutput = TRUE and Create(Textwriter)", Pri=0, Param="Textwriter")]
        [Fact]
        public void CloseOutput_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = true;
            XmlWriter w = null;

            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }
            Stream writerStream = FilePathUtil.getStream("writer.out");
            switch (CurVariation.Param.ToString())
            {
                case "Stream":
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case "Textwriter":
                    StreamWriter tw = new StreamWriter(writerStream, wSettings.Encoding);
                    w = WriterHelper.Create(tw, wSettings);
                    break;
            }
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            if (writerStream.CanWrite)
            {
                writerStream.Dispose();
                Assert.True(false);
            }
            else
                return;
        }

        //[Variation(id=7, Desc="Writer should not close underlying stream when an exception is thrown before Close (Stream)", Pri=1, Param="Stream")]
        //[Variation(id=8, Desc="Writer should not close underlying stream when an exception is thrown before Close (Textwriter)", Pri=1, Param="Textwriter")]
        [Fact]
        public void CloseOutput_4()
        {
            Stream writerStream = FilePathUtil.getStream("writer.out");
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = true;
            XmlWriter w = null;

            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }

            switch (CurVariation.Param.ToString())
            {
                case "Stream":
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case "Textwriter":
                    StreamWriter tw = new StreamWriter(writerStream, wSettings.Encoding);
                    w = WriterHelper.Create(tw, wSettings);
                    break;
            }

            bool bResult = false;
            try
            {
                w.WriteStartDocument();
                w.WriteStartDocument();
            }
            catch (Exception e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                if (writerStream != null && writerStream.CanWrite)
                    bResult = true;
                else
                    bResult = false;
            }
            finally
            {
                writerStream.Dispose();
            }
            Assert.True(bResult);
        }
    }

    //[TestCase(Name="CL = Fragment Tests")]
    public partial class TCFragmentCL
    {
        //[Variation(id=1, Desc="Multiple root elements should be allowed", Pri=1)]
        [Fact]
        public void frag_1()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Fragment);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(CompareReader("<Root /><Root />"));
        }

        //[Variation(id=2, Desc="Top level text should be allowed - PROLOG", Pri=1)]
        [Fact]
        public void frag_2()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Fragment);
            w.WriteString("Top level text");
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(CompareReader("Top level text<Root />"));
        }

        //[Variation(id=3, Desc="Top level text should be allowed - EPILOG", Pri=1)]
        [Fact]
        public void frag_3()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Fragment);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.WriteString("Top level text");
            w.Dispose();
            Assert.True(CompareReader("<Root />Top level text"));
        }

        //[Variation(id=4, Desc="Top level atomic value should be allowed - PROLOG", Pri=1)]
        [Fact]
        public void frag_4()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Fragment);
            int i = 1;
            w.WriteValue(i);
            w.WriteElementString("Root", "test");
            w.Dispose();
            Assert.True(CompareReader("1<Root>test</Root>"));
        }

        //[Variation(id=5, Desc="Top level atomic value should be allowed - EPILOG", Pri=1)]
        [Fact]
        public void frag_5()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Fragment);
            w.WriteElementString("Root", "test");
            int i = 1;
            w.WriteValue(i);
            w.Dispose();
            Assert.True(CompareReader("<Root>test</Root>1"));
        }

        //[Variation(id=6, Desc="Multiple top level atomic values", Pri=1)]
        [Fact]
        public void frag_6()
        {
            XmlWriter w = CreateWriter(ConformanceLevel.Fragment);
            int i = 1;
            w.WriteValue(i); w.WriteValue(i); w.WriteValue(i); w.WriteValue(i);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.WriteValue(i); w.WriteValue(i); w.WriteValue(i); w.WriteValue(i);
            w.Dispose();
            Assert.True(CompareReader("1111<Root />1111"));
        }

        //[Variation(id=7, Desc="WriteDocType should error when CL=fragment", Pri=1)]
        [Fact]
        public void frag_7()
        {
            using (XmlWriter w = CreateWriter(ConformanceLevel.Fragment))
            {
                try
                {
                    w.WriteDocType("ROOT", "publicid", "sysid", "<!ENTITY e 'abc'>");
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        //[Variation(id=8, Desc="WriteStartDocument() should error when CL=fragment", Pri=1)]
        [Fact]
        public void frag_8()
        {
            using (XmlWriter w = CreateWriter(ConformanceLevel.Fragment))
            {
                try
                {
                    w.WriteStartDocument();
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }
    }

    //[TestCase(Name="CL = Auto Tests")]
    public partial class TCAutoCL
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

    //[TestCase(Name="Close()/Flush()")]
    public partial class TCFlushClose
    {
        //[Variation(id=1, Desc="Verify Flush() flushes underlying stream when CloseOutput = true", Pri=1,Param="true")]
        //[Variation(id=2, Desc="Verify Flush() flushes underlying stream when CloseOutput = false", Pri=1,Param="false")]
        [Fact]
        public void flush_1()
        {
            Stream writerStream = new MemoryStream();
            XmlWriterSettings wSettings = new XmlWriterSettings();
            XmlWriter w = null;
            long expectedLength = 0;
            if (this.CurVariation.Param.ToString() == "true")
                wSettings.CloseOutput = true;
            else
                wSettings.CloseOutput = false;

            switch (WriterType)
            {
                case WriterType.WrappedWriter:
                    expectedLength = 113;
                    XmlWriter ww = WriterHelper.Create(writerStream, wSettings);
                    w = WriterHelper.Create(ww, wSettings);
                    break;
                case WriterType.CharCheckingWriter:
                    expectedLength = 113;
                    XmlWriter w1 = WriterHelper.Create(writerStream, wSettings);
                    XmlWriterSettings ws1 = new XmlWriterSettings();
                    ws1.CheckCharacters = true;
                    w = WriterHelper.Create(w1, ws1);
                    break;
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    expectedLength = 113;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    expectedLength = 224;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.UTF8WriterIndent:
                    wSettings.Encoding = Encoding.UTF8;
                    expectedLength = 125;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.UnicodeWriterIndent:
                    wSettings.Encoding = Encoding.Unicode;
                    expectedLength = 248;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
            }
            if (CurVariation.Param.ToString() == "true")
                wSettings.CloseOutput = true;
            else
                wSettings.CloseOutput = false;

            var beginning = writerStream.Length;

            try
            {
                w.WriteStartElement("root");
                w.WriteStartElement("OneChar");
                w.WriteAttributeString("a", "myAttribute");
                w.WriteString("a");
                w.WriteEndElement();

                w.WriteStartElement("twoChars");
                w.WriteString("ab");
                w.WriteEndElement();
                w.WriteEndElement();

                CError.WriteLine("File Size Before Flush: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, beginning, "Before Flush");

                w.Flush();

                CError.WriteLine("File Size After Flush: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, expectedLength, "After Flush");
            }
            catch (Exception)
            {
                Assert.True(false);
            }
            finally
            {
                w.Dispose();
                writerStream.Dispose();
            }
            return;
        }

        //[Variation(id=3, Desc="Verify Close() flushes underlying stream when CloseOutput = true", Pri=1,Param="true")]
        //[Variation(id=4, Desc="Verify Close() flushes underlying stream when CloseOutput = false", Pri=1,Param="false")]
        [Fact]
        public void close_1()
        {
            if (WriterType != WriterType.UTF8Writer && WriterType != WriterType.UnicodeWriter)
                return;

            Stream writerStream = new MemoryStream();
            XmlWriterSettings wSettings = new XmlWriterSettings();

            long expectedLength1 = 0;
            long expectedLength2 = 0;

            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    expectedLength1 = 83;
                    expectedLength2 = 113;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    expectedLength1 = 164;
                    expectedLength2 = 224;
                    break;
            }

            if (CurVariation.Param.ToString() == "true")
                wSettings.CloseOutput = true;
            else
                wSettings.CloseOutput = false;

            XmlWriter w = WriterHelper.Create(writerStream, wSettings);

            try
            {
                var beginning = writerStream.Length;

                w.WriteStartElement("root");
                w.WriteStartElement("OneChar");
                w.WriteAttributeString("a", "myAttribute");
                w.WriteString("a");
                w.WriteEndElement();

                CError.WriteLine("File Size Before Flush: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, beginning, "Before Flush");

                // Flush mid-way
                w.Flush();
                CError.WriteLine("File Size After Flush: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, expectedLength1, "After Flush");

                w.WriteStartElement("twoChars");
                w.WriteString("ab");
                w.WriteEndElement();
                w.WriteEndElement();
                w.Dispose();

                // Now check that Close() called Flush()
                CError.WriteLine("File Size After Writer.Close: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, expectedLength2, "After Writer.Close");

                // Finally, close the underlying stream, it should be flushed now
                writerStream.Flush();
                CError.WriteLine("File Size After Stream.Close: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, expectedLength2, "After Stream.Close");
            }
            catch (XmlException)
            {
                Assert.True(false);
            }
            finally
            {
                if (writerStream != null)
                    writerStream.Dispose();
                if (w != null)
                    w.Dispose();
            }

            return;
        }

        //[Variation(id=5, Desc="Verify WriterSettings after Close()", Pri=1)]
        [Fact]
        public void close_2()
        {
            XmlWriter w = CreateWriter();
            w.Dispose();
            CError.Equals(w.Settings.Indent, IsIndent() ? true : false, "Incorrect default value of Indent");
            CError.Equals(w.Settings.CheckCharacters, true, "Incorrect default value of CheckCharacters");
            return;
        }
    }

    //[TestCase(Name = "XmlWriter with MemoryStream")]
    public partial class TCWriterWithMemoryStream
    {
        public XmlWriter CreateMemWriter(Stream writerStream, XmlWriterSettings settings)
        {
            XmlWriterSettings wSettings = settings.Clone();
            wSettings.CloseOutput = false;
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = false;
            XmlWriter w = null;

            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.WrappedWriter:
                    XmlWriter ww = WriterHelper.Create(writerStream, wSettings);
                    w = WriterHelper.Create(ww, wSettings);
                    break;
                case WriterType.CharCheckingWriter:
                    XmlWriter cw = WriterHelper.Create(writerStream, wSettings);
                    XmlWriterSettings cws = settings.Clone();
                    cws.CheckCharacters = true;
                    w = WriterHelper.Create(cw, cws);
                    break;
                case WriterType.CustomWriter:
                    w = new CustomWriter(writerStream, wSettings);
                    break;
                case WriterType.UTF8WriterIndent:
                    wSettings.Encoding = Encoding.UTF8;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.UnicodeWriterIndent:
                    wSettings.Encoding = Encoding.Unicode;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                default:
                    throw new Exception("Unknown writer type");
            }
            return w;
        }

        //[Variation(Desc = "XmlWellFormedWriter.Close() throws IndexOutOfRangeException")]
        [Fact]
        public void TFS_661130()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter w = CreateMemWriter(ms, ws))
                {
                    w.WriteStartElement("foo");
                    w.WriteString(new String('a', (2048 * 3) - 50));
                    w.WriteCData(String.Empty);
                    w.WriteComment(String.Empty);
                    w.WriteCData(String.Empty);
                    w.WriteComment(String.Empty);
                }
            }
            return;
        }

        //[Variation(Desc = "XmlWellFormedWriter.Close() throws IndexOutOfRangeException")]
        [Fact]
        public void XmlWellFormedWriterCloseThrowsIndexOutOfRangeException()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter w = CreateMemWriter(ms, ws))
                {
                    w.WriteStartElement("foo");
                    w.WriteString(new String('a', (2048 * 3) - 50));
                    w.WriteRaw("");
                    w.WriteCData("");
                    w.WriteComment("");
                    w.WriteCData("");
                    w.WriteRaw("");
                    w.WriteCData("");
                }
            }
            return;
        }

        //[Variation(Desc = "XmlWellFormedWriter.Close() throws IndexOutOfRangeException")]
        [Fact]
        public void TFS_661130b()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter w = CreateMemWriter(ms, ws))
                {
                    w.WriteStartElement("foo");
                    w.WriteString(new String('a', (2048 * 3) - 50));
                    w.WriteRaw("");
                    w.WriteCData("");
                    w.WriteString("");
                    w.WriteRaw("");
                    w.WriteCData("");
                    w.WriteString("");
                    w.WriteRaw("");
                    w.WriteCData("");
                    w.WriteString("");
                }
            }
            return;
        }

        //[Variation(Desc = "860167.IPublisher.PublishPackage crashes due to disposed.MS", Param = "FileStream")]
        [Fact]
        public void TFS_860167()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(ms, ws))
                    {
                        w.WriteElementString("elem", "text");
                        w.Flush();
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms))
                        {
                            outputXml = reader.ReadToEnd();
                        }
                    }
                }
                CError.WriteLine("actual: " + outputXml);
                CError.Compare(outputXml, "<elem>text</elem>", "wrong xml");
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to disposed.FS")]
        [Fact]
        public void TFS_860167a()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (Stream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(ms, ws))
                    {
                        w.WriteElementString("elem", "text");
                        w.Flush();
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms))
                        {
                            outputXml = reader.ReadToEnd();
                        }
                    }
                }
                CError.WriteLine("actual: " + outputXml);
                CError.Compare(outputXml, "<elem>text</elem>", "wrong xml");
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to disposed.BS with MS")]
        [Fact]
        public void TFS_860167e()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream bs = ms)
                    {
                        using (XmlWriter w = CreateMemWriter(bs, ws))
                        {
                            w.WriteElementString("elem", "text");
                            w.Flush();
                            bs.Position = 0;
                            using (StreamReader reader = new StreamReader(bs))
                            {
                                outputXml = reader.ReadToEnd();
                            }
                        }
                    }
                }
                CError.WriteLine("actual: " + outputXml);
                CError.Compare(outputXml, "<elem>text</elem>", "wrong xml");
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to disposed.BS with FS")]
        [Fact]
        public void TFS_860167f()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (Stream ms = new MemoryStream())
                {
                    using (Stream bs = ms)
                    {
                        using (XmlWriter w = CreateMemWriter(bs, ws))
                        {
                            w.WriteElementString("elem", "text");
                            w.Flush();
                            bs.Position = 0;
                            using (StreamReader reader = new StreamReader(bs))
                            {
                                outputXml = reader.ReadToEnd();
                            }
                        }
                    }
                }
                CError.WriteLine("actual: " + outputXml);
                CError.Compare(outputXml, "<elem>text</elem>", "wrong xml");
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to dispose.MS.WriteRaw")]
        [Fact]
        public void TFS_860167b()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(ms, ws))
                    {
                        w.WriteStartElement("foo");
                        w.WriteString(new String('a', (2048 * 3) - 50));
                        w.WriteRaw("");
                        w.WriteCData("");
                        w.WriteComment("");
                        w.WriteCData("");
                        w.WriteRaw("");
                        w.WriteCData("");
                        w.WriteEndElement();
                        w.Flush();
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms))
                        {
                            reader.ReadToEnd();
                        }
                    }
                }
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to dispose.MS.WriteComment")]
        [Fact]
        public void TFS_860167c()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(ms, ws))
                    {
                        w.WriteStartElement("foo");
                        w.WriteString(new String('a', (2048 * 3) - 50));
                        w.WriteCData(String.Empty);
                        w.WriteComment(String.Empty);
                        w.WriteCData(String.Empty);
                        w.WriteComment(String.Empty);
                        w.WriteEndElement();
                        w.Flush();
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms))
                        {
                            reader.ReadToEnd();
                        }
                    }
                }
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to dispose.MS.WriteCData")]
        [Fact]
        public void TFS_860167d()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(ms, ws))
                    {
                        w.WriteStartElement("foo");
                        w.WriteString(new String('a', (2048 * 3) - 50));
                        w.WriteRaw("");
                        w.WriteCData("");
                        w.WriteString("");
                        w.WriteRaw("");
                        w.WriteCData("");
                        w.WriteString("");
                        w.WriteRaw("");
                        w.WriteCData("");
                        w.WriteString("");
                        w.Flush();
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms))
                        {
                            reader.ReadToEnd();
                        }
                    }
                }
                Assert.True(false);
            }
            catch (ObjectDisposedException e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }
    }
}
