// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Dynamic;

namespace System.Xml.Tests
{
    /***********************************************************/
    /*               XsltArgumentList.GetParam                 */
    /***********************************************************/

    //[TestCase(Name = "XsltArgumentList - GetParam", Desc = "Get Param Test Cases")]
    public class CArgIntegrity : XsltApiTestCaseBase
    {
        private ITestOutputHelper _output;
        private string _xslFile;
        private string _xmlFile;

        public CArgIntegrity(ITestOutputHelper output) : base(output)
        {
            _output = output;
            _xslFile = $"{GetTestFilePath()}.xsl";
            _xmlFile = $"{GetTestFilePath()}.xml";
        }

        //[Variation(Desc = "Basic Verification Test", Pri = 0)]
        [InlineData()]
        [Theory]
        public void GetParam1()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                Assert.True(false);
            return;
        }

        private static string s_typeXml = "<order></order>";

        private static string s_typeXsl = @"<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>
  <xsl:param name='param'/>
  <xsl:template match='/'>
    <order>
      <param><xsl:value-of select='$param'/></param>
    </order>
  </xsl:template>
</xsl:stylesheet>";

        private void WriteFiles(string input, string output)
        {
            using (XmlWriter w = XmlWriter.Create(output))
            {
                using (XmlReader r = XmlReader.Create(new StringReader(input)))
                {
                    w.WriteNode(r, false);
                }
            }
        }

        private void WriteXmlAndXslFiles()
        {
            WriteFiles(s_typeXml, _xmlFile);
            WriteFiles(s_typeXsl, _xslFile);
        }

        //[Variation(Desc = "Tuple.XsltArgumentList.AddParam/AddExtensionObject", Param = 1)]
        [InlineData(1)]
        //[Variation(Desc = "DynamicObject.XsltArgumentList.AddParam/AddExtensionObject", Param = 2)]
        [InlineData(2)]
        //[Variation(Desc = "Guid.XsltArgumentList.AddParam/AddExtensionObject", Param = 3)]
        [InlineData(3)]
        //[Variation(Desc = "Dictionary.XsltArgumentList.AddParam/AddExtensionObject", Param = 4)]
        [InlineData(4)]
        [Theory]
        public void var_types(int param)
        {
            WriteXmlAndXslFiles();

            object t = null;
            switch (param)
            {
                case 1: t = Tuple.Create(1, "Melitta", 7.5); break;
                case 2: t = new TestDynamicObject(); break;
                case 3: t = new Guid(); break;
                case 4: t = new Dictionary<string, object>(); break;
            }
            _output.WriteLine(t.ToString());

#pragma warning disable 0618
            XslTransform xslt = new XslTransform();
#pragma warning restore 0618
            xslt.Load(_xslFile);

            XsltArgumentList xslArg = new XsltArgumentList();
            xslArg.AddParam("param", "", t);
            xslArg.AddExtensionObject("", t);

            XPathDocument xpathDom = new XPathDocument(_xmlFile);
            using (StringWriter sw = new StringWriter())
            {
                xslt.Transform(xpathDom, xslArg, sw);
                _output.WriteLine(sw.ToString());
            }
            return;
        }

        public class TestDynamicObject : DynamicObject
        {
            public dynamic GetDynamicObject()
            {
                return new Dictionary<string, object>();
            }
        }

