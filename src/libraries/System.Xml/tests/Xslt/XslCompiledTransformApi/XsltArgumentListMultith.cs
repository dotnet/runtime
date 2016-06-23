using OLEDB.Test.ModuleCore;
using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;

namespace XsltApiV2
{
    public class CSameInstanceXsltArgTestCase : XsltApiTestCaseBase
    {
        // Variables from init string
        protected string _strPath;				// Path of the data files

        // Other global variables
        public XsltArgumentList xsltArg1;					// Shared XsltArgumentList for same instance testing

        public override int Init(object objParam)
        {
            // Get parameter info
            _strPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"XsltApiV2\");
            xsltArg1 = new XsltArgumentList();

            MyObject obj1 = new MyObject(1);
            MyObject obj2 = new MyObject(2);
            MyObject obj3 = new MyObject(3);
            MyObject obj4 = new MyObject(4);
            MyObject obj5 = new MyObject(5);

            xsltArg1.AddExtensionObject("urn:my-obj1", obj1);
            xsltArg1.AddExtensionObject("urn:my-obj2", obj2);
            xsltArg1.AddExtensionObject("urn:my-obj3", obj3);
            xsltArg1.AddExtensionObject("urn:my-obj4", obj4);
            xsltArg1.AddExtensionObject("urn:my-obj5", obj5);

            xsltArg1.AddParam("myArg1", szEmpty, "Test1");
            xsltArg1.AddParam("myArg2", szEmpty, "Test2");
            xsltArg1.AddParam("myArg3", szEmpty, "Test3");
            xsltArg1.AddParam("myArg4", szEmpty, "Test4");
            xsltArg1.AddParam("myArg5", szEmpty, "Test5");

            return TEST_PASS;
        }
    }

    [TestCase(Name = "Same instance testing: XsltArgList - GetParam", Desc = "GetParam test cases")]
    public class CSameInstanceXsltArgumentListGetParam : CSameInstanceXsltArgTestCase
    {
        ////////////////////////////////////////////////////////////////
        // Same instance testing:
        // Multiple GetParam() over same ArgumentList
        ////////////////////////////////////////////////////////////////
        public int GetParam1(object args)
        {
            Object retObj;

            for (int i = 1; i <= 100; i++)
            {
                retObj = xsltArg1.GetParam(((object[])args)[1].ToString(), szEmpty);
                CError.WriteLine("GetParam: Thread " + ((object[])args)[0] + "\tIteration " + i + "\tAdded Value: {0}\tRetrieved Value:{1}\n", "Test1", retObj.ToString());
                if (retObj.ToString() != "Test1")
                {
                    CError.WriteLine("ERROR!!!");
                    return TEST_FAIL;
                }
            }
            return TEST_PASS;
        }

        public int GetParam2(object args)
        {
            Object retObj;

            for (int i = 1; i <= 100; i++)
            {
                retObj = xsltArg1.GetParam(((object[])args)[1].ToString(), szEmpty);
                string expected = "Test" + ((object[])args)[0];
                CError.WriteLine("GetParam: Thread " + ((object[])args)[0] + "\tIteration " + i + "\tAdded Value: {0}\tRetrieved Value:{1}\n", expected, retObj.ToString());
                if (retObj.ToString() != expected)
                {
                    CError.WriteLine("ERROR!!!");
                    return TEST_FAIL;
                }
            }
            return TEST_PASS;
        }

