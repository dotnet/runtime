// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Text;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public class TCCheckChars
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
}
