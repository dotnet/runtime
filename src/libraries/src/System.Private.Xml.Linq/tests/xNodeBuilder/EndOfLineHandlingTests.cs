// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.IO;
using System.Xml.XmlDiff;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeBuilderFunctionalTests : TestModule
    {
        public partial class XNodeBuilderTests : XLinqTestCase
        {
            //[TestCase(Name = "NewLineHandling", Param = "XNodeBuilder")]
            public partial class TCEOFHandling : BridgeHelpers
            {
                private XmlDiff _diff = null;
                public TCEOFHandling()
                {
                    _diff = new XmlDiff();
                }

                private string ExpectedOutput(string input, NewLineHandling h, bool attr)
                {
                    string output = new string(input.ToCharArray());
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
                                output = output.Replace("\n", "\r\n");
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

                //[Variation(Desc = "NewLineHandling Default value - NewLineHandling.Replace", Id = 1, Priority = 0)]
                public void EOF_Handling_01()
                {
                    XmlWriterSettings wSettings = new XmlWriterSettings();
                    TestLog.Compare(wSettings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value for XmlWriterSettings.NewLineHandling");
                    XDocument d = new XDocument();
                    XmlWriter w = CreateWriter(d);
                    w.Dispose();
                    TestLog.Compare(w.Settings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value for XmlWriter.Settings.NewLineHandling");
                }

                //[Variation(Desc = "XmlWriter creation with NewLineHandling.Entitize", Param = NewLineHandling.Entitize, Id = 2, Priority = 0)]
                //[Variation(Desc = "XmlWriter creation with NewLineHandling.Replace", Param = NewLineHandling.Replace, Id = 3, Priority = 0)]
                //[Variation(Desc = "XmlWriter creation with NewLineHandling.None", Param = NewLineHandling.None, Id = 4, Priority = 0)]
                public void EOF_Handling_02()
                {
                    XmlWriterSettings wSettings = new XmlWriterSettings();
                    XDocument d = new XDocument();
                    XmlWriter w = CreateWriter(d);
                    TestLog.Compare(w != null, "XmlWriter creation failed");
                    TestLog.Compare(w.Settings.NewLineHandling, NewLineHandling.Replace, "Invalid NewLineHandling assignment");
                    w.Dispose();
                }

                //[Variation(Desc = "Check for tab character in element with 'Entitize'", Param = NewLineHandling.Entitize, Id = 14, Priority = 0)]
                //[Variation(Desc = "Check for tab character in element with 'Replace'", Param = NewLineHandling.Replace, Id = 15, Priority = 0)]
                //[Variation(Desc = "Check for tab character in element with 'None'", Param = NewLineHandling.None, Id = 16, Priority = 0)]
                public void EOF_Handling_06()
                {
                    string Tabs = "foo\tbar&#x9;foo\n\tbar\t\n\t";
                    XDocument d = new XDocument();
                    XmlWriter w = CreateWriter(d);
                    w.WriteStartElement("root");

                    w.WriteString("foo\tbar");
                    w.WriteCharEntity('\t');
                    w.WriteString("foo\n\tbar\t\n\t");

                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, "<root>" + ExpectedOutput(Tabs, (NewLineHandling)Variation.Param, false) + "</root>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "Check for combinations of NewLine characters in attribute with 'Entitize'", Param = NewLineHandling.Entitize, Id = 17, Priority = 0)]
                //[Variation(Desc = "Check for combinations of NewLine characters in attribute with 'Replace'", Param = NewLineHandling.Replace, Id = 18, Priority = 0)]
                //[Variation(Desc = "Check for combinations of NewLine characters in attribute with 'None'", Param = NewLineHandling.None, Id = 19, Priority = 0)]
                public void EOF_Handling_07()
                {
                    string NewLineCombinations = "\r \n \r\n \n\r \r\r \n\n \r\n\r \n\r\n";

                    XDocument d = new XDocument();
                    XmlWriter w = CreateWriter(d);
                    w.WriteStartElement("root");
                    w.WriteAttributeString("foo", NewLineCombinations);
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, "<root foo=\"" + ExpectedOutput(NewLineCombinations, (NewLineHandling)Variation.Param, true) + "\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "Check for combinations of entities in attribute with 'Entitize'", Param = NewLineHandling.Entitize, Id = 20, Priority = 0)]
                //[Variation(Desc = "Check for combinations of entities in attribute with 'Replace'", Param = NewLineHandling.Replace, Id = 21, Priority = 0)]
                //[Variation(Desc = "Check for combinations of entities in attribute with 'None'", Param = NewLineHandling.None, Id = 22, Priority = 0)]
                public void EOF_Handling_08()
                {
                    string NewLineCombinations = "\r \n \r\n \n\r \r\r \n\n \r\n\r \n\r\n";
                    string NewLineEntities = "&#xD; &#xA; &#xD;&#xA; &#xA;&#xD; &#xD;&#xD; &#xA;&#xA; &#xD;&#xA;&#xD; &#xA;&#xD;&#xA;";

                    XDocument d = new XDocument();
                    XmlWriter w = CreateWriter(d);
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
                    if (!CompareReader(d, "<root foo=\"" + ExpectedOutput(NewLineEntities, (NewLineHandling)Variation.Param, true) + "\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "Check for combinations of NewLine characters and entities in element with 'Entitize'", Param = NewLineHandling.Entitize, Id = 23, Priority = 0)]
                //[Variation(Desc = "Check for combinations of NewLine characters and entities in element with 'Replace'", Param = NewLineHandling.Replace, Id = 24, Priority = 0)]
                //[Variation(Desc = "Check for combinations of NewLine characters and entities in element with 'None'", Param = NewLineHandling.None, Id = 25, Priority = 0)]
                public void EOF_Handling_09()
                {
                    string NewLines = "\r&#xA; &#xD;\n &#xD;\r &#xA;\n \n&#xD; &#xA;\r";

                    XDocument d = new XDocument();
                    XmlWriter w = CreateWriter(d);
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
                    if (!CompareReader(d, "<root foo=\"" + ExpectedOutput(NewLines, (NewLineHandling)Variation.Param, true) + "\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "Check for tab character in attribute with 'Entitize'", Param = NewLineHandling.Entitize, Id = 26, Priority = 0)]
                //[Variation(Desc = "Check for tab character in attribute with 'Replace'", Param = NewLineHandling.Replace, Id = 27, Priority = 0)]
                //[Variation(Desc = "Check for tab character in attribute with 'None'", Param = NewLineHandling.None, Id = 28, Priority = 0)]
                public void EOF_Handling_10()
                {
                    string Tabs = "foo\tbar&#x9;foo\n\tbar\t\n\t";
                    XDocument d = new XDocument();
                    XmlWriter w = CreateWriter(d);
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("foo");

                    w.WriteString("foo\tbar");
                    w.WriteCharEntity('\t');
                    w.WriteString("foo\n\tbar\t\n\t");

                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, "<root foo=\"" + ExpectedOutput(Tabs, (NewLineHandling)Variation.Param, true) + "\" />"))
                        throw new TestException(TestResult.Failed, "");
                }


                /*================== NewLineChars, IndentChars ==================*/

                //[Variation(Desc = "NewLineChars and IndentChars Default values and test for proper indentation, Entitize", Param = NewLineHandling.Entitize, Id = 29, Priority = 1)]
                //[Variation(Desc = "NewLineChars and IndentChars Default values and test for proper indentation, Replace", Param = NewLineHandling.Replace, Id = 30, Priority = 1)]
                //[Variation(Desc = "NewLineChars and IndentChars Default values and test for proper indentation, None", Param = NewLineHandling.None, Id = 31, Priority = 1)]
                public void EOF_Handling_11()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = CreateWriter(d);
                    TestLog.Compare(w.Settings.NewLineChars, Environment.NewLine, "Incorrect default value for XmlWriter.Settings.NewLineChars");
                    TestLog.Compare(w.Settings.IndentChars, "  ", "Incorrect default value for XmlWriter.Settings.IndentChars");

                    w.WriteStartElement("root");
                    w.WriteStartElement("foo");
                    w.WriteElementString("bar", "");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, "<root>\r\n  <foo>\r\n    <bar />\r\n  </foo>\r\n</root>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\\r', '  '", Params = new object[] { NewLineHandling.Entitize, "\r", "  " }, Id = 32, Priority = 2)]
                //[Variation(Desc = "Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\\r', '  '", Params = new object[] { NewLineHandling.Replace, "\r", "  " }, Id = 33, Priority = 2)]
                //[Variation(Desc = "Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\\r', '  '", Params = new object[] { NewLineHandling.None, "\r", "  " }, Id = 34, Priority = 2)]
                //[Variation(Desc = "Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '&#xA;', '  '", Params = new object[] { NewLineHandling.Entitize, "&#xA;", "  " }, Id = 35, Priority = 2)]
                //[Variation(Desc = "Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '&#xA;', '  '", Params = new object[] { NewLineHandling.Replace, "&#xA;", "  " }, Id = 36, Priority = 2)]
                //[Variation(Desc = "Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '&#xA;', '  '", Params = new object[] { NewLineHandling.None, "&#xA;", "  " }, Id = 37, Priority = 2)]
                //[Variation(Desc = "Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\\r', '\\n'", Params = new object[] { NewLineHandling.Entitize, "\r", "\n" }, Id = 38, Priority = 2)]
                //[Variation(Desc = "Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\\r', '\\n'", Params = new object[] { NewLineHandling.Replace, "\r", "\n" }, Id = 39, Priority = 2)]
                //[Variation(Desc = "Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\\r', '\\n'", Params = new object[] { NewLineHandling.None, "\r", "\n" }, Id = 40, Priority = 2)]
                public void EOF_Handling_13()
                {
                    string PrototypeOutput = "<root>&NewLine&Indent<foo>&NewLine&Indent&Indent<bar />&NewLine&Indent</foo>&NewLine</root>";

                    XDocument d = new XDocument();
                    XmlWriter w = CreateWriter(d);
                    w.WriteStartElement("root");
                    w.WriteStartElement("foo");
                    w.WriteElementString("bar", "");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, PrototypeOutput.Replace("&NewLine", Variation.Params[1].ToString()).Replace("&Indent", Variation.Params[2].ToString())))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "NewLine handling in attribute when Indent=true; Entitize, '\\r\\n'", Params = new object[] { NewLineHandling.Entitize, "\r\n" }, Id = 50, Priority = 1)]
                //[Variation(Desc = "NewLine handling in attribute when Indent=true; Replace, '\\r\\n'", Params = new object[] { NewLineHandling.Replace, "\r\n" }, Id = 51, Priority = 1)]
                //[Variation(Desc = "NewLine handling in attribute when Indent=true; None, '\\r\\n'", Params = new object[] { NewLineHandling.None, "\r\n" }, Id = 52, Priority = 1)]
                //[Variation(Desc = "NewLine handling in attribute when Indent=true; Entitize, '\\r'", Params = new object[] { NewLineHandling.Entitize, "\r" }, Id = 53, Priority = 2)]
                //[Variation(Desc = "NewLine handling in attribute when Indent=true; Replace, '\\r'", Params = new object[] { NewLineHandling.Replace, "\r" }, Id = 54, Priority = 2)]
                //[Variation(Desc = "NewLine handling in attribute when Indent=true; None, '\\r'", Params = new object[] { NewLineHandling.None, "\r" }, Id = 54, Priority = 2)]
                //[Variation(Desc = "NewLine handling in attribute when Indent=true; Entitize, '---'", Params = new object[] { NewLineHandling.Entitize, "---" }, Id = 54, Priority = 2)]
                //[Variation(Desc = "NewLine handling in attribute when Indent=true; Replace, '---'", Params = new object[] { NewLineHandling.Replace, "---" }, Id = 55, Priority = 2)]
                //[Variation(Desc = "NewLine handling in attribute when Indent=true; None, '---'", Params = new object[] { NewLineHandling.None, "---" })]
                public void EOF_Handling_15()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = CreateWriter(d);
                    w.WriteStartElement("root");
                    w.WriteAttributeString("foo", "foo\r\nfoo\nfoo\rfoo\tfoo");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, "<root foo=\"" + ExpectedOutput("foo\r\nfoo\nfoo\rfoo\tfoo", (NewLineHandling)Variation.Params[0], true) + "\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; Entitize, '\\r\\n'", Params = new object[] { NewLineHandling.Entitize, "\r\n" }, Id = 56, Priority = 1)]
                //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; Replace, '\\r\\n'", Params = new object[] { NewLineHandling.Replace, "\r\n" }, Id = 57, Priority = 1)]
                //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; None, '\\r\\n'", Params = new object[] { NewLineHandling.None, "\r\n" }, Id = 58, Priority = 1)]
                //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; Entitize, '\\r'", Params = new object[] { NewLineHandling.Entitize, "\r" }, Id = 59, Priority = 2)]
                //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; Replace, '\\r'", Params = new object[] { NewLineHandling.Replace, "\r" }, Id = 60, Priority = 2)]
                //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; None, '\\r'", Params = new object[] { NewLineHandling.None, "\r" }, Id = 61, Priority = 2)]
                //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; Entitize, '---'", Params = new object[] { NewLineHandling.Entitize, "---" }, Id = 62, Priority = 2)]
                //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; Replace, '---'", Params = new object[] { NewLineHandling.Replace, "---" }, Id = 63, Priority = 2)]
                //[Variation(Desc = "NewLine handling between attributes when NewLineOnAttributes=true; None, '---'", Params = new object[] { NewLineHandling.None, "---" }, Id = 64, Priority = 2)]
                public void EOF_Handling_16()
                {
                    string PrototypeOutput = "<root&NewLine  foo=\"fooval\"&NewLine  bar=\"barval\" />";

                    XDocument d = new XDocument();
                    XmlWriter w = CreateWriter(d);
                    w.WriteStartElement("root");
                    w.WriteAttributeString("foo", "fooval");
                    w.WriteAttributeString("bar", "barval");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, PrototypeOutput.Replace("&NewLine", Variation.Params[1].ToString())))
                        throw new TestException(TestResult.Failed, "");
                }
            }
        }
    }
}
