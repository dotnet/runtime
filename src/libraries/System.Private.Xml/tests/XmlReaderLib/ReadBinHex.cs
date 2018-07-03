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
    public abstract partial class TCReadContentAsBinHex : TCXMLReaderBaseGeneral
    {
        public const string ST_ELEM_NAME1 = "ElemAll";
        public const string ST_ELEM_NAME2 = "ElemEmpty";
        public const string ST_ELEM_NAME3 = "ElemNum";
        public const string ST_ELEM_NAME4 = "ElemText";
        public const string ST_ELEM_NAME5 = "ElemNumText";
        public const string ST_ELEM_NAME6 = "ElemLong";
        public static string BinHexXml = "BinHex.xml";

        public const string strTextBinHex = "ABCDEF";
        public const string strNumBinHex = "0123456789";

        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);

            CreateTestFile(EREADER_TYPE.BINHEX_TEST);

            return ret;
        }

        public override int Terminate(object objParam)
        {
            if (DataReader.Internal != null)
            {
                while (DataReader.Read()) ;
                DataReader.Close();
            }
            return base.Terminate(objParam);
        }

        private bool VerifyInvalidReadBinHex(int iBufferSize, int iIndex, int iCount, Type exceptionType)
        {
            bool bPassed = false;
            byte[] buffer = new byte[iBufferSize];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            DataReader.Read();

            if (CheckCanReadBinaryContent()) return true;

            try
            {
                DataReader.ReadContentAsBinHex(buffer, iIndex, iCount);
            }
            catch (Exception e)
            {
                CError.WriteLine("Actual   exception:{0}", e.GetType().ToString());
                CError.WriteLine("Expected exception:{0}", exceptionType.ToString());
                bPassed = (e.GetType().ToString() == exceptionType.ToString());
            }
            return bPassed;
        }

        protected void TestInvalidNodeType(XmlNodeType nt)
        {
            ReloadSource();

            PositionOnNodeType(nt);
            string name = DataReader.Name;
            string value = DataReader.Value;

            byte[] buffer = new byte[1];
            if (CheckCanReadBinaryContent()) return;

            try
            {
                int nBytes = DataReader.ReadContentAsBinHex(buffer, 0, 1);
            }
            catch (InvalidOperationException)
            {
                return;
            }
            CError.Compare(false, "Invalid OP exception not thrown on wrong nodetype");
        }

        [Variation("ReadBinHex Element with all valid value")]
        public int TestReadBinHex_1()
        {
            int binhexlen = 0;
            byte[] binhex = new byte[1000];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            binhexlen = DataReader.ReadContentAsBinHex(binhex, 0, binhex.Length);

            string strActbinhex = "";
            for (int i = 0; i < binhexlen; i = i + 2)
            {
                strActbinhex += System.BitConverter.ToChar(binhex, i);
            }

            CError.Compare(strActbinhex, (strNumBinHex + strTextBinHex), "1. Compare All Valid BinHex");
            return TEST_PASS;
        }

        [Variation("ReadBinHex Element with all valid Num value", Pri = 0)]
        public int TestReadBinHex_2()
        {
            int BinHexlen = 0;
            byte[] BinHex = new byte[1000];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME3);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);

            string strActBinHex = "";
            for (int i = 0; i < BinHexlen; i = i + 2)
            {
                strActBinHex += System.BitConverter.ToChar(BinHex, i);
            }

            CError.Compare(strActBinHex, strNumBinHex, "Compare All Valid BinHex");
            return TEST_PASS;
        }

        [Variation("ReadBinHex Element with all valid Text value")]
        public int TestReadBinHex_3()
        {
            int BinHexlen = 0;
            byte[] BinHex = new byte[1000];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME4);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);

            string strActBinHex = "";
            for (int i = 0; i < BinHexlen; i = i + 2)
            {
                strActBinHex += System.BitConverter.ToChar(BinHex, i);
            }

            CError.Compare(strActBinHex, strTextBinHex, "Compare All Valid BinHex");
            return TEST_PASS;
        }

        public void Dump(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                CError.WriteLineIgnore("Byte" + i + ": " + bytes[i]);
            }
        }

        [Variation("ReadBinHex Element on CDATA", Pri = 0)]
        public int TestReadBinHex_4()
        {
            int BinHexlen = 0;
            byte[] BinHex = new byte[3];

            string xmlStr = "<root><![CDATA[ABCDEF]]></root>";
            ReloadSource(new StringReader(xmlStr));
            DataReader.PositionOnElement("root");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);
            CError.Compare(BinHexlen, 3, "BinHex");
            BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);
            CError.Compare(BinHexlen, 0, "BinHex");

            DataReader.Read();
            CError.Compare(DataReader.NodeType, XmlNodeType.None, "Not on none");
            return TEST_PASS;
        }

        [Variation("ReadBinHex Element with all valid value (from concatenation), Pri=0")]
        public int TestReadBinHex_5()
        {
            int BinHexlen = 0;
            byte[] BinHex = new byte[1000];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME5);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);

            string strActBinHex = "";
            for (int i = 0; i < BinHexlen; i = i + 2)
            {
                strActBinHex += System.BitConverter.ToChar(BinHex, i);
            }

            CError.Compare(strActBinHex, (strNumBinHex + strTextBinHex), "Compare All Valid BinHex");
            return TEST_PASS;
        }

        [Variation("ReadBinHex Element with all long valid value (from concatenation)")]
        public int TestReadBinHex_6()
        {
            int BinHexlen = 0;
            byte[] BinHex = new byte[2000];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME6);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            BinHexlen = DataReader.ReadContentAsBinHex(BinHex, 0, BinHex.Length);

            string strActBinHex = "";
            for (int i = 0; i < BinHexlen; i = i + 2)
            {
                strActBinHex += System.BitConverter.ToChar(BinHex, i);
            }

            string strExpBinHex = "";
            for (int i = 0; i < 10; i++)
                strExpBinHex += (strNumBinHex + strTextBinHex);

            CError.Compare(strActBinHex, strExpBinHex, "Compare All Valid BinHex");
            return TEST_PASS;
        }

        [Variation("ReadBinHex with count > buffer size")]
        public int TestReadBinHex_7()
        {
            return BoolToLTMResult(VerifyInvalidReadBinHex(5, 0, 6, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBinHex with count < 0")]
        public int TestReadBinHex_8()
        {
            return BoolToLTMResult(VerifyInvalidReadBinHex(5, 2, -1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBinHex with index > buffer size")]
        public int vReadBinHex_9()
        {
            return BoolToLTMResult(VerifyInvalidReadBinHex(5, 5, 1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBinHex with index < 0")]
        public int TestReadBinHex_10()
        {
            return BoolToLTMResult(VerifyInvalidReadBinHex(5, -1, 1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBinHex with index + count exceeds buffer")]
        public int TestReadBinHex_11()
        {
            return BoolToLTMResult(VerifyInvalidReadBinHex(5, 0, 10, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBinHex index & count =0")]
        public int TestReadBinHex_12()
        {
            byte[] buffer = new byte[5];
            int iCount = 0;

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            try
            {
                iCount = DataReader.ReadContentAsBinHex(buffer, 0, 0);
            }
            catch (Exception e)
            {
                CError.WriteLine(e.ToString());
                return TEST_FAIL;
            }

            CError.Compare(iCount, 0, "has to be zero");
            return TEST_PASS;
        }

        [Variation("ReadBinHex Element multiple into same buffer (using offset), Pri=0")]
        public int TestReadBinHex_13()
        {
            int BinHexlen = 10;
            byte[] BinHex = new byte[BinHexlen];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME4);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            string strActbinhex = "";
            for (int i = 0; i < BinHexlen; i = i + 2)
            {
                DataReader.ReadContentAsBinHex(BinHex, i, 2);
                strActbinhex = (System.BitConverter.ToChar(BinHex, i)).ToString();
                CError.WriteLine("Actual: " + strActbinhex + " Exp: " + strTextBinHex);
                CError.Compare(string.Compare(strActbinhex, 0, strTextBinHex, i / 2, 1), 0, "Compare All Valid Base64");
            }

            return TEST_PASS;
        }

        [Variation("ReadBinHex with buffer == null")]
        public int TestReadBinHex_14()
        {
            ReloadSource(EREADER_TYPE.BINHEX_TEST);

            DataReader.PositionOnElement(ST_ELEM_NAME4);
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            try
            {
                DataReader.ReadContentAsBinHex(null, 0, 0);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("Read after partial ReadBinHex")]
        public int TestReadBinHex_16()
        {
            ReloadSource(EREADER_TYPE.BINHEX_TEST);

            DataReader.PositionOnElement("ElemNum");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            byte[] buffer = new byte[10];
            int nRead = DataReader.ReadContentAsBinHex(buffer, 0, 8);
            CError.Compare(nRead, 8, "0");

            DataReader.Read();
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Element, "ElemText", string.Empty), "1vn");

            return TEST_PASS;
        }

        [Variation("Current node on multiple calls")]
        public int TestReadBinHex_17()
        {
            ReloadSource(EREADER_TYPE.BINHEX_TEST);

            DataReader.PositionOnElement("ElemNum");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            byte[] buffer = new byte[30];

            int nRead = DataReader.ReadContentAsBinHex(buffer, 0, 2);
            CError.Compare(nRead, 2, "0");

            nRead = DataReader.ReadContentAsBinHex(buffer, 0, 19);
            CError.Compare(nRead, 18, "1");
            CError.Compare(DataReader.VerifyNode(XmlNodeType.EndElement, "ElemNum", string.Empty), "1vn");

            return TEST_PASS;
        }

        [Variation("ReadBinHex with whitespace")]
        public int TestTextReadBinHex_21()
        {
            byte[] buffer = new byte[1];
            string strxml = "<abc> 1 1 B </abc>";
            ReloadSource(new StringReader(strxml));
            DataReader.PositionOnElement("abc");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            int result = 0;
            int nRead;
            while ((nRead = DataReader.ReadContentAsBinHex(buffer, 0, 1)) > 0)
                result += nRead;

            CError.Compare(result, 1, "res");
            CError.Compare(buffer[0], (byte)17, "buffer[0]");

            return TEST_PASS;
        }

        [Variation("ReadBinHex with odd number of chars")]
        public int TestTextReadBinHex_22()
        {
            byte[] buffer = new byte[1];
            string strxml = "<abc>11B</abc>";
            ReloadSource(new StringReader(strxml));
            DataReader.PositionOnElement("abc");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            int result = 0;
            int nRead;
            while ((nRead = DataReader.ReadContentAsBinHex(buffer, 0, 1)) > 0)
                result += nRead;

            CError.Compare(result, 1, "res");
            CError.Compare(buffer[0], (byte)17, "buffer[0]");

            return TEST_PASS;
        }

        [Variation("ReadBinHex when end tag doesn't exist")]
        public int TestTextReadBinHex_23()
        {
            if (IsRoundTrippedReader())
                return TEST_SKIPPED;
            byte[] buffer = new byte[5000];
            string strxml = "<B>" + new string('A', 5000);
            ReloadSource(new StringReader(strxml));
            DataReader.PositionOnElement("B");
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            try
            {
                DataReader.ReadContentAsBinHex(buffer, 0, 5000);
                CError.WriteLine("Accepted incomplete element");
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                CheckXmlException("Xml_UnexpectedEOFInElementContent", e, 1, 5004);
            }
            return TEST_PASS;
        }

        [Variation("WS:WireCompat:hex binary fails to send/return data after 1787 bytes")]
        public int TestTextReadBinHex_24()
        {
            string filename = Path.Combine(TestData, "Common", "Bug99148.xml");
            ReloadSource(filename);

            DataReader.MoveToContent();
            int bytes = -1;
            DataReader.Read();
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            StringBuilder output = new StringBuilder();
            while (bytes != 0)
            {
                byte[] bbb = new byte[1024];
                bytes = DataReader.ReadContentAsBinHex(bbb, 0, bbb.Length);
                for (int i = 0; i < bytes; i++)
                {
                    CError.Write(bbb[i].ToString());
                    output.AppendFormat(bbb[i].ToString());
                }
            }

            CError.WriteLine();
            CError.WriteLine("Length of the output : " + output.ToString().Length);
            return (CError.Compare(output.ToString().Length, 1735, "Expected Length : 1735")) ? TEST_PASS : TEST_FAIL;
        }
    }

    [InheritRequired()]
    public abstract partial class TCReadElementContentAsBinHex : TCXMLReaderBaseGeneral
    {
        public const string ST_ELEM_NAME1 = "ElemAll";
        public const string ST_ELEM_NAME2 = "ElemEmpty";
        public const string ST_ELEM_NAME3 = "ElemNum";
        public const string ST_ELEM_NAME4 = "ElemText";
        public const string ST_ELEM_NAME5 = "ElemNumText";
        public const string ST_ELEM_NAME6 = "ElemLong";
        public static string BinHexXml = "BinHex.xml";

        public const string strTextBinHex = "ABCDEF";
        public const string strNumBinHex = "0123456789";

        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);

            CreateTestFile(EREADER_TYPE.BINHEX_TEST);

            return ret;
        }

        public override int Terminate(object objParam)
        {
            DataReader.Close();

            return base.Terminate(objParam);
        }
        private bool VerifyInvalidReadBinHex(int iBufferSize, int iIndex, int iCount, Type exceptionType)
        {
            bool bPassed = false;
            byte[] buffer = new byte[iBufferSize];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            if (CheckCanReadBinaryContent()) return true;

            try
            {
                DataReader.ReadElementContentAsBinHex(buffer, iIndex, iCount);
            }
            catch (Exception e)
            {
                CError.WriteLine("Actual   exception:{0}", e.GetType().ToString());
                CError.WriteLine("Expected exception:{0}", exceptionType.ToString());
                bPassed = (e.GetType().ToString() == exceptionType.ToString());
            }

            return bPassed;
        }

        protected void TestInvalidNodeType(XmlNodeType nt)
        {
            ReloadSource();

            PositionOnNodeType(nt);
            string name = DataReader.Name;
            string value = DataReader.Value;

            byte[] buffer = new byte[1];
            if (CheckCanReadBinaryContent()) return;
            try
            {
                int nBytes = DataReader.ReadElementContentAsBinHex(buffer, 0, 1);
            }
            catch (InvalidOperationException)
            {
                return;
            }
            CError.Compare(false, "Invalid OP exception not thrown on wrong nodetype");
        }

        [Variation("ReadBinHex Element with all valid value")]
        public int TestReadBinHex_1()
        {
            int binhexlen = 0;
            byte[] binhex = new byte[1000];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            binhexlen = DataReader.ReadElementContentAsBinHex(binhex, 0, binhex.Length);

            string strActbinhex = "";
            for (int i = 0; i < binhexlen; i = i + 2)
            {
                strActbinhex += System.BitConverter.ToChar(binhex, i);
            }

            CError.Compare(strActbinhex, (strNumBinHex + strTextBinHex), "1. Compare All Valid BinHex");
            return TEST_PASS;
        }

        [Variation("ReadBinHex Element with all valid Num value", Pri = 0)]
        public int TestReadBinHex_2()
        {
            int BinHexlen = 0;
            byte[] BinHex = new byte[1000];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME3);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            BinHexlen = DataReader.ReadElementContentAsBinHex(BinHex, 0, BinHex.Length);

            string strActBinHex = "";
            for (int i = 0; i < BinHexlen; i = i + 2)
            {
                strActBinHex += System.BitConverter.ToChar(BinHex, i);
            }

            CError.Compare(strActBinHex, strNumBinHex, "Compare All Valid BinHex");
            return TEST_PASS;
        }

        [Variation("ReadBinHex Element with all valid Text value")]
        public int TestReadBinHex_3()
        {
            int BinHexlen = 0;
            byte[] BinHex = new byte[1000];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME4);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            BinHexlen = DataReader.ReadElementContentAsBinHex(BinHex, 0, BinHex.Length);

            string strActBinHex = "";
            for (int i = 0; i < BinHexlen; i = i + 2)
            {
                strActBinHex += System.BitConverter.ToChar(BinHex, i);
            }

            CError.Compare(strActBinHex, strTextBinHex, "Compare All Valid BinHex");
            return TEST_PASS;
        }

        [Variation("ReadBinHex Element with Comments and PIs", Pri = 0)]
        public int TestReadBinHex_4()
        {
            int BinHexlen = 0;
            byte[] BinHex = new byte[3];

            ReloadSource(new StringReader("<root>AB<!--Comment-->CD<?pi target?>EF</root>"));
            DataReader.PositionOnElement("root");
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            BinHexlen = DataReader.ReadElementContentAsBinHex(BinHex, 0, BinHex.Length);
            CError.Compare(BinHexlen, 3, "BinHex");

            return TEST_PASS;
        }

        [Variation("ReadBinHex Element with all valid value (from concatenation), Pri=0")]
        public int TestReadBinHex_5()
        {
            int BinHexlen = 0;
            byte[] BinHex = new byte[1000];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME5);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            BinHexlen = DataReader.ReadElementContentAsBinHex(BinHex, 0, BinHex.Length);

            string strActBinHex = "";
            for (int i = 0; i < BinHexlen; i = i + 2)
            {
                strActBinHex += System.BitConverter.ToChar(BinHex, i);
            }

            CError.Compare(strActBinHex, (strNumBinHex + strTextBinHex), "Compare All Valid BinHex");
            return TEST_PASS;
        }

        [Variation("ReadBinHex Element with all long valid value (from concatenation)")]
        public int TestReadBinHex_6()
        {
            int BinHexlen = 0;
            byte[] BinHex = new byte[2000];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME6);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            BinHexlen = DataReader.ReadElementContentAsBinHex(BinHex, 0, BinHex.Length);

            string strActBinHex = "";
            for (int i = 0; i < BinHexlen; i = i + 2)
            {
                strActBinHex += System.BitConverter.ToChar(BinHex, i);
            }

            string strExpBinHex = "";
            for (int i = 0; i < 10; i++)
                strExpBinHex += (strNumBinHex + strTextBinHex);

            CError.Compare(strActBinHex, strExpBinHex, "Compare All Valid BinHex");
            return TEST_PASS;
        }

        [Variation("ReadBinHex with count > buffer size")]
        public int TestReadBinHex_7()
        {
            return BoolToLTMResult(VerifyInvalidReadBinHex(5, 0, 6, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBinHex with count < 0")]
        public int TestReadBinHex_8()
        {
            return BoolToLTMResult(VerifyInvalidReadBinHex(5, 2, -1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBinHex with index > buffer size")]
        public int vReadBinHex_9()
        {
            return BoolToLTMResult(VerifyInvalidReadBinHex(5, 5, 1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBinHex with index < 0")]
        public int TestReadBinHex_10()
        {
            return BoolToLTMResult(VerifyInvalidReadBinHex(5, -1, 1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBinHex with index + count exceeds buffer")]
        public int TestReadBinHex_11()
        {
            return BoolToLTMResult(VerifyInvalidReadBinHex(5, 0, 10, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadBinHex index & count =0")]
        public int TestReadBinHex_12()
        {
            byte[] buffer = new byte[5];
            int iCount = 0;

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME1);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            try
            {
                iCount = DataReader.ReadElementContentAsBinHex(buffer, 0, 0);
            }
            catch (Exception e)
            {
                CError.WriteLine(e.ToString());
                return TEST_FAIL;
            }

            CError.Compare(iCount, 0, "has to be zero");
            return TEST_PASS;
        }

        [Variation("ReadBinHex Element multiple into same buffer (using offset), Pri=0")]
        public int TestReadBinHex_13()
        {
            int BinHexlen = 10;
            byte[] BinHex = new byte[BinHexlen];

            ReloadSource(EREADER_TYPE.BINHEX_TEST);
            DataReader.PositionOnElement(ST_ELEM_NAME4);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            string strActbinhex = "";
            for (int i = 0; i < BinHexlen; i = i + 2)
            {
                DataReader.ReadElementContentAsBinHex(BinHex, i, 2);
                strActbinhex = (System.BitConverter.ToChar(BinHex, i)).ToString();
                CError.WriteLine("Actual: " + strActbinhex + " Exp: " + strTextBinHex);
                CError.Compare(string.Compare(strActbinhex, 0, strTextBinHex, i / 2, 1), 0, "Compare All Valid Base64");
            }
            return TEST_PASS;
        }

        [Variation("ReadBinHex with buffer == null")]
        public int TestReadBinHex_14()
        {
            ReloadSource(EREADER_TYPE.BINHEX_TEST);

            DataReader.PositionOnElement(ST_ELEM_NAME4);
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            try
            {
                DataReader.ReadElementContentAsBinHex(null, 0, 0);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("Read after partial ReadBinHex")]
        public int TestReadBinHex_16()
        {
            ReloadSource(EREADER_TYPE.BINHEX_TEST);

            DataReader.PositionOnElement("ElemNum");
            if (CheckCanReadBinaryContent()) return TEST_PASS;

            byte[] buffer = new byte[10];
            int nRead = DataReader.ReadElementContentAsBinHex(buffer, 0, 8);
            CError.Compare(nRead, 8, "0");

            DataReader.Read();
            CError.Compare(DataReader.NodeType, XmlNodeType.Text, "Not on text node");
            return TEST_PASS;
        }


        [Variation("No op node types")]
        public int TestReadBinHex_18()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;
            TestInvalidNodeType(XmlNodeType.Text);
            TestInvalidNodeType(XmlNodeType.Attribute);
            TestInvalidNodeType(XmlNodeType.Whitespace);
            TestInvalidNodeType(XmlNodeType.ProcessingInstruction);
            TestInvalidNodeType(XmlNodeType.CDATA);

            if (!(IsCoreReader() || IsRoundTrippedReader()))
            {
                TestInvalidNodeType(XmlNodeType.EndEntity);
                TestInvalidNodeType(XmlNodeType.EntityReference);
            }
            return TEST_PASS;
        }

        [Variation("ReadBinHex with whitespace")]
        public int TestTextReadBinHex_21()
        {
            byte[] buffer = new byte[1];
            string strxml = "<abc> 1 1 B </abc>";
            ReloadSource(new StringReader(strxml));
            DataReader.PositionOnElement("abc");
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            int result = 0;
            int nRead;
            while ((nRead = DataReader.ReadElementContentAsBinHex(buffer, 0, 1)) > 0)
                result += nRead;

            CError.Compare(result, 1, "res");
            CError.Compare(buffer[0], (byte)17, "buffer[0]");

            return TEST_PASS;
        }

        [Variation("ReadBinHex with odd number of chars")]
        public int TestTextReadBinHex_22()
        {
            byte[] buffer = new byte[1];
            string strxml = "<abc>11B</abc>";
            ReloadSource(new StringReader(strxml));
            DataReader.PositionOnElement("abc");
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            int result = 0;
            int nRead;
            while ((nRead = DataReader.ReadElementContentAsBinHex(buffer, 0, 1)) > 0)
                result += nRead;

            CError.Compare(result, 1, "res");
            CError.Compare(buffer[0], (byte)17, "buffer[0]");

            return TEST_PASS;
        }

        [Variation("ReadBinHex when end tag doesn't exist")]
        public int TestTextReadBinHex_23()
        {
            if (IsRoundTrippedReader())
                return TEST_SKIPPED;
            byte[] buffer = new byte[5000];
            string strxml = "<B>" + new string('A', 5000);
            ReloadSource(new StringReader(strxml));
            DataReader.PositionOnElement("B");
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            try
            {
                DataReader.ReadElementContentAsBinHex(buffer, 0, 5000);
                CError.WriteLine("Accepted incomplete element");
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                CheckXmlException("Xml_UnexpectedEOFInElementContent", e, 1, 5004);
            }
            return TEST_PASS;
        }

        [Variation("WS:WireCompat:hex binary fails to send/return data after 1787 bytes")]
        public int TestTextReadBinHex_24()
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
                bytes = DataReader.ReadElementContentAsBinHex(bbb, 0, bbb.Length);
                for (int i = 0; i < bytes; i++)
                {
                    CError.Write(bbb[i].ToString());
                    output.AppendFormat(bbb[i].ToString());
                }
            }

            CError.WriteLine();
            CError.WriteLine("Length of the output : " + output.ToString().Length);
            return (CError.Compare(output.ToString().Length, 1735, "Expected Length : 1735")) ? TEST_PASS : TEST_FAIL;
        }

        [Variation("430329: SubtreeReader inserted attributes don't work with ReadContentAsBinHex")]
        public int TestReadBinHex_430329()
        {
            if (IsCustomReader() || IsXsltReader() || IsBinaryReader()) return TEST_SKIPPED;

            string strxml = "<root xmlns='0102030405060708090a0B0c'><bar/></root>";
            ReloadSource(new StringReader(strxml));
            DataReader.Read();
            DataReader.Read();
            using (XmlReader sr = DataReader.ReadSubtree())
            {
                sr.Read();
                sr.MoveToFirstAttribute();
                sr.MoveToFirstAttribute();
                byte[] bytes = new byte[4];
                while ((sr.ReadContentAsBinHex(bytes, 0, bytes.Length)) > 0)
                {
                    if (!(IsXPathNavigatorReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()))
                    {
                        CError.WriteLine("LineNumber" + DataReader.LineNumber);
                        CError.WriteLine("LinePosition" + DataReader.LinePosition);
                    }
                }
            }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("ReadBinHex with = in the middle")]
        public int TestReadBinHex_27()
        {
            byte[] buffer = new byte[1];
            string strxml = "<abc>1=2</abc>";
            ReloadSource(new StringReader(strxml));
            DataReader.PositionOnElement("abc");
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            try
            {
                DataReader.ReadElementContentAsBinHex(buffer, 0, 1);
                CError.Compare(false, "ReadBinHex with = in the middle succeeded");
            }
            catch (XmlException) { return TEST_PASS; }
            finally { DataReader.Close(); }
            return TEST_FAIL;
        }

        //[Variation("ReadBinHex runs into an Overflow", Params = new object[] { "1000000" })]
        //[Variation("ReadBinHex runs into an Overflow", Params = new object[] { "10000000" })]
        public int TestReadBinHex_105376()
        {
            int totalfilesize = Convert.ToInt32(CurVariation.Params[0].ToString());
            CError.WriteLine(" totalfilesize = " + totalfilesize);

            string ascii = new string('c', totalfilesize);

            byte[] bits = Encoding.Unicode.GetBytes(ascii);
            CError.WriteLineIgnore("Count = " + bits.Length);
            string base64str = Convert.ToBase64String(bits);

            string fileName = "bug105376c_" + CurVariation.Params[0].ToString() + ".xml";
            MemoryStream mems = new MemoryStream();
            StreamWriter sw = new StreamWriter(mems);
            sw.Write("<root><base64>");
            sw.Write(base64str);
            sw.Write("</base64></root>");
            sw.Flush();//sw.Close();
            FilePathUtil.addStream(fileName, mems);
            ReloadSource(fileName);

            int SIZE = (totalfilesize - 30);
            int SIZE64 = SIZE * 3 / 4;

            DataReader.PositionOnElement("base64");
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            byte[] base64 = new byte[SIZE64];

            try
            {
                DataReader.ReadElementContentAsBinHex(base64, 0, 4096);
                return TEST_FAIL;
            }
            catch (XmlException) { DataReader.Close(); return TEST_PASS; }
            finally { DataReader.Close(); }
        }

        [Variation("call ReadContentAsBinHex on two or more nodes")]
        public int TestReadBinHex_28()
        {
            string xml = "<elem0> 11B <elem1> 11B <elem2> 11B </elem2> 11B </elem1> 11B </elem0>";
            ReloadSource(new StringReader(xml));
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            byte[] buffer = new byte[3];
            int startPos = 0;
            int readSize = 3;
            int currentSize = 0;

            DataReader.Read();
            while (DataReader.Read())
            {
                currentSize = DataReader.ReadContentAsBinHex(buffer, startPos, readSize);
                CError.Equals(currentSize, 1, "size");
                CError.Equals(buffer[0], (byte)17, "buffer");
                if (!(IsXPathNavigatorReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()))
                {
                    CError.WriteLine("LineNumber" + DataReader.LineNumber);
                    CError.WriteLine("LinePosition" + DataReader.LinePosition);
                }
            }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("read BinHex over invalid text node")]
        public int TestReadBinHex_29()
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
                    currentSize = DataReader.ReadContentAsBinHex(buffer, 0, 5);
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

        [Variation("goto to text node, ask got.Value, readcontentasBinHex")]
        public int TestReadBinHex_30()
        {
            string xml = "<elem0>123</elem0>";
            ReloadSource(new StringReader(xml));
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            byte[] buffer = new byte[3];

            DataReader.Read();
            DataReader.Read();
            CError.Compare(DataReader.Value, "123", "value");
            CError.Compare(DataReader.ReadContentAsBinHex(buffer, 0, 1), 1, "size");
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("goto to text node, readcontentasBinHex, ask got.Value")]
        public int TestReadBinHex_31()
        {
            string xml = "<elem0>123</elem0>";
            ReloadSource(new StringReader(xml));
            if (CheckCanReadBinaryContent()) return TEST_PASS;
            byte[] buffer = new byte[3];

            DataReader.Read();
            DataReader.Read();
            CError.Compare(DataReader.ReadContentAsBinHex(buffer, 0, 1), 1, "size");
            CError.Compare(DataReader.Value, (IsCharCheckingReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXmlValidatingReader() || IsXPathNavigatorReader()) ? "123" : "3", "value");
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("goto to huge text node, read several chars with ReadContentAsBinHex and Move forward with .Read()")]
        public int TestReadBinHex_32()
        {
            string xml = "<elem0>1234567 89 1234 123345 5676788 5567712 34567 89 1234 123345 5676788 55677</elem0>";
            ReloadSource(new StringReader(xml));
            byte[] buffer = new byte[5];

            DataReader.Read();
            DataReader.Read();
            try
            {
                CError.Compare(DataReader.ReadContentAsBinHex(buffer, 0, 5), 5, "size");
            }
            catch (NotSupportedException) { return TEST_PASS; }
            DataReader.Read();
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("goto to huge text node with invalid chars, read several chars with ReadContentAsBinHex and Move forward with .Read()")]
        public int TestReadBinHex_33()
        {
            string xml = "<elem0>123 $^ 56789 abcdefg hij klmn opqrst  12345 uvw xy ^ z</elem0>";
            ReloadSource(new StringReader(xml));
            byte[] buffer = new byte[5];

            DataReader.Read();
            DataReader.Read();
            try
            {
                CError.Compare(DataReader.ReadContentAsBinHex(buffer, 0, 5), 5, "size");
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

        //[Variation("ReadContentAsBinHex on an xmlns attribute", Param = "<foo xmlns='default'> <bar > id='1'/> </foo>")]
        //[Variation("ReadContentAsBinHex on an xmlns:k attribute", Param = "<k:foo xmlns:k='default'> <k:bar id='1'/> </k:foo>")]
        //[Variation("ReadContentAsBinHex on an xml:space attribute", Param = "<foo xml:space='default'> <bar > id='1'/> </foo>")]
        //[Variation("ReadContentAsBinHex on an xml:lang attribute", Param = "<foo xml:lang='default'> <bar > id='1'/> </foo>")]
        public int TestBinHex_34()
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
                CError.Equals(DataReader.ReadContentAsBinHex(buffer, 0, 8), 5, "size");
                CError.Equals(false, "No exception");
            }
            catch (XmlException) { return TEST_PASS; }
            catch (NotSupportedException) { return TEST_PASS; }
            finally
            {
                DataReader.Close();
            }
            return TEST_FAIL;
        }

        [Variation("call ReadContentAsBinHex on two or more nodes and whitespace")]
        public int TestReadBinHex_35()
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
                currentSize = DataReader.ReadContentAsBinHex(buffer, startPos, readSize);
                CError.Equals(currentSize, 1, "size");
                CError.Equals(buffer[0], (byte)18, "buffer");
                if (!(IsXPathNavigatorReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()))
                {
                    CError.WriteLine("LineNumber" + DataReader.LineNumber);
                    CError.WriteLine("LinePosition" + DataReader.LinePosition);
                }
            }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("call ReadContentAsBinHex on two or more nodes and whitespace after call Value")]
        public int TestReadBinHex_36()
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
                currentSize = DataReader.ReadContentAsBinHex(buffer, startPos, readSize);
                CError.Equals(currentSize, 1, "size");
                CError.Equals(buffer[0], (byte)18, "buffer");
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
