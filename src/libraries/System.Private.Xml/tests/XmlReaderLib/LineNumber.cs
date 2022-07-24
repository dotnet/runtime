// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    [InheritRequired()]
    public abstract partial class TCLinePos : TCXMLReaderBaseGeneral
    {
        public const string ST_ELEMENT = "ELEMENT";
        public const string ST_SKIP = "SKIP";
        public const string ST_ENTITYREF = "ENTITYREF";
        public const string ST_A0 = "a0";
        public const string ST_A1 = "a1";
        public const string ST_A2 = "a2";
        public const string ST_BASE64 = "BASE64";
        public const string ST_BINHEX = "BINHEX";
        public const string ST_CHARENTITY = "CHARENTITY";

        public const string ST_BOOLXSD = "BOOLXSD";
        public const string ST_DATE = "DATE";
        public const string ST_DATETIME = "DATETIME";
        public const string ST_INT = "INT";
        public const string ST_TIME = "TIME";
        public const string ST_TIMESPAN = "TIMESPAN";
        public const string ST_DECIMAL2 = "DECIMAL";

        private void CheckPos(int line, int pos)
        {
            if (!IsCustomReader())
            {
                CError.WriteLine("(" + DataReader.Name + "," + DataReader.Value + ")");
                CError.Compare(DataReader.Settings.LineNumberOffset, line, "LineNumber");
                CError.Compare(DataReader.Settings.LinePositionOffset, pos, "LinePos");
            }
        }

        public void PositionOnNodeType2(XmlNodeType nodeType)
        {
            while (DataReader.Read() && DataReader.NodeType != nodeType)
            {
            }
            if (DataReader.EOF)
            {
                throw new CTestFailedException("Couldn't find XmlNodeType " + nodeType);
            }
        }

        [Variation("LineNumber/LinePos after Read and NodeType = Element", Priority = 0)]
        public int TestLinePos1()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            DataReader.PositionOnElement(ST_ELEMENT);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after Read and NodeType = CDATA", Priority = 0)]
        public int TestLinePos2()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            PositionOnNodeType2(XmlNodeType.CDATA);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after Read and NodeType = Comment", Priority = 0)]
        public int TestLinePos4()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            PositionOnNodeType2(XmlNodeType.Comment);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after Read and NodeType = EndElement", Priority = 0)]
        public int TestLinePos6()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            PositionOnNodeType2(XmlNodeType.EndElement);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after Read and NodeType = EntityReference, not expanded", Priority = 0)]
        public int TestLinePos7()
        {
            CError.Skip("Skipped");
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            PositionOnNodeType2(XmlNodeType.EntityReference);

            CheckPos(11, 14);

            DataReader.Read();
            CheckPos(11, 17);

            DataReader.Read();
            CheckPos(11, 19);

            DataReader.Read();
            CheckPos(11, 24);

            DataReader.Read();
            CError.Compare(DataReader.NodeType, XmlNodeType.EndElement, "ee");
            CheckPos(11, 27);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after Read and NodeType = ProcessingInstruction", Priority = 0)]
        public int TestLinePos9()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));

            PositionOnNodeType2(XmlNodeType.ProcessingInstruction);
            CheckPos(0, 0);

            PositionOnNodeType2(XmlNodeType.ProcessingInstruction);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after Read and NodeType = SignificantWhitespace", Priority = 0)]
        public int TestLinePos10()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            PositionOnNodeType2(XmlNodeType.SignificantWhitespace);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after Read and NodeType = Text", Priority = 0)]
        public int TestLinePos11()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            PositionOnNodeType2(XmlNodeType.Text);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after Read and NodeType = Whitespace", Priority = 0)]
        public int TestLinePos12()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            PositionOnNodeType2(XmlNodeType.Whitespace);
            CheckPos(0, 0);
            PositionOnNodeType2(XmlNodeType.Whitespace);
            CheckPos(0, 0);
            PositionOnNodeType2(XmlNodeType.Whitespace);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after Read and NodeType = XmlDeclaration", Priority = 0)]
        public int TestLinePos13()
        {
            if (IsSubtreeReader()) CError.Skip("Skipped");
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            PositionOnNodeType2(XmlNodeType.XmlDeclaration);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after MoveToElement")]
        public int TestLinePos14()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));

            DataReader.PositionOnElement(ST_ELEMENT);
            CheckPos(0, 0);
            DataReader.MoveToAttribute(1);
            DataReader.MoveToElement();
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after MoveToFirstAttribute/MoveToNextAttribute")]
        public int TestLinePos15()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            DataReader.PositionOnElement(ST_ELEMENT);
            CheckPos(0, 0);
            DataReader.MoveToFirstAttribute();
            CheckPos(0, 0);
            DataReader.MoveToNextAttribute();
            CheckPos(0, 0);
            DataReader.MoveToNextAttribute();
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after MoveToAttribute")]
        public int TestLinePos16()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            DataReader.PositionOnElement(ST_ELEMENT);
            CheckPos(0, 0);
            DataReader.MoveToAttribute(1);
            CheckPos(0, 0);
            DataReader.MoveToAttribute(0);
            CheckPos(0, 0);
            DataReader.MoveToAttribute(2);
            CheckPos(0, 0);
            DataReader.MoveToAttribute(ST_A0);
            CheckPos(0, 0);
            DataReader.MoveToAttribute(ST_A2);
            CheckPos(0, 0);
            DataReader.MoveToAttribute(ST_A1);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after Skip")]
        public int TestLinePos18()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            DataReader.PositionOnElement(ST_ELEMENT);
            DataReader.MoveToFirstAttribute();
            DataReader.Skip();
            CheckPos(0, 0);
            DataReader.PositionOnElement(ST_SKIP);
            DataReader.Skip();
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after ReadInnerXml")]
        public int TestLinePos19()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            DataReader.PositionOnElement(ST_ELEMENT);
            DataReader.MoveToFirstAttribute();
            DataReader.ReadInnerXml();
            CheckPos(0, 0);
            DataReader.PositionOnElement(ST_SKIP);
            DataReader.ReadInnerXml();
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after MoveToContent")]
        public int TestLinePos20()
        {
            if (IsSubtreeReader()) CError.Skip("Skipped");

            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));

            PositionOnNodeType2(XmlNodeType.XmlDeclaration);
            DataReader.MoveToContent();
            CheckPos(0, 0);

            PositionOnNodeType2(XmlNodeType.Comment);
            DataReader.MoveToContent();
            CheckPos(0, 0);

            PositionOnNodeType2(XmlNodeType.ProcessingInstruction);
            DataReader.MoveToContent();
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after ReadBase64 successive calls")]
        public int TestLinePos21()
        {
            if (IsCustomReader()) CError.Skip("Skipped");

            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            DataReader.PositionOnElement(ST_BASE64);

            byte[] arr = new byte[3];
            DataReader.ReadElementContentAsBase64(arr, 0, 3);
            CheckPos(0, 0);

            DataReader.ReadElementContentAsBase64(arr, 0, 3);
            CheckPos(0, 0);

            DataReader.ReadElementContentAsBase64(arr, 0, 1);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after ReadBinHex successive calls")]
        public int TestLinePos22()
        {
            if (IsCustomReader()) CError.Skip("Skipped");

            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            DataReader.PositionOnElement(ST_BINHEX);

            byte[] arr = new byte[1];

            DataReader.ReadElementContentAsBinHex(arr, 0, 1);
            CheckPos(0, 0);

            DataReader.ReadElementContentAsBinHex(arr, 0, 1);
            CheckPos(0, 0);

            DataReader.ReadElementContentAsBinHex(arr, 0, 1);
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after ReadEndElement")]
        public int TestLinePos26()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            DataReader.PositionOnElement(ST_CHARENTITY);

            DataReader.Read(); // Text
            DataReader.Read(); // EndElement

            CheckPos(0, 0);

            DataReader.ReadEndElement();
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after ReadString")]
        public int TestLinePos27()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            DataReader.PositionOnElement(ST_ENTITYREF);

            DataReader.Read();
            CheckPos(0, 0);

            DataReader.Read();
            CheckPos(0, 0);

            DataReader.Read();
            CheckPos(0, 0);

            DataReader.Read();
            CheckPos(0, 0);

            DataReader.Read();
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos after element containing entities in attribute values")]
        public int TestLinePos39()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            DataReader.PositionOnElement("EMBEDDED");
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("LineNumber/LinePos when Read = false")]
        public int TestLinePos40()
        {
            ReloadSource(Path.Combine(TestData, "Common", "LineNumber.xml"));
            while (DataReader.Read()) ;
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("XmlTextReader:LineNumber and LinePos don't return the right position after ReadInnerXml is called")]
        public int TestLinePos41()
        {
            string strXml = "<ROOT><CHARS2>\nxxx<MARKUP/>yyy\n</CHARS2></ROOT>";
            ReloadSourceStr(strXml);
            if (!IsCustomReader())
            {
                DataReader.Read();
                DataReader.Read();

                CError.Equals(DataReader.LineNumber, 1, "ln1");
                CError.Equals(DataReader.LinePosition, 8, "lp1");

                DataReader.ReadInnerXml();
                CError.Equals(DataReader.LineNumber, 3, "ln2");
                CError.Equals(DataReader.LinePosition, 12, "lp2");
            }
            return TEST_PASS;
        }

        [Variation("XmlTextReader: LineNum and LinePosition incorrect for EndTag token and text element")]
        public int TestLinePos42()
        {
            string strXml = "<foo>\n       fooooooo\n</foo>";
            ReloadSourceStr(strXml);
            if (!IsCustomReader())
            {
                DataReader.Read();
                CError.Equals(DataReader.LineNumber, 1, null);
                CError.Equals(DataReader.LinePosition, 2, null);

                DataReader.Read();
                CError.Equals(DataReader.LineNumber, 1, null);
                CError.Equals(DataReader.LinePosition, 6, null);

                DataReader.Read();
                CError.Equals(DataReader.LineNumber, 3, null);
                CError.Equals(DataReader.LinePosition, 3, null);
            }
            return TEST_PASS;
        }

        [Variation("Bogus LineNumber value when reading attribute over XmlTextReader")]
        public int TestLinePos43()
        {
            string strXml = "<foo\n    attr1='bar'\n    attr2='foo'\n/>";
            ReloadSourceStr(strXml);
            if (!IsCustomReader())
            {
                DataReader.Read();
                CError.Equals(DataReader.LineNumber, 1, null);
                CError.Equals(DataReader.LinePosition, 2, null);

                DataReader.MoveToFirstAttribute();
                CError.Equals(DataReader.LineNumber, 2, null);
                CError.Equals(DataReader.LinePosition, 5, null);

                DataReader.MoveToNextAttribute();
                CError.Equals(DataReader.LineNumber, 3, null);
                CError.Equals(DataReader.LinePosition, 5, null);

                DataReader.Read();
                CError.Equals(DataReader.LineNumber, IsSubtreeReader() ? 0 : 4, null);
                CError.Equals(DataReader.LinePosition, IsSubtreeReader() ? 0 : 3, null);
            }
            return TEST_PASS;
        }

        [Variation("LineNumber and LinePosition on attribute with columns")]
        public int TestLinePos44()
        {
            string strxml = "<PRODUCT xmlns:a='abc' xmlns:b='abc'/>";
            ReloadSourceStr(strxml);

            CError.Compare(DataReader.Read(), true, "Read");
            CError.Compare(DataReader.MoveToNextAttribute(), true, "MoveToNextAttribute");
            CError.Compare(DataReader.Value, "abc", "MoveToNextAttribute");
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, "xmlns:a", "abc"), "xmlns:a");
            CheckPos(0, 0);
            return TEST_PASS;
        }

        [Variation("HasLineInfo")]
        public int TestLinePos45()
        {
            XmlReader DataReader = ReaderHelper.Create(new StringReader("<root></root>"));
            DataReader.Read();
            CError.Compare((DataReader as IXmlLineInfo).HasLineInfo(), "DataReader HasLineInfo");
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReader vr = ReaderHelper.Create(DataReader, rs);
            vr.Read();
            CError.Compare((vr as IXmlLineInfo).HasLineInfo(), "DataReader HasLineInfo");
            return TEST_PASS;
        }

        [Variation("XmlException LineNumber and LinePosition")]
        public int TestLinePos99()
        {
            string strxml = "<portfolio>\n <stock exchange=nasdaq/>\n</portfolio>";
            ReloadSourceStr(strxml);

            try
            {
                while (DataReader.Read()) ;
            }
            catch (XmlException e)
            {
                CError.Compare(e.LineNumber, 2, "ln");
                CError.Compare(e.LinePosition, 18, "lp");
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("Check error message on a non-wellformed XML")]
        public int ReadingNonWellFormedXmlThrows()
        {
            string filename = Path.Combine(TestData, "Common", "Bug86503.txt");
            try
            {
                ReloadSource(filename);
                DataReader.Read();
            }
            catch (XmlException) { return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("When an XmlException is thrown both XmlException.LineNumber and XmlTextReader.LineNumber should be same")]
        public int XmlExceptionAndXmlTextReaderLineNumberShouldBeSameAfterExceptionIsThrown()
        {
            string filename = Path.Combine(TestData, "Common", "invalid-ucs4.xml");
            if (!IsCustomReader())
            {
                try
                {
                    ReloadSource(filename);
                    while (DataReader.Read()) ;
                    return TEST_FAIL;
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e.Message);
                    CError.WriteLine("Reader Line : {0}, Exception Line {1}", DataReader.LineNumber, e.LinePosition);
                    CError.Equals(DataReader.LineNumber, IsSubtreeReader() ? 0 : e.LineNumber, "Reader line number and Exception line number must be same");

                    CError.WriteLine("Reader Position : {0}, Exception Position {1}", DataReader.LinePosition, e.LinePosition);
                    CError.Equals(DataReader.LinePosition, IsSubtreeReader() ? 0 : e.LinePosition, "Reader line position and Exception line position must be same");
                    return TEST_PASS;
                }
            }
            return TEST_PASS;
        }

        [Variation("Xml(Text)Reader does not increase line number for a new line in element end tag")]
        public int XmlReaderShouldIncreaseLineNumberAfterNewLineInElementTag()
        {
            string fileName = Path.Combine(TestData, "Common", "Bug411697.xml");
            if (!IsCustomReader())
            {
                ReloadSource(fileName);
                int lastLineNumber = 0;
                while (DataReader.Read())
                {
                    lastLineNumber = DataReader.LineNumber;
                }
                if (lastLineNumber != 2 && !IsSubtreeReader())
                    CError.Compare(false, "Failed");
            }
            return TEST_PASS;
        }

        [Variation("LineNumber and LinePosition are not correct")]
        public int LineNumberAndLinePositionAreCorrect()
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            Stream fs = FilePathUtil.getStream(Path.Combine(TestData, "Common", "Bug297091.xsl"));
            {
                XmlReader DataReader = ReaderHelper.Create(fs, rs, Path.Combine(TestData, "Common", "Bug297091.xsl"));

                DataReader.Read();
                if (DataReader.NodeType != XmlNodeType.Element || ((IXmlLineInfo)DataReader).LineNumber != 1 || ((IXmlLineInfo)DataReader).LinePosition != 2)
                    CError.Compare(false, "Failed");

                DataReader.Read();
                if (DataReader.NodeType != XmlNodeType.Whitespace || ((IXmlLineInfo)DataReader).LineNumber != 4 || ((IXmlLineInfo)DataReader).LinePosition != 2)
                    CError.Compare(false, "Failed");

                DataReader.Read();
                if (DataReader.NodeType != XmlNodeType.Element || ((IXmlLineInfo)DataReader).LineNumber != 5 || ((IXmlLineInfo)DataReader).LinePosition != 3)
                    CError.Compare(false, "Failed");

                DataReader.Read();
                if (DataReader.NodeType != XmlNodeType.Whitespace || ((IXmlLineInfo)DataReader).LineNumber != 5 || ((IXmlLineInfo)DataReader).LinePosition != 28)
                    CError.Compare(false, "Failed");

                DataReader.Read();
                if (DataReader.NodeType != XmlNodeType.EndElement || ((IXmlLineInfo)DataReader).LineNumber != 6 || ((IXmlLineInfo)DataReader).LinePosition != 3)
                    CError.Compare(false, "Failed");
            }
            return TEST_PASS;
        }
    }
}
