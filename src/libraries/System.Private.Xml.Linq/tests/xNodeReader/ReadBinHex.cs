// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Test.ModuleCore;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeReaderFunctionalTests : TestModule
    {
        public partial class XNodeReaderTests : XLinqTestCase
        {
            public partial class TCReadContentAsBinHex : BridgeHelpers
            {
                public const string ST_ELEM_NAME1 = "ElemAll";
                public const string ST_ELEM_NAME2 = "ElemEmpty";
                public const string ST_ELEM_NAME3 = "ElemNum";
                public const string ST_ELEM_NAME4 = "ElemText";
                public const string ST_ELEM_NAME5 = "ElemNumText";
                public const string ST_ELEM_NAME6 = "ElemLong";
                public const string strTextBinHex = "ABCDEF";
                public const string strNumBinHex = "0123456789";

                public override void Init()
                {
                    base.Init();
                    CreateBinHexTestFile(pBinHexXml);
                }

                public override void Terminate()
                {
                    base.Terminate();
                }

                private bool VerifyInvalidReadBinHex(int iBufferSize, int iIndex, int iCount, Type exceptionType)
                {
                    bool bPassed = false;
                    byte[] buffer = new byte[iBufferSize];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    DataReader.Read();

                    if (!DataReader.CanReadBinaryContent) return true;
                    try
                    {
                        DataReader.ReadContentAsBinHex(buffer, iIndex, iCount);
                    }
                    catch (Exception e)
                    {
                        bPassed = (e.GetType().ToString() == exceptionType.ToString());
                        if (!bPassed)
                        {
                            TestLog.WriteLine("Actual   exception:{0}", e.GetType().ToString());
                            TestLog.WriteLine("Expected exception:{0}", exceptionType.ToString());
                        }
                    }

                    return bPassed;
                }

                protected void TestInvalidNodeType(XmlNodeType nt)
                {
                    XmlReader DataReader = GetReader(pBinHexXml);

                    PositionOnNodeType(DataReader, nt);
                    string name = DataReader.Name;
                    string value = DataReader.Value;

                    byte[] buffer = new byte[1];
                    if (!DataReader.CanReadBinaryContent) return;

                    try
                    {
                        int nBytes = DataReader.ReadContentAsBinHex(buffer, 0, 1);
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }
                    TestLog.Compare(false, "Invalid OP exception not thrown on wrong nodetype");
                }

                //[Variation("ReadBinHex Element with all valid value")]
                public void TestReadBinHex_1()
                {
                    int binhexlen = 0;
                    byte[] binhex = new byte[1000];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    binhexlen = DataReader.ReadContentAsBinHex(binhex, 0, binhex.Length);

                    string strActbinhex = "";
                    for (int i = 0; i < binhexlen; i = i + 2)
                    {
                        strActbinhex += System.BitConverter.ToChar(binhex, i);
                    }
                    TestLog.Compare(strActbinhex, (strNumBinHex + strTextBinHex), "1. Compare All Valid BinHex");
                }

                //[Variation("ReadBinHex Element with all valid Num value", Priority = 0)]
                public void TestReadBinHex_2()
                {
                    int BinHexlen = 0;
                    byte[] BinHex = new byte[1000];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME3);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);

                    string strActBinHex = "";
                    for (int i = 0; i < BinHexlen; i = i + 2)
                    {
                        strActBinHex += System.BitConverter.ToChar(BinHex, i);
                    }

                    TestLog.Compare(strActBinHex, strNumBinHex, "Compare All Valid BinHex");
                }

                //[Variation("ReadBinHex Element with all valid Text value")]
                public void TestReadBinHex_3()
                {
                    int BinHexlen = 0;
                    byte[] BinHex = new byte[1000];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);

                    string strActBinHex = "";
                    for (int i = 0; i < BinHexlen; i = i + 2)
                    {
                        strActBinHex += System.BitConverter.ToChar(BinHex, i);
                    }

                    TestLog.Compare(strActBinHex, strTextBinHex, "Compare All Valid BinHex");
                }

                //[Variation("ReadBinHex Element on CDATA", Priority = 0)]
                public void TestReadBinHex_4()
                {
                    int BinHexlen = 0;
                    byte[] BinHex = new byte[3];

                    string xmlStr = "<root><![CDATA[ABCDEF]]></root>";
                    XmlReader DataReader = GetReader(new StringReader(xmlStr));
                    PositionOnElement(DataReader, "root");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);
                    TestLog.Compare(BinHexlen, 3, "BinHex");
                    BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);
                    TestLog.Compare(BinHexlen, 0, "BinHex");

                    DataReader.Read();
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.None, "Not on none");
                }

                //[Variation("ReadBinHex Element with all valid value (from concatenation), Priority=0")]
                public void TestReadBinHex_5()
                {
                    int BinHexlen = 0;
                    byte[] BinHex = new byte[1000];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME5);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);

                    string strActBinHex = "";
                    for (int i = 0; i < BinHexlen; i = i + 2)
                    {
                        strActBinHex += System.BitConverter.ToChar(BinHex, i);
                    }
                    TestLog.Compare(strActBinHex, (strNumBinHex + strTextBinHex), "Compare All Valid BinHex");
                }

                //[Variation("ReadBinHex Element with all long valid value (from concatenation)")]
                public void TestReadBinHex_6()
                {
                    int BinHexlen = 0;
                    byte[] BinHex = new byte[2000];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME6);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);

                    string strActBinHex = "";
                    for (int i = 0; i < BinHexlen; i = i + 2)
                    {
                        strActBinHex += System.BitConverter.ToChar(BinHex, i);
                    }

                    string strExpBinHex = "";
                    for (int i = 0; i < 10; i++)
                        strExpBinHex += (strNumBinHex + strTextBinHex);

                    TestLog.Compare(strActBinHex, strExpBinHex, "Compare All Valid BinHex");
                }

                //[Variation("ReadBinHex with count > buffer size")]
                public void TestReadBinHex_7()
                {
                    BoolToLTMResult(VerifyInvalidReadBinHex(5, 0, 6, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBinHex with count < 0")]
                public void TestReadBinHex_8()
                {
                    BoolToLTMResult(VerifyInvalidReadBinHex(5, 2, -1, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBinHex with index > buffer size")]
                public void vReadBinHex_9()
                {
                    BoolToLTMResult(VerifyInvalidReadBinHex(5, 5, 1, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBinHex with index < 0")]
                public void TestReadBinHex_10()
                {
                    BoolToLTMResult(VerifyInvalidReadBinHex(5, -1, 1, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBinHex with index + count exceeds buffer")]
                public void TestReadBinHex_11()
                {
                    BoolToLTMResult(VerifyInvalidReadBinHex(5, 0, 10, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBinHex index & count =0")]
                public void TestReadBinHex_12()
                {
                    byte[] buffer = new byte[5];
                    int iCount = 0;

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    try
                    {
                        iCount = DataReader.ReadContentAsBinHex(buffer, 0, 0);
                    }
                    catch (Exception e)
                    {
                        TestLog.WriteLine(e.ToString());
                        throw new TestException(TestResult.Failed, "");
                    }

                    TestLog.Compare(iCount, 0, "has to be zero");
                }

                //[Variation("ReadBinHex Element multiple into same buffer (using offset), Priority=0")]
                public void TestReadBinHex_13()
                {
                    int BinHexlen = 10;
                    byte[] BinHex = new byte[BinHexlen];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    string strActbinhex = "";
                    for (int i = 0; i < BinHexlen; i = i + 2)
                    {
                        DataReader.ReadContentAsBinHex(BinHex, i, 2);
                        strActbinhex = (System.BitConverter.ToChar(BinHex, i)).ToString();
                        TestLog.Compare(string.Compare(strActbinhex, 0, strTextBinHex, i / 2, 1), 0, "Compare All Valid Base64");
                    }
                }

                //[Variation("ReadBinHex with buffer == null")]
                public void TestReadBinHex_14()
                {
                    XmlReader DataReader = GetReader(pBinHexXml);

                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    try
                    {
                        DataReader.ReadContentAsBinHex(null, 0, 0);
                    }
                    catch (ArgumentNullException)
                    {
                        return;
                    }

                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadBinHex after failed ReadBinHex")]
                public void TestReadBinHex_15()
                {
                    XmlReader DataReader = GetReader(pBinHexXml);

                    PositionOnElement(DataReader, "ElemErr");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] buffer = new byte[10];
                    int nRead = 0;
                    try
                    {
                        nRead = DataReader.ReadContentAsBinHex(buffer, 0, 1);
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException e)
                    {
                        int idx = e.Message.IndexOf("a&");
                        TestLog.Compare(idx >= 0, "msg");
                        CheckXmlException("Xml_UserException", e, 1, 968);
                    }
                }

                //[Variation("Read after partial ReadBinHex")]
                public void TestReadBinHex_16()
                {
                    XmlReader DataReader = GetReader(pBinHexXml);

                    PositionOnElement(DataReader, "ElemNum");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] buffer = new byte[10];
                    int nRead = DataReader.ReadContentAsBinHex(buffer, 0, 8);
                    TestLog.Compare(nRead, 8, "0");

                    DataReader.Read();
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Element, "ElemText", string.Empty), "1vn");
                }

                //[Variation("Current node on multiple calls")]
                public void TestReadBinHex_17()
                {
                    XmlReader DataReader = GetReader(pBinHexXml);

                    PositionOnElement(DataReader, "ElemNum");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] buffer = new byte[30];

                    int nRead = DataReader.ReadContentAsBinHex(buffer, 0, 2);
                    TestLog.Compare(nRead, 2, "0");

                    nRead = DataReader.ReadContentAsBinHex(buffer, 0, 19);
                    TestLog.Compare(nRead, 18, "1");
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.EndElement, "ElemNum", string.Empty), "1vn");
                }

                //[Variation("ReadBinHex with whitespace")]
                public void TestTextReadBinHex_21()
                {
                    byte[] buffer = new byte[1];
                    string strxml = "<abc> 1 1 B </abc>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "abc");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;
                    int result = 0;
                    int nRead;
                    while ((nRead = DataReader.ReadContentAsBinHex(buffer, 0, 1)) > 0)
                        result += nRead;

                    TestLog.Compare(result, 1, "res");
                    TestLog.Compare(buffer[0], (byte)17, "buffer[0]");
                }

                //[Variation("ReadBinHex with odd number of chars")]
                public void TestTextReadBinHex_22()
                {
                    byte[] buffer = new byte[1];
                    string strxml = "<abc>11B</abc>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "abc");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;
                    int result = 0;
                    int nRead;
                    while ((nRead = DataReader.ReadContentAsBinHex(buffer, 0, 1)) > 0)
                        result += nRead;

                    TestLog.Compare(result, 1, "res");
                    TestLog.Compare(buffer[0], (byte)17, "buffer[0]");
                }

                //[Variation("ReadBinHex when end tag doesn't exist")]
                public void TestTextReadBinHex_23()
                {
                    byte[] buffer = new byte[5000];
                    string strxml = "<B>" + new string('A', 5000);
                    try
                    {
                        XmlReader DataReader = GetReaderStr(strxml);
                        PositionOnElement(DataReader, "B");
                        DataReader.Read();
                        if (!DataReader.CanReadBinaryContent) return;
                        DataReader.ReadContentAsBinHex(buffer, 0, 5000);
                        TestLog.WriteLine("Accepted incomplete element");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException e)
                    {
                        CheckXmlException("Xml_UnexpectedEOFInElementContent", e, 1, 5004);
                    }
                }

                //[Variation("WS:WireCompat:hex binary fails to send/return data after 1787 bytes going Whidbey to Everett")]
                public void TestTextReadBinHex_24()
                {
                    string filename = Path.Combine("TestData", "XmlReader", "Common", "Bug99148.xml");
                    XmlReader DataReader = GetReader(filename);

                    DataReader.MoveToContent();
                    int bytes = -1;
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    StringBuilder output = new StringBuilder();
                    while (bytes != 0)
                    {
                        byte[] bbb = new byte[1024];
                        bytes = DataReader.ReadContentAsBinHex(bbb, 0, bbb.Length);
                        for (int i = 0; i < bytes; i++)
                        {
                            output.Append(bbb[i]);
                        }
                    }

                    if (TestLog.Compare(output.ToString().Length, 1735, "Expected Length : 1735"))
                        return;
                    else
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation("DebugAssert in ReadContentAsBinHex")]
                public void DebugAssertInReadContentAsBinHex()
                {
                    XmlReader DataReader = GetReaderStr(@"<root>
<boo>hey</boo>
</root>");

                    byte[] buffer = new byte[5];
                    int iCount = 0;
                    while (DataReader.Read())
                    {
                        if (DataReader.NodeType == XmlNodeType.Element)
                            break;
                    }
                    if (!DataReader.CanReadBinaryContent) return;
                    DataReader.Read();
                    iCount = DataReader.ReadContentAsBinHex(buffer, 0, 0);
                }
            }

            //[TestCase(Name = "ReadElementContentAsBinHex", Desc = "ReadElementContentAsBinHex")]
            public partial class TCReadElementContentAsBinHex : BridgeHelpers
            {
                public const string ST_ELEM_NAME1 = "ElemAll";
                public const string ST_ELEM_NAME2 = "ElemEmpty";
                public const string ST_ELEM_NAME3 = "ElemNum";
                public const string ST_ELEM_NAME4 = "ElemText";
                public const string ST_ELEM_NAME5 = "ElemNumText";
                public const string ST_ELEM_NAME6 = "ElemLong";
                public const string strTextBinHex = "ABCDEF";
                public const string strNumBinHex = "0123456789";

                public override void Init()
                {
                    base.Init();
                    CreateBinHexTestFile(pBinHexXml);
                }

                public override void Terminate()
                {
                    base.Terminate();
                }

                private bool VerifyInvalidReadBinHex(int iBufferSize, int iIndex, int iCount, Type exceptionType)
                {
                    bool bPassed = false;
                    byte[] buffer = new byte[iBufferSize];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    if (!DataReader.CanReadBinaryContent) return true;
                    try
                    {
                        DataReader.ReadElementContentAsBinHex(buffer, iIndex, iCount);
                    }
                    catch (Exception e)
                    {
                        bPassed = (e.GetType().ToString() == exceptionType.ToString());
                        if (!bPassed)
                        {
                            TestLog.WriteLine("Actual   exception:{0}", e.GetType().ToString());
                            TestLog.WriteLine("Expected exception:{0}", exceptionType.ToString());
                        }
                    }

                    return bPassed;
                }

                protected void TestInvalidNodeType(XmlNodeType nt)
                {
                    XmlReader DataReader = GetReader(pBinHexXml);

                    PositionOnNodeType(DataReader, nt);
                    string name = DataReader.Name;
                    string value = DataReader.Value;
                    if (!DataReader.CanReadBinaryContent) return;
                    byte[] buffer = new byte[1];
                    try
                    {
                        int nBytes = DataReader.ReadElementContentAsBinHex(buffer, 0, 1);
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }
                    TestLog.Compare(false, "Invalid OP exception not thrown on wrong nodetype");
                }

                //[Variation("ReadBinHex Element with all valid value")]
                public void TestReadBinHex_1()
                {
                    int binhexlen = 0;
                    byte[] binhex = new byte[1000];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    if (!DataReader.CanReadBinaryContent) return;

                    binhexlen = DataReader.ReadElementContentAsBinHex(binhex, 0, binhex.Length);

                    string strActbinhex = "";
                    for (int i = 0; i < binhexlen; i = i + 2)
                    {
                        strActbinhex += System.BitConverter.ToChar(binhex, i);
                    }

                    TestLog.Compare(strActbinhex, (strNumBinHex + strTextBinHex), "1. Compare All Valid BinHex");
                }

                //[Variation("ReadBinHex Element with all valid Num value", Priority = 0)]
                public void TestReadBinHex_2()
                {
                    int BinHexlen = 0;
                    byte[] BinHex = new byte[1000];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME3);
                    if (!DataReader.CanReadBinaryContent) return;

                    BinHexlen = DataReader.ReadElementContentAsBinHex(BinHex, 0, BinHex.Length);

                    string strActBinHex = "";
                    for (int i = 0; i < BinHexlen; i = i + 2)
                    {
                        strActBinHex += System.BitConverter.ToChar(BinHex, i);
                    }
                    TestLog.Compare(strActBinHex, strNumBinHex, "Compare All Valid BinHex");
                }

                //[Variation("ReadBinHex Element with all valid Text value")]
                public void TestReadBinHex_3()
                {
                    int BinHexlen = 0;
                    byte[] BinHex = new byte[1000];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    if (!DataReader.CanReadBinaryContent) return;

                    BinHexlen = DataReader.ReadElementContentAsBinHex(BinHex, 0, BinHex.Length);

                    string strActBinHex = "";
                    for (int i = 0; i < BinHexlen; i = i + 2)
                    {
                        strActBinHex += System.BitConverter.ToChar(BinHex, i);
                    }
                    TestLog.Compare(strActBinHex, strTextBinHex, "Compare All Valid BinHex");
                }

                //[Variation("ReadBinHex Element with Comments and PIs", Priority = 0)]
                public void TestReadBinHex_4()
                {
                    int BinHexlen = 0;
                    byte[] BinHex = new byte[3];

                    XmlReader DataReader = GetReader(new StringReader("<root>AB<!--Comment-->CD<?pi target?>EF</root>"));
                    PositionOnElement(DataReader, "root");
                    if (!DataReader.CanReadBinaryContent) return;

                    BinHexlen = DataReader.ReadElementContentAsBinHex(BinHex, 0, BinHex.Length);
                    TestLog.Compare(BinHexlen, 3, "BinHex");
                }

                //[Variation("ReadBinHex Element with all valid value (from concatenation), Priority=0")]
                public void TestReadBinHex_5()
                {
                    int BinHexlen = 0;
                    byte[] BinHex = new byte[1000];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME5);
                    if (!DataReader.CanReadBinaryContent) return;

                    BinHexlen = DataReader.ReadElementContentAsBinHex(BinHex, 0, BinHex.Length);

                    string strActBinHex = "";
                    for (int i = 0; i < BinHexlen; i = i + 2)
                    {
                        strActBinHex += System.BitConverter.ToChar(BinHex, i);
                    }

                    TestLog.Compare(strActBinHex, (strNumBinHex + strTextBinHex), "Compare All Valid BinHex");
                }

                //[Variation("ReadBinHex Element with all long valid value (from concatenation)")]
                public void TestReadBinHex_6()
                {
                    int BinHexlen = 0;
                    byte[] BinHex = new byte[2000];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME6);
                    if (!DataReader.CanReadBinaryContent) return;

                    BinHexlen = DataReader.ReadElementContentAsBinHex(BinHex, 0, BinHex.Length);

                    string strActBinHex = "";
                    for (int i = 0; i < BinHexlen; i = i + 2)
                    {
                        strActBinHex += System.BitConverter.ToChar(BinHex, i);
                    }

                    string strExpBinHex = "";
                    for (int i = 0; i < 10; i++)
                        strExpBinHex += (strNumBinHex + strTextBinHex);

                    TestLog.Compare(strActBinHex, strExpBinHex, "Compare All Valid BinHex");
                }

                //[Variation("ReadBinHex with count > buffer size")]
                public void TestReadBinHex_7()
                {
                    BoolToLTMResult(VerifyInvalidReadBinHex(5, 0, 6, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBinHex with count < 0")]
                public void TestReadBinHex_8()
                {
                    BoolToLTMResult(VerifyInvalidReadBinHex(5, 2, -1, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBinHex with index > buffer size")]
                public void vReadBinHex_9()
                {
                    BoolToLTMResult(VerifyInvalidReadBinHex(5, 5, 1, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBinHex with index < 0")]
                public void TestReadBinHex_10()
                {
                    BoolToLTMResult(VerifyInvalidReadBinHex(5, -1, 1, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBinHex with index + count exceeds buffer")]
                public void TestReadBinHex_11()
                {
                    BoolToLTMResult(VerifyInvalidReadBinHex(5, 0, 10, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBinHex index & count =0")]
                public void TestReadBinHex_12()
                {
                    byte[] buffer = new byte[5];
                    int iCount = 0;

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    if (!DataReader.CanReadBinaryContent) return;

                    try
                    {
                        iCount = DataReader.ReadElementContentAsBinHex(buffer, 0, 0);
                    }
                    catch (Exception e)
                    {
                        TestLog.WriteLine(e.ToString());
                        throw new TestException(TestResult.Failed, "");
                    }

                    TestLog.Compare(iCount, 0, "has to be zero");
                }

                //[Variation("ReadBinHex Element multiple into same buffer (using offset), Priority=0")]
                public void TestReadBinHex_13()
                {
                    int BinHexlen = 10;
                    byte[] BinHex = new byte[BinHexlen];

                    XmlReader DataReader = GetReader(pBinHexXml);
                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    if (!DataReader.CanReadBinaryContent) return;

                    string strActbinhex = "";
                    for (int i = 0; i < BinHexlen; i = i + 2)
                    {
                        DataReader.ReadElementContentAsBinHex(BinHex, i, 2);
                        strActbinhex = (System.BitConverter.ToChar(BinHex, i)).ToString();
                        TestLog.Compare(string.Compare(strActbinhex, 0, strTextBinHex, i / 2, 1), 0, "Compare All Valid Base64");
                    }
                }

                //[Variation("ReadBinHex with buffer == null")]
                public void TestReadBinHex_14()
                {
                    XmlReader DataReader = GetReader(pBinHexXml);

                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    if (!DataReader.CanReadBinaryContent) return;
                    try
                    {
                        DataReader.ReadElementContentAsBinHex(null, 0, 0);
                    }
                    catch (ArgumentNullException)
                    {
                        return;
                    }

                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadBinHex after failed ReadBinHex")]
                public void TestReadBinHex_15()
                {
                    XmlReader DataReader = GetReader(pBinHexXml);

                    PositionOnElement(DataReader, "ElemErr");
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] buffer = new byte[10];
                    int nRead = 0;
                    try
                    {
                        nRead = DataReader.ReadElementContentAsBinHex(buffer, 0, 1);
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException e)
                    {
                        int idx = e.Message.IndexOf("a&");
                        TestLog.Compare(idx >= 0, "msg");
                        CheckXmlException("Xml_UserException", e, 1, 968);
                    }
                }

                //[Variation("Read after partial ReadBinHex")]
                public void TestReadBinHex_16()
                {
                    XmlReader DataReader = GetReader(pBinHexXml);

                    PositionOnElement(DataReader, "ElemNum");
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] buffer = new byte[10];
                    int nRead = DataReader.ReadElementContentAsBinHex(buffer, 0, 8);
                    TestLog.Compare(nRead, 8, "0");

                    DataReader.Read();
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Text, "Not on text node");
                }

                //[Variation("ReadBinHex with whitespace")]
                public void TestTextReadBinHex_21()
                {
                    byte[] buffer = new byte[1];
                    string strxml = "<abc> 1 1 B </abc>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "abc");
                    if (!DataReader.CanReadBinaryContent) return;
                    int result = 0;
                    int nRead;
                    while ((nRead = DataReader.ReadElementContentAsBinHex(buffer, 0, 1)) > 0)
                        result += nRead;

                    TestLog.Compare(result, 1, "res");
                    TestLog.Compare(buffer[0], (byte)17, "buffer[0]");
                }

                //[Variation("ReadBinHex with odd number of chars")]
                public void TestTextReadBinHex_22()
                {
                    byte[] buffer = new byte[1];
                    string strxml = "<abc>11B</abc>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "abc");
                    if (!DataReader.CanReadBinaryContent) return;
                    int result = 0;
                    int nRead;
                    while ((nRead = DataReader.ReadElementContentAsBinHex(buffer, 0, 1)) > 0)
                        result += nRead;

                    TestLog.Compare(result, 1, "res");
                    TestLog.Compare(buffer[0], (byte)17, "buffer[0]");
                }

                //[Variation("ReadBinHex when end tag doesn't exist")]
                public void TestTextReadBinHex_23()
                {
                    byte[] buffer = new byte[5000];
                    string strxml = "<B>" + new string('A', 5000);
                    try
                    {
                        XmlReader DataReader = GetReaderStr(strxml);
                        PositionOnElement(DataReader, "B");
                        DataReader.ReadElementContentAsBinHex(buffer, 0, 5000);
                        TestLog.WriteLine("Accepted incomplete element");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException e)
                    {
                        CheckXmlException("Xml_UnexpectedEOFInElementContent", e, 1, 5004);
                    }
                }

                //[Variation("WS:WireCompat:hex binary fails to send/return data after 1787 bytes going Whidbey to Everett")]
                public void TestTextReadBinHex_24()
                {
                    string filename = Path.Combine("TestData", "XmlReader", "Common", "Bug99148.xml");
                    XmlReader DataReader = GetReader(filename);

                    DataReader.MoveToContent();
                    if (!DataReader.CanReadBinaryContent) return;
                    int bytes = -1;

                    StringBuilder output = new StringBuilder();
                    while (bytes != 0)
                    {
                        byte[] bbb = new byte[1024];
                        bytes = DataReader.ReadElementContentAsBinHex(bbb, 0, bbb.Length);
                        for (int i = 0; i < bytes; i++)
                        {
                            output.Append(bbb[i]);
                        }
                    }
                    if (TestLog.Compare(output.ToString().Length, 1735, "Expected Length : 1735"))
                        return;
                    else
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation("SubtreeReader inserted attributes don't work with ReadContentAsBinHex")]
                public void TestTextReadBinHex_25()
                {
                    string strxml = "<root xmlns='0102030405060708090a0B0c'><bar/></root>";
                    using (XmlReader r = GetReader(new StringReader(strxml)))
                    {
                        r.Read();
                        r.Read();
                        using (XmlReader sr = r.ReadSubtree())
                        {
                            if (!sr.CanReadBinaryContent) return;
                            sr.Read();
                            sr.MoveToFirstAttribute();
                            sr.MoveToFirstAttribute();
                            byte[] bytes = new byte[4];
                            while ((sr.ReadContentAsBinHex(bytes, 0, bytes.Length)) > 0) { }
                        }
                    }
                }
            }
        }
    }
}
