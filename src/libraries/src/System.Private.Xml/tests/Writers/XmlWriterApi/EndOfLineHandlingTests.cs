// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Text;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    //[TestCase(Name="XmlWriterSettings: NewLineHandling")]
    public partial class TCEOFHandling : XmlFactoryWriterTestCaseBase
    {
        public override int Init(object o)
        {
            if (WriterType == WriterType.UnicodeWriter || WriterType == WriterType.UTF8Writer || WriterType == WriterType.WrappedWriter
                 || WriterType == WriterType.CharCheckingWriter || WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent)
            {
                int i = base.Init(0);
                return i;
            }
            return TEST_SKIPPED;
        }

        private static NewLineHandling[] s_nlHandlingMembers = { NewLineHandling.Entitize, NewLineHandling.Replace, NewLineHandling.None };
        private StringWriter _strWriter = null;

        private XmlWriter CreateMemWriter(XmlWriterSettings settings)
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
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    w = WriterHelper.Create(_strWriter, wSettings);
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    w = WriterHelper.Create(_strWriter, wSettings);
                    break;
                case WriterType.WrappedWriter:
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    XmlWriter ww = WriterHelper.Create(_strWriter, wSettings);
                    w = WriterHelper.Create(ww, wSettings);
                    break;
                case WriterType.CharCheckingWriter:
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    XmlWriter cw = WriterHelper.Create(_strWriter, wSettings);
                    XmlWriterSettings cws = settings.Clone();
                    cws.CheckCharacters = true;
                    w = WriterHelper.Create(cw, cws);
                    break;
                case WriterType.UTF8WriterIndent:
                    wSettings.Encoding = Encoding.UTF8;
                    wSettings.Indent = true;
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    w = WriterHelper.Create(_strWriter, wSettings);
                    break;
                case WriterType.UnicodeWriterIndent:
                    wSettings.Encoding = Encoding.Unicode;
                    wSettings.Indent = true;
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    w = WriterHelper.Create(_strWriter, wSettings);
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
                        output = output.Replace("\n", nl);
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

        //[Variation(Desc = "NewLineHandling Default value - NewLineHandling.Replace", id = 1, Pri = 0)]
        public int EOF_Handling_01()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Compare(wSettings.NewLineHandling, NewLineHandling.Replace, "Incorect default value for XmlWriterSettings.NewLineHandling");

            XmlWriter w = CreateWriter();
            w.Dispose();
            CError.Compare(w.Settings.NewLineHandling, NewLineHandling.Replace, "Incorect default value for XmlWriter.Settings.NewLineHandling");

            return TEST_PASS;
        }

        //[Variation(Desc = "XmlWriter creation with NewLineHandling.Entitize", Param = NewLineHandling.Entitize, id = 2, Pri = 0)]
        //[Variation(Desc = "XmlWriter creation with NewLineHandling.Replace", Param = NewLineHandling.Replace, id = 3, Pri = 0)]
        //[Variation(Desc = "XmlWriter creation with NewLineHandling.None", Param = NewLineHandling.None, id = 4, Pri = 0)]
        public int EOF_Handling_02()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;
            XmlWriter w = CreateMemWriter(wSettings);
            CError.Compare(w != null, "XmlWriter creation failed");
            CError.Compare(w.Settings.NewLineHandling, (NewLineHandling)CurVariation.Param, "Invalid NewLineHandling assignment");
            w.Dispose();

            return TEST_PASS;
        }

        /*================== Verification in Text Nodes ==================*/

        //[Variation(Desc = "Check for combinations of NewLine characters in element with 'Entitize'", Param = NewLineHandling.Entitize, id = 5, Pri = 0)]
        //[Variation(Desc = "Check for combinations of NewLine characters in element with 'Replace'", Param = NewLineHandling.Replace, id = 6, Pri = 0)]
        //[Variation(Desc = "Check for combinations of NewLine characters in element with 'None'", Param = NewLineHandling.None, id = 7, Pri = 0)]
        public int EOF_Handling_03()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLineCombinations = "\r \n \r\n \n\r \r\r \n\n \r\n\r \n\r\n";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteElementString("root", NewLineCombinations);
            w.Dispose();
            VerifyOutput("<root>" + ExpectedOutput(NewLineCombinations, (NewLineHandling)CurVariation.Param, false) + "</root>");

            return TEST_PASS;
        }

        //[Variation(Desc = "Check for combinations of entities in element with 'Entitize'", Param = NewLineHandling.Entitize, id = 8, Pri = 0)]
        //[Variation(Desc = "Check for combinations of entities in element with 'Replace'", Param = NewLineHandling.Replace, id = 9, Pri = 0)]
        //[Variation(Desc = "Check for combinations of entities in element with 'None'", Param = NewLineHandling.None, id = 10, Pri = 0)]
        public int EOF_Handling_04()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLineCombinations = "\r \n \r\n \n\r \r\r \n\n \r\n\r \n\r\n";
            string NewLineEntities = "&#xD; &#xA; &#xD;&#xA; &#xA;&#xD; &#xD;&#xD; &#xA;&#xA; &#xD;&#xA;&#xD; &#xA;&#xD;&#xA;";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteStartElement("root");

            for (int i = 0; i < NewLineCombinations.Length; i++)
            {
                if (NewLineCombinations[i] == ' ') w.WriteString(" ");
                else w.WriteCharEntity(NewLineCombinations[i]);
            }

            w.WriteEndElement();
            w.Dispose();
            VerifyOutput("<root>" + ExpectedOutput(NewLineEntities, (NewLineHandling)CurVariation.Param, false) + "</root>");

            return TEST_PASS;
        }

        //[Variation(Desc = "Check for combinations of NewLine characters and entities in element with 'Entitize'", Param = NewLineHandling.Entitize, id = 11, Pri = 0)]
        //[Variation(Desc = "Check for combinations of NewLine characters and entities in element with 'Replace'", Param = NewLineHandling.Replace, id = 12, Pri = 0)]
        //[Variation(Desc = "Check for combinations of NewLine characters and entities in element with 'None'", Param = NewLineHandling.None, id = 13, Pri = 0)]
        public int EOF_Handling_05()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLines = "\r&#xA; &#xD;\n &#xD;\r &#xA;\n \n&#xD; &#xA;\r";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;

            XmlWriter w = CreateMemWriter(wSettings);
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
            VerifyOutput("<root>" + ExpectedOutput(NewLines, (NewLineHandling)CurVariation.Param, false) + "</root>");

            return TEST_PASS;
        }


        //[Variation(Desc = "Check for tab character in element with 'Entitize'", Param = NewLineHandling.Entitize, id = 14, Pri = 0)]
        //[Variation(Desc = "Check for tab character in element with 'Replace'", Param = NewLineHandling.Replace, id = 15, Pri = 0)]
        //[Variation(Desc = "Check for tab character in element with 'None'", Param = NewLineHandling.None, id = 16, Pri = 0)]
        public int EOF_Handling_06()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string Tabs = "foo\tbar&#x9;foo\n\tbar\t\n\t";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteStartElement("root");

            w.WriteString("foo\tbar");
            w.WriteCharEntity('\t');
            w.WriteString("foo\n\tbar\t\n\t");

            w.WriteEndElement();
            w.Dispose();
            VerifyOutput("<root>" + ExpectedOutput(Tabs, (NewLineHandling)CurVariation.Param, false) + "</root>");

            return TEST_PASS;
        }


        /*================== Verification in Attributes ==================*/

        //[Variation(Desc = "Check for combinations of NewLine characters in attribute with 'Entitize'", Param = NewLineHandling.Entitize, id = 17, Pri = 0)]
        //[Variation(Desc = "Check for combinations of NewLine characters in attribute with 'Replace'", Param = NewLineHandling.Replace, id = 18, Pri = 0)]
        //[Variation(Desc = "Check for combinations of NewLine characters in attribute with 'None'", Param = NewLineHandling.None, id = 19, Pri = 0)]
        public int EOF_Handling_07()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLineCombinations = "\r \n \r\n \n\r \r\r \n\n \r\n\r \n\r\n";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("foo", NewLineCombinations);
            w.WriteEndElement();
            w.Dispose();
            VerifyOutput("<root foo=\"" + ExpectedOutput(NewLineCombinations, (NewLineHandling)CurVariation.Param, true) + "\" />");

            return TEST_PASS;
        }

        //[Variation(Desc = "Check for combinations of entities in attribute with 'Entitize'", Param = NewLineHandling.Entitize, id = 20, Pri = 0)]
        //[Variation(Desc = "Check for combinations of entities in attribute with 'Replace'", Param = NewLineHandling.Replace, id = 21, Pri = 0)]
        //[Variation(Desc = "Check for combinations of entities in attribute with 'None'", Param = NewLineHandling.None, id = 22, Pri = 0)]
        public int EOF_Handling_08()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLineCombinations = "\r \n \r\n \n\r \r\r \n\n \r\n\r \n\r\n";
            string NewLineEntities = "&#xD; &#xA; &#xD;&#xA; &#xA;&#xD; &#xD;&#xD; &#xA;&#xA; &#xD;&#xA;&#xD; &#xA;&#xD;&#xA;";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;

            XmlWriter w = CreateMemWriter(wSettings);
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
            VerifyOutput("<root foo=\"" + ExpectedOutput(NewLineEntities, (NewLineHandling)CurVariation.Param, true) + "\" />");

            return TEST_PASS;
        }

        //[Variation(Desc = "Check for combinations of NewLine characters and entities in element with 'Entitize'", Param = NewLineHandling.Entitize, id = 23, Pri = 0)]
        //[Variation(Desc = "Check for combinations of NewLine characters and entities in element with 'Replace'", Param = NewLineHandling.Replace, id = 24, Pri = 0)]
        //[Variation(Desc = "Check for combinations of NewLine characters and entities in element with 'None'", Param = NewLineHandling.None, id = 25, Pri = 0)]
        public int EOF_Handling_09()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLines = "\r&#xA; &#xD;\n &#xD;\r &#xA;\n \n&#xD; &#xA;\r";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;

            XmlWriter w = CreateMemWriter(wSettings);
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
            VerifyOutput("<root foo=\"" + ExpectedOutput(NewLines, (NewLineHandling)CurVariation.Param, true) + "\" />");

            return TEST_PASS;
        }

        //[Variation(Desc = "Check for tab character in attribute with 'Entitize'", Param = NewLineHandling.Entitize, id = 26, Pri = 0)]
        //[Variation(Desc = "Check for tab character in attribute with 'Replace'", Param = NewLineHandling.Replace, id = 27, Pri = 0)]
        //[Variation(Desc = "Check for tab character in attribute with 'None'", Param = NewLineHandling.None, id = 28, Pri = 0)]
        public int EOF_Handling_10()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string Tabs = "foo\tbar&#x9;foo\n\tbar\t\n\t";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteStartAttribute("foo");

            w.WriteString("foo\tbar");
            w.WriteCharEntity('\t');
            w.WriteString("foo\n\tbar\t\n\t");

            w.WriteEndAttribute();
            w.WriteEndElement();
            w.Dispose();
            VerifyOutput("<root foo=\"" + ExpectedOutput(Tabs, (NewLineHandling)CurVariation.Param, true) + "\" />");

            return TEST_PASS;
        }


        /*================== NewLineChars, IndentChars ==================*/

        //[Variation(Desc = "NewLineChars and IndentChars Default values and test for proper indentation, Entitize", Param = NewLineHandling.Entitize, id = 29, Pri = 1)]
        //[Variation(Desc = "NewLineChars and IndentChars Default values and test for proper indentation, Replace", Param = NewLineHandling.Replace, id = 30, Pri = 1)]
        //[Variation(Desc = "NewLineChars and IndentChars Default values and test for proper indentation, None", Param = NewLineHandling.None, id = 31, Pri = 1)]
        public int EOF_Handling_11()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;
            wSettings.Indent = true;

            XmlWriter w = CreateMemWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, nl, "Incorect default value for XmlWriter.Settings.NewLineChars");
            CError.Compare(w.Settings.IndentChars, "  ", "Incorect default value for XmlWriter.Settings.IndentChars");

            w.WriteStartElement("root");
            w.WriteStartElement("foo");
            w.WriteElementString("bar", "");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            VerifyOutput(string.Format("<root>{0}  <foo>{0}    <bar />{0}  </foo>{0}</root>", nl));

            return TEST_PASS;
        }

        //[Variation(Desc = "1.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\\r', '  '", Params = new object[] { NewLineHandling.Entitize, "\r", "  " }, id = 32, Pri = 2)]
        //[Variation(Desc = "2.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\\r', '  '", Params = new object[] { NewLineHandling.Replace, "\r", "  " }, id = 33, Pri = 2)]
        //[Variation(Desc = "3.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\\r', '  '", Params = new object[] { NewLineHandling.None, "\r", "  " }, id = 34, Pri = 2)]
        //[Variation(Desc = "4.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '&#xA;', '  '", Params = new object[] { NewLineHandling.Entitize, "&#xA;", "  " }, id = 35, Pri = 2)]
        //[Variation(Desc = "5.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '&#xA;', '  '", Params = new object[] { NewLineHandling.Replace, "&#xA;", "  " }, id = 36, Pri = 2)]
        //[Variation(Desc = "6.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '&#xA;', '  '", Params = new object[] { NewLineHandling.None, "&#xA;", "  " }, id = 37, Pri = 2)]
        //[Variation(Desc = "7.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\\r', '\\n'", Params = new object[] { NewLineHandling.Entitize, "\r", "\n" }, id = 38, Pri = 2)]
        //[Variation(Desc = "8.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\\r', '\\n'", Params = new object[] { NewLineHandling.Replace, "\r", "\n" }, id = 39, Pri = 2)]
        //[Variation(Desc = "9.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\\r', '\\n'", Params = new object[] { NewLineHandling.None, "\r", "\n" }, id = 40, Pri = 2)]
        public int EOF_Handling_13()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string PrototypeOutput = "<root>&NewLine&Indent<foo>&NewLine&Indent&Indent<bar />&NewLine&Indent</foo>&NewLine</root>";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Params[0];
            wSettings.Indent = true;
            wSettings.NewLineChars = CurVariation.Params[1].ToString();
            wSettings.IndentChars = CurVariation.Params[2].ToString();

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteStartElement("foo");
            w.WriteElementString("bar", "");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            VerifyOutput(PrototypeOutput.Replace("&NewLine", CurVariation.Params[1].ToString()).Replace("&Indent", CurVariation.Params[2].ToString()));

            return TEST_PASS;
        }

        //[Variation(Desc = "NewLine handling in text node when Indent=true; Entitize, '\\r\\n'", Params = new object[] { NewLineHandling.Entitize, "\r\n" }, id = 41, Pri = 1)]
        //[Variation(Desc = "NewLine handling in text node when Indent=true; Replace, '\\r\\n'", Params = new object[] { NewLineHandling.Replace, "\r\n" }, id = 42, Pri = 1)]
        //[Variation(Desc = "NewLine handling in text node when Indent=true; None, '\\r\\n'", Params = new object[] { NewLineHandling.None, "\r\n" }, id = 43, Pri = 1)]
        //[Variation(Desc = "NewLine handling in text node when Indent=true; Entitize, '\\r'", Params = new object[] { NewLineHandling.Entitize, "\r" }, id = 44, Pri = 2)]
        //[Variation(Desc = "NewLine handling in text node when Indent=true; Replace, '\\r'", Params = new object[] { NewLineHandling.Replace, "\r" }, id = 45, Pri = 2)]
        //[Variation(Desc = "NewLine handling in text node when Indent=true; None, '\\r'", Params = new object[] { NewLineHandling.None, "\r" }, id = 46, Pri = 2)]
        //[Variation(Desc = "NewLine handling in text node when Indent=true; Entitize, '---'", Params = new object[] { NewLineHandling.Entitize, "---" }, id = 47, Pri = 2)]
        //[Variation(Desc = "NewLine handling in text node when Indent=true; Replace, '---'", Params = new object[] { NewLineHandling.Replace, "---" }, id = 48, Pri = 2)]
        //[Variation(Desc = "NewLine handling in text node when Indent=true; None, '---'", Params = new object[] { NewLineHandling.None, "---" }, id = 49, Pri = 2)]
        public int EOF_Handling_14()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string PrototypeOutput = "<root>foo&NewLinefoo&NewLinefoo&NewLinefoo\tfoo</root>";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Params[0];
            wSettings.Indent = true;
            wSettings.NewLineChars = CurVariation.Params[1].ToString();

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteElementString("root", "foo\r\nfoo\nfoo\rfoo\tfoo");
            w.Dispose();

            if ((NewLineHandling)CurVariation.Params[0] == NewLineHandling.Replace)
                VerifyOutput(PrototypeOutput.Replace("&NewLine", CurVariation.Params[1].ToString()));
            else
                VerifyOutput("<root>" + ExpectedOutput("foo\r\nfoo\nfoo\rfoo\tfoo", (NewLineHandling)CurVariation.Params[0], false) + "</root>");

            return TEST_PASS;
        }

        //[Variation(Desc = "NewLine handling in attribute when Indent=true; Entitize, '\\r\\n'", Params = new object[] { NewLineHandling.Entitize, "\r\n" }, id = 50, Pri = 1)]
        //[Variation(Desc = "NewLine handling in attribute when Indent=true; Replace, '\\r\\n'", Params = new object[] { NewLineHandling.Replace, "\r\n" }, id = 51, Pri = 1)]
        //[Variation(Desc = "NewLine handling in attribute when Indent=true; None, '\\r\\n'", Params = new object[] { NewLineHandling.None, "\r\n" }, id = 52, Pri = 1)]
        //[Variation(Desc = "NewLine handling in attribute when Indent=true; Entitize, '\\r'", Params = new object[] { NewLineHandling.Entitize, "\r" }, id = 53, Pri = 2)]
        //[Variation(Desc = "NewLine handling in attribute when Indent=true; Replace, '\\r'", Params = new object[] { NewLineHandling.Replace, "\r" }, id = 54, Pri = 2)]
        //[Variation(Desc = "NewLine handling in attribute when Indent=true; None, '\\r'", Params = new object[] { NewLineHandling.None, "\r" }, id = 54, Pri = 2)]
        //[Variation(Desc = "NewLine handling in attribute when Indent=true; Entitize, '---'", Params = new object[] { NewLineHandling.Entitize, "---" }, id = 54, Pri = 2)]
        //[Variation(Desc = "NewLine handling in attribute when Indent=true; Replace, '---'", Params = new object[] { NewLineHandling.Replace, "---" }, id = 55, Pri = 2)]
        //[Variation(Desc = "NewLine handling in attribute when Indent=true; None, '---'", Params = new object[] { NewLineHandling.None, "---" })]
        public int EOF_Handling_15()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Params[0];
            wSettings.Indent = true;
            wSettings.NewLineChars = CurVariation.Params[1].ToString();

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("foo", "foo\r\nfoo\nfoo\rfoo\tfoo");
            w.WriteEndElement();
            w.Dispose();

            VerifyOutput("<root foo=\"" + ExpectedOutput("foo\r\nfoo\nfoo\rfoo\tfoo", (NewLineHandling)CurVariation.Params[0], true) + "\" />");

            return TEST_PASS;
        }

        //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; Entitize, '\\r\\n'", Params = new object[] { NewLineHandling.Entitize, "\r\n" }, id = 56, Pri = 1)]
        //[Variation(Desc = "NewLine handling betwwen attributes when NewLineOnAttributes=true; Replace, '\\r\\n'", Params = new object[] { NewLineHandling.Replace, "\r\n" }, id = 57, Pri = 1)]
        //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; None, '\\r\\n'", Params = new object[] { NewLineHandling.None, "\r\n" }, id = 58, Pri = 1)]
        //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; Entitize, '\\r'", Params = new object[] { NewLineHandling.Entitize, "\r" }, id = 59, Pri = 2)]
        //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; Replace, '\\r'", Params = new object[] { NewLineHandling.Replace, "\r" }, id = 60, Pri = 2)]
        //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; None, '\\r'", Params = new object[] { NewLineHandling.None, "\r" }, id = 61, Pri = 2)]
        //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; Entitize, '---'", Params = new object[] { NewLineHandling.Entitize, "---" }, id = 62, Pri = 2)]
        //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; Replace, '---'", Params = new object[] { NewLineHandling.Replace, "---" }, id = 63, Pri = 2)]
        //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; None, '---'", Params = new object[] { NewLineHandling.None, "---" }, id = 64, Pri = 2)]
        public int EOF_Handling_16()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string PrototypeOutput = "<root&NewLine  foo=\"fooval\"&NewLine  bar=\"barval\" />";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Params[0];
            wSettings.Indent = true;
            wSettings.NewLineChars = CurVariation.Params[1].ToString();
            wSettings.NewLineOnAttributes = true;

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("foo", "fooval");
            w.WriteAttributeString("bar", "barval");
            w.WriteEndElement();
            w.Dispose();

            VerifyOutput(PrototypeOutput.Replace("&NewLine", CurVariation.Params[1].ToString()));

            return TEST_PASS;
        }


        /*================== Other types of nodes ==================*/

        //[Variation(Desc = "Sanity tests for various types of nodes with 'Entitize'", Param = NewLineHandling.Entitize, id = 65, Pri = 0)]
        //[Variation(Desc = "Sanity tests for various types of nodes with 'Replace'", Param = NewLineHandling.Replace, id = 66, Pri = 0)]
        //[Variation(Desc = "Sanity tests for various types of nodes with 'None'", Param = NewLineHandling.None, id = 67, Pri = 0)]
        public int EOF_Handling_17()
        {
            if (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent)
                return TEST_SKIPPED;

            XmlWriterSettings wSettings = new XmlWriterSettings();
            string NewLines = "\r \n \r\n";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;

            XmlWriter w = CreateMemWriter(wSettings);
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
            if ((NewLineHandling)CurVariation.Param == NewLineHandling.Entitize)
                expOut = "<root><![CDATA[" + NewLines + "]]>" + ExpectedOutput(NewLines, NewLineHandling.Entitize, false) + "</root>" + "<?pi " + NewLines + "?>" + ExpectedOutput(NewLines, NewLineHandling.Entitize, false) + "<!--" + NewLines + "-->";
            else
                expOut = ExpectedOutput("<root><![CDATA[" + NewLines + "]]>" + NewLines + "</root><?pi " + NewLines + "?>" + NewLines + "<!--" + NewLines + "-->", (NewLineHandling)CurVariation.Param, false);

            VerifyOutput(expOut);

            return TEST_PASS;
        }

        //[Variation(Desc = "Custom NewLineChars inside CDATA & Comment when Indent=true; Entitize, '\\r\\n'", Params = new object[] { NewLineHandling.Entitize, "\r\n" }, id = 68, Pri = 1)]
        //[Variation(Desc = "Custom NewLineChars inside CDATA & Comment when Indent=true; Replace, '\\r\\n'", Params = new object[] { NewLineHandling.Replace, "\r\n" }, id = 69, Pri = 1)]
        //[Variation(Desc = "Custom NewLineChars inside CDATA & Comment when Indent=true; None, '\\r\\n'", Params = new object[] { NewLineHandling.None, "\r\n" }, id = 70, Pri = 1)]
        //[Variation(Desc = "Custom NewLineChars inside CDATA & Comment when Indent=true; Entitize, '\\r'", Params = new object[] { NewLineHandling.Entitize, "\r" }, id = 71, Pri = 2)]
        //[Variation(Desc = "Custom NewLineChars inside CDATA & Comment when Indent=true; Replace, '\\r'", Params = new object[] { NewLineHandling.Replace, "\r" }, id = 72, Pri = 2)]
        //[Variation(Desc = "Custom NewLineChars inside CDATA & Comment when Indent=true; None, '\\r'", Params = new object[] { NewLineHandling.None, "\r" }, id = 73, Pri = 2)]
        //[Variation(Desc = "Custom NewLineChars inside CDATA & Comment when Indent=true; Entitize, '---'", Params = new object[] { NewLineHandling.Entitize, "---" }, id = 74, Pri = 2)]
        //[Variation(Desc = "Custom NewLineChars inside CDATA & Comment when Indent=true; Replace, '---'", Params = new object[] { NewLineHandling.Replace, "---" }, id = 75, Pri = 2)]
        //[Variation(Desc = "Custom NewLineChars inside CDATA & Comment when Indent=true; None, '---'", Params = new object[] { NewLineHandling.None, "---" }, id = 76, Pri = 2)]
        public int EOF_Handling_18()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string PrototypeOutput = "<root><![CDATA[foo&NewLinefoo&NewLinefoo&NewLinefoo\tfoo]]></root>&NewLine<?pi foo&NewLinefoo&NewLinefoo&NewLinefoo\tfoo?>&NewLine<!--foo&NewLinefoo&NewLinefoo&NewLinefoo\tfoo-->";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Params[0];
            wSettings.Indent = true;
            wSettings.NewLineChars = CurVariation.Params[1].ToString();

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteCData("foo\r\nfoo\nfoo\rfoo\tfoo");
            w.WriteEndElement();
            w.WriteProcessingInstruction("pi", "foo\r\nfoo\nfoo\rfoo\tfoo");
            w.WriteComment("foo\r\nfoo\nfoo\rfoo\tfoo");
            w.Dispose();

            if ((NewLineHandling)CurVariation.Params[0] == NewLineHandling.Replace)
                VerifyOutput(PrototypeOutput.Replace("&NewLine", CurVariation.Params[1].ToString()));
            else
                VerifyOutput("<root><![CDATA[foo\r\nfoo\nfoo\rfoo\tfoo]]></root>&NewLine<?pi foo\r\nfoo\nfoo\rfoo\tfoo?>&NewLine<!--foo\r\nfoo\nfoo\rfoo\tfoo-->".Replace("&NewLine", CurVariation.Params[1].ToString()));

            return TEST_PASS;
        }


        //[Variation(Desc = "Wrapped writer tests for various types of nodes with 'Entitize'", Param = NewLineHandling.Entitize, Priority = 2)]
        //[Variation(Desc = "Wrapped writer tests for various types of nodes with 'Replace'", Param = NewLineHandling.Replace, Priority = 2)]
        //[Variation(Desc = "Wrapped writer tests for various types of nodes with 'None'", Param = NewLineHandling.None, Priority = 2)]
        public int EOF_Handling_19()
        {
            if (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent)
                CError.Skip("skipped");
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Param;
            wSettings.CheckCharacters = false;
            XmlWriter ww = CreateMemWriter(wSettings);
            XmlWriterSettings ws = wSettings.Clone();
            ws.NewLineHandling = NewLineHandling.Replace;
            ws.CheckCharacters = true;
            XmlWriter w = WriterHelper.Create(ww, ws);

            string NewLines = "\r \n " + nl;

            w.WriteStartElement("root");
            w.WriteCData(NewLines);
            w.WriteChars(NewLines.ToCharArray(), 0, NewLines.Length);
            w.WriteEndElement();
            w.WriteProcessingInstruction("pi", NewLines);
            w.WriteWhitespace(NewLines);
            w.WriteComment(NewLines);
            w.Dispose();

            string expOut;
            if ((NewLineHandling)CurVariation.Param == NewLineHandling.Entitize)
                expOut = "<root><![CDATA[" + NewLines + "]]>" + ExpectedOutput(NewLines, NewLineHandling.Entitize, false) + "</root>" + "<?pi " + NewLines + "?>" + ExpectedOutput(NewLines, NewLineHandling.Entitize, false) + "<!--" + NewLines + "-->";
            else
                expOut = ExpectedOutput("<root><![CDATA[" + NewLines + "]]>" + NewLines + "</root><?pi " + NewLines + "?>" + NewLines + "<!--" + NewLines + "-->", NewLineHandling.Replace, false);

            VerifyOutput(expOut);
            return TEST_PASS;
        }

        //[Variation(Desc = "XmlWriterSettings.IndentChars - valid values", Priority = 2, Param = 1)]
        //[Variation(Desc = "XmlWriterSettings.NewLineChars - valid values", Priority = 2, Param = 2)]
        //[Variation(Desc = "XmlWriterSettings.IndentChars - valid values", Priority = 2, Param = 3)]
        //[Variation(Desc = "XmlWriterSettings.NewLineChars - valid values", Priority = 2, Param = 4)]
        //[Variation(Desc = "XmlWriterSettings.IndentChars - valid values", Priority = 2, Param = 5)]
        //[Variation(Desc = "XmlWriterSettings.NewLineChars - valid values", Priority = 2, Param = 6)]
        //[Variation(Desc = "XmlWriterSettings.IndentChars - valid values", Priority = 2, Param = 7)]
        //[Variation(Desc = "XmlWriterSettings.NewLineChars - valid values", Priority = 2, Param = 8)]
        //[Variation(Desc = "XmlWriterSettings.IndentChars - valid values", Priority = 2, Param = 9)]
        //[Variation(Desc = "XmlWriterSettings.NewLineChars - valid values", Priority = 2, Param = 10)]
        //[Variation(Desc = "XmlWriterSettings.Indent - valid values", Priority = 2, Param = 11)]
        //[Variation(Desc = "XmlWriterSettings.Indent - valid values", Priority = 2, Param = 12)]
        public int EOF_Handling_20()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            switch ((int)CurVariation.Param)
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
            return TEST_PASS;
        }

        //[Variation(Desc = "10.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\uD800\uDC00', '\uD800\uDC00'", Params = new object[] { NewLineHandling.Entitize, "\uD800\uDC00", "\uD800\uDC00" })]
        //[Variation(Desc = "11.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\uD800\uDC00', '  '", Params = new object[] { NewLineHandling.Replace, "\uD800\uDC00", "\uD800\uDC00" })]
        //[Variation(Desc = "12.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\uD800\uDC00', '&lt;&gt;'", Params = new object[] { NewLineHandling.None, "\uD800\uDC00", "\uD800\uDC00" })]
        //[Variation(Desc = "13.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '&lt;&gt;', '&lt;&gt;'", Params = new object[] { NewLineHandling.Entitize, "&lt;&gt;", "&lt;&gt;" })]
        //[Variation(Desc = "14.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '&lt;&gt;', '&lt;&gt;'", Params = new object[] { NewLineHandling.Replace, "&lt;&gt;", "&lt;&gt;" })]
        //[Variation(Desc = "15.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '&lt;&gt;', '&lt;&gt;'", Params = new object[] { NewLineHandling.None, "&lt;&gt;", "&lt;&gt;" })]
        public int EOF_Handling_21()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            string PrototypeOutput = "<root>&NewLine&Indent<foo>&NewLine&Indent&Indent<bar />&NewLine&Indent</foo>&NewLine</root>";

            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Params[0];
            wSettings.Indent = true;
            wSettings.NewLineChars = CurVariation.Params[1].ToString();
            wSettings.IndentChars = CurVariation.Params[2].ToString();

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteStartElement("foo");
            w.WriteElementString("bar", "");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            VerifyOutput(PrototypeOutput.Replace("&NewLine", CurVariation.Params[1].ToString()).Replace("&Indent", CurVariation.Params[2].ToString()));
            return TEST_PASS;
        }

        //[Variation(Desc = "16.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\uDE40\uDA72', '\uDE40\uDA72'", Params = new object[] { NewLineHandling.Entitize })]
        //[Variation(Desc = "17.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\uDE40\uDA72', '\uDE40\uDA72'", Params = new object[] { NewLineHandling.Replace })]
        //[Variation(Desc = "18.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\uDE40\uDA72', '\uDE40\uDA72'", Params = new object[] { NewLineHandling.None })]
        public int EOF_Handling_22()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NewLineHandling = (NewLineHandling)CurVariation.Params[0];
            wSettings.Indent = true;
            wSettings.NewLineChars = "\uDE40\uDA72";
            wSettings.IndentChars = "\uDE40\uDA72";

            XmlWriter w = CreateMemWriter(wSettings);
            try
            {
                w.WriteStartElement("root");
                w.WriteStartElement("foo");
                w.Dispose();
            }
            catch (ArgumentException e) { CError.WriteLine(e.Message); return TEST_PASS; }
            return TEST_FAIL;
        }
    }
}

