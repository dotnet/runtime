// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.ModuleCore;
using System;
using System.IO;
using System.Text;
using System.Xml;
using XmlCoreTest.Common;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {

        public partial class XNodeReaderTests : XLinqTestCase
        {
            //[TestCase(Name = "ReadContentAsBase64", Desc = "ReadContentAsBase64")]
            public partial class TCReadContentAsBase64 : BridgeHelpers
            {
                public const string ST_ELEM_NAME1 = "ElemAll";
                public const string ST_ELEM_NAME2 = "ElemEmpty";
                public const string ST_ELEM_NAME3 = "ElemNum";
                public const string ST_ELEM_NAME4 = "ElemText";
                public const string ST_ELEM_NAME5 = "ElemNumText";
                public const string ST_ELEM_NAME6 = "ElemLong";
                public const string strTextBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                public const string strNumBase64 = "0123456789+/";

                public override void Init()
                {
                    base.Init();
                    CreateBase64TestFile(pBase64Xml);
                }

                public override void Terminate()
                {
                    DeleteTestFile(pBase64Xml);
                    base.Terminate();
                }

                private bool VerifyInvalidReadBase64(int iBufferSize, int iIndex, int iCount, Type exceptionType)
                {
                    bool bPassed = false;
                    byte[] buffer = new byte[iBufferSize];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return true;
                    try
                    {
                        DataReader.ReadContentAsBase64(buffer, iIndex, iCount);
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

                protected void TestOnInvalidNodeType(XmlNodeType nt)
                {
                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnNodeType(DataReader, nt);
                    if (!DataReader.CanReadBinaryContent) return;
                    try
                    {
                        byte[] buffer = new byte[1];
                        int nBytes = DataReader.ReadContentAsBase64(buffer, 0, 1);
                    }
                    catch (InvalidOperationException ioe)
                    {
                        if (ioe.ToString().IndexOf(nt.ToString()) < 0)
                            TestLog.Compare(false, "Call threw wrong invalid operation exception on " + nt);
                        else
                            return;
                    }
                    TestLog.Compare(false, "Call succeeded on " + nt);
                }

                protected void TestOnNopNodeType(XmlNodeType nt)
                {
                    XmlReader DataReader = GetReader(pBase64Xml);

                    PositionOnNodeType(DataReader, nt);
                    string name = DataReader.Name;
                    string value = DataReader.Value;
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] buffer = new byte[1];
                    int nBytes = DataReader.ReadContentAsBase64(buffer, 0, 1);
                    TestLog.Compare(nBytes, 0, "nBytes");
                    TestLog.Compare(VerifyNode(DataReader, nt, name, value), "vn");
                }

                //[Variation("ReadBase64 Element with all valid value")]
                public void TestReadBase64_1()
                {
                    int base64len = 0;
                    byte[] base64 = new byte[1000];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    base64len = DataReader.ReadContentAsBase64(base64, 0, base64.Length);

                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        strActbase64 += System.BitConverter.ToChar(base64, i);
                    }

                    TestLog.Compare(strActbase64, (strTextBase64 + strNumBase64), "Compare All Valid Base64");
                }

                //[Variation("ReadBase64 Element with all valid Num value", Priority = 0)]
                public void TestReadBase64_2()
                {
                    int base64len = 0;
                    byte[] base64 = new byte[1000];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME3);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    base64len = DataReader.ReadContentAsBase64(base64, 0, base64.Length);

                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        strActbase64 += System.BitConverter.ToChar(base64, i);
                    }

                    TestLog.Compare(strActbase64, strNumBase64, "Compare All Valid Base64");
                }

                //[Variation("ReadBase64 Element with all valid Text value")]
                public void TestReadBase64_3()
                {
                    int base64len = 0;
                    byte[] base64 = new byte[1000];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    base64len = DataReader.ReadContentAsBase64(base64, 0, base64.Length);

                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        strActbase64 += System.BitConverter.ToChar(base64, i);
                    }

                    TestLog.Compare(strActbase64, strTextBase64, "Compare All Valid Base64");
                }

                //[Variation("ReadBase64 Element with all valid value (from concatenation), Priority=0")]
                public void TestReadBase64_5()
                {
                    int base64len = 0;
                    byte[] base64 = new byte[1000];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME5);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    base64len = DataReader.ReadContentAsBase64(base64, 0, base64.Length);

                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        strActbase64 += System.BitConverter.ToChar(base64, i);
                    }

                    TestLog.Compare(strActbase64, (strTextBase64 + strNumBase64), "Compare All Valid Base64");
                }

                //[Variation("ReadBase64 Element with Long valid value (from concatenation), Priority=0")]
                public void TestReadBase64_6()
                {
                    int base64len = 0;
                    byte[] base64 = new byte[2000];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME6);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    base64len = DataReader.ReadContentAsBase64(base64, 0, base64.Length);

                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        strActbase64 += System.BitConverter.ToChar(base64, i);
                    }

                    string strExpbase64 = "";
                    for (int i = 0; i < 10; i++)
                        strExpbase64 += (strTextBase64 + strNumBase64);

                    TestLog.Compare(strActbase64, strExpbase64, "Compare All Valid Base64");
                }

                //[Variation("ReadBase64 with count > buffer size")]
                public void ReadBase64_7()
                {
                    BoolToLTMResult(VerifyInvalidReadBase64(5, 0, 6, typeof(NotSupportedException)));
                }

                //[Variation("ReadBase64 with count < 0")]
                public void ReadBase64_8()
                {
                    BoolToLTMResult(VerifyInvalidReadBase64(5, 2, -1, typeof(NotSupportedException)));
                }

                //[Variation("ReadBase64 with index > buffer size")]
                public void ReadBase64_9()
                {
                    BoolToLTMResult(VerifyInvalidReadBase64(5, 5, 1, typeof(NotSupportedException)));
                }

                //[Variation("ReadBase64 with index < 0")]
                public void ReadBase64_10()
                {
                    BoolToLTMResult(VerifyInvalidReadBase64(5, -1, 1, typeof(NotSupportedException)));
                }

                //[Variation("ReadBase64 with index + count exceeds buffer")]
                public void ReadBase64_11()
                {
                    BoolToLTMResult(VerifyInvalidReadBase64(5, 0, 10, typeof(NotSupportedException)));
                }

                //[Variation("ReadBase64 index & count =0")]
                public void ReadBase64_12()
                {
                    byte[] buffer = new byte[5];
                    int iCount = 0;

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    iCount = DataReader.ReadContentAsBase64(buffer, 0, 0);

                    TestLog.Compare(iCount, 0, "has to be zero");
                }

                //[Variation("ReadBase64 Element multiple into same buffer (using offset), Priority=0")]
                public void TestReadBase64_13()
                {
                    int base64len = 20;
                    byte[] base64 = new byte[base64len];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        DataReader.ReadContentAsBase64(base64, i, 2);
                        strActbase64 = (System.BitConverter.ToChar(base64, i)).ToString();
                        TestLog.Compare(String.Compare(strActbase64, 0, strTextBase64, i / 2, 1), 0, "Compare All Valid Base64");
                    }
                }

                //[Variation("ReadBase64 with buffer == null")]
                public void TestReadBase64_14()
                {
                    XmlReader DataReader = GetReader(pBase64Xml);

                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    try
                    {
                        DataReader.ReadContentAsBase64(null, 0, 0);
                    }
                    catch (ArgumentNullException)
                    {
                        return;
                    }

                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadBase64 after failure")]
                public void TestReadBase64_15()
                {
                    XmlReader DataReader = GetReader(pBase64Xml);

                    PositionOnElement(DataReader, "ElemErr");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;


                    byte[] buffer = new byte[10];
                    int nRead = 0;
                    try
                    {
                        nRead = DataReader.ReadContentAsBase64(buffer, 0, 1);
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException e)
                    {
                        CheckXmlException("Xml_InvalidBase64Value", e, 0, 1);
                    }
                }

                //[Variation("Read after partial ReadBase64", Priority = 0)]
                public void TestReadBase64_16()
                {
                    XmlReader DataReader = GetReader(pBase64Xml);

                    PositionOnElement(DataReader, "ElemNum");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] buffer = new byte[10];
                    int nRead = DataReader.ReadContentAsBase64(buffer, 0, 8);
                    TestLog.Compare(nRead, 8, "0");

                    DataReader.Read();

                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "1vn");
                }

                //[Variation("Current node on multiple calls")]
                public void TestReadBase64_17()
                {
                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, "ElemNum");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] buffer = new byte[30];

                    int nRead = DataReader.ReadContentAsBase64(buffer, 0, 2);
                    TestLog.Compare(nRead, 2, "0");

                    nRead = DataReader.ReadContentAsBase64(buffer, 0, 23);
                    TestLog.Compare(nRead, 22, "1");

                    DataReader.Read();
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Nodetype not end element");
                    TestLog.Compare(DataReader.Name, "ElemText", "Nodetype not end element");
                }

                //[Variation("No op node types")]
                public void TestReadBase64_18()
                {
                    TestOnInvalidNodeType(XmlNodeType.EndElement);
                }

                //[Variation("ReadBase64 with incomplete sequence")]
                public void TestTextReadBase64_23()
                {
                    byte[] expected = new byte[] { 0, 16, 131, 16, 81 };

                    byte[] buffer = new byte[10];
                    string strxml = "<r><ROOT>ABCDEFG</ROOT></r>";
                    XmlReader DataReader = GetReaderStr(strxml);

                    PositionOnElement(DataReader, "ROOT");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    int result = 0;
                    int nRead;
                    while ((nRead = DataReader.ReadContentAsBase64(buffer, result, 1)) > 0)
                        result += nRead;

                    TestLog.Compare(result, expected.Length, "res");
                    for (int i = 0; i < result; i++)
                        TestLog.Compare(buffer[i], expected[i], "buffer[" + i + "]");
                }

                //[Variation("ReadBase64 when end tag doesn't exist")]
                public void TestTextReadBase64_24()
                {
                    byte[] buffer = new byte[5000];
                    string strxml = "<B>" + new string('c', 5000);
                    try
                    {
                        XmlReader DataReader = GetReaderStr(strxml);
                        PositionOnElement(DataReader, "B");
                        DataReader.Read();
                        if (!DataReader.CanReadBinaryContent) return;

                        DataReader.ReadContentAsBase64(buffer, 0, 5000);
                        TestLog.WriteLine("Accepted incomplete element");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException e)
                    {
                        CheckXmlException("Xml_UnexpectedEOFInElementContent", e, 1, 5004);
                    }
                }

                //[Variation("ReadBase64 with whitespace in the mIddle")]
                public void TestTextReadBase64_26()
                {
                    byte[] buffer = new byte[1];
                    string strxml = "<abc> AQID  B            B  </abc>";
                    int nRead;

                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "abc");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    for (int i = 0; i < 4; i++)
                    {
                        nRead = DataReader.ReadContentAsBase64(buffer, 0, 1);
                        TestLog.Compare(nRead, 1, "res" + i);
                        TestLog.Compare(buffer[0], (byte)(i + 1), "buffer " + i);
                    }

                    nRead = DataReader.ReadContentAsBase64(buffer, 0, 1);
                    TestLog.Compare(nRead, 0, "nRead 0");
                }

                //[Variation("ReadBase64 with = in the mIddle")]
                public void TestTextReadBase64_27()
                {
                    byte[] buffer = new byte[1];
                    string strxml = "<abc>AQI=ID</abc>";
                    int nRead;

                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "abc");

                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    for (int i = 0; i < 2; i++)
                    {
                        nRead = DataReader.ReadContentAsBase64(buffer, 0, 1);
                        TestLog.Compare(nRead, 1, "res" + i);
                        TestLog.Compare(buffer[0], (byte)(i + 1), "buffer " + i);
                    }

                    try
                    {
                        DataReader.ReadContentAsBase64(buffer, 0, 1);
                        TestLog.WriteLine("ReadBase64 with = in the middle succeeded");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException e)
                    {
                        CheckXmlException("Xml_InvalidBase64Value", e, 0, 1);
                    }
                }

                //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "10000" })]
                //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "1000000" })]
                //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "10000000" })]
                public void RunBase64DoesnNotRunIntoOverflow()
                {
                    int totalfilesize = Convert.ToInt32(Variation.Params[0].ToString());
                    string ascii = new string('c', totalfilesize);

                    byte[] bits = Encoding.Unicode.GetBytes(ascii);
                    string base64str = Convert.ToBase64String(bits);

                    string fileName = "bug105376_" + Variation.Params[0].ToString() + ".xml";
                    FilePathUtil.addStream(fileName, new MemoryStream());
                    StreamWriter sw = new StreamWriter(FilePathUtil.getStream(fileName));
                    sw.Write("<root><base64>");
                    sw.Write(base64str);
                    sw.Write("</base64></root>");
                    sw.Flush();

                    XmlReader DataReader = GetReader(fileName);

                    int SIZE = (totalfilesize - 30);
                    int SIZE64 = SIZE * 3 / 4;

                    PositionOnElement(DataReader, "base64");
                    DataReader.Read();
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] base64 = new byte[SIZE64];

                    int startPos = 0;
                    int readSize = 4096;

                    int currentSize = 0;
                    currentSize = DataReader.ReadContentAsBase64(base64, startPos, readSize);
                    TestLog.Compare(currentSize, readSize, "Read other than first chunk");

                    readSize = SIZE64 - readSize;
                    currentSize = DataReader.ReadContentAsBase64(base64, startPos, readSize);
                    TestLog.Compare(currentSize, readSize, "Read other than remaining Chunk Size");

                    readSize = 0;
                    currentSize = DataReader.ReadContentAsBase64(base64, startPos, readSize);
                    TestLog.Compare(currentSize, 0, "Read other than Zero Bytes");

                    DataReader.Dispose();
                }
            }

            //[TestCase(Name = "ReadElementContentAsBase64", Desc = "ReadElementContentAsBase64")]
            public partial class TCReadElementContentAsBase64 : BridgeHelpers
            {
                public const string ST_ELEM_NAME1 = "ElemAll";
                public const string ST_ELEM_NAME2 = "ElemEmpty";
                public const string ST_ELEM_NAME3 = "ElemNum";
                public const string ST_ELEM_NAME4 = "ElemText";
                public const string ST_ELEM_NAME5 = "ElemNumText";
                public const string ST_ELEM_NAME6 = "ElemLong";
                public const string strTextBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                public const string strNumBase64 = "0123456789+/";

                public override void Init()
                {
                    base.Init();
                    CreateBase64TestFile(pBase64Xml);
                }

                public override void Terminate()
                {
                    DeleteTestFile(pBase64Xml);
                    base.Terminate();
                }

                private bool VerifyInvalidReadBase64(int iBufferSize, int iIndex, int iCount, Type exceptionType)
                {
                    bool bPassed = false;
                    byte[] buffer = new byte[iBufferSize];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    if (!DataReader.CanReadBinaryContent) return true;

                    try
                    {
                        DataReader.ReadContentAsBase64(buffer, iIndex, iCount);
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

                protected void TestOnInvalidNodeType(XmlNodeType nt)
                {
                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnNodeType(DataReader, nt);
                    if (!DataReader.CanReadBinaryContent) return;
                    try
                    {
                        byte[] buffer = new byte[1];
                        int nBytes = DataReader.ReadElementContentAsBase64(buffer, 0, 1);
                    }
                    catch (InvalidOperationException ioe)
                    {
                        if (ioe.ToString().IndexOf(nt.ToString()) < 0)
                            TestLog.Compare(false, "Call threw wrong invalid operation exception on " + nt);
                        else
                            return;
                    }
                    TestLog.Compare(false, "Call succeeded on " + nt);
                }

                //[Variation("ReadBase64 Element with all valid value")]
                public void TestReadBase64_1()
                {
                    int base64len = 0;
                    byte[] base64 = new byte[1000];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    if (!DataReader.CanReadBinaryContent) return;

                    base64len = DataReader.ReadElementContentAsBase64(base64, 0, base64.Length);

                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        strActbase64 += System.BitConverter.ToChar(base64, i);
                    }

                    TestLog.Compare(strActbase64, (strTextBase64 + strNumBase64), "Compare All Valid Base64");
                }

                //[Variation("ReadBase64 Element with all valid Num value", Priority = 0)]
                public void TestReadBase64_2()
                {
                    int base64len = 0;
                    byte[] base64 = new byte[1000];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME3);
                    if (!DataReader.CanReadBinaryContent) return;

                    base64len = DataReader.ReadElementContentAsBase64(base64, 0, base64.Length);

                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        strActbase64 += System.BitConverter.ToChar(base64, i);
                    }

                    TestLog.Compare(strActbase64, strNumBase64, "Compare All Valid Base64");
                }

                //[Variation("ReadBase64 Element with all valid Text value")]
                public void TestReadBase64_3()
                {
                    int base64len = 0;
                    byte[] base64 = new byte[1000];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    if (!DataReader.CanReadBinaryContent) return;

                    base64len = DataReader.ReadElementContentAsBase64(base64, 0, base64.Length);

                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        strActbase64 += System.BitConverter.ToChar(base64, i);
                    }

                    TestLog.Compare(strActbase64, strTextBase64, "Compare All Valid Base64");
                }

                //[Variation("ReadBase64 Element with all valid value (from concatenation), Priority=0")]
                public void TestReadBase64_5()
                {
                    int base64len = 0;
                    byte[] base64 = new byte[1000];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME5);
                    if (!DataReader.CanReadBinaryContent) return;

                    base64len = DataReader.ReadElementContentAsBase64(base64, 0, base64.Length);

                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        strActbase64 += System.BitConverter.ToChar(base64, i);
                    }

                    TestLog.Compare(strActbase64, (strTextBase64 + strNumBase64), "Compare All Valid Base64");
                }

                //[Variation("ReadBase64 Element with Long valid value (from concatenation), Priority=0")]
                public void TestReadBase64_6()
                {
                    int base64len = 0;
                    byte[] base64 = new byte[2000];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME6);
                    if (!DataReader.CanReadBinaryContent) return;

                    base64len = DataReader.ReadElementContentAsBase64(base64, 0, base64.Length);

                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        strActbase64 += System.BitConverter.ToChar(base64, i);
                    }

                    string strExpbase64 = "";
                    for (int i = 0; i < 10; i++)
                        strExpbase64 += (strTextBase64 + strNumBase64);

                    TestLog.Compare(strActbase64, strExpbase64, "Compare All Valid Base64");
                }

                //[Variation("ReadBase64 with count > buffer size")]
                public void ReadBase64_7()
                {
                    BoolToLTMResult(VerifyInvalidReadBase64(5, 0, 6, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBase64 with count < 0")]
                public void ReadBase64_8()
                {
                    BoolToLTMResult(VerifyInvalidReadBase64(5, 2, -1, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBase64 with index > buffer size")]
                public void ReadBase64_9()
                {
                    BoolToLTMResult(VerifyInvalidReadBase64(5, 5, 1, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBase64 with index < 0")]
                public void ReadBase64_10()
                {
                    BoolToLTMResult(VerifyInvalidReadBase64(5, -1, 1, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBase64 with index + count exceeds buffer")]
                public void ReadBase64_11()
                {
                    BoolToLTMResult(VerifyInvalidReadBase64(5, 0, 10, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadBase64 index & count =0")]
                public void ReadBase64_12()
                {
                    byte[] buffer = new byte[5];
                    int iCount = 0;

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME1);
                    if (!DataReader.CanReadBinaryContent) return;
                    iCount = DataReader.ReadElementContentAsBase64(buffer, 0, 0);

                    TestLog.Compare(iCount, 0, "has to be zero");
                }

                //[Variation("ReadBase64 Element multiple into same buffer (using offset), Priority=0")]
                public void TestReadBase64_13()
                {
                    int base64len = 20;
                    byte[] base64 = new byte[base64len];

                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    if (!DataReader.CanReadBinaryContent) return;
                    string strActbase64 = "";
                    for (int i = 0; i < base64len; i = i + 2)
                    {
                        DataReader.ReadElementContentAsBase64(base64, i, 2);
                        strActbase64 = (System.BitConverter.ToChar(base64, i)).ToString();
                        TestLog.Compare(String.Compare(strActbase64, 0, strTextBase64, i / 2, 1), 0, "Compare All Valid Base64");
                    }
                }

                //[Variation("ReadBase64 with buffer == null")]
                public void TestReadBase64_14()
                {
                    XmlReader DataReader = GetReader(pBase64Xml);

                    PositionOnElement(DataReader, ST_ELEM_NAME4);
                    if (!DataReader.CanReadBinaryContent) return;
                    try
                    {
                        DataReader.ReadElementContentAsBase64(null, 0, 0);
                    }
                    catch (ArgumentNullException)
                    {
                        return;
                    }

                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadBase64 after failure")]
                public void TestReadBase64_15()
                {
                    XmlReader DataReader = GetReader(pBase64Xml);

                    PositionOnElement(DataReader, "ElemErr");
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] buffer = new byte[10];
                    int nRead = 0;
                    try
                    {
                        nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 1);
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException e)
                    {
                        CheckXmlException("Xml_InvalidBase64Value", e, 0, 1);
                    }
                }

                //[Variation("Read after partial ReadBase64", Priority = 0)]
                public void TestReadBase64_16()
                {
                    XmlReader DataReader = GetReader(pBase64Xml);

                    PositionOnElement(DataReader, "ElemNum");
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] buffer = new byte[10];
                    int nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 8);
                    TestLog.Compare(nRead, 8, "0");

                    DataReader.Read();
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Text, "1vn");
                }

                //[Variation("Current node on multiple calls")]
                public void TestReadBase64_17()
                {
                    XmlReader DataReader = GetReader(pBase64Xml);
                    PositionOnElement(DataReader, "ElemNum");
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] buffer = new byte[30];

                    int nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 2);
                    TestLog.Compare(nRead, 2, "0");

                    nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 23);
                    TestLog.Compare(nRead, 22, "1");

                    TestLog.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Nodetype not end element");
                    TestLog.Compare(DataReader.Name, "ElemNum", "Nodetype not end element");
                }

                //[Variation("ReadBase64 with incomplete sequence")]
                public void TestTextReadBase64_23()
                {
                    byte[] expected = new byte[] { 0, 16, 131, 16, 81 };

                    byte[] buffer = new byte[10];
                    string strxml = "<r><ROOT>ABCDEFG</ROOT></r>";
                    XmlReader DataReader = GetReaderStr(strxml);

                    PositionOnElement(DataReader, "ROOT");
                    if (!DataReader.CanReadBinaryContent) return;

                    int result = 0;
                    int nRead;
                    while ((nRead = DataReader.ReadElementContentAsBase64(buffer, result, 1)) > 0)
                        result += nRead;

                    TestLog.Compare(result, expected.Length, "res");
                    for (int i = 0; i < result; i++)
                        TestLog.Compare(buffer[i], expected[i], "buffer[" + i + "]");
                }

                //[Variation("ReadBase64 when end tag doesn't exist")]
                public void TestTextReadBase64_24()
                {
                    byte[] buffer = new byte[5000];
                    string strxml = "<B>" + new string('c', 5000);
                    try
                    {
                        XmlReader DataReader = GetReaderStr(strxml);
                        PositionOnElement(DataReader, "B");
                        if (!DataReader.CanReadBinaryContent) return;

                        DataReader.ReadElementContentAsBase64(buffer, 0, 5000);
                        TestLog.WriteLine("Accepted incomplete element");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException e)
                    {
                        CheckXmlException("Xml_UnexpectedEOFInElementContent", e, 1, 5004);
                    }
                }

                //[Variation("ReadBase64 with whitespace in the mIddle")]
                public void TestTextReadBase64_26()
                {
                    byte[] buffer = new byte[1];
                    string strxml = "<abc> AQID  B            B  </abc>";
                    int nRead;

                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "abc");
                    if (!DataReader.CanReadBinaryContent) return;

                    for (int i = 0; i < 4; i++)
                    {
                        nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 1);
                        TestLog.Compare(nRead, 1, "res" + i);
                        TestLog.Compare(buffer[0], (byte)(i + 1), "buffer " + i);
                    }

                    nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 1);
                    TestLog.Compare(nRead, 0, "nRead 0");
                }

                //[Variation("ReadBase64 with = in the mIddle")]
                public void TestTextReadBase64_27()
                {
                    byte[] buffer = new byte[1];
                    string strxml = "<abc>AQI=ID</abc>";
                    int nRead;

                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "abc");
                    if (!DataReader.CanReadBinaryContent) return;

                    for (int i = 0; i < 2; i++)
                    {
                        nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 1);
                        TestLog.Compare(nRead, 1, "res" + i);
                        TestLog.Compare(buffer[0], (byte)(i + 1), "buffer " + i);
                    }

                    try
                    {
                        DataReader.ReadElementContentAsBase64(buffer, 0, 1);
                        TestLog.WriteLine("ReadBase64 with = in the middle succeeded");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException e)
                    {
                        CheckXmlException("Xml_InvalidBase64Value", e, 0, 1);
                    }
                }

                //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "10000" })]
                //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "1000000" })]
                //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "10000000" })]
                public void ReadBase64DoesNotRunIntoOverflow2()
                {
                    int totalfilesize = Convert.ToInt32(Variation.Params[0].ToString());

                    string ascii = new string('c', totalfilesize);

                    byte[] bits = Encoding.Unicode.GetBytes(ascii);
                    string base64str = Convert.ToBase64String(bits);

                    string fileName = "bug105376_" + Variation.Params[0].ToString() + ".xml";
                    FilePathUtil.addStream(fileName, new MemoryStream());
                    StreamWriter sw = new StreamWriter(FilePathUtil.getStream(fileName));
                    sw.Write("<root><base64>");
                    sw.Write(base64str);
                    sw.Write("</base64></root>");
                    sw.Flush();

                    XmlReader DataReader = GetReader(fileName);

                    int SIZE = (totalfilesize - 30);
                    int SIZE64 = SIZE * 3 / 4;

                    PositionOnElement(DataReader, "base64");
                    if (!DataReader.CanReadBinaryContent) return;

                    byte[] base64 = new byte[SIZE64];

                    int startPos = 0;
                    int readSize = 4096;

                    int currentSize = 0;
                    currentSize = DataReader.ReadElementContentAsBase64(base64, startPos, readSize);
                    TestLog.Compare(currentSize, readSize, "Read other than first chunk");

                    readSize = SIZE64 - readSize;
                    currentSize = DataReader.ReadElementContentAsBase64(base64, startPos, readSize);
                    TestLog.Compare(currentSize, readSize, "Read other than remaining Chunk Size");

                    readSize = 0;
                    currentSize = DataReader.ReadElementContentAsBase64(base64, startPos, readSize);
                    TestLog.Compare(currentSize, 0, "Read other than Zero Bytes");

                    DataReader.Dispose();
                }

                //[Variation("SubtreeReader inserted attributes don't work with ReadContentAsBase64")]
                public void SubtreeReaderInsertedAttributesWontWorkWithReadContentAsBase64()
                {
                    string strxml1 = "<root xmlns='";
                    string strxml2 = "'><bar/></root>";

                    string[] binValue = new string[] { "AAECAwQFBgcI==", "0102030405060708090a0B0c" };
                    for (int i = 0; i < binValue.Length; i++)
                    {
                        string strxml = strxml1 + binValue[i] + strxml2;
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
                                while ((sr.ReadContentAsBase64(bytes, 0, bytes.Length)) > 0) { }
                            }
                        }
                    }
                }
            }
        }
    }
}
