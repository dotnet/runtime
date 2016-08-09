// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // TestCase TCXML Normalization
    //
    ////////////////////////////////////////////////////////////////
    [TestCase(Name = "FactoryReader Normalization", Desc = "FactoryReader")]
    public partial class TCNormalization : TCXMLReaderBaseGeneral
    {
        protected const String ST_ATTR_TEST_NAME = "ATTRIBUTE5";
        protected const String ST_ATTR_EXP_STRING = "x x";
        protected const String ST_ATTR_EXP_STRING_MS = "x     x";
        protected const String ST_ELEM_EXP_STRING = "x\nx";


        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);

            CreateTestFile(EREADER_TYPE.LBNORMALIZATION);
            return ret;
        }


        public override int Terminate(object objParam)
        {
            // just in case it failed without closing
            DataReader.Close();

            return base.Terminate(objParam);
        }


        ////////////////////////////////////////////////////////////////
        // Variations
        ////////////////////////////////////////////////////////////////
        [Variation("XmlTextReader Normalization - CRLF in Attribute value", Pri = 0)]
        public int TestNormalization1()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement(ST_ATTR_TEST_NAME);
            bPassed = CError.Equals(DataReader.GetAttribute("CRLF"), ST_ATTR_EXP_STRING, CurVariation.Desc);
            return BoolToLTMResult(bPassed);
        }


        [Variation("XmlTextReader Normalization - CR in Attribute value")]
        public int TestNormalization2()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement(ST_ATTR_TEST_NAME);
            bPassed = CError.Equals(DataReader.GetAttribute("CR"), ST_ATTR_EXP_STRING, CurVariation.Desc);
            return BoolToLTMResult(bPassed);
        }


        [Variation("XmlTextReader Normalization - LF in Attribute value")]
        public int TestNormalization3()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement(ST_ATTR_TEST_NAME);
            bPassed = CError.Equals(DataReader.GetAttribute("LF"), ST_ATTR_EXP_STRING, CurVariation.Desc);
            return BoolToLTMResult(bPassed);
        }


        [Variation("XmlTextReader Normalization - multiple spaces in Attribute value", Pri = 0)]
        public int TestNormalization4()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement(ST_ATTR_TEST_NAME);

            // as far as the MS attribute is CDATA internal spaces are not compacted
            bPassed = CError.Equals(DataReader.GetAttribute("MS"), ST_ATTR_EXP_STRING_MS, CurVariation.Desc);
            return BoolToLTMResult(bPassed);
        }


        [Variation("XmlTextReader Normalization - tab in Attribute value", Pri = 0)]
        public int TestNormalization5()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement(ST_ATTR_TEST_NAME);
            bPassed = CError.Equals(DataReader.GetAttribute("TAB"), ST_ATTR_EXP_STRING, CurVariation.Desc);
            return BoolToLTMResult(bPassed);
        }


        [Variation("XmlTextReader Normalization - CRLF in text node", Pri = 0)]
        public int TestNormalization6()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement("ENDOFLINE1");
            DataReader.Read();
            bPassed = CError.Equals(DataReader.Value, ST_ELEM_EXP_STRING, CurVariation.Desc);
            return BoolToLTMResult(bPassed);
        }


        [Variation("XmlTextReader Normalization - CR in text node")]
        public int TestNormalization7()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement("ENDOFLINE2");
            DataReader.Read();
            bPassed = CError.Equals(DataReader.Value, ST_ELEM_EXP_STRING, CurVariation.Desc);
            return BoolToLTMResult(bPassed);
        }


        [Variation("XmlTextReader Normalization - LF in text node")]
        public int TestNormalization8()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement("ENDOFLINE3");
            DataReader.Read();
            bPassed = CError.Equals(DataReader.Value, ST_ELEM_EXP_STRING, CurVariation.Desc);
            return BoolToLTMResult(bPassed);
        }


        [Variation("XmlTextReader Normalization = true with invalid chars", Pri = 0)]
        public int TestNormalization9()
        {
            ReloadSourceStr("<root>&#0;&#1;&#2;&#3;&#4;&#5;&#6;&#7;&#8;&#9;</root>");

            try
            {
                while (DataReader.Read()) ;
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        // XML 1.0 SE
        [Variation("Line breaks normalization in document entity")]
        public int TestNormalization11()
        {
            ReloadSource();

            // #xD should be replaced by #xA
            while (DataReader.Read())
            {
                if (DataReader.HasValue)
                {
                    if (DataReader.Value.IndexOf('\r') != -1)
                    {
                        CError.WriteLine("#xD found in node {0}, line {1} col {2}", DataReader.NodeType, DataReader.LineNumber, DataReader.LinePosition);
                        return TEST_FAIL;
                    }
                }
            }

            return TEST_PASS;
        }


        [Variation("XmlTextReader Normalization = true with invalid chars")]
        public int TestNormalization14()
        {
            string[] invalidXML = { "0", "8", "B", "C", "E", "1F", "FFFE", "FFFF" };

            for (int i = 0; i < invalidXML.Length; i++)
            {
                string strxml = String.Format("<ROOT>&#x{0};</ROOT>", invalidXML[i]);

                ReloadSourceStr(strxml);
                try
                {
                    while (DataReader.Read()) ;

                    CError.WriteLine("Accepted invalid character XML");
                    return TEST_FAIL;
                }
                catch (XmlException e)
                {
                    CheckXmlException("Xml_InvalidCharacter", e, 1, 10);
                }
            }

            return TEST_PASS;
        }

        [Variation("Character entities with Normalization=true")]
        public int TestNormalization16()
        {
            string strxml = "<e a='a&#xD;\r\n \r&#xA;b&#xD;&#x20;&#x9;&#x41;'/>";
            string expNormalizedValue = "a\r   \nb\r \tA";

            ReloadSourceStr(strxml);
            DataReader.Read();

            // use different ways of getting the value
            string valueGet = DataReader.GetAttribute("a");

            DataReader.MoveToAttribute("a");

            string valueMove = DataReader.Value;

            DataReader.ReadAttributeValue();

            string valueRead = DataReader.Value;

            CError.Compare(valueGet, expNormalizedValue, "Wrong normalization (GetAttributeValue)");
            CError.Compare(valueMove, expNormalizedValue, "Wrong normalization (MoveToAttribute)");
            CError.Compare(valueRead, expNormalizedValue, "Wrong normalization (ReadAttributeValue)");
            return TEST_PASS;
        }


        [Variation("Character entities with in text nodes")]
        public int TestNormalization17()
        {
            string strxml = "<root>a&#xD;&#xA;&#xD;b</root>";

            ReloadSourceStr(strxml);
            DataReader.PositionOnNodeType(XmlNodeType.Text);
            CError.Compare(DataReader.Value, "a\r\n\rb", "Wrong end-of-line handling");
            return TEST_PASS;
        }
    }
}
