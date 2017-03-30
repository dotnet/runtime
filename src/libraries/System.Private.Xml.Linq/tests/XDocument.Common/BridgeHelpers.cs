// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XmlDiff;
using Microsoft.Test.ModuleCore;
using XmlCoreTest.Common;

namespace CoreXml.Test.XLinq
{
    public class BridgeHelpers : XLinqTestCase
    {
        private XmlDiff _diff = null;
        private XmlReaderSettings _rsx;
        private XmlReaderSettings _rsxNoWs;
        public BridgeHelpers()
        {
            _diff = new XmlDiff();
            _rsx = new XmlReaderSettings();
            _rsx.DtdProcessing = DtdProcessing.Ignore;

            _rsxNoWs = new XmlReaderSettings();
            _rsxNoWs.DtdProcessing = DtdProcessing.Ignore;
            _rsxNoWs.IgnoreWhitespace = true;
            Init();
        }

        //BridgeHelpers constants
        public const string TestSaveFileName = "testSave.xml";
        public const string ST_XML = "xml";
        public const string strBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        public const string strBinHex = "0123456789ABCDEF";
        public static string pLbNormEnt1 = "se3_1.ent";
        public static string pLbNormEnt2 = "se3_2.ent";
        public static string pGenericXml = "Generic.xml";
        public static string pXsltCopyStylesheet = "XsltCtest.xsl";
        public static string pValidXDR = "XdrFile.xml";

        //Reader constants        
        public const string ST_TEST_NAME = "ISDEFAULT";
        public const string ST_ENTTEST_NAME = "ENTITY1";
        public const string ST_MARKUP_TEST_NAME = "CHARS2";
        public const string ST_EMPTY_TEST_NAME = "EMPTY1";
        public static string pJunkFileXml = "Junk.xml";
        public static string pBase64Xml = "Base64.xml";
        public static string pBinHexXml = "BinHex.xml";
        public const string ST_EXPAND_ENTITIES = "xxx>xxxBxxxDxxxe1fooxxx";
        public const string ST_EXPAND_ENTITIES2 = "xxx&gt;xxxBxxxDxxxe1fooxxx";
        public const string ST_ENT1_ATT_EXPAND_ENTITIES = "xxx<xxxAxxxCxxxe1fooxxx";
        public const string ST_EXPAND_CHAR_ENTITIES = "xxx>xxxBxxxDxxx";
        public const string ST_GEN_ENT_NAME = "e1";
        public const string ST_ENT1_ATT_EXPAND_CHAR_ENTITIES4 = "xxx&lt;xxxAxxxCxxxe1fooxxx";
        public const string ST_IGNORE_ENTITIES = "xxx&gt;xxx&#66;xxx&#x44;xxx&e1;xxx";
        public static string strNamespace = "http://www.foo.com";
        public static string strAttr = "Attr";
        public const string ST_D1_VALUE = "d1value";
        public const string ST_GEN_ENT_VALUE = "e1foo";

        //Writer helpers      
        public XmlWriter CreateWriter()
        {
            XDocument doc = new XDocument();
            return CreateWriter(doc);
        }

        public XmlWriter CreateWriter(XDocument d)
        {
            return d.CreateWriter();
        }

        //Reader helpers
        public XmlReader GetReader()
        {
            string file = Path.Combine("TestData", "XmlReader", "API", pGenericXml);
            Stream s = FilePathUtil.getStream(file);

            if (s == null)
            {
                throw new FileNotFoundException("File Not Found: " + pGenericXml);
            }

            using (XmlReader r = XmlReader.Create(s, _rsx))
            {
                XDocument doc = XDocument.Load(r, LoadOptions.PreserveWhitespace);
                return doc.CreateReader();
            }
        }

        public XmlReader GetPGenericXmlReader()
        {
            string file = Path.Combine("TestData", "XmlReader", "API", pGenericXml);
            {
                Stream s = FilePathUtil.getStreamDirect(file);

                if (s == null)
                {
                    throw new FileNotFoundException("File Not Found: " + pGenericXml);
                }

                using (XmlReader r = XmlReader.Create(s, _rsx))
                {
                    XDocument doc = XDocument.Load(r, LoadOptions.PreserveWhitespace);
                    return doc.CreateReader();
                }
            }
        }

        public XmlReader GetReader(string strSource, bool preserveWhitespace)
        {
            using (XmlReader r = XmlReader.Create(FilePathUtil.getStream(strSource), preserveWhitespace ? _rsx : _rsxNoWs))
            {
                XDocument doc = XDocument.Load(r, preserveWhitespace ? LoadOptions.PreserveWhitespace : LoadOptions.None);
                return doc.CreateReader();
            }
        }

        public XmlReader GetReader(string strSource)
        {
            using (XmlReader r = XmlReader.Create(FilePathUtil.getStream(strSource), _rsx))
            {
                XDocument doc = XDocument.Load(r, LoadOptions.PreserveWhitespace);
                return doc.CreateReader();
            }
        }

        public XmlReader GetReader(TextReader sr)
        {
            using (XmlReader r = XmlReader.Create(sr, _rsx))
            {
                XDocument doc = XDocument.Load(r, LoadOptions.PreserveWhitespace);
                return doc.CreateReader();
            }
        }

        public XmlReader GetReader(XmlReader r)
        {
            XDocument doc = XDocument.Load(r);
            return doc.CreateReader();
        }

        public XmlReader GetReader(Stream stream)
        {
            using (XmlReader r = XmlReader.Create(stream, _rsx))
            {
                XDocument doc = XDocument.Load(r);
                return doc.CreateReader();
            }
        }

        public XmlReader GetReaderStr(string xml)
        {
            using (XmlReader r = XmlReader.Create(new StringReader(xml), _rsx))
            {
                XDocument doc = XDocument.Load(r, LoadOptions.PreserveWhitespace);
                return doc.CreateReader();
            }
        }

        public static string GetTestFileName()
        {
            return Path.Combine("TestData", "XmlReader", "API", pGenericXml);
        }

        public bool CompareReader(XDocument doc, string expectedXml)
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ConformanceLevel = ConformanceLevel.Auto;
            rs.DtdProcessing = DtdProcessing.Ignore;
            rs.CloseInput = true;
            _diff.Option = XmlDiffOption.IgnoreAttributeOrder;

            using (XmlReader r1 = doc.CreateReader())
            using (XmlReader r2 = XmlReader.Create(new StringReader(expectedXml), rs))
            {
                if (!_diff.Compare(r1, r2))
                {
                    TestLog.WriteLine("Mismatch : expected: " + expectedXml + "\n actual: " + doc.ToString());
                    return false;
                }
            }
            return true;
        }

        public bool CompareReader(XmlReader r1, string expectedXml)
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ConformanceLevel = ConformanceLevel.Auto;
            rs.CloseInput = true;
            _diff.Option = XmlDiffOption.IgnoreAttributeOrder;

