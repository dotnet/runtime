// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeReaderFunctionalTests : TestModule
    {
        public partial class XNodeReaderTests : XLinqTestCase
        {
            public partial class TCAttributeAccess : BridgeHelpers
            {
                //[Variation("Attribute Access test using ordinal (Ascending Order)", Priority = 0)]
                public void TestAttributeAccess1()
                {
                    XmlReader DataReader = GetReader();
                    string[] astr = new string[10];
                    string n;
                    string qname;

                    PositionOnElement(DataReader, "ACT0");

                    int start = 1;
                    int end = DataReader.AttributeCount;

                    for (int i = start; i < end; i++)
                    {
                        astr[i - 1] = DataReader[i];
                        n = strAttr + (i - 1);

                        qname = "foo:" + n;
                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");

                        TestLog.Compare(DataReader[n, strNamespace], DataReader.GetAttribute(n, strNamespace), "Compare this(name,strNamespace) with GetAttribute(name,strNamespace)");

                        TestLog.Compare(DataReader[i], DataReader[n, strNamespace], "Compare this(i) with this(name,strNamespace)");
                        TestLog.Compare(DataReader.MoveToAttribute(n, strNamespace), true, "MoveToAttribute(name,strNamespace)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(n, strNamespace), "Compare MoveToAttribute(name,strNamespace) with GetAttribute(name,strNamespace)");

                        TestLog.Compare(DataReader[i], DataReader[qname], "Compare this(i) with this(qname)");
                        TestLog.Compare(DataReader.MoveToAttribute(qname), true, "MoveToAttribute(qname)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(qname), "Compare MoveToAttribute(qname) with GetAttribute(qname)");
                    }

                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        TestLog.Compare(astr[i], DataReader.GetAttribute(i), "Compare value with GetAttribute");
                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");

                        n = strAttr + i;
                        TestLog.Compare(DataReader[n], DataReader.GetAttribute(n), "Compare this(name) with GetAttribute(name)");

                        TestLog.Compare(DataReader[i], DataReader[n], "Compare this(i) with this(name)");
                        TestLog.Compare(DataReader.MoveToAttribute(n), true, "MoveToAttribute(name)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(n), "Compare MoveToAttribute(name) with GetAttribute(name)");
                    }
                }

                //[Variation("Attribute Access test using ordinal (Descending Order)")]
                public void TestAttributeAccess2()
                {
                    XmlReader DataReader = GetReader();
                    string[] astr = new string[10];
                    string n;
                    string qname;

                    PositionOnElement(DataReader, "ACT0");
                    int start = 1;
                    int end = DataReader.AttributeCount;

                    for (int i = end - 1; i >= start; i--)
                    {
                        astr[i - 1] = DataReader[i];
                        n = strAttr + (i - 1);

                        qname = "foo:" + n;

                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");

                        TestLog.Compare(DataReader[n, strNamespace], DataReader.GetAttribute(n, strNamespace), "Compare this(name,strNamespace) with GetAttribute(name,strNamespace)");

                        TestLog.Compare(DataReader[i], DataReader[n, strNamespace], "Compare this(i) with this(name,strNamespace)");
                        TestLog.Compare(DataReader.MoveToAttribute(n, strNamespace), true, "MoveToAttribute(name,strNamespace)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(n, strNamespace), "Compare MoveToAttribute(name,strNamespace) with GetAttribute(name,strNamespace)");

                        TestLog.Compare(DataReader[qname], DataReader.GetAttribute(qname), "Compare this(qname) with GetAttribute(qname)");

                        TestLog.Compare(DataReader[i], DataReader[qname], "Compare this(i) with this(qname)");
                        TestLog.Compare(DataReader.MoveToAttribute(qname), true, "MoveToAttribute(qname)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(qname), "Compare MoveToAttribute(qname) with GetAttribute(qname)");
                    }

                    PositionOnElement(DataReader, "ACT1");
                    for (int i = (DataReader.AttributeCount - 1); i > 0; i--)
                    {
                        n = strAttr + i;

                        TestLog.Compare(astr[i], DataReader.GetAttribute(i), "Compare value with GetAttribute");
                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");

                        TestLog.Compare(DataReader[n], DataReader.GetAttribute(n), "Compare this(name) with GetAttribute(name)");

                        TestLog.Compare(DataReader[i], DataReader[n], "Compare this(i) with this(name)");
                        TestLog.Compare(DataReader.MoveToAttribute(n), true, "MoveToAttribute(name)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(n), "Compare MoveToAttribute(name) with GetAttribute(name)");
                    }
                }

                //[Variation("Attribute Access test using ordinal (Odd number)", Priority = 0)]
                public void TestAttributeAccess3()
                {
                    XmlReader DataReader = GetReader();
                    string[] astr = new string[10];
                    string n;
                    string qname;

                    PositionOnElement(DataReader, "ACT0");
                    int start = 1;
                    int end = DataReader.AttributeCount;

                    for (int i = start; i < end; i += 2)
                    {
                        astr[i - 1] = DataReader[i];
                        n = strAttr + (i - 1);

                        qname = "foo:" + n;

                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");

                        TestLog.Compare(DataReader[n, strNamespace], DataReader.GetAttribute(n, strNamespace), "Compare this(name,strNamespace) with GetAttribute(name,strNamespace)");

                        TestLog.Compare(DataReader[i], DataReader[n, strNamespace], "Compare this(i) with this(name,strNamespace)");
                        TestLog.Compare(DataReader.MoveToAttribute(n, strNamespace), true, "MoveToAttribute(name,strNamespace)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(n, strNamespace), "Compare MoveToAttribute(name,strNamespace) with GetAttribute(name,strNamespace)");

                        TestLog.Compare(DataReader[qname], DataReader.GetAttribute(qname), "Compare this(qname) with GetAttribute(qname)");

                        TestLog.Compare(DataReader[i], DataReader[qname], "Compare this(i) with this(qname)");
                        TestLog.Compare(DataReader.MoveToAttribute(qname), true, "MoveToAttribute(qname)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(qname), "Compare MoveToAttribute(qname) with GetAttribute(qname)");
                    }

                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i += 2)
                    {
                        TestLog.Compare(astr[i], DataReader.GetAttribute(i), "Compare value with GetAttribute");
                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");

                        n = strAttr + i;
                        TestLog.Compare(DataReader[n], DataReader.GetAttribute(n), "Compare this(name) with GetAttribute(name)");

                        TestLog.Compare(DataReader[i], DataReader[n], "Compare this(i) with this(name)");
                        TestLog.Compare(DataReader.MoveToAttribute(n), true, "MoveToAttribute(name)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(n), "Compare MoveToAttribute(name) with GetAttribute(name)");
                    }
                }

                //[Variation("Attribute Access test using ordinal (Even number)")]
                public void TestAttributeAccess4()
                {
                    XmlReader DataReader = GetReader();
                    string[] astr = new string[10];
                    string n;
                    string qname;

                    PositionOnElement(DataReader, "ACT0");
                    int start = 1;
                    int end = DataReader.AttributeCount;

                    for (int i = start; i < end; i += 3)
                    {
                        astr[i - 1] = DataReader[i];
                        n = strAttr + (i - 1);

                        qname = "foo:" + n;

                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");

                        TestLog.Compare(DataReader[n, strNamespace], DataReader.GetAttribute(n, strNamespace), "Compare this(name,strNamespace) with GetAttribute(name,strNamespace)");

                        TestLog.Compare(DataReader[i], DataReader[n, strNamespace], "Compare this(i) with this(name,strNamespace)");
                        TestLog.Compare(DataReader.MoveToAttribute(n, strNamespace), true, "MoveToAttribute(name,strNamespace)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(n, strNamespace), "Compare MoveToAttribute(name,strNamespace) with GetAttribute(name,strNamespace)");

                        TestLog.Compare(DataReader[qname], DataReader.GetAttribute(qname), "Compare this(qname) with GetAttribute(qname)");

                        TestLog.Compare(DataReader[i], DataReader[qname], "Compare this(i) with this(qname)");
                        TestLog.Compare(DataReader.MoveToAttribute(qname), true, "MoveToAttribute(qname)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(qname), "Compare MoveToAttribute(qname) with GetAttribute(qname)");
                    }

                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i += 3)
                    {
                        TestLog.Compare(astr[i], DataReader.GetAttribute(i), "Compare value with GetAttribute");
                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");

                        n = strAttr + i;
                        TestLog.Compare(DataReader[n], DataReader.GetAttribute(n), "Compare this(name) with GetAttribute(name)");

                        TestLog.Compare(DataReader[i], DataReader[n], "Compare this(i) with this(name)");
                        TestLog.Compare(DataReader.MoveToAttribute(n), true, "MoveToAttribute(name)");
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(n), "Compare MoveToAttribute(name) with GetAttribute(name)");
                    }
                }

                //[Variation("Attribute Access with namespace=null")]
                public void TestAttributeAccess5()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    TestLog.Compare(DataReader[strAttr + 1, null], null, "Item");
                    TestLog.Compare(DataReader.Name, "ACT1", "Reader changed position");
                }
            }

            public partial class TCThisName : BridgeHelpers
            {
                //[Variation("This[Name] Verify with GetAttribute(Name)", Priority = 0)]
                public void ThisWithName1()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        TestLog.Compare(DataReader[strName], DataReader.GetAttribute(strName), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
                    }
                }

                //[Variation("This[Name, null] Verify with GetAttribute(Name)")]
                public void ThisWithName2()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        TestLog.Compare(DataReader[strName, null], null, "Ordinal (" + i + "): Should have returned null");
                    }
                }

                //[Variation("This[Name] Verify with GetAttribute(Name,null)")]
                public void ThisWithName3()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        TestLog.Compare(DataReader.GetAttribute(strName, null), null, "Ordinal (" + i + "): Should have returned null");
                    }
                }

                //[Variation("This[Name, NamespaceURI] Verify with GetAttribute(Name, NamespaceURI)", Priority = 0)]
                public void ThisWithName4()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 1; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        TestLog.Compare(DataReader[strName, strNamespace], DataReader.GetAttribute(strName, strNamespace), "Ordinal (" + i + "): Compare GetAttribute(strName,strNamespace) and this[strName,strNamespace]");
                    }
                }

                //[Variation("This[Name, null] Verify not the same as GetAttribute(Name, NamespaceURI)")]
                public void ThisWithName5()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 1; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + (i - 1);
                        TestLog.Compare(DataReader[strName, null], null, "Ordinal (" + i + "): Should have returned null");
                    }
                }

                //[Variation("This[Name, NamespaceURI] Verify not the same as GetAttribute(Name, null)")]
                public void ThisWithName6()
                {
                    string strName;

                    XmlReader DataReader = GetReader();
                    for (int i = 1; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + (i - 1);
                        if (DataReader.GetAttribute(strName, null) == DataReader[strName, strNamespace])
                            throw new TestException(TestResult.Failed, Variation.Desc);
                    }
                }

                //[Variation("This[Name] Verify with MoveToAttribute(Name)", Priority = 0)]
                public void ThisWithName7()
                {
                    string strName;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        DataReader.MoveToAttribute(strName);
                        TestLog.Compare(DataReader.Value, DataReader[strName], "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
                    }
                }

                //[Variation("This[Name, null] Verify with MoveToAttribute(Name)")]
                public void ThisWithName8()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        DataReader.MoveToAttribute(strName);
                        TestLog.Compare(DataReader[strName, null], null, "Ordinal (" + i + "): Should have returned null");
                    }
                }

                //[Variation("This[Name] Verify with MoveToAttribute(Name,null)")]
                public void ThisWithName9()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        TestLog.Compare(DataReader.MoveToAttribute(strName, null), false, "Ordinal (" + i + "): Reader should not have moved");
                    }
                }

                //[Variation("This[Name, NamespaceURI] Verify not the same as MoveToAttribute(Name, null)", Priority = 0)]
                public void ThisWithName10()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 1; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + (i - 1);
                        TestLog.Compare(DataReader.MoveToAttribute(strName, null), false, "Ordinal (" + i + "): Reader should not have moved");
                    }
                }

                //[Variation("This[Name, null] Verify not the same as MoveToAttribute(Name, NamespaceURI)")]
                public void ThisWithName11()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        DataReader.MoveToAttribute(strName, strNamespace);
                        TestLog.Compare(DataReader[strName, null], null, "Ordinal (" + i + "): Should have returned null");
                    }
                }

                //[Variation("This[Name, namespace] Verify not the same as MoveToAttribute(Name, namespace)")]
                public void ThisWithName12()
                {
                    string strName;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 1; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + (i - 1);
                        DataReader.MoveToAttribute(strName, strNamespace);
                        TestLog.Compare(DataReader.Value, DataReader[strName, strNamespace], "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
                    }
                }

                //[Variation("This(String.Empty)")]
                public void ThisWithName13()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY1");
                    TestLog.Compare(DataReader[string.Empty], null, "Should have returned null");
                }

                //[Variation("This[String.Empty,String.Empty]")]
                public void ThisWithName14()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY1");
                    TestLog.Compare(DataReader[string.Empty, string.Empty], null, "Should have returned null");
                }

                //[Variation("This[QName] Verify with GetAttribute(Name, NamespaceURI)", Priority = 0)]
                public void ThisWithName15()
                {
                    string strName;
                    string qname;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 1; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        qname = "foo:" + strName;
                        TestLog.Compare(DataReader[qname], DataReader.GetAttribute(strName, strNamespace), "Ordinal (" + i + "): Compare GetAttribute(strName,strNamespace) and this[qname]");
                        TestLog.Compare(DataReader[qname], DataReader.GetAttribute(qname), "Ordinal (" + i + "): Compare GetAttribute(qname) and this[qname]");
                    }
                }

                //[Variation("This[QName] invalid Qname")]
                public void ThisWithName16()
                {
                    string strName;
                    string qname;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");

                    int i = 1;
                    strName = strAttr + i;
                    qname = "foo1:" + strName;
                    TestLog.Compare(DataReader.MoveToAttribute(qname), false, "MoveToAttribute(invalid qname)");
                    TestLog.Compare(DataReader[qname], null, "Compare this[invalid qname] with null");
                    TestLog.Compare(DataReader.GetAttribute(qname), null, "Compare GetAttribute(invalid qname) with null");
                }
            }

            public partial class TCMoveToAttributeReader : BridgeHelpers
            {
                //[Variation("MoveToAttribute(String.Empty)")]
                public void MoveToAttributeWithName1()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY1");
                    TestLog.Compare(DataReader.MoveToAttribute(string.Empty), false, "Should have returned false");
                    TestLog.Compare(DataReader.Value, string.Empty, "Compare MoveToAttribute with String.Empty");
                }

                //[Variation("MoveToAttribute(String.Empty,String.Empty)")]
                public void MoveToAttributeWithName2()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY1");
                    TestLog.Compare(DataReader.MoveToAttribute(string.Empty, string.Empty), false, "Compare the call to MoveToAttribute");
                    TestLog.Compare(DataReader.Value, string.Empty, "Compare MoveToAttribute(strName)");
                }
            }

            //[TestCase(Name = "GetAttributeOrdinal", Desc = "GetAttributeOrdinal")]
            public partial class TCGetAttributeOrdinal : BridgeHelpers
            {
                //[Variation("GetAttribute(i) Verify with This[i] - Double Quote", Priority = 0)]
                public void GetAttributeWithGetAttrDoubleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare GetAttribute(i) and this[i]");
                    }
                }

                //[Variation("GetAttribute[i] Verify with This[i] - Single Quote")]
                public void OrdinalWithGetAttrSingleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare GetAttribute(i) and this[i]");
                    }
                }

                //[Variation("GetAttribute(i) Verify with MoveToAttribute[i] - Double Quote", Priority = 0)]
                public void GetAttributeWithMoveAttrDoubleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        string str = DataReader.GetAttribute(i);

                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                        TestLog.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
                    }
                }

                //[Variation("GetAttribute(i) Verify with MoveToAttribute[i] - Single Quote")]
                public void GetAttributeWithMoveAttrSingleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        string str = DataReader.GetAttribute(i);

                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader[i], "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                        TestLog.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
                    }
                }

                //[Variation("GetAttribute(i) NegativeOneOrdinal", Priority = 0)]
                public void NegativeOneOrdinal()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    string str = DataReader.GetAttribute(-1);
                }

                //[Variation("GetAttribute(i) FieldCountOrdinal")]
                public void FieldCountOrdinal()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    string str = DataReader.GetAttribute(DataReader.AttributeCount);
                }

                //[Variation("GetAttribute(i) OrdinalPlusOne", Priority = 0)]
                public void OrdinalPlusOne()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    string str = DataReader.GetAttribute(DataReader.AttributeCount + 1);
                }

                //[Variation("GetAttribute(i) OrdinalMinusOne")]
                public void OrdinalMinusOne()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    string str = DataReader.GetAttribute(-2);
                }
            }

            //[TestCase(Name = "GetAttributeName", Desc = "GetAttributeName")]
            public partial class TCGetAttributeName : BridgeHelpers
            {
                //[Variation("GetAttribute(Name) Verify with This[Name]", Priority = 0)]
                public void GetAttributeWithName1()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        TestLog.Compare(DataReader[strName], DataReader.GetAttribute(strName), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
                    }
                }

                //[Variation("GetAttribute(Name, null) Verify with This[Name]")]
                public void GetAttributeWithName2()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        TestLog.Compare(DataReader[strName], DataReader.GetAttribute(strName), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
                    }
                }

                //[Variation("GetAttribute(Name) Verify with This[Name,null]")]
                public void GetAttributeWithName3()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        TestLog.Compare(DataReader[strName], DataReader.GetAttribute(strName), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
                    }
                }

                //[Variation("GetAttribute(Name, NamespaceURI) Verify with This[Name, NamespaceURI]", Priority = 0)]
                public void GetAttributeWithName4()
                {
                    string strName;
                    string qname;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 1; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        qname = "foo:" + strName;
                        TestLog.Compare(DataReader[strName, strNamespace], DataReader.GetAttribute(strName, strNamespace), "Ordinal (" + i + "): Compare GetAttribute(strName,strNamespace) and this[strName,strNamespace]");
                        TestLog.Compare(DataReader[qname], DataReader.GetAttribute(strName, strNamespace), "Ordinal (" + i + "): Compare GetAttribute(strName,strNamespace) and this[strName,strNamespace]");
                        TestLog.Compare(DataReader[qname], DataReader.GetAttribute(qname), "Ordinal (" + i + "): Compare GetAttribute(qname) and this[qname]");
                    }
                }

                //[Variation("GetAttribute(Name, null) Verify not the same as This[Name, NamespaceURI]")]
                public void GetAttributeWithName5()
                {
                    string strName;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 1; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + (i - 1);
                        if (DataReader.GetAttribute(strName) == DataReader[strName, strNamespace])
                        {
                            if (DataReader[strName, strNamespace] == string.Empty)
                                throw new TestException(TestResult.Failed, Variation.Desc);
                        }
                    }
                }

                //[Variation("GetAttribute(Name, NamespaceURI) Verify not the same as This[Name, null]")]
                public void GetAttributeWithName6()
                {
                    string strName;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 1; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + (i - 1);
                        if (DataReader.GetAttribute(strName, strNamespace) == DataReader[strName])
                            throw new TestException(TestResult.Failed, Variation.Desc);
                    }
                }

                //[Variation("GetAttribute(Name) Verify with MoveToAttribute(Name)")]
                public void GetAttributeWithName7()
                {
                    string strName;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        DataReader.MoveToAttribute(strName);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(strName), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
                    }
                }

                //[Variation("GetAttribute(Name,null) Verify with MoveToAttribute(Name)", Priority = 1)]
                public void GetAttributeWithName8()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        DataReader.MoveToAttribute(strName);
                        TestLog.Compare(DataReader.GetAttribute(strName, null), null, "Ordinal (" + i + "): Did not return null");
                    }
                }

                //[Variation("GetAttribute(Name) Verify with MoveToAttribute(Name,null)", Priority = 1)]
                public void GetAttributeWithName9()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        TestLog.Compare(DataReader.MoveToAttribute(strName, null), false, "Ordinal (" + i + "): Incorrect move");
                        TestLog.Compare(DataReader.Value, string.Empty, "Ordinal (" + i + "): DataReader.Value should be empty string");
                    }
                }

                //[Variation("GetAttribute(Name, NamespaceURI) Verify not the same as MoveToAttribute(Name, null)")]
                public void GetAttributeWithName10()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 1; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + (i - 1);
                        TestLog.Compare(DataReader.MoveToAttribute(strName, null), false, "Incorrect move");
                        TestLog.Compare(DataReader.Value, string.Empty, "Ordinal (" + i + "): DataReader.Value should be empty string");
                    }
                }

                //[Variation("GetAttribute(Name, null) Verify not the same as MoveToAttribute(Name, NamespaceURI)")]
                public void GetAttributeWithName11()
                {
                    string strName;
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + i;
                        DataReader.MoveToAttribute(strName, strNamespace);
                        TestLog.Compare(DataReader.GetAttribute(strName, null), null, "Should have returned null");
                    }
                }

                //[Variation("GetAttribute(Name, namespace) Verify not the same as MoveToAttribute(Name, namespace)")]
                public void GetAttributeWithName12()
                {
                    string strName;

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 1; i < DataReader.AttributeCount; i++)
                    {
                        strName = strAttr + (i - 1);
                        DataReader.MoveToAttribute(strName, strNamespace);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(strName, strNamespace), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
                    }
                }

                //[Variation("GetAttribute(String.Empty)")]
                public void GetAttributeWithName13()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    TestLog.Compare(DataReader.GetAttribute(string.Empty), null, "Should have returned null");
                }

                //[Variation("GetAttribute(String.Empty,String.Empty)")]
                public void GetAttributeWithName14()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    TestLog.Compare(DataReader.GetAttribute(string.Empty, string.Empty), null, "Compare GetAttribute(strName) and this[strName]");
                }
            }

            //[TestCase(Name = "ThisOrdinal", Desc = "ThisOrdinal")]
            public partial class TCThisOrdinal : BridgeHelpers
            {
                //[Variation("This[i] Verify with GetAttribute[i] - Double Quote", Priority = 0)]
                public void OrdinalWithGetAttrDoubleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare GetAttribute[i] and this[i]");
                    }
                }

                //[Variation("This[i] Verify with GetAttribute[i] - Single Quote")]
                public void OrdinalWithGetAttrSingleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        TestLog.Compare(DataReader[i], DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare GetAttribute[i] and this[i]");
                    }
                }

                //[Variation("This[i] Verify with MoveToAttribute[i] - Double Quote", Priority = 0)]
                public void OrdinalWithMoveAttrDoubleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        string str = DataReader[i];

                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader[i], "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                        TestLog.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
                    }
                }

                //[Variation("This[i] Verify with MoveToAttribute[i] - Single Quote")]
                public void OrdinalWithMoveAttrSingleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        string str = DataReader[i];

                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader[i], "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                        TestLog.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
                    }
                }

                //[Variation("ThisOrdinal NegativeOneOrdinal", Priority = 0)]
                public void NegativeOneOrdinal()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    string str = DataReader[-1];
                }

                //[Variation("ThisOrdinal FieldCountOrdinal")]
                public void FieldCountOrdinal()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    string str = DataReader[DataReader.AttributeCount];
                }

                //[Variation("ThisOrdinal OrdinalPlusOne", Priority = 0)]
                public void OrdinalPlusOne()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    string str = DataReader[DataReader.AttributeCount + 1];
                }

                //[Variation("ThisOrdinal OrdinalMinusOne")]
                public void OrdinalMinusOne()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    string str = DataReader[-2];
                }
            }

            //[TestCase(Name = "MoveToAttributeOrdinal", Desc = "MoveToAttributeOrdinal")]
            public partial class TCMoveToAttributeOrdinal : BridgeHelpers
            {
                //[Variation("MoveToAttribute(i) Verify with This[i] - Double Quote", Priority = 0)]
                public void MoveToAttributeWithGetAttrDoubleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader[i], DataReader.Value, "Ordinal (" + i + "): Compare GetAttribute(i) and this[i]");
                    }
                }

                //[Variation("MoveToAttribute(i) Verify with This[i] - Single Quote")]
                public void MoveToAttributeWithGetAttrSingleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader[i], DataReader.Value, "Ordinal (" + i + "): Compare GetAttribute(i) and this[i]");
                    }
                }

                //[Variation("MoveToAttribute(i) Verify with GetAttribute(i) - Double Quote", Priority = 0)]
                public void MoveToAttributeWithMoveAttrDoubleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        string str = DataReader.GetAttribute(i);

                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                        TestLog.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
                    }
                }

                //[Variation("MoveToAttribute(i) Verify with GetAttribute[i] - Single Quote")]
                public void MoveToAttributeWithMoveAttrSingleQ()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    for (int i = 0; i < DataReader.AttributeCount; i++)
                    {
                        string str = DataReader.GetAttribute(i);

                        DataReader.MoveToAttribute(i);
                        TestLog.Compare(DataReader.Value, DataReader[i], "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                        TestLog.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
                    }
                }

                //[Variation("MoveToAttribute(i) NegativeOneOrdinal", Priority = 0)]
                public void NegativeOneOrdinal()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    try
                    {
                        DataReader.MoveToAttribute(-1);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("MoveToAttribute(i) FieldCountOrdinal")]
                public void FieldCountOrdinal()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");
                    try
                    {
                        DataReader.MoveToAttribute(DataReader.AttributeCount);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("MoveToAttribute(i) OrdinalPlusOne", Priority = 0)]
                public void OrdinalPlusOne()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    try
                    {
                        DataReader.MoveToAttribute(DataReader.AttributeCount + 1);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("MoveToAttribute(i) OrdinalMinusOne")]
                public void OrdinalMinusOne()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    try
                    {
                        DataReader.MoveToAttribute(-2);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }
            }

            //[TestCase(Name = "MoveToFirstAttribute", Desc = "MoveToFirstAttribute")]
            public partial class TCMoveToFirstAttribute : BridgeHelpers
            {
                //[Variation("MoveToFirstAttribute() When AttributeCount=0, <EMPTY1/> ", Priority = 0)]
                public void MoveToFirstAttribute1()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY1");
                    TestLog.Compare(DataReader.MoveToFirstAttribute(), false, Variation.Desc);
                }

                //[Variation("MoveToFirstAttribute() When AttributeCount=0, <NONEMPTY1>ABCDE</NONEMPTY1> ")]
                public void MoveToFirstAttribute2()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONEMPTY1");
                    TestLog.Compare(DataReader.MoveToFirstAttribute(), false, Variation.Desc);
                }

                //[Variation("MoveToFirstAttribute() When iOrdinal=0, with namespace")]
                public void MoveToFirstAttribute3()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");

                    string strFirst;

                    TestLog.Compare(DataReader.MoveToFirstAttribute(), true, Variation.Desc);
                    strFirst = DataReader.Value;

                    TestLog.Compare(strFirst, DataReader.GetAttribute(0), Variation.Desc);
                }

                //[Variation("MoveToFirstAttribute() When iOrdinal=0, without namespace")]
                public void MoveToFirstAttribute4()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");

                    string strFirst;

                    TestLog.Compare(DataReader.MoveToFirstAttribute(), true, Variation.Desc);
                    strFirst = DataReader.Value;

                    TestLog.Compare(strFirst, DataReader.GetAttribute(0), Variation.Desc);
                }

                //[Variation("MoveToFirstAttribute() When iOrdinal=mIddle, with namespace")]
                public void MoveToFirstAttribute5()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");

                    string strFirst;

                    DataReader.MoveToAttribute((int)((DataReader.AttributeCount) / 2));
                    TestLog.Compare(DataReader.MoveToFirstAttribute(), true, Variation.Desc);
                    strFirst = DataReader.Value;

                    TestLog.Compare(strFirst, DataReader.GetAttribute(0), Variation.Desc);
                }

                //[Variation("MoveToFirstAttribute() When iOrdinal=mIddle, without namespace")]
                public void MoveToFirstAttribute6()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");

                    string strFirst;

                    DataReader.MoveToAttribute((int)((DataReader.AttributeCount) / 2));
                    TestLog.Compare(DataReader.MoveToFirstAttribute(), true, Variation.Desc);
                    strFirst = DataReader.Value;

                    TestLog.Compare(strFirst, DataReader.GetAttribute(0), Variation.Desc);
                }

                //[Variation("MoveToFirstAttribute() When iOrdinal=end, with namespace")]
                public void MoveToFirstAttribute7()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");

                    string strFirst;

                    DataReader.MoveToAttribute((DataReader.AttributeCount) - 1);
                    TestLog.Compare(DataReader.MoveToFirstAttribute(), true, Variation.Desc);
                    strFirst = DataReader.Value;

                    TestLog.Compare(strFirst, DataReader.GetAttribute(0), Variation.Desc);
                }

                //[Variation("MoveToFirstAttribute() When iOrdinal=end, without namespace")]
                public void MoveToFirstAttribute8()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");

                    string strFirst;

                    DataReader.MoveToAttribute((DataReader.AttributeCount) - 1);
                    TestLog.Compare(DataReader.MoveToFirstAttribute(), true, Variation.Desc);
                    strFirst = DataReader.Value;

                    TestLog.Compare(strFirst, DataReader.GetAttribute(0), Variation.Desc);
                }
            }

            public partial class TCMoveToNextAttribute : BridgeHelpers
            {
                //[Variation("MoveToNextAttribute() When AttributeCount=0, <EMPTY1/> ", Priority = 0)]
                public void MoveToNextAttribute1()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "EMPTY1");
                    TestLog.Compare(DataReader.MoveToNextAttribute(), false, Variation.Desc);
                }

                //[Variation("MoveToNextAttribute() When AttributeCount=0, <NONEMPTY1>ABCDE</NONEMPTY1> ")]
                public void MoveToNextAttribute2()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "NONEMPTY1");
                    TestLog.Compare(DataReader.MoveToNextAttribute(), false, Variation.Desc);
                }

                //[Variation("MoveToNextAttribute() When iOrdinal=0, with namespace")]
                public void MoveToNextAttribute3()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");

                    string strValue;

                    TestLog.Compare(DataReader.MoveToNextAttribute(), true, Variation.Desc);
                    strValue = DataReader.Value;

                    TestLog.Compare(strValue, DataReader.GetAttribute(0), Variation.Desc);

                    TestLog.Compare(DataReader.MoveToFirstAttribute(), true, Variation.Desc);
                    TestLog.Compare(DataReader.MoveToNextAttribute(), true, Variation.Desc);
                    strValue = DataReader.Value;

                    TestLog.Compare(strValue, DataReader.GetAttribute(1), Variation.Desc);
                }

                //[Variation("MoveToNextAttribute() When iOrdinal=0, without namespace")]
                public void MoveToNextAttribute4()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");
                    string strValue;

                    TestLog.Compare(DataReader.MoveToNextAttribute(), true, Variation.Desc);
                    strValue = DataReader.Value;

                    TestLog.Compare(strValue, DataReader.GetAttribute(0), Variation.Desc);

                    TestLog.Compare(DataReader.MoveToFirstAttribute(), true, Variation.Desc);
                    TestLog.Compare(DataReader.MoveToNextAttribute(), true, Variation.Desc);
                    strValue = DataReader.Value;

                    TestLog.Compare(strValue, DataReader.GetAttribute(1), Variation.Desc);
                }

                //[Variation("MoveToFirstAttribute() When iOrdinal=mIddle, with namespace")]
                public void MoveToFirstAttribute5()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");

                    string strValue0;
                    string strValue;
                    int iMid = (DataReader.AttributeCount) / 2;

                    DataReader.MoveToAttribute(iMid + 1);
                    strValue0 = DataReader.Value;

                    DataReader.MoveToAttribute(iMid);
                    TestLog.Compare(DataReader.MoveToNextAttribute(), true, Variation.Desc);
                    strValue = DataReader.Value;

                    TestLog.Compare(strValue0, strValue, Variation.Desc);
                    TestLog.Compare(strValue, DataReader.GetAttribute(iMid + 1), Variation.Desc);
                }

                //[Variation("MoveToFirstAttribute() When iOrdinal=mIddle, without namespace")]
                public void MoveToFirstAttribute6()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");

                    string strValue0;
                    string strValue;
                    int iMid = (DataReader.AttributeCount) / 2;

                    DataReader.MoveToAttribute(iMid + 1);
                    strValue0 = DataReader.Value;

                    DataReader.MoveToAttribute(iMid);
                    TestLog.Compare(DataReader.MoveToNextAttribute(), true, Variation.Desc);
                    strValue = DataReader.Value;

                    TestLog.Compare(strValue0, strValue, Variation.Desc);
                    TestLog.Compare(strValue, DataReader.GetAttribute(iMid + 1), Variation.Desc);
                }

                //[Variation("MoveToFirstAttribute() When iOrdinal=end, with namespace")]
                public void MoveToFirstAttribute7()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT0");

                    string strFirst;

                    DataReader.MoveToAttribute((DataReader.AttributeCount) - 1);
                    TestLog.Compare(DataReader.MoveToFirstAttribute(), true, Variation.Desc);
                    strFirst = DataReader.Value;

                    TestLog.Compare(strFirst, DataReader.GetAttribute(0), Variation.Desc);
                }

                //[Variation("MoveToFirstAttribute() When iOrdinal=end, without namespace")]
                public void MoveToFirstAttribute8()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "ACT1");

                    string strFirst;

                    DataReader.MoveToAttribute((DataReader.AttributeCount) - 1);
                    TestLog.Compare(DataReader.MoveToFirstAttribute(), true, Variation.Desc);
                    strFirst = DataReader.Value;

                    TestLog.Compare(strFirst, DataReader.GetAttribute(0), Variation.Desc);
                }
            }

            public partial class TCAttributeTest : BridgeHelpers
            {
                //[Variation("Attribute Test On None")]
                public void TestAttributeTestNodeType_None()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.None))
                    {
                        TestLog.Compare(DataReader.AttributeCount, 0, "Checking AttributeCount");
                        TestLog.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                        TestLog.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                        TestLog.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
                    }
                }

                //[Variation("Attribute Test  On Element", Priority = 0)]
                public void TestAttributeTestNodeType_Element()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.Element))
                    {
                        TestLog.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                        TestLog.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                        TestLog.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                        TestLog.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
                    }
                }

                //[Variation("Attribute Test On Text", Priority = 0)]
                public void TestAttributeTestNodeType_Text()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.Text))
                    {
                        TestLog.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                        TestLog.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                        TestLog.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                        TestLog.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
                    }
                }

                //[Variation("Attribute Test On CDATA")]
                public void TestAttributeTestNodeType_CDATA()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.CDATA))
                    {
                        TestLog.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                        TestLog.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                        TestLog.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                        TestLog.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
                    }
                }

                //[Variation("Attribute Test On ProcessingInstruction")]
                public void TestAttributeTestNodeType_ProcessingInstruction()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.ProcessingInstruction))
                    {
                        TestLog.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                        TestLog.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                        TestLog.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                        TestLog.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
                    }
                }

                //[Variation("AttributeTest On Comment")]
                public void TestAttributeTestNodeType_Comment()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.Comment))
                    {
                        TestLog.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                        TestLog.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                        TestLog.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                        TestLog.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
                    }
                }

                //[Variation("AttributeTest On DocumentType", Priority = 0)]
                public void TestAttributeTestNodeType_DocumentType()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.DocumentType))
                    {
                        TestLog.Compare(DataReader.AttributeCount, 0, "Checking AttributeCount");
                        TestLog.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                        TestLog.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                        TestLog.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
                    }
                }

                //[Variation("AttributeTest On Whitespace")]
                public void TestAttributeTestNodeType_Whitespace()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.Whitespace))
                    {
                        TestLog.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                        TestLog.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                        TestLog.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                        TestLog.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
                    }
                }

                //[Variation("AttributeTest On EndElement")]
                public void TestAttributeTestNodeType_EndElement()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.EndElement))
                    {
                        TestLog.Compare(DataReader.AttributeCount, 0, "Checking AttributeCount");
                        TestLog.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                        TestLog.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                        TestLog.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
                    }
                }

                //[Variation("AttributeTest On XmlDeclaration", Priority = 0)]
                public void TestAttributeTestNodeType_XmlDeclaration()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.XmlDeclaration))
                    {
                        int nCount = 3;
                        TestLog.Compare(DataReader.AttributeCount, nCount, "Checking AttributeCount");
                        TestLog.Compare(DataReader.HasAttributes, true, "Checking HasAttributes");
                        TestLog.Compare(DataReader.MoveToFirstAttribute(), true, "Checking MoveToFirstAttribute");

                        bool bNext = true;
                        TestLog.Compare(DataReader.MoveToNextAttribute(), bNext, "Checking MoveToNextAttribute");
                    }
                }

                //[Variation("AttributeTest On EndEntity")]
                public void TestAttributeTestNodeType_EndEntity()
                {
                    XmlReader DataReader = GetReader();
                    if (FindNodeType(DataReader, XmlNodeType.EndEntity))
                    {
                        TestLog.Compare(DataReader.AttributeCount, 0, "Checking AttributeCount");
                        TestLog.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                        TestLog.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                        TestLog.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
                    }
                }
            }

            //[TestCase(Name = "ReadURI", Desc = "Read URI")]
            public partial class TATextReaderDocType : BridgeHelpers
            {
                //[Variation("Valid URI reference as SystemLiteral")]
                public void TATextReaderDocType_1()
                {
                    string strxml = "<?xml version='1.0' standalone='no'?><!DOCTYPE ROOT SYSTEM 'se2.dtd'[]><ROOT/>";

                    XmlReader r = GetReaderStr(strxml);
                    while (r.Read()) ;
                }

                void TestUriChar(char ch)
                {
                    string filename = string.Format("f{0}.dtd", ch);
                    string strxml = string.Format("<!DOCTYPE ROOT SYSTEM '{0}' []><ROOT></ROOT>", filename);

                    XmlReader r = GetReaderStr(strxml);

                    while (r.Read()) ;
                }

                // XML 1.0 SE
                //[Variation("URI reference with disallowed characters in SystemLiteral")]
                public void TATextReaderDocType_4()
                {
                    string strDisallowed = " {}^`";

                    for (int i = 0; i < strDisallowed.Length; i++)
                        TestUriChar(strDisallowed[i]);
                }
            }

            public partial class TCXmlns : BridgeHelpers
            {
                private string _ST_ENS1 = "EMPTY_NAMESPACE1";
                private string _ST_NS2 = "NAMESPACE2";

                //[Variation("Name, LocalName, Prefix and Value with xmlns=ns attribute", Priority = 0)]
                public void TXmlns1()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, _ST_ENS1);
                    DataReader.MoveToAttribute("xmlns");

                    TestLog.Compare(DataReader.LocalName, "xmlns", "ln");
                    TestLog.Compare(DataReader.Name, "xmlns", "n");
                    TestLog.Compare(DataReader.Prefix, string.Empty, "p");
                    TestLog.Compare(DataReader.Value, "14", "v");
                }

                //[Variation("Name, LocalName, Prefix and Value with xmlns:p=ns attribute")]
                public void TXmlns2()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, _ST_NS2);
                    DataReader.MoveToAttribute(0);
                    TestLog.Compare(DataReader.LocalName, "bar", "ln");
                    TestLog.Compare(DataReader.Name, "xmlns:bar", "n");
                    TestLog.Compare(DataReader.Prefix, "xmlns", "p");
                    TestLog.Compare(DataReader.Value, "1", "v");
                }

                //[Variation("LookupNamespace with xmlns=ns attribute")]
                public void TXmlns3()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, _ST_ENS1);
                    DataReader.MoveToAttribute(1);
                    TestLog.Compare(DataReader.LookupNamespace("xmlns"), "http://www.w3.org/2000/xmlns/", "ln");
                }

                //[Variation("MoveToAttribute access on xmlns attribute")]
                public void TXmlns4()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, _ST_ENS1);

                    DataReader.MoveToAttribute(1);

                    TestLog.Compare(DataReader.LocalName, "xmlns", "ln");
                    TestLog.Compare(DataReader.Name, "xmlns", "n");
                    TestLog.Compare(DataReader.Prefix, string.Empty, "p");
                    TestLog.Compare(DataReader.Value, "14", "v");

                    DataReader.MoveToElement();
                    TestLog.Compare(DataReader.MoveToAttribute("xmlns"), true, "mta(str)");

                    TestLog.Compare(DataReader.LocalName, "xmlns", "ln");
                    TestLog.Compare(DataReader.Name, "xmlns", "n");
                    TestLog.Compare(DataReader.Prefix, string.Empty, "p");
                    TestLog.Compare(DataReader.Value, "14", "v");

                    DataReader.MoveToElement();
                    TestLog.Compare(DataReader.MoveToAttribute("xmlns"), true, "mta(str, str)");

                    TestLog.Compare(DataReader.LocalName, "xmlns", "ln");
                    TestLog.Compare(DataReader.Name, "xmlns", "n");
                    TestLog.Compare(DataReader.Prefix, string.Empty, "p");
                    TestLog.Compare(DataReader.Value, "14", "v");

                    DataReader.MoveToElement();
                    TestLog.Compare(DataReader.MoveToAttribute("xmlns", "14"), false, "mta inv");
                }

                //[Variation("GetAttribute access on xmlns attribute")]
                public void TXmlns5()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, _ST_ENS1);
                    TestLog.Compare(DataReader.GetAttribute(1), "14", "ga(i)");
                    TestLog.Compare(DataReader.GetAttribute("xmlns"), "14", "ga(str)");
                    TestLog.Compare(DataReader.GetAttribute("xmlns"), "14", "ga(str, str)");
                    TestLog.Compare(DataReader.GetAttribute("xmlns", "14"), null, "ga inv");
                }

                //[Variation("this[xmlns] attribute access")]
                public void TXmlns6()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, _ST_ENS1);
                    TestLog.Compare(DataReader[1], "14", "this[i]");
                    TestLog.Compare(DataReader["xmlns"], "14", "this[str]");
                    TestLog.Compare(DataReader["xmlns", "14"], null, "this inv");
                }
            }

            public partial class TCXmlnsPrefix : BridgeHelpers
            {
                private string _ST_ENS1 = "EMPTY_NAMESPACE1";
                private string _ST_NS2 = "NAMESPACE2";
                private string _strXmlns = "http://www.w3.org/2000/xmlns/";

                //[Variation("NamespaceURI of xmlns:a attribute", Priority = 0)]
                public void TXmlnsPrefix1()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, _ST_NS2);
                    DataReader.MoveToAttribute(0);

                    TestLog.Compare(DataReader.NamespaceURI, _strXmlns, "nu");
                }

                //[Variation("NamespaceURI of element/attribute with xmlns attribute", Priority = 0)]
                public void TXmlnsPrefix2()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, _ST_ENS1);
                    TestLog.Compare(DataReader.NamespaceURI, "14", "nue");

                    DataReader.MoveToAttribute("Attr0");
                    TestLog.Compare(DataReader.NamespaceURI, string.Empty, "nu");

                    DataReader.MoveToAttribute("xmlns");
                    TestLog.Compare(DataReader.NamespaceURI, _strXmlns, "nu");
                }

                //[Variation("LookupNamespace with xmlns prefix")]
                public void TXmlnsPrefix3()
                {
                    XmlReader DataReader = GetReader();
                    DataReader.Read();
                    TestLog.Compare(DataReader.LookupNamespace("xmlns"), null, "ln");
                }

                //[Variation("Define prefix for 'www.w3.org/2000/xmlns'", Priority = 0)]
                public void TXmlnsPrefix4()
                {
                    string strxml = "<ROOT xmlns:pxmlns='http://www.w3.org/2000/xmlns/'/>";
                    try
                    {
                        XmlReader DataReader = GetReaderStr(strxml);
                        DataReader.Read();
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException) { }
                }

                //[Variation("Redefine namespace attached to xmlns prefix")]
                public void TXmlnsPrefix5()
                {
                    string strxml = "<ROOT xmlns:xmlns='http://www.w3.org/2002/xmlns/'/>";
                    try
                    {
                        XmlReader DataReader = GetReaderStr(strxml);
                        DataReader.Read();
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException) { }
                }
            }
        }
    }
}
