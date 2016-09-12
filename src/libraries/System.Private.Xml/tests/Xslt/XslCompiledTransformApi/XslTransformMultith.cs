// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public class SameInstanceXslTransformTestCase : XsltApiTestCaseBase2
    {
        // Variables from init string
        protected string _strPath;					// Path of the data files

        // Other global variables
        public XslCompiledTransform xsltSameInstance;				// Used for same instance testing of XsltArgumentList

        private ITestOutputHelper _output;
        public SameInstanceXslTransformTestCase(ITestOutputHelper output) : base(output)
        {
            _output = output;
            Init(null);
        }

        public /*override*/ new void Init(object objParam)
        {
            xsltSameInstance = new XslCompiledTransform();
            _strPath = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltApiV2\");
            return;
        }
    }

    //[TestCase(Name = "Same instance testing: Transform() - Type & MethodInfo")]
    public class SameTypeCompiledTransform : XsltApiTestCaseBase2
    {
        protected string _strPath; // Path of the data files
        protected int counter;

        private Assembly _asm;
        private Type _type;
        private MethodInfo _meth;
        private Byte[] _staticData;
        private Type[] _ebTypes;

        private ITestOutputHelper _output;
        public SameTypeCompiledTransform(ITestOutputHelper output) : base(output)
        {
            _output = output;
            Init(null);
        }

        public /*override*/ new void Init(object objParam)
        {
            _strPath = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltApiV2\");
            counter = 0;
            InitTestData();
            return;
        }

        private void InitTestData()
        {
            string filePath = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\Scripting28.dll");
            _asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(filePath));
            _type = _asm.GetType("Scripting28");
            _meth = ReflectionTestCaseBase.GetStaticMethod(_type, "Execute");
            _staticData = (Byte[])_type.GetTypeInfo().GetField("staticData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(_type);
            _ebTypes = (Type[])_type.GetTypeInfo().GetField("ebTypes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(_type);
        }

        public int LoadAndTransformWithType(Object args)
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(_type);
            string filePath = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\Scripting28.xsl");
            xslt.Transform(filePath, String.Format("out{0}.txt", System.Threading.Interlocked.Increment(ref counter)));
            return 1;
        }

        public int LoadAndTransformWithMethodInfo(Object args)
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(_meth, _staticData, _ebTypes);
            string filePath = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\Scripting28.xsl");
            xslt.Transform(filePath, String.Format("out{0}.txt", System.Threading.Interlocked.Increment(ref counter)));
            return 1;
        }

        ////////////////////////////////////////////////////////////////
        // Same type testing:
        // Multiple Transform() over same XslCompiledTransform object
        ////////////////////////////////////////////////////////////////
        //[Variation("Multiple Loads on Type and Transform")]
        [ActiveIssue(9873)]
        [InlineData()]
        [Theory]
        public void proc1()
        {
            CThreads rThreads = new CThreads(_output);

            for (int i = 0; i < 1000; i++)
            {
                rThreads.Add(new ThreadFunc(LoadAndTransformWithType), i.ToString());
            }

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Loads with Common MethodInfo, ebTypes and static data and Transform")]
        [ActiveIssue(9873)]
        [InlineData()]
        [Theory]
        public void proc2()
        {
            CThreads rThreads = new CThreads(_output);

            for (int i = 0; i < 1000; i++)
            {
                rThreads.Add(new ThreadFunc(LoadAndTransformWithMethodInfo), i.ToString());
            }

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }
    }

    //[TestCase(Name = "Same instance testing: Transform() - READER")]
    public class SameInstanceXslTransformReader : SameInstanceXslTransformTestCase
    {
        private XPathDocument _xd;			// Loads XML file
        private XmlReader _xrData;           // Loads XML File
        //private XmlReader xr;				// Reader output of transform is not supported in XSLT V2

        private ITestOutputHelper _output;
        public SameInstanceXslTransformReader(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        public void Load(string _strXslFile, string _strXmlFile)
        {
            _xrData = XmlReader.Create(_strPath + _strXmlFile);
            _xd = new XPathDocument(_xrData, XmlSpace.Preserve);
            _xrData.Dispose();

            XmlReaderSettings xrs = new XmlReaderSettings();
#pragma warning disable 0618
            xrs.ProhibitDtd = false;
#pragma warning restore 0618
            XmlReader xrTemp = XmlReader.Create(_strPath + _strXslFile, xrs);
            xsltSameInstance.Load(xrTemp);
        }

        ////////////////////////////////////////////////////////////////
        // Same instance testing:
        // Multiple Transform() over same XslCompiledTransform object
        ////////////////////////////////////////////////////////////////
        public int Transform(object args)
        {
            for (int i = 1; i <= 100; i++)
            {
                StringWriter sw = new StringWriter();
                xsltSameInstance.Transform(_xrData, null, sw);
                _output.WriteLine("Transform: Thread " + args + "\tIteration " + i + "\tDone with READER transform...");
            }
            return 1;
        }

        //[Variation("Multiple Transform(): Reader - Basic Test")]
        [InlineData()]
        [Theory]
        public void proc1()
        {
            Load("xslt_multithreading_test.xsl", "foo.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): Reader - QFE 505 Repro")]
        [InlineData()]
        [Theory(Skip = "Resolving of External URIs is no longer allowed")]
        public void proc2()
        {
            Load("QFE505_multith_customer_repro_with_or_expr.xsl", "QFE505_multith_customer_repro_with_or_expr.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): Reader - AVTs")]
        [InlineData()]
        [Theory]
        public void proc3()
        {
            Load("xslt_multith_AVTs.xsl", "xslt_multith_AVTs.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): Reader - xsl:key")]
        [InlineData()]
        [Theory]
        public void proc4()
        {
            Load("xslt_multith_keytest.xsl", "xslt_multith_keytest.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): Reader - xsl:sort")]
        [InlineData()]
        [Theory]
        public void proc5()
        {
            Load("xslt_multith_sorting.xsl", "xslt_multith_sorting.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): Reader - Attribute Sets")]
        [InlineData()]
        [Theory]
        public void proc6()
        {
            Load("xslt_mutith_attribute_sets.xsl", "xslt_mutith_attribute_sets.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): Reader - Boolean Expression AND")]
        [InlineData()]
        [Theory]
        public void proc7()
        {
            Load("xslt_mutith_boolean_expr_and.xsl", "xslt_mutith_boolean_expr_and.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): Reader - Boolean Expression OR")]
        [InlineData()]
        [Theory]
        public void proc8()
        {
            Load("xslt_mutith_boolean_expr_or.xsl", "xslt_mutith_boolean_expr_or.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): Reader - FormatNubmer function")]
        [InlineData()]
        [Theory]
        public void proc9()
        {
            Load("xslt_mutith_format_number.xsl", "xslt_mutith_format_number.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): Reader - Position() function")]
        [InlineData()]
        [Theory]
        public void proc10()
        {
            Load("xslt_mutith_position_func.xsl", "xslt_mutith_position_func.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): Reader - preserve space")]
        [InlineData()]
        [Theory]
        public void proc11()
        {
            Load("xslt_mutith_preserve_space.xsl", "xslt_mutith_preserve_space.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): Reader - Variable nodeset")]
        [InlineData()]
        [Theory]
        public void proc12()
        {
            Load("xslt_mutith_variable_nodeset.xsl", "xslt_mutith_variable_nodeset.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }
    }

    //[TestCase(Name = "Same instance testing: Transform() - TEXTWRITER")]
    public class SameInstanceXslTransformWriter : SameInstanceXslTransformTestCase
    {
        private XPathDocument _xd;		// Loads XML file
        private XmlReader _xrData;	    // Loads XML file

        private ITestOutputHelper _output;
        public SameInstanceXslTransformWriter(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        ////////////////////////////////////////////////////////////////
        // Same instance testing:
        // Multiple Transform() over same XslCompiledTransform object
        ////////////////////////////////////////////////////////////////
        public int Transform(object args)
        {
            for (int i = 1; i <= 100; i++)
            {
                using (XmlTextWriter tw = new XmlTextWriter(System.IO.TextWriter.Null))
                {
                    xsltSameInstance.Transform(_xrData, null, tw);
                }
            }

            //_output.WriteLine("Transform: Thread " + args + "\tDone with WRITER transform...");
            return 1;
        }

        public void Load(string _strXslFile, string _strXmlFile)
        {
            _xrData = XmlReader.Create(_strPath + _strXmlFile);
            _xd = new XPathDocument(_xrData, XmlSpace.Preserve);
            _xrData.Dispose();

            XmlReaderSettings xrs = new XmlReaderSettings();
#pragma warning disable 0618
            xrs.ProhibitDtd = false;
#pragma warning restore 0618
            XmlReader xrTemp = XmlReader.Create(_strPath + _strXslFile, xrs);
            xsltSameInstance.Load(xrTemp);
        }

        //[Variation("Multiple Transform(): TextWriter - Basic Test")]
        [InlineData()]
        [Theory]
        public void proc1()
        {
            Load("xslt_multithreading_test.xsl", "foo.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): TextWriter - QFE 505 Repro")]
        [InlineData()]
        [Theory(Skip = "Resolving of External URIs is no longer allowed")]
        public void proc2()
        {
            Load("QFE505_multith_customer_repro_with_or_expr.xsl", "QFE505_multith_customer_repro_with_or_expr.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): TextWriter - AVTs")]
        [InlineData()]
        [Theory]
        public void proc3()
        {
            Load("xslt_multith_AVTs.xsl", "xslt_multith_AVTs.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): TextWriter - xsl:key")]
        [InlineData()]
        [Theory]
        public void proc4()
        {
            Load("xslt_multith_keytest.xsl", "xslt_multith_keytest.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): TextWriter - xsl:sort")]
        [InlineData()]
        [Theory]
        public void proc5()
        {
            Load("xslt_multith_sorting.xsl", "xslt_multith_sorting.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): TextWriter - Attribute Sets")]
        [InlineData()]
        [Theory]
        public void proc6()
        {
            Load("xslt_mutith_attribute_sets.xsl", "xslt_mutith_attribute_sets.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): TextWriter - Boolean Expression AND")]
        [InlineData()]
        [Theory]
        public void proc7()
        {
            Load("xslt_mutith_boolean_expr_and.xsl", "xslt_mutith_boolean_expr_and.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): TextWriter - Boolean Expression OR")]
        [InlineData()]
        [Theory]
        public void proc8()
        {
            Load("xslt_mutith_boolean_expr_or.xsl", "xslt_mutith_boolean_expr_or.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): TextWriter - FormatNubmer function")]
        [InlineData()]
        [Theory]
        public void proc9()
        {
            Load("xslt_mutith_format_number.xsl", "xslt_mutith_format_number.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): TextWriter - Position() function")]
        [InlineData()]
        [Theory]
        public void proc10()
        {
            Load("xslt_mutith_position_func.xsl", "xslt_mutith_position_func.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): TextWriter - preserve space")]
        [InlineData()]
        [Theory]
        public void proc11()
        {
            Load("xslt_mutith_preserve_space.xsl", "xslt_mutith_preserve_space.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }

        //[Variation("Multiple Transform(): TextWriter - Variable nodeset")]
        [InlineData()]
        [Theory]
        public void proc12()
        {
            Load("xslt_mutith_variable_nodeset.xsl", "xslt_mutith_variable_nodeset.xml");

            CThreads rThreads = new CThreads(_output);
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }
    }
}