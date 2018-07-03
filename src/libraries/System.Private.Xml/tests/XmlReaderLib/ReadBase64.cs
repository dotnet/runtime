// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Text;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    [InheritRequired()]
    public abstract partial class TCReadContentAsBase64 : TCXMLReaderBaseGeneral
    {
        public const string ST_ELEM_NAME1 = "ElemAll";
        public const string ST_ELEM_NAME2 = "ElemEmpty";
        public const string ST_ELEM_NAME3 = "ElemNum";
        public const string ST_ELEM_NAME4 = "ElemText";
        public const string ST_ELEM_NAME5 = "ElemNumText";
        public const string ST_ELEM_NAME6 = "ElemLong";
        public const string Base64Xml = "Base64.xml";

        public const string strTextBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        public const string strNumBase64 = "0123456789+/";

        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);

            CreateTestFile(EREADER_TYPE.BASE64_TEST);

            return ret;
        }

        public override int Terminate(object objParam)
        {
            DataReader.Close();
            return base.Terminate(objParam);
        }
        private bool VerifyInvalidReadBase64(int iBufferSize, int iIndex, int iCount, Type exceptionType)
        {
            bool bPassed = false;
            byte[] buffer = new byte[iBufferSize];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return true;
            try
            {
                DataReader.ReadContentAsBase64(buffer, iIndex, iCount);
            }
            catch (Exception e)
            {
                CError.WriteLine("Actual   exception:{0}", e.GetType().ToString());
                CError.WriteLine("Expected exception:{0}", exceptionType.ToString());
                bPassed = (e.GetType().ToString() == exceptionType.ToString());
            }

            return bPassed;
        }

        protected void TestOnInvalidNodeType(XmlNodeType nt)
        {
            ReloadSource();
            PositionOnNodeType(nt);
            if (CheckCanReadBinaryContent()) return;
            try
            {
                byte[] buffer = new byte[1];
                int nBytes = DataReader.ReadContentAsBase64(buffer, 0, 1);
            }
            catch (InvalidOperationException ioe)
            {
                if (ioe.ToString().IndexOf(nt.ToString()) < 0)
                    CError.Compare(false, "Call threw wrong invalid operation exception on " + nt);
                else
                    return;
            }
            CError.Compare(false, "Call succeeded on " + nt);
        }

        protected void TestOnNopNodeType(XmlNodeType nt)
        {
            ReloadSource();

            PositionOnNodeType(nt);
            string name = DataReader.Name;
            string value = DataReader.Value;
            CError.WriteLine("Name=" + name);
            CError.WriteLine("Value=" + value);
            if (CheckCanReadBinaryContent()) return;

            byte[] buffer = new byte[1];
            int nBytes = DataReader.ReadContentAsBase64(buffer, 0, 1);
            CError.Compare(nBytes, 0, "nBytes");
            CError.Compare(DataReader.VerifyNode(nt, name, value), "vn");
            CError.WriteLine("Succeeded:{0}", nt);
        }

        ////////////////////////////////////////////////////////////////
        // Variations
        ////////////////////////////////////////////////////////////////

        [Variation("ReadBase64 Element with all valid value")]
        public int TestReadBase64_1()
        {
            int base64len = 0;
            byte[] base64 = new byte[1000];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            base64len = DataReader.ReadContentAsBase64(base64, 0, base64.Length);

            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                strActbase64 += System.BitConverter.ToChar(base64, i);
            }

            CError.Compare(strActbase64, (strTextBase64 + strNumBase64), "Compare All Valid Base64");
            return TEST_PASS;
        }

        [Variation("ReadBase64 Element with all valid Num value", Pri = 0)]
        public int TestReadBase64_2()
        {
            int base64len = 0;
            byte[] base64 = new byte[1000];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME3);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            base64len = DataReader.ReadContentAsBase64(base64, 0, base64.Length);

            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                strActbase64 += System.BitConverter.ToChar(base64, i);
            }

            CError.Compare(strActbase64, strNumBase64, "Compare All Valid Base64");
            return TEST_PASS;
        }

        [Variation("ReadBase64 Element with all valid Text value")]
        public int TestReadBase64_3()
        {
            int base64len = 0;
            byte[] base64 = new byte[1000];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME4);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            base64len = DataReader.ReadContentAsBase64(base64, 0, base64.Length);

            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                strActbase64 += System.BitConverter.ToChar(base64, i);
            }

            CError.Compare(strActbase64, strTextBase64, "Compare All Valid Base64");
            return TEST_PASS;
        }

        [Variation("ReadBase64 Element with all valid value (from concatenation), Pri=0")]
        public int TestReadBase64_5()
        {
            int base64len = 0;
            byte[] base64 = new byte[1000];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME5);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            base64len = DataReader.ReadContentAsBase64(base64, 0, base64.Length);

            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                strActbase64 += System.BitConverter.ToChar(base64, i);
            }

            CError.Compare(strActbase64, (strTextBase64 + strNumBase64), "Compare All Valid Base64");
            return TEST_PASS;
        }

        [Variation("ReadBase64 Element with Long valid value (from concatenation), Pri=0")]
        public int TestReadBase64_6()
        {
            int base64len = 0;
            byte[] base64 = new byte[2000];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME6);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            base64len = DataReader.ReadContentAsBase64(base64, 0, base64.Length);

            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                strActbase64 += System.BitConverter.ToChar(base64, i);
            }

            string strExpbase64 = "";
            for (int i = 0; i < 10; i++)
                strExpbase64 += (strTextBase64 + strNumBase64);

            CError.Compare(strActbase64, strExpbase64, "Compare All Valid Base64");
            return TEST_PASS;
        }

        [Variation("ReadBase64 with count > buffer size")]
        public int ReadBase64_7()
        {
            return BoolToLTMResult(VerifyInvalidReadBase64(5, 0, 6, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBase64 with count < 0")]
        public int ReadBase64_8()
        {
            return BoolToLTMResult(VerifyInvalidReadBase64(5, 2, -1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBase64 with index > buffer size")]
        public int ReadBase64_9()
        {
            return BoolToLTMResult(VerifyInvalidReadBase64(5, 5, 1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBase64 with index < 0")]
        public int ReadBase64_10()
        {
            return BoolToLTMResult(VerifyInvalidReadBase64(5, -1, 1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBase64 with index + count exceeds buffer")]
        public int ReadBase64_11()
        {
            return BoolToLTMResult(VerifyInvalidReadBase64(5, 0, 10, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBase64 index & count =0")]
        public int ReadBase64_12()
        {
            byte[] buffer = new byte[5];
            int iCount = 0;

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            iCount = DataReader.ReadContentAsBase64(buffer, 0, 0);

            CError.Compare(iCount, 0, "has to be zero");
            return TEST_PASS;
        }

        [Variation("ReadBase64 Element multiple into same buffer (using offset), Pri=0")]
        public int TestReadBase64_13()
        {
            int base64len = 20;
            byte[] base64 = new byte[base64len];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME4);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                DataReader.ReadContentAsBase64(base64, i, 2);
                strActbase64 = (System.BitConverter.ToChar(base64, i)).ToString();
                CError.Compare(string.Compare(strActbase64, 0, strTextBase64, i / 2, 1), 0, "Compare All Valid Base64");
            }

            return TEST_PASS;
        }

        [Variation("ReadBase64 with buffer == null")]
        public int TestReadBase64_14()
        {
            ReloadSource(EREADER_TYPE.BASE64_TEST);

            DataReader.PositionOnElement(ST_ELEM_NAME4);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            try
            {
                DataReader.ReadContentAsBase64(null, 0, 0);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }

            return TEST_FAIL;
        }

        [Variation("ReadBase64 after failure")]
        public int TestReadBase64_15()
        {
            ReloadSource(EREADER_TYPE.BASE64_TEST);

            DataReader.PositionOnElement("ElemErr");
            DataReader.Read();
            var line = ((IXmlLineInfo)DataReader.Internal).LinePosition;

            if (CheckCanReadBinaryContent()) return TEST_PASS;


            byte[] buffer = new byte[10];
            int nRead = 0;
            try
            {
                nRead = DataReader.ReadContentAsBase64(buffer, 0, 1);
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                if (IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader() || IsXmlValidatingReader() || IsCharCheckingReader())
                    CheckException("Xml_InvalidBase64Value", e);
                else
                {
                    CheckXmlException("Xml_UserException", e, 1, line);
                }
            }
            return TEST_PASS;
        }

        [Variation("Read after partial ReadBase64", Pri = 0)]
        public int TestReadBase64_16()
        {
            ReloadSource(EREADER_TYPE.BASE64_TEST);

            DataReader.PositionOnElement("ElemNum");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            byte[] buffer = new byte[10];
            int nRead = DataReader.ReadContentAsBase64(buffer, 0, 8);
            CError.Compare(nRead, 8, "0");

            DataReader.Read();

            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "1vn");

            return TEST_PASS;
        }

        [Variation("Current node on multiple calls")]
        public int TestReadBase64_17()
        {
            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement("ElemNum");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            byte[] buffer = new byte[30];

            int nRead = DataReader.ReadContentAsBase64(buffer, 0, 2);
            CError.Compare(nRead, 2, "0");

            nRead = DataReader.ReadContentAsBase64(buffer, 0, 23);
            CError.Compare(nRead, 22, "1");

            DataReader.Read();
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Nodetype not end element");
            CError.Compare(DataReader.Name, "ElemText", "Nodetype not end element");

            return TEST_PASS;
        }

        [Variation("ReadBase64 with incomplete sequence")]
        public int TestTextReadBase64_23()
        {
            byte[] expected = new byte[] { 0, 16, 131, 16, 81 };

            byte[] buffer = new byte[10];
            string strxml = "<r><ROOT>ABCDEFG</ROOT></r>";
            ReloadSourceStr(strxml);

            DataReader.PositionOnElement("ROOT");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            int result = 0;
            int nRead;
            while ((nRead = DataReader.ReadContentAsBase64(buffer, result, 1)) > 0)
                result += nRead;

            CError.Compare(result, expected.Length, "res");
            for (int i = 0; i < result; i++)
                CError.Compare(buffer[i], expected[i], "buffer[" + i + "]");

            return TEST_PASS;
        }

        [Variation("ReadBase64 when end tag doesn't exist")]
        public int TestTextReadBase64_24()
        {
            if (IsRoundTrippedReader())
                return TEST_SKIPPED;
            byte[] buffer = new byte[5000];
            string strxml = "<B>" + new string('c', 5000);
            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("B");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            try
            {
                DataReader.ReadContentAsBase64(buffer, 0, 5000);
                CError.WriteLine("Accepted incomplete element");
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                CheckXmlException("Xml_UnexpectedEOFInElementContent", e, 1, 5004);
            }

            return TEST_PASS;
        }

        [Variation("ReadBase64 with whitespace in the middle")]
        public int TestTextReadBase64_26()
        {
            byte[] buffer = new byte[1];
            string strxml = "<abc> AQID  B            B  </abc>";
            int nRead;

            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("abc");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            for (int i = 0; i < 4; i++)
            {
                nRead = DataReader.ReadContentAsBase64(buffer, 0, 1);
                CError.Compare(nRead, 1, "res" + i);
                CError.Compare(buffer[0], (byte)(i + 1), "buffer " + i);
            }

            nRead = DataReader.ReadContentAsBase64(buffer, 0, 1);
            CError.Compare(nRead, 0, "nRead 0");

            return TEST_PASS;
        }

        [Variation("ReadBase64 with = in the middle")]
        public int TestTextReadBase64_27()
        {
            byte[] buffer = new byte[1];
            string strxml = "<abc>AQI=ID</abc>";
            int nRead;

            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("abc");

            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            for (int i = 0; i < 2; i++)
            {
                nRead = DataReader.ReadContentAsBase64(buffer, 0, 1);
                CError.Compare(nRead, 1, "res" + i);
                CError.Compare(buffer[0], (byte)(i + 1), "buffer " + i);
            }

            try
            {
                DataReader.ReadContentAsBase64(buffer, 0, 1);
                CError.WriteLine("ReadBase64 with = in the middle succeeded");
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                if (IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader() || IsXmlValidatingReader() || IsCharCheckingReader())
                    CheckException("Xml_InvalidBase64Value", e);
                else
                    CheckXmlException("Xml_UserException", e, 1, 6);
            }

            return TEST_PASS;
        }

        //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "10000" })]
        //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "1000000" })]
        //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "10000000" })]
        public int ReadBase64BufferOverflowWorksProperly()
        {
            int totalfilesize = Convert.ToInt32(CurVariation.Params[0].ToString());
            CError.WriteLine(" totalfilesize = " + totalfilesize);

            string ascii = new string('c', totalfilesize);

            byte[] bits = Encoding.Unicode.GetBytes(ascii);
            CError.WriteLineIgnore("Count = " + bits.Length);
            string base64str = Convert.ToBase64String(bits);

            string fileName = "bug105376_" + CurVariation.Params[0].ToString() + ".xml";
            MemoryStream mems = new MemoryStream();
            StreamWriter sw = new StreamWriter(mems);
            {
                sw.Write("<root><base64>");
                sw.Write(base64str);
                sw.Write("</base64></root>");
            }
            FilePathUtil.addStream(fileName, mems);
            ReloadSource(fileName);
            int SIZE = (totalfilesize - 30);
            int SIZE64 = SIZE * 3 / 4;

            DataReader.PositionOnElement("base64");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            CError.WriteLine("ReadBase64 method... ");
            CError.WriteLine(int.MaxValue);

            byte[] base64 = new byte[SIZE64];

            CError.WriteLine("SIZE64 = {0}", base64.Length);
            int startPos = 0;
            int readSize = 4096;

            int currentSize = 0;
            currentSize = DataReader.ReadContentAsBase64(base64, startPos, readSize);
            CError.Compare(currentSize, readSize, "Read other than first chunk");

            readSize = SIZE64 - readSize;
            currentSize = DataReader.ReadContentAsBase64(base64, startPos, readSize);
            CError.Compare(currentSize, readSize, "Read other than remaining Chunk Size");

            readSize = 0;
            currentSize = DataReader.ReadContentAsBase64(base64, startPos, readSize);
            CError.Compare(currentSize, 0, "Read other than Zero Bytes");
            DataReader.Close();
            return TEST_PASS;
        }
    }

    [InheritRequired()]
    public abstract partial class TCReadElementContentAsBase64 : TCXMLReaderBaseGeneral
    {
        public const string ST_ELEM_NAME1 = "ElemAll";
        public const string ST_ELEM_NAME2 = "ElemEmpty";
        public const string ST_ELEM_NAME3 = "ElemNum";
        public const string ST_ELEM_NAME4 = "ElemText";
        public const string ST_ELEM_NAME5 = "ElemNumText";
        public const string ST_ELEM_NAME6 = "ElemLong";
        private const string Base64Xml = "Base64.xml";

        public const string strTextBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        public const string strNumBase64 = "0123456789+/";

        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);

            CreateTestFile(EREADER_TYPE.BASE64_TEST);

            return ret;
        }

        public override int Terminate(object objParam)
        {
            DataReader.Close();

            return base.Terminate(objParam);
        }

        private bool VerifyInvalidReadBase64(int iBufferSize, int iIndex, int iCount, Type exceptionType)
        {
            bool bPassed = false;
            byte[] buffer = new byte[iBufferSize];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            if (CheckCanReadBinaryContent()) return true;

            try
            {
                DataReader.ReadContentAsBase64(buffer, iIndex, iCount);
            }
            catch (Exception e)
            {
                CError.WriteLine("Actual   exception:{0}", e.GetType().ToString());
                CError.WriteLine("Expected exception:{0}", exceptionType.ToString());
                bPassed = (e.GetType().ToString() == exceptionType.ToString());
            }

            return bPassed;
        }

        protected void TestOnInvalidNodeType(XmlNodeType nt)
        {
            ReloadSource();
            PositionOnNodeType(nt);
            if (CheckCanReadBinaryContent()) return;
            try
            {
                byte[] buffer = new byte[1];
                int nBytes = DataReader.ReadElementContentAsBase64(buffer, 0, 1);
            }
            catch (InvalidOperationException ioe)
            {
                if (ioe.ToString().IndexOf(nt.ToString()) < 0)
                    CError.Compare(false, "Call threw wrong invalid operation exception on " + nt);
                else
                    return;
            }
            CError.Compare(false, "Call succeeded on " + nt);
        }

        ////////////////////////////////////////////////////////////////
        // Variations
        ////////////////////////////////////////////////////////////////
        [Variation("ReadBase64 Element with all valid value")]
        public int TestReadBase64_1()
        {
            int base64len = 0;
            byte[] base64 = new byte[1000];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            base64len = DataReader.ReadElementContentAsBase64(base64, 0, base64.Length);

            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                strActbase64 += System.BitConverter.ToChar(base64, i);
            }

            CError.Compare(strActbase64, (strTextBase64 + strNumBase64), "Compare All Valid Base64");
            return TEST_PASS;
        }

        [Variation("ReadBase64 Element with all valid Num value", Pri = 0)]
        public int TestReadBase64_2()
        {
            int base64len = 0;
            byte[] base64 = new byte[1000];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME3);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            base64len = DataReader.ReadElementContentAsBase64(base64, 0, base64.Length);

            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                strActbase64 += System.BitConverter.ToChar(base64, i);
            }

            CError.Compare(strActbase64, strNumBase64, "Compare All Valid Base64");
            return TEST_PASS;
        }

        [Variation("ReadBase64 Element with all valid Text value")]
        public int TestReadBase64_3()
        {
            int base64len = 0;
            byte[] base64 = new byte[1000];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME4);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            base64len = DataReader.ReadElementContentAsBase64(base64, 0, base64.Length);

            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                strActbase64 += System.BitConverter.ToChar(base64, i);
            }

            CError.Compare(strActbase64, strTextBase64, "Compare All Valid Base64");
            return TEST_PASS;
        }

        [Variation("ReadBase64 Element with all valid value (from concatenation), Pri=0")]
        public int TestReadBase64_5()
        {
            int base64len = 0;
            byte[] base64 = new byte[1000];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME5);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            base64len = DataReader.ReadElementContentAsBase64(base64, 0, base64.Length);

            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                strActbase64 += System.BitConverter.ToChar(base64, i);
            }

            CError.Compare(strActbase64, (strTextBase64 + strNumBase64), "Compare All Valid Base64");
            return TEST_PASS;
        }

        [Variation("ReadBase64 Element with Long valid value (from concatenation), Pri=0")]
        public int TestReadBase64_6()
        {
            int base64len = 0;
            byte[] base64 = new byte[2000];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME6);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            base64len = DataReader.ReadElementContentAsBase64(base64, 0, base64.Length);

            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                strActbase64 += System.BitConverter.ToChar(base64, i);
            }

            string strExpbase64 = "";
            for (int i = 0; i < 10; i++)
                strExpbase64 += (strTextBase64 + strNumBase64);

            CError.Compare(strActbase64, strExpbase64, "Compare All Valid Base64");
            return TEST_PASS;
        }

        [Variation("ReadBase64 with count > buffer size")]
        public int ReadBase64_7()
        {
            return BoolToLTMResult(VerifyInvalidReadBase64(5, 0, 6, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBase64 with count < 0")]
        public int ReadBase64_8()
        {
            return BoolToLTMResult(VerifyInvalidReadBase64(5, 2, -1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBase64 with index > buffer size")]
        public int ReadBase64_9()
        {
            return BoolToLTMResult(VerifyInvalidReadBase64(5, 5, 1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBase64 with index < 0")]
        public int ReadBase64_10()
        {
            return BoolToLTMResult(VerifyInvalidReadBase64(5, -1, 1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBase64 with index + count exceeds buffer")]
        public int ReadBase64_11()
        {
            return BoolToLTMResult(VerifyInvalidReadBase64(5, 0, 10, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBase64 index & count =0")]
        public int ReadBase64_12()
        {
            byte[] buffer = new byte[5];
            int iCount = 0;

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            iCount = DataReader.ReadElementContentAsBase64(buffer, 0, 0);

            CError.Compare(iCount, 0, "has to be zero");
            return TEST_PASS;
        }

        [Variation("ReadBase64 Element multiple into same buffer (using offset), Pri=0")]
        public int TestReadBase64_13()
        {
            int base64len = 20;
            byte[] base64 = new byte[base64len];

            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME4);
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            string strActbase64 = "";
            for (int i = 0; i < base64len; i = i + 2)
            {
                DataReader.ReadElementContentAsBase64(base64, i, 2);
                strActbase64 = (System.BitConverter.ToChar(base64, i)).ToString();
                CError.Compare(string.Compare(strActbase64, 0, strTextBase64, i / 2, 1), 0, "Compare All Valid Base64");
            }

            return TEST_PASS;
        }

        [Variation("ReadBase64 with buffer == null")]
        public int TestReadBase64_14()
        {
            ReloadSource(EREADER_TYPE.BASE64_TEST);

            DataReader.PositionOnElement(ST_ELEM_NAME4);
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            try
            {
                DataReader.ReadElementContentAsBase64(null, 0, 0);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }

            return TEST_FAIL;
        }

        [Variation("ReadBase64 after failure")]
        public int TestReadBase64_15()
        {
            ReloadSource(EREADER_TYPE.BASE64_TEST);

            DataReader.PositionOnElement("ElemErr");
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            var line = ((IXmlLineInfo)DataReader.Internal).LinePosition + "ElemErr".Length + 1;

            byte[] buffer = new byte[10];
            int nRead = 0;
            try
            {
                nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 1);
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                if (IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader() || IsXmlValidatingReader() || IsCharCheckingReader())
                    CheckException("Xml_InvalidBase64Value", e);
                else
                    CheckXmlException("Xml_UserException", e, 1, line);
            }
            return TEST_PASS;
        }

        [Variation("Read after partial ReadBase64", Pri = 0)]
        public int TestReadBase64_16()
        {
            ReloadSource(EREADER_TYPE.BASE64_TEST);

            DataReader.PositionOnElement("ElemNum");
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            byte[] buffer = new byte[10];
            int nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 8);
            CError.Compare(nRead, 8, "0");

            DataReader.Read();
            CError.Compare(DataReader.NodeType, XmlNodeType.Text, "1vn");

            return TEST_PASS;
        }

        [Variation("Current node on multiple calls")]
        public int TestReadBase64_17()
        {
            ReloadSource(EREADER_TYPE.BASE64_TEST);
            DataReader.PositionOnElement("ElemNum");
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            byte[] buffer = new byte[30];

            int nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 2);
            CError.Compare(nRead, 2, "0");

            nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 23);
            CError.Compare(nRead, 22, "1");

            CError.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Nodetype not end element");
            CError.Compare(DataReader.Name, "ElemNum", "Nodetype not end element");

            return TEST_PASS;
        }

        [Variation("ReadBase64 with incomplete sequence")]
        public int TestTextReadBase64_23()
        {
            byte[] expected = new byte[] { 0, 16, 131, 16, 81 };

            byte[] buffer = new byte[10];
            string strxml = "<r><ROOT>ABCDEFG</ROOT></r>";
            ReloadSourceStr(strxml);

            DataReader.PositionOnElement("ROOT");
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            int result = 0;
            int nRead;
            while ((nRead = DataReader.ReadElementContentAsBase64(buffer, result, 1)) > 0)
                result += nRead;

            CError.Compare(result, expected.Length, "res");
            for (int i = 0; i < result; i++)
                CError.Compare(buffer[i], expected[i], "buffer[" + i + "]");

            return TEST_PASS;
        }

        [Variation("ReadBase64 when end tag doesn't exist")]
        public int TestTextReadBase64_24()
        {
            if (IsRoundTrippedReader())
                return TEST_SKIPPED;
            byte[] buffer = new byte[5000];
            string strxml = "<B>" + new string('c', 5000);
            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("B");
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            try
            {
                DataReader.ReadElementContentAsBase64(buffer, 0, 5000);
                CError.WriteLine("Accepted incomplete element");
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                CheckXmlException("Xml_UnexpectedEOFInElementContent", e, 1, 5004);
            }

            return TEST_PASS;
        }

        [Variation("ReadBase64 with whitespace in the middle")]
        public int TestTextReadBase64_26()
        {
            byte[] buffer = new byte[1];
            string strxml = "<abc> AQID  B            B  </abc>";
            int nRead;

            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("abc");
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            for (int i = 0; i < 4; i++)
            {
                nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 1);
                CError.Compare(nRead, 1, "res" + i);
                CError.Compare(buffer[0], (byte)(i + 1), "buffer " + i);
            }

            nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 1);
            CError.Compare(nRead, 0, "nRead 0");

            return TEST_PASS;
        }

        [Variation("ReadBase64 with = in the middle")]
        public int TestTextReadBase64_27()
        {
            byte[] buffer = new byte[1];
            string strxml = "<abc>AQI=ID</abc>";
            int nRead;

            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("abc");
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            for (int i = 0; i < 2; i++)
            {
                nRead = DataReader.ReadElementContentAsBase64(buffer, 0, 1);
                CError.Compare(nRead, 1, "res" + i);
                CError.Compare(buffer[0], (byte)(i + 1), "buffer " + i);
            }

            try
            {
                DataReader.ReadElementContentAsBase64(buffer, 0, 1);
                CError.WriteLine("ReadBase64 with = in the middle succeeded");
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                if (IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader() || IsXmlValidatingReader() || IsCharCheckingReader())
                    CheckException("Xml_InvalidBase64Value", e);
                else
                    CheckXmlException("Xml_UserException", e, 1, 6);
            }

            return TEST_PASS;
        }

        //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "10000" })]
        //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "1000000" })]
        //[Variation("ReadBase64 runs into an Overflow", Params = new object[] { "10000000" })]
        public int ReadBase64RunsIntoOverflow()
        {
            if (CheckCanReadBinaryContent() || IsSubtreeReader() || IsCharCheckingReader() || IsWrappedReader())
                return TEST_SKIPPED;
            int totalfilesize = Convert.ToInt32(CurVariation.Params[0].ToString());
            CError.WriteLine(" totalfilesize = " + totalfilesize);

            string ascii = new string('c', totalfilesize);

            byte[] bits = Encoding.Unicode.GetBytes(ascii);
            CError.WriteLineIgnore("Count = " + bits.Length);
            string base64str = Convert.ToBase64String(bits);

            string fileName = "bug105376_" + CurVariation.Params[0].ToString() + ".xml";
            MemoryStream mems = new MemoryStream();
            StreamWriter sw = new StreamWriter(mems);
            {
                sw.Write("<root><base64>");
                sw.Write(base64str);
                sw.Write("</base64></root>");
            }
            FilePathUtil.addStream(fileName, mems);
            ReloadSource(fileName);

            int SIZE = (totalfilesize - 30);
            int SIZE64 = SIZE * 3 / 4;

            DataReader.PositionOnElement("base64");

            CError.WriteLine("ReadBase64 method... ");
            CError.WriteLine(int.MaxValue);

            byte[] base64 = new byte[SIZE64];

            CError.WriteLine("SIZE64 = {0}", base64.Length);
            int startPos = 0;
            int readSize = 4096;

            int currentSize = 0;
            currentSize = DataReader.ReadElementContentAsBase64(base64, startPos, readSize);
            CError.Compare(currentSize, readSize, "Read other than first chunk");

            readSize = SIZE64 - readSize;
            currentSize = DataReader.ReadElementContentAsBase64(base64, startPos, readSize);
            CError.Compare(currentSize, readSize, "Read other than remaining Chunk Size");

            readSize = 0;
            currentSize = DataReader.ReadElementContentAsBase64(base64, startPos, readSize);
            CError.Compare(currentSize, 0, "Read other than Zero Bytes");

            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("WS:WireCompat:hex binary fails to send/return data after 1787 bytes")]
        public int TestReadBase64ReadsTheContent()
        {
            string filename = Path.Combine(TestData, "Common", "Bug99148.xml");
            ReloadSource(filename);

            DataReader.MoveToContent();
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            int bytes = -1;

            StringBuilder output = new StringBuilder();
            while (bytes != 0)
            {
                byte[] bbb = new byte[1024];
                bytes = DataReader.ReadElementContentAsBase64(bbb, 0, bbb.Length);
                for (int i = 0; i < bytes; i++)
                {
                    CError.Write(bbb[i].ToString());
                    output.AppendFormat(bbb[i].ToString());
                }
            }
            CError.WriteLine();
            CError.WriteLine("Length of the output : " + output.ToString().Length);
            CError.Compare(output.ToString().Length, 6072, "Expected Length : 6072");
            return TEST_PASS;
        }

        [Variation("SubtreeReader inserted attributes don't work with ReadContentAsBase64")]
        public int SubtreeReaderInsertedAttributesWorkWithReadContentAsBase64()
        {
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            string strxml1 = "<root xmlns='";
            string strxml2 = "'><bar/></root>";

            string[] binValue = new string[] { "AAECAwQFBgcI==", "0102030405060708090a0B0c" };
            for (int i = 0; i < binValue.Length; i++)
            {
                string strxml = strxml1 + binValue[i] + strxml2;
                ReloadSourceStr(strxml);
                DataReader.Read();
                DataReader.Read();
                using (XmlReader sr = DataReader.ReadSubtree())
                {
                    sr.Read();
                    sr.MoveToFirstAttribute();
                    sr.MoveToFirstAttribute();
                    byte[] bytes = new byte[4];
                    while ((sr.ReadContentAsBase64(bytes, 0, bytes.Length)) > 0) { }
                }
            }
            return TEST_PASS;
        }

        [Variation("call ReadContentAsBase64 on two or more nodes")]
        public int TestReadBase64_28()
        {
            string xml = "<elem0>123<elem1>123<elem2>123</elem2>123</elem1>123</elem0>";
            ReloadSource(new StringReader(xml));
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            byte[] buffer = new byte[3];
            int startPos = 0;
            int readSize = 3;
            int currentSize = 0;

            DataReader.Read();
            while (DataReader.Read())
            {
                currentSize = DataReader.ReadContentAsBase64(buffer, startPos, readSize);
                CError.Equals(currentSize, 2, "size");
                CError.Equals(buffer[0], (byte)215, "buffer1");
                CError.Equals(buffer[1], (byte)109, "buffer2");
                if (!(IsXPathNavigatorReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()))
                {
                    CError.WriteLine("LineNumber" + DataReader.LineNumber);
                    CError.WriteLine("LinePosition" + DataReader.LinePosition);
                }
            }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("read Base64 over invalid text node")]
        public int TestReadBase64_29()
        {
            string xml = "<elem0>12%45<elem1>12%45<elem2>12%45</elem2>12%45</elem1>12%45</elem0>";
            ReloadSource(new StringReader(xml));
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            byte[] buffer = new byte[5];
            int currentSize = 0;

            while (DataReader.Read())
            {
                DataReader.Read();
                try
                {
                    currentSize = DataReader.ReadContentAsBase64(buffer, 0, 5);
                    if (!(IsCharCheckingReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXmlValidatingReader() || IsXPathNavigatorReader()))
                        return TEST_FAIL;
                }
                catch (XmlException)
                {
                    CError.Compare(currentSize, 0, "size");
                }
            }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("goto to text node, ask got.Value, readcontentasBase64")]
        public int TestReadBase64_30()
        {
            string xml = "<elem0>123</elem0>";
            ReloadSourceStr(xml);
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            byte[] buffer = new byte[3];

            DataReader.Read();
            DataReader.Read();
            CError.Compare(DataReader.Value, "123", "value");
            CError.Compare(DataReader.ReadContentAsBase64(buffer, 0, 1), 1, "size");
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("goto to text node, readcontentasBase64, ask got.Value")]
        public int TestReadBase64_31()
        {
            string xml = "<elem0>123</elem0>";
            ReloadSourceStr(xml);
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            byte[] buffer = new byte[3];

            DataReader.Read();
            DataReader.Read();
            CError.Compare(DataReader.ReadContentAsBase64(buffer, 0, 1), 1, "size");
            CError.Compare(DataReader.Value, (IsCharCheckingReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXmlValidatingReader() || IsXPathNavigatorReader()) ? "123" : "3", "value");
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("goto to huge text node, read several chars with ReadContentAsBase64 and Move forward with .Read()")]
        public int TestReadBase64_32()
        {
            string xml = "<elem0>1234567 89 1234 123345 5676788 5567712 34567 89 1234 123345 5676788 55677</elem0>";
            ReloadSource(new StringReader(xml));
            byte[] buffer = new byte[5];

            DataReader.Read();
            DataReader.Read();
            try
            {
                CError.Compare(DataReader.ReadContentAsBase64(buffer, 0, 5), 5, "size");
            }
            catch (NotSupportedException) { return TEST_PASS; }
            DataReader.Read();
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("goto to huge text node with invalid chars, read several chars with ReadContentAsBase64 and Move forward with .Read()")]
        public int TestReadBase64_33()
        {
            string xml = "<elem0>123 $^ 56789 abcdefg hij klmn opqrst  12345 uvw xy ^ z</elem0>";
            ReloadSource(new StringReader(xml));
            byte[] buffer = new byte[5];

            DataReader.Read();
            DataReader.Read();
            try
            {
                CError.Compare(DataReader.ReadContentAsBase64(buffer, 0, 5), 5, "size");
                DataReader.Read();
            }
            catch (XmlException) { return TEST_PASS; }
            catch (NotSupportedException) { return TEST_PASS; }
            finally
            {
                DataReader.Close();
            }
            return TEST_FAIL;
        }

        //[Variation("ReadContentAsBase64 on an xmlns attribute", Param = "<foo xmlns='default'> <bar id='1'/> </foo>")]
        //[Variation("ReadContentAsBase64 on an xmlns:k attribute", Param = "<k:foo xmlns:k='default'> <k:bar id='1'/> </k:foo>")]
        //[Variation("ReadContentAsBase64 on an xml:space attribute", Param = "<foo xml:space='default'> <bar id='1'/> </foo>")]
        //[Variation("ReadContentAsBase64 on an xml:lang attribute", Param = "<foo xml:lang='default'> <bar id='1'/> </foo>")]
        public int TestReadBase64_34()
        {
            string xml = (string)CurVariation.Param;
            byte[] buffer = new byte[8];
            try
            {
                ReloadSource(new StringReader(xml));
                DataReader.Read();
                if (IsBinaryReader()) DataReader.Read();
                DataReader.MoveToAttribute(0);
                CError.Compare(DataReader.Value, "default", "value");
                CError.Equals(DataReader.ReadContentAsBase64(buffer, 0, 8), 5, "size");
            }
            catch (NotSupportedException) { }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("call ReadContentAsBase64 on two or more nodes and whitespace")]
        public int TestReadReadBase64_35()
        {
            string xml = @"<elem0>   123" + "\n" + @" <elem1>" + "\r" + @"123 
<elem2>
123  </elem2>" + "\r\n" + @"  123</elem1>          123           </elem0>";
            ReloadSource(new StringReader(xml));
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            byte[] buffer = new byte[3];
            int startPos = 0;
            int readSize = 3;
            int currentSize = 0;

            DataReader.Read();
            while (DataReader.Read())
            {
                currentSize = DataReader.ReadContentAsBase64(buffer, startPos, readSize);
                CError.Equals(currentSize, 2, "size");
                CError.Equals(buffer[0], (byte)215, "buffer1");
                CError.Equals(buffer[1], (byte)109, "buffer2");
                if (!(IsXPathNavigatorReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()))
                {
                    CError.WriteLine("LineNumber" + DataReader.LineNumber);
                    CError.WriteLine("LinePosition" + DataReader.LinePosition);
                }
            }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("call ReadContentAsBase64 on two or more nodes and whitespace after call Value")]
        public int TestReadReadBase64_36()
        {
            string xml = @"<elem0>   123" + "\n" + @" <elem1>" + "\r" + @"123 
<elem2>
123  </elem2>" + "\r\n" + @"  123</elem1>          123           </elem0>";
            ReloadSource(new StringReader(xml));
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            byte[] buffer = new byte[3];
            int startPos = 0;
            int readSize = 3;
            int currentSize = 0;

            DataReader.Read();
            while (DataReader.Read())
            {
                CError.Equals(DataReader.Value.Contains("123"), "Value");
                currentSize = DataReader.ReadContentAsBase64(buffer, startPos, readSize);
                CError.Equals(currentSize, 2, "size");
                CError.Equals(buffer[0], (byte)215, "buffer1");
                CError.Equals(buffer[1], (byte)109, "buffer2");
                if (!(IsXPathNavigatorReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()))
                {
                    CError.WriteLine("LineNumber" + DataReader.LineNumber);
                    CError.WriteLine("LinePosition" + DataReader.LinePosition);
                }
            }
            DataReader.Close();
            return TEST_PASS;
        }
    }
}
