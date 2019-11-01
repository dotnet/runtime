// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // TestCase TCXML ReadState
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract class TCReadState : TCXMLReaderBaseGeneral
    {
        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);
            CreateTestFile(EREADER_TYPE.JUNK);
            return ret;
        }

        public override int Terminate(object objParam)
        {
            // just in case it failed without closing
            DataReader.Close();

            DeleteTestFile(EREADER_TYPE.JUNK);

            return base.Terminate(objParam);
        }

        [Variation("XmlReader ReadState", Pri = 0)]
        public int ReadState1()
        {
            ReloadSource(EREADER_TYPE.JUNK);

            try
            {
                DataReader.Read();
            }
            catch (XmlException)
            {
                CError.WriteLine(DataReader.ReadState);
                CError.Compare(DataReader.ReadState, ReadState.Error, "ReadState should be Error");

                return TEST_PASS;
            }

            throw new CTestException(CTestBase.TEST_FAIL, "wrong exception");
        }
    }

    /////////////////////////////////////////////////////////////////////////
    // TestCase ReadInnerXml
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract class TCReadInnerXml : TCXMLReaderBaseGeneral
    {
        public const string ST_ENT_TEXT = "xxx&gt;xxx&#66;xxx&#x44;xxx&e1;xxx";

        private void VerifyNextNode(XmlNodeType nt, string name, string value)
        {
            while (DataReader.NodeType == XmlNodeType.Whitespace ||
                    DataReader.NodeType == XmlNodeType.SignificantWhitespace)
            {
                // skip all whitespace nodes
                // if EOF is reached NodeType=None
                DataReader.Read();
            }

            CError.Compare(DataReader.VerifyNode(nt, name, value), "VerifyNextNode");
        }

        ////////////////////////////////////////////////////////////////
        // Variations
        ////////////////////////////////////////////////////////////////
        [Variation("ReadInnerXml on Empty Tag", Pri = 0)]
        public int TestReadInnerXml1()
        {
            bool bPassed = false;
            string strExpected = string.Empty;

            ReloadSource();

            DataReader.PositionOnElement("EMPTY1");

            bPassed = CError.Equals(DataReader.ReadInnerXml(), strExpected, CurVariation.Desc);
            VerifyNextNode(XmlNodeType.Element, "EMPTY2", string.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadInnerXml on non Empty Tag", Pri = 0)]
        public int TestReadInnerXml2()
        {
            bool bPassed = false;
            string strExpected = string.Empty;

            ReloadSource();
            DataReader.PositionOnElement("EMPTY2");

            bPassed = CError.Equals(DataReader.ReadInnerXml(), strExpected, CurVariation.Desc);
            VerifyNextNode(XmlNodeType.Element, "EMPTY3", string.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadInnerXml on non Empty Tag with text content", Pri = 0)]
        public int TestReadInnerXml3()
        {
            bool bPassed = false;
            string strExpected = "ABCDE";

            ReloadSource();
            DataReader.PositionOnElement("NONEMPTY1");

            bPassed = CError.Equals(DataReader.ReadInnerXml(), strExpected, CurVariation.Desc);

            VerifyNextNode(XmlNodeType.Element, "NONEMPTY2", string.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadInnerXml on non Empty Tag with Attribute", Pri = 0)]
        public int TestReadInnerXml4()
        {
            bool bPassed = false;
            string strExpected = "1234";

            ReloadSource();
            DataReader.PositionOnElement("NONEMPTY2");
            bPassed = CError.Equals(DataReader.ReadInnerXml(), strExpected, CurVariation.Desc);
            VerifyNextNode(XmlNodeType.Element, "ACT2", string.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadInnerXml on non Empty Tag with text and markup content (mixed content)")]
        public int TestReadInnerXml5()
        {
            bool bPassed = false;
            string strExpected;
            strExpected = "xxx<MARKUP />yyy";

            ReloadSource();
            DataReader.PositionOnElement("CHARS2");

            bPassed = CError.Equals(DataReader.ReadInnerXml(), strExpected, CurVariation.Desc);
            VerifyNextNode(XmlNodeType.Element, "CHARS_ELEM1", string.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadInnerXml with multiple Level of elements")]
        public int TestReadInnerXml6()
        {
            bool bPassed = false;
            string strExpected;
            strExpected = "<ELEM1 /><ELEM2>xxx yyy</ELEM2><ELEM3 />";

            ReloadSource();
            DataReader.PositionOnElement("SKIP3");

            bPassed = CError.Equals(DataReader.ReadInnerXml(), strExpected, CurVariation.Desc);
            VerifyNextNode(XmlNodeType.Element, "AFTERSKIP3", string.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadInnerXml with multiple Level of elements, text and attributes", Pri = 0)]
        public int TestReadInnerXml7()
        {
            bool bPassed = false;
            string strExpected = "<e1 a1='a1value' a2='a2value'><e2 a1='a1value' a2='a2value'><e3 a1='a1value' a2='a2value'>leave</e3></e2></e1>";

            strExpected = strExpected.Replace('\'', '"');

            ReloadSource();
            DataReader.PositionOnElement("CONTENT");

            bPassed = CError.Equals(DataReader.ReadInnerXml(), strExpected, CurVariation.Desc);
            VerifyNextNode(XmlNodeType.Element, "TITLE", string.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadInnerXml with entity references, EntityHandling = ExpandEntities")]
        public int TestReadInnerXml8()
        {
            bool bPassed = false;

            string strExpected = ST_EXPAND_ENTITIES3;

            if (IsXsltReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXmlValidatingReader() || IsCoreReader() || IsXPathNavigatorReader())
                strExpected = ST_EXPAND_ENTITIES2;

            ReloadSource();
            DataReader.PositionOnElement(ST_ENTTEST_NAME);

            bPassed = CError.Equals(DataReader.ReadInnerXml(), strExpected, CurVariation.Desc);
            VerifyNextNode(XmlNodeType.Element, "ENTITY2", string.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadInnerXml on attribute node", Pri = 0)]
        public int TestReadInnerXml9()
        {
            bool bPassed = false;
            string strExpected = "a1value";

            ReloadSource();
            DataReader.PositionOnElement("ATTRIBUTE2");
            bPassed = DataReader.MoveToFirstAttribute();

            bPassed = CError.Equals(DataReader.ReadInnerXml(), strExpected, CurVariation.Desc);
            VerifyNextNode(XmlNodeType.Attribute, "a1", strExpected);

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadInnerXml on attribute node with entity reference in value", Pri = 0)]
        public int TestReadInnerXml10()
        {
            bool bPassed = false;
            string strExpected;
            if (IsXsltReader() || IsXmlNodeReaderDataDoc() || IsXmlValidatingReader() || IsCoreReader() || IsXPathNavigatorReader())
            {
                strExpected = ST_ENT1_ATT_EXPAND_CHAR_ENTITIES4;
            }
            else if (IsXmlNodeReader())
            {
                strExpected = ST_ENT1_ATT_EXPAND_CHAR_ENTITIES2;
            }
            else
            {
                strExpected = ST_ENT1_ATT_EXPAND_CHAR_ENTITIES2;
            }

            string strExpectedAttValue = ST_ENT1_ATT_EXPAND_ENTITIES;
            if (IsXmlTextReader())
                strExpectedAttValue = ST_ENT1_ATT_EXPAND_CHAR_ENTITIES;

            ReloadSource();
            DataReader.PositionOnElement(ST_ENTTEST_NAME);
            bPassed = DataReader.MoveToFirstAttribute();

            bPassed = CError.Equals(DataReader.ReadInnerXml(), strExpected, CurVariation.Desc);

            VerifyNextNode(XmlNodeType.Attribute, "att1", strExpectedAttValue);

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadInnerXml on Text", Pri = 0)]
        public int TestReadInnerXml11()
        {
            XmlNodeType nt;
            string name;
            string value;

            ReloadSource();
            DataReader.PositionOnNodeType(XmlNodeType.Text);
            CError.Compare(DataReader.ReadInnerXml(), string.Empty, CurVariation.Desc);

            // save status and compare with Read
            nt = DataReader.NodeType;
            name = DataReader.Name;
            value = DataReader.Value;

            ReloadSource();
            DataReader.PositionOnNodeType(XmlNodeType.Text);
            DataReader.Read();
            CError.Compare(DataReader.VerifyNode(nt, name, value), "vn");

            return TEST_PASS;
        }

        [Variation("ReadInnerXml on CDATA")]
        public int TestReadInnerXml12()
        {
            if (IsXsltReader() || IsXPathNavigatorReader())
            {
                while (FindNodeType(XmlNodeType.CDATA) == TEST_PASS)
                    return TEST_FAIL;
                return TEST_PASS;
            }

            ReloadSource();
            if (FindNodeType(XmlNodeType.CDATA) == TEST_PASS)
            {
                CError.Compare(DataReader.ReadInnerXml(), string.Empty, CurVariation.Desc);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("ReadInnerXml on ProcessingInstruction")]
        public int TestReadInnerXml13()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.ProcessingInstruction) == TEST_PASS)
            {
                CError.Compare(DataReader.ReadInnerXml(), string.Empty, CurVariation.Desc);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("ReadInnerXml on Comment")]
        public int TestReadInnerXml14()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.Comment) == TEST_PASS)
            {
                CError.Compare(DataReader.ReadInnerXml(), string.Empty, CurVariation.Desc);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("ReadInnerXml on EndElement")]
        public int TestReadInnerXml16()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.EndElement) == TEST_PASS)
            {
                CError.Compare(DataReader.ReadInnerXml(), string.Empty, CurVariation.Desc);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("ReadInnerXml on XmlDeclaration")]
        public int TestReadInnerXml17()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) == TEST_PASS)
            {
                CError.Compare(DataReader.ReadInnerXml(), string.Empty, CurVariation.Desc);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("Current node after ReadInnerXml on element", Pri = 0)]
        public int TestReadInnerXml18()
        {
            ReloadSource();

            DataReader.PositionOnElement("SKIP2");

            DataReader.ReadInnerXml();
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Element, "AFTERSKIP2", string.Empty), true, "VN");

            return TEST_PASS;
        }

        [Variation("Current node after ReadInnerXml on element")]
        public int TestReadInnerXml19()
        {
            ReloadSource();

            DataReader.PositionOnElement("MARKUP");

            CError.Compare(DataReader.ReadInnerXml(), string.Empty, "RIX");
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Text, string.Empty, "yyy"), true, "VN");

            return TEST_PASS;
        }

        [Variation("ReadInnerXml with entity references, EntityHandling = ExpandCharEntites")]
        public int TestTextReadInnerXml2()
        {
            bool bPassed = false;
            string strExpected;
            if (IsXsltReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                strExpected = ST_EXPAND_ENTITIES2;
            else
            {
                if (IsXmlNodeReader())
                {
                    strExpected = ST_EXPAND_ENTITIES3;
                }
                else
                {
                    strExpected = ST_EXPAND_ENTITIES3;
                }
            }

            ReloadSource();
            DataReader.PositionOnElement(ST_ENTTEST_NAME);

            bPassed = CError.Equals(DataReader.ReadInnerXml(), strExpected, CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadInnerXml on EntityReference")]
        public int TestTextReadInnerXml4()
        {
            if (IsXsltReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.EntityReference);

            CError.Compare(DataReader.ReadInnerXml(), string.Empty, CurVariation.Desc);

            return TEST_PASS;
        }

        [Variation("ReadInnerXml on EndEntity")]
        public int TestTextReadInnerXml5()
        {
            if (IsXmlTextReader() || IsXsltReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.EntityReference);

            CError.Compare(DataReader.ReadInnerXml(), string.Empty, CurVariation.Desc);

            return TEST_PASS;
        }

        [Variation("ReadInnerXml on XmlDeclaration attributes")]
        public int TestTextReadInnerXml16()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();
            DataReader.PositionOnNodeType(XmlNodeType.XmlDeclaration);
            DataReader.MoveToAttribute(DataReader.AttributeCount / 2);

            if (IsBinaryReader())
            {
                CError.Compare(DataReader.ReadInnerXml(), "utf-8", "inner");
                CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, "encoding", "utf-8"), true, "vn");
            }
            else
            {
                CError.Compare(DataReader.ReadInnerXml(), "UTF-8", "inner");
                CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, "encoding", "UTF-8"), true, "vn");
            }

            return TEST_PASS;
        }

        [Variation("One large element")]
        public int TestTextReadInnerXml18()
        {
            string strp = "a                                                             ";
            strp += strp;
            strp += strp;
            strp += strp;
            strp += strp;
            strp += strp;
            strp += strp;
            strp += strp;

            string strxml = "<Name a=\"b\">" + strp + "</Name>";
            ReloadSourceStr(strxml);

            DataReader.Read();
            CError.Compare(DataReader.ReadInnerXml(), strp, "rix");

            return TEST_PASS;
        }
    }

    /////////////////////////////////////////////////////////////////////////
    // TestCase MoveToContent
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCMoveToContent : TCXMLReaderBaseGeneral
    {
        public const string ST_TEST_NAME1 = "GOTOCONTENT";
        public const string ST_TEST_NAME2 = "SKIPCONTENT";
        public const string ST_TEST_NAME3 = "MIXCONTENT";

        public const string ST_TEST_TEXT = "some text";
        public const string ST_TEST_CDATA = "cdata info";

        ////////////////////////////////////////////////////////////////
        // Variations
        ////////////////////////////////////////////////////////////////
        [Variation("MoveToContent on Skip XmlDeclaration", Pri = 0)]
        public int TestMoveToContent1()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();

            PositionOnNodeType(XmlNodeType.XmlDeclaration);
            CError.Compare(DataReader.MoveToContent(), XmlNodeType.Element, CurVariation.Desc);
            CError.Compare(DataReader.Name, "PLAY", CurVariation.Desc);

            return TEST_PASS;
        }

        [Variation("MoveToContent on Read through All invalid Content Node(PI, Comment and whitespace)", Pri = 0)]
        public int TestMoveToContent3()
        {
            ReloadSource();

            DataReader.PositionOnElement(ST_TEST_NAME2);
            CError.Compare(DataReader.MoveToContent(), XmlNodeType.Element, CurVariation.Desc);
            CError.Compare(DataReader.Name, ST_TEST_NAME2, "Element name");

            DataReader.Read();
            CError.Compare(DataReader.MoveToContent(), XmlNodeType.EndElement, "Move to EndElement");
            CError.Compare(DataReader.Name, ST_TEST_NAME2, "EndElement value");
            return TEST_PASS;
        }

        [Variation("MoveToContent on Attribute", Pri = 0)]
        public int TestMoveToContent5()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_NAME1);
            PositionOnNodeType(XmlNodeType.Attribute);

            CError.Compare(DataReader.MoveToContent(), XmlNodeType.Element, "Move to EndElement");
            CError.Compare(DataReader.Name, ST_TEST_NAME2, "EndElement value");
            return TEST_PASS;
        }

        public const string ST_ENT1_ELEM_ALL_ENTITIES_TYPE = "xxxBxxxDxxxe1fooxxx";

        public const string ST_ATT1_NAME = "att1";
        public const string ST_GEN_ENT_NAME = "e1";
        public const string ST_GEN_ENT_VALUE = "e1foo";
    }

    /////////////////////////////////////////////////////////////////////////
    // TestCase IsStartElement
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCIsStartElement : TCXMLReaderBaseGeneral
    {
        private const string ST_TEST_ELEM = "DOCNAMESPACE";
        private const string ST_TEST_EMPTY_ELEM = "NOSPACE";
        private const string ST_TEST_ELEM_NS = "NAMESPACE1";
        private const string ST_TEST_EMPTY_ELEM_NS = "EMPTY_NAMESPACE1";

        [Variation("IsStartElement on Regular Element, no namespace", Pri = 0)]
        public int TestIsStartElement1()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM);

            CError.Compare(DataReader.IsStartElement(), true, "IsStartElement()");
            CError.Compare(DataReader.IsStartElement(ST_TEST_ELEM), true, "IsStartElement(n)");
            CError.Compare(DataReader.IsStartElement(ST_TEST_ELEM, string.Empty), true, "IsStartElement(n,ns)");

            return TEST_PASS;
        }

        [Variation("IsStartElement on Empty Element, no namespace", Pri = 0)]
        public int TestIsStartElement2()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM);

            CError.Compare(DataReader.IsStartElement(), true, "IsStartElement()");
            CError.Compare(DataReader.IsStartElement(ST_TEST_EMPTY_ELEM), true, "IsStartElement(n)");
            CError.Compare(DataReader.IsStartElement(ST_TEST_EMPTY_ELEM, string.Empty), true, "IsStartElement(n,ns)");
            return TEST_PASS;
        }

        [Variation("IsStartElement on regular Element, with namespace", Pri = 0)]
        public int TestIsStartElement3()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM_NS);
            DataReader.PositionOnElement("bar:check");
            CError.WriteLine(DataReader.NamespaceURI);

            CError.Compare(DataReader.IsStartElement(), true, "IsStartElement()");
            CError.Compare(DataReader.IsStartElement("check", "1"), true, "IsStartElement(n,ns)");
            CError.Compare(DataReader.IsStartElement("check", string.Empty), false, "IsStartElement(n)");
            CError.Compare(DataReader.IsStartElement("check"), false, "IsStartElement2(n)");
            CError.Compare(DataReader.IsStartElement("bar:check"), true, "IsStartElement(qname)");
            CError.Compare(DataReader.IsStartElement("bar1:check"), false, "IsStartElement(invalid_qname)");
            return TEST_PASS;
        }

        [Variation("IsStartElement on Empty Tag, with default namespace", Pri = 0)]
        public int TestIsStartElement4()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM_NS);

            CError.Compare(DataReader.IsStartElement(), true, "IsStartElement()");
            CError.Compare(DataReader.IsStartElement(ST_TEST_EMPTY_ELEM_NS, "14"), true, "IsStartElement(n,ns)");
            CError.Compare(DataReader.IsStartElement(ST_TEST_EMPTY_ELEM_NS, string.Empty), false, "IsStartElement(n)");
            CError.Compare(DataReader.IsStartElement(ST_TEST_EMPTY_ELEM_NS), true, "IsStartElement2(n)");

            return TEST_PASS;
        }

        [Variation("IsStartElement with Name=String.Empty")]
        public int TestIsStartElement5()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM);
            CError.Compare(DataReader.IsStartElement(string.Empty), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("IsStartElement on Empty Element with Name and Namespace=String.Empty")]
        public int TestIsStartElement6()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM);
            CError.Compare(DataReader.IsStartElement(string.Empty, string.Empty), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("IsStartElement on CDATA")]
        public int TestIsStartElement7()
        {
            //XSLT doesn't deal with CDATA
            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.CDATA);
            CError.Compare(DataReader.IsStartElement(), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("IsStartElement on EndElement, no namespace")]
        public int TestIsStartElement8()
        {
            ReloadSource();
            DataReader.PositionOnElement("NONAMESPACE");
            PositionOnNodeType(XmlNodeType.EndElement);
            CError.Compare(DataReader.IsStartElement(), false, "IsStartElement()");
            CError.Compare(DataReader.IsStartElement("NONAMESPACE"), false, "IsStartElement(n)");
            CError.Compare(DataReader.IsStartElement("NONAMESPACE", string.Empty), false, "IsStartElement(n,ns)");
            return TEST_PASS;
        }

        [Variation("IsStartElement on EndElement, with namespace")]
        public int TestIsStartElement9()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM_NS);
            DataReader.PositionOnElement("bar:check");
            CError.WriteLine(DataReader.NamespaceURI);
            PositionOnNodeType(XmlNodeType.EndElement);

            CError.Compare(DataReader.IsStartElement(), false, "IsStartElement()");
            CError.Compare(DataReader.IsStartElement("check", "1"), false, "IsStartElement(n,ns)");
            CError.Compare(DataReader.IsStartElement("bar:check"), false, "IsStartElement(qname)");
            return TEST_PASS;
        }

        [Variation("IsStartElement on Attribute")]
        public int TestIsStartElement10()
        {
            ReloadSource();
            PositionOnNodeType(XmlNodeType.Attribute);
            CError.Compare(DataReader.IsStartElement(), true, CurVariation.Desc);
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("IsStartElement on Text")]
        public int TestIsStartElement11()
        {
            ReloadSource();
            PositionOnNodeType(XmlNodeType.Text);
            CError.Compare(DataReader.IsStartElement(), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("IsStartElement on ProcessingInstruction")]
        public int TestIsStartElement12()
        {
            ReloadSource();
            PositionOnNodeType(XmlNodeType.ProcessingInstruction);
            CError.Compare(DataReader.IsStartElement(), IsSubtreeReader() ? false : true, CurVariation.Desc);
            CError.Compare(DataReader.NodeType, IsSubtreeReader() ? XmlNodeType.Text : XmlNodeType.Element, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("IsStartElement on Comment")]
        public int TestIsStartElement13()
        {
            ReloadSource();
            PositionOnNodeType(XmlNodeType.Comment);
            CError.Compare(DataReader.IsStartElement(), IsSubtreeReader() ? false : true, CurVariation.Desc);
            CError.Compare(DataReader.NodeType, IsSubtreeReader() ? XmlNodeType.Text : XmlNodeType.Element, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("IsStartElement on XmlDeclaration")]
        public int TestIsStartElement15()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.XmlDeclaration);
            CError.Compare(DataReader.IsStartElement(), true, CurVariation.Desc);
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("IsStartElement on EntityReference")]
        public int TestTextIsStartElement1()
        {
            if (IsXsltReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.EntityReference);
            CError.Compare(DataReader.IsStartElement(), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("IsStartElement on EndEntity")]
        public int TestTextIsStartElement2()
        {
            if (IsXsltReader() || IsXmlTextReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.EndEntity);
            CError.Compare(DataReader.IsStartElement(), false, CurVariation.Desc);
            return TEST_PASS;
        }
    }

    /////////////////////////////////////////////////////////////////////////
    // TestCase ReadStartElement
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCReadStartElement : TCXMLReaderBaseGeneral
    {
        private const string ST_TEST_ELEM = "DOCNAMESPACE";
        private const string ST_TEST_EMPTY_ELEM = "NOSPACE";
        private const string ST_TEST_ELEM_NS = "NAMESPACE1";
        private const string ST_TEST_EMPTY_ELEM_NS = "EMPTY_NAMESPACE1";

        [Variation("ReadStartElement on Regular Element, no namespace", Pri = 0)]
        public int TestReadStartElement1()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM);
            DataReader.ReadStartElement();

            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM);
            DataReader.ReadStartElement(ST_TEST_ELEM);

            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM);
            DataReader.ReadStartElement(ST_TEST_ELEM, string.Empty);
            return TEST_PASS;
        }

        [Variation("ReadStartElement on Empty Element, no namespace", Pri = 0)]
        public int TestReadStartElement2()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM);
            DataReader.ReadStartElement();

            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM);
            DataReader.ReadStartElement(ST_TEST_EMPTY_ELEM);

            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM);
            DataReader.ReadStartElement(ST_TEST_EMPTY_ELEM, string.Empty);
            return TEST_PASS;
        }

        [Variation("ReadStartElement on regular Element, with namespace", Pri = 0)]
        public int TestReadStartElement3()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM_NS);
            DataReader.PositionOnElement("bar:check");
            CError.WriteLine(DataReader.NamespaceURI);
            DataReader.ReadStartElement();

            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM_NS);
            DataReader.PositionOnElement("bar:check");
            CError.WriteLine(DataReader.NamespaceURI);
            DataReader.ReadStartElement("check", "1");

            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM_NS);
            DataReader.PositionOnElement("bar:check");
            DataReader.ReadStartElement("bar:check");
            return TEST_PASS;
        }

        [Variation("Passing ns=String.EmptyErrorCase: ReadStartElement on regular Element, with namespace", Pri = 0)]
        public int TestReadStartElement4()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM_NS);
            DataReader.PositionOnElement("bar:check");
            CError.WriteLine(DataReader.NamespaceURI);

            try
            {
                DataReader.ReadStartElement("check", string.Empty);
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("Passing no ns: ReadStartElement on regular Element, with namespace", Pri = 0)]
        public int TestReadStartElement5()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM_NS);
            DataReader.PositionOnElement("bar:check");
            CError.WriteLine(DataReader.NamespaceURI);

            try
            {
                DataReader.ReadStartElement("check");
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadStartElement on Empty Tag, with namespace")]
        public int TestReadStartElement6()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM_NS);
            DataReader.ReadStartElement();

            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM_NS);
            DataReader.ReadStartElement(ST_TEST_EMPTY_ELEM_NS, "14");

            return TEST_PASS;
        }

        [Variation("ErrorCase: ReadStartElement on Empty Tag, with namespace, passing ns=String.Empty")]
        public int TestReadStartElement7()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM_NS);

            try
            {
                DataReader.ReadStartElement(ST_TEST_EMPTY_ELEM_NS, string.Empty);
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadStartElement on Empty Tag, with namespace, passing no ns")]
        public int TestReadStartElement8()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM_NS);

            DataReader.ReadStartElement(ST_TEST_EMPTY_ELEM_NS);
            return TEST_PASS;
        }

        [Variation("ReadStartElement with Name=String.Empty")]
        public int TestReadStartElement9()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM);
            try
            {
                DataReader.ReadStartElement(string.Empty);
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadStartElement on Empty Element with Name and Namespace=String.Empty")]
        public int TestReadStartElement10()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM_NS);
            try
            {
                DataReader.ReadStartElement(string.Empty, string.Empty);
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadStartElement on CDATA")]
        public int TestReadStartElement11()
        {
            //XSLT doesn't deal with CDATA
            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.CDATA);
            try
            {
                DataReader.ReadStartElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadStartElement() on EndElement, no namespace")]
        public int TestReadStartElement12()
        {
            ReloadSource();
            DataReader.PositionOnElement("NONAMESPACE");
            PositionOnNodeType(XmlNodeType.EndElement);
            try
            {
                DataReader.ReadStartElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadStartElement(n) on EndElement, no namespace")]
        public int TestReadStartElement13()
        {
            ReloadSource();
            DataReader.PositionOnElement("NONAMESPACE");
            PositionOnNodeType(XmlNodeType.EndElement);
            try
            {
                DataReader.ReadStartElement("NONAMESPACE");
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadStartElement(n, String.Empty) on EndElement, no namespace")]
        public int TestReadStartElement14()
        {
            ReloadSource();
            DataReader.PositionOnElement("NONAMESPACE");
            PositionOnNodeType(XmlNodeType.EndElement);
            try
            {
                DataReader.ReadStartElement("NONAMESPACE", string.Empty);
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadStartElement() on EndElement, with namespace")]
        public int TestReadStartElement15()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM_NS);
            DataReader.PositionOnElement("bar:check");
            CError.WriteLine(DataReader.NamespaceURI);
            PositionOnNodeType(XmlNodeType.EndElement);

            try
            {
                DataReader.ReadStartElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadStartElement(n,ns) on EndElement, with namespace")]
        public int TestReadStartElement16()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM_NS);
            DataReader.PositionOnElement("bar:check");
            CError.WriteLine(DataReader.NamespaceURI);
            PositionOnNodeType(XmlNodeType.EndElement);

            try
            {
                DataReader.ReadStartElement("check", "1");
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }
    }

    /////////////////////////////////////////////////////////////////////////
    // TestCase ReadEndElement
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCReadEndElement : TCXMLReaderBaseGeneral
    {
        private const string ST_TEST_ELEM = "DOCNAMESPACE";
        private const string ST_TEST_EMPTY_ELEM = "NOSPACE";
        private const string ST_TEST_ELEM_NS = "NAMESPACE1";
        private const string ST_TEST_EMPTY_ELEM_NS = "EMPTY_NAMESPACE1";

        [Variation("ReadEndElement() on EndElement, no namespace", Pri = 0)]
        public int TestReadEndElement1()
        {
            ReloadSource();
            DataReader.PositionOnElement("NONAMESPACE");
            PositionOnNodeType(XmlNodeType.EndElement);
            DataReader.ReadEndElement();
            DataReader.VerifyNode(XmlNodeType.EndElement, "NONAMESPACE", string.Empty);
            return TEST_PASS;
        }

        [Variation("ReadEndElement() on EndElement, with namespace", Pri = 0)]
        public int TestReadEndElement2()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM_NS);
            DataReader.PositionOnElement("bar:check");
            CError.WriteLine(DataReader.NamespaceURI);
            PositionOnNodeType(XmlNodeType.EndElement);
            DataReader.ReadEndElement();
            DataReader.VerifyNode(XmlNodeType.EndElement, "bar:check", string.Empty);
            return TEST_PASS;
        }

        [Variation("ReadEndElement on Start Element, no namespace")]
        public int TestReadEndElement3()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM);
            try
            {
                DataReader.ReadEndElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadEndElement on Empty Element, no namespace", Pri = 0)]
        public int TestReadEndElement4()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM);
            try
            {
                DataReader.ReadEndElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadEndElement on regular Element, with namespace", Pri = 0)]
        public int TestReadEndElement5()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_ELEM_NS);
            DataReader.PositionOnElement("bar:check");
            CError.WriteLine(DataReader.NamespaceURI);
            try
            {
                DataReader.ReadEndElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadEndElement on Empty Tag, with namespace", Pri = 0)]
        public int TestReadEndElement6()
        {
            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_EMPTY_ELEM_NS);
            try
            {
                DataReader.ReadEndElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadEndElement on CDATA")]
        public int TestReadEndElement7()
        {
            //XSLT doesn't deal with CDATA
            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.CDATA);
            try
            {
                DataReader.ReadEndElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadEndElement on Text")]
        public int TestReadEndElement9()
        {
            ReloadSource();
            PositionOnNodeType(XmlNodeType.Text);
            try
            {
                DataReader.ReadEndElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadEndElement on ProcessingInstruction")]
        public int TestReadEndElement10()
        {
            ReloadSource();
            PositionOnNodeType(XmlNodeType.ProcessingInstruction);
            try
            {
                DataReader.ReadEndElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadEndElement on Comment")]
        public int TestReadEndElement11()
        {
            ReloadSource();
            PositionOnNodeType(XmlNodeType.Comment);
            try
            {
                DataReader.ReadEndElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadEndElement on XmlDeclaration")]
        public int TestReadEndElement13()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.XmlDeclaration);
            try
            {
                DataReader.ReadEndElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadEndElement on EntityReference")]
        public int TestTextReadEndElement1()
        {
            if (IsXsltReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.EntityReference);
            try
            {
                DataReader.ReadEndElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadEndElement on EndEntity")]
        public int TestTextReadEndElement2()
        {
            if (IsXsltReader() || IsXmlTextReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.EndEntity);
            try
            {
                DataReader.ReadEndElement();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML MoveToElement
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCMoveToElement : TCXMLReaderBaseGeneral
    {
        [Variation("Attribute node")]
        public int v1()
        {
            ReloadSource();

            DataReader.PositionOnElement("elem2");
            DataReader.MoveToAttribute(1);
            CError.Compare(DataReader.MoveToElement(), "MTE on elem2 failed");
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Element, "elem2", string.Empty), "MTE moved on wrong node");
            return TEST_PASS;
        }

        [Variation("Element node")]
        public int v2()
        {
            ReloadSource();

            DataReader.PositionOnElement("elem2");
            CError.Compare(!DataReader.MoveToElement(), "MTE on elem2 failed");
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Element, "elem2", string.Empty), "MTE moved on wrong node");
            return TEST_PASS;
        }

        [Variation("XmlDeclaration node")]
        public int v3()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();

            DataReader.PositionOnNodeType(XmlNodeType.XmlDeclaration);
            CError.Compare(!DataReader.MoveToElement(), "MTE on xmldecl failed");
            CError.Compare(DataReader.NodeType, XmlNodeType.XmlDeclaration, "xmldecl");
            CError.Compare(DataReader.Name, "xml", "Name");
            if (IsBinaryReader())
                CError.Compare(DataReader.Value, "version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"", "Value");
            else
                CError.Compare(DataReader.Value, "version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"", "Value");


            return TEST_PASS;
        }

        [Variation("Comment node")]
        public int v5()
        {
            ReloadSource();

            DataReader.PositionOnNodeType(XmlNodeType.Comment);
            CError.Compare(!DataReader.MoveToElement(), "MTE on comment failed");
            CError.Compare(DataReader.NodeType, XmlNodeType.Comment, "comment");
            return TEST_PASS;
        }
    }
}
