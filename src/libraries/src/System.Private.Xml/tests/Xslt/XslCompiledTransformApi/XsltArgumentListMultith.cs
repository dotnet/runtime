// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public class CSameInstanceXsltArgTestCase2 : XsltApiTestCaseBase2
    {
        // Variables from init string
        protected string _strPath;				// Path of the data files

        // Other global variables
        public XsltArgumentList xsltArg1;					// Shared XsltArgumentList for same instance testing

        private ITestOutputHelper _output;
        public CSameInstanceXsltArgTestCase2(ITestOutputHelper output) : base(output)
        {
            _output = output;
            Init(null);
        }

        public new void Init(object objParam)
        {
            // Get parameter info
            _strPath = Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "XsltApiV2");
            xsltArg1 = new XsltArgumentList();

            MyObject obj1 = new MyObject(1, _output);
            MyObject obj2 = new MyObject(2, _output);
            MyObject obj3 = new MyObject(3, _output);
            MyObject obj4 = new MyObject(4, _output);
            MyObject obj5 = new MyObject(5, _output);

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

            return;
        }
    }

    //[TestCase(Name = "Same instance testing: XsltArgList - GetParam", Desc = "GetParam test cases")]
    public class CSameInstanceXsltArgumentListGetParam : CSameInstanceXsltArgTestCase2
    {
        private ITestOutputHelper _output;
        public CSameInstanceXsltArgumentListGetParam(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        ////////////////////////////////////////////////////////////////
        // Same instance testing:
        // Multiple GetParam() over same ArgumentList
        ////////////////////////////////////////////////////////////////
        public int GetParam1(object args)
        {
            object retObj;

            for (int i = 1; i <= 100; i++)
            {
                retObj = xsltArg1.GetParam(((object[])args)[1].ToString(), szEmpty);
                _output.WriteLine("GetParam: Thread " + ((object[])args)[0] + "\tIteration " + i + "\tAdded Value: {0}\tRetrieved Value:{1}\n", "Test1", retObj.ToString());
                if (retObj.ToString() != "Test1")
                {
                    _output.WriteLine("ERROR!!!");
                    return 0;
                }
            }
            return 1;
        }

        public int GetParam2(object args)
        {
            object retObj;

            for (int i = 1; i <= 100; i++)
            {
                retObj = xsltArg1.GetParam(((object[])args)[1].ToString(), szEmpty);
                string expected = "Test" + ((object[])args)[0];
                _output.WriteLine("GetParam: Thread " + ((object[])args)[0] + "\tIteration " + i + "\tAdded Value: {0}\tRetrieved Value:{1}\n", expected, retObj.ToString());
                if (retObj.ToString() != expected)
                {
                    _output.WriteLine("ERROR!!!");
                    return 0;
                }
            }
            return 1;
        }

        //[Variation("Multiple GetParam for same parameter name")]
        [InlineData()]
        [Theory]
        public void proc1()
        {
            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(GetParam1), new object[] { 1, "myArg1" });
            rThreads.Add(new ThreadFunc(GetParam1), new object[] { 2, "myArg1" });
            rThreads.Add(new ThreadFunc(GetParam1), new object[] { 3, "myArg1" });
            rThreads.Add(new ThreadFunc(GetParam1), new object[] { 4, "myArg1" });
            rThreads.Add(new ThreadFunc(GetParam1), new object[] { 5, "myArg1" });

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple GetParam for different parameter name")]
        [InlineData()]
        [Theory]
        public void proc2()
        {
            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(GetParam2), new object[] { 1, "myArg1" });
            rThreads.Add(new ThreadFunc(GetParam2), new object[] { 2, "myArg2" });
            rThreads.Add(new ThreadFunc(GetParam2), new object[] { 3, "myArg3" });
            rThreads.Add(new ThreadFunc(GetParam2), new object[] { 4, "myArg4" });
            rThreads.Add(new ThreadFunc(GetParam2), new object[] { 5, "myArg5" });

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }
    }

    //[TestCase(Name = "Same instance testing: XsltArgList - GetExtensionObject", Desc = "GetExtensionObject test cases")]
    public class CSameInstanceXsltArgumentListGetExtnObject : CSameInstanceXsltArgTestCase2
    {
        private ITestOutputHelper _output;
        public CSameInstanceXsltArgumentListGetExtnObject(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        ////////////////////////////////////////////////////////////////
        // Same instance testing:
        // Multiple GetExtensionObject() over same ArgumentList
        ////////////////////////////////////////////////////////////////
        public int GetExtnObject1(object args)
        {
            object retObj;

            for (int i = 1; i <= 100; i++)
            {
                retObj = xsltArg1.GetExtensionObject(((object[])args)[1].ToString());
                _output.WriteLine("GetExtensionObject: Thread " + ((object[])args)[0] + "\tIteration " + i + "\tValue returned: " + ((MyObject)retObj).MyValue());
                if (((MyObject)retObj).MyValue() != 1)
                {
                    _output.WriteLine("ERROR!!! Set and retrieved value appear to be different");
                    return 0;
                }
            }
            return 1;
        }

        public int GetExtnObject2(object args)
        {
            object retObj;

            for (int i = 1; i <= 100; i++)
            {
                retObj = xsltArg1.GetExtensionObject(((object[])args)[1].ToString());
                _output.WriteLine("GetExtensionObject: Thread " + ((object[])args)[0] + "\tIteration " + i + "\tValue returned: " + ((MyObject)retObj).MyValue());
                if (((MyObject)retObj).MyValue() != (int)((object[])args)[0])
                {
                    _output.WriteLine("ERROR!!! Set and retrieved value appear to be different");
                    return 0;
                }
            }
            return 1;
        }

        //[Variation("Multiple GetExtensionObject for same namespace System.Xml.Tests")]
        [InlineData()]
        [Theory]
        public void proc1()
        {
            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(GetExtnObject1), new object[] { 1, "urn:my-obj1" });
            rThreads.Add(new ThreadFunc(GetExtnObject1), new object[] { 2, "urn:my-obj1" });
            rThreads.Add(new ThreadFunc(GetExtnObject1), new object[] { 3, "urn:my-obj1" });
            rThreads.Add(new ThreadFunc(GetExtnObject1), new object[] { 4, "urn:my-obj1" });
            rThreads.Add(new ThreadFunc(GetExtnObject1), new object[] { 5, "urn:my-obj1" });

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple GetExtensionObject for different namespace System.Xml.Tests")]
        [InlineData()]
        [Theory]
        public void proc2()
        {
            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(GetExtnObject2), new object[] { 1, "urn:my-obj1" });
            rThreads.Add(new ThreadFunc(GetExtnObject2), new object[] { 2, "urn:my-obj2" });
            rThreads.Add(new ThreadFunc(GetExtnObject2), new object[] { 3, "urn:my-obj3" });
            rThreads.Add(new ThreadFunc(GetExtnObject2), new object[] { 4, "urn:my-obj4" });
            rThreads.Add(new ThreadFunc(GetExtnObject2), new object[] { 5, "urn:my-obj5" });

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }
    }

    //[TestCase(Name = "Same instance testing: XsltArgList - Transform", Desc = "Multiple transforms")]
    public class CSameInstanceXsltArgumentListTransform : CSameInstanceXsltArgTestCase2
    {
        private ITestOutputHelper _output;
        public CSameInstanceXsltArgumentListTransform(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        ////////////////////////////////////////////////////////////////
        // Same instance testing:
        // Multiple Transform() using shared ArgumentList
        ////////////////////////////////////////////////////////////////
        public int SharedArgList(object args)
        {
            string _strXslFile = ((object[])args)[1].ToString();
            string _strXmlFile = ((object[])args)[2].ToString();

            if (_strXslFile.Substring(0, 5) != "http:")
                _strXslFile = Path.Combine(_strPath, _strXslFile);
            if (_strXmlFile.Substring(0, 5) != "http:")
                _strXmlFile = Path.Combine(_strPath, _strXmlFile);

            XmlReader xrData = XmlReader.Create(_strXmlFile);
            XPathDocument xd = new XPathDocument(xrData, XmlSpace.Preserve);
            xrData.Dispose();

            XslCompiledTransform xslt = new XslCompiledTransform();
            XmlReaderSettings xrs = new XmlReaderSettings();
#pragma warning disable 0618
            xrs.ProhibitDtd = false;
#pragma warning restore 0618
            XmlReader xrTemp = XmlReader.Create(_strXslFile);
            xslt.Load(xrTemp);

            StringWriter sw = new StringWriter();
            for (int i = 1; i <= 100; i++)
            {
                xslt.Transform(xd, xsltArg1, sw);
                _output.WriteLine("SharedArgumentList: Thread " + ((object[])args)[0] + "\tIteration " + i + "\tDone with transform...");
            }
            return 1;
        }

        ////////////////////////////////////////////////////////////////
        // Same instance testing:
        // Multiple Transform() using shared ArgumentList
        ////////////////////////////////////////////////////////////////
        //[Variation("Multiple transforms using shared ArgumentList")]
        [InlineData()]
        [Theory]
        public void proc1()
        {
            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(SharedArgList), new object[] { 1, "xsltarg_multithreading1.xsl", "foo.xml" });
            rThreads.Add(new ThreadFunc(SharedArgList), new object[] { 2, "xsltarg_multithreading2.xsl", "foo.xml" });
            rThreads.Add(new ThreadFunc(SharedArgList), new object[] { 3, "xsltarg_multithreading3.xsl", "foo.xml" });
            rThreads.Add(new ThreadFunc(SharedArgList), new object[] { 4, "xsltarg_multithreading4.xsl", "foo.xml" });
            rThreads.Add(new ThreadFunc(SharedArgList), new object[] { 5, "xsltarg_multithreading5.xsl", "foo.xml" });

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }
    }
}