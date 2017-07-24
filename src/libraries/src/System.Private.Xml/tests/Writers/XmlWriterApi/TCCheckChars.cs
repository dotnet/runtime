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
        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "String")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "CData")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "Comment")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "CharEntity")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "EntityRef")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "SurrogateCharEntity")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "PI")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "Whitespace")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "Chars")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "RawString")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "RawChars")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "WriteValue")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "WriteDocTypeName")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "WriteDocTypeSysid")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "WriteDocTypePubid")]
        public void checkChars_1(XmlWriterTestCaseBase utils, string tokenType)
        {
            char[] invalidXML = { '\u0000', '\u0008', '\u000B', '\u000C', '\u000E', '\u001F', '\uFFFE', '\uFFFF' };

            XmlWriter w = utils.CreateWriter(ConformanceLevel.Auto);

            try
            {
                switch (tokenType)
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

        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "String")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "CharEntity")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "Chars")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "WriteValue")]
        public void checkChars_2(XmlWriterTestCaseBase utils, string tokenType)
        {
            char[] invalidXML = { '\u0000', '\u0008', '\u000B', '\u000C', '\u000E', '\u001F', '\uFFFE', '\uFFFF' };

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = false;
            wSettings.CloseOutput = false;

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.CheckCharacters, false, "Mismatch in CheckCharacters");
            try
            {
                w.WriteStartElement("Root");
                switch (tokenType)
                {
                    case "String":
                        w.WriteString(invalidXML[0].ToString());
                        w.WriteEndElement();
                        w.Dispose();
                        Assert.True(utils.CompareReader("<Root>&#x0;</Root>"));
                        return;
                    case "CharEntity":
                        w.WriteCharEntity(invalidXML[3]);
                        w.WriteEndElement();
                        w.Dispose();
                        Assert.True(utils.CompareReader("<Root>&#xC;</Root>"));
                        return;
                    case "Chars":
                        w.WriteChars(invalidXML, 1, 5);
                        w.WriteEndElement();
                        w.Dispose();
                        Assert.True(utils.CompareReader("<Root>&#x8;&#xB;&#xC;&#xE;&#x1F;</Root>"));
                        return;
                    case "WriteValue":
                        w.WriteValue(invalidXML[3].ToString());
                        w.WriteEndElement();
                        w.Dispose();
                        Assert.True(utils.CompareReader("<Root>&#xC;</Root>"));
                        return;
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

        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "Comment")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "PI")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "CData")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "RawString")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "RawChars")]
        public void checkChars_3(XmlWriterTestCaseBase utils, string tokenType)
        {
            char[] invalidXML = { '\u0000', '\u0008', '\u000B', '\u000C', '\u000E', '\u001F', '\uFFFE', '\uFFFF' };

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = false;
            wSettings.CloseOutput = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.CheckCharacters, false, "Mismatch in CheckCharacters");
            w.WriteStartElement("Root");
            switch (tokenType)
            {
                case "Comment":
                    w.WriteComment(invalidXML[2].ToString());
                    w.WriteEndElement();
                    w.Dispose();
                    string exp = utils.IsIndent() ? "<Root>" + Environment.NewLine + "  <!--\u000B-->" + Environment.NewLine + "</Root>" : "<Root><!--\u000B--></Root>";
                    Assert.True(utils.CompareString(exp));
                    break;
                case "PI":
                    w.WriteProcessingInstruction("pi", invalidXML[6].ToString());
                    w.WriteEndElement();
                    w.Dispose();
                    exp = utils.IsIndent() ? "<Root>" + Environment.NewLine + "  <?pi \uFFFE?>" + Environment.NewLine + "</Root>" : "<Root><?pi \uFFFE?></Root>";
                    Assert.True(utils.CompareString(exp));
                    break;
                case "RawString":
                    w.WriteRaw(invalidXML[4].ToString());
                    w.WriteEndElement();
                    w.Dispose();
                    Assert.True(utils.CompareString("<Root>\u000E</Root>"));
                    break;
                case "RawChars":
                    w.WriteRaw(invalidXML, 6, 2);
                    w.WriteEndElement();
                    w.Dispose();
                    Assert.True(utils.CompareString("<Root>\uFFFE\uFFFF</Root>"));
                    break;
                case "CData":
                    w.WriteCData(invalidXML[1].ToString());
                    w.WriteEndElement();
                    w.Dispose();
                    Assert.True(utils.CompareString("<Root><![CDATA[\u0008]]></Root>"));
                    break;
                default:
                    CError.Compare(false, "Invalid param value");
                    Assert.True(false);
                    break;
            }
        }

        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "Whitespace", false)]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "Surrogate", false)]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "EntityRef", false)]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "Name", false)]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "NmToken", false)]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "QName", false)]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "Whitespace", true)]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "Surrogate", true)]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "EntityRef", true)]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "Name", true)]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "NmToken", true)]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, WriterType.AllButCustom & ~WriterType.CharCheckingWriter, "QName", true)]		
        public void checkChars_4(XmlWriterTestCaseBase utils, string tokenType, bool checkCharacters)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = checkCharacters;
            wSettings.CloseOutput = true;

            char[] invalidXML = { '\u0000', '\u0008', '\u000B', '\u000C', '\u000E', '\u001F', '\uFFFE', '\uFFFF' };
            XmlWriter w = utils.CreateWriter(wSettings);
            try
            {
                w.WriteStartElement("Root");
                switch (tokenType)
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
                if ((utils.WriterType == WriterType.UTF8Writer || utils.WriterType == WriterType.UnicodeWriter || utils.WriterType == WriterType.WrappedWriter || utils.IsIndent()) &&
                    (tokenType == "Name" || tokenType == "NmToken"))
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

        /*=============================================================================
        The writer contructor will throw XmlException when CheckCharacters=true and
            - IndentChars or NewLineChars contains non-whitespace character when NewLineOnAttributes=true 
        or
            - IndentChars or NewLineChars contains <, &, ]]> or an invalid surrogate character when NewLineOnAttributes=false
        ===============================================================================*/

        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, "<")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, "&")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, "]]>")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, "~surogate~")]
        public void checkChars_7(XmlWriterTestCaseBase utils, string indentChars)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = true;
            wSettings.CloseOutput = true;
            wSettings.Indent = true;
            wSettings.IndentChars = indentChars;
            if (indentChars == "~surogate~")
                wSettings.IndentChars = "\uDD12\uDD01";

            XmlWriter w = null;
            try
            {
                w = WriterHelper.Create("writer.out", wSettings, overrideAsync: true, async: utils.Async);
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

            Assert.True(false, "Did not throw exception");
        }

        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, "<")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, "&")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, "]]>")]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter, "~surogate~")]
        public void checkChars_8(XmlWriterTestCaseBase utils, string newLineChars)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = true;
            wSettings.CloseOutput = true;
            wSettings.NewLineChars = newLineChars;
            if (newLineChars == "~surogate~")
                wSettings.NewLineChars = "\uDD12\uDD01";

            XmlWriter w = null;
            try
            {
                w = WriterHelper.Create("writer.out", wSettings, overrideAsync: true, async: utils.Async);
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

        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter)]
        public void checkChars_9(XmlWriterTestCaseBase utils)
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
                w = WriterHelper.Create("writer.out", wSettings, overrideAsync: true, async: utils.Async);
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
