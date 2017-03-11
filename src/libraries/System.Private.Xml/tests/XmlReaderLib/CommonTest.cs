// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAssembly]

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // TestCase TCXML Dispose
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCDispose : TCXMLReaderBaseGeneral
    {
        [Variation("Test Integrity of all values after Dispose")]
        public int Variation1()
        {
            ReloadSource();
            DataReader.Dispose();

            CError.Compare(DataReader.Name, String.Empty, "Name");
            CError.Compare(DataReader.NodeType, XmlNodeType.None, "NodeType");
            CError.Compare(DataReader.ReadState, ReadState.Closed, "ReadState");
            CError.Compare(DataReader.AttributeCount, 0, "Attrib Count");
            CError.Compare(DataReader.XmlLang, String.Empty, "XML Lang");
            CError.Compare(DataReader.XmlSpace, XmlSpace.None, "Space");
            if (!(IsXmlNodeReader() || IsXmlNodeReaderDataDoc()))
                CError.Compare(DataReader.BaseURI, String.Empty, "BaseUri");
            CError.Compare(DataReader.Depth, 0, "Depth");
            CError.Compare(DataReader.EOF, IsSubtreeReader() ? true : false, "EOF");
            CError.Compare(DataReader.HasAttributes, false, "HasAttr");
            CError.Compare(DataReader.HasValue, false, "HasValue");
            CError.Compare(DataReader.IsDefault, false, "IsDefault");
            if (!(IsXsltReader() || IsCustomReader() || IsBinaryReader() || IsXPathNavigatorReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()))
            {
                CError.Compare(DataReader.LineNumber, 0, "LN");
                CError.Compare(DataReader.LinePosition, 0, "LP");
            }
            CError.Compare(DataReader.LocalName, String.Empty, "LocalName");
            CError.Compare(DataReader.IsEmptyElement, false, "IsEmptyElement");
            CError.Compare(DataReader.Read(), IsCharCheckingReader() ? true : false, "Read");
            CError.Compare(DataReader.ReadAttributeValue(), false, "ReadAV");
            CError.Compare(DataReader.ReadInnerXml(), string.Empty, "ReadIX");
            CError.Compare(DataReader.ReadOuterXml(), string.Empty, "ReadOX");

            return TEST_PASS;
        }

        [Variation("Call Dispose Multiple(3) Times")]
        public int Variation2()
        {
            ReloadSource();
            DataReader.Dispose();
            DataReader.Dispose();
            DataReader.Dispose();
            return TEST_PASS;
        }

        [Variation("Check Dispose with Exclusive Stream")]
        public int Variation3()
        {
            return TEST_SKIPPED;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML Invalid XML
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCInvalidXML : TCXMLReaderBaseGeneral
    {
        private bool TestInvalidXmlFile(string filename, int expectedLine, int expectedPosition, string expectedErrorCode)
        {
            try
            {
                ReloadSource(filename);
                CError.WriteLine(filename);
                return TestInvalidXml(expectedLine, expectedPosition, expectedErrorCode);
            }
            catch (XmlException) { return false; }
        }

        private bool TestInvalidXmlFile(string filename, int expectedLine, int expectedPosition, string expectedErrorCode, string varSetting)
        {
            try
            {
                ReloadSource(filename);
                CError.WriteLine(filename);
                return TestInvalidXml(expectedLine, expectedPosition, expectedErrorCode, varSetting);
            }
            catch (XmlException) { return false; }
        }

        private bool TestInvalidXmlStr(string strxml, int expectedLine, int expectedPosition, string expectedErrorCode)
        {
            try
            {
                ReloadSource(new StringReader(strxml));
                return TestInvalidXml(expectedLine, expectedPosition, expectedErrorCode);
            }
            catch (XmlException) { return true; }
        }

        private bool TestInvalidXml(int expectedLine, int expectedPosition, string expectedErrorCode)
        {
            try
            {
                while (DataReader.Read()) ;
            }
            catch (XmlException e)
            {
                CheckXmlException(expectedErrorCode, e, expectedLine, expectedPosition);
                return true;
            }

            CError.Equals(false, "Accepted invalid XML");
            return false;
        }

        private bool TestInvalidXml(int expectedLine, int expectedPosition, string expectedErrorCode, string varSetting)
        {
            while (DataReader.Read()) ;
            return true;
        }

        private bool TestValidXmlStr(string xml)
        {
            try
            {
                ReloadSource(new StringReader(xml));
                while (DataReader.Read()) ;
                return true;
            }
            catch (Exception e)
            {
                CError.WriteLine(e);
                return false;
            }
        }

        [Variation("Read with invalid content")]
        public int Read1()
        {
            string xml = "<ROOT>[content]]>text</ROOT>";
            try
            {
                ReloadSourceStr(xml);
                while (DataReader.Read()) ;
            }
            catch (XmlException e)
            {
                CheckXmlException("Xml_CDATAEndInText", e, 1, 15);
                return TEST_PASS;
            }

            return TEST_FAIL;
        }

        [Variation("Read with invalid end tag")]
        public int Read2()
        {
            string strxml = "<docElem>\n</docElem>\n</docElem>";
            try
            {
                ReloadSourceStr(strxml);
                while (DataReader.Read()) ;
            }
            catch (XmlException e)
            {
                CError.Compare(e.LineNumber, 3, "ln");
                CError.Compare(e.LinePosition, 3, "lp");
                CheckXmlException("Xml_UnexpectedEndTag", e, 3, 3);
                return TEST_PASS;
            }
            return IsSubtreeReader() ? TEST_PASS : TEST_FAIL;
        }

        [Variation("Read with invalid name")]
        public int Read3()
        {
            string strxml = "<root:  />";
            try
            {
                ReloadSourceStr(strxml);
                while (DataReader.Read()) ;
            }
            catch (XmlException e)
            {
                CError.WriteLine("Line Number : " + e.LineNumber);
                CError.WriteLine("Line Position : " + e.LinePosition);
                CError.WriteLine("Message : {0}", e.Message);
                CError.WriteLine("Source  : {0}", e.Source);
                CheckXmlException("Xml_BadStartNameChar", e, 1, 7);
                return TEST_PASS;
            }

            return TEST_FAIL;
        }

        [Variation("Read with invalid characters")]
        public int Read4()
        {
            string filename = Path.Combine(TestData, "Common", "bad.xml");
            try
            {
                ReloadSource(filename);
                while (DataReader.Read()) ;
                CError.WriteLine("Accepted invalid character");
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                CheckXmlException("Xml_InvalidCharInThisEncoding", e, 11, 69);
            }

            return TEST_PASS;
        }

        [Variation("Read with two DOCTYPE nodes")]
        public int Read5()
        {
            string strxml = "<!DOCTYPE root [<!ELEMENT root ANY >]><!DOCTYPE root [	<!ELEMENT root ANY >]><root/>";
            try
            {
                ReloadSource(new StringReader(strxml));
                DataReader.Read();
                while (DataReader.Read()) ;
                CError.WriteLine("Accepted two DOCTYPE nodes");
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                CheckXmlException("Xml_MultipleDTDsProvided", e, 1, 39);
            }

            return TEST_PASS;
        }

        [Variation("Read with missing root end element")]
        public int Read6()
        {
            string strxml = "<!DOCTYPE root [<!ELEMENT root ANY >]>\n<root>abcd<root/>";
            try
            {
                ReloadSource(new StringReader(strxml));
                while (DataReader.Read()) ;
                CError.WriteLine("Accepted XML without root end element");
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                CheckXmlException("Xml_UnexpectedEOFInElementContent", e, 2, 18);
            }
            return TEST_PASS;
        }

        [Variation("Read invalid text declaration")]
        public int Read8()
        {
            string filename = Path.Combine(TestData, "Common", "bug_65660a.xml");
            try
            {
                ReloadSource(filename);
                while (DataReader.Read()) ;
                CError.WriteLine("Accepted invalid text declaration");
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                CError.WriteLine("Line Number : {0}", e.LineNumber);
                CError.WriteLine("Line Position : {0}", e.LinePosition);
                CError.WriteLine("Message : {0}", e.Message);
                CError.WriteLine("Source  : {0}", e.Source);
                CheckXmlException("Xml_XmlDeclNotFirst", e, 1, 23);
                return TEST_PASS;
            }
        }

        [Variation("Read with surrogate char in entity")]
        public int Read10a()
        {
            string strxml = "<!DOCTYPE ROOT [<!ENTITY a \"\uFF71\">]><ROOT att=\"&a;\"/>";
            ReloadSource(new StringReader(strxml));
            while (DataReader.Read()) ;
            return TEST_PASS;
        }

        [Variation("Read with invalid namespace")]
        public int Read11()
        {
            string strxml = "<root xmlns:p=''/>";
            try
            {
                ReloadSourceStr(strxml);
                while (DataReader.Read()) ;
                CError.WriteLine("Accepted non WF document");
                return TEST_FAIL;
            }
            catch (XmlException e)
            {
                CheckXmlException("Xml_BadNamespaceDecl", e, 1, 15);
            }

            return TEST_PASS;
        }

        [Variation("Attribute containing invalid character &")]
        public int Read12()
        {
            if (IsSubtreeReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()) return TEST_SKIPPED;
            bool bPassed = TestInvalidXmlStr("<test value='foo & bar' />", 1, 19, "Xml_ErrorParsingEntityName");
            if (IsCoreReader() || IsXmlValidatingReader())
            {
                bPassed = TestInvalidXmlStr("<test value='foo&bar' />", 1, 21, "Xml_UnexpectedTokenEx") && bPassed;
            }
            else
            {
                bPassed = TestInvalidXmlStr("<test value='foo&bar' />", 1, 18, "Xml_ErrorParsingEntityName") && bPassed;
            }

            return BoolToLTMResult(bPassed);
        }

        [Variation("Incomplete DOCTYPE")]
        public int Read13()
        {
            if (IsSubtreeReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()) return TEST_SKIPPED;
            bool bPassed = TestInvalidXmlStr("<?xml version = '1.0'?>\n<!DOCTYPE test [   <!ELEMENT test ANY><!ELEMENT test2 EMPTY><!ENTITY entempty ", 2, 79, "Xml_IncompleteDtdContent");
            bPassed = TestInvalidXmlStr("<?xml version = '1.0'?>\n<!DOCTYPE test [   \n<!ATTLIST test abc", 3, 19, "Xml_IncompleteDtdContent") && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("Undefined namespace")]
        public int Read14()
        {
            if (IsSubtreeReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()) return TEST_SKIPPED;
            bool bPassed = TestInvalidXmlStr("<a b:c=''/>", 1, 4, "Xml_UnknownNs");
            bPassed = TestInvalidXmlStr("<a:b/>", 1, 2, "Xml_UnknownNs") && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("Read an XML Fragment which has unclosed elements")]
        public int Read15()
        {
            if (IsXmlNodeReader() || IsXmlNodeReaderDataDoc()) return TEST_SKIPPED;
            string strxml = "<a>x<a/>";
            bool bPassed = TestInvalidXmlStr(strxml, 1, 9, "Xml_UnexpectedEOFInElementContent");

            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid entity value")]
        public int Read16()
        {
            string filename = Path.Combine(TestData, "Common", "bug_62766.xml");
            try
            {
                ReloadSource(filename);
                while (DataReader.Read()) ;
                if (IsXmlTextReader()) return TEST_PASS;
                else
                {
                    CError.WriteLine("Accepted invalid entity");
                    return TEST_FAIL;
                }
            }
            catch (XmlException e)
            {
                CheckXmlException("Xml_BadAttributeChar", e, 5, 14);
                return TEST_PASS;
            }
        }

        [Variation("Read invalid UCS4 file")]
        public int Read17()
        {
            if (IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader()) return TEST_SKIPPED;
            string filename = Path.Combine(TestData, "Common", "invalid-ucs4.xml");
            bool bPassed = TestInvalidXmlFile(filename, 35, 10, "Xml_InvalidCharInThisEncoding");

            return BoolToLTMResult(bPassed);
        }

        [Variation("Read invalid UCS4 file 1234")]
        public int Read18()
        {
            if (IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader()) return TEST_SKIPPED;
            string filename = Path.Combine(TestData, "Common", "invalid-ucs4_1234.xml");
            bool bPassed = TestInvalidXmlFile(filename, 35, 10, "Xml_InvalidCharInThisEncoding");

            return BoolToLTMResult(bPassed);
        }

        [Variation("Read invalid UCS4 file 2143")]
        public int Read19()
        {
            if (IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader()) return TEST_SKIPPED;
            string filename = Path.Combine(TestData, "Common", "invalid-ucs4_2143.xml");
            bool bPassed = TestInvalidXmlFile(filename, 35, 10, "Xml_InvalidCharInThisEncoding");

            return BoolToLTMResult(bPassed);
        }

        [Variation("Read invalid UCS4 file 3412")]
        public int Read20()
        {
            if (IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader()) return TEST_SKIPPED;
            string filename = Path.Combine(TestData, "Common", "invalid-ucs4_3412.xml");
            bool bPassed = TestInvalidXmlFile(filename, 35, 10, "Xml_InvalidCharInThisEncoding");

            return BoolToLTMResult(bPassed);
        }

        [Variation("Read invalid PIs")]
        public int Read21()
        {
            if (IsSubtreeReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()) return TEST_SKIPPED;

            bool bPassed = TestInvalidXmlStr("<?xml?><!DOCTYPE root []><root/>", 1, 6, "Xml_InvalidXmlDecl");
            bPassed = TestInvalidXmlStr("<?pi", 1, 5, "Xml_UnexpectedEOF") && bPassed;
            bPassed = TestInvalidXmlStr("<?pi?", 1, 5, "Xml_BadNameChar") && bPassed;
            bPassed = TestInvalidXmlStr("<?:: abc?>", 1, 3, "Xml_BadStartNameChar") && bPassed;
            bPassed = TestInvalidXmlStr("<?pi ?", 1, 7, "Xml_UnexpectedEOF") && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("Tag name > 4K, invalid")]
        public int Read22()
        {
            string strtag = new String('a', 17 * 1024);
            string strxml = String.Format("<{0}><{0}>{0}</{0}></{0}", strtag);
            bool bPassed = TestInvalidXmlStr(strxml, 1, 87050, "Xml_UnexpectedEOFInElementContent");

            strxml = String.Format("<{0}><{0}>{0}</0></0>", strtag);
            bPassed = TestInvalidXmlStr(strxml, 1, 52231, "Xml_BadStartNameChar") && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("Surrogate char in name > 4K, invalid")]
        public int Read22a()
        {
            string strtag = new String('\uFF71', 17 * 1024);
            string strxml = String.Format("<{0}><{0}>{0}</{0}></{0}>", strtag);
            bool bPassed = TestInvalidXmlStr(strxml, 1, 2, "Xml_BadStartNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Line number/position of whitespace before external entity (regression)")]
        public int Read23()
        {
            if (IsCoreReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            string filename = Path.Combine(TestData, "Common", "bug_57841.xml");

            int[][] expPos = new int[][] { new int[] { 9, 7 }, new int[] { 9, 14 }, new int[] { 9, 21 }, new int[] { 9, 28 }, new int[] { 9, 35 } };

            ReloadSource(filename);
            DataReader.PositionOnElement("root");
            for (int i = 0; i < expPos.Length; i++)
            {
                PositionOnNodeType(XmlNodeType.Whitespace);
                CError.Compare((DataReader.Internal as IXmlLineInfo).LineNumber, expPos[i][0], "LineNumber " + i.ToString());
                CError.Compare((DataReader.Internal as IXmlLineInfo).LinePosition, expPos[i][1], "LinePosition " + i.ToString());
                DataReader.Read();
            }
            return TEST_PASS;
        }

        [Variation("1.Valid XML declaration.Errata5", Param = "1.0")]
        public int Read24a()
        {
            string str = String.Format("<?xml version='{0}' ?><root />", this.CurVariation.Param);
            CError.WriteLine(str);
            bool bPassed = TestValidXmlStr(str);
            return BoolToLTMResult(bPassed);
        }

        //[Variation("1.Invalid XML declaration.version", Param = "2.0")]
        //[Variation("2.Invalid XML declaration.version", Param = "1.1.")]
        //[Variation("3.Invalid XML declaration.version", Param = "0.1")]
        //[Variation("4.Invalid XML declaration.version", Param = "0.9")]
        //[Variation("5.Invalid XML declaration.version", Param = "1")]
        //[Variation("6.Invalid XML declaration.version", Param = "1.a")]
        //[Variation("7.Invalid XML declaration.version", Param = "#45")]
        //[Variation("8.Invalid XML declaration.version", Param = "\\uD812")]
        public int Read25b()
        {
            if (IsSubtreeReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()) return TEST_SKIPPED;
            string str = String.Format("<?xml version='{0}'>", this.CurVariation.Param);
            CError.WriteLine(str);
            bool bPassed = TestInvalidXmlStr(str, 1, 16, "Xml_InvalidVersionNumber");
            return BoolToLTMResult(bPassed);
        }

        //[Variation("1.Invalid XML declaration.standalone", Param = "true")]
        //[Variation("2.Invalid XML declaration.standalone", Param = "false")]
        //[Variation("3.Invalid XML declaration.standalone", Param = "Yes")]
        //[Variation("4.Invalid XML declaration.standalone", Param = "No")]
        //[Variation("5.Invalid XML declaration.standalone", Param = "1")]
        //[Variation("6.Invalid XML declaration.standalone", Param = "0")]
        //[Variation("7.Invalid XML declaration.standalone", Param = "#45")]
        //[Variation("8.Invalid XML declaration.standalone", Param = "\\uD812")]
        public int Read26b()
        {
            if (IsSubtreeReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc()) return TEST_SKIPPED;
            string str = String.Format("<?xml version='1.0' standalone='{0}'>", this.CurVariation.Param);
            CError.WriteLine(str);
            bool bPassed = TestInvalidXmlStr(str, 1, 32, "Xml_InvalidXmlDecl");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside comment")]
        public int Read28()
        {
            bool bPassed = TestInvalidXmlStr("<root><!--\uD812--></root>", 1, 12, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root><!--\uFF71--></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside comment.begin")]
        public int Read29a()
        {
            bool bPassed = TestInvalidXmlStr("<root><!--\uD812comment --></root>", 1, 12, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root><!--\uFF71comment--></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside comment.mid")]
        public int Read29b()
        {
            bool bPassed = TestInvalidXmlStr("<root><!--comment\uD812comment --></root>", 1, 19, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root><!--comment\uFF71comment --></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside comment.end")]
        public int Read29c()
        {
            bool bPassed = TestInvalidXmlStr("<root><!--comment\uD812--></root>", 1, 19, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root><!--comment\uFF71--></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside PI")]
        public int Read30()
        {
            bool bPassed = TestInvalidXmlStr("<root><?\uDD12?></root>", 1, 9, "Xml_BadStartNameChar");
            bPassed = TestInvalidXmlStr("<root><?\uFF71?></root>", 1, 9, "Xml_BadStartNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside PI.begin")]
        public int Read30a()
        {
            bool bPassed = TestInvalidXmlStr("<root><?\uDD12pi pi ?></root>", 1, 9, "Xml_BadStartNameChar");
            bPassed = TestInvalidXmlStr("<root><?\uFF71pi pi ?></root>", 1, 9, "Xml_BadStartNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside PI.mid")]
        public int Read30b()
        {
            bool bPassed = TestInvalidXmlStr("<root><?pi\uDD12pi?></root>", 1, 11, "Xml_BadNameChar");
            bPassed = TestInvalidXmlStr("<root><?pi\uFF71pi?></root>", 1, 11, "Xml_BadNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside PI.end")]
        public int Read30c()
        {
            bool bPassed = TestInvalidXmlStr("<root><?pi pi \uDD12?></root>", 1, 15, "Xml_InvalidCharacter");
            bPassed = TestInvalidXmlStr("<root><?abcd\uFF71?></root>", 1, 13, "Xml_BadNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read an invalid character which is a lower part of the surrogate pair")]
        public int Read31()
        {
            bool bPassed = TestInvalidXmlStr("<root attr='\uDF20'/>", 1, 13, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root attr='\uFF71'/>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read an invalid character which is a lower part of the surrogate pair.begin")]
        public int Read31a()
        {
            bool bPassed = TestInvalidXmlStr("<root attr='\uDF20AB'/>", 1, 13, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root attr='\uFF71AB'/>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read an invalid character which is a lower part of the surrogate pair.mid")]
        public int Read31b()
        {
            bool bPassed = TestInvalidXmlStr("<root attr='A\uDF20B'/>", 1, 14, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root attr='A\uFF71B'/>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read an invalid character which is a lower part of the surrogate pair.end")]
        public int Read31c()
        {
            bool bPassed = TestInvalidXmlStr("<root attr='AB\uDF20'/>", 1, 15, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root attr='AB\uFF71'/>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in a name")]
        public int Read32()
        {
            bool bPassed = TestInvalidXmlStr("<\uD812/>", 1, 2, "Xml_BadStartNameChar");
            bPassed = TestInvalidXmlStr("<\uFF71/>", 1, 2, "Xml_BadStartNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in a name.begin")]
        public int Read32a()
        {
            bool bPassed = TestInvalidXmlStr("<\uD812AB/>", 1, 2, "Xml_BadStartNameChar");
            bPassed = TestInvalidXmlStr("<\uFF71AB/>", 1, 2, "Xml_BadStartNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in a name.mid")]
        public int Read32b()
        {
            bool bPassed = TestInvalidXmlStr("<A\uD812B/>", 1, 3, "Xml_BadNameChar");
            bPassed = TestInvalidXmlStr("<A\uFF71B/>", 1, 3, "Xml_BadNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in a name.end")]
        public int Read32c()
        {
            bool bPassed = TestInvalidXmlStr("<AB\uD812/>", 1, 4, "Xml_BadNameChar");
            bPassed = TestInvalidXmlStr("<AB\uFF71/>", 1, 4, "Xml_BadNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside text")]
        public int Read33()
        {
            bool bPassed = TestInvalidXmlStr("<root>\uD812</root>", 1, 8, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root>\uFF71</root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside text.begin")]
        public int Read33a()
        {
            bool bPassed = TestInvalidXmlStr("<root>\uD812abcd</root>", 1, 8, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root>\uFF71abcd</root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside text.mid")]
        public int Read33b()
        {
            bool bPassed = TestInvalidXmlStr("<root>ab\uD812cd</root>", 1, 10, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root>ab\uFF71cd</root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside text.end")]
        public int Read33c()
        {
            bool bPassed = TestInvalidXmlStr("<root>abcd\uD812</root>", 1, 12, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root>abcd\uFF71</root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside CDATA")]
        public int Read34()
        {
            bool bPassed = TestInvalidXmlStr("<root><![CDATA[\uD812]]></root>", 1, 17, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root><![CDATA[\uFF71]]></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside CDATA.begin")]
        public int Read34a()
        {
            bool bPassed = TestInvalidXmlStr("<root><![CDATA[\uD812abcd]]></root>", 1, 17, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root><![CDATA[\uFF71abcd]]></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside CDATA.mid")]
        public int Read34b()
        {
            bool bPassed = TestInvalidXmlStr("<root><![CDATA[ab\uD812cd]]></root>", 1, 19, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root><![CDATA[ab\uFF71cd]]></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with invalid surrogate inside CDATA.end")]
        public int Read34c()
        {
            bool bPassed = TestInvalidXmlStr("<root><![CDATA[abcd\uD812]]></root>", 1, 21, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root><![CDATA[abcd\uFF71]]></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in attr.name")]
        public int Read35()
        {
            bool bPassed = TestInvalidXmlStr("<root \uD812='b'></root>", 1, 7, "Xml_BadStartNameChar");
            bPassed = TestInvalidXmlStr("<root \uFF71='b'></root>", 1, 7, "Xml_BadStartNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in attr.name.begin")]
        public int Read35a()
        {
            bool bPassed = TestInvalidXmlStr("<root \uD812abc='b'></root>", 1, 7, "Xml_BadStartNameChar");
            bPassed = TestInvalidXmlStr("<root \uFF71abcd='b'></root>", 1, 7, "Xml_BadStartNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in attr.name.mid")]
        public int Read35b()
        {
            bool bPassed = TestInvalidXmlStr("<root ab\uD812cd='b'></root>", 1, 9, "Xml_UnexpectedTokenEx");
            bPassed = TestInvalidXmlStr("<root ab\uFF71cd='b'></root>", 1, 9, "Xml_UnexpectedTokenEx");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in attr.name.end")]
        public int Read35c()
        {
            bool bPassed = TestInvalidXmlStr("<root abcd\uD812='b'></root>", 1, 11, "Xml_UnexpectedTokenEx");
            bPassed = TestInvalidXmlStr("<root abcd\uFF71='b'></root>", 1, 11, "Xml_UnexpectedTokenEx");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in attr.val")]
        public int Read36()
        {
            bool bPassed = TestInvalidXmlStr("<root abcd='\uD812'></root>", 1, 14, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root abcd='\uFF71'></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in attr.val.begin")]
        public int Read36a()
        {
            bool bPassed = TestInvalidXmlStr("<root abcd='\uD812xyz'></root>", 1, 14, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root abcd='\uFF71xyz'></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in attr.val.mid")]
        public int Read36b()
        {
            bool bPassed = TestInvalidXmlStr("<root abcd='xy\uD812zy'></root>", 1, 16, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root abcd='xy\uFF71zy'></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in attr.val.end")]
        public int Read36c()
        {
            bool bPassed = TestInvalidXmlStr("<root abcd='xyz\uD812'></root>", 1, 17, "Xml_InvalidCharacter");
            bPassed = TestValidXmlStr("<root abcd='xyz\uFF71'></root>");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in a DTD")]
        public int Read37()
        {
            bool bPassed = TestInvalidXmlStr("<!DOCTYPE \uD812 []><\uD812/>", 1, 11, "Xml_BadStartNameChar");
            bPassed = TestInvalidXmlStr("<!DOCTYPE \uFF71 []><\uFF71/>", 1, 11, "Xml_BadStartNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in a DTD.begin")]
        public int Read37a()
        {
            bool bPassed = TestInvalidXmlStr("<!DOCTYPE \uD812root []><\uD812root/>", 1, 11, "Xml_BadStartNameChar");
            bPassed = TestInvalidXmlStr("<!DOCTYPE \uFF71root []><\uFF71root/>", 1, 11, "Xml_BadStartNameChar");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in a DTD.mid")]
        public int Read37b()
        {
            bool bPassed = TestInvalidXmlStr("<!DOCTYPE ro\uD812ot []><ro\uD812ot/>", 1, 13, "Xml_ExpectExternalOrClose");
            bPassed = TestInvalidXmlStr("<!DOCTYPE ro\uFF71ot []><ro\uFF71ot/>", 1, 13, "Xml_ExpectExternalOrClose");
            return BoolToLTMResult(bPassed);
        }

        [Variation("Read with surrogate in a DTD.end")]
        public int Read37c()
        {
            bool bPassed = TestInvalidXmlStr("<!DOCTYPE root\uD812 []><root\uD812/>", 1, 15, "Xml_ExpectExternalOrClose");
            bPassed = TestInvalidXmlStr("<!DOCTYPE root\uFF71 []><root\uFF71/>", 1, 15, "Xml_ExpectExternalOrClose");
            return BoolToLTMResult(bPassed);
        }

        [Variation("For non-wellformed XMLs, check for the line info in the error message")]
        public int InvalidCommentCharacters()
        {
            if (IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader()) return TEST_SKIPPED;
            string filename = Path.Combine(TestData, "Common", "Bug92020c.xml");
            bool bPassed = TestInvalidXmlFile(filename, 2, 18, "Xml_InvalidCommentChars");
            return BoolToLTMResult(bPassed);
        }

        [Variation("The XmlReader is reporting errors with -ve column values")]
        public int FactoryReaderInvalidCharacter()
        {
            if (!IsFactoryReader()) return TEST_SKIPPED;
            string filename = Path.Combine(TestData, "Common", "Bug26771.xml");
            bool bPassed = TestInvalidXmlFile(filename, 33, 235, "Xml_InvalidCharacter");
            return BoolToLTMResult(bPassed);
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML Read
    //

    [InheritRequired()]
    public abstract partial class TCRead2 : TCXMLReaderBaseGeneral
    {
        private void TestUCS4Encoding(string filename, string expEncoding)
        {
            CError.WriteLine("Reading {0} ...", filename);

            ReloadSource(filename);
            DataReader.Read();
            expEncoding = expEncoding.Trim(); // just reference this parameter
            while (DataReader.Read()) ;
        }

        [Variation("Read valid UCS4 file")]
        public int v1()
        {
            string filename = Path.Combine(TestData, "Common", "valid-ucs4.xml");
            TestUCS4Encoding(filename, "ucs-4");

            return TEST_PASS;
        }

        [Variation("Read after Close")]
        public int v2()
        {
            string strxml = "<ROOT>text</ROOT>";
            ReloadSourceStr(strxml);

            DataReader.Read();
            DataReader.Close();
            CError.Compare(DataReader.EOF, IsSubtreeReader() ? true : false, "close");

            DataReader.Read();
            CError.Compare(DataReader.EOF, IsSubtreeReader() ? true : false, "read");

            return TEST_PASS;
        }

        [Variation("Read stream less than 4K")]
        public int v3()
        {
            string filename = Path.Combine(TestData, "Common", "bug_62146.xml");
            ReloadSource(filename);
            while (DataReader.Read()) ;

            return TEST_PASS;
        }

        [Variation("Read with surrogate character entity")]
        public int v4()
        {
            string strxml = "<root>&#1113088;</root>";
            ReloadSourceStr(strxml);

            DataReader.Read();
            DataReader.Read();

            CError.Compare(DataReader.Value.Length, 2, "len");
            CError.Compare(Convert.ToUInt16(DataReader.Value[0]), (UInt16)56319, "v[0]");
            CError.Compare(Convert.ToUInt16(DataReader.Value[1]), (UInt16)56320, "v[1]");

            return TEST_PASS;
        }

        [Variation("Read with surrogates inside, comments/PIs, text, CDATA")]
        public int v6()
        {
            string strxml = "<root><!--comment \uD812\uDD12--><?pi pi \uD812\uDD12?>\uD812\uDD12<![CDATA[\uD812\uDD12]]></root>";
            ReloadSourceStr(strxml);

            DataReader.Read();
            DataReader.Read();

            CError.Compare(DataReader.Value, "comment \uD812\uDD12", "comment");
            DataReader.Read();
            CError.Compare(DataReader.Value, "pi \uD812\uDD12", "pi");

            DataReader.Read();
            if (IsXsltReader() || IsXPathNavigatorReader())
            {
                CError.Compare(DataReader.Value, "\uD812\uDD12\uD812\uDD12", "text xslt");
            }
            else
            {
                CError.Compare(DataReader.Value, "\uD812\uDD12", "text");

                DataReader.Read();
                CError.Compare(DataReader.Value, "\uD812\uDD12", "CDATA");
            }

            DataReader.Read();
            DataReader.CompareNode(XmlNodeType.EndElement, "root", String.Empty);

            DataReader.Read();
            DataReader.CompareNode(XmlNodeType.None, String.Empty, String.Empty);

            return TEST_PASS;
        }

        [Variation("Read valid UCS4 file 1234")]
        public int v7()
        {
            string filename = Path.Combine(TestData, "Common", "valid-ucs4_1234.xml");
            TestUCS4Encoding(filename, "ucs-4 (Bigendian)");

            return TEST_PASS;
        }

        [Variation("Read valid UCS4 file 2143")]
        public int v8()
        {
            string filename = Path.Combine(TestData, "Common", "valid-ucs4_2143.xml");
            TestUCS4Encoding(filename, "ucs-4 (order 2143)");

            return TEST_PASS;
        }

        [Variation("Read valid UCS4 file 3412")]
        public int v9()
        {
            string filename = Path.Combine(TestData, "Common", "valid-ucs4_3412.xml");
            TestUCS4Encoding(filename, "ucs-4 (order 3412)");

            return TEST_PASS;
        }

        [Variation("Tag name > 4K")]
        public int v10()
        {
            string strtag = new String('a', 17 * 1024);
            string strxml = String.Format("<{0}><{0}>{0}</{0}></{0}>", strtag);
            ReloadSourceStr(strxml);

            while (DataReader.Read()) ;

            return TEST_PASS;
        }

        [Variation("Whitespace characters in character entities")]
        public int v12()
        {
            string strxml = "<case>&#x20;&#x9;&#xA;&#xD;</case>";
            ReloadSourceStr(strxml);

            DataReader.Read();
            bool bPassed = DataReader.VerifyNode(XmlNodeType.Element, "case", String.Empty);
            if (!(IsXsltReader() || IsXPathNavigatorReader()))
            {
                DataReader.Read();
                bPassed = DataReader.VerifyNode(XmlNodeType.Whitespace, String.Empty, " \t\n\r") && bPassed;
            }
            DataReader.Read();
            bPassed = DataReader.VerifyNode(XmlNodeType.EndElement, "case", String.Empty) && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("Root element 18 chars length")]
        public int v13()
        {
            string filename = Path.Combine(TestData, "Common", "Bug_70237.xml");
            ReloadSource(filename);
            string name = "a1234567890abcdefg";
            DataReader.PositionOnElement(name);
            DataReader.CompareNode(XmlNodeType.Element, name, String.Empty);

            while (DataReader.Read()) ;

            return TEST_PASS;
        }

        [Variation("File with external DTD")]
        public int v14()
        {
            string filename = Path.Combine(TestData, "Common", "Bug68766.xml");
            ReloadSource(filename);

            while (DataReader.Read()) ;

            return TEST_PASS;
        }

        [Variation("Namespace prefix starting with xml")]
        public int Read31()
        {
            string strxml = "<schema xmlns:xmlSomething='someurl'/>";
            ReloadSourceStr(strxml);

            while (DataReader.Read()) ;

            return TEST_PASS;
        }

        [Variation("Instantiate an XmlException object without a parameter")]
        public int XmlExceptionCtorWithNoParamsDoesNotThrow()
        {
            try
            {
                XmlException e = new XmlException();
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.Message);
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Test Read of Empty Elements")]
        public int ReadEmpty()
        {
            string xml = "<root/>";
            ReloadSourceStr(xml);
            while (DataReader.Read())
            {
                if (DataReader.NodeType == XmlNodeType.EndElement)
                {
                    CError.WriteLine("EndElement NodeType for empty element");
                    return TEST_FAIL;
                }
            }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("1.Parsing this 'some]' as fragment fails with 'Unexpected EOF' error")]
        public int Read33()
        {
            string[] s = { "sosososo\uD812", "\uD812" };
            for (int i = 0; i < s.Length; i++)
            {
                try
                {
                    ReloadSourceStr(s[i]);
                    DataReader.Read();
                }
                catch (XmlException) { return TEST_PASS; }
            }
            return TEST_FAIL;
        }

        [Variation("2. Parsing this 'some]' as fragment fails with 'Unexpected EOF' error")]
        public int Read33a()
        {
            OneByteStream sim = new OneByteStream(new byte[] { 0xFE, 0xFF, 0, (byte)'s', 0, (byte)'o',
            0, (byte)'s', 0, (byte)'o', 0, (byte)'s', 0, (byte)'o', 0, (byte)'s', 0, (byte)'o',
            0, (byte)']'});

            XmlReaderSettings set = new XmlReaderSettings();
            set.ConformanceLevel = ConformanceLevel.Fragment;
            using (XmlReader r = ReaderHelper.Create(sim, set))
            {
                r.Read();
                CError.WriteLine(r.Value);
            }
            return TEST_PASS;
        }

        [Variation("Parsing xml:space attribute with spaces")]
        public int Read34()
        {
            if (IsXPathNavigatorReader()) return TEST_SKIPPED;

            ReloadSourceStr("<root xml:space='preserve '/>");
            DataReader.Read();
            CError.Compare(DataReader.XmlSpace, (IsCustomReader() || IsXsltReader()) ? XmlSpace.None : XmlSpace.Preserve, "Error");
            return TEST_PASS;
        }

        [Variation("Parsing valid xml in ASCII encoding")]
        public int Read35()
        {
            ReloadSource(new StringReader("<?xml version='1.0' encoding='us-ascii'?><Ro\u00F6t \u00F6='\u00F6'> \u00F6 \u00F6 <!--\u00F6 \u00F6-->\u00F6\u00F6\u00F6<![CDATA[\u00F6\u00F6 \u00F6]]>\u00F6 \u00F6 \u00F6 \u00F6 </Ro\u00F6t>"));
            while (DataReader.Read())
            {
                CError.WriteLine(DataReader.Value);
                CError.WriteLine(DataReader.Depth);
                if (DataReader.HasAttributes)
                {
                    while (DataReader.MoveToNextAttribute())
                    {
                        CError.WriteLine(DataReader.Value);
                        CError.WriteLine(DataReader.Depth);
                    }
                }
            }
            return TEST_PASS;
        }

        [Variation("Parsing valid xml with huge attributes")]
        public int Read36()
        {
            ReloadSource(Path.Combine(TestData, "Common", "hugeattributes.xml"));
            while (DataReader.Read())
            {
                object ob = DataReader.Value;
                int d = DataReader.Depth;
                if (DataReader.HasAttributes)
                {
                    while (DataReader.MoveToNextAttribute())
                    {
                        object o = DataReader.Value;
                        int da = DataReader.Depth;
                    }
                }
            }
            return TEST_PASS;
        }

        [Variation("XmlReader accepts invalid <!ATTLIST e a NOTATION (prefix:name) #IMPLIED> declaration")]
        public int Read37()
        {
            if (IsSubtreeReader()) CError.Skip("Skipped");

            string xml = @"<!DOCTYPE e [
  <!ATTLIST e a NOTATION (prefix:name) #IMPLIED>
]>
<e/>
";
            ReloadSource(new StringReader(xml));
            while (DataReader.Read())
            {
                CError.WriteLine(DataReader.Value);
                CError.WriteLine(DataReader.Depth);
                if (DataReader.HasAttributes)
                {
                    while (DataReader.MoveToNextAttribute())
                    {
                        CError.WriteLine(DataReader.Value);
                        CError.WriteLine(DataReader.Depth);
                    }
                }
            }
            return TEST_PASS;
        }

        [Variation("XmlReader reports strange error message on &#; character entity reference")]
        public int Read38()
        {
            try
            {
                ReloadSource(new StringReader(@"<root>&#;</root>"));
                while (DataReader.Read())
                {
                    CError.WriteLine(DataReader.Value);
                }
            }
            catch (XmlException e) { CError.WriteLine(e); CError.WriteLine(DataReader.Value); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Assert and wrong XmlException.Message when run non-wf xml")]
        public int Read39()
        {
            try
            {
                ReloadSource(new StringReader(@"<SearchTerms><Term Text="));
                while (DataReader.Read())
                {
                    CError.WriteLine(DataReader.Value);
                }
            }
            catch (XmlException e) { CError.WriteLine(e); CError.WriteLine(DataReader.Value); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Testing general entity references itself")]
        public int Read41()
        {
            if (IsBinaryReader() || IsXmlTextReader()) return TEST_SKIPPED;
            string xml = @"<!DOCTYPE ROOT [<!ENTITY a '&a;'>]><ROOT att='&a;'/>";
            try
            {
                ReloadSource(new StringReader(xml));
                while (DataReader.Read())
                {
                    CError.WriteLine(DataReader.Value);
                }
            }
            catch (XmlException e) { CError.WriteLine(e); CError.WriteLine(DataReader.Value); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Testing duplicate attribute")]
        public int Read42()
        {
            string xml = @"<a x:b='d' y:b='d' xmlns:x='c' xmlns:y='c' />";
            try
            {
                ReloadSource(new StringReader(xml));
                while (DataReader.Read())
                {
                    CError.WriteLine(DataReader.Value);
                }
            }
            catch (XmlException e) { CError.WriteLine(e); CError.WriteLine(DataReader.Value); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Testing xml without root element")]
        public int Read43()
        {
            string xml = @"<!DOCTYPE skd PUBLIC 'dsck' ""[]>";
            try
            {
                ReloadSource(new StringReader(xml));
                while (DataReader.Read())
                {
                    CError.WriteLine(DataReader.Value);
                }
            }
            catch (XmlException e) { CError.WriteLine(e); CError.WriteLine(DataReader.Value); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Testing xml with unexpected token")]
        public int Read44()
        {
            string xml = @"<?xml version='1.0'?><!DOCTYPE doc [<!ELEMENT doc ANY><!ENTITY en1 '<doc/>']><doc>&en1;</doc>";
            try
            {
                ReloadSource(new StringReader(xml));
                while (DataReader.Read())
                {
                    CError.WriteLine(DataReader.Value);
                }
            }
            catch (XmlException e) { CError.WriteLine(e); CError.WriteLine(DataReader.Value); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("XmlException when run non-wf xml")]
        public int Read45()
        {
            try
            {
                ReloadSource(new StringReader(@"<SearchTerms        "));
                while (DataReader.Read())
                {
                    CError.WriteLine(DataReader.Value);
                }
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Parsing valid xml with 100 attributes with same names and diff.namespaces")]
        public int Read46()
        {
            ReloadSource(Path.Combine(TestData, "Common", "100attr.xml"));
            while (DataReader.Read())
            {
                object ob = DataReader.Value;
                int d = DataReader.Depth;
                if (DataReader.HasAttributes)
                {
                    while (DataReader.MoveToNextAttribute())
                    {
                        object o = DataReader.Value;
                        int da = DataReader.Depth;
                    }
                }
            }
            return TEST_PASS;
        }

        [Variation("Parsing xml with invalid surrogate pair in PUBLIC")]
        public int Read47()
        {
            try
            {
                ReloadSource(new StringReader("<!DOCTYPE html PUBLIC \"  \uD812  \" \"  \uD812  \">  <greeting>  &hello;  </greeting>"));
                while (DataReader.Read()) ;
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Recursive entity reference inside attribute")]
        public int Read48()
        {
            if (IsBinaryReader() || IsXmlTextReader()) return TEST_SKIPPED;
            string xml = "<!DOCTYPE node [ <!ENTITY d \"<foo b='&a;'/>\">  <!ENTITY a \"&d;\"> <!ELEMENT node ANY> ]> <node>&d;</node>";
            try
            {
                ReloadSource(new StringReader(xml));
                while (DataReader.Read())
                {
                    CError.WriteLine(DataReader.Value);
                    if (DataReader.HasAttributes)
                    {
                        while (DataReader.MoveToNextAttribute())
                        {
                            CError.WriteLine(DataReader.Value);
                        }
                    }
                }
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Parsing valid xml with large number of attributes inside single element")]
        public int Read49()
        {
            ReloadSource(Path.Combine(TestData, "Common", "AttributesLargeNumber.xml"));
            while (DataReader.Read())
            {
                object ob = DataReader.Value;
                int d = DataReader.Depth;
                if (DataReader.HasAttributes)
                {
                    while (DataReader.MoveToNextAttribute())
                    {
                        object o = DataReader.Value;
                        int da = DataReader.Depth;
                    }
                }
            }
            return TEST_PASS;
        }

        //[Variation("1.Test DTD with namespaces", Param = 1)]
        //[Variation("2.Test DTD with namespaces", Param = 2)]
        //[Variation("3.Test DTD with namespaces", Param = 3)]
        public int Read50()
        {
            string xml = "";
            switch ((int)CurVariation.Param)
            {
                case 1: xml = @"<!DOCTYPE p:e [<!ELEMENT p:e (p:x*)><!ATTLIST p:e        xmlns:p CDATA 'foo'><!ELEMENT p:x (p:x*)>]><p:e xmlns:p='bar'><p:x/></p:e>"; break;
                case 2: xml = @"<!DOCTYPE e [<!ELEMENT e (x*)><!ATTLIST e        xmlns CDATA 'foo'><!ELEMENT x (x*)>]><e xmlns='bar'><x/></e>"; break;
                case 3: xml = @"<!DOCTYPE e [<!ELEMENT e (x*)><!ATTLIST e        xmlns CDATA 'foo'><!ELEMENT x (x*)>]><e><x/></e>"; break;
            }

            ReloadSource(new StringReader(xml));
            while (DataReader.Read())
            {
                CError.WriteLine(DataReader.Value);
                if (DataReader.HasAttributes)
                {
                    while (DataReader.MoveToNextAttribute())
                    {
                        CError.WriteLine(DataReader.Value);
                    }
                }
            }
            return TEST_PASS;
        }

        //[Variation("1.DOCTYPE root SYSTEM with valid surr.pair", Param = 1)]
        //[Variation("2.DOCTYPE root SYSTEM with identifier", Param = 2)]
        //[Variation("3.DOCTYPE root PUBLIC with identifier", Param = 3)]
        public int Read51()
        {
            string xml = "";
            switch ((int)CurVariation.Param)
            {
                case 1: xml = "<!DOCTYPE root SYSTEM \"\uD812\uDD12\"><root/>"; break;
                case 2: xml = "<!DOCTYPE root SYSTEM \"#\"><root/>"; break;
                case 3: xml = "<!DOCTYPE root PUBLIC \"\" \"#\"><root/>"; break;
            }
            try
            {
                ReloadSource(new StringReader(xml));
                while (DataReader.Read())
                {
                    CError.WriteLine(DataReader.Value);
                }
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            catch (FileNotFoundException e1) { CError.WriteLine(e1); return TEST_PASS; }
            return TEST_FAIL;
        }

        //[Variation("1.Parsing invalid DOCTYPE", Param = 1)]
        //[Variation("2.Parsing invalid DOCTYPE", Param = 2)]
        //[Variation("3.Parsing invalid DOCTYPE", Param = 3)]
        //[Variation("4.Parsing invalid DOCTYPE", Param = 4)]
        //[Variation("5.Parsing invalid DOCTYPE", Param = 5)]
        //[Variation("6.Parsing invalid DOCTYPE", Param = 6)]
        //[Variation("7.Parsing invalid DOCTYPE", Param = 7)]
        //[Variation("8.Parsing invalid xml version", Param = 8)]
        //[Variation("9.Parsing invalid xml version,DOCTYPE", Param = 9)]
        //[Variation("10.Parsing invalid xml version", Param = 10)]
        //[Variation("11.Parsing invalid xml version", Param = 11)]
        //[Variation("12.Parsing invalid xml version", Param = 12)]
        public int Read53()
        {
            string xml = "";
            switch ((int)CurVariation.Param)
            {
                case 1: xml = "<!DOCTYPE <"; break;
                case 2: xml = "<!DOCTYPE root SYSTEM"; break;
                case 3: xml = "<!DOCTYPE []<root/>"; break;
                case 4: xml = "<!DOCTYPE root PUBLIC >]>"; break;
                case 5: xml = "<!DOCTYPE "; break;
                case 6: xml = "<!DOCTYPE >"; break;
                case 7: xml = "<!DOCTYPE ["; break;
                case 8: xml = " <?xml version=\"1.0\"     ?>"; break;
                case 9: xml = "<?xml version='1.0'                 ?><!DOCTYPE doc [ <!ELEMENT doc ANY >"; break;
                case 10: xml = "< ?xml version=\"1.0\"     ?>"; break;
                case 11: xml = "<? xml version=\"1.0\"     ?>"; break;
                case 12: xml = "<?xml version      =     \"   1.0       \"     ?>"; break;
            }
            try
            {
                ReloadSource(new StringReader(xml));
                while (DataReader.Read()) { CError.WriteLine(DataReader.Value); };
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Parse an XML declaration that will have some whitespace before the closing")]
        public int Read54()
        {
            string xml = @"<?xml version   =   '1.0'                 
?><!DOCTYPE doc [
 <!ELEMENT doc ANY >
 <!ELEMENT a ANY >
	<!ATTLIST a xml:lang CDATA #IMPLIED>
]>
<doc>
   <a xml:lang='de'>
      blar
   </a>
   <a xml:lang='en'>
      blar
   </a>
   <a xml:lang='en-US'>
      blar
   </a>
   <a xml:lang='xxx-en'>
      blar
   </a>
</doc>
";
            ReloadSource(new StringReader(xml));
            while (DataReader.Read())
            {
                object ob = DataReader.Value;
                int d = DataReader.Depth;
                if (DataReader.HasAttributes)
                {
                    while (DataReader.MoveToNextAttribute())
                    {
                        object o = DataReader.Value;
                        int da = DataReader.Depth;
                    }
                }
            }
            return TEST_PASS;
        }

        //[Variation("Parsing xml with DTD and 200 attributes", Param = 1)]
        //[Variation("Parsing xml with DTD and 200 attributes with ns", Param = 2)]
        public int Read55()
        {
            string xml = Path.Combine(TestData, "Common", "Attr200Valid.xml");
            if ((int)CurVariation.Param == 2)
                xml = Path.Combine(TestData, "Common", "Attr200WithNS.xml");
            ReloadSource(xml);
            while (DataReader.Read())
            {
                object ob = DataReader.Value;
                int d = DataReader.Depth;
                if (DataReader.HasAttributes)
                {
                    while (DataReader.MoveToNextAttribute())
                    {
                        object o = DataReader.Value;
                        int da = DataReader.Depth;
                    }
                }
            }
            return TEST_PASS;
        }

        //[Variation("Parsing xml with DTD and 200 attributes and 1 duplicate", Param = 1)]
        //[Variation("Parsing xml with DTD and 200 attributes with ns and 1 duplicate", Param = 2)]
        public int Read56()
        {
            int param = (int)CurVariation.Param;
            string xml = Path.Combine(TestData, "Common", (param == 1) ? "Attr201Invalid.xml" : "Attr201WithNS.xml");
            try
            {
                ReloadSource(xml);
                while (DataReader.Read())
                {
                    object ob = DataReader.Value;
                    int d = DataReader.Depth;
                    if (DataReader.HasAttributes)
                    {
                        while (DataReader.MoveToNextAttribute())
                        {
                            object o = DataReader.Value;
                            int da = DataReader.Depth;
                        }
                    }
                }
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Parse xml with whitespace nodes")]
        public int Read57()
        {
            string xml = @"<?xml version   =   '1.0'" + "\r\n" + @" ?><!DOCTYPE" + "\r" + @"doc " + "\n" + @"[ <!ELEMENT " + "\r\n" + @"doc " + "\r\n" + @"ANY" + "\r\n" + @">
 <!ELEMENT a" + "\r\n" + @"ANY >" + "\r\n" + @"	<!ATTLIST" + "\r\n" + @"a" + "\r\n" + @"xml:lang CDATA #IMPLIED>" + "\r\n" + @"]>" + "\r\n" + @"<doc>" + "\r\n" +
@"   <a " + "\r\n" + @"xml:lang='de'>" + "\r\n" + @"      blar" + "\r\n" + @"   </a>" + "\r\n" + @"   <a xml:lang='\r\n'>" + "\r\n" + @"      blar" + "\r\n"
+ @"   </a>" + "\r\n" + @"   <a xml:lang='\r'>" + "\r\n" + @"      blar   " + "\r\n" + @"</a>   <a xml:lang='\n'>  " + "\r\n" + @"    blar   </a>" + "\r\n" + @"</doc>";
            ReloadSource(new StringReader(xml));
            while (DataReader.Read())
            {
                object ob = DataReader.Value;
                int d = DataReader.Depth;
                if (DataReader.HasAttributes)
                {
                    while (DataReader.MoveToNextAttribute())
                    {
                        object o = DataReader.Value;
                        int da = DataReader.Depth;
                    }
                }
            }
            return TEST_PASS;
        }

        [Variation("Parse xml with whitespace nodes and invalid char")]
        public int Read58()
        {
            string xml = @"<?xml version   =   '1.0'" + "\r\n" + @" ?><!DOCTYPE'" + "\r" + @"doc '" + "\n" + @"[ <!ELEMENT '" + "\r\n" + @"doc '" + "\r\n" + @"ANY'" + "\r\n" + @">
 <!ELEMENT a'" + "\r\n" + @"ANY >'" + "\r\n" + @"	<!ATTLIST'" + "\r\n" + @"a'" + "\r\n" + @"xml:lang CDATA #IMPLIED>'" + "\r\n" + @"]>'" + "\r\n" + @"<doc>'" + "\r\n" +
@"   <a '" + "\r\n" + @"xml:lang='de'>'" + "\r\n" + @"      blar'" + "\r\n" + @"   </a>'" + "\r\n" + @"   <a xml:lang='\r\n'>'" + "\r\n" + @"      blar'" + "\r\n"
+ @"   </a>'" + "\r\n" + @"   <a xml:lang='\r'>'" + "\r\n" + @"      blar   '" + "\r\n" + @"</a>   <a xml:lang='\n'>  '" + "\r\n" + @"    blar   </a>'" + "\r\n" + @"</doc>";
            try
            {
                ReloadSource(new StringReader(xml));
                while (DataReader.Read()) ;
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Parse xml with uri attribute")]
        public int Read59()
        {
            string xml = @"<root xmlns='uri'/>";
            ReloadSource(new StringReader(xml));
            DataReader.Read();
            if (IsBinaryReader()) DataReader.Read();
            CError.Compare(DataReader.MoveToFirstAttribute(), true, "MoveToFirstAttribute");
            CError.Compare(DataReader.LocalName, "xmlns", "LocalName");
            CError.Compare(DataReader.Prefix, "", "Prefix");
            CError.Compare(DataReader.NamespaceURI, "http://www.w3.org/2000/xmlns/", "NamespaceUri");
            CError.Compare(DataReader.Value, "uri", "Value");
            return TEST_PASS;
        }

        [Variation("Parse xml with in ascii encoding")]
        public int Read61()
        {
            string uri = Path.Combine(TestData, "Common", "riversrss.xml");
            ReloadSource(uri);
            while (DataReader.Read()) ;
            return TEST_PASS;
        }

        [Variation("XmlReader doesn't fail when numeric character entity computation overflows")]
        public int Read63()
        {
            try
            {
                string xml = "<root>&#x10000000A;</root>";
                ReloadSource(new StringReader(xml));
                while (DataReader.Read())
                {
                    DataReader.MoveToContent();
                    CError.WriteLine(DataReader.Value);
                }
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        //[Variation("XmlReader should fail on ENTITY name with colons in it", Param = 1)]
        //[Variation("XmlReader should fail on ENTITY name with colons in it", Param = 1)]
        public int Read64()
        {
            int param = (int)CurVariation.Param;
            string xml = (param == 1) ? "<doc>    <?foo:bar 123?></doc>" : "<doc>    &foo:bar;</doc>";

            try
            {
                ReloadSource(new StringReader(xml));
                while (DataReader.Read()) CError.WriteLine(DataReader.Value);
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        //[Variation("1.Parsing xml in gb18030 encoding", Param = 1)]
        //[Variation("2.Parsing xml in UTF32 encoding", Param = 2)]
        public int Read65()
        {
            int param = (int)CurVariation.Param;
            string xml = Path.Combine(TestData, "Common", (param == 1) ? "1_GB18030.xml" : "3_UTF32.xml");
            ReloadSource(xml);
            while (DataReader.Read()) CError.WriteLine(DataReader.Value);
            return TEST_PASS;
        }

        [Variation("Parse input with a character zero 0x00 at root level.")]
        public int Read66()
        {
            string xml = Path.Combine(TestData, "Common", "Bug615675.xml");
            try
            {
                ReloadSource(xml);
                while (DataReader.Read()) CError.WriteLine(DataReader.Value);
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return (IsSubtreeReader() || IsXmlTextReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXmlValidatingReader() || IsXPathNavigatorReader() || IsXsltReader()) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation("1.Parse input with utf-16 encoding", Param = "charset01.xml")]
        //[Variation("3.Parse input with utf-16 encoding", Param = "charset03.xml")]
        public int Read68()
        {
            string xml = Path.Combine(TestData, "Common", "" + (string)CurVariation.Param);
            ReloadSource(xml);
            while (DataReader.Read()) CError.WriteLine(DataReader.Value);
            return TEST_PASS;
        }

        [Variation("2.Parse input with utf-16 encoding", Param = "charset02.xml")]
        public int Read68a()
        {
            string xml = Path.Combine(TestData, "Common", "" + (string)CurVariation.Param);
            try
            {
                ReloadSource(xml);
                while (DataReader.Read()) CError.WriteLine(DataReader.Value);
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        [Variation("Add column position to the exception reported when end tag does not match the start tag")]
        public int Read70()
        {
            string xml = "<doc><elem/></do>";

            try
            {
                ReloadSource(new StringReader(xml));
                while (DataReader.Read()) CError.WriteLine(DataReader.Value);
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        private class OneByteStream : System.IO.Stream
        {
            private byte[] _input;
            private int _pos;

            public OneByteStream(byte[] input)
            {
                _input = input;
                _pos = 0;
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            public override void Flush()
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public override long Length
            {
                get { return _input.Length; }
            }

            public override long Position
            {
                get
                {
                    throw new Exception("The method or operation is not implemented.");
                }
                set
                {
                    throw new Exception("The method or operation is not implemented.");
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int tocopy = count;
                if (tocopy > _input.Length - _pos)
                {
                    tocopy = _input.Length - _pos;
                }
                if (tocopy > 4)
                {
                    tocopy = 4;
                }

                int i;
                for (i = 0; i < tocopy; i++)
                {
                    buffer[offset + i] = _input[_pos + i];
                }
                _pos += i;
                return i;
            }

            public override long Seek(long offset, System.IO.SeekOrigin origin)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public override void SetLength(long value)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }
    }

    [InheritRequired()]
    public abstract partial class TCBufferBoundaries : TCXMLReaderBaseGeneral
    {
        // Just in case this ones required.
        public MemoryStream GetStream(string sTag, string eTag, string content, int val)
        {
            BufferBoundary bb = new BufferBoundary(sTag, eTag, content, val);
            bb.PrepareStream();
            bb.StringAtBufferBoundary();
            bb.FinishStream();
            MemoryStream ms = bb.memoryStream;
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4000", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4088", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4089", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4090", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4091", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4092", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4093", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4096", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4097", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4098", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4099", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4101", "4096" })]
        //[Variation("Test PI Buffer Boundaries with variable byte boundary", Params = new object[] { "4102", "4096" })]
        public int v1()
        {
            int initialVal = Convert.ToInt16(CurVariation.Params[0].ToString());
            int bufferBoundary = Convert.ToInt16(CurVariation.Params[1].ToString());

            if (!(IsCoreReader() || IsBinaryReader() || IsXmlTextReader() || IsXmlValidatingReader()))
            {
                return TEST_SKIPPED;
            }

            BufferBoundary bb = new BufferBoundary(BufferBoundary.START_TAG, BufferBoundary.END_TAG, "a", initialVal);
            bb.PrepareStream();
            bb.bufferBoundaryLength = bufferBoundary;
            bb.StringAtBufferBoundary();
            bb.FinishStream();
            MemoryStream ms = bb.memoryStream;
            ms.Seek(0, SeekOrigin.Begin);

            ReloadSource(ms, "");
            CError.WriteLine("Initial Buffer = " + initialVal);
            CError.WriteLine("Buffer Boundary = " + bufferBoundary);

            while (DataReader.Read())
            {
                if (DataReader.NodeType == XmlNodeType.ProcessingInstruction)
                {
                    CError.WriteLine("PI Name = " + DataReader.Name);
                    bool equal = (DataReader.Value == bb.nodeValue.ToString());
                    if (!equal)
                        return TEST_FAIL;
                }
            }
            return TEST_PASS;
        }
    }
}

