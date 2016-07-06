// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class XNodeReaderTests : XLinqTestCase
        {
            public partial class TCDepth : BridgeHelpers
            {
                //[Variation("XmlReader Depth at the Root", Priority = 0)]
                public void TestDepth1()
                {
                    XmlReader DataReader = GetReader();
                    int iDepth = 0;
                    PositionOnElement(DataReader, "PLAY");
                    TestLog.Compare(DataReader.Depth, iDepth, "Depth should be " + iDepth);
                    DataReader.Dispose();
                }

                //[Variation("XmlReader Depth at Empty Tag")]
                public void TestDepth2()
                {
                    XmlReader DataReader = GetReader();
                    int iDepth = 2;
                    PositionOnElement(DataReader, "EMPTY1");
                    TestLog.Compare(DataReader.Depth, iDepth, "Depth should be " + iDepth);
                }

                //[Variation("XmlReader Depth at Empty Tag with Attributes")]
                public void TestDepth3()
                {
                    XmlReader DataReader = GetReader();
                    int iDepth = 2;
                    PositionOnElement(DataReader, "ACT1");
                    TestLog.Compare(DataReader.Depth, iDepth, "Element Depth should be " + (iDepth).ToString());
                    while (DataReader.MoveToNextAttribute() == true)
                    {
                        TestLog.Compare(DataReader.Depth, iDepth + 1, "Attr Depth should be " + (iDepth + 1).ToString());
                    }
                }

                //[Variation("XmlReader Depth at Non Empty Tag with Text")]
                public void TestDepth4()
                {
                    XmlReader DataReader = GetReader();
                    int iDepth = 2;
                    PositionOnElement(DataReader, "NONEMPTY1");
                    TestLog.Compare(DataReader.Depth, iDepth, "Depth should be " + iDepth);
                    while (true == DataReader.Read())
                    {
                        if (DataReader.NodeType == XmlNodeType.Text)
                            TestLog.Compare(DataReader.Depth, iDepth + 1, "Depth should be " + (iDepth + 1).ToString());

                        if (DataReader.Name == "NONEMPTY1" && (DataReader.NodeType == XmlNodeType.EndElement)) break;
                    }
                    TestLog.Compare(DataReader.Depth, iDepth, "Depth should be " + iDepth);
                }
            }

            public partial class TCNamespace : BridgeHelpers
            {
                public static string pNONAMESPACE = "NONAMESPACE";

                //[Variation("Namespace test within a scope (no nested element)", Priority = 0)]
                public void TestNamespace1()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NAMESPACE0");

                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "NAMESPACE1") break;

                        if (DataReader.NodeType == XmlNodeType.Element)
                        {
                            TestLog.Compare(DataReader.NamespaceURI, "1", "Compare Namespace");
                            TestLog.Compare(DataReader.Name, "bar:check", "Compare Name");
                            TestLog.Compare(DataReader.LocalName, "check", "Compare LocalName");
                            TestLog.Compare(DataReader.Prefix, "bar", "Compare Prefix");
                        }
                    }
                }

                //[Variation("Namespace test within a scope (with nested element)", Priority = 0)]
                public void TestNamespace2()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, "NAMESPACE1");
                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "NONAMESPACE") break;
                        if ((DataReader.NodeType == XmlNodeType.Element) && (DataReader.LocalName == "check"))
                        {
                            TestLog.Compare(DataReader.NamespaceURI, "1", "Compare Namespace");
                            TestLog.Compare(DataReader.Name, "bar:check", "Compare Name");
                            TestLog.Compare(DataReader.LocalName, "check", "Compare LocalName");
                            TestLog.Compare(DataReader.Prefix, "bar", "Compare Prefix");
                        }
                    }
                    TestLog.Compare(DataReader.NamespaceURI, String.Empty, "Compare Namespace with String.Empty");
                }

                //[Variation("Namespace test immediately outside the Namespace scope")]
                public void TestNamespace3()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, pNONAMESPACE);
                    TestLog.Compare(DataReader.NamespaceURI, String.Empty, "Compare Namespace with EmptyString");
                    TestLog.Compare(DataReader.Name, pNONAMESPACE, "Compare Name");
                    TestLog.Compare(DataReader.LocalName, pNONAMESPACE, "Compare LocalName");
                    TestLog.Compare(DataReader.Prefix, String.Empty, "Compare Prefix");
                }

                //[Variation("Namespace test Attribute should has no default namespace", Priority = 0)]
                public void TestNamespace4()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONAMESPACE1");
                    TestLog.Compare(DataReader.NamespaceURI, "1000", "Compare Namespace for Element");
                    if (DataReader.MoveToFirstAttribute())
                    {
                        TestLog.Compare(DataReader.NamespaceURI, String.Empty, "Compare Namespace for Attr");
                    }
                }

                //[Variation("Namespace test with multiple Namespace declaration", Priority = 0)]
                public void TestNamespace5()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, "NAMESPACE2");
                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "NAMESPACE3") break;
                        if ((DataReader.NodeType == XmlNodeType.Element) && (DataReader.LocalName == "check"))
                        {
                            TestLog.Compare(DataReader.NamespaceURI, "2", "Compare Namespace");
                            TestLog.Compare(DataReader.Name, "bar:check", "Compare Name");
                            TestLog.Compare(DataReader.LocalName, "check", "Compare LocalName");
                            TestLog.Compare(DataReader.Prefix, "bar", "Compare Prefix");
                        }
                    }
                }

                //[Variation("Namespace test with multiple Namespace declaration, including default namespace")]
                public void TestNamespace6()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, "NAMESPACE3");
                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "NONAMESPACE") break;

                        if (DataReader.NodeType == XmlNodeType.Element)
                        {
                            if (DataReader.LocalName == "check")
                            {
                                TestLog.Compare(DataReader.NamespaceURI, "1", "Compare Namespace");
                                TestLog.Compare(DataReader.Name, "check", "Compare Name");
                                TestLog.Compare(DataReader.LocalName, "check", "Compare LocalName");
                                TestLog.Compare(DataReader.Prefix, String.Empty, "Compare Prefix");
                            }
                            else if (DataReader.LocalName == "check1")
                            {
                                TestLog.Compare(DataReader.NamespaceURI, "1", "Compare Namespace");
                                TestLog.Compare(DataReader.Name, "check1", "Compare Name");
                                TestLog.Compare(DataReader.LocalName, "check1", "Compare LocalName");
                                TestLog.Compare(DataReader.Prefix, String.Empty, "Compare Prefix");
                            }
                            else if (DataReader.LocalName == "check8")
                            {
                                TestLog.Compare(DataReader.NamespaceURI, "8", "Compare Namespace");
                                TestLog.Compare(DataReader.Name, "d:check8", "Compare Name");
                                TestLog.Compare(DataReader.LocalName, "check8", "Compare LocalName");
                                TestLog.Compare(DataReader.Prefix, "d", "Compare Prefix");
                            }
                            else if (DataReader.LocalName == "check100")
                            {
                                TestLog.Compare(DataReader.NamespaceURI, "100", "Compare Namespace");
                                TestLog.Compare(DataReader.Name, "check100", "Compare Name");
                                TestLog.Compare(DataReader.LocalName, "check100", "Compare LocalName");
                                TestLog.Compare(DataReader.Prefix, String.Empty, "Compare Prefix");
                            }
                            else if (DataReader.LocalName == "check5")
                            {
                                TestLog.Compare(DataReader.NamespaceURI, "5", "Compare Namespace");
                                TestLog.Compare(DataReader.Name, "d:check5", "Compare Name");
                                TestLog.Compare(DataReader.LocalName, "check5", "Compare LocalName");
                                TestLog.Compare(DataReader.Prefix, "d", "Compare Prefix");
                            }
                            else if (DataReader.LocalName == "check14")
                            {
                                TestLog.Compare(DataReader.NamespaceURI, "14", "Compare Namespace");
                                TestLog.Compare(DataReader.Name, "check14", "Compare Name");
                                TestLog.Compare(DataReader.LocalName, "check14", "Compare LocalName");
                                TestLog.Compare(DataReader.Prefix, String.Empty, "Compare Prefix");
                            }
                            else if (DataReader.LocalName == "a13")
                            {
                                TestLog.Compare(DataReader.NamespaceURI, "1", "Compare Namespace1");
                                TestLog.Compare(DataReader.Name, "a13", "Compare Name1");
                                TestLog.Compare(DataReader.LocalName, "a13", "Compare LocalName1");
                                TestLog.Compare(DataReader.Prefix, String.Empty, "Compare Prefix1");
                                DataReader.MoveToFirstAttribute();
                                TestLog.Compare(DataReader.NamespaceURI, "13", "Compare Namespace2");
                                TestLog.Compare(DataReader.Name, "a:check", "Compare Name2");
                                TestLog.Compare(DataReader.LocalName, "check", "Compare LocalName2");
                                TestLog.Compare(DataReader.Prefix, "a", "Compare Prefix2");
                                TestLog.Compare(DataReader.Value, "Namespace=13", "Compare Name2");
                            }
                        }
                    }
                }

                //[Variation("Namespace URI for xml prefix", Priority = 0)]
                public void TestNamespace7()
                {
                    string strxml = "<ROOT xml:space='preserve'/>";
                    XmlReader DataReader = GetReader(new StringReader(strxml));
                    PositionOnElement(DataReader, "ROOT");
                    DataReader.MoveToFirstAttribute();
                    TestLog.Compare(DataReader.NamespaceURI, "http://www.w3.org/XML/1998/namespace", "xml");
                }
            }

            public partial class TCLookupNamespace : BridgeHelpers
            {
                //[Variation("LookupNamespace test within EmptyTag")]
                public void LookupNamespace1()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY_NAMESPACE");
                    do
                    {
                        TestLog.Compare(DataReader.LookupNamespace("bar"), "1", "Compare LookupNamespace");
                    } while (DataReader.MoveToNextAttribute() == true);
                }

                //[Variation("LookupNamespace test with Default namespace within EmptyTag", Priority = 0)]
                public void LookupNamespace2()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY_NAMESPACE1");
                    do
                    {
                        TestLog.Compare(DataReader.LookupNamespace(String.Empty), "14", "Compare LookupNamespace");
                    } while (DataReader.MoveToNextAttribute() == true);
                }

                //[Variation("LookupNamespace test within a scope (no nested element)", Priority = 0)]
                public void LookupNamespace3()
                {
                    XmlReader DataReader = GetReader();
                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "NAMESPACE0") break;
                        TestLog.Compare(DataReader.LookupNamespace("bar"), null, "Compare LookupNamespace");
                    }

                    while (true == DataReader.Read())
                    {
                        TestLog.Compare(DataReader.LookupNamespace("bar"), "1", "Compare LookupNamespace");
                        if (DataReader.Name == "NAMESPACE0" && DataReader.NodeType == XmlNodeType.EndElement) break;
                    }
                }

                //[Variation("LookupNamespace test within a scope (with nested element)", Priority = 0)]
                public void LookupNamespace4()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NAMESPACE1");
                    while (true == DataReader.Read())
                    {
                        TestLog.Compare(DataReader.LookupNamespace("bar"), "1", "Compare LookupNamespace");
                        if (DataReader.Name == "NAMESPACE1" && DataReader.NodeType == XmlNodeType.EndElement)
                        {
                            DataReader.Read();
                            break;
                        }
                    }
                    TestLog.Compare(DataReader.LookupNamespace("bar"), null, "Compare LookupNamespace with String.Empty");
                }

                //[Variation("LookupNamespace test immediately outside the Namespace scope")]
                public void LookupNamespace5()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONAMESPACE");
                    TestLog.Compare(DataReader.LookupNamespace("bar"), null, "Compare LookupNamespace with null");
                }

                //[Variation("LookupNamespace test with multiple Namespace declaration", Priority = 0)]
                public void LookupNamespace6()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NAMESPACE2");

                    string strValue = "1";
                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "c")
                        {
                            strValue = "2";
                            TestLog.Compare(DataReader.LookupNamespace("bar"), strValue, "Compare LookupNamespace-a");
                            if (DataReader.NodeType == XmlNodeType.EndElement)
                                strValue = "1";
                        }
                        else
                            TestLog.Compare(DataReader.LookupNamespace("bar"), strValue, "Compare LookupNamespace-a");

                        if (DataReader.Name == "NAMESPACE2" && DataReader.NodeType == XmlNodeType.EndElement)
                        {
                            TestLog.Compare(DataReader.LookupNamespace("bar"), strValue, "Compare LookupNamespace-a");
                            DataReader.Read();
                            break;
                        }
                    }
                }

                void CompareAllNS(XmlReader DataReader, string strDef, string strA, string strB, string strC, string strD, string strE, string strF, string strG, string strH)
                {
                    TestLog.Compare(DataReader.LookupNamespace(String.Empty), strDef, "Compare LookupNamespace-default");
                    TestLog.Compare(DataReader.LookupNamespace("a"), strA, "Compare LookupNamespace-a");
                    TestLog.Compare(DataReader.LookupNamespace("b"), strB, "Compare LookupNamespace-b");
                    TestLog.Compare(DataReader.LookupNamespace("c"), strC, "Compare LookupNamespace-c");
                    TestLog.Compare(DataReader.LookupNamespace("d"), strD, "Compare LookupNamespace-d");
                    TestLog.Compare(DataReader.LookupNamespace("e"), strE, "Compare LookupNamespace-e");
                    TestLog.Compare(DataReader.LookupNamespace("f"), strF, "Compare LookupNamespace-f");
                    TestLog.Compare(DataReader.LookupNamespace("g"), strG, "Compare LookupNamespace-g");
                    TestLog.Compare(DataReader.LookupNamespace("h"), strH, "Compare LookupNamespace-h");
                }

                //[Variation("Namespace test with multiple Namespace declaration, including default namespace")]
                public void LookupNamespace7()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NAMESPACE3");

                    string strDef = "1";
                    string strA = null;
                    string strB = null;
                    string strC = null;
                    string strD = null;
                    string strE = null;
                    string strF = null;
                    string strG = null;
                    string strH = null;
                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "a")
                        {
                            strA = "2";
                            strB = "3";
                            strC = "4";
                            CompareAllNS(DataReader, strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                            if (DataReader.NodeType == XmlNodeType.EndElement)
                            {
                                strA = null;
                                strB = null;
                                strC = null;
                            }
                        }
                        else if (DataReader.Name == "b")
                        {
                            strD = "5";
                            strE = "6";
                            strF = "7";
                            CompareAllNS(DataReader, strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                            if (DataReader.NodeType == XmlNodeType.EndElement)
                            {
                                strD = null;
                                strE = null;
                                strF = null;
                            }
                        }
                        else if (DataReader.Name == "c")
                        {
                            strD = "8";
                            strE = "9";
                            strF = "10";
                            CompareAllNS(DataReader, strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                            if (DataReader.NodeType == XmlNodeType.EndElement)
                            {
                                strD = "5";
                                strE = "6";
                                strF = "7";
                            }
                        }
                        else if (DataReader.Name == "d")
                        {
                            strG = "11";
                            strH = "12";
                            CompareAllNS(DataReader, strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                            if (DataReader.NodeType == XmlNodeType.EndElement)
                            {
                                strG = null;
                                strH = null;
                            }
                        }
                        else if (DataReader.Name == "testns")
                        {
                            strDef = "100";
                            CompareAllNS(DataReader, strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                            if (DataReader.NodeType == XmlNodeType.EndElement)
                            {
                                strDef = "1";
                            }
                        }
                        else if (DataReader.Name == "a13")
                        {
                            strA = "13";
                            CompareAllNS(DataReader, strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                            do
                            {
                                CompareAllNS(DataReader, strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                            } while (DataReader.MoveToNextAttribute() == true);
                            strA = null;
                        }
                        else if (DataReader.Name == "check14")
                        {
                            strDef = "14";
                            CompareAllNS(DataReader, strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                            if (DataReader.NodeType == XmlNodeType.EndElement)
                            {
                                strDef = "1";
                            }
                        }
                        else
                            CompareAllNS(DataReader, strDef, strA, strB, strC, strD, strE, strF, strG, strH);

                        if (DataReader.Name == "NAMESPACE3" && DataReader.NodeType == XmlNodeType.EndElement)
                        {
                            CompareAllNS(DataReader, strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                            DataReader.Read();
                            break;
                        }
                    }
                }

                //[Variation("LookupNamespace on whitespace node PreserveWhitespaces = true", Priority = 0)]
                public void LookupNamespace8()
                {
                    string strxml = "<ROOT xmlns:p='1'>\n<E1/></ROOT>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnNodeType(DataReader, XmlNodeType.Text);
                    string ns = DataReader.LookupNamespace("p");
                    TestLog.Compare(ns, "1", "ln");
                }

                //[Variation("Different prefix on inner element for the same namespace", Priority = 0)]
                public void LookupNamespace9()
                {
                    string ns = "http://www.w3.org/1999/XMLSchema";
                    string filename = Path.Combine("TestData", "XmlReader", "Common", "bug_57723.xml");

                    XmlReader DataReader = GetReader(filename);

                    PositionOnElement(DataReader, "element");
                    TestLog.Compare(DataReader.LookupNamespace("q1"), ns, "q11");
                    TestLog.Compare(DataReader.LookupNamespace("q2"), null, "q21");

                    DataReader.Read();
                    PositionOnElement(DataReader, "element");
                    TestLog.Compare(DataReader.LookupNamespace("q1"), ns, "q12");
                    TestLog.Compare(DataReader.LookupNamespace("q2"), ns, "q22");
                }

                //[Variation("LookupNamespace when Namespaces = false", Priority = 0)]
                public void LookupNamespace10()
                {
                    string strxml = "<ROOT xmlns:p='1'>\n<E1/></ROOT>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "ROOT");
                    TestLog.Compare(DataReader.LookupNamespace("p"), "1", "ln ROOT");
                    PositionOnElement(DataReader, "E1");
                    TestLog.Compare(DataReader.LookupNamespace("p"), "1", "ln E1");
                    DataReader.Read();
                    TestLog.Compare(DataReader.LookupNamespace("p"), "1", "ln /ROOT");
                }
            }

            public partial class TCHasValue : BridgeHelpers
            {
                //[Variation("HasValue On None")]
                public void TestHasValueNodeType_None()
                {
                    XmlReader DataReader = GetReader();
                    bool b = DataReader.HasValue;
                    if (b)
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation("HasValue On Element", Priority = 0)]
                public void TestHasValueNodeType_Element()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.Element))
                    {
                        bool b = DataReader.HasValue;
                        if (b)
                            throw new TestFailedException("HasValue returns True");
                        else
                            return;
                    }
                }

                //[Variation("Get node with a scalar value, verify the value with valid ReadString")]
                public void TestHasValue1()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONEMPTY1");
                    DataReader.Read();
                    TestLog.Compare(DataReader.HasValue, true, "HasValue test");
                }

                //[Variation("HasValue On Attribute", Priority = 0)]
                public void TestHasValueNodeType_Attribute()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.Attribute))
                    {
                        bool b = DataReader.HasValue;
                        if (!b)
                            throw new TestFailedException("HasValue for Attribute returns false");
                        else
                            return;
                    }
                }

                //[Variation("HasValue On Text", Priority = 0)]
                public void TestHasValueNodeType_Text()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.Text))
                    {
                        bool b = DataReader.HasValue;
                        if (!b)
                            throw new TestFailedException("HasValue for Text returns false");
                        else
                            return;
                    }
                }

                //[Variation("HasValue On CDATA", Priority = 0)]
                public void TestHasValueNodeType_CDATA()
                {
                    XmlReader DataReader = GetReader();

                    while (FindNodeType(DataReader, XmlNodeType.CDATA))
                    {
                        bool b = DataReader.HasValue;
                        if (!b)
                            throw new TestFailedException("HasValue for CDATA returns false");
                        else
                            return;
                    }
                }

                //[Variation("HasValue On ProcessingInstruction", Priority = 0)]
                public void TestHasValueNodeType_ProcessingInstruction()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.ProcessingInstruction))
                    {
                        bool b = DataReader.HasValue;
                        if (!b)
                            throw new TestException(TestResult.Failed, "HasValue for PI returns false");
                        else
                            return;
                    }
                }

                //[Variation("HasValue On Comment", Priority = 0)]
                public void TestHasValueNodeType_Comment()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.Comment))
                    {
                        bool b = DataReader.HasValue;
                        if (!b)
                            throw new TestException(TestResult.Failed, "HasValue for Comment returns false");
                        else
                            return;
                    }
                }

                //[Variation("HasValue On DocumentType", Priority = 0)]
                public void TestHasValueNodeType_DocumentType()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.DocumentType))
                    {
                        bool b = DataReader.HasValue;
                        if (!b)
                            throw new TestException(TestResult.Failed, "HasValue returns True");
                        else
                            return;
                    }
                }

                //[Variation("HasValue On Whitespace PreserveWhitespaces = true", Priority = 0)]
                public void TestHasValueNodeType_Whitespace()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.Whitespace))
                    {
                        bool b = DataReader.HasValue;
                        if (!b)
                            throw new TestException(TestResult.Failed, "HasValue returns False");
                        else
                            return;
                    }
                }

                //[Variation("HasValue On EndElement")]
                public void TestHasValueNodeType_EndElement()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.EndElement))
                    {
                        bool b = DataReader.HasValue;
                        if (b)
                            throw new TestException(TestResult.Failed, "HasValue returns True");
                        else
                            return;
                    }
                }

                //[Variation("HasValue On XmlDeclaration", Priority = 0)]
                public void TestHasValueNodeType_XmlDeclaration()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.XmlDeclaration))
                    {
                        bool b = DataReader.HasValue;
                        if (!b)
                            throw new TestException(TestResult.Failed, "HasValue returns False");
                        else
                            return;
                    }
                }

                //[Variation("HasValue On EntityReference")]
                public void TestHasValueNodeType_EntityReference()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.EntityReference))
                    {
                        bool b = DataReader.HasValue;
                        if (b)
                            throw new TestException(TestResult.Failed, "HasValue returns True");
                        else
                            return;
                    }
                }

                //[Variation("HasValue On EndEntity")]
                public void TestHasValueNodeType_EndEntity()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.EndEntity))
                    {
                        bool b = DataReader.HasValue;
                        if (b)
                            throw new TestException(TestResult.Failed, "HasValue returns True");
                        else
                            return;
                    }
                }

                //[Variation("PI Value containing surrogates", Priority = 0)]
                public void v13()
                {
                    string strxml = "<root><?target \uD800\uDC00\uDBFF\uDFFF?></root>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    DataReader.Read();
                    DataReader.Read();
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.ProcessingInstruction, "nt");
                    TestLog.Compare(DataReader.Value, "\uD800\uDC00\uDBFF\uDFFF", "piv");
                }
            }

            public partial class TCIsEmptyElement2 : BridgeHelpers
            {
                //[Variation("Set and Get an element that ends with />", Priority = 0)]
                public void TestEmpty1()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY1");
                    bool b = DataReader.IsEmptyElement;
                    if (!b)
                        throw new TestException(TestResult.Failed, "DataReader is NOT_EMPTY, supposed to be EMPTY");
                }

                //[Variation("Set and Get an element with an attribute that ends with />", Priority = 0)]
                public void TestEmpty2()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY2");
                    bool b = DataReader.IsEmptyElement;
                    if (!b)
                        throw new TestException(TestResult.Failed, "DataReader is NOT_EMPTY, supposed to be EMPTY");
                }

                //[Variation("Set and Get an element that ends without />", Priority = 0)]
                public void TestEmpty3()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONEMPTY1");
                    bool b = DataReader.IsEmptyElement;
                    if (b)
                        throw new TestException(TestResult.Failed, "DataReader is EMPTY, supposed to be NOT_EMPTY");
                }

                //[Variation("Set and Get an element with an attribute that ends with />", Priority = 0)]
                public void TestEmpty4()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONEMPTY2");
                    bool b = DataReader.IsEmptyElement;
                    if (b)
                        throw new TestException(TestResult.Failed, "DataReader is EMPTY, supposed to be NOT_EMPTY");
                }

                //[Variation("IsEmptyElement On Element", Priority = 0)]
                public void TestEmptyNodeType_Element()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.Element))
                    {
                        bool b = DataReader.IsEmptyElement;
                        if (b)
                            throw new TestException(TestResult.Failed, "IsEmptyElement returns True");
                        else
                            return;
                    }
                }

                //[Variation("IsEmptyElement On None")]
                public void TestEmptyNodeType_None()
                {
                    XmlReader DataReader = GetReader();
                    bool b = DataReader.IsEmptyElement;
                    if (b)
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation("IsEmptyElement On Text")]
                public void TestEmptyNodeType_Text()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.Text))
                    {
                        bool b = DataReader.IsEmptyElement;
                        if (b)
                            throw new TestException(TestResult.Failed, "IsEmptyElement returns True");
                        else
                            return;
                    }
                }

                //[Variation("IsEmptyElement On CDATA")]
                public void TestEmptyNodeType_CDATA()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.CDATA))
                    {
                        bool b = DataReader.IsEmptyElement;
                        if (b)
                            throw new TestException(TestResult.Failed, "IsEmptyElement returns True");
                        else
                            return;
                    }
                }

                //[Variation("IsEmptyElement On ProcessingInstruction")]
                public void TestEmptyNodeType_ProcessingInstruction()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.ProcessingInstruction))
                    {
                        bool b = DataReader.IsEmptyElement;
                        if (b)
                            throw new TestException(TestResult.Failed, "IsEmptyElement returns True");
                        else
                            return;
                    }
                }

                //[Variation("IsEmptyElement On Comment")]
                public void TestEmptyNodeType_Comment()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.Comment))
                    {
                        bool b = DataReader.IsEmptyElement;
                        if (!b)
                            return;
                        else
                            throw new TestException(TestResult.Failed, "IsEmptyElement returns True");
                    }
                }

                //[Variation("IsEmptyElement On DocumentType")]
                public void TestEmptyNodeType_DocumentType()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.DocumentType))
                    {
                        bool b = DataReader.IsEmptyElement;
                        if (!b)
                            return;
                        else
                            throw new TestException(TestResult.Failed, "IsEmptyElement returns True");
                    }
                }

                //[Variation("IsEmptyElement On Whitespace PreserveWhitespaces = true")]
                public void TestEmptyNodeType_Whitespace()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.Whitespace))
                    {
                        bool b = DataReader.IsEmptyElement;
                        if (!b)
                            return;
                        else
                            throw new TestException(TestResult.Failed, "IsEmptyElement returns True");
                    }
                }

                //[Variation("IsEmptyElement On EndElement")]
                public void TestEmptyNodeType_EndElement()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.EndElement))
                    {
                        bool b = DataReader.IsEmptyElement;
                        if (!b)
                            return;
                        else
                            throw new TestException(TestResult.Failed, "IsEmptyElement returns True");
                    }
                }

                //[Variation("IsEmptyElement On EntityReference")]
                public void TestEmptyNodeType_EntityReference()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.EntityReference))
                    {
                        bool b = DataReader.IsEmptyElement;
                        if (!b)
                            return;
                        else
                            throw new TestException(TestResult.Failed, "IsEmptyElement returns True");
                    }
                }

                //[Variation("IsEmptyElement On EndEntity")]
                public void TestEmptyNodeType_EndEntity()
                {
                    XmlReader DataReader = GetReader();
                    while (FindNodeType(DataReader, XmlNodeType.EndEntity))
                    {
                        bool b = DataReader.IsEmptyElement;
                        if (!b)
                            return;
                        else
                            throw new TestException(TestResult.Failed, "IsEmptyElement returns True");
                    }
                }
            }

            public partial class TCXmlSpace : BridgeHelpers
            {
                //[Variation("XmlSpace test within EmptyTag")]
                public void TestXmlSpace1()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY_XMLSPACE");

                    do
                    {
                        TestLog.Compare(DataReader.XmlSpace, XmlSpace.Default, "Compare XmlSpace with Default");
                    } while (DataReader.MoveToNextAttribute() == true);
                }

                //[Variation("Xmlspace test within a scope (no nested element)", Priority = 0)]
                public void TestXmlSpace2()
                {
                    XmlReader DataReader = GetReader();
                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "XMLSPACE1") break;
                        TestLog.Compare(DataReader.XmlSpace, XmlSpace.None, "Compare XmlSpace with None");
                    }

                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "XMLSPACE1" && (DataReader.NodeType == XmlNodeType.EndElement)) break;
                        TestLog.Compare(DataReader.XmlSpace, XmlSpace.Default, "Compare XmlSpace with Default");
                    }
                }

                //[Variation("Xmlspace test within a scope (with nested element)", Priority = 0)]
                public void TestXmlSpace3()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "XMLSPACE2");
                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "NOSPACE") break;
                        TestLog.Compare(DataReader.XmlSpace, XmlSpace.Preserve, "Compare XmlSpace with Preserve");
                    }
                    TestLog.Compare(DataReader.XmlSpace, XmlSpace.None, "Compare XmlSpace outside scope");
                }

                //[Variation("Xmlspace test immediately outside the XmlSpace scope")]
                public void TestXmlSpace4()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NOSPACE");
                    TestLog.Compare(DataReader.XmlSpace, XmlSpace.None, "Compare XmlSpace with None");
                }

                //[Variation("XmlSpace test with multiple XmlSpace declaration")]
                public void TestXmlSpace5()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "XMLSPACE2A");

                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "XMLSPACE3") break;
                        TestLog.Compare(DataReader.XmlSpace, XmlSpace.Default, "Compare XmlSpace with Default");
                    }

                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "XMLSPACE4")
                        {
                            while (true == DataReader.Read())
                            {
                                TestLog.Compare(DataReader.XmlSpace, XmlSpace.Default, "Compare XmlSpace with Default");
                                if (DataReader.Name == "XMLSPACE4" && DataReader.NodeType == XmlNodeType.EndElement)
                                {
                                    DataReader.Read();
                                    break;
                                }
                            }
                        }

                        TestLog.Compare(DataReader.XmlSpace, XmlSpace.Preserve, "Compare XmlSpace with Preserve");

                        if (DataReader.Name == "XMLSPACE3" && DataReader.NodeType == XmlNodeType.EndElement)
                        {
                            DataReader.Read();
                            break;
                        }
                    }

                    do
                    {
                        TestLog.Compare(DataReader.XmlSpace, XmlSpace.Default, "Compare XmlSpace with Default");
                        if (DataReader.Name == "XMLSPACE2A" && DataReader.NodeType == XmlNodeType.EndElement)
                        {
                            DataReader.Read();
                            break;
                        }
                    } while (true == DataReader.Read());

                    TestLog.Compare(DataReader.XmlSpace, XmlSpace.None, "Compare XmlSpace outside scope");
                }
            }

            public partial class TCXmlLang : BridgeHelpers
            {
                //[Variation("XmlLang test within EmptyTag")]
                public void TestXmlLang1()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY_XMLLANG");
                    do
                    {
                        TestLog.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with en-US");
                    } while (DataReader.MoveToNextAttribute() == true);
                }

                //[Variation("XmlLang test within a scope (no nested element)", Priority = 0)]
                public void TestXmlLang2()
                {
                    XmlReader DataReader = GetReader();
                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "XMLLANG0") break;
                        TestLog.Compare(DataReader.XmlLang, String.Empty, "Compare XmlLang with String.Empty");
                    }

                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "XMLLANG0" && (DataReader.NodeType == XmlNodeType.EndElement)) break;

                        if (DataReader.NodeType == XmlNodeType.EntityReference)
                        {
                            TestLog.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with EntityRef");

                            if (DataReader.CanResolveEntity)
                            {
                                DataReader.ResolveEntity();
                                TestLog.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang after ResolveEntity");
                                while (DataReader.Read() && DataReader.NodeType != XmlNodeType.EndEntity)
                                {
                                    TestLog.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang While Read ");
                                }
                                if (DataReader.NodeType == XmlNodeType.EndEntity)
                                {
                                    TestLog.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang at EndEntity ");
                                }
                            }
                        }
                        else
                            TestLog.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with Preserve");
                    }
                }

                //[Variation("XmlLang test within a scope (with nested element)", Priority = 0)]
                public void TestXmlLang3()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "XMLLANG1");

                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "NOXMLLANG") break;
                        TestLog.Compare(DataReader.XmlLang, "en-GB", "Compare XmlLang with en-GB");
                    }
                }

                //[Variation("XmlLang test immediately outside the XmlLang scope")]
                public void TestXmlLang4()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NOXMLLANG");
                    TestLog.Compare(DataReader.XmlLang, String.Empty, "Compare XmlLang with EmptyString");
                }

                //[Variation("XmlLang test with multiple XmlLang declaration")]
                public void TestXmlLang5()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "XMLLANG2");
                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "XMLLANG1") break;
                        TestLog.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with Preserve");
                    }

                    while (true == DataReader.Read())
                    {
                        if (DataReader.Name == "XMLLANG0")
                        {
                            while (true == DataReader.Read())
                            {
                                TestLog.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with en-US");
                                if (DataReader.Name == "XMLLANG0" && DataReader.NodeType == XmlNodeType.EndElement)
                                {
                                    DataReader.Read();
                                    break;
                                }
                            }
                        }

                        TestLog.Compare(DataReader.XmlLang, "en-GB", "Compare XmlLang with en_GB");

                        if (DataReader.Name == "XMLLANG1" && DataReader.NodeType == XmlNodeType.EndElement)
                        {
                            DataReader.Read();
                            break;
                        }
                    }

                    do
                    {
                        TestLog.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with en-US");
                        if (DataReader.Name == "XMLLANG2" && DataReader.NodeType == XmlNodeType.EndElement)
                        {
                            DataReader.Read();
                            break;
                        }
                    } while (true == DataReader.Read());
                }

                // XML 1.0 SE
                //[Variation("XmlLang valid values", Priority = 0)]
                public void TestXmlLang6()
                {
                    const string ST_VALIDXMLLANG = "VALIDXMLLANG";
                    string[] aValidLang = { "a", "", "ab-cd-", "a b-cd" };

                    XmlReader DataReader = GetReader();

                    for (int i = 0; i < aValidLang.Length; i++)
                    {
                        string strelem = ST_VALIDXMLLANG + i;
                        PositionOnElement(DataReader, strelem);
                        //DataReader.Read();
                        TestLog.Compare(DataReader.XmlLang, aValidLang[i], "XmlLang");
                    }
                }

                // XML 1.0 SE
                //[Variation("More XmlLang valid values")]
                public void TestXmlTextReaderLang1()
                {
                    string[] aValidLang = { "", "ab-cd-", "abcdefghi", "ab-cdefghijk", "a b-cd", "ab-c d" };

                    for (int i = 0; i < aValidLang.Length; i++)
                    {
                        string strxml = String.Format("<ROOT xml:lang='{0}'/>", aValidLang[i]);

                        XmlReader DataReader = GetReaderStr(strxml);

                        while (DataReader.Read()) ;
                    }
                }
            }

            public partial class TCSkip : BridgeHelpers
            {
                public bool VerifySkipOnNodeType(XmlNodeType testNodeType)
                {
                    bool bPassed = false;
                    XmlNodeType actNodeType;
                    String strActName;
                    String strActValue;

                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, testNodeType);
                    DataReader.Read();
                    actNodeType = DataReader.NodeType;
                    strActName = DataReader.Name;
                    strActValue = DataReader.Value;

                    DataReader = GetReader();
                    PositionOnNodeType(DataReader, testNodeType);
                    DataReader.Skip();
                    bPassed = VerifyNode(DataReader, actNodeType, strActName, strActValue);

                    return bPassed;
                }

                //[Variation("Call Skip on empty element", Priority = 0)]
                public void TestSkip1()
                {
                    bool bPassed = false;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "SKIP1");

                    DataReader.Skip();

                    bPassed = VerifyNode(DataReader, XmlNodeType.Element, "AFTERSKIP1", String.Empty);

                    BoolToLTMResult(bPassed);
                }

                //[Variation("Call Skip on element", Priority = 0)]
                public void TestSkip2()
                {
                    bool bPassed = false;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "SKIP2");

                    DataReader.Skip();

                    bPassed = VerifyNode(DataReader, XmlNodeType.Element, "AFTERSKIP2", String.Empty);

                    BoolToLTMResult(bPassed);
                }

                //[Variation("Call Skip on element with content", Priority = 0)]
                public void TestSkip3()
                {
                    bool bPassed = false;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "SKIP3");

                    DataReader.Skip();

                    bPassed = VerifyNode(DataReader, XmlNodeType.Element, "AFTERSKIP3", String.Empty);

                    BoolToLTMResult(bPassed);
                }

                //[Variation("Call Skip on text node (leave node)", Priority = 0)]
                public void TestSkip4()
                {
                    bool bPassed = false;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "SKIP3");
                    PositionOnElement(DataReader, "ELEM2");
                    DataReader.Read();
                    bPassed = (DataReader.NodeType == XmlNodeType.Text);

                    DataReader.Skip();

                    bPassed = VerifyNode(DataReader, XmlNodeType.EndElement, "ELEM2", String.Empty) && bPassed;

                    BoolToLTMResult(bPassed);
                }

                //[Variation("Call Skip in while read loop", Priority = 0)]
                public void skip307543()
                {
                    XmlReader DataReader = GetReader(Path.Combine("TestData", "XmlReader", "Common", "skip307543.xml"));
                    while (DataReader.Read())
                        DataReader.Skip();
                }

                //[Variation("Call Skip on text node with another element: <elem2>text<elem3></elem3></elem2>")]
                public void TestSkip5()
                {
                    bool bPassed = false;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "SKIP4");
                    PositionOnElement(DataReader, "ELEM2");
                    DataReader.Read();
                    bPassed = (DataReader.NodeType == XmlNodeType.Text);

                    DataReader.Skip();

                    bPassed = VerifyNode(DataReader, XmlNodeType.Element, "ELEM3", String.Empty) && bPassed;

                    BoolToLTMResult(bPassed);
                }

                //[Variation("Call Skip on attribute", Priority = 0)]
                public void TestSkip6()
                {
                    bool bPassed = false;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_ENTTEST_NAME);
                    bPassed = DataReader.MoveToFirstAttribute();

                    DataReader.Skip();

                    bPassed = VerifyNode(DataReader, XmlNodeType.Element, "ENTITY2", String.Empty) && bPassed;

                    BoolToLTMResult(bPassed);
                }

                //[Variation("Call Skip on text node of attribute")]
                public void TestSkip7()
                {
                    bool bPassed = false;

                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, ST_ENTTEST_NAME);
                    bPassed = DataReader.MoveToFirstAttribute();
                    bPassed = DataReader.ReadAttributeValue() && bPassed;
                    bPassed = (DataReader.NodeType == XmlNodeType.Text) && bPassed;

                    DataReader.Skip();

                    bPassed = VerifyNode(DataReader, XmlNodeType.Element, "ENTITY2", String.Empty) && bPassed;

                    BoolToLTMResult(bPassed);
                }

                //[Variation("Call Skip on CDATA", Priority = 0)]
                public void TestSkip8()
                {
                    XmlReader DataReader = GetReader();
                    BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.CDATA));
                }

                //[Variation("Call Skip on Processing Instruction", Priority = 0)]
                public void TestSkip9()
                {
                    BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.ProcessingInstruction));
                }

                //[Variation("Call Skip on Comment", Priority = 0)]
                public void TestSkip10()
                {
                    BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.Comment));
                }

                //[Variation("Call Skip on Whitespace", Priority = 0)]
                public void TestSkip12()
                {
                    XmlReader DataReader = GetReader();
                    BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.Whitespace));
                }

                //[Variation("Call Skip on EndElement", Priority = 0)]
                public void TestSkip13()
                {
                    BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.EndElement));
                }

                //[Variation("Call Skip on root Element")]
                public void TestSkip14()
                {
                    bool bPassed;

                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Element);

                    DataReader.Skip();

                    bPassed = VerifyNode(DataReader, XmlNodeType.None, String.Empty, String.Empty);

                    BoolToLTMResult(bPassed);
                }

                //[Variation("XmlTextReader ArgumentOutOfRangeException when handling ampersands")]
                public void XmlTextReaderDoesNotThrowWhenHandlingAmpersands()
                {
                    string xmlStr = @"<a>
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
&gt; 
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
	fffffffffffffffffffffffffffffffffffffff
&amp;
</a>
";
                    XmlReader DataReader = GetReader(new StringReader(xmlStr));
                    PositionOnElement(DataReader, "a");
                    DataReader.Skip();
                }
            }

            public partial class TCIsDefault : BridgeHelpers
            {
            }

            public partial class TCBaseURI : BridgeHelpers
            {
                //[Variation("BaseURI for element node", Priority = 0)]
                public void TestBaseURI1()
                {
                    bool bPassed = false;
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Element);
                    bPassed = TestLog.Equals(DataReader.BaseURI, String.Empty, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("BaseURI for attribute node", Priority = 0)]
                public void TestBaseURI2()
                {
                    bool bPassed = false;
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Attribute);
                    bPassed = TestLog.Equals(DataReader.BaseURI, String.Empty, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("BaseURI for text node", Priority = 0)]
                public void TestBaseURI3()
                {
                    bool bPassed = false;
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Text);
                    bPassed = TestLog.Equals(DataReader.BaseURI, String.Empty, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("BaseURI for CDATA node")]
                public void TestBaseURI4()
                {
                    XmlReader DataReader = GetReader();
                    bool bPassed = false;
                    PositionOnNodeType(DataReader, XmlNodeType.CDATA);
                    bPassed = TestLog.Equals(DataReader.BaseURI, String.Empty, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("BaseURI for PI node")]
                public void TestBaseURI6()
                {
                    bool bPassed = false;
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.ProcessingInstruction);
                    bPassed = TestLog.Equals(DataReader.BaseURI, String.Empty, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("BaseURI for Comment node")]
                public void TestBaseURI7()
                {
                    bool bPassed = false;
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Comment);
                    bPassed = TestLog.Equals(DataReader.BaseURI, String.Empty, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("BaseURI for Whitespace node PreserveWhitespaces = true")]
                public void TestBaseURI9()
                {
                    bool bPassed = false;
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.Whitespace);
                    bPassed = TestLog.Equals(DataReader.BaseURI, String.Empty, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("BaseURI for EndElement node")]
                public void TestBaseURI10()
                {
                    bool bPassed = false;
                    XmlReader DataReader = GetReader();
                    PositionOnNodeType(DataReader, XmlNodeType.EndElement);
                    bPassed = TestLog.Equals(DataReader.BaseURI, String.Empty, Variation.Desc);
                    BoolToLTMResult(bPassed);
                }

                //[Variation("BaseURI for external General Entity")]
                public void TestTextReaderBaseURI4()
                {
                    bool bPassed = false;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ENTITY5");
                    DataReader.Read();
                    bPassed = TestLog.Equals(DataReader.BaseURI, String.Empty, "Before ResolveEntity");
                    bPassed = VerifyNode(DataReader, XmlNodeType.Text, String.Empty, ST_GEN_ENT_VALUE) && bPassed;
                    bPassed = TestLog.Equals(DataReader.BaseURI, String.Empty, "After ResolveEntity");
                    BoolToLTMResult(bPassed);
                }
            }
        }
    }
}
