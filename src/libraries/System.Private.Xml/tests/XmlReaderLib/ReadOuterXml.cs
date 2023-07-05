// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    /////////////////////////////////////////////////////////////////////////
    // TestCase ReadOuterXml
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCReadOuterXml : TCXMLReaderBaseGeneral
    {
        // Element names to test ReadOuterXml on
        private static string s_EMP1 = "EMPTY1";
        private static string s_EMP2 = "EMPTY2";
        private static string s_EMP3 = "EMPTY3";
        private static string s_EMP4 = "EMPTY4";
        private static string s_ENT1 = "ENTITY1";
        private static string s_NEMP0 = "NONEMPTY0";
        private static string s_NEMP1 = "NONEMPTY1";
        private static string s_NEMP2 = "NONEMPTY2";
        private static string s_ELEM1 = "CHARS2";
        private static string s_ELEM2 = "SKIP3";
        private static string s_ELEM3 = "CONTENT";
        private static string s_ELEM4 = "COMPLEX";

        // Element names after the ReadOuterXml call
        private static string s_NEXT1 = "COMPLEX";
        private static string s_NEXT2 = "ACT2";
        private static string s_NEXT3 = "CHARS_ELEM1";
        private static string s_NEXT4 = "AFTERSKIP3";
        private static string s_NEXT5 = "TITLE";
        private static string s_NEXT6 = "ENTITY2";
        private static string s_NEXT7 = "DUMMY";

        // Expected strings returned by ReadOuterXml
        private static string s_EXP_EMP1 = "<EMPTY1 />";
        private static string s_EXP_EMP2 = "<EMPTY2 val=\"abc\" />";
        private static string s_EXP_EMP3 = "<EMPTY3></EMPTY3>";
        private static string s_EXP_EMP4 = "<EMPTY4 val=\"abc\"></EMPTY4>";
        private static string s_EXP_NEMP1 = "<NONEMPTY1>ABCDE</NONEMPTY1>";
        private static string s_EXP_NEMP2 = "<NONEMPTY2 val=\"abc\">1234</NONEMPTY2>";
        private static string s_EXP_ELEM1 = "<CHARS2>xxx<MARKUP />yyy</CHARS2>";
        private static string s_EXP_ELEM2 = "<SKIP3><ELEM1 /><ELEM2>xxx yyy</ELEM2><ELEM3 /></SKIP3>";
        private static string s_EXP_ELEM3 = "<CONTENT><e1 a1='a1value' a2='a2value'><e2 a1='a1value' a2='a2value'><e3 a1='a1value' a2='a2value'>leave</e3></e2></e1></CONTENT>";
        private static string s_EXP_ELEM4 = "<COMPLEX>Text<!-- comment --><![CDATA[cdata]]></COMPLEX>";
        private static string s_EXP_ELEM4_XSLT = "<COMPLEX>Text<!-- comment -->cdata</COMPLEX>";
        private static string s_EXP_ENT1_EXPAND_ALL = "<ENTITY1 att1=\"xxx&lt;xxxAxxxCxxxNO_REFERENCEe1;xxx\">xxx&gt;xxxBxxxDxxxNO_REFERENCEe1;xxx</ENTITY1>";
        private static string s_EXP_ENT1_EXPAND_CHAR = "<ENTITY1 att1=\"xxx&lt;xxxAxxxCxxx&e1;xxx\">xxx&gt;xxxBxxxDxxx&e1;xxx</ENTITY1>";

        private int TestOuterOnElement(string strElem, string strOuterXml, string strNextElemName, bool bWhitespace)
        {
            ReloadSource();
            DataReader.PositionOnElement(strElem);

            CError.Compare(DataReader.ReadOuterXml(), strOuterXml, "outer");

            if (bWhitespace)
            {
                if (!(IsXsltReader() || IsXPathNavigatorReader()))// xslt doesn't return whitespace
                {
                    if (IsCoreReader())
                    {
                        CError.Compare(DataReader.VerifyNode(XmlNodeType.Whitespace, string.Empty, "\n"), true, "vn");
                    }
                    else
                    {
                        CError.Compare(DataReader.VerifyNode(XmlNodeType.Whitespace, string.Empty, "\r\n"), true, "vn");
                    }
                    DataReader.Read();
                }
            }
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Element, strNextElemName, string.Empty), true, "vn2");

            return TEST_PASS;
        }

        private int TestOuterOnAttribute(string strElem, string strName, string strValue)
        {
            ReloadSource();
            DataReader.PositionOnElement(strElem);

            DataReader.MoveToAttribute(DataReader.AttributeCount / 2);

            string strExpected = string.Format("{0}=\"{1}\"", strName, strValue);

            CError.Compare(DataReader.ReadOuterXml(), strExpected, "outer");
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, strName, strValue), true, "vn");

            return TEST_PASS;
        }

        private int TestOuterOnNodeType(XmlNodeType nt)
        {
            ReloadSource();
            PositionOnNodeType(nt);
            DataReader.Read();

            XmlNodeType expNt = DataReader.NodeType;
            string expName = DataReader.Name;
            string expValue = DataReader.Value;

            ReloadSource();
            PositionOnNodeType(nt);

            CError.Compare(DataReader.ReadOuterXml(), string.Empty, "outer");
            CError.Compare(DataReader.VerifyNode(expNt, expName, expValue), true, "vn");

            return TEST_PASS;
        }

        ////////////////////////////////////////////////////////////////
        // Variations
        ////////////////////////////////////////////////////////////////

        [Variation("ReadOuterXml on empty element w/o attributes", Pri = 0)]
        public int ReadOuterXml1()
        {
            if (IsBinaryReader())
            {
                return TEST_SKIPPED;
            }
            return TestOuterOnElement(s_EMP1, s_EXP_EMP1, s_EMP2, true);
        }

        [Variation("ReadOuterXml on empty element w/ attributes", Pri = 0)]
        public int ReadOuterXml2()
        {
            if (IsBinaryReader())
            {
                return TEST_SKIPPED;
            }

            return TestOuterOnElement(s_EMP2, s_EXP_EMP2, s_EMP3, true);
        }

        [Variation("ReadOuterXml on full empty element w/o attributes")]
        public int ReadOuterXml3()
        {
            return TestOuterOnElement(s_EMP3, s_EXP_EMP3, s_NEMP0, true);
        }

        [Variation("ReadOuterXml on full empty element w/ attributes")]
        public int ReadOuterXml4()
        {
            return TestOuterOnElement(s_EMP4, s_EXP_EMP4, s_NEXT1, true);
        }

        [Variation("ReadOuterXml on element with text content", Pri = 0)]
        public int ReadOuterXml5()
        {
            return TestOuterOnElement(s_NEMP1, s_EXP_NEMP1, s_NEMP2, true);
        }

        [Variation("ReadOuterXml on element with attributes", Pri = 0)]
        public int ReadOuterXml6()
        {
            return TestOuterOnElement(s_NEMP2, s_EXP_NEMP2, s_NEXT2, true);
        }

        [Variation("ReadOuterXml on element with text and markup content")]
        public int ReadOuterXml7()
        {
            if (IsBinaryReader())
            {
                return TEST_SKIPPED;
            }

            return TestOuterOnElement(s_ELEM1, s_EXP_ELEM1, s_NEXT3, true);
        }

        [Variation("ReadOuterXml with multiple level of elements")]
        public int ReadOuterXml8()
        {
            if (IsBinaryReader())
            {
                return TEST_SKIPPED;
            }

            return TestOuterOnElement(s_ELEM2, s_EXP_ELEM2, s_NEXT4, false);
        }

        [Variation("ReadOuterXml with multiple level of elements, text and attributes", Pri = 0)]
        public int ReadOuterXml9()
        {
            string strExpected = s_EXP_ELEM3;

            strExpected = strExpected.Replace('\'', '"');

            return TestOuterOnElement(s_ELEM3, strExpected, s_NEXT5, true);
        }

        [Variation("ReadOuterXml on element with complex content (CDATA, PIs, Comments)", Pri = 0)]
        public int ReadOuterXml10()
        {
            if (IsXsltReader() || IsXPathNavigatorReader())
                return TestOuterOnElement(s_ELEM4, s_EXP_ELEM4_XSLT, s_NEXT7, true);
            else
                return TestOuterOnElement(s_ELEM4, s_EXP_ELEM4, s_NEXT7, true);
        }

        [Variation("ReadOuterXml on element with entities, EntityHandling = ExpandEntities")]
        public int ReadOuterXml11()
        {
            string strExpected;

            if (IsXsltReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXmlValidatingReader() || IsCoreReader() || IsXPathNavigatorReader())
            {
                strExpected = s_EXP_ENT1_EXPAND_ALL;
            }
            else
            {
                strExpected = s_EXP_ENT1_EXPAND_CHAR;
            }


            return TestOuterOnElement(s_ENT1, strExpected, s_NEXT6, false);
        }

        [Variation("ReadOuterXml on attribute node of empty element")]
        public int ReadOuterXml12()
        {
            return TestOuterOnAttribute(s_EMP2, "val", "abc");
        }

        [Variation("ReadOuterXml on attribute node of full empty element")]
        public int ReadOuterXml13()
        {
            return TestOuterOnAttribute(s_EMP4, "val", "abc");
        }

        [Variation("ReadOuterXml on attribute node", Pri = 0)]
        public int ReadOuterXml14()
        {
            return TestOuterOnAttribute(s_NEMP2, "val", "abc");
        }

        [Variation("ReadOuterXml on attribute with entities, EntityHandling = ExpandEntities", Pri = 0)]
        public int ReadOuterXml15()
        {
            ReloadSource();
            DataReader.PositionOnElement(s_ENT1);

            DataReader.MoveToAttribute(DataReader.AttributeCount / 2);

            string strExpected;
            if (IsXsltReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXmlValidatingReader() || IsCoreReader() || IsXPathNavigatorReader())
                strExpected = "att1=\"xxx&lt;xxxAxxxCxxxNO_REFERENCEe1;xxx\"";
            else
            {
                strExpected = "att1=\"xxx&lt;xxxAxxxCxxx&e1;xxx\"";
            }
            CError.Compare(DataReader.ReadOuterXml(), strExpected, "outer");
            if (IsXmlTextReader())
                CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, "att1", ST_ENT1_ATT_EXPAND_CHAR_ENTITIES), true, "vn");
            else
                CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, "att1", ST_ENT1_ATT_EXPAND_ENTITIES), true, "vn");

            return TEST_PASS;
        }

        [Variation("ReadOuterXml on Comment")]
        public int ReadOuterXml16()
        {
            return TestOuterOnNodeType(XmlNodeType.Comment);
        }

        [Variation("ReadOuterXml on ProcessingInstruction")]
        public int ReadOuterXml17()
        {
            return TestOuterOnNodeType(XmlNodeType.ProcessingInstruction);
        }

        [Variation("ReadOuterXml on DocumentType")]
        public int ReadOuterXml18()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            return TestOuterOnNodeType(XmlNodeType.DocumentType);
        }

        [Variation("ReadOuterXml on XmlDeclaration")]
        public int ReadOuterXml19()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            return TestOuterOnNodeType(XmlNodeType.XmlDeclaration);
        }

        [Variation("ReadOuterXml on EndElement")]
        public int ReadOuterXml20()
        {
            return TestOuterOnNodeType(XmlNodeType.EndElement);
        }

        [Variation("ReadOuterXml on Text")]
        public int ReadOuterXml21()
        {
            return TestOuterOnNodeType(XmlNodeType.Text);
        }

        [Variation("ReadOuterXml on EntityReference")]
        public int ReadOuterXml22()
        {
            if (IsXsltReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            return TestOuterOnNodeType(XmlNodeType.EntityReference);
        }

        [Variation("ReadOuterXml on EndEntity")]
        public int ReadOuterXml23()
        {
            if (IsXmlTextReader() || IsXsltReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            return TestOuterOnNodeType(XmlNodeType.EndEntity);
        }

        [Variation("ReadOuterXml on CDATA")]
        public int ReadOuterXml24()
        {
            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            return TestOuterOnNodeType(XmlNodeType.CDATA);
        }

        [Variation("ReadOuterXml on XmlDeclaration attributes")]
        public int ReadOuterXml25()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();
            DataReader.PositionOnNodeType(XmlNodeType.XmlDeclaration);

            DataReader.MoveToAttribute(DataReader.AttributeCount / 2);

            CError.Compare(DataReader.ReadOuterXml().ToLower(), "encoding=\"utf-8\"", "outer");
            if (IsBinaryReader())
                CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, "encoding", "utf-8"), true, "vn");
            else
                CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, "encoding", "UTF-8"), true, "vn");
            return TEST_PASS;
        }

        [Variation("ReadOuterXml on DocumentType attributes")]
        public int ReadOuterXml26()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();
            DataReader.PositionOnNodeType(XmlNodeType.DocumentType);

            DataReader.MoveToAttribute(DataReader.AttributeCount / 2);

            CError.Compare(DataReader.ReadOuterXml(), "SYSTEM=\"AllNodeTypes.dtd\"", "outer");
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, "SYSTEM", "AllNodeTypes.dtd"), true, "vn");

            return TEST_PASS;
        }

        [Variation("ReadOuterXml on element with entities, EntityHandling = ExpandCharEntities")]
        public int TRReadOuterXml27()
        {
            string strExpected;
            if (IsXsltReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                strExpected = s_EXP_ENT1_EXPAND_ALL;
            else
            {
                if (IsXmlNodeReader())
                {
                    strExpected = s_EXP_ENT1_EXPAND_CHAR;
                }
                else
                {
                    strExpected = s_EXP_ENT1_EXPAND_CHAR;
                }
            }

            ReloadSource();
            DataReader.PositionOnElement(s_ENT1);

            CError.Compare(DataReader.ReadOuterXml(), strExpected, "outer");
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Element, s_NEXT6, string.Empty), true, "vn");

            return TEST_PASS;
        }

        [Variation("ReadOuterXml on attribute with entities, EntityHandling = ExpandCharEntities")]
        public int TRReadOuterXml28()
        {
            string strExpected;
            if (IsXsltReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                strExpected = "att1=\"xxx&lt;xxxAxxxCxxxNO_REFERENCEe1;xxx\"";
            else
            {
                if (IsXmlNodeReader())
                {
                    strExpected = "att1=\"xxx&lt;xxxAxxxCxxxNO_REFERENCEe1;xxx\"";
                }
                else
                {
                    strExpected = "att1=\"xxx&lt;xxxAxxxCxxxNO_REFERENCEe1;xxx\"";
                }
            }

            ReloadSource();
            DataReader.PositionOnElement(s_ENT1);

            DataReader.MoveToAttribute(DataReader.AttributeCount / 2);
            CError.Compare(DataReader.ReadOuterXml(), strExpected, "outer");
            if (IsXmlTextReader())
                CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, "att1", ST_ENT1_ATT_EXPAND_CHAR_ENTITIES), true, "vn");
            else
                CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, "att1", ST_ENT1_ATT_EXPAND_ENTITIES), true, "vn");

            return TEST_PASS;
        }

        [Variation("One large element")]
        public int TestTextReadOuterXml29()
        {
            string strp = "a                                                             ";
            strp += strp;
            strp += strp;
            strp += strp;
            strp += strp;
            strp += strp;
            strp += strp;
            strp += strp;

            string strxml = "<Name a=\"b\">" + strp + " </Name>";
            ReloadSourceStr(strxml);

            DataReader.Read();
            CError.Compare(DataReader.ReadOuterXml(), strxml, "rox");

            return TEST_PASS;
        }

        [Variation("Read OuterXml when Namespaces=false and has an attribute xmlns")]
        public int ReadOuterXmlWhenNamespacesIgnoredWorksWithXmlns()
        {
            ReloadSourceStr("<?xml version='1.0' encoding='utf-8' ?> <foo xmlns='testing'><bar id='1'/></foo>");
            if (IsXmlTextReader() || IsXmlValidatingReader())
                DataReader.Namespaces = false;
            DataReader.MoveToContent();
            CError.WriteLine(DataReader.ReadOuterXml());
            return TEST_PASS;
        }

        [Variation("XmlReader.ReadOuterXml outputs multiple namespace declarations if called within multiple XmlReader.ReadSubtree() calls")]
        public int SubtreeXmlReaderOutputsSingleNamespaceDeclaration()
        {
            string xml = @"<root xmlns = ""http://www.test.com/"">    <device>        <thing>1</thing>    </device></root>";
            ReloadSourceStr(xml);
            DataReader.ReadToFollowing("device");
            Foo(DataReader.ReadSubtree());
            return TEST_PASS;
        }
        private void Foo(XmlReader reader)
        {
            reader.Read();
            Bar(reader.ReadSubtree());
        }
        private void Bar(XmlReader reader)
        {
            reader.Read();
            string foo = reader.ReadOuterXml();
            CError.Compare(foo, "<device xmlns=\"http://www.test.com/\">        <thing>1</thing>    </device>",
                "<device xmlns=\"http://www.test.com/\"><thing>1</thing></device>", "mismatch");
        }
    }
}
