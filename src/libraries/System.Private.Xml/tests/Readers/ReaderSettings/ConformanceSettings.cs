// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Collections.Generic;
using System.IO;
using WebData.BaseLib;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    [TestCase(Name = "Conformance Settings", Desc = "Conformance Settings")]
    public partial class TCConformanceSettings : TCXMLReaderBaseGeneral
    {
        public string GetSimpleInvalidXml()
        {
            string invalidCharString = StringGen.GetIllegalXmlString(10, true);
            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutDecl();
            mn.OpenElement("&#xd800;");
            mn.CloseElement();
            mn.PutText(invalidCharString);
            mn.PutEndElement();
            return mn.GetNodes();
        }


        //[Variation("Wrapping Tests: CR with CR", Pri = 2, Params = new object[] { "Auto", "Auto", "<root/>", "true" })]
        //[Variation("Wrapping Tests: CR with CR", Pri = 2, Params = new object[] { "Auto", "Auto", "<root/><root/>", "true" })]
        //[Variation("Wrapping Tests: CR with CR", Pri = 2, Params = new object[] { "Fragment", "Auto", "<root/><root/>", "true" })]
        //[Variation("Wrapping Tests: CR with CR", Pri = 2, Params = new object[] { "Document", "Auto", "<root/>", "true" })]

        //[Variation("Wrapping Tests: CR with CR", Pri = 2, Params = new object[] { "Auto", "Fragment", "<root/>", "false" })]
        //[Variation("Wrapping Tests: CR with CR", Pri = 2, Params = new object[] { "Fragment", "Fragment", "<root/>", "true" })]
        //[Variation("Wrapping Tests: CR with CR", Pri = 2, Params = new object[] { "Fragment", "Fragment", "<root/><root/>", "true" })]
        //[Variation("Wrapping Tests: CR with CR", Pri = 2, Params = new object[] { "Document", "Fragment", "<root/>", "false" })]

        //[Variation("Wrapping Tests: CR with CR", Pri = 2, Params = new object[] { "Auto", "Document", "<root/>", "false" })]
        //[Variation("Wrapping Tests: CR with CR", Pri = 2, Params = new object[] { "Fragment", "Document", "<root/><root/>", "false" })]
        //[Variation("Wrapping Tests: CR with CR", Pri = 2, Params = new object[] { "Document", "Document", "<root/>", "true" })]


        public int wrappingTests()
        {
            string underlyingReaderLevel = this.CurVariation.Params[0].ToString();
            string wrappingReaderLevel = this.CurVariation.Params[1].ToString();
            string conformanceXml = this.CurVariation.Params[2].ToString();
            bool valid = this.CurVariation.Params[3].ToString() == "true";

            CError.WriteLine(underlyingReaderLevel);
            CError.WriteLine(wrappingReaderLevel);
            CError.WriteLine(conformanceXml);
            CError.WriteLine("IsValid = " + valid);


            try
            {
                XmlReaderSettings rsU = new XmlReaderSettings();
                rsU.ConformanceLevel = (ConformanceLevel)Enum.Parse(typeof(ConformanceLevel), underlyingReaderLevel);
                XmlReader rU = ReaderHelper.Create(new StringReader(conformanceXml), rsU, (string)null);
                XmlReaderSettings rsW = new XmlReaderSettings();
                rsW.ConformanceLevel = (ConformanceLevel)Enum.Parse(typeof(ConformanceLevel), wrappingReaderLevel);
                XmlReader rW = ReaderHelper.Create(rU, rsW);
                CError.Compare(rW.ReadState, ReadState.Initial, "ReadState not initial");
            }
            catch (InvalidOperationException ioe)
            {
                CError.WriteLineIgnore(ioe.ToString());
                if (valid)
                    throw new CTestFailedException("Valid case throws InvalidOperation");
                else
                    return TEST_PASS;
            }

            if (!valid)
                throw new CTestFailedException("Invalid case doesn't throw InvalidOperation");
            else
                return TEST_PASS;
        }

        [Variation("Default Values", Pri = 0)]
        public int v1()
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            CError.Compare(rs.CheckCharacters, true, "CheckCharacters not true");
            CError.Compare(rs.ConformanceLevel, ConformanceLevel.Document, "Conformance Level not document by default");
            return TEST_PASS;
        }

        //[Variation("Default Reader, Check Characters On and pass invalid characters", Pri = 0, Params = new object[]{"CoreValidatingReader"})]
        //[Variation("Default Reader, Check Characters On and pass invalid characters", Pri = 0, Params = new object[]{"CoreReader"})]
        public int v2()
        {
            string readerType = (string)this.CurVariation.Params[0];
            bool exceptionThrown = false;
            string invalidCharXml = GetSimpleInvalidXml();

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.CheckCharacters = true;

            XmlReader reader = ReaderHelper.CreateReader(readerType, new StringReader(invalidCharXml), false, null, rs);
            try
            {
                while (reader.Read()) ;
            }
            catch (XmlException xe)
            {
                CError.WriteLine(xe.Message);
                exceptionThrown = true;
            }

            reader.Dispose();

            if (!exceptionThrown)
                return TEST_FAIL;

            return TEST_PASS;
        }

        //[Variation("Default Reader, Check Characters Off and pass invalid characters in text", Pri = 0, Params = new object[] { "CoreValidatingReader" })]
        //[Variation("Default Reader, Check Characters Off and pass invalid characters in text", Pri = 0, Params = new object[]{"CoreReader"})]
        public int v3()
        {
            string readerType = (string)this.CurVariation.Params[0];

            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutDecl();
            mn.OpenElement();
            mn.CloseElement();

            //This is an invalid char in XML.
            mn.PutText("&#xd800;");
            mn.PutEndElement();
            string invalidCharXml = mn.GetNodes();

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.CheckCharacters = false;

            using (XmlReader reader = ReaderHelper.CreateReader(readerType, new StringReader(invalidCharXml), false, null, rs))
            {
                try
                {
                    while (reader.Read()) ;
                }
                catch (XmlException xe)
                {
                    CError.WriteIgnore(invalidCharXml);
                    CError.WriteLine(xe.Message);
                    CError.WriteLine(xe.StackTrace);
                    return TEST_FAIL;
                }
            }

            return TEST_PASS;
        }

        //[Variation("Default Reader, Check Characters Off and pass invalid characters in element", Pri = 0, Params = new object[] { "CoreValidatingReader" })]
        //[Variation("Default Reader, Check Characters Off and pass invalid characters in element", Pri = 0, Params = new object[] {"CoreReader"})]
        public int v4()
        {
            string readerType = (string)this.CurVariation.Params[0];
            bool exceptionThrown = false;

            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutDecl();
            mn.OpenElement("&#xd800;");
            mn.CloseEmptyElement();

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.CheckCharacters = false;
            XmlReader reader = ReaderHelper.CreateReader(readerType, new StringReader(mn.GetNodes()), false, null, rs);
            try
            {
                while (reader.Read()) ;
            }
            catch (XmlException xe)
            {
                CError.WriteIgnore(xe.Message + "\n");
                exceptionThrown = true;
            }

            mn.Close();
            reader.Dispose();

            if (!exceptionThrown)
                return TEST_FAIL;

            return TEST_PASS;
        }

        //[Variation("Default Reader, Check Characters On and pass invalid characters in text", Pri = 0, Params = new object[] { "CoreValidatingReader" })]
        //[Variation("Default Reader, Check Characters On and pass invalid characters in text", Pri = 0, Params = new object[] { "CoreReader" })]
        public int v5()
        {
            string readerType = (string)this.CurVariation.Params[0];
            bool exceptionThrown = false;

            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutDecl();
            mn.OpenElement();
            mn.CloseElement();
            mn.PutText("&#xd800;"); //This is invalid char in XML.
            mn.PutEndElement();
            string invalidCharXml = mn.GetNodes();

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.CheckCharacters = true;

            XmlReader reader = ReaderHelper.CreateReader(readerType, new StringReader(invalidCharXml), false, null, rs);
            try
            {
                while (reader.Read()) ;
            }
            catch (XmlException xe)
            {
                CError.WriteIgnore(invalidCharXml);
                CError.WriteLine(xe.Message);
                CError.WriteLine(xe.StackTrace);
                exceptionThrown = true;
            }

            mn.Close();
            reader.Dispose();

            if (!exceptionThrown)
                return TEST_FAIL;

            return TEST_PASS;
        }

        public string GetPatternXml(string pattern)
        {
            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutPattern(pattern);
            CError.WriteLine("'" + mn.GetNodes() + "'");
            return mn.GetNodes();
        }


        private static bool[] s_pri0ExpectedNone = { false, false, false, false, false, true, true, true, true, true, false };
        private static bool[] s_pri0ExpectedFragment = { false, false, false, false, false, true, true, true, true, true, false };
        private static bool[] s_pri0ExpectedDocument = { true, true, false, true, true, true, true, true, true, true, false };

        public object[] GetAllPri0ConformanceTestXmlStrings()
        {
            /*
            The following XML Strings will be created :
            
            1 Text at Top Level
            2 More than one element at top level
            3 WhiteSpace at Top level
            4 Top Level Attribute
            5 Multiple Contiguous Text Nodes.
            6 Same Prefix declared twice.
            7 xml:space contains wrong value
            8 Invalid Name for element
            9 Invalid Name for attribute
            10 Prefix Xml matched with wrong namespace URI.
            11 prefix Xml missing Namepace URI
            12 prefix or localname xmlns matches with wrong namespace URI
            13 prefix or localname xmlns missing namespace uri.

            */

            List<string> list = new List<string>();
            ManagedNodeWriter mn = null;

            list.Add(GetPatternXml("T")); //1
            list.Add(GetPatternXml("XEMEM"));//2
            list.Add(GetPatternXml("WEM"));//3
            list.Add(GetPatternXml("TPT"));//4
            list.Add(GetPatternXml("A"));//5

            //6
            mn = new ManagedNodeWriter();
            mn.PutPattern("XE");
            mn.PutAttribute("xmlns:a", "http://www.foo.com");
            mn.PutAttribute("xmlns:a", "http://www.foo.com");
            mn.PutPattern("M");
            CError.WriteLine(mn.GetNodes());
            list.Add(mn.GetNodes());
            mn.Close();

            //7
            mn = new ManagedNodeWriter();
            mn.PutPattern("XE");
            mn.PutAttribute("xml:space", "rubbish");
            mn.PutPattern("M");
            CError.WriteLine(mn.GetNodes());
            list.Add(mn.GetNodes());
            mn.Close();

            //8
            mn = new ManagedNodeWriter();
            mn.PutPattern("X");
            mn.OpenElement(UnicodeCharHelper.GetInvalidCharacters(CharType.XmlChar));
            mn.PutPattern("M");
            CError.WriteLine(mn.GetNodes());
            list.Add(mn.GetNodes());
            mn.Close();

            //9
            mn = new ManagedNodeWriter();
            mn.PutPattern("XE");
            mn.PutAttribute(UnicodeCharHelper.GetInvalidCharacters(CharType.XmlChar), UnicodeCharHelper.GetInvalidCharacters(CharType.XmlChar));
            mn.PutPattern("M");
            CError.WriteLine(mn.GetNodes());
            list.Add(mn.GetNodes());
            mn.Close();

            //10
            mn = new ManagedNodeWriter();
            mn.PutPattern("XE");
            mn.PutAttribute("xmlns:xml", "http://wrong");
            mn.PutPattern("M");
            CError.WriteLine(mn.GetNodes());
            list.Add(mn.GetNodes());
            mn.Close();

            //11
            mn = new ManagedNodeWriter();
            mn.PutPattern("XE");
            mn.PutAttribute("xml:space", "default");
            mn.PutPattern("M");
            CError.WriteLine(mn.GetNodes());
            list.Add(mn.GetNodes());
            mn.Close();

            return list.ToArray();
        }

        //Conformance-level tests
        [Variation("Conformance Level to Auto and test various scenarios from test plan", Pri = 0)]
        public int CAuto()
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ConformanceLevel = ConformanceLevel.Auto;
            object[] xml = GetAllPri0ConformanceTestXmlStrings();
            if (xml.Length > s_pri0ExpectedNone.Length)
            {
                CError.WriteLine("Invalid Compare attempted");
                return TEST_FAIL;
            }

            bool failed = false;

            for (int i = 0; i < xml.Length; i++)
            {
                XmlReader reader = ReaderHelper.Create(new StringReader((string)xml[i]), rs, (string)null);
                bool actual = false;
                try
                {
                    while (reader.Read()) ;
                }
                catch (XmlException xe)
                {
                    CError.Write("Case : " + (i + 1));
                    CError.WriteLine(xe.Message);
                    actual = true;
                }

                if (actual != s_pri0ExpectedNone[i])
                {
                    CError.WriteLine("ConformanceLevel = Auto");
                    CError.WriteLine("Test Failed for Case : " + (i + 1));
                    CError.WriteLine((string)xml[i]);
                    failed = true;
                }
            }//end for

            if (failed)
                return TEST_FAIL;

            return TEST_PASS;
        }//end variation

        [Variation("Conformance Level to Fragment and test various scenarios from test plan", Pri = 0)]
        public int CFragment()
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ConformanceLevel = ConformanceLevel.Fragment;
            object[] xml = GetAllPri0ConformanceTestXmlStrings();
            if (xml.Length > s_pri0ExpectedFragment.Length)
            {
                CError.WriteLine("Invalid Compare attempted");
                return TEST_FAIL;
            }

            bool failed = false;

            for (int i = 0; i < xml.Length; i++)
            {
                XmlReader reader = ReaderHelper.Create(new StringReader((string)xml[i]), rs, (string)null);
                bool actual = false;
                try
                {
                    while (reader.Read()) ;
                }
                catch (XmlException xe)
                {
                    CError.Write("Case : " + (i + 1));
                    CError.WriteLine(xe.Message);
                    actual = true;
                }

                if (actual != s_pri0ExpectedFragment[i])
                {
                    CError.WriteLine("ConformanceLevel = Fragment");
                    CError.WriteLine("Test Failed for Case" + (i + 1));
                    CError.WriteLine((string)xml[i]);
                    failed = true;
                }
            }//end for

            if (failed)
                return TEST_FAIL;

            return TEST_PASS;
        }//end variation

        [Variation("Conformance Level to Document and test various scenarios from test plan", Pri = 0)]
        public int CDocument()
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ConformanceLevel = ConformanceLevel.Document;
            object[] xml = GetAllPri0ConformanceTestXmlStrings();
            if (xml.Length > s_pri0ExpectedDocument.Length)
            {
                CError.WriteLine("Invalid Compare attempted");
                return TEST_FAIL;
            }

            bool failed = false;

            for (int i = 0; i < xml.Length; i++)
            {
                XmlReader reader = ReaderHelper.Create(new StringReader((string)xml[i]), rs, (string)null);
                bool actual = false;
                try
                {
                    while (reader.Read()) ;
                }
                catch (XmlException xe)
                {
                    CError.Write("Case : " + (i + 1));
                    CError.WriteLine(xe.Message);
                    actual = true;
                }

                if (actual != s_pri0ExpectedDocument[i])
                {
                    CError.WriteLine("ConformanceLevel = Document");
                    CError.WriteLine("Test Failed for Case" + (i + 1));
                    CError.WriteLine("|" + (string)xml[i] + "|");
                    failed = true;
                }
            }

            if (failed)
                return TEST_FAIL;

            return TEST_PASS;
        }


        [Variation("Test Invalid Value Range for enum properties", Pri = 1)]
        public int InvalidValueRange()
        {
            XmlReaderSettings settings = null;

            try
            {
                settings = new XmlReaderSettings();
                settings.ConformanceLevel = (ConformanceLevel)666;
                return TEST_FAIL;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            return TEST_PASS;
        }
    }
}