        [Variation("Multiple GetParam for same parameter name")]
        public int proc1()
        {
            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(GetParam1), new Object[] { 1, "myArg1" });
            rThreads.Add(new ThreadFunc(GetParam1), new Object[] { 2, "myArg1" });
            rThreads.Add(new ThreadFunc(GetParam1), new Object[] { 3, "myArg1" });
            rThreads.Add(new ThreadFunc(GetParam1), new Object[] { 4, "myArg1" });
            rThreads.Add(new ThreadFunc(GetParam1), new Object[] { 5, "myArg1" });

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple GetParam for different parameter name")]
        public int proc2()
        {
            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(GetParam2), new Object[] { 1, "myArg1" });
            rThreads.Add(new ThreadFunc(GetParam2), new Object[] { 2, "myArg2" });
            rThreads.Add(new ThreadFunc(GetParam2), new Object[] { 3, "myArg3" });
            rThreads.Add(new ThreadFunc(GetParam2), new Object[] { 4, "myArg4" });
            rThreads.Add(new ThreadFunc(GetParam2), new Object[] { 5, "myArg5" });

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }
    }

    [TestCase(Name = "Same instance testing: XsltArgList - GetExtensionObject", Desc = "GetExtensionObject test cases")]
    public class CSameInstanceXsltArgumentListGetExtnObject : CSameInstanceXsltArgTestCase
    {
        ////////////////////////////////////////////////////////////////
        // Same instance testing:
        // Multiple GetExtensionObject() over same ArgumentList
        ////////////////////////////////////////////////////////////////
        public int GetExtnObject1(object args)
        {
            Object retObj;

            for (int i = 1; i <= 100; i++)
            {
                retObj = xsltArg1.GetExtensionObject(((object[])args)[1].ToString());
                CError.WriteLine("GetExtensionObject: Thread " + ((object[])args)[0] + "\tIteration " + i + "\tValue returned: " + ((MyObject)retObj).MyValue());
                if (((MyObject)retObj).MyValue() != 1)
                {
                    CError.WriteLine("ERROR!!! Set and retrieved value appear to be different");
                    return TEST_FAIL;
                }
            }
            return TEST_PASS;
        }

        public int GetExtnObject2(object args)
        {
            Object retObj;

            for (int i = 1; i <= 100; i++)
            {
                retObj = xsltArg1.GetExtensionObject(((object[])args)[1].ToString());
                CError.WriteLine("GetExtensionObject: Thread " + ((object[])args)[0] + "\tIteration " + i + "\tValue returned: " + ((MyObject)retObj).MyValue());
                if (((MyObject)retObj).MyValue() != (int)((object[])args)[0])
                {
                    CError.WriteLine("ERROR!!! Set and retrieved value appear to be different");
                    return TEST_FAIL;
                }
            }
            return TEST_PASS;
        }

        [Variation("Multiple GetExtensionObject for same namespace URI")]
        public int proc1()
        {
            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(GetExtnObject1), new Object[] { 1, "urn:my-obj1" });
            rThreads.Add(new ThreadFunc(GetExtnObject1), new Object[] { 2, "urn:my-obj1" });
            rThreads.Add(new ThreadFunc(GetExtnObject1), new Object[] { 3, "urn:my-obj1" });
            rThreads.Add(new ThreadFunc(GetExtnObject1), new Object[] { 4, "urn:my-obj1" });
            rThreads.Add(new ThreadFunc(GetExtnObject1), new Object[] { 5, "urn:my-obj1" });

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple GetExtensionObject for different namespace URI")]
        public int proc2()
        {
            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(GetExtnObject2), new Object[] { 1, "urn:my-obj1" });
            rThreads.Add(new ThreadFunc(GetExtnObject2), new Object[] { 2, "urn:my-obj2" });
            rThreads.Add(new ThreadFunc(GetExtnObject2), new Object[] { 3, "urn:my-obj3" });
            rThreads.Add(new ThreadFunc(GetExtnObject2), new Object[] { 4, "urn:my-obj4" });
            rThreads.Add(new ThreadFunc(GetExtnObject2), new Object[] { 5, "urn:my-obj5" });

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }
    }

    [TestCase(Name = "Same instance testing: XsltArgList - Transform", Desc = "Multiple transforms")]
    public class CSameInstanceXsltArgumentListTransform : CSameInstanceXsltArgTestCase
    {
        ////////////////////////////////////////////////////////////////
        // Same instance testing:
        // Multiple Transform() using shared ArgumentList
        ////////////////////////////////////////////////////////////////
        public int SharedArgList(object args)
        {
            string _strXslFile = ((object[])args)[1].ToString();
            string _strXmlFile = ((object[])args)[2].ToString();

            if (_strXslFile.Substring(0, 5) != "http:")
                _strXslFile = _strPath + _strXslFile;
            if (_strXmlFile.Substring(0, 5) != "http:")
                _strXmlFile = _strPath + _strXmlFile;

            XmlReader xrData = XmlReader.Create(_strXmlFile);
            XPathDocument xd = new XPathDocument(xrData, XmlSpace.Preserve);
            xrData.Close();

            XslCompiledTransform xslt = new XslCompiledTransform();
            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ProhibitDtd = false;
            XmlReader xrTemp = XmlReader.Create(_strXslFile);
            xslt.Load(xrTemp);

            StringWriter sw = new StringWriter();
            for (int i = 1; i <= 100; i++)
            {
                xslt.Transform(xd, xsltArg1, sw);
                CError.WriteLine("SharedArgumentList: Thread " + ((object[])args)[0] + "\tIteration " + i + "\tDone with transform...");
            }
            return TEST_PASS;
        }

        ////////////////////////////////////////////////////////////////
        // Same instance testing:
        // Multiple Transform() using shared ArgumentList
        ////////////////////////////////////////////////////////////////
        [Variation("Multiple transforms using shared ArgumentList")]
        public int proc1()
        {
            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(SharedArgList), new object[] { 1, "xsltarg_multithreading1.xsl", "foo.xml" });
            rThreads.Add(new ThreadFunc(SharedArgList), new object[] { 2, "xsltarg_multithreading2.xsl", "foo.xml" });
            rThreads.Add(new ThreadFunc(SharedArgList), new object[] { 3, "xsltarg_multithreading3.xsl", "foo.xml" });
            rThreads.Add(new ThreadFunc(SharedArgList), new object[] { 4, "xsltarg_multithreading4.xsl", "foo.xml" });
            rThreads.Add(new ThreadFunc(SharedArgList), new object[] { 5, "xsltarg_multithreading5.xsl", "foo.xml" });

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }
    }
}