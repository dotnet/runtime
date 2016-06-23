using OLEDB.Test.ModuleCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;

namespace XsltApiV2
{
    /***********************************************************/
    /*               XsltArgumentList.GetParam                 */
    /***********************************************************/

    [TestCase(Name = "XsltArgumentList - GetParam", Desc = "Get Param Test Cases")]
    public class CArgIntegrity : XsltApiTestCaseBase
    {
        [Variation(Desc = "Basic Verification Test", Pri = 0)]
        public int GetParam1()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                return TEST_FAIL;
            return TEST_PASS;
        }

        private static string typeXml = "<order></order>";

        private static string typeXsl = @"<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>
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
            WriteFiles(typeXml, "type.xml");
            WriteFiles(typeXsl, "type.xsl");
        }

        [Variation(Desc = "Tuple.XsltArgumentList.AddParam/AddExtensionObject", Param = 1)]
        [Variation(Desc = "DynamicObject.XsltArgumentList.AddParam/AddExtensionObject", Param = 2)]
        [Variation(Desc = "Guid.XsltArgumentList.AddParam/AddExtensionObject", Param = 3)]
        [Variation(Desc = "Dictionary.XsltArgumentList.AddParam/AddExtensionObject", Param = 4)]
        public int GetParam_Tuple()
        {
            WriteXmlAndXslFiles();

            object t = null;
            int param = (int)this.CurVariation.Param;
            switch (param)
            {
                case 1: t = Tuple.Create(1, "Melitta", 7.5); break;
                case 2: t = new TestDynamicObject(); break;
                case 3: t = new Guid(); break;
                case 4: t = new Dictionary<string, object>(); break;
            }
            CError.WriteLine(t);

            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load("type.xsl");

            XsltArgumentList xslArg = new XsltArgumentList();
            xslArg.AddParam("param", "", t);
            xslArg.AddExtensionObject("", t);

            try
            {
                xslt.Transform("type.xml", xslArg, new StringWriter());
            }
            catch (Exception e)
            {
                CError.WriteLine(e.Message);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        public class TestDynamicObject : DynamicObject
        {
            public dynamic GetDynamicObject()
            {
                return new Dictionary<string, object>();
            }
        }

        [Variation("Param name is null")]
        public int GetParam2()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam(null, szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Did not return NULL for null param name {0}", retObj);
                return TEST_FAIL;
            }
            else
                return TEST_PASS;
        }

        [Variation("Param name is empty string")]
        public int GetParam3()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam(szEmpty, szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Did not return NULL for empty string param name: {0}", retObj);
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Param name is non existent")]
        public int GetParam4()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam("RandomName", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Did not return NULL for non-existent parameter name: {0}", retObj);
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Invalid Param name")]
        public int GetParam5()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam(szInvalid, szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Did not return NULL for an invalid param name");
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Very Long Param")]
        public int GetParam6()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam(szLongString, szEmpty, "Test6");
            retObj = m_xsltArg.GetParam(szLongString, szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test6", retObj);
            if (retObj.ToString() != "Test6")
                return TEST_FAIL;
            return TEST_PASS;
        }

        [Variation("Namespace URI = null")]
        public int GetParam7()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam("myArg1", null);
            if (retObj != null)
            {
                CError.WriteLine("Did not return NULL for null namespace URI");
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Namespace URI is empty string")]
        public int GetParam8()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test8");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test8", retObj);
            if (retObj.ToString() != "Test8")
                return TEST_FAIL;
            return TEST_PASS;
        }

        [Variation("Namespace URI non-existent")]
        public int GetParam9()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test9");
            retObj = m_xsltArg.GetParam("myArg1", "http://www.microsoft.com");
            if (retObj != null)
            {
                CError.WriteLine("Did not retrieve a null value for non-existent uri");
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg2", "http://www.msn.com", "Test1");
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Did not retrieve a null value for non-existent uri");
                return TEST_FAIL;
            }

            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Did not retrieve a null value for non-existent uri");
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Very long namespace uri")]
        public int GetParam10()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szLongNS, "Test10");
            retObj = m_xsltArg.GetParam("myArg1", szLongNS);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test10", retObj);
            if (retObj.ToString() != "Test10")
                return TEST_FAIL;
            return TEST_PASS;
        }

        [Variation("Invalid Namespace URI")]
        public int GetParam11()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("myArg1", szInvalid, "Test11");

            retObj = m_xsltArg.GetParam("myArg1", szInvalid);

            if (CError.Compare(retObj.ToString() == "Test11", "Expected myArg1 = Test11"))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Different Data Types")]
        public int GetParam12()
        {
            m_xsltArg = new XsltArgumentList();
            String obj = "0.00";

            // string
            m_xsltArg.AddParam("myArg1", szEmpty, obj);
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "0.00", retObj);
            if (retObj.ToString() != "0.00")
            {
                CError.WriteLine("Failed to add/get a value for {0} of type {1}", "0.00", "string");
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }

            //int -- check conversions and value for original object and returned object
            //DCR - 298350 : Changing the expected value as per this DCR
            int i = 8;
            m_xsltArg.AddParam("myArg2", szEmpty, i);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value:{1}", i, retObj);
            CError.Compare(retObj.GetType(), i.GetType(), "Expected Type is " + i.GetType());

            Boolean bF = (1 == 0);
            m_xsltArg.AddParam("myArg3", szEmpty, bF);
            retObj = m_xsltArg.GetParam("myArg3", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bF.ToString(), retObj);
            if (!bF.Equals(retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0} of type {1}", bF.ToString(), "boolean");
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }

            Boolean bT = (1 == 1);
            m_xsltArg.AddParam("myArg4", szEmpty, bT);
            retObj = m_xsltArg.GetParam("myArg4", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bT.ToString(), retObj);
            if (!bT.Equals(retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0} of type {1}", bT.ToString(), "boolean");
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }

            XPathDocument xd = new XPathDocument(FullFilePath("fish.xml"));

            m_xsltArg.AddParam("myArg5", szEmpty, ((IXPathNavigable)xd).CreateNavigator());
            retObj = m_xsltArg.GetParam("myArg5", szEmpty);
            if (retObj == null)
            {
                CError.WriteLine("Failed to add/get a value of type {1}", "XPathNavigator");
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Case Sensitivity")]
        public int GetParam13()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myarg1", szEmpty);
            if (retObj != null)
                return TEST_FAIL;
            retObj = m_xsltArg.GetParam("myArg1 ", szEmpty);
            if (retObj != null)
                return TEST_FAIL;
            retObj = m_xsltArg.GetParam("myArg", szEmpty);
            if (retObj != null)
                return TEST_FAIL;

            return TEST_PASS;
        }

        [Variation("Whitespace")]
        public int GetParam14()
        {
            int i = 1;
            m_xsltArg = new XsltArgumentList();

            foreach (String str in szWhiteSpace)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, "Test" + str);
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj.ToString() != "Test" + str)
                {
                    CError.WriteLine("Error processing {0} test for whitespace arg in first set", i);
                    return TEST_FAIL;
                }
                i++;
            }

            foreach (String str in szWhiteSpace)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, "Test"); // dont add whitespace to name here since addparam would throw
                retObj = m_xsltArg.GetParam("myArg" + str, szEmpty);
                if (retObj != null)
                {
                    CError.WriteLine("Error processing {0} test for whitespace arg in second set. Returned object is not null.", i);
                    return TEST_FAIL;
                }
                i++;
            }
            return TEST_PASS;
        }

        [Variation("Call After Param has been removed")]
        public int GetParam15()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myArg1", szEmpty);
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);

            if (retObj != null)
                return TEST_FAIL;
            return TEST_PASS;
        }

        [Variation("Call multiple Times")]
        public int GetParam16()
        {
            m_xsltArg = new XsltArgumentList();
            int i = 0;

            m_xsltArg.AddParam("myArg1", szEmpty, "Test16");
            for (i = 0; i < 200; i++)
            {
                retObj = m_xsltArg.GetParam("myArg1", szEmpty);
                if (retObj.ToString() != "Test16")
                {
                    CError.WriteLine("Failed after retrieving {0} times", i);
                    CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test16", retObj);
                    return TEST_FAIL;
                }
            }
            CError.WriteLine("Retrievied {0} times", i);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            return TEST_PASS;
        }

        [Variation("Using XSL namespace")]
        public int GetParam17()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test17");

            retObj = m_xsltArg.GetParam("myArg3", szDefaultNS);
            if (retObj != null)
            {
                CError.WriteLine("Return a non-null value when retrieving Param with namespace {0}", szXslNS);
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Resolving conflicts with variables with different namespaces")]
        public int GetParam18()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);

            m_xsltArg.AddParam("myArg1", "http://www.msn.com", "Test2");
            retObj = m_xsltArg.GetParam("myArg1", "http://www.msn.com");
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);

            if (retObj.ToString() != "Test2")
                return TEST_FAIL;

            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            CError.WriteLine("Retrieve Original Value:{0}\nActual Retrieved Value: {1}", "Test1", retObj);

            if (retObj.ToString() != "Test1")
                return TEST_FAIL;
            return TEST_PASS;
        }

        [Variation("Namespace AND param = null")]
        public int GetParam19()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetParam(null, null);
            if (retObj != null)
            {
                CError.WriteLine("Did not return NULL for null parameter name");
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Data Types - Of type Double ")]
        public int GetParamDoubles()
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
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d1, retObj);
            if (!double.IsPositiveInfinity((double)retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0}", d1);
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg2", szEmpty, d2);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d2, retObj);
            if (!double.IsNegativeInfinity((double)retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0}", d2);
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg3", szEmpty, d3);
            retObj = m_xsltArg.GetParam("myArg3", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d3, retObj);
            if (!double.IsNaN((double)retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0}", d3);
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg4", szEmpty, d4);
            retObj = m_xsltArg.GetParam("myArg4", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d4, retObj);
            if (!d4.Equals((double)retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0}", d4);
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg5", szEmpty, d5);
            retObj = m_xsltArg.GetParam("myArg5", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d5, retObj);
            if (!d5.Equals(retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0}", d5);
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg6", szEmpty, d6);
            retObj = m_xsltArg.GetParam("myArg6", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d6, retObj);
            if (!d6.Equals(retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0}", d6);
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg7", szEmpty, d7);
            retObj = m_xsltArg.GetParam("myArg7", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", d7, retObj);
            if (!d7.Equals(retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0}", d7);
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        //DCR : 298350 - XsltArgumentList no longer reports the same type on the GetParam methods
        [Variation(id = 20, Desc = "Add Parameter other than XSLT Data Type and verify the type, expected same as added", Pri = 0)]
        public int GetParam20()
        {
            int i = 10;
            CError.WriteLine("Adding integer parameter of value {0}", i);
            m_xsltArg.AddParam("intArg", "", i);

            Type exp = i.GetType();
            Type act = m_xsltArg.GetParam("intArg", "").GetType();

            CError.WriteLine("Added Type : {0}", exp);
            CError.WriteLine("Returned Type : {0}", act);

            CError.Compare(act, exp, "Expected Type is integer");
            return TEST_PASS;
        }
    }

    //DCR 298350 - Testing on all possible datatypes
    [TestCase(Name = "XsltArgumentList - GetParam (DCR298350)", Desc = "ArgumentList Tests for XsltArgumentList")]
    public class CArgumentList : XsltApiTestCaseBase
    {
        private string Expected = string.Empty;
        private string Actual = string.Empty;

        public override void DetermineVariations()
        {
            base.DetermineVariations();
            string sDataTypes = Path.Combine(FilePathUtil.GetTestDataPath(), @"XsltApiV2\DataTypes.xml");

            //Load the datatypes XML and dynamic variations
            XmlDocument domTypes = new XmlDocument();
            domTypes.Load(sDataTypes);
            XmlNodeList DataTypes = domTypes.SelectNodes("//Type[@Test='true']");

            string DataTypeName = string.Empty;
            string DataTypeValue = string.Empty;
            string FriendlyName = string.Empty;
            string Namespace = string.Empty;

            int varID = 0;
            foreach (XmlNode DataType in DataTypes)
            {
                DataTypeName = DataType.SelectSingleNode("CLR/Name").InnerText;
                DataTypeValue = DataType.SelectSingleNode("XSD/Value").InnerText;
                FriendlyName = DataType.SelectSingleNode("@Name").Value;

                CVariation variation = new CVariation(this);
                variation.Name = "XsltArgumentListTest"; //look for public int XsltArgumentListTest()
                variation.id = ++varID;
                variation.Desc = "Test on '" + FriendlyName + "'";
                variation.Priority = 1;
                variation.Params = new object[] { DataTypeName, FriendlyName, Namespace, DataTypeValue };
                AddVariation(variation);
            }
        }

        private XsltArgumentList GetArgumentList(ref string DataTypeName, string FriendlyName, string Namespace, string DataTypeValue)
        {
            XsltArgumentList argList = new XsltArgumentList();
            object DataType = null;
            object[] DataTypeArr = null;
            bool ParamAdded = false;

            switch (DataTypeName)
            {
                case "System.Uri":
                    DataType = new Uri(DataTypeValue);
                    break;

                case "System.Byte[]":
                    Byte[] ByteDataTypeArr = new Byte[2];
                    ByteDataTypeArr[0] = 1;
                    ByteDataTypeArr[1] = 127;
                    argList.AddParam(FriendlyName, Namespace, ByteDataTypeArr);
                    ParamAdded = true;
                    break;

                case "System.Boolean":
                    DataType = new Boolean();
                    DataType = false;
                    Expected = "<result>false</result>";
                    break;

                case "System.SByte":
                    DataType = new SByte();
                    DataType = (SByte)126;
                    break;

                case "System.DateTime":
                    DataType = DateTime.Now;
                    break;

                case "System.Decimal":
                    DataType = new Decimal();
                    DataType = (Decimal)10.6;
                    break;

                case "System.Double":
                    DataType = new Double();
                    DataType = 10.65225E2;
                    break;

                case "System.TimeSpan":
                    DataType = new TimeSpan(10, 20, 30, 40, 50);
                    break;

                case "System.String[]":
                    DataTypeArr = new String[2];
                    DataTypeArr[0] = "str1";
                    DataTypeArr[0] = "str2";
                    DataType = DataTypeArr;
                    break;

                case "System.String":
                    DataType = string.Empty;
                    DataType = "str";
                    break;

                case "System.Single":
                    DataType = new Single();
                    DataType = (Single)100;
                    break;

                case "System.Int32":
                    DataType = new Int32();
                    DataType = 100000;
                    break;

                case "System.Int64":
                    DataType = new Int64();
                    DataType = 12678967543233;
                    break;

                case "System.Int16":
                    DataType = new Int16();
                    DataType = (Int16)12678;
                    break;

                case "System.Byte":
                    DataType = new Byte();
                    DataType = (Byte)255;
                    break;

                case "System.UInt16":
                    DataType = new UInt16();
                    DataType = (UInt16)65535;
                    break;

                case "System.UInt32":
                    DataType = new UInt32();
                    DataType = 4294967295;
                    break;

                case "System.UInt64":
                    DataType = new UInt64();
                    DataType = 18446744073709551615;
                    break;

                case "System.Object":
                    DataType = new Object();
                    long l = 99999999999999;
                    DataType = l;
                    break;

                case "System.Xml.XmlQualifiedName":
                    DataType = new XmlQualifiedName("x");
                    break;

                case "XPathNavigableObject":
                    string xml = "<root att='val' xmlns:ns='namespace'><?pi data?><!--comment-->text<![CDATA[cdata]]></root>";
                    DataType = CreateXPathNavigableObject(xml, ref DataTypeName, FriendlyName);
                    break;

                default:
                    break;
            }

            if (!ParamAdded)
                argList.AddParam(FriendlyName, Namespace, DataType);
            return argList;
        }

        private object CreateXPathNavigableObject(string xml, ref string DataTypeName, string FriendlyName)
        {
            XPathNavigator nav = null;
            XPathNodeIterator iterator = null;
            object ret = null;

            //Add a namespace
            XmlNameTable nt = new NameTable();
            XmlNamespaceManager ns = new XmlNamespaceManager(nt);
            ns.AddNamespace("ns", "namespace");

            //Load the XML in XmlDomcument
            XmlDocument dom = new XmlDocument(nt);
            dom.LoadXml(xml);

            switch (FriendlyName)
            {
                case "empty":
                    ret = "";
                    break;

                case "navigator":
                    nav = dom.CreateNavigator();
                    nav.MoveToFirstChild();
                    ret = nav;
                    Expected = "<result>" + nav.OuterXml + "</result>";
                    break;

                case "document":
                    ret = dom;
                    break;

                case "xpathdocument":
                    //Load the XML in XPathDocument
                    XmlTextReader tr = new XmlTextReader(new StringReader(xml));
                    XPathDocument xpathdoc = new XPathDocument(tr);
                    ret = xpathdoc;
                    break;

                case "iterator":
                    nav = dom.CreateNavigator();
                    iterator = nav.SelectChildren("root", "");
                    Expected = "<result>" + nav.OuterXml + "</result>";
                    ret = iterator;
                    break;

                case "element":
                    nav = dom.CreateNavigator();
                    ret = nav.SelectSingleNode("root");
                    Expected = "<result>" + nav.OuterXml + "</result>";
                    break;

                case "attribute":
                    nav = dom.CreateNavigator();
                    ret = nav.SelectSingleNode("root/@att");
                    Expected = "<result att=\"val\"/>";
                    break;

                case "namespace":
                    nav = dom.CreateNavigator();
                    ret = nav.SelectSingleNode("root/namespace::*");
                    break;

                case "text":
                    nav = dom.CreateNavigator();
                    ret = nav.SelectSingleNode("root/text()");
                    break;

                case "comment":
                    nav = dom.CreateNavigator();
                    ret = nav.SelectSingleNode("root/comment()");
                    Expected = "<result><!--comment--></result>";
                    break;

                case "pi":
                    nav = dom.CreateNavigator();
                    ret = nav.SelectSingleNode("root/processing-instruction()");
                    Expected = "<result><?pi data?></result>";
                    break;

                case "item":
                    nav = dom.CreateNavigator();
                    nav.SelectSingleNode("root");
                    XPathItem xpathitem = (XPathItem)nav;
                    ret = xpathitem;
                    break;

                case "anykind":
                    nav = dom.CreateNavigator();
                    ret = nav;
                    break;

                default:
                    break;
            }

            //Overwrite the DataTypeName by the DataTypeName expected
            DataTypeName = ret.GetType().ToString();
            return ret;
        }

        public int XsltArgumentListTest()
        {
            string DataTypeName = CurVariation.Params[0].ToString();
            string FriendlyName = CurVariation.Params[1].ToString();
            string Namespace = CurVariation.Params[2].ToString();
            string DataTypeValue = CurVariation.Params[3].ToString();

            XsltArgumentList argList = GetArgumentList(ref DataTypeName, FriendlyName, Namespace, DataTypeValue);

            //Verify if the datatype of the argument obtained is same as the datatype of the argument added.
            string exp = DataTypeName;
            string act = argList.GetParam(FriendlyName, Namespace).GetType().ToString();
            CError.WriteLine("Testing the type : " + exp);
            CError.Compare(act, exp, "Expected datatype is '" + exp + "'");
            return TEST_PASS;
        }
    }

    /***********************************************************/
    /*      XsltArgumentList.GetExtensionObject                */
    /***********************************************************/

    [TestCase(Name = "XsltArgumentList - GetExtensionObject", Desc = "XsltArgumentList.GetExtensionObject")]
    public class CArgGetExtObj : XsltApiTestCaseBase
    {
        [Variation(Desc = "Basic Verification Test", Pri = 1)]
        public int GetExtObject1()
        {
            MyObject obj = new MyObject(1);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            retObj = m_xsltArg.GetExtensionObject(szDefaultNS);

            CError.WriteLine("Retrieved value: {0}", ((MyObject)retObj).MyValue());
            if (((MyObject)retObj).MyValue() != obj.MyValue())
            {
                CError.WriteLine("Set and retrieved value appear to be different");
                return TEST_FAIL;
            }

            string expXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><result xmlns:myObj=\"urn:my-object\"><func1>1.Test1</func1><func2>2.Test2</func2><func3>3.Test3</func3></result>";
            if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (CheckResult(expXml) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Namespace URI = null")]
        public int GetExtObject2()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.GetExtensionObject(null);
            }
            catch (System.ArgumentNullException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("ArgumentNullException not thrown for null namespace URI");
            return TEST_PASS;
        }

        [Variation("Namespace URI is empty string", Param = "showParam.txt")]
        public int GetExtObject3()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            try
            {
                retObj = m_xsltArg.GetExtensionObject(szEmpty);
            }
            catch (Exception e)
            {
                CError.WriteLine(e);
                return TEST_FAIL;
            }

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Namespace URI non-existent")]
        public int GetExtObject4()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetExtensionObject(szDefaultNS);

            if (retObj != null)
            {
                CError.WriteLine("Did not return a NULL value for a non-existent URI");
                return TEST_FAIL;
            }
            try
            {
                if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS))
                    Transform_ArgList("fruits.xml");
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Did not throw exception for an invalid transform");
            return TEST_FAIL;
        }

        [Variation("Very long namespace URI")]
        public int GetExtObject5()
        {
            m_xsltArg = new XsltArgumentList();
            MyObject obj = new MyObject(5);

            m_xsltArg.AddExtensionObject(szLongNS, obj);
            retObj = m_xsltArg.GetExtensionObject(szLongNS);

            if (((MyObject)retObj).MyValue() != obj.MyValue())
            {
                CError.WriteLine("Set and retrieved value appear to be different");
                return TEST_FAIL;
            }

            string expXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><result xmlns:myObj=\"http://www.microsoft.com/this/is/a/very/long/namespace/uri/to/do/the/api/testing/for/xslt/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/\"><func1>1.Test1</func1><func2>2.Test2</func2><func3>3.Test3</func3></result>";
            if ((LoadXSL("MyObjectLongNS.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (CheckResult(expXml) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Invalid namespace URI")]
        public int GetExtObject6()
        {
            m_xsltArg = new XsltArgumentList();
            MyObject obj = new MyObject(6);
            m_xsltArg.AddExtensionObject(szInvalid, obj);

            retObj = m_xsltArg.GetExtensionObject(szInvalid);

            if (CError.Compare(retObj == obj, "Expected Object = " + obj.ToString()))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Different Data Types")]
        public int GetExtObject7()
        {
            m_xsltArg = new XsltArgumentList();
            String obj = "0.00";

            // string
            m_xsltArg.AddExtensionObject("myArg1", obj);
            retObj = m_xsltArg.GetExtensionObject("myArg1");
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "0.00", retObj);
            if (retObj.ToString() != "0.00")
            {
                CError.WriteLine("Failed to add/get a value for {0} of type {1}", "0.00", "string");
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }

            int i = 8;

            m_xsltArg.AddExtensionObject("myArg2", i);
            retObj = m_xsltArg.GetExtensionObject("myArg2");
            CError.WriteLine("Added Value:{0}\nRetrieved Value:{1}", i, retObj);
            if (!i.Equals(retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0} with conversion from int to double", i);
                CError.WriteLine("Retrieved: {0}", retObj.ToString());
                return TEST_FAIL;
            }

            //must also be same instance!!!
            if (i != (int)retObj)
                return TEST_FAIL;

            Boolean bF = (1 == 0);

            m_xsltArg.AddExtensionObject("myArg3", bF);
            retObj = m_xsltArg.GetExtensionObject("myArg3");
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bF.ToString(), retObj);
            if (!bF.Equals(retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0} of type {1}", bF.ToString(), "boolean");
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }

            Boolean bT = (1 == 1);

            m_xsltArg.AddExtensionObject("myArg4", bT);
            retObj = m_xsltArg.GetExtensionObject("myArg4");
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", bT.ToString(), retObj);
            if (!bT.Equals(retObj))
            {
                CError.WriteLine("Failed to add/get a value for {0} of type {1}", bT.ToString(), "boolean");
                CError.WriteLine("Retrieved: {0}  ", retObj);
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Case sensitivity")]
        public int GetExtObject8()
        {
            MyObject obj = new MyObject(8);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject("urn:my-object", obj);

            retObj = m_xsltArg.GetExtensionObject("urn:my-object");
            if (((MyObject)retObj).MyValue() != obj.MyValue())
            {
                CError.WriteLine("Set and retrieved value appear to be different");
                return TEST_FAIL;
            }

            retObj = m_xsltArg.GetExtensionObject("URN:MY-OBJECT");
            if (retObj != null)
            {
                CError.WriteLine("Set and retrieved value appear to be different for URN:MY-OBJECT");
                return TEST_FAIL;
            }

            retObj = m_xsltArg.GetExtensionObject("urn:My-Object");
            if (retObj != null)
            {
                CError.WriteLine("Set and retrieved value appear to be different for urn:My-Object");
                return TEST_FAIL;
            }

            retObj = m_xsltArg.GetExtensionObject("urn-my:object");
            if (retObj != null)
            {
                CError.WriteLine("Set and retrieved value appear to be different for urn-my:object");
                return TEST_FAIL;
            }

            string expXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><result xmlns:myObj=\"urn:my-object\"><func1>1.Test1</func1><func2>2.Test2</func2><func3>3.Test3</func3></result>";
            if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (CheckResult(expXml) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Whitespace")]
        public int GetExtObject9()
        {
            int i = 1;
            m_xsltArg = new XsltArgumentList();

            foreach (String str in szWhiteSpace)
            {
                MyObject obj = new MyObject(i);

                m_xsltArg.AddExtensionObject(szDefaultNS + str, obj);
                retObj = m_xsltArg.GetExtensionObject(szDefaultNS + str);
                if (((MyObject)retObj).MyValue() != i)
                {
                    CError.WriteLine("Error processing {0} test for whitespace arg", i);
                    return TEST_FAIL;
                }
                i++;
            }

            try
            {
                if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS))
                    Transform_ArgList("fruits.xml");
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Did not throw expected exception: System.Xml.Xsl.XsltException");
            return TEST_FAIL;
        }

        [Variation("Call after object has been removed")]
        public int GetExtObject10()
        {
            MyObject obj = new MyObject(10);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.RemoveExtensionObject(szDefaultNS);
            retObj = m_xsltArg.GetExtensionObject(szDefaultNS);

            if (retObj != null)
            {
                CError.WriteLine("Did not retrieve a NULL value for a non-existent object returned");
                return TEST_FAIL;
            }

            try
            {
                if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS))
                    Transform_ArgList("fruits.xml");
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Did not throw expected exception: System.Xml.Xsl.XsltException");
            return TEST_FAIL;
        }

        [Variation("Call multiple times")]
        public int GetExtObject11()
        {
            MyObject obj = new MyObject(11);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);

            for (int i = 0; i < 500; i++)
            {
                retObj = m_xsltArg.GetExtensionObject(szDefaultNS);
                if (((MyObject)retObj).MyValue() != obj.MyValue())
                {
                    CError.WriteLine("Set and retrieved value appear to be different after {i} tries", i);
                    return TEST_FAIL;
                }
            }
            string expXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><result xmlns:myObj=\"urn:my-object\"><func1>1.Test1</func1><func2>2.Test2</func2><func3>3.Test3</func3></result>";
            if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (CheckResult(expXml) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Using XSL Namespace")]
        public int GetExtObject12()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.GetExtensionObject(szDefaultNS);
            if (retObj != null)
            {
                CError.WriteLine("Did not retrieve null value when using namespace {0}", szXslNS);
                return TEST_FAIL;
            }
            return TEST_PASS;
        }
    }

    /***********************************************************/
    /*               XsltArgumentList.AddParam                 */
    /***********************************************************/

    [TestCase(Name = "XsltArgumentList - AddParam : Reader, Reader", Desc = "READER,READER")]
    [TestCase(Name = "XsltArgumentList - AddParam : Reader, Stream", Desc = "READER,STREAM")]
    [TestCase(Name = "XsltArgumentList - AddParam : Reader, Writer", Desc = "READER,WRITER")]
    [TestCase(Name = "XsltArgumentList - AddParam : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    [TestCase(Name = "XsltArgumentList - AddParam : URI, Reader", Desc = "URI,READER")]
    [TestCase(Name = "XsltArgumentList - AddParam : URI, Stream", Desc = "URI,STREAM")]
    [TestCase(Name = "XsltArgumentList - AddParam : URI, Writer", Desc = "URI,WRITER")]
    [TestCase(Name = "XsltArgumentList - AddParam : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    [TestCase(Name = "XsltArgumentList - AddParam : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    [TestCase(Name = "XsltArgumentList - AddParam : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    [TestCase(Name = "XsltArgumentList - AddParam : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    [TestCase(Name = "XsltArgumentList - AddParam : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CArgAddParam : XsltApiTestCaseBase
    {
        [Variation(Desc = "Basic Verification Test", Pri = 1, Param = "showParam1.txt")]
        public int AddParam1()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                return TEST_FAIL;

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Param  = null")]
        public int AddParam2()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam(null, szEmpty, "Test1");
            }
            catch (System.ArgumentNullException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("System.ArgumentNullException not thrown for adding null param");
            return TEST_FAIL;
        }

        [Variation("Param name is empty string")]
        public int AddParam3()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam(szEmpty, szEmpty, "Test1");
            }
            catch (System.ArgumentNullException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("System.ArgumentNullException not thrown for param name empty string");
            return TEST_FAIL;
        }

        [Variation("Very Long Param Name", Param = "LongParam.txt")]
        public int AddParam4()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam(szLongString, szEmpty, "Test1");
            retObj = m_xsltArg.GetParam(szLongString, szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                return TEST_FAIL;

            if ((LoadXSL("showParamLongName.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Invalid Param name")]
        public int AddParam5()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam(szInvalid, szEmpty, "Test1");
            }
            catch (System.Xml.XmlException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("System.Xml.XmlException not thrown for invalid param name");
            return TEST_FAIL;
        }

        [Variation("Namespace URI = null")]
        public int AddParam6()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam("myArg1", null, "Test1");
            }
            catch (System.ArgumentNullException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("System.ArgumentNullException not thrown for null namespace URI");
            return TEST_FAIL;
        }

        [Variation("Namespace URI is empty string", Param = "showParam7.txt")]
        public int AddParam7()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test7");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);

            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test7", retObj);
            if (retObj.ToString() != "Test7")
                return TEST_FAIL;

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Very long namespace uri", Param = "showParam.txt")]
        public int AddParam8()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szLongNS, "Test8");
            retObj = m_xsltArg.GetParam("myArg1", szLongNS);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test8")
                return TEST_FAIL;

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Invalid Namespace URI")]
        public int AddParam9()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("myArg1", szInvalid, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szInvalid);
            if (CError.Compare(retObj.ToString() == "Test1", "Expected Arg1 = Test1"))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Setting a param that already exists")]
        public int AddParam11()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test2");
            try
            {
                m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            }
            catch (System.ArgumentException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Did not throw System.ArgumentException for adding a param that already exists");
            return TEST_FAIL;
        }

        [Variation("Object with same name, different namespace URI", Param = "AddParam12.txt")]
        public int AddParam12()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                return TEST_FAIL;

            m_xsltArg.AddParam("myArg1", "http://www.msn.com", "Test2");
            retObj = m_xsltArg.GetParam("myArg1", "http://www.msn.com");
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);

            if (retObj.ToString() != "Test2")
                return TEST_FAIL;

            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            CError.WriteLine("Retrieve Original Value:{0}\nActual Retrieved Value: {1}", "Test1", retObj);

            if (retObj.ToString() != "Test1")
                return TEST_FAIL;

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Object with same namespace URI, different name", Param = "AddParam13.txt")]
        public int AddParam13()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                return TEST_FAIL;

            m_xsltArg.AddParam("myArg2", szEmpty, "Test2");
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);

            if (retObj.ToString() != "Test2")
                return TEST_FAIL;

            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            CError.WriteLine("Retrieve Original Value:{0}\nActual Retrieved Value: {1}", "Test1", retObj);

            if (retObj.ToString() != "Test1")
                return TEST_FAIL;

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Case Sensitivity", Param = "AddParam14.txt")]
        public int AddParam14()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                return TEST_FAIL;

            m_xsltArg.AddParam("myarg1", szEmpty, "Test2");
            retObj = m_xsltArg.GetParam("myarg1", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);
            if (retObj.ToString() != "Test2")
                return TEST_FAIL;

            m_xsltArg.AddParam("myArg2", szEmpty, "Test2");
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);
            if (retObj.ToString() != "Test2")
                return TEST_FAIL;

            m_xsltArg.AddParam("myarg3", szEmpty, "Test3");
            retObj = m_xsltArg.GetParam("myarg3", szEmpty);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test3", retObj);
            if (retObj.ToString() != "Test3")
                return TEST_FAIL;

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Object is null")]
        public int AddParam15()
        {
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.AddParam("myArg1", szEmpty, null);
            }
            catch (System.ArgumentNullException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("System.ArgumentNullException not thrown for null object");
            return TEST_FAIL;
        }

        [Variation("Add/remove object many times", Param = "AddParam16.txt")]
        public int AddParam16()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();
            String obj = "Test";

            for (int i = 0; i < 200; i++)
            {
                m_xsltArg.AddParam("myArg2", szEmpty, obj + i);
                retObj = m_xsltArg.GetParam("myArg2", szEmpty);
                if (retObj.ToString() != ("Test" + i))
                {
                    CError.WriteLine("Failed to add/remove iteration {0}", i);
                    return TEST_FAIL;
                }
                m_xsltArg.RemoveParam("myArg2", szEmpty);
            }

            for (int i = 0; i < 200; i++)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, obj + i);
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj.ToString() != (obj + i))
                {
                    CError.WriteLine("Failed in 2nd part to add/remove iteration {0}", i);
                    return TEST_FAIL;
                }
                m_xsltArg.RemoveParam("myArg2", szEmpty);
            }

            for (int i = 0; i < 200; i++)
            {
                m_xsltArg.RemoveParam("myArg" + i, szEmpty);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, obj + "2");
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj.ToString() != "Test2")
                return TEST_FAIL;
            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Whitespace in URI and param", Param = "AddParam17.txt")]
        public int AddParam17()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
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
                    CError.WriteLine("Improperly reported an exception for a whitespace value");
                    return TEST_FAIL;
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
                CError.WriteLine("At least one whitespace test failed.");
                return TEST_FAIL;
            }

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Adding many objects", Param = "AddParam18.txt")]
        public int AddParam18()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();
            String obj = "Test";

            for (int i = 1; i < 7; i++)
            {
                m_xsltArg.AddParam("myArg" + +i, szEmpty, obj + i);
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj.ToString() != ("Test" + i))
                    return TEST_FAIL;
            }

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Add same object many times", Param = "AddParam19.txt")]
        public int AddParam19()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();
            String obj = "Test";

            for (int i = 0; i < 300; i++)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, obj + "1");
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj.ToString() != ("Test" + "1"))
                {
                    CError.WriteLine("Failed to add {0}", "myArg" + i);
                    return TEST_FAIL;
                }
                m_xsltArg.RemoveParam("myArg" + i, szEmpty);
            }

            m_xsltArg.AddParam("myArg2", szEmpty, "Test2");
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj.ToString() != ("Test2"))
                return TEST_FAIL;
            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Using Different XSLT namespace", Param = "AddParam20.txt")]
        public int AddParam20()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();

            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", "urn:" + szXslNS, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", "urn:" + szXslNS);
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
                return TEST_FAIL;

            m_xsltArg.AddParam("myArg2", "urn:tmp", "Test2");
            retObj = m_xsltArg.GetParam("myArg2", "urn:tmp");
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test2", retObj);
            if (retObj.ToString() != "Test2")
                return TEST_FAIL;

            m_xsltArg.AddParam("myArg3", "urn:my-object", "Test3");
            retObj = m_xsltArg.GetParam("myArg3", "urn:my-object");
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test3", retObj);
            if (retObj.ToString() != "Test3")
                return TEST_FAIL;

            m_xsltArg.AddParam("myArg4", "urn:MY-OBJECT", "Test4");
            retObj = m_xsltArg.GetParam("myArg4", "urn:MY-OBJECT");
            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test4", retObj);
            if (retObj.ToString() != "Test4")
                return TEST_FAIL;

            if ((LoadXSL("showParamNS.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Using Default XSLT namespace")]
        public int AddParam21()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("myArg1", szXslNS, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szXslNS);

            if (CError.Compare(retObj.ToString() == "Test1", "Expected myArg1 = Test1"))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Parameters should not be cached")]
        public int AddExtObject32()
        {
            if (LoadXSL("test_Param.xsl") == TEST_PASS)
            {
                m_xsltArg = new XsltArgumentList();
                m_xsltArg.AddParam("myParam1", szEmpty, "first");

                // Transform once
                if ((Transform_ArgList("foo.xml") == TEST_PASS) && (CheckResult(383.6292620645) == TEST_PASS))
                {
                    m_xsltArg = new XsltArgumentList();
                    m_xsltArg.AddParam("myParam1", szEmpty, "second");

                    // Transform again to make sure that parameter from first transform are not cached
                    if ((Transform_ArgList("foo.xml") == TEST_PASS) && (CheckResult(384.9801823644) == TEST_PASS))
                        return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }
    }

    /***************************************************************/
    /*               XsltArgumentList.AddParam Misc Tests          */
    /*Bug 268515 - Global param value is overridden by local value */
    /***************************************************************/

    //Testcases with Reader outputs are skipped because they don't write to an output file
    [TestCase(Name = "XsltArgumentList - AddParam Misc : Reader, Stream", Desc = "READER,STREAM")]
    [TestCase(Name = "XsltArgumentList - AddParam Misc : Reader, Writer", Desc = "READER,WRITER")]
    [TestCase(Name = "XsltArgumentList - AddParam Misc : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    [TestCase(Name = "XsltArgumentList - AddParam Misc : URI, Stream", Desc = "URI,STREAM")]
    [TestCase(Name = "XsltArgumentList - AddParam Misc : URI, Writer", Desc = "URI,WRITER")]
    [TestCase(Name = "XsltArgumentList - AddParam Misc : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    [TestCase(Name = "XsltArgumentList - AddParam Misc : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    [TestCase(Name = "XsltArgumentList - AddParam Misc : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    [TestCase(Name = "XsltArgumentList - AddParam Misc : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CArgAddParamMisc : XsltApiTestCaseBase
    {
        //All the below variations, there is no parameter sent and default global value is set

        //global param is xsl:param local param is xsl:param
        [Variation(id = 1, Pri = 2, Desc = "No param sent, global param used, local param exists with a default value", Params = new object[] { "AddParameterA1.xsl", "default local" })]
        [Variation(id = 2, Pri = 2, Desc = "No param sent, global param used, local param exists with no default value", Params = new object[] { "AddParameterA2.xsl", "" })]
        [Variation(id = 3, Pri = 2, Desc = "No param sent, global param used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterA3.xsl", "default global" })]
        [Variation(id = 4, Pri = 2, Desc = "No param sent, global param used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterA4.xsl", "with-param" })]
        [Variation(id = 5, Pri = 2, Desc = "No param sent, global param used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterA5.xsl", "" })]
        [Variation(id = 6, Pri = 2, Desc = "No param sent, global param used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterA6.xsl", "default global" })]
        [Variation(id = 7, Pri = 2, Desc = "No param sent, global param used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterA7.xsl", "default global" })]

        //global param is xsl:variable local param is xsl:param
        [Variation(id = 8, Pri = 2, Desc = "No param sent, global variable used, local param exists with a default value", Params = new object[] { "AddParameterDA1.xsl", "default local" })]
        [Variation(id = 9, Pri = 2, Desc = "No param sent, global variable used, local param exists with no default value", Params = new object[] { "AddParameterDA2.xsl", "" })]
        [Variation(id = 10, Pri = 2, Desc = "No param sent, global variable used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterDA3.xsl", "default global" })]
        [Variation(id = 11, Pri = 2, Desc = "No param sent, global variable used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterDA4.xsl", "with-param" })]
        [Variation(id = 12, Pri = 2, Desc = "No param sent, global variable used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterDA5.xsl", "" })]
        [Variation(id = 13, Pri = 2, Desc = "No param sent, global variable used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterDA6.xsl", "default global" })]
        [Variation(id = 14, Pri = 2, Desc = "No param sent, global variable used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterDA7.xsl", "default global" })]

        //global param is xsl:param local param is xsl:variable
        [Variation(id = 15, Pri = 2, Desc = "No param sent, global param used, local variable exists with a default value", Params = new object[] { "AddParameterEA1.xsl", "default local" })]
        [Variation(id = 16, Pri = 2, Desc = "No param sent, global param used, local variable exists with no default value", Params = new object[] { "AddParameterEA2.xsl", "" })]
        [Variation(id = 17, Pri = 2, Desc = "No param sent, global param used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterEA3.xsl", "default global" })]
        [Variation(id = 18, Pri = 2, Desc = "No param sent, global param used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterEA4.xsl", "default local" })]
        [Variation(id = 19, Pri = 2, Desc = "No param sent, global param used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterEA5.xsl", "" })]
        [Variation(id = 20, Pri = 2, Desc = "No param sent, global param used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterEA6.xsl", "default global" })]
        [Variation(id = 21, Pri = 2, Desc = "No param sent, global param used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterEA7.xsl", "default global" })]

        //global param is xsl:variable local param is xsl:variable
        [Variation(id = 22, Pri = 2, Desc = "No param sent, global variable used, local variable exists with a default value", Params = new object[] { "AddParameterFA1.xsl", "default local" })]
        [Variation(id = 23, Pri = 2, Desc = "No param sent, global variable used, local variable exists with no default value", Params = new object[] { "AddParameterFA2.xsl", "" })]
        [Variation(id = 24, Pri = 2, Desc = "No param sent, global variable used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterFA3.xsl", "default global" })]
        [Variation(id = 25, Pri = 2, Desc = "No param sent, global variable used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterFA4.xsl", "default local" })]
        [Variation(id = 26, Pri = 2, Desc = "No param sent, global variable used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterFA5.xsl", "" })]
        [Variation(id = 27, Pri = 2, Desc = "No param sent, global variable used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterFA6.xsl", "default global" })]
        [Variation(id = 28, Pri = 2, Desc = "No param sent, global variable used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterFA7.xsl", "default global" })]
        public int AddParam1()
        {
            m_xsltArg = new XsltArgumentList();
            string xslFile = CurVariation.Params[0].ToString();
            string expected = "<result>" + CurVariation.Params[1].ToString() + "</result>";

            if ((LoadXSL(xslFile) == TEST_PASS) && (Transform_ArgList("AddParameter.xml") == TEST_PASS))
                return VerifyResult(expected);
            else
                return TEST_FAIL;
        }

        //All the below variations, param is sent from client code

        //global param is xsl:param local param is xsl:param
        [Variation(id = 29, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value", Params = new object[] { "AddParameterB1.xsl", "default local" })]
        [Variation(id = 30, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value", Params = new object[] { "AddParameterB2.xsl", "" })]
        [Variation(id = 31, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterB3.xsl", "outside param" })]
        [Variation(id = 32, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterB4.xsl", "with-param" })]
        [Variation(id = 33, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterB5.xsl", "" })]
        [Variation(id = 34, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterB6.xsl", "outside param" })]
        [Variation(id = 35, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterB7.xsl", "outside param" })]

        //global param is xsl:variable local param is xsl:param
        [Variation(id = 36, Pri = 2, Desc = "Param sent, global variable used, local param exists with a default value", Params = new object[] { "AddParameterDB1.xsl", "default local" })]
        [Variation(id = 37, Pri = 2, Desc = "Param sent, global variable used, local param exists with no default value", Params = new object[] { "AddParameterDB2.xsl", "" })]
        [Variation(id = 38, Pri = 2, Desc = "Param sent, global variable used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterDB3.xsl", "default global" })]
        [Variation(id = 39, Pri = 2, Desc = "Param sent, global variable used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterDB4.xsl", "with-param" })]
        [Variation(id = 40, Pri = 2, Desc = "Param sent, global variable used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterDB5.xsl", "" })]
        [Variation(id = 41, Pri = 2, Desc = "Param sent, global variable used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterDB6.xsl", "default global" })]
        [Variation(id = 42, Pri = 2, Desc = "Param sent, global variable used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterDB7.xsl", "default global" })]

        //global param is xsl:param local param is xsl:variable
        [Variation(id = 43, Pri = 2, Desc = "Param sent, global param used, local variable exists with a default value", Params = new object[] { "AddParameterEB1.xsl", "default local" })]
        [Variation(id = 44, Pri = 2, Desc = "Param sent, global param used, local variable exists with no default value", Params = new object[] { "AddParameterEB2.xsl", "" })]
        [Variation(id = 45, Pri = 2, Desc = "Param sent, global param used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterEB3.xsl", "outside param" })]
        [Variation(id = 46, Pri = 2, Desc = "Param sent, global param used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterEB4.xsl", "default local" })]
        [Variation(id = 47, Pri = 2, Desc = "Param sent, global param used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterEB5.xsl", "" })]
        [Variation(id = 48, Pri = 2, Desc = "Param sent, global param used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterEB6.xsl", "outside param" })]
        [Variation(id = 49, Pri = 2, Desc = "Param sent, global param used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterEB7.xsl", "outside param" })]

        //global param is xsl:variable local param is xsl:variable
        [Variation(id = 50, Pri = 2, Desc = "Param sent, global variable used, local variable exists with a default value", Params = new object[] { "AddParameterFB1.xsl", "default local" })]
        [Variation(id = 51, Pri = 2, Desc = "Param sent, global variable used, local variable exists with no default value", Params = new object[] { "AddParameterFB2.xsl", "" })]
        [Variation(id = 52, Pri = 2, Desc = "Param sent, global variable used, local variable doesn't exist but reference to param exists", Params = new object[] { "AddParameterFB3.xsl", "default global" })]
        [Variation(id = 53, Pri = 2, Desc = "Param sent, global variable used, local variable exists with a default value and with-param sends a value", Params = new object[] { "AddParameterFB4.xsl", "default local" })]
        [Variation(id = 54, Pri = 2, Desc = "Param sent, global variable used, local variable exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterFB5.xsl", "" })]
        [Variation(id = 55, Pri = 2, Desc = "Param sent, global variable used, local variable doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterFB6.xsl", "default global" })]
        [Variation(id = 56, Pri = 2, Desc = "Param sent, global variable used, local variable doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterFB7.xsl", "default global" })]
        public int AddParam2()
        {
            string xslFile = CurVariation.Params[0].ToString();
            string expected = "<result>" + CurVariation.Params[1].ToString() + "</result>";

            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("param1", "", "outside param");

            if ((LoadXSL(xslFile) == TEST_PASS) && (Transform_ArgList("AddParameter.xml") == TEST_PASS))
                return VerifyResult(expected);
            else
                return TEST_FAIL;
        }

        //All the below variations, empty param is sent from client code
        //global param is xsl:param local param is xsl:param
        [Variation(id = 57, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value", Params = new object[] { "AddParameterB1.xsl", "default local" })]
        [Variation(id = 58, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value", Params = new object[] { "AddParameterB2.xsl", "" })]
        [Variation(id = 59, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist but reference to param exists", Params = new object[] { "AddParameterB3.xsl", "" })]
        [Variation(id = 60, Pri = 2, Desc = "Param sent, global param used, local param exists with a default value and with-param sends a value", Params = new object[] { "AddParameterB4.xsl", "with-param" })]
        [Variation(id = 61, Pri = 2, Desc = "Param sent, global param used, local param exists with no default value and with-param doesn't send a value", Params = new object[] { "AddParameterB5.xsl", "" })]
        [Variation(id = 62, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends a value", Params = new object[] { "AddParameterB6.xsl", "" })]
        [Variation(id = 63, Pri = 2, Desc = "Param sent, global param used, local param doesn't exist, reference to param, with-param sends no value", Params = new object[] { "AddParameterB7.xsl", "" })]
        public int AddParam3()
        {
            string xslFile = CurVariation.Params[0].ToString();
            string expected = "<result>" + CurVariation.Params[1].ToString() + "</result>";

            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("param1", "", "");

            if ((LoadXSL(xslFile) == TEST_PASS) && (Transform_ArgList("AddParameter.xml") == TEST_PASS))
                return VerifyResult(expected);
            else
                return TEST_FAIL;
        }
    }

    /***********************************************************/
    /*          XsltArgumentList.AddExtensionObject            */
    /***********************************************************/

    [TestCase(Name = "XsltArgumentList - AddExtensionObject : Reader , Reader", Desc = "READER,READER")]
    [TestCase(Name = "XsltArgumentList - AddExtensionObject : Reader, Stream", Desc = "READER,STREAM")]
    [TestCase(Name = "XsltArgumentList - AddExtensionObject : Reader, Writer", Desc = "READER,WRITER")]
    [TestCase(Name = "XsltArgumentList - AddExtensionObject : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    [TestCase(Name = "XsltArgumentList - AddExtensionObject : URI, Reader", Desc = "URI,READER")]
    [TestCase(Name = "XsltArgumentList - AddExtensionObject : URI, Stream", Desc = "URI,STREAM")]
    [TestCase(Name = "XsltArgumentList - AddExtensionObject : URI, Writer", Desc = "URI,WRITER")]
    [TestCase(Name = "XsltArgumentList - AddExtensionObject : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    [TestCase(Name = "XsltArgumentList - AddExtensionObject : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    [TestCase(Name = "XsltArgumentList - AddExtensionObject : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    [TestCase(Name = "XsltArgumentList - AddExtensionObject : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    [TestCase(Name = "XsltArgumentList - AddExtensionObject : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CArgAddExtObj : XsltApiTestCaseBase
    {
        private PermissionSet nonePermSet = new PermissionSet(PermissionState.None);

        [Variation(Desc = "Basic Verification Test", Pri = 1, Param = "myObjectDef.txt")]
        public int AddExtObject1()
        {
            MyObject obj = new MyObject(1);
            m_xsltArg = new XsltArgumentList();
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            nonePermSet.PermitOnly();
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("namespace URI = null")]
        public int AddExtObject2()
        {
            MyObject obj = new MyObject(2);
            m_xsltArg = new XsltArgumentList();

            try
            {
                nonePermSet.PermitOnly(); ;
                m_xsltArg.AddExtensionObject(null, obj);
            }
            catch (System.ArgumentNullException)
            {
                CodeAccessPermission.RevertPermitOnly();
                return TEST_PASS;
            }
            CError.WriteLine("System.ArgumentNullException not generated for null namespace uri");
            return TEST_FAIL;
        }

        [Variation("namespace URI is empty string")]
        public int AddExtObject3()
        {
            MyObject obj = new MyObject(3);
            m_xsltArg = new XsltArgumentList();

            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szEmpty, obj);
            CodeAccessPermission.RevertPermitOnly();
            return TEST_PASS;
        }

        [Variation("Very long namespace URI", Param = "myObjectLongNs.txt")]
        public int AddExtObject4()
        {
            m_xsltArg = new XsltArgumentList();
            MyObject obj = new MyObject(4);
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szLongNS, obj);
            CodeAccessPermission.RevertPermitOnly();

            if ((LoadXSL("MyObjectLongNS.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Invalid namespace URI")]
        public int AddExtObject5()
        {
            MyObject obj = new MyObject(5);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szInvalid, obj);
            CodeAccessPermission.RevertPermitOnly();
            return TEST_PASS;
        }

        [Variation("Same Namespace different objects")]
        public int AddExtObject7()
        {
            MyObject obj1 = new MyObject(1);
            MyObject obj2 = new MyObject(2);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj1);
            try
            {
                m_xsltArg.AddExtensionObject(szDefaultNS, obj2);
            }
            catch (System.ArgumentException)
            {
                CodeAccessPermission.RevertPermitOnly();
                return TEST_PASS;
            }
            CError.WriteLine("Did not launch exception 'System.ArgumentException' for an item already added");
            return TEST_FAIL;
        }

        [Variation("Case sensitivity", Param = "myObjectDef.txt")]
        public int AddExtObject8()
        {
            MyObject obj = new MyObject(1);
            m_xsltArg = new XsltArgumentList();
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject("urn:my-object", obj);

            retObj = m_xsltArg.GetExtensionObject("urn:my-object");
            if (((MyObject)retObj).MyValue() != obj.MyValue())
            {
                CError.WriteLine("Set and retrieved value appear to be different");
                return TEST_FAIL;
            }
            m_xsltArg.AddExtensionObject("URN:MY-OBJECT", obj);
            m_xsltArg.AddExtensionObject("urn:My-Object", obj);
            m_xsltArg.AddExtensionObject("urn-my:object", obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Set a null object")]
        public int AddExtObject9()
        {
            MyObject obj = new MyObject(9);
            m_xsltArg = new XsltArgumentList();

            try
            {
                nonePermSet.PermitOnly(); ;
                m_xsltArg.AddExtensionObject(szDefaultNS, null);
            }
            catch (System.ArgumentNullException)
            {
                CodeAccessPermission.RevertPermitOnly();
                return TEST_PASS;
            }
            CError.WriteLine("Did not launch exception 'System.ArgumentNullException' for adding a null-valued item");
            return TEST_FAIL;
        }

        [Variation("Unitialized and NULL return values from the methods in the extension object")]
        public int AddExtObject10()
        {
            MyObject obj = new MyObject(10);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObject_Null.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (CheckResult(424.8906559839) == TEST_PASS || CheckResult(425.0247531107) == TEST_PASS /* for writer */))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Add many objects", Param = "myObjectDef.txt")]
        public int AddExtObject11()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            MyObject obj1 = new MyObject(100);
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj1);

            for (int i = 1; i < 500; i++)
            {
                MyObject obj = new MyObject(i);
                m_xsltArg.AddExtensionObject(szDefaultNS + i, obj);
            }

            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Whitespace")]
        public int AddExtObject12()
        {
            int i = 1;
            m_xsltArg = new XsltArgumentList();

            nonePermSet.PermitOnly(); ;
            foreach (String str in szWhiteSpace)
            {
                MyObject obj = new MyObject(i);
                m_xsltArg.AddExtensionObject(szDefaultNS + str, obj);
                i++;
            }
            CodeAccessPermission.RevertPermitOnly();

            try
            {
                if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS))
                    Transform_ArgList("fruits.xml", true);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Did not throw expected exception");
            return TEST_FAIL;
        }

        [Variation("Add object many times")]
        public int AddExtObject13()
        {
            MyObject obj = new MyObject(13);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            try
            {
                m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            }
            catch (System.ArgumentException)
            {
                CodeAccessPermission.RevertPermitOnly();
                return TEST_PASS;
            }
            CError.WriteLine("Did not exception for adding an extension object that already exists");
            return TEST_FAIL;
        }

        [Variation("Add and Remove multiple times", Param = "myObjectDef.txt")]
        public int AddExtObject14()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            MyObject obj = new MyObject(14);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            for (int i = 0; i < 400; i++)
            {
                m_xsltArg.AddExtensionObject(szDefaultNS, obj);
                m_xsltArg.RemoveExtensionObject(szDefaultNS);
            }
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Namespace URI non-existent")]
        public int AddExtObject15()
        {
            MyObject obj = new MyObject(15);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szSimple, obj);
            CodeAccessPermission.RevertPermitOnly();
            try
            {
                if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS))
                    Transform_ArgList("fruits.xml", true);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Did not throw expected exception");
            return TEST_FAIL;
        }

        [Variation("Accessing Private and protected Items")]
        public int AddExtObject16()
        {
            MyObject obj = new MyObject(1);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
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
                        return TEST_PASS;
                    }
                }
            }
            return TEST_FAIL;
        }

        [Variation("Writing To Output")]
        public int AddExtObject17()
        {
            MyObject obj = new MyObject(17);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObject_ConsoleWrite.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (CheckResult(421.8660259804) == TEST_PASS || CheckResult(421.8527116762) == TEST_PASS /* for writer */))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Recursive Functions", Param = "myObject_Recursion.txt")]
        public int AddExtObject18()
        {
            MyObject obj = new MyObject(18);
            m_xsltArg = new XsltArgumentList();
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObject_Recursion.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Function-exists tests", Param = "MyObject_FnExists.txt")]
        public int AddExtObject20()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            MyObject obj = new MyObject(20);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObject_FnExists.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Argument Tests", Param = "MyObject_Arguments.txt")]
        public int AddExtObject21()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            MyObject obj = new MyObject(1);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObject_Arguments.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Multiple Objects in same NameSpace")]
        public int AddExtObject24()
        {
            m_xsltArg = new XsltArgumentList();

            double d = 1;
            int i = 1;
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject("urn:myspace", d);
            try
            {
                m_xsltArg.AddExtensionObject("urn:myspace", i);
            }
            catch (System.ArgumentException)
            {
                CodeAccessPermission.RevertPermitOnly();
                return TEST_PASS;
            }
            CError.WriteLine("Exception not thrown for URI namespace already in use");
            return TEST_FAIL;
        }

        [Variation("Case Sensitivity")]
        public int AddExtObject25()
        {
            MyObject obj = new MyObject(25);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if (LoadXSL("MyObject_CaseSensitive.xsl") == TEST_PASS)
            {
                try
                {
                    Transform_ArgList("fruits.xml");
                    CheckResult(419.3031944636);
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Exception not thrown for wrong-case spelling of methods");
            return TEST_FAIL;
        }

        [Variation("Object namespace not found")]
        public int AddExtObject26()
        {
            MyObject obj = new MyObject(26);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObject_NotFoundNS.xsl") == TEST_PASS))
            {
                try
                {
                    Transform_ArgList("fruits.xml", true);
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Exception not thrown for NS not found");
            return TEST_FAIL;
        }

        [Variation("Maintaining State", Param = "MyObject_KeepingState.txt")]
        public int AddExtObject27()
        {
            MyObject obj = new MyObject(27);
            m_xsltArg = new XsltArgumentList();
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObject_KeepingState.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Deliberately Messing Up the Stylesheet", Param = "MyObject_KillerStrings.txt")]
        public int AddExtObject28()
        {
            MyObject obj = new MyObject(28);
            m_xsltArg = new XsltArgumentList();
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObject_KillerStrings.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS))
            {
                if (MyOutputType() == OutputType.Writer)
                    /* writer output is slighlty different which causes a mismatch so we won't compare */
                    return TEST_PASS;
                return VerifyResult(Baseline, _strOutFile);
            }
            else
                return TEST_FAIL;
        }

        [Variation("Function not found in Object")]
        public int AddExtObject29()
        {
            MyObject obj = new MyObject(29);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObject_NotFound.xsl") == TEST_PASS))
            {
                try
                {
                    Transform_ArgList("fruits.xml", true);
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Exception not thrown for method not found");
            return TEST_FAIL;
        }

        [Variation("Using Default XSLT namespace")]
        public int AddExtObject31()
        {
            MyObject obj = new MyObject(31);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szXslNS, obj);
            retObj = m_xsltArg.GetExtensionObject(szXslNS);
            CodeAccessPermission.RevertPermitOnly();

            if (CError.Compare(retObj, obj, "Expected Object : " + obj.ToString()))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Extension objects should not be cached during Transform()", Param = "Bug78587")]
        public int AddExtObject32()
        {
            string Baseline1 = "baseline\\" + CurVariation.Param.ToString() + "a.txt";
            string Baseline2 = "baseline\\" + CurVariation.Param.ToString() + "b.txt";

            if (LoadXSL("Bug78587.xsl") == TEST_PASS)
            {
                m_xsltArg = new XsltArgumentList();
                m_xsltArg.AddExtensionObject("id", new Id("first"));
                m_xsltArg.AddExtensionObject("capitalizer", new Capitalizer());

                // Transform once
                if ((Transform_ArgList("Bug78587.xml") == TEST_PASS) && (VerifyResult(Baseline1, _strOutFile) == TEST_PASS))
                {
                    m_xsltArg = new XsltArgumentList();
                    m_xsltArg.AddExtensionObject("id", new Id("second"));
                    m_xsltArg.AddExtensionObject("capitalizer", new Capitalizer());

                    // Transform again to make sure that extension objects from first transform are not cached
                    if ((Transform_ArgList("Bug78587.xml") == TEST_PASS) && (VerifyResult(Baseline2, _strOutFile) == TEST_PASS))
                        return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        [Variation(id = 33, Desc = "Calling extension object from select in xsl:apply-templates", Params = new object[] { "apply-templates.xsl", "apply-templates.txt" })]
        [Variation(id = 34, Desc = "Calling extension object from select in xsl:for-each", Params = new object[] { "for-each.xsl", "for-each.txt" })]
        [Variation(id = 35, Desc = "Calling extension object from select in xsl:copy-of", Params = new object[] { "copy-of.xsl", "copy-of.txt" })]
        [Variation(id = 36, Desc = "Calling extension object from select in xsl:sort", Params = new object[] { "sort.xsl", "sort.txt" })]
        [Variation(id = 37, Desc = "Calling extension object from select in xsl:variable", Params = new object[] { "variable.xsl", "variable.txt" })]
        [Variation(id = 38, Desc = "Calling extension object from select in xsl:param", Params = new object[] { "param.xsl", "param.txt" })]
        [Variation(id = 39, Desc = "Calling extension object from select in xsl:with-param", Params = new object[] { "with-param.xsl", "with-param.txt" })]
        [Variation(id = 40, Desc = "Calling extension object from select in xsl:value-of", Params = new object[] { "value-of.xsl", "value-of.txt" })]
        public int AddExtObject33()
        {
            ExObj obj = new ExObj(0);
            m_xsltArg = new XsltArgumentList();
            string xslFile = CurVariation.Params[0].ToString();
            string Baseline = "baseline\\" + CurVariation.Params[1].ToString();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject("urn-myobject", obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL(xslFile) == TEST_PASS) && (Transform_ArgList("ExtData.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation(id = 41, Desc = "Calling extension function from select in xsl:variable and variable is used for incrementing an integer", Params = new object[] { "variable2.xsl", "variable2.txt" })]
        [Variation(id = 42, Desc = "Calling extension function from select in xsl:variable but variable is never used", Params = new object[] { "variable3.xsl", "variable3.txt" })]
        [Variation(id = 43, Desc = "Calling extension function from select in global xsl:variable but variable is never used", Params = new object[] { "variable4.xsl", "variable4.txt" })]
        [Variation(id = 44, Desc = "Calling extension function from select in xsl:param and parameter is used for incrementing an integer", Params = new object[] { "param2.xsl", "param2.txt" })]
        [Variation(id = 45, Desc = "Calling extension function from select in xsl:param but parameter is never used", Params = new object[] { "param3.xsl", "param3.txt" })]
        [Variation(id = 46, Desc = "Calling extension function from select in global xsl:param but parameter is never used", Params = new object[] { "param4.xsl", "param4.txt" })]
        public int AddExtObject41()
        {
            /*
             * In these variations, the XSLT calls the extension function Increment from XSLT.
             * In some cases, the variable is never used in the XSLT (Bug 357711)
             * Verify by calling an extenfion function like increment and check the state of the variable
            */
            ExObj obj = new ExObj(0);
            m_xsltArg = new XsltArgumentList();
            string xslFile = CurVariation.Params[0].ToString();
            string Baseline = "baseline\\" + CurVariation.Params[1].ToString();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject("urn-myobject", obj);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL(xslFile) == TEST_PASS) && (Transform_ArgList("ExtData.xml") == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }
    }

    public class ExObj : XsltApiTestCaseBase
    {
        public ExObj(int i)
        {
            count = 0;
        }

        //Return a node-set
        public XPathNodeIterator ReturnNodeSet(string xpath)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<books><book><title>XML Primer</title><author>A</author></book><book><title>XSLT Basics</title><author>B</author></book><book><title>Advanced XSLT</title><author>C</author></book></books>");
            XPathNavigator nav = doc.CreateNavigator();
            XPathNodeIterator iterator = nav.Select(xpath);
            return iterator;
        }

        public static int count;

        public int Increment()
        {
            return ++count;
        }
    }

    /***********************************************************/
    /*            XsltArgumentList.RemoveParam                 */
    /***********************************************************/

    [TestCase(Name = "XsltArgumentList - RemoveParam : Reader , Reader", Desc = "READER,READER")]
    [TestCase(Name = "XsltArgumentList - RemoveParam : URI, Stream", Desc = "URI,STREAM")]
    [TestCase(Name = "XsltArgumentList - RemoveParam : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    [TestCase(Name = "XsltArgumentList - RemoveParam : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CArgRemoveParam : XsltApiTestCaseBase
    {
        private string baseline = string.Empty;

        [Variation(id = 1, Desc = "Basic Verification Test", Pri = 1, Param = "RemoveParam1.txt")]
        public int RemoveParam1()
        {
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test2");
            m_xsltArg.RemoveParam("myArg1", szEmpty);
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Value of Removed Object is not null : {0}", retObj);
                return TEST_FAIL;
            }
            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);

            CError.WriteLine("Added Value:{0}\nRetrieved Value: {1}", "Test1", retObj);
            if (retObj.ToString() != "Test1")
            {
                CError.WriteLine("Value of removed object is not as expected : {0}", retObj);
                return TEST_FAIL;
            }

            baseline = "baseline\\" + CurVariation.Param.ToString();
            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(baseline, "out.xml") == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation(id = 2, Desc = "Param name is null", Pri = 1, Param = "RemoveParam2.txt")]
        public int RemoveParam2()
        {
            m_xsltArg = new XsltArgumentList();
            retObj = m_xsltArg.RemoveParam(null, szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Did not return NULL for null parameter name");
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Param name is empty string", Param = "showParam.txt")]
        public int RemoveParam3()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.RemoveParam(szEmpty, szEmpty);

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Param name is non-existent", Param = "showParam.txt")]
        public int RemoveParam4()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.RemoveParam(szSimple, szEmpty);

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Invalid Param name", Param = "showParam.txt")]
        public int RemoveParam5()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.RemoveParam(szInvalid, szEmpty);

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Very long param name", Param = "showParamLongName.txt")]
        public int RemoveParam6()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam(szLongString, szEmpty, "Test1");
            m_xsltArg.RemoveParam(szLongString, szEmpty);

            if ((LoadXSL("showParamLongName.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Namespace URI is null")]
        public int RemoveParam7()
        {
            m_xsltArg = new XsltArgumentList();

            retObj = m_xsltArg.RemoveParam("myArg1", null);
            if (retObj != null)
            {
                CError.WriteLine("Did not return NULL for null URI namespace");
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Namespace URI is empty string", Param = "showParam.txt")]
        public int RemoveParam8()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myArg1", szEmpty);

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Namespace URI is non-existent", Param = "RemoveParam9.txt")]
        public int RemoveParam9()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myArg1", "http://www.xsltTest.com");

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Very long namespace URI", Param = "showParam.txt")]
        public int RemoveParam10()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szLongString, "Test1");
            m_xsltArg.RemoveParam("myArg1", szLongString);

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Different Data Types", Param = "showParam.txt")]
        public int RemoveParam11()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();

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
                CError.WriteLine("Failed to remove {0}", d1);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg2", szEmpty, d2);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", d2);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg3", szEmpty, d3);
            m_xsltArg.RemoveParam("myArg3", szEmpty);
            retObj = m_xsltArg.GetParam("myArg3", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", d3);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg4", szEmpty, d4);
            m_xsltArg.RemoveParam("myArg4", szEmpty);
            retObj = m_xsltArg.GetParam("myArg4", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", d4);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg5", szEmpty, d5);
            m_xsltArg.RemoveParam("myArg5", szEmpty);
            retObj = m_xsltArg.GetParam("myArg5", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", d5);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg6", szEmpty, d6);
            m_xsltArg.RemoveParam("myArg6", szEmpty);
            retObj = m_xsltArg.GetParam("myArg6", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", d6);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg7", szEmpty, d7);
            m_xsltArg.RemoveParam("myArg7", szEmpty);
            retObj = m_xsltArg.GetParam("myArg7", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", d7);
                return TEST_FAIL;
            }

            String obj = "0.00";

            // string
            m_xsltArg.AddParam("myArg1", szEmpty, obj);
            m_xsltArg.RemoveParam("myArg1", szEmpty);
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", obj);
                return TEST_FAIL;
            }

            //int
            int i = 2;

            m_xsltArg.AddParam("myArg2", szEmpty, i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", i);
                return TEST_FAIL;
            }

            Boolean bF = (1 == 0);
            m_xsltArg.AddParam("myArg4", szEmpty, bF);
            m_xsltArg.RemoveParam("myArg4", szEmpty);
            retObj = m_xsltArg.GetParam("myArg4", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", bF);
                return TEST_FAIL;
            }

            Boolean bT = (1 == 1);
            m_xsltArg.AddParam("myArg5", szEmpty, bT);
            m_xsltArg.RemoveParam("myArg5", szEmpty);
            retObj = m_xsltArg.GetParam("myArg5", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", bT);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (Int16)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", i);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (UInt16)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", i);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (Int32)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", i);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (UInt32)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", i);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (Int64)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", i);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (UInt64)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", i);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (Single)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", i);
                return TEST_FAIL;
            }

            m_xsltArg.AddParam("myArg2", szEmpty, (Decimal)i);
            m_xsltArg.RemoveParam("myArg2", szEmpty);
            retObj = m_xsltArg.GetParam("myArg2", szEmpty);
            if (retObj != null)
            {
                CError.WriteLine("Failed to remove {0}", i);
                return TEST_FAIL;
            }

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Case Sensitivity", Param = "RemoveParam12.txt")]
        public int RemoveParam12()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myarg1", szEmpty);
            m_xsltArg.RemoveParam("MYARG1", szEmpty);
            m_xsltArg.RemoveParam("myArg1 ", szEmpty);

            // perform a transform for kicks and ensure all is ok.
            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Whitespace", Param = "RemoveParam13.txt")]
        public int RemoveParam13()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            int i = 1;
            m_xsltArg = new XsltArgumentList();

            foreach (String str in szWhiteSpace)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, "Test" + str);
                m_xsltArg.RemoveParam("myArg" + i, szEmpty);
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj != null)
                {
                    CError.WriteLine("Error removing case #{0} from this test", i);
                    return TEST_FAIL;
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
                    CError.WriteLine("Error removing case #{0} in the second batch from this test", i);
                    return TEST_FAIL;
                }
                i++;
            }

            // perform a transform for kicks and ensure all is ok.
            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Call Multiple Times", Param = "showParam.txt")]
        public int RemoveParam14()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            m_xsltArg.RemoveParam("myArg1", szEmpty);
            for (int i = 0; i < 500; i++)
                m_xsltArg.RemoveParam("myArg1", szEmpty);

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Using Default XSLT Namespace")]
        public int RemoveParam15()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.RemoveParam("myParam", szDefaultNS);

            return TEST_PASS;
        }
    }

    /***********************************************************/
    /*        XslCompiledTransform.RemoveExtensionObject               */
    /***********************************************************/

    [TestCase(Name = "XsltArgumentList - RemoveExtensionObject : Reader, Stream", Desc = "READER,STREAM")]
    [TestCase(Name = "XsltArgumentList - RemoveExtensionObject : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    [TestCase(Name = "XsltArgumentList - RemoveExtensionObject : URI, Reader", Desc = "URI,READER")]
    [TestCase(Name = "XsltArgumentList - RemoveExtensionObject : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    public class CArgRemoveExtObj : XsltApiTestCaseBase
    {
        private PermissionSet nonePermSet = new PermissionSet(PermissionState.None);

        [Variation(Desc = "Basic Verification Test", Pri = 1)]
        public int RemoveExtObj1()
        {
            MyObject obj = new MyObject(1);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.RemoveExtensionObject(szDefaultNS);
            CodeAccessPermission.RevertPermitOnly();

            try
            {
                if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS))
                    Transform_ArgList("fruits.xml", true);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Did not throw expected exception");
            return TEST_FAIL;
        }

        [Variation("Namespace URI is null")]
        public int RemoveExtObj2()
        {
            MyObject obj = new MyObject(2);
            m_xsltArg = new XsltArgumentList();

            try
            {
                m_xsltArg.RemoveExtensionObject(null);
            }
            catch (System.ArgumentNullException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Exception not generated for null parameter name");
            return TEST_FAIL;
        }

        [Variation("Call Multiple Times", Param = "showParam.txt")]
        public int RemoveExtObj3()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            MyObject obj = new MyObject(10);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            for (int i = 0; i < 500; i++)
                m_xsltArg.RemoveExtensionObject(szDefaultNS);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Namespace URI is non-existent", Param = "MyObjectDef.txt")]
        public int RemoveExtObj4()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            MyObject obj = new MyObject(4);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.RemoveExtensionObject(szSimple);
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Very long namespace URI")]
        public int RemoveExtObj5()
        {
            m_xsltArg = new XsltArgumentList();
            MyObject obj = new MyObject(5);
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject("urn:" + szLongNS, obj);
            m_xsltArg.RemoveExtensionObject("urn:" + szLongNS);
            CodeAccessPermission.RevertPermitOnly();
            try
            {
                if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS))
                    Transform_ArgList("fruits.xml", true);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Did not throw expected exception");
            return TEST_FAIL;
        }

        [Variation("Different Data Types", Param = "showParam.txt")]
        public int RemoveExtObj6()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            MyObject obj = new MyObject(6);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
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
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Case Sensitivity", Param = "MyObjectDef.txt")]
        public int RemoveExtObj7()
        {
            MyObject obj = new MyObject(7);
            m_xsltArg = new XsltArgumentList();
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.AddExtensionObject("urn:my-object", obj);

            m_xsltArg.RemoveExtensionObject("URN:MY-OBJECT");
            m_xsltArg.RemoveExtensionObject("urn:My-Object");
            m_xsltArg.RemoveExtensionObject("urn-my:object");
            m_xsltArg.RemoveExtensionObject("urn:my-object ");
            CodeAccessPermission.RevertPermitOnly();
            if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Whitespace")]
        public int RemoveExtObj8()
        {
            int i = 1;
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            foreach (String str in szWhiteSpace)
            {
                MyObject obj = new MyObject(i);

                m_xsltArg.AddExtensionObject(szDefaultNS + str, obj);
                m_xsltArg.RemoveExtensionObject(szDefaultNS + str);
                retObj = m_xsltArg.GetExtensionObject(szDefaultNS + str);
                if (retObj != null)
                {
                    CError.WriteLine("Error deleting case #{0} for whitespace arg", i);
                    return TEST_FAIL;
                }
                i++;
            }
            CodeAccessPermission.RevertPermitOnly();

            try
            {
                if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS))
                    Transform_ArgList("fruits.xml", true);
            }
            catch (System.Xml.Xsl.XsltException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Did not exception for object that could not be executed");
            return TEST_FAIL;
        }

        [Variation("Using default XSLT namespace", Param = "showParam.txt")]
        public int RemoveExtObj9()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            MyObject obj = new MyObject(10);
            m_xsltArg = new XsltArgumentList();
            nonePermSet.PermitOnly(); ;
            m_xsltArg.RemoveExtensionObject(szDefaultNS);
            CodeAccessPermission.RevertPermitOnly();
            // ensure we can still do a transform
            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }
    }

    /***********************************************************/
    /*        XslCompiledTransform.Clear                               */
    /***********************************************************/

    [TestCase(Name = "XsltArgumentList - Clear", Desc = "XsltArgumentList.Clear")]
    public class CArgClear : XsltApiTestCaseBase
    {
        [Variation(Desc = "Basic Verification Test", Pri = 1, Param = "showParam.txt")]
        public int Clear1()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return TEST_SKIPPED;

            m_xsltArg.Clear();
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
                return TEST_FAIL;

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Clear with nothing loaded", Param = "showParam.txt")]
        public int Clear2()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.Clear();
            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Clear Params", Param = "showParam.txt")]
        public int Clear3()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return TEST_SKIPPED;

            m_xsltArg.Clear();
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
                return TEST_FAIL;

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Clear Extension Objects")]
        public int Clear4()
        {
            MyObject obj = new MyObject(26);
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.Clear();
            retObj = m_xsltArg.GetExtensionObject(szDefaultNS);
            if (retObj != null)
            {
                CError.WriteLine("Did not appear to clear an extension object");
                return TEST_FAIL;
            }

            if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS))
            {
                try
                {
                    Transform_ArgList("fruits.xml");
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Exception not thrown for NS not found");
            return TEST_FAIL;
        }

        [Variation("Clear Many Objects", Param = "showParam.txt")]
        public int Clear5()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();
            String obj = "Test";

            for (int i = 0; i < 200; i++)
            {
                m_xsltArg.AddParam("myArg2", szEmpty, obj + i);
                retObj = m_xsltArg.GetParam("myArg2", szEmpty);
                if (retObj.ToString() != (obj + i))
                {
                    CError.WriteLine("Failed to add/remove iteration {0}", i);
                    CError.WriteLine("{0} : {1}", retObj, obj + i);

                    return TEST_FAIL;
                }
                m_xsltArg.Clear();
            }

            for (int i = 0; i < 200; i++)
            {
                m_xsltArg.AddParam("myArg" + i, szEmpty, obj + i);
                retObj = m_xsltArg.GetParam("myArg" + i, szEmpty);
                if (retObj.ToString() != (obj + i))
                {
                    CError.WriteLine("Failed in 2nd part to add/remove iteration {0}", i);
                    return TEST_FAIL;
                }
            }

            //  CError.WriteLine(retObj.GetType());

            m_xsltArg.Clear();

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Clear Multiple Times", Param = "showParam.txt")]
        public int Clear6()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return TEST_SKIPPED;

            for (int i = 0; i < 300; i++)
                m_xsltArg.Clear();
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj != null)
                return TEST_FAIL;

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Loading one object, but clearing another", Param = "ClearParam7.txt")]
        public int Clear7()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();
            XsltArgumentList m_2 = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return TEST_SKIPPED;

            m_2.Clear();

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) &&
                (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Clear after objects have been \"Removed\"", Param = "showParam.txt")]
        public int Clear8()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            m_xsltArg = new XsltArgumentList();

            m_xsltArg.AddParam("myArg1", szEmpty, "Test1");
            retObj = m_xsltArg.GetParam("myArg1", szEmpty);
            if (retObj.ToString() != "Test1")
                return TEST_SKIPPED;
            retObj = m_xsltArg.RemoveParam("myArg1", szEmpty);
            m_xsltArg.Clear();

            if ((LoadXSL("showParam.xsl") != TEST_PASS) || (Transform_ArgList("fruits.xml") != TEST_PASS) ||
                (VerifyResult(Baseline, _strOutFile) != TEST_PASS))
                return TEST_FAIL;

            MyObject obj = new MyObject(26);

            m_xsltArg.AddExtensionObject(szDefaultNS, obj);
            m_xsltArg.RemoveExtensionObject(szDefaultNS);
            m_xsltArg.Clear();

            if ((LoadXSL("MyObjectDef.xsl") == TEST_PASS))
            {
                try
                {
                    Transform_ArgList("fruits.xml");
                }
                catch (System.Xml.Xsl.XsltException)
                {
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Exception not thrown for NS not found");
            return TEST_FAIL;
        }
    }

    [TestCase(Name = "XsltArgumentList - Events", Desc = "Events raised by xsl:message")]
    public class XsltEvents : XsltApiTestCaseBase
    {
        public bool EventRaised;
        public string OutFile = string.Empty;

        public void SerializeMessage(string outFile, string message)
        {
            StreamWriter sw = new StreamWriter(outFile);
            sw.Write(message);
            sw.Close();
        }

        private void argList_XsltMessageEncountered(object sender, XsltMessageEncounteredEventArgs e)
        {
            EventRaised = true;
            CError.WriteLine("---- OnMessageEvent Raised ----");
            CError.WriteLine(e.Message);
            SerializeMessage(OutFile, e.Message);
        }

        [Variation(id = 1, Desc = "OnQueryEvent Exists - xsl:message with terminate='no'", Priority = 0, Params = new object[] { "Message1.xsl", "no", "yes", "Message1.txt" })]
        [Variation(id = 2, Desc = "OnQueryEvent doesn't exist - xsl:message with terminate='no'", Priority = 2, Params = new object[] { "Message2.xsl", "no", "no", "Message2.txt" })]
        [Variation(id = 3, Desc = "OnQueryEvent Exists - xsl:message with terminate='yes'", Priority = 0, Params = new object[] { "Message3.xsl", "yes", "yes", "Message3.txt" })]
        [Variation(id = 4, Desc = "OnQueryEvent doesn't exist - xsl:message with terminate='yes'", Priority = 2, Params = new object[] { "Message4.xsl", "yes", "no", "Message4.txt" })]
        [Variation(id = 5, Desc = "OnQueryEvent Exists - xsl:message with XML Content and terminate='no'", Priority = 1, Params = new object[] { "Message5.xsl", "no", "yes", "Message5.txt" })]
        [Variation(id = 6, Desc = "OnQueryEvent doesn't exist - xsl:message with XML Content and  terminate='no'", Priority = 2, Params = new object[] { "Message6.xsl", "no", "no", "Message6.txt" })]
        [Variation(id = 7, Desc = "OnQueryEvent Exists - xsl:message with XML Content and  terminate='yes'", Priority = 1, Params = new object[] { "Message7.xsl", "yes", "yes", "Message7.txt" })]
        [Variation(id = 8, Desc = "OnQueryEvent doesn't exist - xsl:message with XML Content and  terminate='yes'", Priority = 2, Params = new object[] { "Message8.xsl", "yes", "no", "Message8.txt" })]
        [Variation(id = 9, Desc = "OnQueryEvent Exists - xsl:message with template content and  terminate='no'", Priority = 1, Params = new object[] { "Message9.xsl", "no", "yes", "Message9.txt" })]
        [Variation(id = 10, Desc = "OnQueryEvent Exists - xsl:message with template content and  terminate='yes'", Priority = 1, Params = new object[] { "Message10.xsl", "yes", "yes", "Message10.txt" })]
        public int EventsTests()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            XsltArgumentList argList = new XsltArgumentList();

            //Collect the test data
            string SourceFile = FullFilePath("Message.xml");
            string XslFile = FullFilePath(CurVariation.Params[0].ToString());
            string XslMessageTerminate = CurVariation.Params[1].ToString();
            string EventHandlerExists = CurVariation.Params[2].ToString();
            string Baseline = "baseline\\" + CurVariation.Params[3].ToString();
            OutFile = "Message.txt";

            //Check if the EventHandler Exists
            if (EventHandlerExists.ToLower(CultureInfo.InvariantCulture) == "yes")
                argList.XsltMessageEncountered += new XsltMessageEncounteredEventHandler(argList_XsltMessageEncountered);

            EventRaised = false;

            //Delete the output file if it exists
            if (File.Exists(OutFile))
                File.Delete(OutFile);

            //Create a navigator over the source document
            XPathDocument doc = new XPathDocument(SourceFile);
            XPathNavigator nav = doc.CreateNavigator();

            //Create a temporary output stream
            XmlWriter xw = new XmlTextWriter(CError.Out);

            //Compile the Stylesheet
            CError.WriteLine(XslFile);
            xslt.Load(XslFile);

            if (XslMessageTerminate.ToLower(CultureInfo.InvariantCulture) == "yes")
            {
                try
                {
                    xslt.Transform(nav, argList, xw);
                    CError.WriteLine("**** XsltException NOT Raised ****");
                    return TEST_FAIL;
                }
                catch (XsltException e)
                {
                    CError.WriteLine("----  {0} Raised ----", e.ToString());
                    if (EventHandlerExists == "no")
                        SerializeMessage(OutFile, e.Message);
                }
            }
            else
            {
                xslt.Transform(nav, argList, xw);
            }

            //Verify if the Event is raised and the result is verified
            if (EventRaised)
            {
                return VerifyResult(Baseline, OutFile);
            }
            else
            {
                if (EventHandlerExists == "yes")
                {
                    CError.WriteLine("**** OnMessageEvent NOT Raised ****");
                    return TEST_FAIL;
                }
                else
                {
                    return TEST_PASS;
                }
            }
        }
    }

    [TestCase(Name = "XPathNodeIterator Tests", Desc = "XPathNodeIterator Tests using XsltArgumentList")]
    public class XPathNodeIteratorTests : XsltApiTestCaseBase
    {
        [Variation(id = 1, Desc = "Call Current without MoveNext")]
        public int NodeIter1()
        {
            if (_isInProc)
                return TEST_SKIPPED;

            XslCompiledTransform xslt = new XslCompiledTransform();

            XsltArgumentList xslArg = new XsltArgumentList();
            XmlUrlResolver ur = new XmlUrlResolver();
            Uri uriSource = ur.ResolveUri(null, FullFilePath("sample.xsd"));
            xslArg.AddParam("sourceUri", String.Empty, uriSource.ToString());

            xslt.Load(FullFilePath("xsd2cs1.xsl"), new XsltSettings(true, true), new XmlUrlResolver());

            XPathDocument doc = new XPathDocument(FullFilePath("sample.xsd"));
            StringWriter sw = new StringWriter();
            try
            {
                xslt.Transform(doc, xslArg, sw);
                sw.Close();
                CError.WriteLine("No exception is thrown when .Current is called before .MoveNext on XPathNodeIterator");
                return TEST_FAIL;
            }
            catch (System.InvalidOperationException ex)
            {
                CError.WriteLine(ex.ToString());
                return TEST_PASS;
            }
        }

        [Variation(id = 2, Desc = "Call Current after MoveNext")]
        public int NodeIter2()
        {
            if (_isInProc)
                return TEST_SKIPPED;

            XslCompiledTransform xslt = new XslCompiledTransform();

            XsltArgumentList xslArg = new XsltArgumentList();
            XmlUrlResolver ur = new XmlUrlResolver();
            Uri uriSource = ur.ResolveUri(null, FullFilePath("sample.xsd"));
            xslArg.AddParam("sourceUri", String.Empty, uriSource.ToString());

            xslt.Load(FullFilePath("xsd2cs2.xsl"), new XsltSettings(true, true), new XmlUrlResolver());

            XPathDocument doc = new XPathDocument(FullFilePath("sample.xsd"));
            StringWriter sw = new StringWriter();
            xslt.Transform(doc, xslArg, sw);
            sw.Close();
            return TEST_PASS;
        }
    }
}