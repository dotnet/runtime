// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
//using System.Dynamic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace System.Xml.Tests
{
    /***********************************************************/
    /*               XsltArgumentList.GetParam                 */
    /***********************************************************/

    //[TestCase(Name = "XsltArgumentList - GetParam", Desc = "Get Param Test Cases")]
    public class CArgIntegrity : XsltApiTestCaseBase
    {
        private ITestOutputHelper _output;
        public CArgIntegrity(ITestOutputHelper output) : base(output)
        {
            _output = output;
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

        private static void WriteFiles(string input, string output)
        {
            using (XmlWriter w = XmlWriter.Create(output))
            {
                using (XmlReader r = XmlReader.Create(new StringReader(input)))
                {
                    w.WriteNode(r, false);
                }
            }
        }

        private static void WriteXmlAndXslFiles()
        {
            WriteFiles(s_typeXml, "type.xml");
            WriteFiles(s_typeXsl, "type.xsl");
        }

        //[Variation(Desc = "Tuple.XsltArgumentList.AddParam/AddExtensionObject", Param = 1)]
        [InlineData(1)]
        //[Variation(Desc = "DynamicObject.XsltArgumentList.AddParam/AddExtensionObject", Param = 2)]
        //[InlineData(2)]
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
                //case 2: t = new TestDynamicObject(); break;
                case 3: t = new Guid(); break;
                case 4: t = new Dictionary<string, object>(); break;
            }
            _output.WriteLine(t.ToString());

#pragma warning disable 0618
            XslTransform xslt = new XslTransform();
#pragma warning restore 0618
            xslt.Load("type.xsl");

            XsltArgumentList xslArg = new XsltArgumentList();
            xslArg.AddParam("param", "", t);
            xslArg.AddExtensionObject("", t);

            XPathDocument xpathDom = new XPathDocument("type.xml");
            using (StringWriter sw = new StringWriter())
            {
                xslt.Transform(xpathDom, xslArg, sw);
                _output.WriteLine(sw.ToString());
            }
            return;
        }

        //public class TestDynamicObject : DynamicObject
        //{
        //    public dynamic GetDynamicObject()
        //    {
        //        return new Dictionary<string, object>();
        //    }
        //}

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

        /*
         * This test is no more valid.
         * MDAC Bug # 88407 fix now allows ANY characters in URI
        //[Variation("Invalid Namespace URI")]
        [InlineData()]
        [Theory]
        public void GetParam11()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam("myArg1", szInvalid, "Test11");
            }
            catch (System.UriFormatException)
            {
                return;
            }
            _output.WriteLine("Did not throw System.UriFormatException for invalid namespace System.Xml.Tests");
            Assert.True(false);
        }
        */

        //[Variation("Different Data Types")]
        [InlineData()]
        [Theory]
        public void GetParam12()
        {
            m_xsltArg = new XsltArgumentList();
            String obj = "0.00";

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

            Boolean bF = (1 == 0);
            m_xsltArg.AddParam("myArg3", szEmpty, bF);
            retObj = m_xsltArg.GetParam("myArg3", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bF.ToString(), retObj);
            if (!bF.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0} of type {1}", bF.ToString(), "boolean");
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            Boolean bT = (1 == 1);
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

            foreach (String str in szWhiteSpace)
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

            foreach (String str in szWhiteSpace)
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
    /*      XsltArgumentList.GetExtensionObject                */
    /***********************************************************/

    // TODO: Fix security issue
    ////[TestCase(Name="XsltArgumentList - GetExtensionObject", Desc="XsltArgumentList.GetExtensionObject")]

    //public class CArgGetExtObj : XsltApiTestCaseBase
    //{
    //    //[Variation(Desc="Basic Verification Test",Pri=0)]
    //    [InlineData()]
    //    public int GetExtObject1()
    //    {
    //        MyObject obj = new MyObject(1);
    //        m_xsltArg = new XsltArgumentList();

    //        m_xsltArg.AddExtensionObject(szDefaultNS, obj);
    //        retObj = m_xsltArg.GetExtensionObject(szDefaultNS);

    //        _output.WriteLine("Retrieved value: {0}", ((MyObject)retObj).MyValue());
    //        if(((MyObject)retObj).MyValue() != obj.MyValue())
    //        {
    //            _output.WriteLine("Set and retrieved value appear to be different");
    //            Assert.True(false);
    //        }

    //        if((LoadXSL("myObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
    //            (CheckResult(430.402026847)== TEST_PASS))
    //            return;
    //        else
    //            Assert.True(false);
    //    }

    //    //[Variation("Namespace URI = null")]
    //    [InlineData()]
    //    public int GetExtObject2()
    //    {
    //        m_xsltArg = new XsltArgumentList();

    //        try
    //        {
    //            m_xsltArg.GetExtensionObject(null);
    //        }
    //        catch(System.ArgumentNullException)
    //        {
    //            return;
    //        }
    //        _output.WriteLine("ArgumentNullException not thrown for null namespace System.Xml.Tests");
    //        return;
    //    }

    //    //[Variation("Namespace URI is empty string")]
    //    [InlineData()]
    //    public int GetExtObject3()
    //    {
    //        m_xsltArg = new XsltArgumentList();

    //        try
    //        {
    //            retObj = m_xsltArg.GetExtensionObject(szEmpty);
    //        }
    //        catch(Exception e)
    //        {
    //            _output.WriteLine(e.ToString());
    //            Assert.True(false);
    //        }

    //        if((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
    //            (CheckResult(466.5112789241)== TEST_PASS))
    //            return;
    //        else
    //            Assert.True(false);
    //    }

    //    //[Variation("Namespace URI non-existent")]
    //    [InlineData()]
    //    public int GetExtObject4()
    //    {
    //        m_xsltArg = new XsltArgumentList();

    //        retObj = m_xsltArg.GetExtensionObject(szDefaultNS);

    //        if(retObj != null)
    //        {
    //            _output.WriteLine("Did not return a NULL value for a non-existent URI");
    //            Assert.True(false);
    //        }
    //        try
    //        {
    //            if((LoadXSL("myObjectDef.xsl") == TEST_PASS))
    //                Transform_ArgList("fruits.xml");
    //        }
    //        catch(System.Xml.Xsl.XsltException)
    //        {
    //            return;
    //        }
    //        _output.WriteLine("Did not throw exception for an invalid transform");
    //        Assert.True(false);
    //    }

    //    //[Variation("Very long namespace System.Xml.Tests")]
    //    [InlineData()]
    //    public int GetExtObject5()
    //    {
    //        m_xsltArg = new XsltArgumentList();
    //        MyObject obj = new MyObject(5);

    //        m_xsltArg.AddExtensionObject(szLongNS, obj);
    //        retObj = m_xsltArg.GetExtensionObject(szLongNS);

    //        if(((MyObject)retObj).MyValue() != obj.MyValue())
    //        {
    //            _output.WriteLine("Set and retrieved value appear to be different");
    //            Assert.True(false);
    //        }

    //        if((LoadXSL("MyObjectLongNS.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
    //            (CheckResult(522.0563223871)== TEST_PASS))
    //            return;
    //        else
    //            Assert.True(false);
    //    }

    //    /*
    //     * This test is no more valid.
    //     * MDAC Bug # 88407 fix now allows ANY characters in URI
    //    //[Variation("Invalid namespace System.Xml.Tests")]
    //    [InlineData()]
    //    public int GetExtObject6()
    //    {
    //        m_xsltArg = new XsltArgumentList();
    //        MyObject obj = new MyObject(6);

    //        try
    //        {
    //            m_xsltArg.AddExtensionObject(szInvalid, obj);
    //        }
    //        catch (System.UriFormatException)
    //        {
    //            return;
    //        }
    //        _output.WriteLine("Did not throw System.UriFormatException for invalid namespace System.Xml.Tests");
    //        Assert.True(false);
    //    }
    //    */

    //    //[Variation("Different Data Types")]
    //    [InlineData()]
    //    public int GetExtObject7()
    //    {
    //        m_xsltArg = new XsltArgumentList();
    //        String obj = "0.00";

    //        // string
    //        m_xsltArg.AddExtensionObject("myArg1", obj);
    //        retObj = m_xsltArg.GetExtensionObject("myArg1");
    //        _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "0.00", retObj);
    //        if( retObj.ToString() != "0.00")
    //        {
    //            _output.WriteLine("Failed to add/get a value for {0} of type {1}", "0.00", "string");
    //            _output.WriteLine("Retrieved: {0}  ", retObj);
    //            Assert.True(false);
    //        }

    //        int i = 8;

    //        m_xsltArg.AddExtensionObject("myArg2", i);
    //        retObj = m_xsltArg.GetExtensionObject("myArg2");
    //        _output.WriteLine("Added Value:{0}\nRetrieved Value:{1}", i, retObj);
    //        if(!i.Equals(retObj))
    //        {
    //            _output.WriteLine("Failed to add/get a value for {0} with conversion from int to double", i);
    //            _output.WriteLine("Retrieved: {0}", retObj.ToString());
    //            Assert.True(false);
    //        }

    //        //must also be same instance!!!
    //        if(i != (int)retObj)
    //            Assert.True(false);

    //        Boolean bF = (1==0);

    //        m_xsltArg.AddExtensionObject("myArg3", bF);
    //        retObj = m_xsltArg.GetExtensionObject("myArg3");
    //        _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bF.ToString(), retObj);
    //        if( !bF.Equals(retObj))
    //        {
    //            _output.WriteLine("Failed to add/get a value for {0} of type {1}", bF.ToString(), "boolean");
    //            _output.WriteLine("Retrieved: {0}  ", retObj);
    //            Assert.True(false);
    //        }

    //        Boolean bT = (1==1);

    //        m_xsltArg.AddExtensionObject("myArg4", bT);
    //        retObj = m_xsltArg.GetExtensionObject("myArg4");
    //        _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bT.ToString(), retObj);
    //        if( !bT.Equals(retObj))
    //        {
    //            _output.WriteLine("Failed to add/get a value for {0} of type {1}", bT.ToString(), "boolean");
    //            _output.WriteLine("Retrieved: {0}  ", retObj);
    //            Assert.True(false);
    //        }
    //        return;
    //    }

    //    //[Variation("Case sensitivity")]
    //    [InlineData()]
    //    public int GetExtObject8()
    //    {
    //        MyObject obj = new MyObject(8);
    //        m_xsltArg = new XsltArgumentList();

    //        m_xsltArg.AddExtensionObject("urn:my-object", obj);

    //        retObj = m_xsltArg.GetExtensionObject("urn:my-object");
    //        if(((MyObject)retObj).MyValue() != obj.MyValue())
    //        {
    //            _output.WriteLine("Set and retrieved value appear to be different");
    //            Assert.True(false);
    //        }

    //        retObj = m_xsltArg.GetExtensionObject("URN:MY-OBJECT");
    //        if(retObj != null)
    //        {
    //            _output.WriteLine("Set and retrieved value appear to be different for URN:MY-OBJECT");
    //            Assert.True(false);
    //        }

    //        retObj = m_xsltArg.GetExtensionObject("urn:My-Object");
    //        if(retObj != null)
    //        {
    //            _output.WriteLine("Set and retrieved value appear to be different for urn:My-Object");
    //            Assert.True(false);
    //        }

    //        retObj = m_xsltArg.GetExtensionObject("urn-my:object");
    //        if(retObj != null)
    //        {
    //            _output.WriteLine("Set and retrieved value appear to be different for urn-my:object");
    //            Assert.True(false);
    //        }

    //        if((LoadXSL("myObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
    //            (CheckResult(430.402026847)== TEST_PASS))
    //            return;
    //        else
    //            Assert.True(false);
    //    }

    //    //[Variation("Whitespace")]
    //    [InlineData()]
    //    public int GetExtObject9()
    //    {
    //        int i=1;
    //        m_xsltArg = new XsltArgumentList();

    //        foreach(String str in szWhiteSpace)
    //        {
    //            MyObject obj = new MyObject(i);

    //            m_xsltArg.AddExtensionObject(szDefaultNS + str, obj);
    //            retObj = m_xsltArg.GetExtensionObject(szDefaultNS + str);
    //            if(((MyObject)retObj).MyValue() != i)
    //            {
    //                _output.WriteLine("Error processing {0} test for whitespace arg", i);
    //                Assert.True(false);
    //            }
    //            i++;
    //        }

    //        try
    //        {
    //            if((LoadXSL("myObjectDef.xsl") == TEST_PASS))
    //                Transform_ArgList("fruits.xml");
    //        }
    //        catch(System.Xml.Xsl.XsltException)
    //        {
    //            return;
    //        }
    //        _output.WriteLine("Did not throw expected exception: System.Xml.Xsl.XsltException");
    //        Assert.True(false);
    //    }

    //    //[Variation("Call after object has been removed")]
    //    [InlineData()]
    //    public int GetExtObject10()
    //    {
    //        MyObject obj = new MyObject(10);
    //        m_xsltArg = new XsltArgumentList();

    //        m_xsltArg.AddExtensionObject(szDefaultNS, obj);
    //        m_xsltArg.RemoveExtensionObject(szDefaultNS);
    //        retObj = m_xsltArg.GetExtensionObject(szDefaultNS);

    //        if(retObj != null)
    //        {
    //            _output.WriteLine("Did not retrieve a NULL value for a non-existent object returned");
    //            Assert.True(false);
    //        }

    //        try
    //        {
    //            if((LoadXSL("myObjectDef.xsl") == TEST_PASS))
    //                Transform_ArgList("fruits.xml");
    //        }
    //        catch(System.Xml.Xsl.XsltException)
    //        {
    //            return;
    //        }
    //        _output.WriteLine("Did not throw expected exception: System.Xml.Xsl.XsltException");
    //        Assert.True(false);
    //    }

    //    //[Variation("Call multiple times")]
    //    [InlineData()]
    //    public int GetExtObject11()
    //    {
    //        MyObject obj = new MyObject(11);
    //        m_xsltArg = new XsltArgumentList();

    //        m_xsltArg.AddExtensionObject(szDefaultNS, obj);

    //        for(int i=0; i < 500; i++)
    //        {
    //            retObj = m_xsltArg.GetExtensionObject(szDefaultNS);
    //            if(((MyObject)retObj).MyValue() != obj.MyValue())
    //            {
    //                _output.WriteLine("Set and retrieved value appear to be different after {i} tries", i);
    //                Assert.True(false);
    //            }
    //        }
    //        if((LoadXSL("myObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
    //            (CheckResult(430.402026847)== TEST_PASS))
    //            return;
    //        else
    //            Assert.True(false);
    //    }

    //    //[Variation("Using XSL Namespace")]
    //    [InlineData()]
    //    public int GetExtObject12()
    //    {
    //        m_xsltArg = new XsltArgumentList();

    //        retObj = m_xsltArg.GetExtensionObject(szDefaultNS);
    //        if(retObj != null)
    //        {
    //            _output.WriteLine("Did not retrieve null value when using namespace {0}", szXslNS);
    //            Assert.True(false);
    //        }
    //        return;
    //    }
    //}

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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam1()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                Assert.True(false);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(457.6003003605) == 1))
                return;
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam4()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam(szLongString, szEmpty, "Test1");
            retObj = m_xsltArg.GetParam(szLongString, szEmpty);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                Assert.True(false);

            if ((LoadXSL("showParamLongName.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(383.8692240713) == 1))
                return;
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam7()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test7");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);

            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test7")
                Assert.True(false);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(457.7055635184) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Very long namespace System.Xml.Tests")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam8()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szLongNS, "Test8");
            retObj = m_xsltArg.GetParam("myArg1", szLongNS);
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test8")
                Assert.True(false);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        /*
         * This test is no more valid.
         * MDAC Bug # 88407 fix now allows ANY characters in URI
        //[Variation("Invalid Namespace URI")]
        [InlineData()]
        [Theory]
        public void AddParam9()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam("myArg1", szInvalid, "Test1");
            }
            catch (System.UriFormatException)
            {
                return;
            }
            _output.WriteLine("Did not throw System.UriFormatException for invalid namespace System.Xml.Tests");
            Assert.True(false);
        }
        */

        //[Variation("Objects as different Data Types")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam10()
        {
            m_xsltArg = new XsltArgumentList();
            MyObject m = new MyObject(10, _output);

            m_xsltArg.AddParam("myArg1", szEmpty, m);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(473.4007803959) == 1))
                return;
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam12()
        {
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

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(457.6003003605) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Object with same namespace System.Xml.Tests, different name")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam13()
        {
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

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(449.000354515) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Case Sensitivity")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam14()
        {
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

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(449.000354515) == 1))
                return;
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam16()
        {
            m_xsltArg = new XsltArgumentList();
            String obj = "Test";

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
            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(458.7752486264) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Whitespace in URI and param")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam17()
        {
            int i = 1;
            int errCount = 0;
            m_xsltArg = new XsltArgumentList();

            foreach (String str in szWhiteSpace)
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

            foreach (String str in szWhiteSpace)
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

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(420.7138852814) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Adding many objects")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam18()
        {
            m_xsltArg = new XsltArgumentList();
            String obj = "Test";

            for (int i = 1; i < 7; i++)
            {
                m_xsltArg.AddParam("myArg" + +i, szEmpty, obj + i);
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj.ToString() != ("Test" + i))
                    Assert.True(false);
            }

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(413.6341271694) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Add same object many times")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam19()
        {
            m_xsltArg = new XsltArgumentList();
            String obj = "Test";

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
            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(458.7752486264) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Using Different XSLT namespace")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddParam20()
        {
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

            if ((LoadXSL("showParamNS.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(509.315418596) == 1))
                return;
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
        [InlineData()]
        [Theory]
        public void AddExtObject32()
        {
            if (LoadXSL("test_Param.xsl") == 1)
            {
                m_xsltArg = new XsltArgumentList();
                m_xsltArg.AddParam("myParam1", szEmpty, "first");

                // Transform once
                if ((Transform_ArgList("foo.xml") == 1) && (CheckResult(383.6292620645) == 1))
                {
                    m_xsltArg = new XsltArgumentList();
                    m_xsltArg.AddParam("myParam1", szEmpty, "second");

                    // Transform again to make sure that parameter from first transform are not cached
                    if ((Transform_ArgList("foo.xml") == 1) && (CheckResult(384.9801823644) == 1))
                        return;
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
        [InlineData("AddParameterA1.xsl", "default local")]
        //[Variation(id = 2, Pri = 2, Desc = "No param sent, global param used, local param exists with no default value", Params = new object[] { "AddParameterA2.xsl", "" })]
        [InlineData("AddParameterA2.xsl", "")]
        //[Variation(id = 3, Pri = 2, Desc = "No param sent, global param used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterA3.xsl", "default global" })]
        [InlineData("AddParameterA3.xsl", "default global")]
        //[Variation(id = 4, Pri = 2, Desc = "No param sent, global param used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterA4.xsl", "with-param" })]
        [InlineData("AddParameterA4.xsl", "with-param")]
        //[Variation(id = 5, Pri = 2, Desc = "No param sent, global param used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterA5.xsl", "" })]
        [InlineData("AddParameterA5.xsl", "")]
        //[Variation(id = 6, Pri = 2, Desc = "No param sent, global param used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterA6.xsl", "default global" })]
        [InlineData("AddParameterA6.xsl", "default global")]
        //[Variation(id = 7, Pri = 2, Desc = "No param sent, global param used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterA7.xsl", "default global" })]
        [InlineData("AddParameterA7.xsl", "default global")]

        //global param is xsl:variable local param is xsl:param
        //[Variation(id = 8, Pri = 2, Desc = "No param sent, global variable used, local param exists with a default value", Params = new object[] { "AddParameterDA1.xsl", "default local" })]
        [InlineData("AddParameterDA1.xsl", "default local")]
        //[Variation(id = 9, Pri = 2, Desc = "No param sent, global variable used, local param exists with no default value", Params = new object[] { "AddParameterDA2.xsl", "" })]
        [InlineData("AddParameterDA2.xsl", "")]
        //[Variation(id = 10, Pri = 2, Desc = "No param sent, global variable used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterDA3.xsl", "default global" })]
        [InlineData("AddParameterDA3.xsl", "default global")]
        //[Variation(id = 11, Pri = 2, Desc = "No param sent, global variable used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterDA4.xsl", "with-param" })]
        [InlineData("AddParameterDA4.xsl", "with-param")]
        //[Variation(id = 12, Pri = 2, Desc = "No param sent, global variable used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterDA5.xsl", "" })]
        [InlineData("AddParameterDA5.xsl", "")]
        //[Variation(id = 13, Pri = 2, Desc = "No param sent, global variable used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterDA6.xsl", "default global" })]
        [InlineData("AddParameterDA6.xsl", "default global")]
        //[Variation(id = 14, Pri = 2, Desc = "No param sent, global variable used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterDA7.xsl", "default global" })]
        [InlineData("AddParameterDA7.xsl", "default global")]

        //global param is xsl:param local param is xsl:variable
        //[Variation(id = 15, Pri = 2, Desc = "No param sent, global param used, local variable exists with a default value", Params = new object[] { "AddParameterEA1.xsl", "default local" })]
        [InlineData("AddParameterEA1.xsl", "default local")]
        //[Variation(id = 16, Pri = 2, Desc = "No param sent, global param used, local variable exists with no default value", Params = new object[] { "AddParameterEA2.xsl", "" })]
        [InlineData("AddParameterEA2.xsl", "")]
        //[Variation(id = 17, Pri = 2, Desc = "No param sent, global param used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterEA3.xsl", "default global" })]
        [InlineData("AddParameterEA3.xsl", "default global")]
        //[Variation(id = 18, Pri = 2, Desc = "No param sent, global param used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterEA4.xsl", "default local" })]
        [InlineData("AddParameterEA4.xsl", "default local")]
        //[Variation(id = 19, Pri = 2, Desc = "No param sent, global param used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterEA5.xsl", "" })]
        [InlineData("AddParameterEA5.xsl", "")]
        //[Variation(id = 20, Pri = 2, Desc = "No param sent, global param used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterEA6.xsl", "default global" })]
        [InlineData("AddParameterEA6.xsl", "default global")]
        //[Variation(id = 21, Pri = 2, Desc = "No param sent, global param used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterEA7.xsl", "default global" })]
        [InlineData("AddParameterEA7.xsl", "default global")]

        //global param is xsl:variable local param is xsl:variable
        //[Variation(id = 22, Pri = 2, Desc = "No param sent, global variable used, local variable exists with a default value", Params = new object[] { "AddParameterFA1.xsl", "default local" })]
        [InlineData("AddParameterFA1.xsl", "default local")]
        //[Variation(id = 23, Pri = 2, Desc = "No param sent, global variable used, local variable exists with no default value", Params = new object[] { "AddParameterFA2.xsl", "" })]
        [InlineData("AddParameterFA2.xsl", "")]
        //[Variation(id = 24, Pri = 2, Desc = "No param sent, global variable used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterFA3.xsl", "default global" })]
        [InlineData("AddParameterFA3.xsl", "default global")]
        //[Variation(id = 25, Pri = 2, Desc = "No param sent, global variable used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterFA4.xsl", "default local" })]
        [InlineData("AddParameterFA4.xsl", "default local")]
        //[Variation(id = 26, Pri = 2, Desc = "No param sent, global variable used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterFA5.xsl", "" })]
        [InlineData("AddParameterFA5.xsl", "")]
        //[Variation(id = 27, Pri = 2, Desc = "No param sent, global variable used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterFA6.xsl", "default global" })]
        [InlineData("AddParameterFA6.xsl", "default global")]
        //[Variation(id = 28, Pri = 2, Desc = "No param sent, global variable used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterFA7.xsl", "default global" })]
        [InlineData("AddParameterFA7.xsl", "default global")]
        [Theory]
        public void AddParam1(object param0, object param1)
        {
            m_xsltArg = new XsltArgumentList();
            string xslFile = param0.ToString();
            string expected = "<result>" + param1.ToString() + "</result>";

            if ((LoadXSL(xslFile) == 1) && (Transform_ArgList("AddParameter.xml") == 1))
                VerifyResult(expected);
            else
                Assert.True(false);
        }

        //All the below variations, param is sent from client code

        //global param is xsl:param local param is xsl:param
        //[Variation(id = 29, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value", Params = new object[] { "AddParameterB1.xsl", "default local" })]
        [InlineData("AddParameterB1.xsl", "default local")]
        //[Variation(id = 30, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value", Params = new object[] { "AddParameterB2.xsl", "" })]
        [InlineData("AddParameterB2.xsl", "")]
        //[Variation(id = 31, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterB3.xsl", "outside param" })]
        [InlineData("AddParameterB3.xsl", "outside param")]
        //[Variation(id = 32, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterB4.xsl", "with-param" })]
        [InlineData("AddParameterB4.xsl", "with-param")]
        //[Variation(id = 33, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterB5.xsl", "" })]
        [InlineData("AddParameterB5.xsl", "")]
        //[Variation(id = 34, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterB6.xsl", "outside param" })]
        [InlineData("AddParameterB6.xsl", "outside param")]
        //[Variation(id = 35, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterB7.xsl", "outside param" })]
        [InlineData("AddParameterB7.xsl", "outside param")]

        //global param is xsl:variable local param is xsl:param
        //[Variation(id = 36, Pri = 2, Desc = "Param sent, global variable used, local param exists with a default value", Params = new object[] { "AddParameterDB1.xsl", "default local" })]
        [InlineData("AddParameterDB1.xsl", "default local")]
        //[Variation(id = 37, Pri = 2, Desc = "Param sent, global variable used, local param exists with no default value", Params = new object[] { "AddParameterDB2.xsl", "" })]
        [InlineData("AddParameterDB2.xsl", "")]
        //[Variation(id = 38, Pri = 2, Desc = "Param sent, global variable used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterDB3.xsl", "default global" })]
        [InlineData("AddParameterDB3.xsl", "default global")]
        //[Variation(id = 39, Pri = 2, Desc = "Param sent, global variable used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterDB4.xsl", "with-param" })]
        [InlineData("AddParameterDB4.xsl", "with-param")]
        //[Variation(id = 40, Pri = 2, Desc = "Param sent, global variable used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterDB5.xsl", "" })]
        [InlineData("AddParameterDB5.xsl", "")]
        //[Variation(id = 41, Pri = 2, Desc = "Param sent, global variable used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterDB6.xsl", "default global" })]
        [InlineData("AddParameterDB6.xsl", "default global")]
        //[Variation(id = 42, Pri = 2, Desc = "Param sent, global variable used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterDB7.xsl", "default global" })]
        [InlineData("AddParameterDB7.xsl", "default global")]

        //global param is xsl:param local param is xsl:variable
        //[Variation(id = 43, Pri = 2, Desc = "Param sent, global param used, local variable exists with a default value", Params = new object[] { "AddParameterEB1.xsl", "default local" })]
        [InlineData("AddParameterEB1.xsl", "default local")]
        //[Variation(id = 44, Pri = 2, Desc = "Param sent, global param used, local variable exists with no default value", Params = new object[] { "AddParameterEB2.xsl", "" })]
        [InlineData("AddParameterEB2.xsl", "")]
        //[Variation(id = 45, Pri = 2, Desc = "Param sent, global param used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterEB3.xsl", "outside param" })]
        [InlineData("AddParameterEB3.xsl", "outside param")]
        //[Variation(id = 46, Pri = 2, Desc = "Param sent, global param used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterEB4.xsl", "default local" })]
        [InlineData("AddParameterEB4.xsl", "default local")]
        //[Variation(id = 47, Pri = 2, Desc = "Param sent, global param used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterEB5.xsl", "" })]
        [InlineData("AddParameterEB5.xsl", "")]
        //[Variation(id = 48, Pri = 2, Desc = "Param sent, global param used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterEB6.xsl", "outside param" })]
        [InlineData("AddParameterEB6.xsl", "outside param")]
        //[Variation(id = 49, Pri = 2, Desc = "Param sent, global param used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterEB7.xsl", "outside param" })]
        [InlineData("AddParameterEB7.xsl", "outside param")]

        //global param is xsl:variable local param is xsl:variable
        //[Variation(id = 50, Pri = 2, Desc = "Param sent, global variable used, local variable exists with a default value", Params = new object[] { "AddParameterFB1.xsl", "default local" })]
        [InlineData("AddParameterFB1.xsl", "default local")]
        //[Variation(id = 51, Pri = 2, Desc = "Param sent, global variable used, local variable exists with no default value", Params = new object[] { "AddParameterFB2.xsl", "" })]
        [InlineData("AddParameterFB2.xsl", "")]
        //[Variation(id = 52, Pri = 2, Desc = "Param sent, global variable used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterFB3.xsl", "default global" })]
        [InlineData("AddParameterFB3.xsl", "default global")]
        //[Variation(id = 53, Pri = 2, Desc = "Param sent, global variable used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterFB4.xsl", "default local" })]
        [InlineData("AddParameterFB4.xsl", "default local")]
        //[Variation(id = 54, Pri = 2, Desc = "Param sent, global variable used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterFB5.xsl", "" })]
        [InlineData("AddParameterFB5.xsl", "")]
        //[Variation(id = 55, Pri = 2, Desc = "Param sent, global variable used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterFB6.xsl", "default global" })]
        [InlineData("AddParameterFB6.xsl", "default global")]
        //[Variation(id = 56, Pri = 2, Desc = "Param sent, global variable used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterFB7.xsl", "default global" })]
        [InlineData("AddParameterFB7.xsl", "default global")]
        [Theory]
        public void AddParam2(object param0, object param1)
        {
            string xslFile = param0.ToString();
            string expected = "<result>" + param1.ToString() + "</result>";

            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("param1", "", "outside param");

            if ((LoadXSL(xslFile) == 1) && (Transform_ArgList("AddParameter.xml") == 1))
                VerifyResult(expected);
            else
                Assert.True(false);
        }

        //All the below variations, empty param is sent from client code
        //global param is xsl:param local param is xsl:param
        //[Variation(id = 57, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value", Params = new object[] { "AddParameterB1.xsl", "default local" })]
        [InlineData("AddParameterB1.xsl", "default local")]
        //[Variation(id = 58, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value", Params = new object[] { "AddParameterB2.xsl", "" })]
        [InlineData("AddParameterB2.xsl", "")]
        //[Variation(id = 59, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterB3.xsl", "" })]
        [InlineData("AddParameterB3.xsl", "")]
        //[Variation(id = 60, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterB4.xsl", "with-param" })]
        [InlineData("AddParameterB4.xsl", "with-param")]
        //[Variation(id = 61, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterB5.xsl", "" })]
        [InlineData("AddParameterB5.xsl", "")]
        //[Variation(id = 62, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterB6.xsl", "" })]
        [InlineData("AddParameterB6.xsl", "")]
        //[Variation(id = 63, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterB7.xsl", "" })]
        [InlineData("AddParameterB7.xsl", "")]
        [Theory]
        public void AddParam3(object param0, object param1)
        {
            string xslFile = param0.ToString();
            string expected = "<result>" + param1.ToString() + "</result>";

            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("param1", "", "");

            if ((LoadXSL(xslFile) == 1) && (Transform_ArgList("AddParameter.xml") == 1))
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
    // TODO: Fix security issue
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject1()
        {
            MyObject obj = new MyObject(1, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("myObjectDef.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(430.402026847) == 1))
                return;
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject4()
        {
            m_xsltArg = new XsltArgumentList();
            MyObject obj = new MyObject(4, _output);

            m_xsltArg.AddExtensionObject(szLongNS, obj);

            if ((LoadXSL("MyObjectLongNS.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(522.0563223871) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Different Data Types")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject6()
        {
            m_xsltArg = new XsltArgumentList();
            String obj = "0.00";

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

            Boolean bF = (1 == 0);

            m_xsltArg.AddExtensionObject("myArg3", bF);
            retObj = m_xsltArg.GetExtensionObject("myArg3");
            _output.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bF.ToString(), retObj);
            if (!bF.Equals(retObj))
            {
                _output.WriteLine("Failed to add/get a value for {0} of type {1}", bF.ToString(), "boolean");
                _output.WriteLine("Retrieved: {0}  ", retObj);
                Assert.True(false);
            }

            Boolean bT = (1 == 1);

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

            if ((LoadXSL("MyObject_DataTypes.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(499.4069850096) == 1))
                return;
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject8()
        {
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

            if ((LoadXSL("myObjectDef.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(430.402026847) == 1))
                return;
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject10()
        {
            MyObject obj = new MyObject(10, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_Null.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(424.8906559839) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Add many objects")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject11()
        {
            m_xsltArg = new XsltArgumentList();

            MyObject obj1 = new MyObject(100, _output);
            m_xsltArg.AddExtensionObject(szDefaultNS, obj1);

            for (int i = 1; i < 500; i++)
            {
                MyObject obj = new MyObject(i, _output);
                m_xsltArg.AddExtensionObject(szDefaultNS + i, obj);
            }
            if ((LoadXSL("myObjectDef.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(430.402026847) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Whitespace")]
        [InlineData()]
        [Theory]
        public void AddExtObject12()
        {
            int i = 1;
            m_xsltArg = new XsltArgumentList();

            foreach (String str in szWhiteSpace)
            {
                MyObject obj = new MyObject(i, _output);
                m_xsltArg.AddExtensionObject(szDefaultNS + str, obj);
                i++;
            }
            try
            {
                if ((LoadXSL("myObjectDef.xsl") == 1))
                    Transform_ArgList("fruits.xml", true);
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject14()
        {
            MyObject obj = new MyObject(14, _output);
            m_xsltArg = new XsltArgumentList();

            for (int i = 0; i < 400; i++)
            {
                m_xsltArg.AddExtensionObject(szDefaultNS, obj);
                m_xsltArg.RemoveExtensionObject(szDefaultNS);
            }
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);

            if ((LoadXSL("myObjectDef.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(430.402026847) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Namespace URI non-existent")]
        [InlineData()]
        [Theory]
        public void AddExtObject15()
        {
            MyObject obj = new MyObject(15, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szSimple, obj);
            try
            {
                if ((LoadXSL("myObjectDef.xsl") == 1))
                    Transform_ArgList("fruits.xml", true);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return;
            }
            _output.WriteLine("Did not throw expected exception");
            Assert.True(false);
        }

        //[Variation("Accessing Private and protected Items")]
        [InlineData()]
        [Theory]
        public void AddExtObject16()
        {
            MyObject obj = new MyObject(1, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            try
            {
                LoadXSL("MyObject_PrivateAccess.xsl");
                Transform_ArgList("fruits.xml", true);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                try
                {
                    LoadXSL("MyObject_ProtectedAccess.xsl");
                    Transform_ArgList("fruits.xml", true);
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    try
                    {
                        LoadXSL("MyObject_DefaultAccess.xsl");
                        Transform_ArgList("fruits.xml", true);
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject17()
        {
            MyObject obj = new MyObject(17, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_ConsoleWrite.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(421.8660259804) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Recursive Functions")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject18()
        {
            MyObject obj = new MyObject(18, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_Recursion.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(459.4210605285) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Overloaded Functions")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject19()
        {
            MyObject obj = new MyObject(19, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_Overloads.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(481.1053900491) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Function-exists tests")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject20()
        {
            MyObject obj = new MyObject(20, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_FnExists.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(534.2681508201) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Argument Tests")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject21()
        {
            MyObject obj = new MyObject(1, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_Arguments.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(513.296131727) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Methods returning void and valid types")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject22()
        {
            MyObject obj = new MyObject(22, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_ReturnValidTypes.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(514.0958814743) == 1))
                return;
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
        [InlineData()]
        [Theory]
        public void AddExtObject25()
        {
            MyObject obj = new MyObject(25, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if (LoadXSL("MyObject_CaseSensitive.xsl") == 1)
            {
                try
                {
                    Transform_ArgList("fruits.xml");
                    CheckResult(419.3031944636);
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    return;
                }
            }
            _output.WriteLine("Exception not thrown for wrong-case spelling of methods");
            Assert.True(false);
        }

        //[Variation("Object namespace System.Xml.Tests found")]
        [InlineData()]
        [Theory]
        public void AddExtObject26()
        {
            MyObject obj = new MyObject(26, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_NotFoundNS.xsl") == 1))
            {
                try
                {
                    Transform_ArgList("fruits.xml", true);
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject27()
        {
            MyObject obj = new MyObject(27, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_KeepingState.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(439.7536748395) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Deliberately Messing Up the Stylesheet")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject28()
        {
            MyObject obj = new MyObject(28, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_KillerStrings.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(495.5795381156) == 1 || CheckResult(503.3730396353) == 1))  //multiple baseline
                return;
            else
                Assert.True(false);
        }

        //[Variation("Function not found in Object")]
        [InlineData()]
        [Theory]
        public void AddExtObject29()
        {
            MyObject obj = new MyObject(29, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            if ((LoadXSL("MyObject_NotFound.xsl") == 1))
            {
                try
                {
                    Transform_ArgList("fruits.xml", true);
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void AddExtObject32()
        {
            if (LoadXSL("Bug78587.xsl") == 1)
            {
                m_xsltArg = new XsltArgumentList();
                m_xsltArg.AddExtensionObject("id", new Id("first"));
                m_xsltArg.AddExtensionObject("capitalizer", new Capitalizer());

                // Transform once
                if ((Transform_ArgList("Bug78587.xml") == 1) && (CheckResult(438.9506396879) == 1))
                {
                    m_xsltArg = new XsltArgumentList();
                    m_xsltArg.AddExtensionObject("id", new Id("second"));
                    m_xsltArg.AddExtensionObject("capitalizer", new Capitalizer());

                    // Transform again to make sure that extension objects from first transform are not cached
                    if ((Transform_ArgList("Bug78587.xml") == 1) && (CheckResult(440.0296788876) == 1))
                        return;
                }
            }
            Assert.True(false);
        }
    }

    /***********************************************************/
    /*            XsltArgumentList.RemoveParam                 */
    /***********************************************************/

    //[TestCase(Name = "XsltArgumentList - RemoveParam : Reader , Reader", Desc = "READER,READER")]
    // TODO: Fix security issue
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam1()
        {
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

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(457.6003003605) == 1))
                return;
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam3()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.RemoveParam(szEmpty, szEmpty);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Param name is non-existent")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam4()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.RemoveParam(szSimple, szEmpty);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Invalid Param name")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam5()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.RemoveParam(szInvalid, szEmpty);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Very long param name")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam6()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam(szLongString, szEmpty, "Test1");
            m_xsltArg.RemoveParam(szLongString, szEmpty);

            if ((LoadXSL("showParamLongName.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(400.2204182193) == 1))
                return;
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam8()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myArg1", szEmpty);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Namespace URI is non-existent")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam9()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myArg1", "http://www.xsltTest.com");

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(457.6003003605) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Very long namespace System.Xml.Tests")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam10()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szLongString, "Test1");
            m_xsltArg.RemoveParam("myArg1", szLongString);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Different Data Types")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam11()
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

            String obj = "0.00";

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

            Boolean bF = (1 == 0);
            m_xsltArg.AddParam("myArg4", szEmpty, bF);
            m_xsltArg.RemoveParam("myArg4", szEmpty);
            retObj = m_xsltArg.GetParam("myArg4", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", bF);
                Assert.True(false);
            }

            Boolean bT = (1 == 1);
            m_xsltArg.AddParam("myArg5", szEmpty, bT);
            m_xsltArg.RemoveParam("myArg5", szEmpty);
            retObj = m_xsltArg.GetParam("myArg5", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", bT);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (Int16)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (UInt16)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (Int32)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (UInt32)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (Int64)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (UInt64)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (Single)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (Decimal)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                _output.WriteLine("Failed to remove {0}", i);
                Assert.True(false);
            }

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Case Sensitivity")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam12()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myarg1", szEmpty);
            m_xsltArg.RemoveParam("MYARG1", szEmpty);
            m_xsltArg.RemoveParam("myArg1 ", szEmpty);

            // perform a transform for kicks and ensure all is ok.
            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(457.6003003605) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Whitespace")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam13()
        {
            int i = 1;
            m_xsltArg = new XsltArgumentList();

            foreach (String str in szWhiteSpace)
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
            foreach (String str in szWhiteSpace)
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
            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(421.3863242307) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Call Multiple Times")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveParam14()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myArg1", szEmpty);
            for (int i = 0; i < 500; i++)
                m_xsltArg.RemoveParam("myArg1", szEmpty);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
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
    // TODO: Fix security issue
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
        [InlineData()]
        [Theory]
        public void RemoveExtObj1()
        {
            MyObject obj = new MyObject(1, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.RemoveExtensionObject(szDefaultNS);

            try
            {
                if ((LoadXSL("myObjectDef.xsl") == 1))
                    Transform_ArgList("fruits.xml", true);
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveExtObj3()
        {
            MyObject obj = new MyObject(10, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);

            for (int i = 0; i < 500; i++)
                m_xsltArg.RemoveExtensionObject(szDefaultNS);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Namespace URI is non-existent")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveExtObj4()
        {
            MyObject obj = new MyObject(4, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.RemoveExtensionObject(szSimple);

            if ((LoadXSL("myObjectDef.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(430.402026847) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Very long namespace System.Xml.Tests")]
        [InlineData()]
        [Theory]
        public void RemoveExtObj5()
        {
            m_xsltArg = new XsltArgumentList();
            MyObject obj = new MyObject(5, _output);

            m_xsltArg.AddExtensionObject("urn:" + szLongNS, obj);
            m_xsltArg.RemoveExtensionObject("urn:" + szLongNS);

            try
            {
                if ((LoadXSL("myObjectDef.xsl") == 1))
                    Transform_ArgList("fruits.xml", true);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return;
            }
            _output.WriteLine("Did not throw expected exception");
            Assert.True(false);
        }

        //[Variation("Different Data Types")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveExtObj6()
        {
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

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Case Sensitivity")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveExtObj7()
        {
            MyObject obj = new MyObject(7, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject("urn:my-object", obj);

            m_xsltArg.RemoveExtensionObject("URN:MY-OBJECT");
            m_xsltArg.RemoveExtensionObject("urn:My-Object");
            m_xsltArg.RemoveExtensionObject("urn-my:object");
            m_xsltArg.RemoveExtensionObject("urn:my-object ");

            if ((LoadXSL("myObjectDef.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(430.402026847) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Whitespace")]
        [InlineData()]
        [Theory]
        public void RemoveExtObj8()
        {
            int i = 1;
            m_xsltArg = new XsltArgumentList();

            foreach (String str in szWhiteSpace)
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
                if ((LoadXSL("myObjectDef.xsl") == 1))
                    Transform_ArgList("fruits.xml", true);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return;
            }
            _output.WriteLine("Did not exception for object that could not be executed");
            Assert.True(false);
        }

        //[Variation("Using default XSLT namespace - Bug305503")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void RemoveExtObj9()
        {
            MyObject obj = new MyObject(10, _output);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.RemoveExtensionObject(szDefaultNS);

            // ensure we can still do a transform
            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
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

        //[Variation(Desc = "Basic Verification Test", Pri = 0)]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void Clear1()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return; //return TEST_SKIPPED;

            m_xsltArg.Clear();
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
                Assert.True(false);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Clear with nothing loaded")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void Clear2()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.Clear();
            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Clear Params")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void Clear3()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return; //return TEST_SKIPPED;

            m_xsltArg.Clear();
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
                Assert.True(false);

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Clear Extension Objects")]
        [InlineData()]
        [Theory]
        public void Clear4()
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

            if ((LoadXSL("myObjectDef.xsl") == 1))
            {
                try
                {
                    Transform_ArgList("fruits.xml");
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
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void Clear5()
        {
            m_xsltArg = new XsltArgumentList();
            String obj = "Test";

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

            //	_output.WriteLine(retObj.GetType());

            m_xsltArg.Clear();

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Clear Multiple Times")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void Clear6()
        {
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

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(466.5112789241) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Loading one object, but clearing another")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void Clear7()
        {
            m_xsltArg = new XsltArgumentList();
            XsltArgumentList m_2 = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return; //return TEST_SKIPPED;

            m_2.Clear();

            if ((LoadXSL("showParam.xsl") == 1) && (Transform_ArgList("fruits.xml") == 1) &&
                (CheckResult(457.6003003605) == 1))
                return;
            else
                Assert.True(false);
        }

        //[Variation("Clear after objects have been \"Removed\"")]
        [ActiveIssue(9877)]
        [InlineData()]
        [Theory]
        public void Clear8()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return; //return TEST_SKIPPED;
            retObj = m_xsltArg.RemoveParam("myArg1", szEmpty);
            m_xsltArg.Clear();

            if ((LoadXSL("showParam.xsl") != 1) || (Transform_ArgList("fruits.xml") != 1) ||
                (CheckResult(466.5112789241) != 1))
                Assert.True(false);

            MyObject obj = new MyObject(26, _output);

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.RemoveExtensionObject(szDefaultNS);
            m_xsltArg.Clear();

            if ((LoadXSL("myObjectDef.xsl") == 1))
            {
                try
                {
                    Transform_ArgList("fruits.xml");
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
