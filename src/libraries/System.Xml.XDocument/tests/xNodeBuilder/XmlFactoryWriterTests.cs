// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
//using System.Xml.XPath;
using System.Text;
using System.IO;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class XNodeBuilderTests : XLinqTestCase
        {
            public partial class TCCheckChars : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "CheckChars=true, invalid XML test WriteEntityRef", Priority = 1, Param = "EntityRef")]
                //[Variation(Id = 2, Desc = "CheckChars=true, invalid XML test WriteSurrogateCharEntity", Priority = 1, Param = "SurrogateCharEntity")]
                //[Variation(Id = 3, Desc = "CheckChars=true, invalid XML test WriteWhitespace", Priority = 1, Param = "Whitespace")]
                //[Variation(Id = 4, Desc = "CheckChars=true, invalid name chars WriteDocType(name)", Priority = 1, Param = "WriteDocTypeName")]
                public void checkChars_1()
                {
                    char[] invalidXML = { '\u0000', '\u0008', '\u000B', '\u000C', '\u000E', '\u001F', '\uFFFE', '\uFFFF' };

                    XElement d = new XElement("a");
                    XmlWriter w = d.CreateWriter();

                    try
                    {
                        switch (Variation.Param.ToString())
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
                                TestLog.Compare(false, "Invalid param value");
                                break;
                        }
                    }
                    catch (XmlException)
                    {
                        TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                    catch (ArgumentException)
                    {
                        TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                    finally
                    {
                        w.Dispose();
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 40, Desc = "CheckChars=false, invalid XML characters in WriteWhitespace should error", Priority = 1, Params = new object[] { "Whitespace", false })]
                //[Variation(Id = 41, Desc = "CheckChars=false, invalid XML characters in WriteSurrogateCharEntity should error", Priority = 1, Params = new object[] { "Surrogate", false })]
                //[Variation(Id = 42, Desc = "CheckChars=false, invalid XML characters in WriteEntityRef should error", Priority = 1, Params = new object[] { "EntityRef", false })]
                //[Variation(Id = 43, Desc = "CheckChars=false, invalid XML characters in WriteQualifiedName should error", Priority = 1, Params = new object[] { "QName", false })]
                //[Variation(Id = 44, Desc = "CheckChars=true, invalid XML characters in WriteWhitespace should error", Priority = 1, Params = new object[] { "Whitespace", true })]
                //[Variation(Id = 45, Desc = "CheckChars=true, invalid XML characters in WriteSurrogateCharEntity should error", Priority = 1, Params = new object[] { "Surrogate", true })]
                //[Variation(Id = 46, Desc = "CheckChars=true, invalid XML characters in WriteEntityRef should error", Priority = 1, Params = new object[] { "EntityRef", true })]
                //[Variation(Id = 47, Desc = "CheckChars=true, invalid XML characters in WriteQualifiedName should error", Priority = 1, Params = new object[] { "QName", true })]
                public void checkChars_4()
                {
                    char[] invalidXML = { '\u0000', '\u0008', '\u000B', '\u000C', '\u000E', '\u001F', '\uFFFE', '\uFFFF' };
                    XElement d = new XElement("a");
                    XmlWriter w = d.CreateWriter();
                    try
                    {
                        w.WriteStartElement("Root");
                        switch (Variation.Params[0].ToString())
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
                                TestLog.Compare(false, "Invalid param value");
                                break;
                        }
                    }
                    catch (ArgumentException)
                    {
                        TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                    finally
                    {
                        w.Dispose();
                    }
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCNewLineHandling : BridgeHelpers
            {
                //[Variation(Id = 7, Desc = "Test for CR (xD) inside attr when NewLineHandling = Replace", Priority = 0)]
                public void NewLineHandling_7()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("root");
                    w.WriteAttributeString("attr", "\r");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<root attr=\"&#xD;\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "Test for LF (xA) inside attr when NewLineHandling = Replace", Priority = 0)]
                public void NewLineHandling_8()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("root");
                    w.WriteAttributeString("attr", "\n");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<root attr=\"&#xA;\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 9, Desc = "Test for CR LF (xD xA) inside attr when NewLineHandling = Replace", Priority = 0)]
                public void NewLineHandling_9()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("root");
                    w.WriteAttributeString("attr", "\r\n");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<root attr=\"&#xD;&#xA;\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 10, Desc = "Test for CR (xD) inside attr when NewLineHandling = Entitize", Priority = 0)]
                public void NewLineHandling_10()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("root");
                    w.WriteAttributeString("attr", "\r");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<root attr=\"&#xD;\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 11, Desc = "Test for LF (xA) inside attr when NewLineHandling = Entitize", Priority = 0)]
                public void NewLineHandling_11()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("root");
                    w.WriteAttributeString("attr", "\n");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<root attr=\"&#xA;\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 12, Desc = "Test for CR LF (xD xA) inside attr when NewLineHandling = Entitize", Priority = 0)]
                public void NewLineHandling_12()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("root");
                    w.WriteAttributeString("attr", "\r\n");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<root attr=\"&#xD;&#xA;\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 13, Desc = "Factory-created writers do not entitize 0xD character in text content when NewLineHandling=Entitize")]
                public void NewLineHandling_13()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("a");
                    w.WriteString("A \r\n \r \n B");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<a>A &#xD;\xA &#xD; \xA B</a>"))
                        throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCIndent : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Simple test when false", Priority = 0)]
                public void indent_1()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    TestLog.Compare(w.Settings.Indent, false, "Mismatch in Indent");
                    w.WriteStartElement("Root");
                    w.WriteStartElement("child");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<Root><child /></Root>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Simple test when true", Priority = 0)]
                public void indent_2()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("Root");
                    w.WriteStartElement("child");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<Root>\xD\xA  <child />\xD\xA</Root>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Indent = false, element content is empty", Priority = 0)]
                public void indent_3()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("Root");
                    w.WriteString("");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<Root></Root>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "Indent = false, element content is empty, FullEndElement", Priority = 0)]
                public void indent_5()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("Root");
                    w.WriteString("");
                    w.WriteFullEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<Root></Root>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "Indent = true, element content is empty, FullEndElement", Priority = 0)]
                public void indent_6()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("Root");
                    w.WriteString("");
                    w.WriteFullEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<Root></Root>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 7, Desc = "Indent = true, mixed content", Priority = 0)]
                public void indent_7()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("Root");
                    w.WriteString("test");
                    w.WriteStartElement("child");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<Root>test<child /></Root>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "Indent = true, mixed content, FullEndElement", Priority = 0)]
                public void indent_8()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("Root");
                    w.WriteString("test");
                    w.WriteStartElement("child");
                    w.WriteFullEndElement();
                    w.WriteFullEndElement();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<Root>test<child></child></Root>"))
                        throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCNewLineOnAttributes : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Make sure the setting has no effect when Indent is false", Priority = 0)]
                public void NewLineOnAttributes_1()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    TestLog.Compare(w.Settings.NewLineOnAttributes, false, "Mismatch in NewLineOnAttributes");

                    w.WriteStartElement("root");
                    w.WriteAttributeString("attr1", "value1");
                    w.WriteAttributeString("attr2", "value2");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(d.CreateReader(), "<root attr1=\"value1\" attr2=\"value2\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Attributes of nested elements", Priority = 1)]
                public void NewLineOnAttributes_3()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("level1");
                    w.WriteAttributeString("attr1", "value1");
                    w.WriteAttributeString("attr2", "value2");
                    w.WriteStartElement("level2");
                    w.WriteAttributeString("attr1", "value1");
                    w.WriteAttributeString("attr2", "value2");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareBaseline(d, "NewLineOnAttributes3.txt"))
                        throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCStandAlone : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "StartDocument(bool standalone = true)", Priority = 0)]
                public void standalone_1()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartDocument(true);
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteEndDocument();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<?xml version=\"1.0\" encoding=\"utf8\" standalone=\"yes\"?><Root />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "StartDocument(bool standalone = false)", Priority = 0)]
                public void standalone_2()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartDocument(false);
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteEndDocument();
                    w.Dispose();
                    if (!CompareReader(d.CreateReader(), "<?xml version=\"1.0\" encoding=\"utf8\" standalone=\"no\"?><Root />"))
                        throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCFragmentCL : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "WriteDocType should error when CL=fragment", Priority = 1)]
                public void frag_1()
                {
                    XElement d = new XElement("a");
                    using (XmlWriter w = d.CreateWriter())
                    {
                        try
                        {
                            w.WriteDocType("ROOT", "publicid", "sysid", "<!ENTITY e 'abc'>");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "WriteStartDocument() should error when CL=fragment", Priority = 1)]
                public void frag_2()
                {
                    XElement d = new XElement("a");
                    using (XmlWriter w = d.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCAutoCL : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Change to CL Document after WriteStartDocument()", Priority = 0)]
                public void auto_1()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();

                    w.WriteStartDocument();

                    // PROLOG
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
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
                    TestLog.WriteLine("Conformance level = Document did not take effect");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Change to CL Document after WriteStartDocument(standalone = true)", Priority = 0, Param = "true")]
                //[Variation(Id = 3, Desc = "Change to CL Document after WriteStartDocument(standalone = false)", Priority = 0, Param = "false")]
                public void auto_2()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();

                    switch (Variation.Param.ToString())
                    {
                        case "true":
                            w.WriteStartDocument(true);
                            break;
                        case "false":
                            w.WriteStartDocument(false);
                            break;
                    }
                    // PROLOG
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
                    w.WriteStartElement("root");
                    // ELEMENT CONTENT
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
                    // Inside Attribute
                    w.WriteStartAttribute("attr");
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
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
                    TestLog.WriteLine("Conformance level = Document did not take effect");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "Change to CL Document when you write DocType decl", Priority = 0)]
                public void auto_3()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteDocType("ROOT", "publicid", "sysid", "<!ENTITY e 'abc'>");
                    // PROLOG
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Should switch to Document from Auto when you write top level DTD");
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
                    TestLog.WriteLine("Conformance level = Document did not take effect");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "Change to CL Fragment when you write a root element", Priority = 1)]
                public void auto_4()
                {
                    XElement d = new XElement("a");
                    XmlWriter w = d.CreateWriter();
                    w.WriteStartElement("root");
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
                    w.WriteEndElement();
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
                    w.WriteStartElement("root");
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 6, Desc = "Change to CL Fragment for WriteString at top level", Priority = 1, Param = "String")]
                //[Variation(Id = 7, Desc = "Change to CL Fragment for WriteCData at top level", Priority = 1, Param = "CData")]
                //[Variation(Id = 9, Desc = "Change to CL Fragment for WriteCharEntity at top level", Priority = 1, Param = "CharEntity")]
                //[Variation(Id = 10, Desc = "Change to CL Fragment for WriteSurrogateCharEntity at top level", Priority = 1, Param = "SurrogateCharEntity")]
                //[Variation(Id = 11, Desc = "Change to CL Fragment for WriteChars at top level", Priority = 1, Param = "Chars")]
                //[Variation(Id = 12, Desc = "Change to CL Fragment for WriteRaw at top level", Priority = 1, Param = "Raw")]
                //[Variation(Id = 14, Desc = "Change to CL Fragment for WriteBinHex at top level", Priority = 1, Param = "BinHex")]
                public void auto_5()
                {
                    XElement d = new XElement("a");
                    XmlWriter w = d.CreateWriter();
                    byte[] buffer = new byte[10];

                    switch (Variation.Param.ToString())
                    {
                        case "String":
                            w.WriteString("text");
                            break;
                        case "CData":
                            w.WriteCData("cdata text");
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
                        default:
                            TestLog.Compare(false, "Invalid param in testcase");
                            break;
                    }
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
                    w.Dispose();
                }

                //[Variation(Id = 15, Desc = "WritePI at top level, followed by DTD, expected CL = Document", Priority = 2, Param = "PI")]
                //[Variation(Id = 16, Desc = "WriteComment at top level, followed by DTD, expected CL = Document", Priority = 2, Param = "Comment")]
                //[Variation(Id = 17, Desc = "WriteWhitespace at top level, followed by DTD, expected CL = Document", Priority = 2, Param = "WS")]
                public void auto_6()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    switch (Variation.Param.ToString())
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
                            TestLog.Compare(false, "Invalid param in testcase");
                            break;
                    }

                    w.WriteDocType("ROOT", "publicid", "sysid", "<!ENTITY e 'abc'>");
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
                    w.Dispose();
                }

                //[Variation(Id = 18, Desc = "WritePI at top level, followed by text, expected CL = Fragment", Priority = 2, Param = "PI")]
                //[Variation(Id = 19, Desc = "WriteComment at top level, followed by text, expected CL = Fragment", Priority = 2, Param = "Comment")]
                //[Variation(Id = 20, Desc = "WriteWhitespace at top level, followed by text, expected CL = Fragment", Priority = 2, Param = "WS")]
                public void auto_7()
                {
                    XElement d = new XElement("a");
                    XmlWriter w = d.CreateWriter();
                    w.WriteProcessingInstruction("pi", "text");
                    w.WriteString("text");
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
                    w.Dispose();
                }

                //[Variation(Id = 21, Desc = "WriteNode(XmlReader) when reader positioned on DocType node, expected CL = Document", Priority = 2)]
                public void auto_8()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();

                    string strxml = "<!DOCTYPE test [<!ENTITY e 'abc'>]><Root />";
                    XmlReader xr = GetReaderStr(strxml);

                    xr.Read();
                    w.WriteNode(xr, false);
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Error");
                    w.Dispose();
                    xr.Dispose();
                }

                //[Variation(Id = 22, Desc = "WriteNode(XmlReader) when reader positioned on text node, expected CL = Fragment", Priority = 2)]
                public void auto_10()
                {
                    XElement d = new XElement("a");
                    XmlWriter w = d.CreateWriter();

                    string strxml = "<Root>text</Root>";
                    XmlReader xr = GetReaderStr(strxml);

                    xr.Read(); xr.Read();
                    TestLog.Compare(xr.NodeType.ToString(), "Text", "Error");
                    w.WriteNode(xr, false);
                    TestLog.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Error");
                    w.Dispose();
                    xr.Dispose();
                }
            }
        }
    }
}
