// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.ModuleCore;
using System;
using System.Xml;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeReaderFunctionalTests : TestModule
    {

        public partial class XNodeReaderTests : XLinqTestCase
        {
            public enum EINTEGRITY
            {
                //DataReader
                BEFORE_READ,
                AFTER_READ_FALSE,
                AFTER_RESETSTATE,

                //DataWriter
                BEFORE_WRITE,
                AFTER_WRITE_FALSE,
                AFTER_CLEAR,
                AFTER_FLUSH,

                // Both DataWriter and DataReader
                AFTER_CLOSE,
                CLOSE_IN_THE_MIDDLE,
            }

            //[TestCase(Name = "XMLIntegrityBase", Desc = "XMLIntegrityBase")]
            public partial class TCXMLIntegrityBase : BridgeHelpers
            {
                private EINTEGRITY _eEIntegrity;

                public EINTEGRITY IntegrityVer
                {
                    get { return _eEIntegrity; }
                    set { _eEIntegrity = value; }
                }

                public static string pATTR = "Attr1";
                public static string pNS = "Foo";
                public static string pNAME = "PLAY0";

                public XmlReader ReloadSource()
                {
                    string strFile = GetTestFileName();
                    XmlReader DataReader = GetReader(strFile);
                    InitReaderPointer(DataReader);
                    return DataReader;
                }

                public void InitReaderPointer(XmlReader DataReader)
                {
                    if (this.Desc == "BeforeRead")
                    {
                        IntegrityVer = EINTEGRITY.BEFORE_READ;
                        TestLog.Compare(DataReader.ReadState, ReadState.Initial, "ReadState=Initial");
                        TestLog.Compare(DataReader.EOF, false, "EOF==false");
                    }
                    else if (this.Desc == "AfterReadIsFalse")
                    {
                        IntegrityVer = EINTEGRITY.AFTER_READ_FALSE;
                        while (DataReader.Read()) ;
                        TestLog.Compare(DataReader.ReadState, ReadState.EndOfFile, "ReadState=EOF");
                        TestLog.Compare(DataReader.EOF, true, "EOF==true");
                    }
                    else if (this.Desc == "AfterClose")
                    {
                        IntegrityVer = EINTEGRITY.AFTER_CLOSE;
                        while (DataReader.Read()) ;
                        DataReader.Dispose();
                        TestLog.Compare(DataReader.ReadState, ReadState.Closed, "ReadState=Closed");
                        TestLog.Compare(DataReader.EOF, false, "EOF==true");
                    }
                    else if (this.Desc == "AfterCloseInTheMiddle")
                    {
                        IntegrityVer = EINTEGRITY.CLOSE_IN_THE_MIDDLE;
                        for (int i = 0; i < 1; i++)
                        {
                            if (false == DataReader.Read())
                                throw new TestFailedException("");
                            TestLog.Compare(DataReader.ReadState, ReadState.Interactive, "ReadState=Interactive");
                        }
                        DataReader.Dispose();
                        TestLog.Compare(DataReader.ReadState, ReadState.Closed, "ReadState=Closed");
                        TestLog.Compare(DataReader.EOF, false, "EOF==true");
                    }
                    else if (this.Desc == "AfterResetState")
                    {
                        IntegrityVer = EINTEGRITY.AFTER_RESETSTATE;
                        // position the reader somewhere in the middle of the file
                        PositionOnElement(DataReader, "elem1");
                        TestLog.Compare(DataReader.ReadState, ReadState.Initial, "ReadState=Initial");
                    }
                }

                //[Variation("NodeType")]
                public void GetXmlReaderNodeType()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.None, Variation.Desc);
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.None, Variation.Desc);
                }

                //[Variation("Name")]
                public void GetXmlReaderName()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.Name, string.Empty, Variation.Desc);
                    TestLog.Compare(DataReader.Name, string.Empty, Variation.Desc);
                }

                //[Variation("LocalName")]
                public void GetXmlReaderLocalName()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.LocalName, string.Empty, Variation.Desc);
                    TestLog.Compare(DataReader.LocalName, string.Empty, Variation.Desc);
                }

                //[Variation("NamespaceURI")]
                public void Namespace()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.NamespaceURI, string.Empty, Variation.Desc);
                    TestLog.Compare(DataReader.NamespaceURI, string.Empty, Variation.Desc);
                }

                //[Variation("Prefix")]
                public void Prefix()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.Prefix, string.Empty, Variation.Desc);
                    TestLog.Compare(DataReader.Prefix, string.Empty, Variation.Desc);
                }

                //[Variation("HasValue")]
                public void HasValue()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.HasValue, false, Variation.Desc);
                    TestLog.Compare(DataReader.HasValue, false, Variation.Desc);
                }

                //[Variation("Value")]
                public void GetXmlReaderValue()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.Value, string.Empty, Variation.Desc);
                    TestLog.Compare(DataReader.Value, string.Empty, Variation.Desc);
                }

                //[Variation("Depth")]
                public void GetDepth()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.Depth, 0, Variation.Desc);
                    TestLog.Compare(DataReader.Depth, 0, Variation.Desc);
                }

                //[Variation("BaseURI")]
                public void GetBaseURI()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.BaseURI, string.Empty, Variation.Desc);
                    TestLog.Compare(DataReader.BaseURI, string.Empty, Variation.Desc);
                }

                //[Variation("IsEmptyElement")]
                public void IsEmptyElement()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.IsEmptyElement, false, Variation.Desc);
                    TestLog.Compare(DataReader.IsEmptyElement, false, Variation.Desc);
                }

                //[Variation("IsDefault")]
                public void IsDefault()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.IsDefault, false, Variation.Desc);
                    TestLog.Compare(DataReader.IsDefault, false, Variation.Desc);
                }

                //[Variation("XmlSpace")]
                public void GetXmlSpace()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.XmlSpace, XmlSpace.None, Variation.Desc);
                    TestLog.Compare(DataReader.XmlSpace, XmlSpace.None, Variation.Desc);
                }

                //[Variation("XmlLang")]
                public void GetXmlLang()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.XmlLang, string.Empty, Variation.Desc);
                    TestLog.Compare(DataReader.XmlLang, string.Empty, Variation.Desc);
                }

                //[Variation("AttributeCount")]
                public void AttributeCount()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.AttributeCount, 0, Variation.Desc);
                    TestLog.Compare(DataReader.AttributeCount, 0, Variation.Desc);
                }

                //[Variation("HasAttributes")]
                public void HasAttribute()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.HasAttributes, false, Variation.Desc);
                    TestLog.Compare(DataReader.HasAttributes, false, Variation.Desc);
                }

                //[Variation("GetAttributes(name)")]
                public void GetAttributeName()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.GetAttribute(pATTR), null, "Compare the GetAttribute");
                }

                //[Variation("GetAttribute(String.Empty)")]
                public void GetAttributeEmptyName()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.GetAttribute(string.Empty), null, "Compare the GetAttribute");
                }

                //[Variation("GetAttribute(name,ns)")]
                public void GetAttributeNameNamespace()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.GetAttribute(pATTR, pNS), null, "Compare the GetAttribute");
                }

                //[Variation("GetAttribute(String.Empty, String.Empty)")]
                public void GetAttributeEmptyNameNamespace()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.GetAttribute(string.Empty, string.Empty), null, "Compare the GetAttribute");
                }

                //[Variation("GetAttribute(i)")]
                public void GetAttributeOrdinal()
                {
                    XmlReader DataReader = ReloadSource();
                    DataReader.GetAttribute(0);
                }

                //[Variation("this[i]")]
                public void HelperThisOrdinal()
                {
                    XmlReader DataReader = ReloadSource();
                    string str = DataReader[0];
                }

                //[Variation("this[name]")]
                public void HelperThisName()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader[pATTR], null, "Compare the GetAttribute");
                }

                //[Variation("this[name,namespace]")]
                public void HelperThisNameNamespace()
                {
                    XmlReader DataReader = ReloadSource();
                    string str = DataReader[pATTR, pNS];
                    TestLog.Compare(DataReader[pATTR, pNS], null, "Compare the GetAttribute");
                }

                //[Variation("MoveToAttribute(name)")]
                public void MoveToAttributeName()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.MoveToAttribute(pATTR), false, Variation.Desc);
                    TestLog.Compare(DataReader.MoveToAttribute(pATTR), false, Variation.Desc);
                }

                //[Variation("MoveToAttributeNameNamespace(name,ns)")]
                public void MoveToAttributeNameNamespace()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.MoveToAttribute(pATTR, pNS), false, Variation.Desc);
                    TestLog.Compare(DataReader.MoveToAttribute(pATTR, pNS), false, Variation.Desc);
                }

                //[Variation("MoveToAttribute(i)")]
                public void MoveToAttributeOrdinal()
                {
                    XmlReader DataReader = ReloadSource();
                    DataReader.MoveToAttribute(0);
                }

                //[Variation("MoveToFirstAttribute()")]
                public void MoveToFirstAttribute()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.MoveToFirstAttribute(), false, Variation.Desc);
                    TestLog.Compare(DataReader.MoveToFirstAttribute(), false, Variation.Desc);
                }

                //[Variation("MoveToNextAttribute()")]
                public void MoveToNextAttribute()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.MoveToNextAttribute(), false, Variation.Desc);
                    TestLog.Compare(DataReader.MoveToNextAttribute(), false, Variation.Desc);
                }

                //[Variation("MoveToElement()")]
                public void MoveToElement()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.MoveToElement(), false, Variation.Desc);
                    TestLog.Compare(DataReader.MoveToElement(), false, Variation.Desc);
                }

                //[Variation("Read")]
                public void ReadTestAfterClose()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.Read(), true, Variation.Desc);
                    TestLog.Compare(DataReader.Read(), true, Variation.Desc);
                }

                //[Variation("GetEOF")]
                public void GetEOF()
                {
                    XmlReader DataReader = ReloadSource();
                    if ((IntegrityVer == EINTEGRITY.AFTER_READ_FALSE))
                    {
                        TestLog.Compare(DataReader.EOF, true, Variation.Desc);
                        TestLog.Compare(DataReader.EOF, true, Variation.Desc);
                    }
                    else
                    {
                        TestLog.Compare(DataReader.EOF, false, Variation.Desc);
                        TestLog.Compare(DataReader.EOF, false, Variation.Desc);
                    }
                }

                //[Variation("GetReadState")]
                public void GetReadState()
                {
                    XmlReader DataReader = ReloadSource();
                    ReadState iState = ReadState.Initial;

                    // EndOfFile State
                    if ((IntegrityVer == EINTEGRITY.AFTER_READ_FALSE))
                    {
                        iState = ReadState.EndOfFile;
                    }

                    // Closed State 
                    if ((IntegrityVer == EINTEGRITY.AFTER_CLOSE) || (IntegrityVer == EINTEGRITY.CLOSE_IN_THE_MIDDLE))
                    {
                        iState = ReadState.Closed;
                    }
                    TestLog.Compare(DataReader.ReadState, iState, Variation.Desc);
                    TestLog.Compare(DataReader.ReadState, iState, Variation.Desc);
                }

                //[Variation("Skip")]
                public void XMLSkip()
                {
                    XmlReader DataReader = ReloadSource();
                    DataReader.Skip();
                    DataReader.Skip();
                }

                //[Variation("NameTable")]
                public void TestNameTable()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.NameTable != null, "nt");
                }

                //[Variation("ReadInnerXml")]
                public void ReadInnerXmlTestAfterClose()
                {
                    XmlReader DataReader = ReloadSource();
                    XmlNodeType nt = DataReader.NodeType;
                    string name = DataReader.Name;
                    string value = DataReader.Value;
                    TestLog.Compare(DataReader.ReadInnerXml(), string.Empty, Variation.Desc);
                    TestLog.Compare(VerifyNode(DataReader, nt, name, value), "vn");
                }

                //[Variation("ReadOuterXml")]
                public void TestReadOuterXml()
                {
                    XmlReader DataReader = ReloadSource();
                    XmlNodeType nt = DataReader.NodeType;
                    string name = DataReader.Name;
                    string value = DataReader.Value;
                    TestLog.Compare(DataReader.ReadOuterXml(), string.Empty, Variation.Desc);
                    TestLog.Compare(VerifyNode(DataReader, nt, name, value), "vn");
                }

                //[Variation("MoveToContent")]
                public void TestMoveToContent()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.MoveToContent(), XmlNodeType.Element, Variation.Desc);
                }

                //[Variation("IsStartElement")]
                public void TestIsStartElement()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.IsStartElement(), true, Variation.Desc);
                }

                //[Variation("IsStartElement(name)")]
                public void TestIsStartElementName()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.IsStartElement(pNAME), false, Variation.Desc);
                }

                //[Variation("IsStartElement(String.Empty)")]
                public void TestIsStartElementName2()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.IsStartElement(string.Empty), false, Variation.Desc);
                }

                //[Variation("IsStartElement(name, ns)")]
                public void TestIsStartElementNameNs()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.IsStartElement(pNAME, pNS), false, Variation.Desc);
                }

                //[Variation("IsStartElement(String.Empty,String.Empty)")]
                public void TestIsStartElementNameNs2()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.IsStartElement(string.Empty, string.Empty), false, Variation.Desc);
                }

                //[Variation("ReadStartElement")]
                public void TestReadStartElement()
                {
                    XmlReader DataReader = ReloadSource();
                    DataReader.ReadStartElement();
                }

                //[Variation("ReadStartElement(name)")]
                public void TestReadStartElementName()
                {
                    XmlReader DataReader = ReloadSource();
                    try
                    {
                        DataReader.ReadStartElement(pNAME);
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadStartElement(String.Empty)")]
                public void TestReadStartElementName2()
                {
                    XmlReader DataReader = ReloadSource();
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

                //[Variation("ReadStartElement(name, ns)")]
                public void TestReadStartElementNameNs()
                {
                    XmlReader DataReader = ReloadSource();
                    try
                    {
                        DataReader.ReadStartElement(pNAME, pNS);
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadStartElement(String.Empty,String.Empty)")]
                public void TestReadStartElementNameNs2()
                {
                    XmlReader DataReader = ReloadSource();
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

                //[Variation("ReadEndElement")]
                public void TestReadEndElement()
                {
                    XmlReader DataReader = ReloadSource();
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

                //[Variation("LookupNamespace")]
                public void LookupNamespace()
                {
                    XmlReader DataReader = ReloadSource();
                    string[] astr = { "a", "Foo", string.Empty, "Foo1", "Foo_S" };

                    for (int i = 0; i < astr.Length; i++)
                    {
                        if (DataReader.LookupNamespace(astr[i]) != null)
                        {
                        }
                        TestLog.Compare(DataReader.LookupNamespace(astr[i]), null, Variation.Desc);
                    }
                }

                //[Variation("ReadAttributeValue")]
                public void ReadAttributeValue()
                {
                    XmlReader DataReader = ReloadSource();
                    TestLog.Compare(DataReader.ReadAttributeValue(), false, Variation.Desc);
                    TestLog.Compare(DataReader.ReadAttributeValue(), false, Variation.Desc);
                }

                //[Variation("Close")]
                public void CloseTest()
                {
                    XmlReader DataReader = ReloadSource();
                    DataReader.Dispose();
                    DataReader.Dispose();
                    DataReader.Dispose();
                }
            }
        }
    }
}
