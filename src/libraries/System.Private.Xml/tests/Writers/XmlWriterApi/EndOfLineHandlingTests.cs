// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Text;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public partial class TCEOFHandling
    {
        private static NewLineHandling[] s_nlHandlingMembers = { NewLineHandling.Entitize, NewLineHandling.Replace, NewLineHandling.None };
        private StringWriter _strWriter = null;

        private XmlWriter CreateMemWriter(XmlWriterUtils utils, XmlWriterSettings settings)
        {
            XmlWriterSettings wSettings = settings.Clone();
            wSettings.CloseOutput = false;
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = false;
            XmlWriter w = null;

            switch (utils.WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    w = WriterHelper.Create(_strWriter, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    w = WriterHelper.Create(_strWriter, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.WrappedWriter:
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    XmlWriter ww = WriterHelper.Create(_strWriter, wSettings, overrideAsync: true, async: utils.Async);
                    w = WriterHelper.Create(ww, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.CharCheckingWriter:
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    XmlWriter cw = WriterHelper.Create(_strWriter, wSettings, overrideAsync: true, async: utils.Async);
                    XmlWriterSettings cws = settings.Clone();
                    cws.CheckCharacters = true;
                    w = WriterHelper.Create(cw, cws, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.UTF8WriterIndent:
                    wSettings.Encoding = Encoding.UTF8;
                    wSettings.Indent = true;
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    w = WriterHelper.Create(_strWriter, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.UnicodeWriterIndent:
                    wSettings.Encoding = Encoding.Unicode;
                    wSettings.Indent = true;
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    w = WriterHelper.Create(_strWriter, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                default:
                    throw new Exception("Unknown writer type");
            }
            return w;
        }

        private string ExpectedOutput(string input, NewLineHandling h, bool attr)
        {
            string output = input;
            switch (h)
            {
                case NewLineHandling.Entitize:
                    output = output.Replace("\r", "&#xD;");
                    if (attr)
                    {
                        output = output.Replace("\n", "&#xA;");
                        output = output.Replace("\t", "&#x9;");
                    }
                    break;
                case NewLineHandling.Replace:
                    if (!attr)
                    {
                        output = output.Replace("\r\n", "\n");
                        output = output.Replace("\r", "\n");
                        output = output.Replace("\n", Environment.NewLine);
                    }
                    else
                    {
                        output = output.Replace("\r", "&#xD;");
                        output = output.Replace("\n", "&#xA;");
                        output = output.Replace("\t", "&#x9;");
                    }
                    break;
                default:
                    break;
            }
            return output;
        }

        private void VerifyOutput(string expected)
        {
            string actual = _strWriter.ToString();

            if (actual != expected)
            {
                CError.WriteLineIgnore("Expected: " + expected);
                CError.WriteLineIgnore("Actual: " + actual);
                CError.Compare(false, "Expected and actual output differ!");
            }
        }


        /*================== Constructors ==================*/

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom)]
        public void EOF_Handling_01(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Compare(wSettings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value for XmlWriterSettings.NewLineHandling");

            XmlWriter w = utils.CreateWriter();
            w.Dispose();
            CError.Compare(w.Settings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value for XmlWriter.Settings.NewLineHandling");
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None)]
        public void EOF_Handling_02(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();

            wSettings.NewLineHandling = nlHandling;
            XmlWriter w = CreateMemWriter(utils, wSettings);
            CError.Compare(w != null, "XmlWriter creation failed");
            CError.Compare(w.Settings.NewLineHandling, nlHandling, "Invalid NewLineHandling assignment");
            w.Dispose();

            return;
        }

        /*================== Verification in Text Nodes ==================*/

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None)]
        public void EOF_Handling_03(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLineCombinations = "\r \n \r\n \n\r \r\r \n\n \r\n\r \n\r\n";

            wSettings.NewLineHandling = nlHandling;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteElementString("root", NewLineCombinations);
            w.Dispose();
            VerifyOutput("<root>" + ExpectedOutput(NewLineCombinations, nlHandling, false) + "</root>");

            return;
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None)]
        public void EOF_Handling_04(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLineCombinations = "\r \n \r\n \n\r \r\r \n\n \r\n\r \n\r\n";
            string NewLineEntities = "&#xD; &#xA; &#xD;&#xA; &#xA;&#xD; &#xD;&#xD; &#xA;&#xA; &#xD;&#xA;&#xD; &#xA;&#xD;&#xA;";

            wSettings.NewLineHandling = nlHandling;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");

            for (int i = 0; i < NewLineCombinations.Length; i++)
            {
                if (NewLineCombinations[i] == ' ') w.WriteString(" ");
                else w.WriteCharEntity(NewLineCombinations[i]);
            }

            w.WriteEndElement();
            w.Dispose();
            VerifyOutput("<root>" + ExpectedOutput(NewLineEntities, nlHandling, false) + "</root>");

            return;
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None)]
        public void EOF_Handling_05(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLines = "\r&#xA; &#xD;\n &#xD;\r &#xA;\n \n&#xD; &#xA;\r";

            wSettings.NewLineHandling = nlHandling;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");

            // '\r&#xA; '
            w.WriteString("\r");
            w.WriteCharEntity('\n');
            w.WriteString(" ");

            // '&#xD;\n '
            w.WriteCharEntity('\r');
            w.WriteString("\n ");

            // '&#xD;\r '
            w.WriteCharEntity('\r');
            w.WriteString("\r ");

            // '&#xA;\n '
            w.WriteCharEntity('\n');
            w.WriteString("\n ");

            // '\n&#xD; '
            w.WriteString("\n");
            w.WriteCharEntity('\r');
            w.WriteString(" ");

            // '&#xA;\r'
            w.WriteCharEntity('\n');
            w.WriteString("\r");

            w.WriteEndElement();
            w.Dispose();
            VerifyOutput("<root>" + ExpectedOutput(NewLines, nlHandling, false) + "</root>");

            return;
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None)]
        public void EOF_Handling_06(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string Tabs = "foo\tbar&#x9;foo\n\tbar\t\n\t";

            wSettings.NewLineHandling = nlHandling;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");

            w.WriteString("foo\tbar");
            w.WriteCharEntity('\t');
            w.WriteString("foo\n\tbar\t\n\t");

            w.WriteEndElement();
            w.Dispose();
            VerifyOutput("<root>" + ExpectedOutput(Tabs, nlHandling, false) + "</root>");

            return;
        }


        /*================== Verification in Attributes ==================*/

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None)]
        public void EOF_Handling_07(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLineCombinations = "\r \n \r\n \n\r \r\r \n\n \r\n\r \n\r\n";

            wSettings.NewLineHandling = nlHandling;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("foo", NewLineCombinations);
            w.WriteEndElement();
            w.Dispose();
            VerifyOutput("<root foo=\"" + ExpectedOutput(NewLineCombinations, nlHandling, true) + "\" />");

            return;
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None)]
        public void EOF_Handling_08(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLineCombinations = "\r \n \r\n \n\r \r\r \n\n \r\n\r \n\r\n";
            string NewLineEntities = "&#xD; &#xA; &#xD;&#xA; &#xA;&#xD; &#xD;&#xD; &#xA;&#xA; &#xD;&#xA;&#xD; &#xA;&#xD;&#xA;";

            wSettings.NewLineHandling = nlHandling;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");
            w.WriteStartAttribute("foo");

            for (int i = 0; i < NewLineCombinations.Length; i++)
            {
                if (NewLineCombinations[i] == ' ') w.WriteString(" ");
                else w.WriteCharEntity(NewLineCombinations[i]);
            }

            w.WriteEndAttribute();
            w.WriteEndElement();
            w.Dispose();
            VerifyOutput("<root foo=\"" + ExpectedOutput(NewLineEntities, nlHandling, true) + "\" />");

            return;
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None)]
        public void EOF_Handling_09(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLines = "\r&#xA; &#xD;\n &#xD;\r &#xA;\n \n&#xD; &#xA;\r";

            wSettings.NewLineHandling = nlHandling;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");
            w.WriteStartAttribute("foo");

            // '\r&#xA; '
            w.WriteString("\r");
            w.WriteCharEntity('\n');
            w.WriteString(" ");

            // '&#xD;\n '
            w.WriteCharEntity('\r');
            w.WriteString("\n ");

            // '&#xD;\r '
            w.WriteCharEntity('\r');
            w.WriteString("\r ");

            // '&#xA;\n '
            w.WriteCharEntity('\n');
            w.WriteString("\n ");

            // '\n&#xD; '
            w.WriteString("\n");
            w.WriteCharEntity('\r');
            w.WriteString(" ");

            // '&#xA;\r'
            w.WriteCharEntity('\n');
            w.WriteString("\r");

            w.WriteEndAttribute();
            w.WriteEndElement();
            w.Dispose();
            VerifyOutput("<root foo=\"" + ExpectedOutput(NewLines, nlHandling, true) + "\" />");

            return;
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None)]
        public void EOF_Handling_10(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string Tabs = "foo\tbar&#x9;foo\n\tbar\t\n\t";

            wSettings.NewLineHandling = nlHandling;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");
            w.WriteStartAttribute("foo");

            w.WriteString("foo\tbar");
            w.WriteCharEntity('\t');
            w.WriteString("foo\n\tbar\t\n\t");

            w.WriteEndAttribute();
            w.WriteEndElement();
            w.Dispose();
            VerifyOutput("<root foo=\"" + ExpectedOutput(Tabs, nlHandling, true) + "\" />");

            return;
        }


        /*================== NewLineChars, IndentChars ==================*/

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None)]
        public void EOF_Handling_11(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();

            wSettings.NewLineHandling = nlHandling;
            wSettings.Indent = true;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            CError.Compare(w.Settings.NewLineChars, Environment.NewLine, "Incorrect default value for XmlWriter.Settings.NewLineChars");
            CError.Compare(w.Settings.IndentChars, "  ", "Incorrect default value for XmlWriter.Settings.IndentChars");

            w.WriteStartElement("root");
            w.WriteStartElement("foo");
            w.WriteElementString("bar", "");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            VerifyOutput(string.Format("<root>{0}  <foo>{0}    <bar />{0}  </foo>{0}</root>", Environment.NewLine));
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "\r", "  " )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "\r", "  " )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "\r", "  " )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "&#xA;", "  " )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "&#xA;", "  " )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "&#xA;", "  " )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "\r", "\n" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "\r", "\n" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "\r", "\n" )]
        public void EOF_Handling_13(XmlWriterUtils utils, NewLineHandling nlHandling, string newLineChars, string indentChars)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string PrototypeOutput = "<root>&NewLine&Indent<foo>&NewLine&Indent&Indent<bar />&NewLine&Indent</foo>&NewLine</root>";

            wSettings.NewLineHandling = nlHandling;
            wSettings.Indent = true;
            wSettings.NewLineChars = newLineChars;
            wSettings.IndentChars = indentChars;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");
            w.WriteStartElement("foo");
            w.WriteElementString("bar", "");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            VerifyOutput(PrototypeOutput.Replace("&NewLine", newLineChars).Replace("&Indent", indentChars));

            return;
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "\r\n" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "\r\n" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "\r\n" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "\r" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "\r" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "\r" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "---" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "---" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "---" )]
        public void EOF_Handling_14(XmlWriterUtils utils, NewLineHandling nlHandling, string newLineChars)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string PrototypeOutput = "<root>foo&NewLinefoo&NewLinefoo&NewLinefoo\tfoo</root>";

            wSettings.NewLineHandling = nlHandling;
            wSettings.Indent = true;
            wSettings.NewLineChars = newLineChars;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteElementString("root", "foo\r\nfoo\nfoo\rfoo\tfoo");
            w.Dispose();

            if (nlHandling == NewLineHandling.Replace)
                VerifyOutput(PrototypeOutput.Replace("&NewLine", newLineChars));
            else
                VerifyOutput("<root>" + ExpectedOutput("foo\r\nfoo\nfoo\rfoo\tfoo", nlHandling, false) + "</root>");
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "\r\n" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "\r\n" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "\r\n" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "\r" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "\r" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "\r" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "---" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "---" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "---" )]
        public void EOF_Handling_15(XmlWriterUtils utils, NewLineHandling nlHandling, string newLineChars)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();

            wSettings.NewLineHandling = nlHandling;
            wSettings.Indent = true;
            wSettings.NewLineChars = newLineChars;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("foo", "foo\r\nfoo\nfoo\rfoo\tfoo");
            w.WriteEndElement();
            w.Dispose();

            VerifyOutput("<root foo=\"" + ExpectedOutput("foo\r\nfoo\nfoo\rfoo\tfoo", nlHandling, true) + "\" />");
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "\r\n")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "\r\n")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "\r\n")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "\r")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "\r")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "\r")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "---")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "---")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "---")]
        public void EOF_Handling_16(XmlWriterUtils utils, NewLineHandling nlHandling, string newLineChars)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string PrototypeOutput = "<root&NewLine  foo=\"fooval\"&NewLine  bar=\"barval\" />";

            wSettings.NewLineHandling = nlHandling;
            wSettings.Indent = true;
            wSettings.NewLineChars = newLineChars;
            wSettings.NewLineOnAttributes = true;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("foo", "fooval");
            w.WriteAttributeString("bar", "barval");
            w.WriteEndElement();
            w.Dispose();

            VerifyOutput(PrototypeOutput.Replace("&NewLine", newLineChars));
        }


        /*================== Other types of nodes ==================*/

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom & WriterType.AllButIndenting, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom & WriterType.AllButIndenting, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom & WriterType.AllButIndenting, NewLineHandling.None)]
        public void EOF_Handling_17(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLines = "\r \n \r\n";

            wSettings.NewLineHandling = nlHandling;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");
            w.WriteCData(NewLines);
            w.WriteChars(NewLines.ToCharArray(), 0, NewLines.Length);
            w.WriteEndElement();
            w.WriteProcessingInstruction("pi", NewLines);
            w.WriteWhitespace(NewLines);
            w.WriteComment(NewLines);
            w.Dispose();

            // Inside Comments and CDATA blocks NewLines are never entitized (needs spec BUG)
            string expOut;
            if (nlHandling == NewLineHandling.Entitize)
                expOut = "<root><![CDATA[" + NewLines + "]]>" + ExpectedOutput(NewLines, NewLineHandling.Entitize, false) + "</root>" + "<?pi " + NewLines + "?>" + ExpectedOutput(NewLines, NewLineHandling.Entitize, false) + "<!--" + NewLines + "-->";
            else
                expOut = ExpectedOutput("<root><![CDATA[" + NewLines + "]]>" + NewLines + "</root><?pi " + NewLines + "?>" + NewLines + "<!--" + NewLines + "-->", nlHandling, false);

            VerifyOutput(expOut);
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "\r\n")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "\r\n")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "\r\n")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "\r")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "\r")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "\r")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "---")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "---")]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "---")]
        public void EOF_Handling_18(XmlWriterUtils utils, NewLineHandling nlHandling, string newLineChars)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string PrototypeOutput = "<root><![CDATA[foo&NewLinefoo&NewLinefoo&NewLinefoo\tfoo]]></root>&NewLine<?pi foo&NewLinefoo&NewLinefoo&NewLinefoo\tfoo?>&NewLine<!--foo&NewLinefoo&NewLinefoo&NewLinefoo\tfoo-->";

            wSettings.NewLineHandling = nlHandling;
            wSettings.Indent = true;
            wSettings.NewLineChars = newLineChars;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");
            w.WriteCData("foo\r\nfoo\nfoo\rfoo\tfoo");
            w.WriteEndElement();
            w.WriteProcessingInstruction("pi", "foo\r\nfoo\nfoo\rfoo\tfoo");
            w.WriteComment("foo\r\nfoo\nfoo\rfoo\tfoo");
            w.Dispose();

            if (nlHandling == NewLineHandling.Replace)
                VerifyOutput(PrototypeOutput.Replace("&NewLine", newLineChars));
            else
                VerifyOutput("<root><![CDATA[foo\r\nfoo\nfoo\rfoo\tfoo]]></root>&NewLine<?pi foo\r\nfoo\nfoo\rfoo\tfoo?>&NewLine<!--foo\r\nfoo\nfoo\rfoo\tfoo-->".Replace("&NewLine", newLineChars));

            return;
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom & WriterType.AllButIndenting, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom & WriterType.AllButIndenting, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom & WriterType.AllButIndenting, NewLineHandling.None)]
        public void EOF_Handling_19(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NewLineHandling = nlHandling;
            wSettings.CheckCharacters = false;
            XmlWriter ww = CreateMemWriter(utils, wSettings);
            XmlWriterSettings ws = wSettings.Clone();
            ws.NewLineHandling = NewLineHandling.Replace;
            ws.CheckCharacters = true;
            XmlWriter w = WriterHelper.Create(ww, ws, overrideAsync: true, async: utils.Async);

            string NewLines = "\r \n " + Environment.NewLine;

            w.WriteStartElement("root");
            w.WriteCData(NewLines);
            w.WriteChars(NewLines.ToCharArray(), 0, NewLines.Length);
            w.WriteEndElement();
            w.WriteProcessingInstruction("pi", NewLines);
            w.WriteWhitespace(NewLines);
            w.WriteComment(NewLines);
            w.Dispose();

            string expOut;
            if (nlHandling == NewLineHandling.Entitize)
                expOut = "<root><![CDATA[" + NewLines + "]]>" + ExpectedOutput(NewLines, NewLineHandling.Entitize, false) + "</root>" + "<?pi " + NewLines + "?>" + ExpectedOutput(NewLines, NewLineHandling.Entitize, false) + "<!--" + NewLines + "-->";
            else
                expOut = ExpectedOutput("<root><![CDATA[" + NewLines + "]]>" + NewLines + "</root><?pi " + NewLines + "?>" + NewLines + "<!--" + NewLines + "-->", NewLineHandling.Replace, false);

            VerifyOutput(expOut);
            return;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12)]
        public void EOF_Handling_20(int param)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            switch (param)
            {
                case 1: ws.IndentChars = ""; break;
                case 2: ws.NewLineChars = ""; break;
                case 3: ws.IndentChars = "    "; break;
                case 4: ws.NewLineChars = "   "; break;
                case 5: ws.IndentChars = "  @  "; break;
                case 6: ws.NewLineChars = "  @  "; break;
                case 7: ws.IndentChars = "2"; break;
                case 8: ws.NewLineChars = "2"; break;
                case 9: ws.IndentChars = " a "; break;
                case 10: ws.NewLineChars = " a "; break;
                case 11: ws.Indent = true; break;
                case 12: ws.Indent = false; break;
            }
            return;
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "\uD800\uDC00", "\uD800\uDC00" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "\uD800\uDC00", "\uD800\uDC00" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "\uD800\uDC00", "\uD800\uDC00" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize, "&lt;&gt;", "&lt;&gt;" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace, "&lt;&gt;", "&lt;&gt;" )]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None, "&lt;&gt;", "&lt;&gt;" )]
        public void EOF_Handling_21(XmlWriterUtils utils, NewLineHandling nlHandling, string newLineChars, string indentChars)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string PrototypeOutput = "<root>&NewLine&Indent<foo>&NewLine&Indent&Indent<bar />&NewLine&Indent</foo>&NewLine</root>";

            wSettings.NewLineHandling = nlHandling;
            wSettings.Indent = true;
            wSettings.NewLineChars = newLineChars;
            wSettings.IndentChars = indentChars;

            XmlWriter w = CreateMemWriter(utils, wSettings);
            w.WriteStartElement("root");
            w.WriteStartElement("foo");
            w.WriteElementString("bar", "");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            VerifyOutput(PrototypeOutput.Replace("&NewLine", newLineChars).Replace("&Indent", indentChars));
            return;
        }

        [Theory]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Entitize)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.Replace)]
        [XmlWriterInlineData(~WriterType.Async & WriterType.AllButCustom, NewLineHandling.None)]
        public void EOF_Handling_22(XmlWriterUtils utils, NewLineHandling nlHandling)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NewLineHandling = nlHandling;
            wSettings.Indent = true;
            wSettings.NewLineChars = "\uDE40\uDA72";
            wSettings.IndentChars = "\uDE40\uDA72";

            XmlWriter w = CreateMemWriter(utils, wSettings);
            try
            {
                w.WriteStartElement("root");
                w.WriteStartElement("foo");
                w.Dispose();
            }
            catch (ArgumentException e) { CError.WriteLine(e.Message); return; }
            Assert.True(false);
        }
    }
}
