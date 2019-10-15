// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // TestCase TCXML ResolveEntity and ReadAttributeValue
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCResolveEntity : TCXMLReaderBaseGeneral
    {
        [Variation("ResolveEntity On None")]
        public int TestResolveEntityNodeType_None()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.None) == TEST_PASS)
            {
                try
                {
                    DataReader.ResolveEntity();
                }
                catch (InvalidOperationException)
                {
                    goto next;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);

            next: bool b = DataReader.ReadAttributeValue();
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "ResolveEntity returns True");
            }
            return TEST_FAIL;
        }

        [Variation("ResolveEntity On Element")]
        public int TestResolveEntityNodeType_Element()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.Element) == TEST_PASS)
            {
                try
                {
                    DataReader.ResolveEntity();
                }
                catch (InvalidOperationException)
                {
                    goto next;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);

            next: bool b = DataReader.ReadAttributeValue();
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "ResolveEntity returns True");
            }
            return TEST_FAIL;
        }

        [Variation("ResolveEntity On Attribute")]
        public int TestResolveEntityNodeType_Attribute()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.Attribute) == TEST_PASS)
            {
                try
                {
                    DataReader.ResolveEntity();
                }
                catch (InvalidOperationException)
                {
                    goto next;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);

            next: bool b = DataReader.ReadAttributeValue();
                if (b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "ResolveEntity for Attribute returns true");
            }
            return TEST_FAIL;
        }

        [Variation("ResolveEntity On Text")]
        public int TestResolveEntityNodeType_Text()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.Text) == TEST_PASS)
            {
                try
                {
                    DataReader.ResolveEntity();
                }
                catch (InvalidOperationException)
                {
                    goto next;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);

            next: bool b = DataReader.ReadAttributeValue();
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "ResolveEntity for Text returns true");
            }
            return TEST_FAIL;
        }

        [Variation("ResolveEntity On CDATA")]
        public int TestResolveEntityNodeType_CDATA()
        {
            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            if (FindNodeType(XmlNodeType.CDATA) == TEST_PASS)
            {
                try
                {
                    DataReader.ResolveEntity();
                }
                catch (InvalidOperationException)
                {
                    goto next;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);

            next: bool b = DataReader.ReadAttributeValue();
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "ResolveEntity for CDATA returns true");
            }
            return TEST_FAIL;
        }

        [Variation("ResolveEntity On ProcessingInstruction")]
        public int TestResolveEntityNodeType_ProcessingInstruction()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.ProcessingInstruction) == TEST_PASS)
            {
                try
                {
                    DataReader.ResolveEntity();
                }
                catch (InvalidOperationException)
                {
                    goto next;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);

            next: bool b = DataReader.ReadAttributeValue();
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "ResolveEntity for PI returns true");
            }
            return TEST_FAIL;
        }

        [Variation("ResolveEntity On Comment")]
        public int TestResolveEntityNodeType_Comment()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.Comment) == TEST_PASS)
            {
                try
                {
                    DataReader.ResolveEntity();
                }
                catch (InvalidOperationException)
                {
                    goto next;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);

            next: bool b = DataReader.ReadAttributeValue();
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "ResolveEntity for Comment returns true");
            }
            return TEST_FAIL;
        }

        [Variation("ResolveEntity On Whitespace")]
        public int TestResolveEntityNodeType_Whitespace()
        {
            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            if (FindNodeType(XmlNodeType.Whitespace) == TEST_PASS)
            {
                try
                {
                    DataReader.ResolveEntity();
                }
                catch (InvalidOperationException)
                {
                    goto next;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);

            next: bool b = DataReader.ReadAttributeValue();
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "ResolveEntity returns true");
            }
            return TEST_FAIL;
        }

        [Variation("ResolveEntity On EndElement")]
        public int TestResolveEntityNodeType_EndElement()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.EndElement) == TEST_PASS)
            {
                try
                {
                    DataReader.ResolveEntity();
                }
                catch (InvalidOperationException)
                {
                    goto next;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);

            next: bool b = DataReader.ReadAttributeValue();
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "ResolveEntity returns True");
            }
            return TEST_FAIL;
        }

        [Variation("ResolveEntity On XmlDeclaration")]
        public int TestResolveEntityNodeType_XmlDeclaration()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) == TEST_PASS)
            {
                try
                {
                    DataReader.ResolveEntity();
                }
                catch (InvalidOperationException)
                {
                    goto next;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);

            next: bool b = DataReader.ReadAttributeValue();
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "ResolveEntity returns True");
            }
            return TEST_FAIL;
        }

        [Variation("ResolveEntity On EndEntity")]
        public int TestResolveEntityNodeType_EndEntity()
        {
            if (IsXsltReader() || IsXmlTextReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            if (FindNodeType(XmlNodeType.EndEntity) == TEST_PASS)
            {
                try
                {
                    CError.WriteLine(DataReader.NodeType);
                    DataReader.ResolveEntity();
                    CError.WriteLine("ResolveEntity succeeded");
                }
                catch (InvalidOperationException)
                {
                    goto next;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);

            next: bool b = DataReader.ReadAttributeValue();
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "ResolveEntity returns True");
            }
            return TEST_FAIL;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML ReadAttributeValue
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCReadAttributeValue : TCXMLReaderBaseGeneral
    {
        private bool VerifyAttribute(string strName, string strValue)
        {
            bool bPassed = false;

            bPassed = DataReader.VerifyNode(XmlNodeType.Attribute, strName, strValue);
            bPassed = DataReader.ReadAttributeValue() && bPassed;
            bPassed = DataReader.VerifyNode(XmlNodeType.Text, string.Empty, strValue) && bPassed;
            bPassed = !DataReader.ReadAttributeValue() && bPassed;

            return bPassed;
        }

        ////////////////////////////////////////////////////////////////
        // Variations
        ////////////////////////////////////////////////////////////////
        [Variation("ReadAttributeValue where Attribute count = 0")]
        public int TestReadAttributeValue1()
        {
            ReloadSource();
            DataReader.PositionOnElement("ATTRIBUTE1");

            return BoolToLTMResult(!DataReader.ReadAttributeValue());
        }

        [Variation("ReadAttributeValue where Attribute count = 1")]
        public int TestReadAttributeValue2()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement("ATTRIBUTE2");

            bPassed = DataReader.MoveToFirstAttribute();
            bPassed = VerifyAttribute("a1", "a1value") && bPassed;

            bPassed = !DataReader.MoveToNextAttribute() && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadAttributeValue where Attribute count = 3", Pri = 0)]
        public int TestReadAttributeValue3()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement("ATTRIBUTE3");

            bPassed = DataReader.MoveToFirstAttribute();
            bPassed = VerifyAttribute("a1", "a1value") && bPassed;

            bPassed = DataReader.MoveToNextAttribute() && bPassed;
            bPassed = VerifyAttribute("a2", "a2value") && bPassed;

            bPassed = DataReader.MoveToNextAttribute() && bPassed;
            bPassed = VerifyAttribute("a3", "a3value") && bPassed;

            bPassed = !DataReader.MoveToNextAttribute() && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadAttributeValue where Attribute count = 1 and value is empty String", Pri = 0)]
        public int TestReadAttributeValue4()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement("ATTRIBUTE4");

            bPassed = DataReader.MoveToFirstAttribute();
            bPassed = VerifyAttribute("a1", string.Empty) && bPassed;

            bPassed = !DataReader.MoveToNextAttribute() && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("ReadAttributeValue successive calls")]
        public int TestReadAttributeValue5()
        {
            if (IsXsltReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsBinaryReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            string strxml = "<!DOCTYPE ROOT [<!ENTITY e 'bbb'>]><ROOT attr='a&e;c'/>";
            ReloadSource(new StringReader(strxml));

            DataReader.PositionOnElement("ROOT");
            DataReader.MoveToFirstAttribute();

            CError.Compare(DataReader.ReadAttributeValue(), "rava");
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Text, string.Empty, "a"), "vna");

            CError.Compare(DataReader.ReadAttributeValue(), "rave");
            CError.Compare(DataReader.VerifyNode(XmlNodeType.EntityReference, "e", string.Empty), "vne");

            CError.Compare(DataReader.ReadAttributeValue(), "ravc");
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Text, string.Empty, "c"), "vnc");

            CError.Compare(!DataReader.ReadAttributeValue(), "nrav");

            DataReader.Read();
            CError.Compare(DataReader.VerifyNode(XmlNodeType.None, string.Empty, string.Empty), "vnn");

            return TEST_PASS;
        }
    }
}