        //[Variation("Param name is null")]
        [InlineData()]
        [Theory]
        public void GetParam2()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam(null, szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Did not return NULL for null param name {0}", retObj);
                Assert.True(false);
            }
            else
                return;
        }

        //[Variation("Param name is empty string")]
        [InlineData()]
        [Theory]
        public void GetParam3()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam(szEmpty, szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Did not return NULL for empty string param name: {0}", retObj);
                Assert.True(false);
            }
            return;
        }

        //[Variation("Param name is non existent")]
        [InlineData()]
        [Theory]
        public void GetParam4()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam("RandomName", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Did not return NULL for non-existent parameter name: {0}", retObj);
                Assert.True(false);
            }
            return;
        }

        //[Variation("Invalid Param name")]
        [InlineData()]
        [Theory]
        public void GetParam5()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam(szInvalid, szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Did not return NULL for an invalid param name");
                Assert.True(false);
            }
            return;
        }

        //[Variation("Very Long Param")]
        [InlineData()]
        [Theory]
        public void GetParam6()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam(szLongString, szEmpty, "Test6");
            retObj = m_xsltArg.GetParam(szLongString, szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test6", retObj);
            if (retObj.ToString() != "Test6")
                Assert.True(false);
            return;
        }

        //[Variation("Namespace URI = null")]
        [InlineData()]
        [Theory]
        public void GetParam7()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam("myArg1", null);
            if (retObj != null)
            {
                _output.WriteLine("Did not return NULL for null namespace System.Xml.Tests");
                Assert.True(false);
            }
            return;
        }

        //[Variation("Namespace URI is empty string - Bug 200998")]
        [InlineData()]
        [Theory]
        public void GetParam8()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test8");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test8", retObj);
            if (retObj.ToString() != "Test8")
                Assert.True(false);
            return;
        }

        //[Variation("Namespace URI non-existent")]
        [InlineData()]
        [Theory]
        public void GetParam9()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test9");
            retObj = m_xsltArg.GetParam("myArg1", "http://www.microsoft.com");
            if (retObj != null)
            {
                _output.WriteLine("Did not retrieve a null value for non-existent uri");
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", "http://www.msn.com", "Test1");
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Did not retrieve a null value for non-existent uri");
                Assert.True(false);
            }

            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Did not retrieve a null value for non-existent uri");
                Assert.True(false);
            }
            return;
        }

        //[Variation("Very long namespace System.Xml.Tests")]
        [InlineData()]
        [Theory]
        public void GetParam10()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szLongNS, "Test10");
            retObj = m_xsltArg.GetParam("myArg1", szLongNS);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test10", retObj);
            if (retObj.ToString() != "Test10")
                Assert.True(false);
            return;
        }

        //[Variation("Different Data Types")]
        [InlineData()]
        [Theory]
        public void GetParam12()
        {
            m_xsltArg = new XsltArgumentList();
            string obj = "0.00";

            // string
            m_xsltArg.AddParam("myArg1", szEmpty, obj);
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "0.00", retObj);
            if (retObj.ToString() != "0.00")
            {
                _output.WriteLine("Failed to add/get a value for {0} of type {1}", "0.00", "string");
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            //int -- check conversions and value for original object and returned object
            int j = 8;
            int i = 8;
            m_xsltArg.AddParam("myArg2", szEmpty, i);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value:{1}", i, retObj);
            if (!i.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0} of type {1}", i, "int");
                Assert.True(false);
            }

            if (i.GetType() != j.GetType())
                Assert.True(false);

            bool bF = (1 == 0);
            m_xsltArg.AddParam("myArg3", szEmpty, bF);
            retObj = m_xsltArg.GetParam("myArg3", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bF.ToString(), retObj);
            if (!bF.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0} of type {1}", bF.ToString(), "boolean");
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            bool bT = (1 == 1);
            m_xsltArg.AddParam("myArg4", szEmpty, bT);
            retObj = m_xsltArg.GetParam("myArg4", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bT.ToString(), retObj);
            if (!bT.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0} of type {1}", bT.ToString(), "boolean");
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            XPathDocument xd = new XPathDocument(FullFilePath("Fish.xml"));

            m_xsltArg.AddParam("myArg5", szEmpty, ((IXPathNavigable)xd).CreateNavigator());
            retObj = m_xsltArg.GetParam("myArg5", szEmpty);
            if (retObj == null)
            {
                _output.WriteLine("Failed to add/get a value of type {1}", "XPathNavigator");
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }
            return;
        }

        //[Variation("Case Sensitivity")]
        [InlineData()]
        [Theory]
        public void GetParam13()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myarg1", szEmpty);
            if (retObj != null)
                Assert.True(false);
            retObj = m_xsltArg.GetParam("myArg1 ", szEmpty);
            if (retObj != null)
                Assert.True(false);
            retObj = m_xsltArg.GetParam("myArg", szEmpty);
            if (retObj != null)
                Assert.True(false);

            return;
        }

        //[Variation("Whitespace")]
        [InlineData()]
        [Theory]
        public void GetParam14()
        {
            int i = 1;
            m_xsltArg = new XsltArgumentList();

            foreach (string str in szWhiteSpace)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, "Test" + str);
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj.ToString() != "Test" + str)
                {
                    _output.WriteLine("Error processing {0} test for whitespace arg in first set", i);
                    Assert.True(false);
                }
                i++;
            }

            foreach (string str in szWhiteSpace)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, "Test"); // dont add whitespace to name here since addparam would throw
                retObj = m_xsltArg.GetParam("myArg" + str, szEmpty);
                if (retObj != null)
                {
                    _output.WriteLine("Error processing {0} test for whitespace arg in second set. Returned object is not null.", i);
                    Assert.True(false);
                }
                i++;
            }
            return;
        }

        //[Variation("Call After Param has been removed")]
        [InlineData()]
        [Theory]
        public void GetParam15()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myArg1", szEmpty);
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);

            if (retObj != null)
                Assert.True(false);
            return;
        }

        //[Variation("Call multiple Times")]
        [InlineData()]
        [Theory]
        public void GetParam16()
        {
            m_xsltArg = new XsltArgumentList();
            int i = 0;

            m_xsltArg.AddParam("myArg1", szEmpty, "Test16");
            for (i = 0; i < 200; i++)
            {
                retObj = m_xsltArg.GetParam("myArg1", szEmpty);
                if (retObj.ToString() != "Test16")
                {
                    _output.WriteLine("Failed after retrieving {0} times", i);
                    _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test16", retObj);
                    Assert.True(false);
                }
            }
            _output.WriteLine("Retrievied {0} times", i);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            return;
        }

        //[Variation("Using XSL namespace")]
        [InlineData()]
        [Theory]
        public void GetParam17()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test17");

            retObj = m_xsltArg.GetParam("myArg3", szDefaultNS);
            if (retObj != null)
            {
                _output.WriteLine("Return a non-null value when retrieving Param with namespace {0}", szXslNS);
                Assert.True(false);
            }
            return;
        }

        //[Variation("Resolving conflicts with variables with different namespaces")]
        [InlineData()]
        [Theory]
        public void GetParam18()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);

            m_xsltArg.AddParam("myArg1", "http://www.msn.com", "Test2");
            retObj = m_xsltArg.GetParam("myArg1", "http://www.msn.com");
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);

            if (retObj.ToString() != "Test2")
                Assert.True(false);

            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Retrieve Original Value:{0}\nActual Retrieved Value: {1}", "Test1", retObj);

            if (retObj.ToString() != "Test1")
                Assert.True(false);
            return;
        }

        //[Variation("Namespace AND param = null")]
        [InlineData()]
        [Theory]
        public void GetParam19()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam(null, null);
            if (retObj != null)
            {
                _output.WriteLine("Did not return NULL for null parameter name");
                Assert.True(false);
            }
            return;
        }

        //[Variation("Data Types - Of type Double ")]
        [InlineData()]
        [Theory]
        public void GetParamDoubles()
        {
            double d1 = double.PositiveInfinity;
            double d2 = double.NegativeInfinity;
            double d3 = double.NaN;
            double d4 = 2.000001;
            double d5 = 0.00;
            double d6 = double.MaxValue;
            double d7 = double.MinValue;

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, d1);
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d1, retObj);
            if (!double.IsPositiveInfinity((double)retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0}", d1);
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, d2);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d2, retObj);
            if (!double.IsNegativeInfinity((double)retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0}", d2);
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg3", szEmpty, d3);
            retObj = m_xsltArg.GetParam("myArg3", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d3, retObj);
            if (!double.IsNaN((double)retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0}", d3);
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg4", szEmpty, d4);
            retObj = m_xsltArg.GetParam("myArg4", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d4, retObj);
            if (!d4.Equals((double)retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0}", d4);
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg5", szEmpty, d5);
            retObj = m_xsltArg.GetParam("myArg5", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d5, retObj);
            if (!d5.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0}", d5);
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg6", szEmpty, d6);
            retObj = m_xsltArg.GetParam("myArg6", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d6, retObj);
            if (!d6.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0}", d6);
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg7", szEmpty, d7);
            retObj = m_xsltArg.GetParam("myArg7", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d7, retObj);
            if (!d7.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0}", d7);
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }
            return;
        }
    }

    /***********************************************************/
    /*               XsltArgumentList.AddParam                 */
    /***********************************************************/

    //[TestCase(Name = "XsltArgumentList - AddParam : Reader, Reader", Desc = "READER,READER")]
    //[TestCase(Name = "XsltArgumentList - AddParam : Reader, Stream", Desc = "READER,STREAM")]
    //[TestCase(Name = "XsltArgumentList - AddParam : Reader, Writer", Desc = "READER,WRITER")]
    //[TestCase(Name = "XsltArgumentList - AddParam : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    //[TestCase(Name = "XsltArgumentList - AddParam : URI, Reader", Desc = "URI,READER")]
    //[TestCase(Name = "XsltArgumentList - AddParam : URI, Stream", Desc = "URI,STREAM")]
    //[TestCase(Name = "XsltArgumentList - AddParam : URI, Writer", Desc = "URI,WRITER")]
    //[TestCase(Name = "XsltArgumentList - AddParam : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    //[TestCase(Name = "XsltArgumentList - AddParam : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    //[TestCase(Name = "XsltArgumentList - AddParam : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    //[TestCase(Name = "XsltArgumentList - AddParam : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    //[TestCase(Name = "XsltArgumentList - AddParam : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CArgAddParam : XsltApiTestCaseBase
    {
        private ITestOutputHelper _output;
        public CArgAddParam(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation(Desc = "Basic Verification Test", Pri = 0)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam1(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test1
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                Assert.True(false);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Param  = null")]
        [InlineData()]
        [Theory]
        public void AddParam2()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam(null, szEmpty, "Test1");
            }
            catch (System.ArgumentNullException)
            {
                return;
            }
            _output.WriteLine("System.ArgumentNullException not thrown for adding null param");
            Assert.True(false);
        }

        //[Variation("Param name is empty string")]
        [InlineData()]
        [Theory]
        public void AddParam3()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam(szEmpty, szEmpty, "Test1");
            }
            catch (System.ArgumentNullException)
            {
                return;
            }
            _output.WriteLine("System.ArgumentNullException not thrown for param name empty string");
            Assert.True(false);
        }

        //[Variation("Very Long Param Name")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam4(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test1</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam(szLongString, szEmpty, "Test1");
            retObj = m_xsltArg.GetParam(szLongString, szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                Assert.True(false);

            if ((LoadXSL("showParamLongName.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Invalid Param name")]
        [InlineData()]
        [Theory]
        public void AddParam5()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam(szInvalid, szEmpty, "Test1");
            }
            catch (System.Xml.XmlException)
            {
                return;
            }
            _output.WriteLine("System.Xml.XmlException not thrown for invalid param name");
            Assert.True(false);
        }

        //[Variation("Namespace URI = null")]
        [InlineData()]
        [Theory]
        public void AddParam6()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam("myArg1", null, "Test1");
            }
            catch (System.ArgumentNullException)
            {
                return;
            }
            _output.WriteLine("System.ArgumentNullException not thrown for null namespace System.Xml.Tests");
            Assert.True(false);
        }

        //[Variation("Namespace URI is empty string")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam7(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test7
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test7");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);

            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test7")
                Assert.True(false);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Very long namespace System.Xml.Tests")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam8(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szLongNS, "Test8");
            retObj = m_xsltArg.GetParam("myArg1", szLongNS);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test8")
                Assert.True(false);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Objects as different Data Types")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam10(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.My Custom Object has a value of 10
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();
            MyObject m = new MyObject(10, _output);

            m_xsltArg.AddParam("myArg1", szEmpty, m);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Setting a param that already exists")]
        [InlineData()]
        [Theory]
        public void AddParam11()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test2");
            try
            {
                m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            }
            catch (System.ArgumentException)
            {
                return;
            }
            _output.WriteLine("Did not throw System.ArgumentException for adding a param that already exists");
            Assert.True(false);
        }

        //[Variation("Object with same name, different namespace System.Xml.Tests")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam12(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test1
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                Assert.True(false);

            m_xsltArg.AddParam("myArg1", "http://www.msn.com", "Test2");
            retObj = m_xsltArg.GetParam("myArg1", "http://www.msn.com");
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);

            if (retObj.ToString() != "Test2")
                Assert.True(false);

            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Retrieve Original Value:{0}\nActual Retrieved Value: {1}", "Test1", retObj);

            if (retObj.ToString() != "Test1")
                Assert.True(false);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Object with same namespace System.Xml.Tests, different name")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam13(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test1
		2.Test2
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                Assert.True(false);

            m_xsltArg.AddParam("myArg2", szEmpty, "Test2");
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);

            if (retObj.ToString() != "Test2")
                Assert.True(false);

            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Retrieve Original Value:{0}\nActual Retrieved Value: {1}", "Test1", retObj);

            if (retObj.ToString() != "Test1")
                Assert.True(false);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Case Sensitivity")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam14(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test1
		2.Test2
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                Assert.True(false);

            m_xsltArg.AddParam("myarg1", szEmpty, "Test2");
            retObj = m_xsltArg.GetParam("myarg1", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);
            if (retObj.ToString() != "Test2")
                Assert.True(false);

            m_xsltArg.AddParam("myArg2", szEmpty, "Test2");
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);
            if (retObj.ToString() != "Test2")
                Assert.True(false);

            m_xsltArg.AddParam("myarg3", szEmpty, "Test3");
            retObj = m_xsltArg.GetParam("myarg3", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test3", retObj);
            if (retObj.ToString() != "Test3")
                Assert.True(false);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Object is null")]
        [InlineData()]
        [Theory]
        public void AddParam15()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam("myArg1", szEmpty, null);
            }
            catch (System.ArgumentNullException)
            {
                return;
            }
            _output.WriteLine("System.ArgumentNullException not thrown for null object");
            Assert.True(false);
        }

        //[Variation("Add/remove object many times")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam16(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.Test1
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();
            string obj = "Test";

            for (int i = 0; i < 200; i++)
            {
                m_xsltArg.AddParam("myArg2", szEmpty, obj + i);
                retObj = m_xsltArg.GetParam("myArg2", szEmpty);
                if (retObj.ToString() != ("Test" + i))
                {
                    _output.WriteLine("Failed to add/remove iteration {0}", i);
                    Assert.True(false);
                }
                m_xsltArg.RemoveParam("myArg2", szEmpty);
            }

            for (int i = 0; i < 200; i++)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, obj + i);
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj.ToString() != (obj + i))
                {
                    _output.WriteLine("Failed in 2nd part to add/remove iteration {0}", i);
                    Assert.True(false);
                }
                m_xsltArg.RemoveParam("myArg2", szEmpty);
            }

            for (int i = 0; i < 200; i++)
            {
                m_xsltArg.RemoveParam("myArg" + i, szEmpty);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, obj + "1");
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj.ToString() != ("Test1"))
                Assert.True(false);
            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Whitespace in URI and param")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam17(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test  
		2.Test

		3.Test	
		4.Test

		5.Test	
  
	
		6.No Value Specified</result>";

            int i = 1;
            int errCount = 0;
            m_xsltArg = new XsltArgumentList();

            foreach (string str in szWhiteSpace)
            {
                try
                {
                    m_xsltArg.AddParam("myArg" + i, szEmpty, "Test" + str);
                }
                catch (System.Xml.XmlException)
                {
                    _output.WriteLine("Improperly reported an exception for a whitespace value");
                    Assert.True(false);
                }
                i++;
            }

            foreach (string str in szWhiteSpace)
            {
                try
                {
                    m_xsltArg.AddParam("myArg" + str, szEmpty, "Test");
                }
                catch (System.Xml.XmlException)
                {
                    errCount++;
                }
                finally
                {
                    errCount--;
                }
            }

            if (errCount != 0)
            {
                _output.WriteLine("At least one whitespace test failed.");
                Assert.True(false);
            }

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Adding many objects")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam18(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test1
		2.Test2
		3.Test3
		4.Test4
		5.Test5
		6.Test6</result>";

            m_xsltArg = new XsltArgumentList();
            string obj = "Test";

            for (int i = 1; i < 7; i++)
            {
                m_xsltArg.AddParam("myArg" + +i, szEmpty, obj + i);
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj.ToString() != ("Test" + i))
                    Assert.True(false);
            }

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Add same object many times")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam19(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.Test1
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();
            string obj = "Test";

            for (int i = 0; i < 300; i++)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, obj + "1");
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj.ToString() != ("Test" + "1"))
                {
                    _output.WriteLine("Failed to add {0}", "myArg" + i);
                    Assert.True(false);
                }
                m_xsltArg.RemoveParam("myArg" + i, szEmpty);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj.ToString() != ("Test1"))
                Assert.True(false);
            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Using Different XSLT namespace")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam20(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj1=""urn:http://www.w3.org/1999/XSL/Transform"" xmlns:myObj2=""urn:tmp"" xmlns:myObj3=""urn:my-object"" xmlns:myObj4=""urn:MY-OBJECT"">
		1.Test1
		2.Test2
		3.Test3
		4.Test4
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", "urn:" + szXslNS, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", "urn:" + szXslNS);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                Assert.True(false);

            m_xsltArg.AddParam("myArg2", "urn:tmp", "Test2");
            retObj = m_xsltArg.GetParam("myArg2", "urn:tmp");
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);
            if (retObj.ToString() != "Test2")
                Assert.True(false);

            m_xsltArg.AddParam("myArg3", "urn:my-object", "Test3");
            retObj = m_xsltArg.GetParam("myArg3", "urn:my-object");
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test3", retObj);
            if (retObj.ToString() != "Test3")
                Assert.True(false);

            m_xsltArg.AddParam("myArg4", "urn:MY-OBJECT", "Test4");
            retObj = m_xsltArg.GetParam("myArg4", "urn:MY-OBJECT");
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test4", retObj);
            if (retObj.ToString() != "Test4")
                Assert.True(false);

            if ((LoadXSL("showParamNS.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Using Default XSLT namespace - Bug305503")]
        [InlineData()]
        [Theory]
        public void AddParam21()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("myArg1", szXslNS, "Test1");
            return;
        }

        //[Variation("Parameters should not be cached")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject32(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected1 = @"<?xml version=""1.0"" encoding=""utf-8""?><out>Param: first</out>";
            string expected2 = @"<?xml version=""1.0"" encoding=""utf-8""?><out>Param: second</out>";

            if (LoadXSL("test_Param.xsl", inputType, readerType) == 1)
            {
                m_xsltArg = new XsltArgumentList();
                m_xsltArg.AddParam("myParam1", szEmpty, "first");

                // Transform once
                if (Transform_ArgList("foo.xml", transformType, docType) == 1)
                {
                    VerifyResult(expected1);

                    m_xsltArg = new XsltArgumentList();
                    m_xsltArg.AddParam("myParam1", szEmpty, "second");

                    // Transform again to make sure that parameter from first transform are not cached
                    if (Transform_ArgList("foo.xml", transformType, docType) == 1)
                    {
                        VerifyResult(expected2);
                        return;
                    }
                }
            }
            Assert.True(false);
        }
    }

    /***************************************************************/
    /*               XsltArgumentList.AddParam Misc Tests          */
    /*Bug 268515 - Global param value is overridden by local value */
    /***************************************************************/

    //Testcases with Reader outputs are skipped because they don't write to an output file
    //[TestCase(Name = "XsltArgumentList - AddParam Misc : Reader, Stream", Desc = "READER,STREAM")]
    //[TestCase(Name = "XsltArgumentList - AddParam Misc : Reader, Writer", Desc = "READER,WRITER")]
    //[TestCase(Name = "XsltArgumentList - AddParam Misc : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    //[TestCase(Name = "XsltArgumentList - AddParam Misc : URI, Stream", Desc = "URI,STREAM")]
    //[TestCase(Name = "XsltArgumentList - AddParam Misc : URI, Writer", Desc = "URI,WRITER")]
    //[TestCase(Name = "XsltArgumentList - AddParam Misc : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    //[TestCase(Name = "XsltArgumentList - AddParam Misc : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    //[TestCase(Name = "XsltArgumentList - AddParam Misc : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    //[TestCase(Name = "XsltArgumentList - AddParam Misc : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CArgAddParamMisc : XsltApiTestCaseBase
    {
        private ITestOutputHelper _output;
        public CArgAddParamMisc(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //All the below variations, there is no parameter sent and default global value is set

        //global param is xsl:param local param is xsl:param
        //[Variation(id = 1, Pri = 2, Desc = "No param sent, global param used, local param exists with a default value", Params = new object[] { "AddParameterA1.xsl", "default local" })]
        [InlineData("AddParameterA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 2, Pri = 2, Desc = "No param sent, global param used, local param exists with no default value", Params = new object[] { "AddParameterA2.xsl", "" })]
        [InlineData("AddParameterA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 3, Pri = 2, Desc = "No param sent, global param used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterA3.xsl", "default global" })]
        [InlineData("AddParameterA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 4, Pri = 2, Desc = "No param sent, global param used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterA4.xsl", "with-param" })]
        [InlineData("AddParameterA4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 5, Pri = 2, Desc = "No param sent, global param used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterA5.xsl", "" })]
        [InlineData("AddParameterA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 6, Pri = 2, Desc = "No param sent, global param used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterA6.xsl", "default global" })]
        [InlineData("AddParameterA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 7, Pri = 2, Desc = "No param sent, global param used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterA7.xsl", "default global" })]
        [InlineData("AddParameterA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]

        //global param is xsl:variable local param is xsl:param
        //[Variation(id = 8, Pri = 2, Desc = "No param sent, global variable used, local param exists with a default value", Params = new object[] { "AddParameterDA1.xsl", "default local" })]
        [InlineData("AddParameterDA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 9, Pri = 2, Desc = "No param sent, global variable used, local param exists with no default value", Params = new object[] { "AddParameterDA2.xsl", "" })]
        [InlineData("AddParameterDA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 10, Pri = 2, Desc = "No param sent, global variable used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterDA3.xsl", "default global" })]
        [InlineData("AddParameterDA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 11, Pri = 2, Desc = "No param sent, global variable used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterDA4.xsl", "with-param" })]
        [InlineData("AddParameterDA4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 12, Pri = 2, Desc = "No param sent, global variable used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterDA5.xsl", "" })]
        [InlineData("AddParameterDA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 13, Pri = 2, Desc = "No param sent, global variable used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterDA6.xsl", "default global" })]
        [InlineData("AddParameterDA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 14, Pri = 2, Desc = "No param sent, global variable used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterDA7.xsl", "default global" })]
        [InlineData("AddParameterDA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]

        //global param is xsl:param local param is xsl:variable
        //[Variation(id = 15, Pri = 2, Desc = "No param sent, global param used, local variable exists with a default value", Params = new object[] { "AddParameterEA1.xsl", "default local" })]
        [InlineData("AddParameterEA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 16, Pri = 2, Desc = "No param sent, global param used, local variable exists with no default value", Params = new object[] { "AddParameterEA2.xsl", "" })]
        [InlineData("AddParameterEA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 17, Pri = 2, Desc = "No param sent, global param used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterEA3.xsl", "default global" })]
        [InlineData("AddParameterEA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 18, Pri = 2, Desc = "No param sent, global param used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterEA4.xsl", "default local" })]
        [InlineData("AddParameterEA4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 19, Pri = 2, Desc = "No param sent, global param used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterEA5.xsl", "" })]
        [InlineData("AddParameterEA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 20, Pri = 2, Desc = "No param sent, global param used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterEA6.xsl", "default global" })]
        [InlineData("AddParameterEA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 21, Pri = 2, Desc = "No param sent, global param used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterEA7.xsl", "default global" })]
        [InlineData("AddParameterEA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]

        //global param is xsl:variable local param is xsl:variable
        //[Variation(id = 22, Pri = 2, Desc = "No param sent, global variable used, local variable exists with a default value", Params = new object[] { "AddParameterFA1.xsl", "default local" })]
        [InlineData("AddParameterFA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 23, Pri = 2, Desc = "No param sent, global variable used, local variable exists with no default value", Params = new object[] { "AddParameterFA2.xsl", "" })]
        [InlineData("AddParameterFA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 24, Pri = 2, Desc = "No param sent, global variable used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterFA3.xsl", "default global" })]
        [InlineData("AddParameterFA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 25, Pri = 2, Desc = "No param sent, global variable used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterFA4.xsl", "default local" })]
        [InlineData("AddParameterFA4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 26, Pri = 2, Desc = "No param sent, global variable used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterFA5.xsl", "" })]
        [InlineData("AddParameterFA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 27, Pri = 2, Desc = "No param sent, global variable used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterFA6.xsl", "default global" })]
        [InlineData("AddParameterFA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 28, Pri = 2, Desc = "No param sent, global variable used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterFA7.xsl", "default global" })]
        [InlineData("AddParameterFA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFA7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam1(object param0, object param1, InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            m_xsltArg = new XsltArgumentList();
            string xslFile = param0.ToString();
            string expected = "<result>" + param1.ToString() + "</result>";

            if ((LoadXSL(xslFile, inputType, readerType) == 1) && (Transform_ArgList("AddParameter.xml", transformType, docType) == 1))
                VerifyResult(expected);
            else
                Assert.True(false);
        }

        //All the below variations, param is sent from client code

        //global param is xsl:param local param is xsl:param
        //[Variation(id = 29, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value", Params = new object[] { "AddParameterB1.xsl", "default local" })]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 30, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value", Params = new object[] { "AddParameterB2.xsl", "" })]
        [InlineData("AddParameterB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 31, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterB3.xsl", "outside param" })]
        [InlineData("AddParameterB3.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 32, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterB4.xsl", "with-param" })]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 33, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterB5.xsl", "" })]
        [InlineData("AddParameterB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 34, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterB6.xsl", "outside param" })]
        [InlineData("AddParameterB6.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 35, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterB7.xsl", "outside param" })]
        [InlineData("AddParameterB7.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]

        //global param is xsl:variable local param is xsl:param
        //[Variation(id = 36, Pri = 2, Desc = "Param sent, global variable used, local param exists with a default value", Params = new object[] { "AddParameterDB1.xsl", "default local" })]
        [InlineData("AddParameterDB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 37, Pri = 2, Desc = "Param sent, global variable used, local param exists with no default value", Params = new object[] { "AddParameterDB2.xsl", "" })]
        [InlineData("AddParameterDB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 38, Pri = 2, Desc = "Param sent, global variable used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterDB3.xsl", "default global" })]
        [InlineData("AddParameterDB3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 39, Pri = 2, Desc = "Param sent, global variable used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterDB4.xsl", "with-param" })]
        [InlineData("AddParameterDB4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 40, Pri = 2, Desc = "Param sent, global variable used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterDB5.xsl", "" })]
        [InlineData("AddParameterDB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 41, Pri = 2, Desc = "Param sent, global variable used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterDB6.xsl", "default global" })]
        [InlineData("AddParameterDB6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 42, Pri = 2, Desc = "Param sent, global variable used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterDB7.xsl", "default global" })]
        [InlineData("AddParameterDB7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterDB7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterDB7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterDB7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]

        //global param is xsl:param local param is xsl:variable
        //[Variation(id = 43, Pri = 2, Desc = "Param sent, global param used, local variable exists with a default value", Params = new object[] { "AddParameterEB1.xsl", "default local" })]
        [InlineData("AddParameterEB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 44, Pri = 2, Desc = "Param sent, global param used, local variable exists with no default value", Params = new object[] { "AddParameterEB2.xsl", "" })]
        [InlineData("AddParameterEB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 45, Pri = 2, Desc = "Param sent, global param used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterEB3.xsl", "outside param" })]
        [InlineData("AddParameterEB3.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB3.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB3.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB3.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB3.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB3.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB3.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB3.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB3.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 46, Pri = 2, Desc = "Param sent, global param used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterEB4.xsl", "default local" })]
        [InlineData("AddParameterEB4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 47, Pri = 2, Desc = "Param sent, global param used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterEB5.xsl", "" })]
        [InlineData("AddParameterEB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 48, Pri = 2, Desc = "Param sent, global param used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterEB6.xsl", "outside param" })]
        [InlineData("AddParameterEB6.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB6.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB6.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB6.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB6.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB6.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB6.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB6.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB6.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 49, Pri = 2, Desc = "Param sent, global param used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterEB7.xsl", "outside param" })]
        [InlineData("AddParameterEB7.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB7.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB7.xsl", "outside param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB7.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB7.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB7.xsl", "outside param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterEB7.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterEB7.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterEB7.xsl", "outside param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]

        //global param is xsl:variable local param is xsl:variable
        //[Variation(id = 50, Pri = 2, Desc = "Param sent, global variable used, local variable exists with a default value", Params = new object[] { "AddParameterFB1.xsl", "default local" })]
        [InlineData("AddParameterFB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 51, Pri = 2, Desc = "Param sent, global variable used, local variable exists with no default value", Params = new object[] { "AddParameterFB2.xsl", "" })]
        [InlineData("AddParameterFB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 52, Pri = 2, Desc = "Param sent, global variable used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterFB3.xsl", "default global" })]
        [InlineData("AddParameterFB3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB3.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB3.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB3.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 53, Pri = 2, Desc = "Param sent, global variable used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterFB4.xsl", "default local" })]
        [InlineData("AddParameterFB4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB4.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB4.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB4.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 54, Pri = 2, Desc = "Param sent, global variable used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterFB5.xsl", "" })]
        [InlineData("AddParameterFB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 55, Pri = 2, Desc = "Param sent, global variable used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterFB6.xsl", "default global" })]
        [InlineData("AddParameterFB6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB6.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB6.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB6.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 56, Pri = 2, Desc = "Param sent, global variable used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterFB7.xsl", "default global" })]
        [InlineData("AddParameterFB7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB7.xsl", "default global", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB7.xsl", "default global", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterFB7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterFB7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterFB7.xsl", "default global", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam2(object param0, object param1, InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string xslFile = param0.ToString();
            string expected = "<result>" + param1.ToString() + "</result>";

            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("param1", "", "outside param");

            if ((LoadXSL(xslFile, inputType, readerType) == 1) && (Transform_ArgList("AddParameter.xml", transformType, docType) == 1))
                VerifyResult(expected);
            else
                Assert.True(false);
        }

        //All the below variations, empty param is sent from client code
        //global param is xsl:param local param is xsl:param
        //[Variation(id = 57, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value", Params = new object[] { "AddParameterB1.xsl", "default local" })]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB1.xsl", "default local", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 58, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value", Params = new object[] { "AddParameterB2.xsl", "" })]
        [InlineData("AddParameterB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB2.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 59, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterB3.xsl", "" })]
        [InlineData("AddParameterB3.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB3.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 60, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterB4.xsl", "with-param" })]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB4.xsl", "with-param", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 61, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterB5.xsl", "" })]
        [InlineData("AddParameterB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB5.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 62, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterB6.xsl", "" })]
        [InlineData("AddParameterB6.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB6.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        //[Variation(id = 63, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterB7.xsl", "" })]
        [InlineData("AddParameterB7.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "", InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "", InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData("AddParameterB7.xsl", "", InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddParam3(object param0, object param1, InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string xslFile = param0.ToString();
            string expected = "<result>" + param1.ToString() + "</result>";

            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("param1", "", "");

            if ((LoadXSL(xslFile, inputType, readerType) == 1) && (Transform_ArgList("AddParameter.xml", transformType, docType) == 1))
                VerifyResult(expected);
            else
                Assert.True(false);
        }
    }

    /***********************************************************/
    /*          XsltArgumentList.AddExtensionObject            */
    /***********************************************************/

    //[TestCase(Name = "XsltArgumentList - AddExtensionObject : Reader , Reader", Desc = "READER,READER")]
    //[TestCase(Name = "XsltArgumentList - AddExtensionObject : Reader, Stream", Desc = "READER,STREAM")]
    //[TestCase(Name = "XsltArgumentList - AddExtensionObject : Reader, Writer", Desc = "READER,WRITER")]
    //[TestCase(Name = "XsltArgumentList - AddExtensionObject : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    ////[TestCase(Name="XsltArgumentList - AddExtensionObject : URI, Reader", Desc="URI,READER")]
    ////[TestCase(Name="XsltArgumentList - AddExtensionObject : URI, Stream", Desc="URI,STREAM")]
    ////[TestCase(Name="XsltArgumentList - AddExtensionObject : URI, Writer", Desc="URI,WRITER")]
    ////[TestCase(Name="XsltArgumentList - AddExtensionObject : URI, TextWriter", Desc="URI,TEXTWRITER")]
    //[TestCase(Name = "XsltArgumentList - AddExtensionObject : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    //[TestCase(Name = "XsltArgumentList - AddExtensionObject : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    //[TestCase(Name = "XsltArgumentList - AddExtensionObject : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    //[TestCase(Name = "XsltArgumentList - AddExtensionObject : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CArgAddExtObj : XsltApiTestCaseBase
    {
        private ITestOutputHelper _output;
        public CArgAddExtObj(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation(Desc = "Basic Verification Test", Pri = 0)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject1(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		1.Test1
		2.Test2
		3.Test3</result>";

            MyObject obj = new MyObject(1, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("namespace System.Xml.Tests = null")]
        [InlineData()]
        [Theory]
        public void AddExtObject2()
        {
            MyObject obj = new MyObject(2, _output);
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddExtensionObject(null, obj);
            }
            catch (System.ArgumentNullException)
            {
                return;
            }
            _output.WriteLine("System.ArgumentNullException not generated for null namespace System.Xml.Tests");
            Assert.True(false);
        }

        //[Variation("namespace System.Xml.Tests is empty string - Bug 200998")]
        [InlineData()]
        [Theory]
        public void AddExtObject3()
        {
            MyObject obj = new MyObject(3, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szEmpty, obj);
            return;       //shouldn't throw exception as per bug 200998
        }

        //[Variation("Very long namespace System.Xml.Tests")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject4(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""http://www.miocrosoft.com/this/is/a/very/long/namespace/uri/to/do/the/api/testing/for/xslt/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/"">
		1.Test1
		2.Test2
		3.Test3</result>";

            m_xsltArg = new XsltArgumentList();
            MyObject obj = new MyObject(4, _output);

            m_xsltArg.AddExtensionObject(szLongNS, obj);

            if ((LoadXSL("myObjectLongNS.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Different Data Types")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject6(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>

		String  Argument: System.String
		Int32   Argument: System.Int32
		Boolean Argument: System.Boolean
		Boolean Argument: System.Boolean
		Double  Argument: System.Double
		String  Argument: System.String</result>";

            m_xsltArg = new XsltArgumentList();
            string obj = "0.00";

            // string
            m_xsltArg.AddExtensionObject("myArg1", obj);
            retObj = m_xsltArg.GetExtensionObject("myArg1");
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "0.00", retObj);
            if (retObj.ToString() != "0.00")
            {
                _output.WriteLine("Failed to add/get a value for {0} of type {1}", "0.00", "string");
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            int i = 8;

            m_xsltArg.AddExtensionObject("myArg2", i);
            retObj = m_xsltArg.GetExtensionObject("myArg2");
            _output.WriteLine("Added Value:{0}\nRetrieved Value:{1}", i, retObj);
            if (!i.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0} with conversion from int to double", i);
                _output.WriteLine("Retrieved: {0}", retObj.ToString());
                Assert.True(false);
            }

            //must also be same instance!!!
            if (i != (int)retObj)
                Assert.True(false);

            bool bF = (1 == 0);

            m_xsltArg.AddExtensionObject("myArg3", bF);
            retObj = m_xsltArg.GetExtensionObject("myArg3");
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bF.ToString(), retObj);
            if (!bF.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0} of type {1}", bF.ToString(), "boolean");
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            bool bT = (1 == 1);

            m_xsltArg.AddExtensionObject("myArg4", bT);
            retObj = m_xsltArg.GetExtensionObject("myArg4");
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bT.ToString(), retObj);
            if (!bT.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0} of type {1}", bT.ToString(), "boolean");
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            double d = 3.14;

            m_xsltArg.AddExtensionObject("myArg5", d);
            retObj = m_xsltArg.GetExtensionObject("myArg5");
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d.ToString(), retObj);
            if (!d.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0} of type {1}", bT.ToString(), "boolean");
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            m_xsltArg.AddExtensionObject("myArg6", "3");
            retObj = m_xsltArg.GetExtensionObject("myArg6");
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bT.ToString(), retObj);

            if ((LoadXSL("MyObject_DataTypes.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Same Namespace different objects")]
        [InlineData()]
        [Theory]
        public void AddExtObject7()
        {
            MyObject obj1 = new MyObject(1, _output);
            MyObject obj2 = new MyObject(2, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj1);
            try
            {
                m_xsltArg.AddExtensionObject(szDefaultNS, obj2);
            }
            catch (System.ArgumentException)
            {
                return;
            }
            _output.WriteLine("Did not launch exception 'System.ArgumentException' for an item already added");
            Assert.True(false);
        }

        //[Variation("Case sensitivity")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject8(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		1.Test1
		2.Test2
		3.Test3</result>";

            MyObject obj = new MyObject(1, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject("urn:my-object", obj);

            retObj = m_xsltArg.GetExtensionObject("urn:my-object");
            if (((MyObject)retObj).MyValue() != obj.MyValue())
            {
                _output.WriteLine("Set and retrieved value appear to be different");
                Assert.True(false);
            }
            m_xsltArg.AddExtensionObject("URN:MY-OBJECT", obj);
            m_xsltArg.AddExtensionObject("urn:My-Object", obj);
            m_xsltArg.AddExtensionObject("urn-my:object", obj);

            if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Set a null object")]
        [InlineData()]
        [Theory]
        public void AddExtObject9()
        {
            MyObject obj = new MyObject(9, _output);
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddExtensionObject(szDefaultNS, null);
            }
            catch (System.ArgumentNullException)
            {
                return;
            }
            _output.WriteLine("Did not launch exception 'System.ArgumentNullException' for adding a null-valued item");
            Assert.True(false);
        }

        //[Variation("Unitialized and NULL return values from the methods in the extension object")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject10(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">

		Test1
		Test2: 0</result>";

            MyObject obj = new MyObject(10, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_Null.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Add many objects")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject11(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		1.Test1
		2.Test2
		3.Test3</result>";

            m_xsltArg = new XsltArgumentList();

            MyObject obj1 = new MyObject(100, _output);
            m_xsltArg.AddExtensionObject(szDefaultNS, obj1);

            for (int i = 1; i < 500; i++)
            {
                MyObject obj = new MyObject(i, _output);
                m_xsltArg.AddExtensionObject(szDefaultNS + i, obj);
            }
            if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Whitespace")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject12(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            int i = 1;
            m_xsltArg = new XsltArgumentList();

            foreach (string str in szWhiteSpace)
            {
                MyObject obj = new MyObject(i, _output);
                m_xsltArg.AddExtensionObject(szDefaultNS + str, obj);
                i++;
            }
            try
            {
                if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1))
                    Transform_ArgList("fruits.xml", true, transformType, docType);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return;
            }
            _output.WriteLine("Did not throw expected exception");
            Assert.True(false);
        }

        //[Variation("Add object many times")]
        [InlineData()]
        [Theory]
        public void AddExtObject13()
        {
            MyObject obj = new MyObject(13, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            try
            {
                m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            }
            catch (System.ArgumentException)
            {
                return;
            }
            _output.WriteLine("Did not exception for adding an extension object that already exists");
            Assert.True(false);
        }

        //[Variation("Add and Remove multiple times")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject14(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		1.Test1
		2.Test2
		3.Test3</result>";

            MyObject obj = new MyObject(14, _output);
            m_xsltArg = new XsltArgumentList();

            for (int i = 0; i < 400; i++)
            {
                m_xsltArg.AddExtensionObject(szDefaultNS, obj);
                m_xsltArg.RemoveExtensionObject(szDefaultNS);
            }
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);

            if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Namespace URI non-existent")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject15(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            MyObject obj = new MyObject(15, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szSimple, obj);
            try
            {
                if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1))
                    Transform_ArgList("fruits.xml", true, transformType, docType);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return;
            }
            _output.WriteLine("Did not throw expected exception");
            Assert.True(false);
        }

        //[Variation("Accessing Private and protected Items")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject16(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            MyObject obj = new MyObject(1, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            try
            {
                LoadXSL("MyObject_PrivateAccess.xsl", inputType, readerType);
                Transform_ArgList("fruits.xml", true, transformType, docType);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                try
                {
                    LoadXSL("MyObject_ProtectedAccess.xsl", inputType, readerType);
                    Transform_ArgList("fruits.xml", true, transformType, docType);
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    try
                    {
                        LoadXSL("MyObject_DefaultAccess.xsl", inputType, readerType);
                        Transform_ArgList("fruits.xml", true, transformType, docType);
                    }
                    catch (System.Xml.Xsl.XsltException)
                    {
                        return;
                    }
                }
            }
            Assert.True(false);
        }

        //[Variation("Writing To Output")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject17(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		Here:End
		</result>";

            MyObject obj = new MyObject(17, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_ConsoleWrite.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Recursive Functions")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject18(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		Recursive Function Returning the factorial of five:120</result>";

            MyObject obj = new MyObject(18, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_Recursion.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Overloaded Functions")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject19(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		Overloaded Double: Int Overlaod
		Overloaded Int: Int Overlaod
		Overloaded String: String Overlaod</result>";

            MyObject obj = new MyObject(19, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_Overloads.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Function-exists tests")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject20(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		DoNothing Function Test Pass
		Construtor Function
		Return Int  Function Test Pass
		Return String Function Test Pass
		ReturnInt  Function Test Pass
		Taking in args  Test Pass
		Public Function Test Pass
		Protected Function
		Private Function
		Default Function</result>";

            MyObject obj = new MyObject(20, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_FnExists.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Argument Tests")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject21(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">

		String  Argument: Received a string with value: Hello
		Double  Argument: Received a double with value 3.14
		Boolean Argument: Statement is True
		Boolean True Argument: Statement is False</result>";

            MyObject obj = new MyObject(1, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_Arguments.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Methods returning void and valid types")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject22(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		Get A String:Hello world
		Get A Double:22.41276
		Get A True Boolean:true
		Get A False Boolean:false
		Get An Int:10
		Get Other with ToString() Support:My Custom Object has a value of 22
		Call function with no return type:</result>";

            MyObject obj = new MyObject(22, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_ReturnValidTypes.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Multiple Objects in same NameSpace")]
        [InlineData()]
        [Theory]
        public void AddExtObject24()
        {
            m_xsltArg = new XsltArgumentList();

            double d = 1;
            int i = 1;

            m_xsltArg.AddExtensionObject("urn:myspace", d);
            try
            {
                m_xsltArg.AddExtensionObject("urn:myspace", i);
            }
            catch (System.ArgumentException)
            {
                return;
            }
            _output.WriteLine("Exception not thrown for URI namespace System.Xml.Tests in use");
            Assert.True(false);
        }

        //[Variation("Case Sensitivity")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void TC_ExtensionObj_Function_Mismatch_IncorrectCasing(InputType xslInputType, ReaderType readerType, TransformType outputType, DocType navType)
        {
            MyObject obj = new MyObject(25, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            LoadXSL("MyObject_CaseSensitive.xsl", xslInputType, readerType);
            var e = Assert.Throws<XsltException>(() => Transform_ArgList("fruits.xml", outputType, navType));
            var exceptionSourceAssembly = "System.Xml";
            CheckExpectedError(e, exceptionSourceAssembly, "Xslt_UnknownXsltFunction", new[] { "FN3" });
        }

        //[Variation("Object namespace System.Xml.Tests found")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject26(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            MyObject obj = new MyObject(26, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_NotFoundNS.xsl", inputType, readerType) == 1))
            {
                try
                {
                    Transform_ArgList("fruits.xml", true, transformType, docType);
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    return;
                }
            }
            _output.WriteLine("Exception not thrown for NS not found");
            Assert.True(false);
        }

        //[Variation("Maintaining State")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject27(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		A:27
		B:23
		C:23
		D:23
		E:37
		F:Hello 
		G:Hello World 
		E:-13</result>";

            MyObject obj = new MyObject(27, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_KeepingState.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Deliberately Messing Up the Stylesheet")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject28(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">

		Aiming with Gun: &gt;"" $tmp &gt;;'	 
&amp;
		Aiming with Missile: &lt;xsl:variable name=""tmp""/&gt;
		Aiming with Nuclear: &lt;/xsl:stylesheet&gt;
		Wow...survived all killer ammo.
	</result>";

            MyObject obj = new MyObject(28, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_KillerStrings.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Function not found in Object")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject29(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            MyObject obj = new MyObject(29, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_NotFound.xsl", inputType, readerType) == 1))
            {
                try
                {
                    Transform_ArgList("fruits.xml", true, transformType, docType);
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    return;
                }
            }
            _output.WriteLine("Exception not thrown for method not found");
            Assert.True(false);
        }

        //[Variation("Using Default XSLT namespace -  Bug305503")]
        [InlineData()]
        [Theory]
        public void AddExtObject31()
        {
            MyObject obj = new MyObject(31, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szXslNS, obj);
            return;
        }

        //[Variation("Extension objects should not be cached during Transform()")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void AddExtObject32(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected1 = @"<?xml version=""1.0"" encoding=""utf-8""?><out xmlns:id=""id"" xmlns:cap=""capitalizer"">
			ID: first
			Capitalized ID: FIRST</out>";

            string expected2 = @"<?xml version=""1.0"" encoding=""utf-8""?><out xmlns:id=""id"" xmlns:cap=""capitalizer"">
			ID: second
			Capitalized ID: SECOND</out>";

            if (LoadXSL("Bug78587.xsl", inputType, readerType) == 1)
            {
                m_xsltArg = new XsltArgumentList();
                m_xsltArg.AddExtensionObject("id", new Id("first"));
                m_xsltArg.AddExtensionObject("capitalizer", new Capitalizer());

                // Transform once
                if (Transform_ArgList("Bug78587.xml", transformType, docType) == 1)
                {
                    VerifyResult(expected1);

                    m_xsltArg = new XsltArgumentList();
                    m_xsltArg.AddExtensionObject("id", new Id("second"));
                    m_xsltArg.AddExtensionObject("capitalizer", new Capitalizer());

                    // Transform again to make sure that extension objects from first transform are not cached
                    if (Transform_ArgList("Bug78587.xml", transformType, docType) == 1)
                    {
                        VerifyResult(expected2);
                        return;
                    }
                }
            }
            Assert.True(false);
        }
    }

    /***********************************************************/
    /*            XsltArgumentList.RemoveParam                 */
    /***********************************************************/

    //[TestCase(Name = "XsltArgumentList - RemoveParam : Reader , Reader", Desc = "READER,READER")]
    ////[TestCase(Name="XsltArgumentList - RemoveParam : URI, Stream", Desc="URI,STREAM")]
    //[TestCase(Name = "XsltArgumentList - RemoveParam : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    //[TestCase(Name = "XsltArgumentList - RemoveParam : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CArgRemoveParam : XsltApiTestCaseBase
    {
        private ITestOutputHelper _output;
        public CArgRemoveParam(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation(Desc = "Basic Verification Test", Pri = 0)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam1(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test1
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test2");
            m_xsltArg.RemoveParam("myArg1", szEmpty);
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Value of Removed Object is not null : {0}", retObj);
                Assert.True(false);
            }
            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);

            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
            {
                _output.WriteLine("Value of removed object is not as expected : {0}", retObj);
                Assert.True(false);
            }

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Param name is null")]
        [InlineData()]
        [Theory]
        public void RemoveParam2()
        {
            m_xsltArg = new XsltArgumentList();
            retObj = m_xsltArg.RemoveParam(null, szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Did not return NULL for null parameter name");
                Assert.True(false);
            }
            return;
        }

        //[Variation("Param name is empty string")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam3(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();
            m_xsltArg.RemoveParam(szEmpty, szEmpty);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Param name is non-existent")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam4(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();
            m_xsltArg.RemoveParam(szSimple, szEmpty);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Invalid Param name")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam5(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();
            m_xsltArg.RemoveParam(szInvalid, szEmpty);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Very long param name")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam6(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam(szLongString, szEmpty, "Test1");
            m_xsltArg.RemoveParam(szLongString, szEmpty);

            if ((LoadXSL("showParamLongName.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Namespace URI is null")]
        [InlineData()]
        [Theory]
        public void RemoveParam7()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.RemoveParam("myArg1", null);
            if (retObj != null)
            {
                _output.WriteLine("Did not return NULL for null URI namespace");
                Assert.True(false);
            }
            return;
        }

        //[Variation("Namespace URI is empty string")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam8(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myArg1", szEmpty);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Namespace URI is non-existent")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam9(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test1
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myArg1", "http://www.xsltTest.com");

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Very long namespace System.Xml.Tests")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam10(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szLongString, "Test1");
            m_xsltArg.RemoveParam("myArg1", szLongString);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Different Data Types")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam11(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            double d1 = double.PositiveInfinity;
            double d2 = double.NegativeInfinity;
            double d3 = double.NaN;
            double d4 = 2.000001;
            double d5 = 0.00;
            double d6 = double.MaxValue;
            double d7 = double.MinValue;

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, d1);
            m_xsltArg.RemoveParam("myArg1", szEmpty);
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", d1);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, d2);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", d2);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg3", szEmpty, d3);
            m_xsltArg.RemoveParam("myArg3", szEmpty);
            retObj = m_xsltArg.GetParam("myArg3", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", d3);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg4", szEmpty, d4);
            m_xsltArg.RemoveParam("myArg4", szEmpty);
            retObj = m_xsltArg.GetParam("myArg4", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", d4);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg5", szEmpty, d5);
            m_xsltArg.RemoveParam("myArg5", szEmpty);
            retObj = m_xsltArg.GetParam("myArg5", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", d5);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg6", szEmpty, d6);
            m_xsltArg.RemoveParam("myArg6", szEmpty);
            retObj = m_xsltArg.GetParam("myArg6", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", d6);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg7", szEmpty, d7);
            m_xsltArg.RemoveParam("myArg7", szEmpty);
            retObj = m_xsltArg.GetParam("myArg7", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", d7);
                Assert.True(false);
            }

            string obj = "0.00";

            // string
            m_xsltArg.AddParam("myArg1", szEmpty, obj);
            m_xsltArg.RemoveParam("myArg1", szEmpty);
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", obj);
                Assert.True(false);
            }

            //int
            int i = 2;

            m_xsltArg.AddParam("myArg2", szEmpty, i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            bool bF = (1 == 0);
            m_xsltArg.AddParam("myArg4", szEmpty, bF);
            m_xsltArg.RemoveParam("myArg4", szEmpty);
            retObj = m_xsltArg.GetParam("myArg4", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", bF);
                Assert.True(false);
            }

            bool bT = (1 == 1);
            m_xsltArg.AddParam("myArg5", szEmpty, bT);
            m_xsltArg.RemoveParam("myArg5", szEmpty);
            retObj = m_xsltArg.GetParam("myArg5", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", bT);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (short)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (ushort)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (int)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (uint)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (long)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (ulong)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (float)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (decimal)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Case Sensitivity")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam12(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test1
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myarg1", szEmpty);
            m_xsltArg.RemoveParam("MYARG1", szEmpty);
            m_xsltArg.RemoveParam("myArg1 ", szEmpty);

            // perform a transform for kicks and ensure all is ok.
            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Whitespace")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam13(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test
		2.Test
		3.Test
		4.Test
		5.Test
		6.No Value Specified</result>";

            int i = 1;
            m_xsltArg = new XsltArgumentList();

            foreach (string str in szWhiteSpace)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, "Test" + str);
                m_xsltArg.RemoveParam("myArg" + i, szEmpty);
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj != null)
                {
                    _output.WriteLine("Error removing case #{0} from this test", i);
                    Assert.True(false);
                }
                i++;
            }

            i = 1;
            foreach (string str in szWhiteSpace)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, "Test"); // dont add whitespace to name here since addparam would throw
                m_xsltArg.RemoveParam("myArg" + str, szEmpty);
                retObj = m_xsltArg.GetParam("myArg" + str, szEmpty);
                if (retObj != null)
                {
                    _output.WriteLine("Error removing case #{0} in the second batch from this test", i);
                    Assert.True(false);
                }
                i++;
            }

            // perform a transform for kicks and ensure all is ok.
            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Call Multiple Times")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Writer, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [Theory]
        public void RemoveParam14(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myArg1", szEmpty);
            for (int i = 0; i < 500; i++)
                m_xsltArg.RemoveParam("myArg1", szEmpty);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Using Default XSLT Namespace - Bug305503")]
        [InlineData()]
        [Theory]
        public void RemoveParam15()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.RemoveParam("myParam", szDefaultNS);

            return;
        }
    }

    /***********************************************************/
    /*        XslTransform.RemoveExtensionObject               */
    /***********************************************************/

    //[TestCase(Name = "XsltArgumentList - RemoveExtensionObject : Reader, Stream", Desc = "READER,STREAM")]
    //[TestCase(Name = "XsltArgumentList - RemoveExtensionObject : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    ////[TestCase(Name="XsltArgumentList - RemoveExtensionObject : URI, Reader", Desc="URI,READER")]
    //[TestCase(Name = "XsltArgumentList - RemoveExtensionObject : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    public class CArgRemoveExtObj : XsltApiTestCaseBase
    {
        private ITestOutputHelper _output;
        public CArgRemoveExtObj(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation(Desc = "Basic Verification Test", Pri = 0)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void RemoveExtObj1(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            MyObject obj = new MyObject(1, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.RemoveExtensionObject(szDefaultNS);

            try
            {
                if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1))
                    Transform_ArgList("fruits.xml", true, transformType, docType);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return;
            }
            _output.WriteLine("Did not throw expected exception");
            Assert.True(false);
        }

        //[Variation("Namespace URI is null")]
        [InlineData()]
        [Theory]
        public void RemoveExtObj2()
        {
            MyObject obj = new MyObject(2, _output);
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.RemoveExtensionObject(null);
            }
            catch (System.ArgumentNullException)
            {
                return;
            }
            _output.WriteLine("Exception not generated for null parameter name");
            Assert.True(false);
        }

        //[Variation("Call Multiple Times")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void RemoveExtObj3(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            MyObject obj = new MyObject(10, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);

            for (int i = 0; i < 500; i++)
                m_xsltArg.RemoveExtensionObject(szDefaultNS);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Namespace URI is non-existent")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void RemoveExtObj4(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		1.Test1
		2.Test2
		3.Test3</result>";

            MyObject obj = new MyObject(4, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.RemoveExtensionObject(szSimple);

            if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Very long namespace System.Xml.Tests")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void RemoveExtObj5(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            m_xsltArg = new XsltArgumentList();
            MyObject obj = new MyObject(5, _output);

            m_xsltArg.AddExtensionObject("urn:" + szLongNS, obj);
            m_xsltArg.RemoveExtensionObject("urn:" + szLongNS);

            try
            {
                if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1))
                    Transform_ArgList("fruits.xml", true, transformType, docType);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return;
            }
            _output.WriteLine("Did not throw expected exception");
            Assert.True(false);
        }

        //[Variation("Different Data Types")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void RemoveExtObj6(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            MyObject obj = new MyObject(6, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject("urn:my-object", obj);
            m_xsltArg.RemoveExtensionObject("urn:my-object");

            m_xsltArg.AddExtensionObject("urn:my-object", 2);
            m_xsltArg.RemoveExtensionObject("urn:my-object");

            m_xsltArg.AddExtensionObject("urn:my-object", "Test String");
            m_xsltArg.RemoveExtensionObject("urn:my-object");

            m_xsltArg.AddExtensionObject("urn:my-object", (double)5.1);
            m_xsltArg.RemoveExtensionObject("urn:my-object");

            m_xsltArg.AddExtensionObject("urn:my-object", true);
            m_xsltArg.RemoveExtensionObject("urn:my-object");

            m_xsltArg.AddExtensionObject("urn:my-object", false);
            m_xsltArg.RemoveExtensionObject("urn:my-object");

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Case Sensitivity")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void RemoveExtObj7(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result xmlns:myObj=""urn:my-object"">
		1.Test1
		2.Test2
		3.Test3</result>";

            MyObject obj = new MyObject(7, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject("urn:my-object", obj);

            m_xsltArg.RemoveExtensionObject("URN:MY-OBJECT");
            m_xsltArg.RemoveExtensionObject("urn:My-Object");
            m_xsltArg.RemoveExtensionObject("urn-my:object");
            m_xsltArg.RemoveExtensionObject("urn:my-object ");

            if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Whitespace")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void RemoveExtObj8(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            int i = 1;
            m_xsltArg = new XsltArgumentList();

            foreach (string str in szWhiteSpace)
            {
                MyObject obj = new MyObject(i, _output);

                m_xsltArg.AddExtensionObject(szDefaultNS + str, obj);
                m_xsltArg.RemoveExtensionObject(szDefaultNS + str);
                retObj = m_xsltArg.GetExtensionObject(szDefaultNS + str);
                if (retObj != null)
                {
                    _output.WriteLine("Error deleting case #{0} for whitespace arg", i);
                    Assert.True(false);
                }
                i++;
            }

            try
            {
                if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1))
                    Transform_ArgList("fruits.xml", true, transformType, docType);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return;
            }
            _output.WriteLine("Did not exception for object that could not be executed");
            Assert.True(false);
        }

        //[Variation("Using default XSLT namespace - Bug305503")]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.TextWriter, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Reader, DocType.XPathDocument)]
        [InlineData(InputType.Navigator, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void RemoveExtObj9(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            MyObject obj = new MyObject(10, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.RemoveExtensionObject(szDefaultNS);

            // ensure we can still do a transform
            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }
    }

    /***********************************************************/
    /*        XslTransform.Clear                               */
    /***********************************************************/

    //[TestCase(Name = "XsltArgumentList - Clear", Desc = "XsltArgumentList.Clear")]
    public class CArgClear : XsltApiTestCaseBase
    {
        private ITestOutputHelper _output;
        public CArgClear(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation(Desc = "Basic Verification Test", Pri = 0)][InlineData(InputType.Reader, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void Clear1(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return; //return TEST_SKIPPED;

            m_xsltArg.Clear();
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
                Assert.True(false);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Clear with nothing loaded")]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void Clear2(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.Clear();
            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Clear Params")]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void Clear3(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return; //return TEST_SKIPPED;

            m_xsltArg.Clear();
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
                Assert.True(false);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Clear Extension Objects")]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void Clear4(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            MyObject obj = new MyObject(26, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.Clear();
            retObj = m_xsltArg.GetExtensionObject(szDefaultNS);
            if (retObj != null)
            {
                _output.WriteLine("Did not appear to clear an extension object");
                Assert.True(false);
            }

            if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1))
            {
                try
                {
                    Transform_ArgList("fruits.xml", transformType, docType);
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    return;
                }
            }
            _output.WriteLine("Exception not thrown for NS not found");
            Assert.True(false);
        }

        //[Variation("Clear Many Objects")]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void Clear5(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();
            string obj = "Test";

            for (int i = 0; i < 200; i++)
            {
                m_xsltArg.AddParam("myArg2", szEmpty, obj + i);
                retObj = m_xsltArg.GetParam("myArg2", szEmpty);
                if (retObj.ToString() != (obj + i))
                {
                    _output.WriteLine("Failed to add/remove iteration {0}", i);
                    _output.WriteLine("{0} : {1}", retObj, obj + i);

                    Assert.True(false);
                }
                m_xsltArg.Clear();
            }

            for (int i = 0; i < 200; i++)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, obj + i);
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj.ToString() != (obj + i))
                {
                    _output.WriteLine("Failed in 2nd part to add/remove iteration {0}", i);
                    Assert.True(false);
                }
            }

            m_xsltArg.Clear();

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Clear Multiple Times")]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void Clear6(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return; //return TEST_SKIPPED;

            for (int i = 0; i < 300; i++)
                m_xsltArg.Clear();
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
                Assert.True(false);

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Loading one object, but clearing another")]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void Clear7(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.Test1
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();
            XsltArgumentList m_2 = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return; //return TEST_SKIPPED;

            m_2.Clear();

            if ((LoadXSL("showParam.xsl", inputType, readerType) == 1) && (Transform_ArgList("fruits.xml", transformType, docType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Clear after objects have been \"Removed\"")]
        [InlineData(InputType.URI, ReaderType.XmlValidatingReader, TransformType.Stream, DocType.XPathDocument)]
        [Theory]
        public void Clear8(InputType inputType, ReaderType readerType, TransformType transformType, DocType docType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>
		1.No Value Specified
		2.No Value Specified
		3.No Value Specified
		4.No Value Specified
		5.No Value Specified
		6.No Value Specified</result>";

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return; //return TEST_SKIPPED;
            retObj = m_xsltArg.RemoveParam("myArg1", szEmpty);
            m_xsltArg.Clear();

            if ((LoadXSL("showParam.xsl", inputType, readerType) != 1) || (Transform_ArgList("fruits.xml", transformType, docType) != 1))
            Assert.True(false);

            VerifyResult(expected);

            MyObject obj = new MyObject(26, _output);

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.RemoveExtensionObject(szDefaultNS);
            m_xsltArg.Clear();

            if ((LoadXSL("myObjectDef.xsl", inputType, readerType) == 1))
            {
                try
                {
                    Transform_ArgList("fruits.xml", transformType, docType);
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    return;
                }
            }
            _output.WriteLine("Exception not thrown for NS not found");
            Assert.True(false);
        }
    }
}