            using (XmlReader r2 = XmlReader.Create(new StringReader(expectedXml), rs))
            {
                if (!_diff.Compare(r1, r2))
                {
                    TestLog.WriteLine("Mismatch : expected: " + expectedXml + "\n actual: ");
                    return false;
                }
            }
            return true;
        }

        public string GetString(string fileName)
        {
            string strRet = string.Empty;
            Stream temp = FilePathUtil.getStream(fileName);
            StreamReader srTemp = new StreamReader(temp);
            strRet = srTemp.ReadToEnd();
            srTemp.Dispose();
            temp.Dispose();

            return strRet;
        }

        public bool CompareBaseline(XDocument doc, string baselineFile)
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ConformanceLevel = ConformanceLevel.Auto;
            rs.DtdProcessing = DtdProcessing.Ignore;
            rs.CloseInput = true;
            _diff.Option = XmlDiffOption.IgnoreAttributeOrder;

            using (XmlReader r1 = XmlReader.Create(FilePathUtil.getStream(FullPath(baselineFile)), rs))
            using (XmlReader r2 = doc.CreateReader())
            {
                if (!_diff.Compare(r1, r2))
                {
                    TestLog.WriteLine("Mismatch : expected: " + this.GetString(FullPath(baselineFile)) + "\n actual: " + doc.ToString());
                    return false;
                }
            }
            return true;
        }

        public string FullPath(string fileName)
        {
            if (fileName == null || fileName == string.Empty)
                return fileName;
            return Path.Combine("TestData", "XmlWriter2", fileName);
        }

        public static void EnsureSpace(ref byte[] buffer, int len)
        {
            if (len >= buffer.Length)
            {
                int originalLen = buffer.Length;
                byte[] newBuffer = new byte[(int)(len * 2)];
                for (int i = 0; i < originalLen; newBuffer[i] = buffer[i++])
                {
                    // Intentionally Empty
                }
                buffer = newBuffer;
            }
        }

        public static void WriteToBuffer(ref byte[] destBuff, ref int len, byte srcByte)
        {
            EnsureSpace(ref destBuff, len);
            destBuff[len++] = srcByte;
            return;
        }

        public static void WriteToBuffer(ref byte[] destBuff, ref int len, byte[] srcBuff)
        {
            int srcArrayLen = srcBuff.Length;
            WriteToBuffer(ref destBuff, ref len, srcBuff, 0, (int)srcArrayLen);
            return;
        }

        public static void WriteToBuffer(ref byte[] destBuff, ref int destStart, byte[] srcBuff, int srcStart, int count)
        {
            EnsureSpace(ref destBuff, destStart + count - 1);
            for (int i = srcStart; i < srcStart + count; i++)
            {
                destBuff[destStart++] = srcBuff[i];
            }
        }

        public static void WriteToBuffer(ref byte[] destBuffer, ref int destBuffLen, string strValue)
        {
            for (int i = 0; i < strValue.Length; i++)
            {
                WriteToBuffer(ref destBuffer, ref destBuffLen, System.BitConverter.GetBytes(strValue[i]));
            }
            WriteToBuffer(ref destBuffer, ref destBuffLen, System.BitConverter.GetBytes('\0'));
        }

        public void CheckClosedState(WriteState ws)
        {
            TestLog.Compare(ws, WriteState.Closed, "WriteState should be Closed");
        }

        public void CheckErrorState(WriteState ws)
        {
            TestLog.Compare(ws, WriteState.Error, "WriteState should be Error");
        }

        public void CheckElementState(WriteState ws)
        {
            TestLog.Compare(ws, WriteState.Element, "WriteState should be Element");
        }

        public void VerifyInvalidWrite(string methodName, int iBufferSize, int iIndex, int iCount, Type exceptionType)
        {
            byte[] byteBuffer = new byte[iBufferSize];
            for (int i = 0; i < iBufferSize; i++)
                byteBuffer[i] = (byte)(i + '0');

            char[] charBuffer = new char[iBufferSize];
            for (int i = 0; i < iBufferSize; i++)
                charBuffer[i] = (char)(i + '0');

            XDocument doc = new XDocument();
            XmlWriter w = CreateWriter(doc);
            w.WriteStartElement("root");
            try
            {
                switch (methodName)
                {
                    case "WriteBase64":
                        w.WriteBase64(byteBuffer, iIndex, iCount);
                        break;
                    case "WriteRaw":
                        w.WriteRaw(charBuffer, iIndex, iCount);
                        break;
                    case "WriteBinHex":
                        w.WriteBinHex(byteBuffer, iIndex, iCount);
                        break;
                    case "WriteChars":
                        w.WriteChars(charBuffer, iIndex, iCount);
                        break;
                    default:
                        TestLog.Compare(false, "Unexpected method name " + methodName);
                        break;
                }
            }
            catch (Exception e)
            {
                if (exceptionType.Equals(e.GetType()))
                {
                    return;
                }
                else
                {
                    TestLog.WriteLine("Did not throw exception of type {0}", exceptionType);
                }
            }
            finally
            {
                w.Dispose();
            }
            throw new TestException(TestResult.Failed, "");
        }

        public byte[] StringToByteArray(string src)
        {
            byte[] base64 = new byte[src.Length * 2];

            for (int i = 0; i < src.Length; i++)
            {
                byte[] temp = System.BitConverter.GetBytes(src[i]);
                base64[2 * i] = temp[0];
                base64[2 * i + 1] = temp[1];
            }
            return base64;
        }

        public static bool VerifyNode(XmlReader r, XmlNodeType eExpNodeType, string strExpName, string strExpValue)
        {
            bool bPassed = true;

            if (r.NodeType != eExpNodeType)
            {
                TestLog.WriteLine("NodeType doesn't match");
                TestLog.WriteLine("    Expected NodeType: " + eExpNodeType);
                TestLog.WriteLine("    Actual NodeType: " + r.NodeType);
                bPassed = false;
            }
            if (r.Name != strExpName)
            {
                TestLog.WriteLine("Name doesn't match:");
                TestLog.WriteLine("    Expected Name: '" + strExpName + "'");
                TestLog.WriteLine("    Actual Name: '" + r.Name + "'");

                bPassed = false;
            }
            if (r.Value != strExpValue)
            {
                TestLog.WriteLine("Value doesn't match:");
                TestLog.WriteLine("    Expected Value: '" + strExpValue + "'");
                TestLog.WriteLine("    Actual Value: '" + r.Value + "'");

                bPassed = false;
            }
            return bPassed;
        }

        public void CompareNode(XmlReader r, XmlNodeType eExpNodeType, string strExpName, string strExpValue)
        {
            bool bNode = VerifyNode(r, eExpNodeType, strExpName, strExpValue);
            TestLog.Compare(bNode, "VerifyNode failed");
        }

        public void CheckXmlException(string expectedCode, XmlException e, int expectedLine, int expectedPosition)
        {
            TestLog.Compare(e.LineNumber, expectedLine, "CheckXmlException:LineNumber");
            TestLog.Compare(e.LinePosition, expectedPosition, "CheckXmlException:LinePosition");
        }

        public void PositionOnNodeType(XmlReader r, XmlNodeType nodeType)
        {
            if (nodeType == XmlNodeType.DocumentType)
            {
                TestLog.Skip("There is no DocumentType");
            }

            if (r.NodeType == nodeType)
                return;

            while (r.Read() && r.NodeType != nodeType)
            {
                if (nodeType == XmlNodeType.ProcessingInstruction && r.NodeType == XmlNodeType.XmlDeclaration)
                {
                    if (string.Compare(Name, 0, ST_XML, 0, 3) != 0)
                        return;
                }
                if (r.NodeType == XmlNodeType.Element && nodeType == XmlNodeType.Attribute)
                {
                    if (r.MoveToFirstAttribute())
                    {
                        return;
                    }
                }
            }
            if (r.EOF)
            {
                throw new TestException(TestResult.Failed, "Couldn't find XmlNodeType " + nodeType);
            }
        }

        public void PositionOnElement(XmlReader r, string strElementName)
        {
            if (r.NodeType == XmlNodeType.Element && r.Name == strElementName)
                return;

            while (r.Read())
            {
                if (r.NodeType == XmlNodeType.Element && r.Name == strElementName)
                    break;
            }
            if (r.EOF)
            {
                throw new TestException(TestResult.Failed, "Couldn't find element '" + strElementName + "'");
            }
        }

        public XmlReader CreateReader(int size)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<root>");
            for (int i = 0; i < size; i++)
            {
                sb.Append("A");
            }
            sb.Append("</root>");
            return GetReaderStr(sb.ToString());
        }



        public XmlReader CreateReaderIgnoreWS(string fileName)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreWhitespace = true;
            readerSettings.CloseInput = false;
            Stream stream = FilePathUtil.getStream(fileName);
            return GetReader(XmlReader.Create(stream, readerSettings));
        }

        public XmlReader CreateReader(string fileName)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.DtdProcessing = DtdProcessing.Ignore;
            readerSettings.CloseInput = false;

            Stream stream = FilePathUtil.getStream(fileName);
            return GetReader(XmlReader.Create(stream, readerSettings));
        }

        public XmlReader CreateReader(TextReader sr)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.CloseInput = true;
            return GetReader(XmlReader.Create(sr, readerSettings));
        }

        // return string of current mode
        public string InitStringValue(string str)
        {
            object obj = TestInput.Properties["CommandLine/" + str];
            if (obj == null)
            {
                return string.Empty;
            }
            return obj.ToString();
        }

        public void DiffTwoXmlStrings(string source, string target)
        {
            _diff.Option = XmlDiffOption.IgnoreAttributeOrder;
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ConformanceLevel = ConformanceLevel.Fragment;

            XmlReader src = XmlReader.Create(new StringReader(source), rs);
            XmlReader tgt = XmlReader.Create(new StringReader(target), rs);
            bool retVal = _diff.Compare(src, tgt);
            if (!retVal)
            {
                TestLog.WriteLine("XmlDif failed:");
                TestLog.WriteLine("DIFF: {0}", _diff.ToXml());
                throw new TestException(TestResult.Failed, "");
            }
        }

        public void BoolToLTMResult(bool bResult)
        {
            if (!bResult)
                throw new TestException(TestResult.Failed, "");
        }

        public static void DeleteTestFile(string strFileName)
        {
        }

        public static void CreateByteTestFile(string strFileName)
        {
            FilePathUtil.addStream(strFileName, new MemoryStream());
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));
            tw.WriteLine("x");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateUTF8EncodedTestFile(string strFileName, Encoding encode)
        {
            FilePathUtil.addStream(strFileName, new MemoryStream());
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName), encode);

            tw.WriteLine("<root>");
            tw.Write("�");
            tw.WriteLine("</root>");

            tw.Flush();
            tw.Dispose();
        }

        public static void CreateEncodedTestFile(string strFileName, Encoding encode)
        {
            FilePathUtil.addStream(strFileName, new MemoryStream());
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName), encode);

            tw.WriteLine("<root>");
            tw.WriteLine("</root>");

            tw.Flush();
            tw.Dispose();
        }

        public static void CreateWhitespaceHandlingTestFile(string strFileName)
        {
            FilePathUtil.addStream(strFileName, new MemoryStream());
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<!DOCTYPE dt [");
            tw.WriteLine("<!ELEMENT WHITESPACE1 (#PCDATA)*>");
            tw.WriteLine("<!ELEMENT WHITESPACE2 (#PCDATA)*>");
            tw.WriteLine("<!ELEMENT WHITESPACE3 (#PCDATA)*>");
            tw.WriteLine("]>");
            tw.WriteLine("<doc>");
            tw.WriteLine("<WHITESPACE1>\r\n<ELEM />\r\n</WHITESPACE1>");
            tw.WriteLine("<WHITESPACE2> <ELEM /> </WHITESPACE2>");
            tw.WriteLine("<WHITESPACE3>\t<ELEM />\t</WHITESPACE3>");
            tw.WriteLine("</doc>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateGenericXsltTestFile(string strFileName)
        {
            CreateXSLTStyleSheetWCopyTestFile(pXsltCopyStylesheet);
            CreateGenericTestFile(strFileName);
        }

        public static void CreateGenericTestFile(string strFileName)
        {
            FilePathUtil.addStream(strFileName, new MemoryStream());
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
            tw.WriteLine("<!-- comment1 -->");
            tw.WriteLine("<?PI1_First processing instruction?>");
            tw.WriteLine("<?PI1a?>");
            tw.WriteLine("<?PI1b?>");
            tw.WriteLine("<?PI1c?>");
            tw.WriteLine("<!DOCTYPE root SYSTEM \"AllNodeTypes.dtd\" [");
            tw.WriteLine("<!NOTATION gif SYSTEM \"foo.exe\">");
            tw.WriteLine("<!ELEMENT root ANY>");
            tw.WriteLine("<!ELEMENT elem1 ANY>");
            tw.WriteLine("<!ELEMENT ISDEFAULT ANY>");
            tw.WriteLine("<!ENTITY % e SYSTEM \"AllNodeTypes.ent\">");
            tw.WriteLine("%e;");
            tw.WriteLine("<!ENTITY e1 \"e1foo\">");
            tw.WriteLine("<!ENTITY e2 \"&ext3; e2bar\">");
            tw.WriteLine("<!ENTITY e3 \"&e1; e3bzee \">");
            tw.WriteLine("<!ENTITY e4 \"&e3; e4gee\">");
            tw.WriteLine("<!ATTLIST elem1 child1 CDATA #IMPLIED child2 CDATA \"&e2;\" child3 CDATA #REQUIRED>");
            tw.WriteLine("<!ATTLIST root xmlns:something CDATA #FIXED \"something\" xmlns:my CDATA #FIXED \"my\" xmlns:dt CDATA #FIXED \"urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/\">");
            tw.WriteLine("<!ATTLIST ISDEFAULT d1 CDATA #FIXED \"d1value\">");

            tw.WriteLine("<!ATTLIST MULTISPACES att IDREFS #IMPLIED>");
            tw.WriteLine("<!ELEMENT CATMIXED (#PCDATA)>");

            tw.WriteLine("]>");
            tw.WriteLine("<PLAY>");
            tw.WriteLine("<root xmlns:something=\"something\" xmlns:my=\"my\" xmlns:dt=\"urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/\">");
            tw.WriteLine("<elem1 child1=\"\" child2=\"&e2;\" child3=\"something\">");
            tw.WriteLine("text node two &e1; text node three");
            tw.WriteLine("</elem1>");
            tw.WriteLine("&e2;");
            tw.WriteLine("<![CDATA[ This section contains characters that should not be interpreted as markup. For example, characters ', \",");
            tw.WriteLine("<, >, and & are all fine here.]]>");
            tw.WriteLine("<elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"> ");
            tw.WriteLine("<a />");
            tw.WriteLine("</elem2>");
            tw.WriteLine("<elem2> ");
            tw.WriteLine("elem2-text1");
            tw.WriteLine("<a refs=\"id2\"> ");
            tw.WriteLine("this-is-a    ");
            tw.WriteLine("</a> ");
            tw.WriteLine("elem2-text2");
            tw.WriteLine("&e3;");
            tw.WriteLine("&e4;");
            tw.WriteLine("<!-- elem2-comment1-->");
            tw.WriteLine("elem2-text3");
            tw.WriteLine("<b> ");
            tw.WriteLine("this-is-b");
            tw.WriteLine("</b>");
            tw.WriteLine("elem2-text4");
            tw.WriteLine("<?elem2_PI elem2-PI?>");
            tw.WriteLine("elem2-text5");
            tw.WriteLine("</elem2>");
            tw.WriteLine("<elem2 att1=\"id2\"></elem2>");
            tw.WriteLine("</root>");
            tw.Write("<ENTITY1 att1='xxx&lt;xxx&#65;xxx&#x43;xxx&e1;xxx'>xxx&gt;xxx&#66;xxx&#x44;xxx&e1;xxx</ENTITY1>");
            tw.WriteLine("<ENTITY2 att1='xxx&lt;xxx&#65;xxx&#x43;xxx&e1;xxx'>xxx&gt;xxx&#66;xxx&#x44;xxx&e1;xxx</ENTITY2>");
            tw.WriteLine("<ENTITY3 att1='xxx&lt;xxx&#65;xxx&#x43;xxx&e1;xxx'>xxx&gt;xxx&#66;xxx&#x44;xxx&e1;xxx</ENTITY3>");
            tw.WriteLine("<ENTITY4 att1='xxx&lt;xxx&#65;xxx&#x43;xxx&e1;xxx'>xxx&gt;xxx&#66;xxx&#x44;xxx&e1;xxx</ENTITY4>");
            tw.WriteLine("<ENTITY5>&ext3;</ENTITY5>");
            tw.WriteLine("<ATTRIBUTE1 />");
            tw.WriteLine("<ATTRIBUTE2 a1='a1value' />");
            tw.WriteLine("<ATTRIBUTE3 a1='a1value' a2='a2value' a3='a3value' />");
            tw.WriteLine("<ATTRIBUTE4 a1='' />");
            tw.WriteLine("<ATTRIBUTE5 CRLF='x\r\nx' CR='x\rx' LF='x\nx' MS='x     x' TAB='x\tx' />");
            tw.WriteLine("<?PI1a a\r\n\rb ?>");
            tw.WriteLine("<!--comm\r\n\rent-->");
            tw.WriteLine("<![CDATA[cd\r\n\rata]]>");
            tw.WriteLine("<ENDOFLINE1>x\r\nx</ENDOFLINE1>");
            tw.WriteLine("<ENDOFLINE2>x\rx</ENDOFLINE2>");
            tw.WriteLine("<ENDOFLINE3>x\nx</ENDOFLINE3>");
            tw.WriteLine("<WHITESPACE1>\r\n<ELEM />\r\n</WHITESPACE1>");
            tw.WriteLine("<WHITESPACE2> <ELEM /> </WHITESPACE2>");
            tw.WriteLine("<WHITESPACE3>\t<ELEM />\t</WHITESPACE3>");
            tw.WriteLine("<SKIP1 /><AFTERSKIP1 />");
            tw.WriteLine("<SKIP2></SKIP2><AFTERSKIP2 />");
            tw.WriteLine("<SKIP3><ELEM1 /><ELEM2>xxx yyy</ELEM2><ELEM3 /></SKIP3><AFTERSKIP3></AFTERSKIP3>");
            tw.WriteLine("<SKIP4><ELEM1 /><ELEM2>xxx<ELEM3 /></ELEM2></SKIP4>");
            tw.WriteLine("<CHARS1>0123456789</CHARS1>");
            tw.WriteLine("<CHARS2>xxx<MARKUP />yyy</CHARS2>");
            tw.WriteLine("<CHARS_ELEM1>xxx<MARKUP />yyy</CHARS_ELEM1>");
            tw.WriteLine("<CHARS_ELEM2><MARKUP />yyy</CHARS_ELEM2>");
            tw.WriteLine("<CHARS_ELEM3>xxx<MARKUP /></CHARS_ELEM3>");
            tw.WriteLine("<CHARS_CDATA1>xxx<![CDATA[yyy]]>zzz</CHARS_CDATA1>");
            tw.WriteLine("<CHARS_CDATA2><![CDATA[yyy]]>zzz</CHARS_CDATA2>");
            tw.WriteLine("<CHARS_CDATA3>xxx<![CDATA[yyy]]></CHARS_CDATA3>");
            tw.WriteLine("<CHARS_PI1>xxx<?PI_CHAR1 yyy?>zzz</CHARS_PI1>");
            tw.WriteLine("<CHARS_PI2><?PI_CHAR2?>zzz</CHARS_PI2>");
            tw.WriteLine("<CHARS_PI3>xxx<?PI_CHAR3 yyy?></CHARS_PI3>");
            tw.WriteLine("<CHARS_COMMENT1>xxx<!-- comment1-->zzz</CHARS_COMMENT1>");
            tw.WriteLine("<CHARS_COMMENT2><!-- comment1-->zzz</CHARS_COMMENT2>");
            tw.WriteLine("<CHARS_COMMENT3>xxx<!-- comment1--></CHARS_COMMENT3>");
            tw.Flush();
            tw.WriteLine("<ISDEFAULT />");
            tw.WriteLine("<ISDEFAULT a1='a1value' />");
            tw.WriteLine("<BOOLEAN1>true</BOOLEAN1>");
            tw.WriteLine("<BOOLEAN2>false</BOOLEAN2>");
            tw.WriteLine("<BOOLEAN3>1</BOOLEAN3>");
            tw.WriteLine("<BOOLEAN4>tRue</BOOLEAN4>");
            tw.WriteLine("<DATETIME>1999-02-22T11:11:11</DATETIME>");
            tw.WriteLine("<DATE>1999-02-22</DATE>");
            tw.WriteLine("<TIME>11:11:11</TIME>");
            tw.WriteLine("<INTEGER>9999</INTEGER>");
            tw.WriteLine("<FLOAT>99.99</FLOAT>");
            tw.WriteLine("<DECIMAL>.09</DECIMAL>");
            tw.WriteLine("<CONTENT><e1 a1='a1value' a2='a2value'><e2 a1='a1value' a2='a2value'><e3 a1='a1value' a2='a2value'>leave</e3></e2></e1></CONTENT>");
            tw.WriteLine("<TITLE><!-- this is a comment--></TITLE>");
            tw.WriteLine("<PGROUP>");
            tw.WriteLine("<ACT0 xmlns:foo=\"http://www.foo.com\" foo:Attr0=\"0\" foo:Attr1=\"1111111101\" foo:Attr2=\"222222202\" foo:Attr3=\"333333303\" foo:Attr4=\"444444404\" foo:Attr5=\"555555505\" foo:Attr6=\"666666606\" foo:Attr7=\"777777707\" foo:Attr8=\"888888808\" foo:Attr9=\"999999909\" />");
            tw.WriteLine("<ACT1 Attr0=\'0\' Attr1=\'1111111101\' Attr2=\'222222202\' Attr3=\'333333303\' Attr4=\'444444404\' Attr5=\'555555505\' Attr6=\'666666606\' Attr7=\'777777707\' Attr8=\'888888808\' Attr9=\'999999909\' />");
            tw.WriteLine("<QUOTE1 Attr0=\"0\" Attr1=\'1111111101\' Attr2=\"222222202\" Attr3=\'333333303\' />");
            tw.WriteLine("<PERSONA>DROMIO OF EPHESUS</PERSONA>");
            tw.WriteLine("<QUOTE2 Attr0=\"0\" Attr1=\"1111111101\" Attr2=\'222222202\' Attr3=\'333333303\' />");
            tw.WriteLine("<QUOTE3 Attr0=\'0\' Attr1=\"1111111101\" Attr2=\'222222202\' Attr3=\"333333303\" />");
            tw.WriteLine("<EMPTY1 />");
            tw.WriteLine("<EMPTY2 val=\"abc\" />");
            tw.WriteLine("<EMPTY3></EMPTY3>");
            tw.WriteLine("<NONEMPTY0></NONEMPTY0>");
            tw.WriteLine("<NONEMPTY1>ABCDE</NONEMPTY1>");
            tw.WriteLine("<NONEMPTY2 val=\"abc\">1234</NONEMPTY2>");
            tw.WriteLine("<ACT2 Attr0=\"10\" Attr1=\"1111111011\" Attr2=\"222222012\" Attr3=\"333333013\" Attr4=\"444444014\" Attr5=\"555555015\" Attr6=\"666666016\" Attr7=\"777777017\" Attr8=\"888888018\" Attr9=\"999999019\" />");
            tw.WriteLine("<GRPDESCR>twin brothers, and sons to Aegeon and Aemilia.</GRPDESCR>");
            tw.WriteLine("</PGROUP>");
            tw.WriteLine("<PGROUP>");
            tw.Flush();
            tw.WriteLine("<XMLLANG0 xml:lang=\"en-US\">What color &e1; is it?</XMLLANG0>");
            tw.Write("<XMLLANG1 xml:lang=\"en-GB\">What color is it?<a><b><c>Language Test</c><PERSONA>DROMIO OF EPHESUS</PERSONA></b></a></XMLLANG1>");
            tw.WriteLine("<NOXMLLANG />");
            tw.WriteLine("<EMPTY_XMLLANG Attr0=\"0\" xml:lang=\"en-US\" />");
            tw.WriteLine("<XMLLANG2 xml:lang=\"en-US\">What color is it?<TITLE><!-- this is a comment--></TITLE><XMLLANG1 xml:lang=\"en-GB\">Testing language<XMLLANG0 xml:lang=\"en-US\">What color is it?</XMLLANG0>haha </XMLLANG1>hihihi</XMLLANG2>");
            tw.WriteLine("<DONEXMLLANG />");
            tw.WriteLine("<XMLSPACE1 xml:space=\'default\'>&lt; &gt;</XMLSPACE1>");
            tw.Write("<XMLSPACE2 xml:space=\'preserve\'>&lt; &gt;<a><!-- comment--><b><?PI1a?><c>Space Test</c><PERSONA>DROMIO OF SYRACUSE</PERSONA></b></a></XMLSPACE2>");
            tw.WriteLine("<NOSPACE />");
            tw.WriteLine("<EMPTY_XMLSPACE Attr0=\"0\" xml:space=\'default\' />");
            tw.WriteLine("<XMLSPACE2A xml:space=\'default\'>&lt; <XMLSPACE3 xml:space=\'preserve\'>  &lt; &gt; <XMLSPACE4 xml:space=\'default\'>  &lt; &gt;  </XMLSPACE4> test </XMLSPACE3> &gt;</XMLSPACE2A>");
            tw.WriteLine("<GRPDESCR>twin brothers, and attendants on the two Antipholuses.</GRPDESCR>");
            tw.WriteLine("<DOCNAMESPACE>");
            tw.WriteLine("<NAMESPACE0 xmlns:bar=\"1\"><bar:check>Namespace=1</bar:check></NAMESPACE0>");
            tw.WriteLine("<NAMESPACE1 xmlns:bar=\"1\"><a><b><c><d><bar:check>Namespace=1</bar:check><bar:check2></bar:check2></d></c></b></a></NAMESPACE1>");
            tw.WriteLine("<NONAMESPACE>Namespace=\"\"</NONAMESPACE>");
            tw.WriteLine("<EMPTY_NAMESPACE bar:Attr0=\"0\" xmlns:bar=\"1\" />");
            tw.WriteLine("<EMPTY_NAMESPACE1 Attr0=\"0\" xmlns=\"14\" />");
            tw.WriteLine("<EMPTY_NAMESPACE2 Attr0=\"0\" xmlns=\"14\"></EMPTY_NAMESPACE2>");
            tw.WriteLine("<NAMESPACE2 xmlns:bar=\"1\"><a><b><c xmlns:bar=\"2\"><d><bar:check>Namespace=2</bar:check></d></c></b></a></NAMESPACE2>");
            tw.WriteLine("<NAMESPACE3 xmlns=\"1\"><a xmlns:a=\"2\" xmlns:b=\"3\" xmlns:c=\"4\"><b xmlns:d=\"5\" xmlns:e=\"6\" xmlns:f='7'><c xmlns:d=\"8\" xmlns:e=\"9\" xmlns:f=\"10\">");
            tw.WriteLine("<d xmlns:g=\"11\" xmlns:h=\"12\"><check>Namespace=1</check><testns xmlns=\"100\"><empty100 /><check100>Namespace=100</check100></testns><check1>Namespace=1</check1><d:check8>Namespace=8</d:check8></d></c><d:check5>Namespace=5</d:check5></b></a>");
            tw.WriteLine("<a13 a:check=\"Namespace=13\" xmlns:a=\"13\" /><check14 xmlns=\"14\">Namespace=14</check14></NAMESPACE3>");
            tw.WriteLine("<NONAMESPACE>Namespace=\"\"</NONAMESPACE>");
            tw.WriteLine("<NONAMESPACE1 Attr1=\"one\" xmlns=\"1000\">Namespace=\"\"</NONAMESPACE1>");
            tw.WriteLine("</DOCNAMESPACE>");
            tw.WriteLine("</PGROUP>");
            tw.WriteLine("<GOTOCONTENT>some text<![CDATA[cdata info]]></GOTOCONTENT>");
            tw.WriteLine("<SKIPCONTENT att1=\"\">  <!-- comment1--> \n <?PI_SkipContent instruction?></SKIPCONTENT>");
            tw.WriteLine("<MIXCONTENT>  <!-- comment1-->some text<?PI_SkipContent instruction?><![CDATA[cdata info]]></MIXCONTENT>");
            tw.WriteLine("<A att=\"123\">1<B>2<C>3<D>4<E>5<F>6<G>7<H>8<I>9<J>10");
            tw.WriteLine("<A1 att=\"456\">11<B1>12<C1>13<D1>14<E1>15<F1>16<G1>17<H1>18<I1>19<J1>20");
            tw.WriteLine("<A2 att=\"789\">21<B2>22<C2>23<D2>24<E2>25<F2>26<G2>27<H2>28<I2>29<J2>30");
            tw.WriteLine("<A3 att=\"123\">31<B3>32<C3>33<D3>34<E3>35<F3>36<G3>37<H3>38<I3>39<J3>40");
            tw.WriteLine("<A4 att=\"456\">41<B4>42<C4>43<D4>44<E4>45<F4>46<G4>47<H4>48<I4>49<J4>50");
            tw.WriteLine("<A5 att=\"789\">51<B5>52<C5>53<D5>54<E5>55<F5>56<G5>57<H5>58<I5>59<J5>60");
            tw.WriteLine("<A6 att=\"123\">61<B6>62<C6>63<D6>64<E6>65<F6>66<G6>67<H6>68<I6>69<J6>70");
            tw.WriteLine("<A7 att=\"456\">71<B7>72<C7>73<D7>74<E7>75<F7>76<G7>77<H7>78<I7>79<J7>80");
            tw.WriteLine("<A8 att=\"789\">81<B8>82<C8>83<D8>84<E8>85<F8>86<G8>87<H8>88<I8>89<J8>90");
            tw.WriteLine("<A9 att=\"123\">91<B9>92<C9>93<D9>94<E9>95<F9>96<G9>97<H9>98<I9>99<J9>100");
            tw.WriteLine("<A10 att=\"123\">101<B10>102<C10>103<D10>104<E10>105<F10>106<G10>107<H10>108<I10>109<J10>110");
            tw.WriteLine("</J10>109</I10>108</H10>107</G10>106</F10>105</E10>104</D10>103</C10>102</B10>101</A10>");
            tw.WriteLine("</J9>99</I9>98</H9>97</G9>96</F9>95</E9>94</D9>93</C9>92</B9>91</A9>");
            tw.WriteLine("</J8>89</I8>88</H8>87</G8>86</F8>85</E8>84</D8>83</C8>82</B8>81</A8>");
            tw.WriteLine("</J7>79</I7>78</H7>77</G7>76</F7>75</E7>74</D7>73</C7>72</B7>71</A7>");
            tw.WriteLine("</J6>69</I6>68</H6>67</G6>66</F6>65</E6>64</D6>63</C6>62</B6>61</A6>");
            tw.WriteLine("</J5>59</I5>58</H5>57</G5>56</F5>55</E5>54</D5>53</C5>52</B5>51</A5>");
            tw.WriteLine("</J4>49</I4>48</H4>47</G4>46</F4>45</E4>44</D4>43</C4>42</B4>41</A4>");
            tw.WriteLine("</J3>39</I3>38</H3>37</G3>36</F3>35</E3>34</D3>33</C3>32</B3>31</A3>");
            tw.WriteLine("</J2>29</I2>28</H2>27</G2>26</F2>25</E2>24</D2>23</C2>22</B2>21</A2>");
            tw.WriteLine("</J1>19</I1>18</H1>17</G1>16</F1>15</E1>14</D1>13</C1>12</B1>11</A1>");
            tw.Write("</J>9</I>8</H>7</G>6</F>5</E>4</D>3</C>2</B>1</A>");
            tw.WriteLine("<EMPTY4 val=\"abc\"></EMPTY4>");
            tw.WriteLine("<COMPLEX>Text<!-- comment --><![CDATA[cdata]]></COMPLEX>");
            tw.WriteLine("<DUMMY />");
            tw.WriteLine("<MULTISPACES att=' \r\n \t \r\r\n  n1  \r\n \t \r\r\n  n2  \r\n \t \r\r\n ' />");
            tw.WriteLine("<CAT>AB<![CDATA[CD]]> </CAT>");
            tw.WriteLine("<CATMIXED>AB<![CDATA[CD]]> </CATMIXED>");

            tw.WriteLine("<VALIDXMLLANG0 xml:lang=\"a\" />");
            tw.WriteLine("<VALIDXMLLANG1 xml:lang=\"\" />");
            tw.WriteLine("<VALIDXMLLANG2 xml:lang=\"ab-cd-\" />");
            tw.WriteLine("<VALIDXMLLANG3 xml:lang=\"a b-cd\" />");

            tw.Write("</PLAY>");
            tw.Flush();

            //Create external DTD file
            FilePathUtil.addStream("AllNodeTypes.dtd", new MemoryStream());
            TextWriter twDTD = new StreamWriter(FilePathUtil.getStream("AllNodeTypes.dtd"));
            twDTD.WriteLine("<!ELEMENT elem2 (#PCDATA| a | b )* >");
            twDTD.WriteLine("<!ELEMENT a ANY>");
            twDTD.WriteLine("<!ELEMENT b ANY>");
            twDTD.WriteLine("<!ELEMENT c ANY>");
            twDTD.WriteLine("<!ATTLIST elem2 ");
            twDTD.WriteLine("att1 ID #IMPLIED");
            twDTD.WriteLine("att2 CDATA #IMPLIED");
            twDTD.WriteLine("att3 CDATA #IMPLIED>");
            twDTD.WriteLine("<!ATTLIST a refs IDREFS #IMPLIED>");
            twDTD.Flush();

            // Create Ent file
            FilePathUtil.addStream("AllNodeTypes.ent", new MemoryStream());
            TextWriter twENT = new StreamWriter(FilePathUtil.getStream("AllNodeTypes.ent"));
            twENT.WriteLine("<!ELEMENT foo ANY>");
            twENT.WriteLine("<!ENTITY % ext4 \"blah\">");
            twENT.WriteLine("<!ENTITY ext3 \"%ext4;\">");
            twENT.Flush();
        }

        public static void CreateInvalidDTDTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<?xml version=\"1.0\"?><!DOCTYPE Root [<!ELEMENT Root ANY><!ELEMENT E ANY><!ATTLIST E	A1 NOTATION (N) #IMPLIED>]>");
            tw.WriteLine("<Root><E A1=\"N\" /></Root>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateValidDTDTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<?xml version=\"1.0\"?><!DOCTYPE Root [<!ELEMENT Root ANY><!ELEMENT E ANY><!ATTLIST E	IMAGE_FORMAT (bmp|jpg|gif) #IMPLIED>]>");
            tw.Write("<Root><E A1=\"gif\" /></Root>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateWellFormedDTDTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.Write("<!DOCTYPE foo [<!ELEMENT foo (e1, e2, e3)><!ENTITY bar \"<e1> <e4 /> </e1> <e2> that </e2>\">");
            tw.Write("<!ELEMENT e1 (e4)><!ELEMENT e2 ANY><!ELEMENT e3 ANY><!ELEMENT e4 ANY>]>");
            tw.Write("<foo>&bar;<e3 /></foo>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateNonWellFormedDTDTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.Write("<!DOCTYPE foo [<!ELEMENT foo (e1, e2, e3)><!ENTITY bar \"<e1> <e4 /> </e1> <e2> that </e2></e2>\">");
            tw.Write("<!ELEMENT e1 (e4)><!ELEMENT e2 ANY><!ELEMENT e3 ANY><!ELEMENT e4 ANY>]>");
            tw.Write("<foo>&bar;<e3 /></foo>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateInvWellFormedDTDTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.Write("<!DOCTYPE foo [<!ELEMENT foo (e1, e2, e3)><!ENTITY bar \"<e1> this </e1> <e2> that </e2>\">");
            tw.Write("<!ELEMENT e1 (e4)><!ELEMENT e2 ANY><!ELEMENT e3 ANY><!ELEMENT e4 ANY>]>");
            tw.Write("<foo>&bar;<e3 /></foo>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateInvalidXMLXDRTestFile(string strFileName)
        {
            // Create XDR before
            CreateXDRTestFile(pValidXDR);

            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<?xml version=\"1.0\" ?><e:Root xmlns:e=\"x-schema:xdrfile.xml\">");
            tw.WriteLine("<e:e1>Element 1</e:e1></e:Root>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateXDRXMLTestFile(string strFileName)
        {
            // Create XDR before
            CreateXDRTestFile(pValidXDR);

            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<bar xmlns=\"x-schema:XdrFile.xml\"> <tt /> <tt /></bar>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateXDRTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<Schema xmlns=\"uuid:BDC6E3F0-6DA3-11d1-A2A3-00AA00C14882\"><ElementType content=\"empty\" name=\"tt\"></ElementType>");
            tw.WriteLine("<ElementType content=\"eltOnly\" order=\"seq\" name=\"bar\" model=\"closed\"><element type=\"tt\" /><element type=\"tt\" /></ElementType>");
            tw.WriteLine("</Schema>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateInvalidNamespaceTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<NAMESPACE0 xmlns:bar=\"1\"><bar1:check>Namespace=1</bar1:check></NAMESPACE0>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateNamespaceTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<DOCNAMESPACE>");
            tw.WriteLine("<NAMESPACE0 xmlns:bar=\"1\"><bar:check>Namespace=1</bar:check></NAMESPACE0>");
            tw.WriteLine("<NAMESPACE1 xmlns:bar=\"1\"><a><b><c><d><bar:check>Namespace=1</bar:check></d></c></b></a></NAMESPACE1>");
            tw.WriteLine("<NONAMESPACE>Namespace=\"\"</NONAMESPACE>");
            tw.WriteLine("<EMPTY_NAMESPACE bar:Attr0=\"0\" xmlns:bar=\"1\" />");
            tw.WriteLine("<EMPTY_NAMESPACE1 Attr0=\"0\" xmlns=\"14\" />");
            tw.WriteLine("<NAMESPACE2 xmlns:bar=\"1\"><a><b><c xmlns:bar=\"2\"><d><bar:check>Namespace=2</bar:check></d></c></b></a></NAMESPACE2>");
            tw.WriteLine("<NAMESPACE3 xmlns=\"1\"><a xmlns:a=\"2\" xmlns:b=\"3\" xmlns:c=\"4\"><b xmlns:d=\"5\" xmlns:e=\"6\" xmlns:f='7'><c xmlns:d=\"8\" xmlns:e=\"9\" xmlns:f=\"10\">");
            tw.WriteLine("<d xmlns:g=\"11\" xmlns:h=\"12\"><check>Namespace=1</check><testns xmlns=\"100\"><check100>Namespace=100</check100></testns><check1>Namespace=1</check1><d:check8>Namespace=8</d:check8></d></c><d:check5>Namespace=5</d:check5></b></a>");
            tw.WriteLine("<a13 a:check=\"Namespace=13\" xmlns:a=\"13\" /><check14 xmlns=\"14\">Namespace=14</check14></NAMESPACE3>");
            tw.WriteLine("<NONAMESPACE>Namespace=\"\"</NONAMESPACE>");
            tw.WriteLine("</DOCNAMESPACE>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateXmlLangTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<PGROUP>");
            tw.WriteLine("<PERSONA>DROMIO OF EPHESUS</PERSONA>");
            tw.WriteLine("<PERSONA>DROMIO OF SYRACUSE</PERSONA>");
            tw.WriteLine("<XMLLANG0 xml:lang=\"en-US\">What color is it?</XMLLANG0>");
            tw.Write("<XMLLANG1 xml:lang=\"en-GB\">What color is it?<a><b><c>Language Test</c><PERSONA>DROMIO OF EPHESUS</PERSONA></b></a></XMLLANG1>");
            tw.WriteLine("<NOXMLLANG />");
            tw.WriteLine("<EMPTY_XMLLANG Attr0=\"0\" xml:lang=\"en-US\" />");
            tw.WriteLine("<XMLLANG2 xml:lang=\"en-US\">What color is it?<TITLE><!-- this is a comment--></TITLE><XMLLANG1 xml:lang=\"en-GB\">Testing language<XMLLANG0 xml:lang=\"en-US\">What color is it?</XMLLANG0>haha </XMLLANG1>hihihi</XMLLANG2>");
            tw.WriteLine("<DONEXMLLANG />");
            tw.WriteLine("</PGROUP>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateXmlSpaceTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<PGROUP>");
            tw.WriteLine("<PERSONA>DROMIO OF EPHESUS</PERSONA>");
            tw.WriteLine("<PERSONA>DROMIO OF SYRACUSE</PERSONA>");
            tw.WriteLine("<XMLSPACE1 xml:space=\'default\'>&lt; &gt;</XMLSPACE1>");
            tw.Write("<XMLSPACE2 xml:space=\'preserve\'>&lt; &gt;<a><b><c>Space Test</c><PERSONA>DROMIO OF SYRACUSE</PERSONA></b></a></XMLSPACE2>");
            tw.WriteLine("<NOSPACE />");
            tw.WriteLine("<EMPTY_XMLSPACE Attr0=\"0\" xml:space=\'default\' />");
            tw.WriteLine("<XMLSPACE2A xml:space=\'default\'>&lt; <XMLSPACE3 xml:space=\'preserve\'>  &lt; &gt; <XMLSPACE4 xml:space=\'default\'>  &lt; &gt;  </XMLSPACE4> test </XMLSPACE3> &gt;</XMLSPACE2A>");
            tw.WriteLine("<GRPDESCR>twin brothers, and attendants on the two Antipholuses.</GRPDESCR>");
            tw.WriteLine("</PGROUP>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateJunkTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            string str = new String('Z', (1 << 20) - 1);
            tw.Write(str);
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateBase64TestFile(string strFileName)
        {
            byte[] Wbase64 = new byte[2048];
            int Wbase64len = 0;
            byte[] WNumOnly = new byte[1024];
            int WNumOnlylen = 0;
            byte[] WTextOnly = new byte[1024];
            int WTextOnlylen = 0;
            int i = 0;

            for (i = 0; i < strBase64.Length; i++)
            {
                WriteToBuffer(ref Wbase64, ref Wbase64len, System.BitConverter.GetBytes(strBase64[i]));
            }

            for (i = 52; i < strBase64.Length; i++)
            {
                WriteToBuffer(ref WNumOnly, ref WNumOnlylen, System.BitConverter.GetBytes(strBase64[i]));
            }

            for (i = 0; i < strBase64.Length - 12; i++)
            {
                WriteToBuffer(ref WTextOnly, ref WTextOnlylen, System.BitConverter.GetBytes(strBase64[i]));
            }

            FilePathUtil.addStream(strFileName, new MemoryStream());

            XmlWriter w = XmlWriter.Create(FilePathUtil.getStream(strFileName));
            w.WriteStartDocument();
            w.WriteDocType("Root", null, null, "<!ENTITY e 'abc'>");
            w.WriteStartElement("Root");
            w.WriteStartElement("ElemAll");
            w.WriteBase64(Wbase64, 0, (int)Wbase64len);
            w.WriteEndElement();

            w.WriteStartElement("ElemEmpty");
            w.WriteString(String.Empty);
            w.WriteEndElement();

            w.WriteStartElement("ElemNum");
            w.WriteBase64(WNumOnly, 0, (int)WNumOnlylen);
            w.WriteEndElement();

            w.WriteStartElement("ElemText");
            w.WriteBase64(WTextOnly, 0, (int)WTextOnlylen);
            w.WriteEndElement();

            w.WriteStartElement("ElemNumText");
            w.WriteBase64(WTextOnly, 0, (int)WTextOnlylen);
            w.WriteBase64(WNumOnly, 0, (int)WNumOnlylen);
            w.WriteEndElement();

            w.WriteStartElement("ElemLong");
            for (i = 0; i < 10; i++)
                w.WriteBase64(Wbase64, 0, (int)Wbase64len);
            w.WriteEndElement();

            w.WriteElementString("ElemErr", "a&AQID");

            w.WriteStartElement("ElemMixed");
            w.WriteRaw("D2BAa<MIX>abc</MIX>AQID");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Flush();
        }

        public static void CreateBinHexTestFile(string strFileName)
        {
            byte[] Wbinhex = new byte[2000];
            int Wbinhexlen = 0;
            byte[] WNumOnly = new byte[2000];
            int WNumOnlylen = 0;
            byte[] WTextOnly = new byte[2000];
            int WTextOnlylen = 0;
            int i = 0;

            for (i = 0; i < strBinHex.Length; i++)
            {
                WriteToBuffer(ref Wbinhex, ref Wbinhexlen, System.BitConverter.GetBytes(strBinHex[i]));
            }

            for (i = 0; i < 10; i++)
            {
                WriteToBuffer(ref WNumOnly, ref WNumOnlylen, System.BitConverter.GetBytes(strBinHex[i]));
            }

            for (i = 10; i < strBinHex.Length; i++)
            {
                WriteToBuffer(ref WTextOnly, ref WTextOnlylen, System.BitConverter.GetBytes(strBinHex[i]));
            }
            FilePathUtil.addStream(strFileName, new MemoryStream());

            XmlWriter w = XmlWriter.Create(FilePathUtil.getStream(strFileName));
            w.WriteStartElement("Root");
            w.WriteStartElement("ElemAll");
            w.WriteBinHex(Wbinhex, 0, (int)Wbinhexlen);
            w.WriteEndElement();
            w.Flush();

            w.WriteStartElement("ElemEmpty");
            w.WriteString(String.Empty);
            w.WriteEndElement();

            w.WriteStartElement("ElemNum");
            w.WriteBinHex(WNumOnly, 0, (int)WNumOnlylen);
            w.WriteEndElement();

            w.WriteStartElement("ElemText");
            w.WriteBinHex(WTextOnly, 0, (int)WTextOnlylen);
            w.WriteEndElement();

            w.WriteStartElement("ElemNumText");
            w.WriteBinHex(WNumOnly, 0, (int)WNumOnlylen);
            w.WriteBinHex(WTextOnly, 0, (int)WTextOnlylen);
            w.WriteEndElement();

            w.WriteStartElement("ElemLong");
            for (i = 0; i < 10; i++)
                w.WriteBinHex(Wbinhex, 0, (int)Wbinhexlen);
            w.WriteEndElement();

            w.WriteElementString("ElemErr", "a&A2A3");

            w.WriteEndElement();
            w.Flush();
            w.Dispose();
        }

        public static void CreateBigElementTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            string str = new String('Z', (1 << 20) - 1);
            tw.WriteLine("<Root>");
            tw.Write("<");
            tw.Write(str);
            tw.WriteLine("X />");
            tw.Flush();

            tw.Write("<");
            tw.Write(str);
            tw.WriteLine("Y />");
            tw.WriteLine("</Root>");

            tw.Flush();
            tw.Dispose();
        }
        public static void CreateXSLTStyleSheetWCopyTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));
            tw.WriteLine("<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\">");
            tw.WriteLine("<xsl:template match=\"/\">");
            tw.WriteLine("<xsl:copy-of select=\"/\" />");
            tw.WriteLine("</xsl:template>");
            tw.WriteLine("</xsl:stylesheet>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateConstructorTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<?xml version=\"1.0\"?>");
            tw.WriteLine("<ROOT>");
            tw.WriteLine("<ATTRIBUTE3 a1='a1value' a2='a2value' a3='a3value' />");
            tw.Write("</ROOT>");
            tw.Flush();
            tw.Dispose();
        }

        public static void CreateLineNumberTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<?xml version=\"1.0\" ?>");
            tw.WriteLine(" <!DOCTYPE DT [");
            tw.WriteLine("<!ELEMENT root ANY>");
            tw.WriteLine("<!ENTITY % ext SYSTEM \"LineNumber.ent\">%ext;");
            tw.WriteLine("<!ENTITY e1 'e1foo'>]>");
            tw.WriteLine("<ROOT>");
            tw.WriteLine(" <ELEMENT a0='a0&e1;v' a1='a1value' a2='a2&e1;v'><EMBEDDED /></ELEMENT>");
            tw.WriteLine("<![CDATA[ This section contains CDATA]]>");
            tw.WriteLine("<CHARENTITY>AB&#x43;CD</CHARENTITY>");
            tw.WriteLine("<COMMENT><!-- comment node--></COMMENT>");
            tw.WriteLine("<ENTITYREF>A&e1;B&ext3;C</ENTITYREF>");
            tw.WriteLine("<?PI1?>");
            tw.WriteLine("<SKIP />");
            tw.WriteLine("<BASE64>9F6hJU++</BASE64>");
            tw.WriteLine("<BINHEX>9F6C</BINHEX>");
            tw.WriteLine("<BOOLXSD>true</BOOLXSD>");
            tw.WriteLine("<BOOLXDR>true</BOOLXDR>");
            tw.WriteLine("<DATE>2005-02-14</DATE>");
            tw.WriteLine("<DATETIME>2005-02-14T14:25:44</DATETIME>");
            tw.WriteLine("<DECIMAL>-14.25</DECIMAL>");
            tw.WriteLine("<INT>-1425</INT>");
            tw.WriteLine("<TIME>12:05:24</TIME>");
            tw.WriteLine("<TIMESPAN>3.12:05:24</TIMESPAN>");
            tw.WriteLine(" <?PI2 abc?>");
            tw.WriteLine("<SIG_WHITESPACE xml:space='preserve'>  </SIG_WHITESPACE>");
            tw.Write("</ROOT>");
            tw.Flush();
            tw.Dispose();

            // Create Ent file
            FilePathUtil.addStream("LineNumber.ent", new MemoryStream());
            TextWriter twENT = new StreamWriter(FilePathUtil.getStream("LineNumber.ent"));
            twENT.WriteLine("<!ENTITY % ext4 \"blah\">");
            twENT.WriteLine("<!ENTITY ext31 \"%ext4;\">");
            twENT.WriteLine("<!ENTITY ext3 'zzz'>");
            twENT.Flush();
            twENT.Dispose();
        }

        public static void CreateLbNormalizationTestFile(string strFileName)
        {
            TextWriter tw = new StreamWriter(FilePathUtil.getStream(strFileName));

            tw.WriteLine("<?xml version=\"1.0\" standalone=\"no\"?>");
            tw.WriteLine("<!DOCTYPE ROOT");
            tw.WriteLine("[");
            tw.WriteLine("<!ENTITY ge1 SYSTEM \"{0}\">", pLbNormEnt1);
            tw.WriteLine("<!ENTITY % pe SYSTEM \"{0}\">", pLbNormEnt2);
            tw.WriteLine("%pe;");
            tw.WriteLine("]>");
            tw.WriteLine("<ROOT>&ge1;&ext1;</ROOT>");
            tw.Flush();
            tw.Dispose();

            // Create Ent file
            FilePathUtil.addStream(pLbNormEnt1, new MemoryStream());
            TextWriter twENT = new StreamWriter(FilePathUtil.getStream(pLbNormEnt1));
            twENT.WriteLine("<?xml version=\"1.0\"?>");
            twENT.WriteLine("<E1 xml:space=\"preserve\">");
            twENT.WriteLine("</E1>");
            twENT.WriteLine();
            twENT.Flush();
            twENT.Dispose();

            // Create Ent file
            FilePathUtil.addStream(pLbNormEnt2, new MemoryStream());
            twENT = new StreamWriter(FilePathUtil.getStream(pLbNormEnt2));
            twENT.WriteLine("<!ENTITY ext1 \"<E3>");
            twENT.WriteLine("</E3>\">");
            twENT.WriteLine("");
            twENT.WriteLine();
            twENT.Flush();
            twENT.Dispose();
        }

        public bool FindNodeType(XmlReader r, XmlNodeType _nodetype)
        {
            if (r.NodeType == _nodetype)
                return false;

            while (r.Read())
            {
                if (r.NodeType == XmlNodeType.EntityReference)
                {
                    if (r.CanResolveEntity)
                        r.ResolveEntity();
                }

                if (r.NodeType == XmlNodeType.ProcessingInstruction && r.NodeType == XmlNodeType.XmlDeclaration)
                {
                    if (String.Compare(r.Name, 0, ST_XML, 0, 3) != 0)
                        return true;
                }

                if (r.NodeType == _nodetype)
                {
                    return true;
                }

                if (r.NodeType == XmlNodeType.Element && (_nodetype == XmlNodeType.Attribute))
                {
                    if (r.MoveToFirstAttribute())
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    // Class Signatures used for verifying names 
    public class Signatures
    {
        private string[] _stringsExpected;
        public int m_index;

        public Signatures(string[] strs)
        {
            m_index = 0;
            _stringsExpected = strs;
        }

        public static implicit operator Signatures(string[] strs)
        {
            return new Signatures(strs);
        }
    }

    public class CustomReader : XmlReader
    {
        private XmlReader _tr = null;

        public CustomReader(TextReader txtReader, bool isFragment)
        {
            if (!isFragment)
                _tr = XmlReader.Create(txtReader);
            else
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                _tr = XmlReader.Create(txtReader, settings);
            }
        }

        public CustomReader(string url, bool isFragment)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            if (!isFragment)
            {
                _tr = XmlReader.Create(url, settings);
            }
            else
            {
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                _tr = XmlReader.Create(url, settings);
            }
        }

        public override int Depth
        {
            get
            {
                return _tr.Depth;
            }
        }

        public override string Value
        {
            get
            {
                return _tr.Value;
            }
        }

        public override bool MoveToElement()
        {
            return _tr.MoveToElement();
        }

        public override bool IsEmptyElement
        {
            get
            {
                return _tr.IsEmptyElement;
            }
        }

        public override string LocalName
        {
            get
            {
                return _tr.LocalName;
            }
        }

        public override XmlNodeType NodeType
        {
            get
            {
                return _tr.NodeType;
            }
        }

        public override bool MoveToNextAttribute()
        {
            return _tr.MoveToNextAttribute();
        }

        public override bool MoveToFirstAttribute()
        {
            return _tr.MoveToFirstAttribute();
        }

        public override string LookupNamespace(string prefix)
        {
            return _tr.LookupNamespace(prefix);
        }

        public new void Dispose()
        {
            _tr.Dispose();
        }

        public override bool EOF
        {
            get
            {
                return _tr.EOF;
            }
        }

        public override bool HasValue
        {
            get
            {
                return _tr.HasValue;
            }
        }

        public override string NamespaceURI
        {
            get
            {
                return _tr.NamespaceURI;
            }
        }

        public override bool Read()
        {
            return _tr.Read();
        }

        public override XmlNameTable NameTable
        {
            get
            {
                return _tr.NameTable;
            }
        }

        public override bool CanResolveEntity
        {
            get
            {
                return _tr.CanResolveEntity;
            }
        }

        public override void ResolveEntity()
        {
            _tr.ResolveEntity();
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            return _tr.GetAttribute(name, namespaceURI);
        }

        public override string GetAttribute(string name)
        {
            return _tr.GetAttribute(name);
        }

        public override string GetAttribute(int i)
        {
            return _tr.GetAttribute(i);
        }

        public override string BaseURI
        {
            get
            {
                return _tr.BaseURI;
            }
        }

        public override bool ReadAttributeValue()
        {
            return _tr.ReadAttributeValue();
        }

        public override string Prefix
        {
            get
            {
                return _tr.Prefix;
            }
        }

        public override bool MoveToAttribute(string name, string ns)
        {
            return _tr.MoveToAttribute(name, ns);
        }

        public override bool MoveToAttribute(string name)
        {
            return _tr.MoveToAttribute(name);
        }

        public override int AttributeCount
        {
            get
            {
                return _tr.AttributeCount;
            }
        }

        public override ReadState ReadState
        {
            get
            {
                return _tr.ReadState;
            }
        }
    }

    public class CustomWriter : XmlWriter
    {
        private XmlWriter _writer = null;
        private TextWriter _stream = null;

        public CustomWriter(TextWriter stream, XmlWriterSettings xws)
        {
            _stream = stream;
            _writer = XmlWriter.Create(stream, xws);
        }

        public override void WriteStartDocument()
        {
            _writer.WriteStartDocument();
        }

        public override void WriteStartDocument(bool standalone)
        {
            _writer.WriteStartDocument(standalone);
        }

        public override void WriteEndDocument()
        {
            _writer.WriteEndDocument();
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            _writer.WriteDocType(name, pubid, sysid, subset);
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            _writer.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteEndElement()
        {
            _writer.WriteEndElement();
        }

        public override void WriteFullEndElement()
        {
            _writer.WriteFullEndElement();
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            _writer.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteEndAttribute()
        {
            _writer.WriteEndAttribute();
        }

        public override void WriteCData(string text)
        {
            _writer.WriteCData(text);
        }

        public override void WriteComment(string text)
        {
            _writer.WriteComment(text);
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            _writer.WriteProcessingInstruction(name, text);
        }

        public override void WriteEntityRef(string name)
        {
            _writer.WriteEntityRef(name);
        }

        public override void WriteCharEntity(char ch)
        {
            _writer.WriteCharEntity(ch);
        }

        public override void WriteWhitespace(string ws)
        {
            _writer.WriteWhitespace(ws);
        }

        public override void WriteString(string text)
        {
            _writer.WriteString(text);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            _writer.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            _writer.WriteChars(buffer, index, count);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            _writer.WriteRaw(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            _writer.WriteRaw(data);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            _writer.WriteBase64(buffer, index, count);
        }

        public override WriteState WriteState
        {
            get
            {
                return _writer.WriteState;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                return _writer.XmlSpace;
            }
        }

        public override string XmlLang
        {
            get
            {
                return _writer.XmlLang;
            }
        }

        public new void Dispose()
        {
            _writer.Dispose();
            _stream.Dispose();
        }

        public override void Flush()
        {
            _writer.Flush();
        }

        public override string LookupPrefix(string ns)
        {
            return _writer.LookupPrefix(ns);
        }
    }
}
