// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using Microsoft.Test.ModuleCore;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeReaderFunctionalTests : TestModule
    {

        public partial class XNodeReaderTests : XLinqTestCase
        {
            public partial class TCReadOuterXml : BridgeHelpers
            {
                // Element names to test ReadOuterXml on
                private static string s_EMP1 = "EMPTY1";
                private static string s_EMP2 = "EMPTY2";
                private static string s_EMP3 = "EMPTY3";
                private static string s_EMP4 = "EMPTY4";
                private static string s_ENT1 = "ENTITY1";
                private static string s_NEMP0 = "NONEMPTY0";
                private static string s_NEMP1 = "NONEMPTY1";
                private static string s_NEMP2 = "NONEMPTY2";
                private static string s_ELEM1 = "CHARS2";
                private static string s_ELEM2 = "SKIP3";
                private static string s_ELEM3 = "CONTENT";
                private static string s_ELEM4 = "COMPLEX";

                // Element names after the ReadOuterXml call
                private static string s_NEXT1 = "COMPLEX";
                private static string s_NEXT2 = "ACT2";
                private static string s_NEXT3 = "CHARS_ELEM1";
                private static string s_NEXT4 = "AFTERSKIP3";
                private static string s_NEXT5 = "TITLE";
                private static string s_NEXT6 = "ENTITY2";
                private static string s_NEXT7 = "DUMMY";

                // Expected strings returned by ReadOuterXml
                private static string s_EXP_EMP1 = "<EMPTY1 />";
                private static string s_EXP_EMP2 = "<EMPTY2 val=\"abc\" />";
                private static string s_EXP_EMP3 = "<EMPTY3></EMPTY3>";
                private static string s_EXP_EMP4 = "<EMPTY4 val=\"abc\"></EMPTY4>";
                private static string s_EXP_NEMP1 = "<NONEMPTY1>ABCDE</NONEMPTY1>";
                private static string s_EXP_NEMP2 = "<NONEMPTY2 val=\"abc\">1234</NONEMPTY2>";
                private static string s_EXP_ELEM1 = "<CHARS2>xxx<MARKUP />yyy</CHARS2>";
                private static string s_EXP_ELEM2 = "<SKIP3><ELEM1 /><ELEM2>xxx yyy</ELEM2><ELEM3 /></SKIP3>";
                private static string s_EXP_ELEM3 = "<CONTENT><e1 a1=\"a1value\" a2=\"a2value\"><e2 a1=\"a1value\" a2=\"a2value\"><e3 a1=\"a1value\" a2=\"a2value\">leave</e3></e2></e1></CONTENT>";
                private static string s_EXP_ELEM4 = "<COMPLEX>Text<!-- comment --><![CDATA[cdata]]></COMPLEX>";
                private static string s_EXP_ENT1_EXPAND_CHAR = "<ENTITY1 att1=\"xxx&lt;xxxAxxxCxxxe1fooxxx\">xxx&gt;xxxBxxxDxxxe1fooxxx</ENTITY1>";

                public override void Init()
                {
                    base.Init();
                }

                public override void Terminate()
                {
                    base.Terminate();
                }

                void TestOuterOnText(string strElem, string strOuterXml, string strNextElemName, bool bWhitespace)
                {
                    XmlReader DataReader = GetReader();//GetReader(pGenericXml);
                    PositionOnElement(DataReader, strElem);
                    TestLog.Compare(DataReader.ReadOuterXml(), strOuterXml, "outer");
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Text, string.Empty, "\n"), true, "vn2");
                }

                void TestOuterOnElement(string strElem, string strOuterXml, string strNextElemName, bool bWhitespace)
                {
                    XmlReader DataReader = GetReader();//GetReader(pGenericXml);
                    PositionOnElement(DataReader, strElem);
                    TestLog.Compare(DataReader.ReadOuterXml(), strOuterXml, "outer");
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Element, strNextElemName, string.Empty), true, "vn2");
                }

                void TestOuterOnAttribute(string strElem, string strName, string strValue)
                {
                    XmlReader DataReader = GetReader();//GetReader(pGenericXml);
                    PositionOnElement(DataReader, strElem);
                    DataReader.MoveToAttribute(DataReader.AttributeCount / 2);
                    string strExpected = string.Format("{0}=\"{1}\"", strName, strValue);
                    TestLog.Compare(DataReader.ReadOuterXml(), strExpected, "outer");
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Attribute, strName, strValue), true, "vn");
                }

                void TestOuterOnNodeType(XmlNodeType nt)
                {
                    XmlReader DataReader = GetReader();//GetReader(pGenericXml);
                    PositionOnNodeType(DataReader, nt);
                    DataReader.Read();

                    XmlNodeType expNt = DataReader.NodeType;
                    string expName = DataReader.Name;
                    string expValue = DataReader.Value;

                    PositionOnNodeType(DataReader, nt);
                    TestLog.Compare(DataReader.ReadOuterXml(), string.Empty, "outer");
                    TestLog.Compare(VerifyNode(DataReader, expNt, expName, expValue), true, "vn");
                }

                //[Variation("ReadOuterXml on empty element w/o attributes", Priority = 0)]
                public void ReadOuterXml1()
                {
                    TestOuterOnText(s_EMP1, s_EXP_EMP1, s_EMP2, true);
                }

                //[Variation("ReadOuterXml on empty element w/ attributes", Priority = 0)]
                public void ReadOuterXml2()
                {
                    TestOuterOnText(s_EMP2, s_EXP_EMP2, s_EMP3, true);
                }

                //[Variation("ReadOuterXml on full empty element w/o attributes")]
                public void ReadOuterXml3()
                {
                    TestOuterOnText(s_EMP3, s_EXP_EMP3, s_NEMP0, true);
                }

                //[Variation("ReadOuterXml on full empty element w/ attributes")]
                public void ReadOuterXml4()
                {
                    TestOuterOnText(s_EMP4, s_EXP_EMP4, s_NEXT1, true);
                }

                //[Variation("ReadOuterXml on element with text content", Priority = 0)]
                public void ReadOuterXml5()
                {
                    TestOuterOnText(s_NEMP1, s_EXP_NEMP1, s_NEMP2, true);
                }

                //[Variation("ReadOuterXml on element with attributes", Priority = 0)]
                public void ReadOuterXml6()
                {
                    TestOuterOnText(s_NEMP2, s_EXP_NEMP2, s_NEXT2, true);
                }

                //[Variation("ReadOuterXml on element with text and markup content")]
                public void ReadOuterXml7()
                {
                    TestOuterOnText(s_ELEM1, s_EXP_ELEM1, s_NEXT3, true);
                }

                //[Variation("ReadOuterXml with multiple level of elements")]
                public void ReadOuterXml8()
                {
                    TestOuterOnElement(s_ELEM2, s_EXP_ELEM2, s_NEXT4, false);
                }

                //[Variation("ReadOuterXml with multiple level of elements, text and attributes", Priority = 0)]
                public void ReadOuterXml9()
                {
                    string strExpected = s_EXP_ELEM3;
                    TestOuterOnText(s_ELEM3, strExpected, s_NEXT5, true);
                }

                //[Variation("ReadOuterXml on element with complex content (CDATA, PIs, Comments)", Priority = 0)]
                public void ReadOuterXml10()
                {
                    TestOuterOnText(s_ELEM4, s_EXP_ELEM4, s_NEXT7, true);
                }

                //[Variation("ReadOuterXml on attribute node of empty element")]
                public void ReadOuterXml12()
                {
                    TestOuterOnAttribute(s_EMP2, "val", "abc");
                }

                //[Variation("ReadOuterXml on attribute node of full empty element")]
                public void ReadOuterXml13()
                {
                    TestOuterOnAttribute(s_EMP4, "val", "abc");
                }

                //[Variation("ReadOuterXml on attribute node", Priority = 0)]
                public void ReadOuterXml14()
                {
                    TestOuterOnAttribute(s_NEMP2, "val", "abc");
                }

                //[Variation("ReadOuterXml on attribute with entities, EntityHandling = ExpandEntities", Priority = 0)]
                public void ReadOuterXml15()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, s_ENT1);

                    DataReader.MoveToAttribute(DataReader.AttributeCount / 2);

                    string strExpected = "att1=\"xxx&lt;xxxAxxxCxxxe1fooxxx\"";

                    TestLog.Compare(DataReader.ReadOuterXml(), strExpected, "outer");
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Attribute, "att1", ST_ENT1_ATT_EXPAND_ENTITIES), true, "vn");
                }

                //[Variation("ReadOuterXml on ProcessingInstruction")]
                public void ReadOuterXml17()
                {
                    TestOuterOnNodeType(XmlNodeType.ProcessingInstruction);
                }

                //[Variation("ReadOuterXml on CDATA")]
                public void ReadOuterXml24()
                {
                    TestOuterOnNodeType(XmlNodeType.CDATA);
                }

                [Fact]
                public void ReadOuterXmlOnXmlDeclarationAttributes()
                {
                    using (XmlReader DataReader = GetPGenericXmlReader())
                    {
                        DataReader.Read();
                        try
                        {
                            DataReader.MoveToAttribute(DataReader.AttributeCount / 2);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (ArgumentOutOfRangeException) { }
                        Assert.True(TestLog.Compare(DataReader.ReadOuterXml(), string.Empty, "outer"));
                        Assert.True((DataReader.NodeType != XmlNodeType.Attribute) || (DataReader.Name != string.Empty) || (DataReader.Value != "UTF-8"));
                    }
                }

                //[Variation("ReadOuterXml on element with entities, EntityHandling = ExpandCharEntities")]
                public void TRReadOuterXml27()
                {
                    string strExpected = s_EXP_ENT1_EXPAND_CHAR;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, s_ENT1);

                    TestLog.Compare(DataReader.ReadOuterXml(), strExpected, "outer");
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Element, s_NEXT6, string.Empty), true, "vn");
                }

                //[Variation("ReadOuterXml on attribute with entities, EntityHandling = ExpandCharEntites")]
                public void TRReadOuterXml28()
                {
                    string strExpected = "att1=\"xxx&lt;xxxAxxxCxxxe1fooxxx\"";
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, s_ENT1);
                    DataReader.MoveToAttribute(DataReader.AttributeCount / 2);
                    TestLog.Compare(DataReader.ReadOuterXml(), strExpected, "outer");
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Attribute, "att1", ST_ENT1_ATT_EXPAND_ENTITIES), true, "vn");
                }

                //[Variation("One large element")]
                public void TestTextReadOuterXml29()
                {
                    string strp = "a                                                             ";
                    strp += strp;
                    strp += strp;
                    strp += strp;
                    strp += strp;
                    strp += strp;
                    strp += strp;
                    strp += strp;

                    string strxml = "<Name a=\"b\">" + strp + " </Name>";
                    XmlReader DataReader = GetReaderStr(strxml);

                    DataReader.Read();
                    TestLog.Compare(DataReader.ReadOuterXml(), strxml, "rox");
                }

                //[Variation("Read OuterXml when Namespaces=false and has an attribute xmlns")]
                public void ReadOuterXmlWhenNamespacesEqualsToFalseAndHasAnAttributeXmlns()
                {
                    string xml = "<?xml version='1.0' encoding='utf-8' ?> <foo xmlns=\"testing\"><bar id=\"1\" /></foo>";
                    XmlReader DataReader = GetReaderStr(xml);
                    DataReader.MoveToContent();
                    TestLog.Compare(DataReader.ReadOuterXml(), "<foo xmlns=\"testing\"><bar id=\"1\" /></foo>", "mismatch");
                }
            }
        }
    }
}
