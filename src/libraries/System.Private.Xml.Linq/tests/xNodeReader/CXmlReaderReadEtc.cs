// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeReaderFunctionalTests : TestModule
    {
        public partial class XNodeReaderTests : XLinqTestCase
        {
            //[TestCase(Name = "ReadState", Desc = "ReadState")]
            public partial class TCReadState : BridgeHelpers
            {
                //[Variation("XmlReader ReadState Initial", Priority = 0)]
                public void ReadState1()
                {
                    XDocument doc = new XDocument();
                    XmlReader r = doc.CreateReader();
                    if (r.ReadState != ReadState.Initial)
                        throw new TestFailedException("");
                }

                //[Variation("XmlReader ReadState Interactive", Priority = 0)]
                public void ReadState2()
                {
                    XDocument doc = XDocument.Parse("<a/>");
                    XmlReader r = doc.CreateReader();
                    while (r.Read())
                    {
                        if (r.ReadState != ReadState.Interactive)
                            throw new TestFailedException("");
                        else
                            return;
                    }
                    if (r.ReadState != ReadState.EndOfFile)
                        throw new TestFailedException("");
                }

                //[Variation("XmlReader ReadState EndOfFile", Priority = 0)]
                public void ReadState3()
                {
                    XDocument doc = new XDocument();
                    XmlReader r = doc.CreateReader();
                    while (r.Read()) { };
                    if (r.ReadState != ReadState.EndOfFile)
                        throw new TestFailedException("");
                }

                //[Variation("XmlReader ReadState Initial", Priority = 0)]
                public void ReadState4()
                {
                    XDocument doc = XDocument.Parse("<a/>");
                    XmlReader r = doc.CreateReader();
                    try
                    {
                        r.ReadContentAsInt();
                    }
                    catch (InvalidOperationException) { }
                    if (r.ReadState != ReadState.Initial)
                        throw new TestFailedException("");
                }

                //[Variation("XmlReader ReadState EndOfFile", Priority = 0)]
                public void ReadState5()
                {
                    XDocument doc = XDocument.Parse("<a/>");
                    XmlReader r = doc.CreateReader();
                    while (r.Read()) { };
                    try
                    {
                        r.ReadContentAsInt();
                    }
                    catch (InvalidOperationException) { }
                    if (r.ReadState != ReadState.EndOfFile)
                        throw new TestFailedException("");
                }
            }

            //[TestCase(Name = "ReadInnerXml", Desc = "ReadInnerXml")]
            public partial class TCReadInnerXml : BridgeHelpers
            {
                void VerifyNextNode(XmlReader DataReader, XmlNodeType nt, string name, string value)
                {
                    while (DataReader.NodeType == XmlNodeType.Whitespace ||
                            DataReader.NodeType == XmlNodeType.SignificantWhitespace)
                    {
                        // skip all whitespace nodes
                        // if EOF is reached NodeType=None
                        DataReader.Read();
                    }

                    TestLog.Compare(VerifyNode(DataReader, nt, name, value), "VerifyNextNode");
                }

                //[Variation("ReadInnerXml on Empty Tag", Priority = 0)]
                public void TestReadInnerXml1()
                {
                    bool bPassed = false;
                    string strExpected = string.Empty;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY1");
                    bPassed = TestLog.Equals(DataReader.ReadInnerXml(), strExpected, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("ReadInnerXml on non Empty Tag", Priority = 0)]
                public void TestReadInnerXml2()
                {
                    bool bPassed = false;
                    string strExpected = string.Empty;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY2");
                    bPassed = TestLog.Equals(DataReader.ReadInnerXml(), strExpected, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("ReadInnerXml on non Empty Tag with text content", Priority = 0)]
                public void TestReadInnerXml3()
                {
                    bool bPassed = false;
                    string strExpected = "ABCDE";

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONEMPTY1");
                    bPassed = TestLog.Equals(DataReader.ReadInnerXml(), strExpected, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("ReadInnerXml with multiple Level of elements")]
                public void TestReadInnerXml6()
                {
                    bool bPassed = false;
                    string strExpected;
                    strExpected = "<ELEM1 /><ELEM2>xxx yyy</ELEM2><ELEM3 />";

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "SKIP3");
                    bPassed = TestLog.Equals(DataReader.ReadInnerXml(), strExpected, Variation.Desc);
                    VerifyNextNode(DataReader, XmlNodeType.Element, "AFTERSKIP3", string.Empty);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("ReadInnerXml with multiple Level of elements, text and attributes", Priority = 0)]
                public void TestReadInnerXml7()
                {
                    bool bPassed = false;
                    string strExpected = "<e1 a1='a1value' a2='a2value'><e2 a1='a1value' a2='a2value'><e3 a1='a1value' a2='a2value'>leave</e3></e2></e1>";
                    strExpected = strExpected.Replace('\'', '"');
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "CONTENT");

                    bPassed = TestLog.Equals(DataReader.ReadInnerXml(), strExpected, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("ReadInnerXml with entity references, EntityHandling = ExpandEntities")]
                public void TestReadInnerXml8()
                {
                    bool bPassed = false;
                    string strExpected = ST_EXPAND_ENTITIES2;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_ENTTEST_NAME);

                    bPassed = TestLog.Equals(DataReader.ReadInnerXml(), strExpected, Variation.Desc);
                    VerifyNextNode(DataReader, XmlNodeType.Element, "ENTITY2", string.Empty);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("ReadInnerXml on attribute node", Priority = 0)]
                public void TestReadInnerXml9()
                {
                    bool bPassed = false;
                    string strExpected = "a1value";

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ATTRIBUTE2");
                    bPassed = DataReader.MoveToFirstAttribute();
                    bPassed = TestLog.Equals(DataReader.ReadInnerXml(), strExpected, Variation.Desc);
                    VerifyNextNode(DataReader, XmlNodeType.Attribute, "a1", strExpected);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("ReadInnerXml on attribute node with entity reference in value", Priority = 0)]
                public void TestReadInnerXml10()
                {
                    bool bPassed = false;
                    string strExpected = ST_ENT1_ATT_EXPAND_CHAR_ENTITIES4;
                    string strExpectedAttValue = ST_ENT1_ATT_EXPAND_ENTITIES;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_ENTTEST_NAME);
                    bPassed = DataReader.MoveToFirstAttribute();
                    bPassed = TestLog.Equals(DataReader.ReadInnerXml(), strExpected, Variation.Desc);
                    VerifyNextNode(DataReader, XmlNodeType.Attribute, "att1", strExpectedAttValue);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("ReadInnerXml on Text", Priority = 0)]
                public void TestReadInnerXml11()
                {
                    XmlNodeType nt;
                    string name;
                    string value;

                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Text);
                    TestLog.Compare(DataReader.ReadInnerXml(), string.Empty, Variation.Desc);

                    // save status and compare with Read
                    nt = DataReader.NodeType;
                    name = DataReader.Name;
                    value = DataReader.Value;

                    DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Text);
                    DataReader.Read();
                    TestLog.Compare(VerifyNode(DataReader, nt, name, value), "vn");
                }

                //[Variation("ReadInnerXml on CDATA")]
                public void TestReadInnerXml12()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.CDATA))
                    {
                        TestLog.Compare(DataReader.ReadInnerXml(), string.Empty, Variation.Desc);
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadInnerXml on ProcessingInstruction")]
                public void TestReadInnerXml13()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.ProcessingInstruction))
                    {
                        TestLog.Compare(DataReader.ReadInnerXml(), string.Empty, Variation.Desc);
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadInnerXml on Comment")]
                public void TestReadInnerXml14()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.Comment))
                    {
                        TestLog.Compare(DataReader.ReadInnerXml(), string.Empty, Variation.Desc);
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadInnerXml on EndElement")]
                public void TestReadInnerXml16()
                {
                    XmlReader DataReader = GetReader();
                    FindNodeType(DataReader, XmlNodeType.EndElement);
                    TestLog.Compare(DataReader.ReadInnerXml(), string.Empty, Variation.Desc);
                }

                //[Variation("ReadInnerXml on XmlDeclaration")]
                public void TestReadInnerXml17()
                {
                    XmlReader DataReader = GetReader();
                    TestLog.Compare(DataReader.ReadInnerXml(), string.Empty, Variation.Desc);
                }

                //[Variation("Current node after ReadInnerXml on element", Priority = 0)]
                public void TestReadInnerXml18()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "SKIP2");
                    DataReader.ReadInnerXml();
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Element, "AFTERSKIP2", string.Empty), true, "VN");
                }

                //[Variation("Current node after ReadInnerXml on element")]
                public void TestReadInnerXml19()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "MARKUP");
                    TestLog.Compare(DataReader.ReadInnerXml(), string.Empty, "RIX");
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Text, string.Empty, "yyy"), true, "VN");
                }

                //[Variation("ReadInnerXml with entity references, EntityHandling = ExpandCharEntities")]
                public void TestTextReadInnerXml2()
                {
                    bool bPassed = false;
                    string strExpected = ST_EXPAND_ENTITIES2;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_ENTTEST_NAME);
                    bPassed = TestLog.Equals(DataReader.ReadInnerXml(), strExpected, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("ReadInnerXml on EntityReference")]
                public void TestTextReadInnerXml4()
                {
                    XmlReader DataReader = GetReader();
                    TestLog.Compare(DataReader.ReadInnerXml(), string.Empty, Variation.Desc);
                }

                //[Variation("ReadInnerXml on EndEntity")]
                public void TestTextReadInnerXml5()
                {
                    XmlReader DataReader = GetReader();
                    TestLog.Compare(DataReader.ReadInnerXml(), string.Empty, Variation.Desc);
                }

                //[Variation("One large element")]
                public void TestTextReadInnerXml18()
                {
                    string strp = "a                                                             ";
                    strp += strp;
                    strp += strp;
                    strp += strp;
                    strp += strp;
                    strp += strp;
                    strp += strp;
                    strp += strp;

                    string strxml = "<Name a=\"b\">" + strp + "</Name>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    DataReader.Read();
                    TestLog.Compare(DataReader.ReadInnerXml(), strp, "rix");
                }
            }

            //[TestCase(Name = "MoveToContent", Desc = "MoveToContent")]
            public partial class TCMoveToContent : BridgeHelpers
            {
                public const string ST_TEST_NAME1 = "GOTOCONTENT";
                public const string ST_TEST_NAME2 = "SKIPCONTENT";
                public const string ST_TEST_NAME3 = "MIXCONTENT";

                public const string ST_TEST_TEXT = "some text";
                public const string ST_TEST_CDATA = "cdata info";

                //[Variation("MoveToContent on Skip XmlDeclaration", Priority = 0)]
                public void TestMoveToContent1()
                {
                    XmlReader DataReader = GetReader();
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.Element, Variation.Desc);
                    TestLog.Compare(DataReader.Name, "PLAY", Variation.Desc);
                }

                //[Variation("MoveToContent on Read through All valid Content Node(Element, Text, CDATA, and EndElement)", Priority = 0)]
                public void TestMoveToContent2()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, ST_TEST_NAME1);
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.Element, Variation.Desc);
                    TestLog.Compare(DataReader.Name, ST_TEST_NAME1, "Element name");

                    DataReader.Read();
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.Text, "Move to Text");

                    TestLog.Compare(DataReader.ReadContentAsString(), ST_TEST_TEXT + ST_TEST_CDATA, "Read String");

                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.EndElement, "Move to EndElement");
                    TestLog.Compare(DataReader.Name, ST_TEST_NAME1, "EndElement value");
                }

                //[Variation("MoveToContent on Read through All invalid Content Node(PI, Comment and whitespace)", Priority = 0)]
                public void TestMoveToContent3()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, ST_TEST_NAME2);
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.Element, Variation.Desc);
                    TestLog.Compare(DataReader.Name, ST_TEST_NAME2, "Element name");

                    DataReader.Read();
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.Text, "Move to Text");
                    TestLog.Compare(DataReader.Name, "", "EndElement value");
                }

                //[Variation("MoveToContent on Read through Mix valid and Invalid Content Node")]
                public void TestMoveToContent4()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, ST_TEST_NAME3);
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.Element, Variation.Desc);
                    TestLog.Compare(DataReader.Name, ST_TEST_NAME3, "Element name");

                    DataReader.Read();
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.Text, "Move to Text");

                    DataReader.Read();
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.Text, "Move to Text");
                    TestLog.Compare(DataReader.Value, ST_TEST_TEXT, "text value");

                    DataReader.Read();
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.CDATA, "Move to CDATA");
                    TestLog.Compare(DataReader.Name, "", "CDATA value");

                    DataReader.Read();
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.EndElement, "Move to EndElement");
                    TestLog.Compare(DataReader.Name, ST_TEST_NAME3, "EndElement value");
                }

                //[Variation("MoveToContent on Attribute", Priority = 0)]
                public void TestMoveToContent5()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_NAME1);
                    PositionOnNodeType(DataReader, XmlNodeType.Attribute);
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.Element, "Move to EndElement");
                    TestLog.Compare(DataReader.Name, ST_TEST_NAME2, "EndElement value");
                }
            }

            //[TestCase(Name = "IsStartElement", Desc = "IsStartElement")]
            public partial class TCIsStartElement : BridgeHelpers
            {
                private const string ST_TEST_ELEM = "DOCNAMESPACE";
                private const string ST_TEST_EMPTY_ELEM = "NOSPACE";
                private const string ST_TEST_ELEM_NS = "NAMESPACE1";
                private const string ST_TEST_EMPTY_ELEM_NS = "EMPTY_NAMESPACE1";

                //[Variation("IsStartElement on Regular Element, no namespace", Priority = 0)]
                public void TestIsStartElement1()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM);

                    TestLog.Compare(DataReader.IsStartElement(), true, "IsStartElement()");
                    TestLog.Compare(DataReader.IsStartElement(ST_TEST_ELEM), true, "IsStartElement(n)");
                    TestLog.Compare(DataReader.IsStartElement(ST_TEST_ELEM, string.Empty), true, "IsStartElement(n,ns)");
                }

                //[Variation("IsStartElement on Empty Element, no namespace", Priority = 0)]
                public void TestIsStartElement2()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM);

                    TestLog.Compare(DataReader.IsStartElement(), true, "IsStartElement()");
                    TestLog.Compare(DataReader.IsStartElement(ST_TEST_EMPTY_ELEM), true, "IsStartElement(n)");
                    TestLog.Compare(DataReader.IsStartElement(ST_TEST_EMPTY_ELEM, string.Empty), true, "IsStartElement(n,ns)");
                }

                //[Variation("IsStartElement on regular Element, with namespace", Priority = 0)]
                public void TestIsStartElement3()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM_NS);
                    PositionOnElement(DataReader, "bar:check");

                    TestLog.Compare(DataReader.IsStartElement(), true, "IsStartElement()");
                    TestLog.Compare(DataReader.IsStartElement("check", "1"), true, "IsStartElement(n,ns)");
                    TestLog.Compare(DataReader.IsStartElement("check", string.Empty), false, "IsStartElement(n)");
                    TestLog.Compare(DataReader.IsStartElement("check"), false, "IsStartElement2(n)");
                    TestLog.Compare(DataReader.IsStartElement("bar:check"), true, "IsStartElement(qname)");
                    TestLog.Compare(DataReader.IsStartElement("bar1:check"), false, "IsStartElement(invalid_qname)");
                }

                //[Variation("IsStartElement on Empty Tag, with default namespace", Priority = 0)]
                public void TestIsStartElement4()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM_NS);

                    TestLog.Compare(DataReader.IsStartElement(), true, "IsStartElement()");
                    TestLog.Compare(DataReader.IsStartElement(ST_TEST_EMPTY_ELEM_NS, "14"), true, "IsStartElement(n,ns)");
                    TestLog.Compare(DataReader.IsStartElement(ST_TEST_EMPTY_ELEM_NS, string.Empty), false, "IsStartElement(n)");
                    TestLog.Compare(DataReader.IsStartElement(ST_TEST_EMPTY_ELEM_NS), true, "IsStartElement2(n)");
                }

                //[Variation("IsStartElement with Name=String.Empty")]
                public void TestIsStartElement5()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM);
                    TestLog.Compare(DataReader.IsStartElement(string.Empty), false, Variation.Desc);
                }

                //[Variation("IsStartElement on Empty Element with Name and Namespace=String.Empty")]
                public void TestIsStartElement6()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM);
                    TestLog.Compare(DataReader.IsStartElement(string.Empty, string.Empty), false, Variation.Desc);
                }

                //[Variation("IsStartElement on CDATA")]
                public void TestIsStartElement7()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.CDATA);
                    TestLog.Compare(DataReader.IsStartElement(), false, Variation.Desc);
                }

                //[Variation("IsStartElement on EndElement, no namespace")]
                public void TestIsStartElement8()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONAMESPACE");
                    PositionOnNodeType(DataReader, XmlNodeType.EndElement);
                    TestLog.Compare(DataReader.IsStartElement(), false, "IsStartElement()");
                    TestLog.Compare(DataReader.IsStartElement("NONAMESPACE"), false, "IsStartElement(n)");
                    TestLog.Compare(DataReader.IsStartElement("NONAMESPACE", string.Empty), false, "IsStartElement(n,ns)");
                }

                //[Variation("IsStartElement on EndElement, with namespace")]
                public void TestIsStartElement9()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM_NS);
                    PositionOnElement(DataReader, "bar:check");
                    PositionOnNodeType(DataReader, XmlNodeType.EndElement);

                    TestLog.Compare(DataReader.IsStartElement(), false, "IsStartElement()");
                    TestLog.Compare(DataReader.IsStartElement("check", "1"), false, "IsStartElement(n,ns)");
                    TestLog.Compare(DataReader.IsStartElement("bar:check"), false, "IsStartElement(qname)");
                }

                //[Variation("IsStartElement on Attribute")]
                public void TestIsStartElement10()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Attribute);
                    TestLog.Compare(DataReader.IsStartElement(), true, Variation.Desc);
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, Variation.Desc);
                }

                //[Variation("IsStartElement on Text")]
                public void TestIsStartElement11()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Text);
                    TestLog.Compare(DataReader.IsStartElement(), false, Variation.Desc);
                }

                //[Variation("IsStartElement on ProcessingInstruction")]
                public void TestIsStartElement12()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.ProcessingInstruction);
                    TestLog.Compare(DataReader.IsStartElement(), true, Variation.Desc);
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, Variation.Desc);
                }

                //[Variation("IsStartElement on Comment")]
                public void TestIsStartElement13()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Comment);
                    TestLog.Compare(DataReader.IsStartElement(), true, Variation.Desc);
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, Variation.Desc);
                }
            }

            //[TestCase(Name = "ReadStartElement", Desc = "ReadStartElement")]
            public partial class TCReadStartElement : BridgeHelpers
            {
                private const string ST_TEST_ELEM = "DOCNAMESPACE";
                private const string ST_TEST_EMPTY_ELEM = "NOSPACE";
                private const string ST_TEST_ELEM_NS = "NAMESPACE1";
                private const string ST_TEST_EMPTY_ELEM_NS = "EMPTY_NAMESPACE1";

                //[Variation("ReadStartElement on Regular Element, no namespace", Priority = 0)]
                public void TestReadStartElement1()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM);
                    DataReader.ReadStartElement();

                    DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM);
                    DataReader.ReadStartElement(ST_TEST_ELEM);

                    DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM);
                    DataReader.ReadStartElement(ST_TEST_ELEM, string.Empty);
                }

                //[Variation("ReadStartElement on Empty Element, no namespace", Priority = 0)]
                public void TestReadStartElement2()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM);
                    DataReader.ReadStartElement();

                    DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM);
                    DataReader.ReadStartElement(ST_TEST_EMPTY_ELEM);

                    DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM);
                    DataReader.ReadStartElement(ST_TEST_EMPTY_ELEM, string.Empty);
                }

                //[Variation("ReadStartElement on regular Element, with namespace", Priority = 0)]
                public void TestReadStartElement3()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM_NS);
                    PositionOnElement(DataReader, "bar:check");
                    DataReader.ReadStartElement();

                    DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM_NS);
                    PositionOnElement(DataReader, "bar:check");
                    DataReader.ReadStartElement("check", "1");

                    DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM_NS);
                    PositionOnElement(DataReader, "bar:check");
                    DataReader.ReadStartElement("bar:check");
                }

                //[Variation("Passing ns=String.EmptyErrorCase: ReadStartElement on regular Element, with namespace", Priority = 0)]
                public void TestReadStartElement4()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM_NS);
                    PositionOnElement(DataReader, "bar:check");

                    try
                    {
                        DataReader.ReadStartElement("check", string.Empty);
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("Passing no ns: ReadStartElement on regular Element, with namespace", Priority = 0)]
                public void TestReadStartElement5()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM_NS);
                    PositionOnElement(DataReader, "bar:check");

                    try
                    {
                        DataReader.ReadStartElement("check");
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadStartElement on Empty Tag, with namespace")]
                public void TestReadStartElement6()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM_NS);
                    DataReader.ReadStartElement();

                    DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM_NS);
                    DataReader.ReadStartElement(ST_TEST_EMPTY_ELEM_NS, "14");
                }

                //[Variation("ErrorCase: ReadStartElement on Empty Tag, with namespace, passing ns=String.Empty")]
                public void TestReadStartElement7()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM_NS);

                    try
                    {
                        DataReader.ReadStartElement(ST_TEST_EMPTY_ELEM_NS, string.Empty);
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadStartElement on Empty Tag, with namespace, passing no ns")]
                public void TestReadStartElement8()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM_NS);

                    DataReader.ReadStartElement(ST_TEST_EMPTY_ELEM_NS);
                }

                //[Variation("ReadStartElement with Name=String.Empty")]
                public void TestReadStartElement9()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM);
                    try
                    {
                        DataReader.ReadStartElement(string.Empty);
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadStartElement on Empty Element with Name and Namespace=String.Empty")]
                public void TestReadStartElement10()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM_NS);
                    try
                    {
                        DataReader.ReadStartElement(string.Empty, string.Empty);
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadStartElement on CDATA")]
                public void TestReadStartElement11()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.CDATA);
                    try
                    {
                        DataReader.ReadStartElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadStartElement() on EndElement, no namespace")]
                public void TestReadStartElement12()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONAMESPACE");
                    PositionOnNodeType(DataReader, XmlNodeType.EndElement);
                    try
                    {
                        DataReader.ReadStartElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadStartElement(n) on EndElement, no namespace")]
                public void TestReadStartElement13()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONAMESPACE");
                    PositionOnNodeType(DataReader, XmlNodeType.EndElement);
                    try
                    {
                        DataReader.ReadStartElement("NONAMESPACE");
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadStartElement(n, String.Empty) on EndElement, no namespace")]
                public void TestReadStartElement14()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONAMESPACE");
                    PositionOnNodeType(DataReader, XmlNodeType.EndElement);
                    try
                    {
                        DataReader.ReadStartElement("NONAMESPACE", string.Empty);
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadStartElement() on EndElement, with namespace")]
                public void TestReadStartElement15()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM_NS);
                    PositionOnElement(DataReader, "bar:check");
                    PositionOnNodeType(DataReader, XmlNodeType.EndElement);

                    try
                    {
                        DataReader.ReadStartElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadStartElement(n,ns) on EndElement, with namespace")]
                public void TestReadStartElement16()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM_NS);
                    PositionOnElement(DataReader, "bar:check");
                    PositionOnNodeType(DataReader, XmlNodeType.EndElement);

                    try
                    {
                        DataReader.ReadStartElement("check", "1");
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }
            }

            //[TestCase(Name = "ReadEndElement", Desc = "ReadEndElement")]
            public partial class TCReadEndElement : BridgeHelpers
            {
                private const string ST_TEST_ELEM = "DOCNAMESPACE";
                private const string ST_TEST_EMPTY_ELEM = "NOSPACE";
                private const string ST_TEST_ELEM_NS = "NAMESPACE1";
                private const string ST_TEST_EMPTY_ELEM_NS = "EMPTY_NAMESPACE1";

                [Fact]
                public void TestReadEndElementOnEndElementWithoutNamespace()
                {
                    using (XmlReader DataReader = GetPGenericXmlReader())
                    {
                        PositionOnElement(DataReader, "NONAMESPACE");
                        PositionOnNodeType(DataReader, XmlNodeType.EndElement);
                        Assert.True(VerifyNode(DataReader, XmlNodeType.EndElement, "NONAMESPACE", string.Empty));
                    }
                }

                [Fact]
                public void TestReadEndElementOnEndElementWithNamespace()
                {
                    using (XmlReader DataReader = GetPGenericXmlReader())
                    {
                        PositionOnElement(DataReader, ST_TEST_ELEM_NS);
                        PositionOnElement(DataReader, "bar:check");
                        PositionOnNodeType(DataReader, XmlNodeType.EndElement);
                        Assert.True(VerifyNode(DataReader, XmlNodeType.EndElement, "bar:check", string.Empty));
                    }
                }

                //[Variation("ReadEndElement on Start Element, no namespace")]
                private void TestReadEndElement3()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM);
                    try
                    {
                        DataReader.ReadEndElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadEndElement on Empty Element, no namespace", Priority = 0)]
                private void TestReadEndElement4()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM);
                    try
                    {
                        DataReader.ReadEndElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadEndElement on regular Element, with namespace", Priority = 0)]
                private void TestReadEndElement5()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_ELEM_NS);
                    PositionOnElement(DataReader, "bar:check");
                    try
                    {
                        DataReader.ReadEndElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadEndElement on Empty Tag, with namespace", Priority = 0)]
                private void TestReadEndElement6()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_EMPTY_ELEM_NS);
                    try
                    {
                        DataReader.ReadEndElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadEndElement on CDATA")]
                private void TestReadEndElement7()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.CDATA);
                    try
                    {
                        DataReader.ReadEndElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadEndElement on Text")]
                private void TestReadEndElement9()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Text);
                    try
                    {
                        DataReader.ReadEndElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadEndElement on ProcessingInstruction")]
                private void TestReadEndElement10()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.ProcessingInstruction);
                    try
                    {
                        DataReader.ReadEndElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadEndElement on Comment")]
                private void TestReadEndElement11()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Comment);
                    try
                    {
                        DataReader.ReadEndElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadEndElement on XmlDeclaration")]
                private void TestReadEndElement13()
                {
                    XmlReader DataReader = GetReader();
                    try
                    {
                        DataReader.ReadEndElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadEndElement on EntityReference")]
                private void TestTextReadEndElement1()
                {
                    XmlReader DataReader = GetReader();
                    try
                    {
                        DataReader.ReadEndElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadEndElement on EndEntity")]
                private void TestTextReadEndElement2()
                {
                    XmlReader DataReader = GetReader();
                    try
                    {
                        DataReader.ReadEndElement();
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCMoveToElement : BridgeHelpers
            {
                //[Variation("Attribute node")]
                public void v1()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, "elem2");
                    DataReader.MoveToAttribute(1);
                    TestLog.Compare(DataReader.MoveToElement(), "MTE on elem2 failed");
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Element, "elem2", string.Empty), "MTE moved on wrong node");
                }

                //[Variation("Element node")]
                public void v2()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, "elem2");
                    TestLog.Compare(!DataReader.MoveToElement(), "MTE on elem2 failed");
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Element, "elem2", string.Empty), "MTE moved on wrong node");
                }

                //[Variation("Comment node")]
                public void v5()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnNodeType(DataReader, XmlNodeType.Comment);
                    TestLog.Compare(!DataReader.MoveToElement(), "MTE on comment failed");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Comment, "comment");
                }
            }
        }
    }
}
