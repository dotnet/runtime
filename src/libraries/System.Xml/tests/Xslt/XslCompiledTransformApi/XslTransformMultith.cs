using OLEDB.Test.ModuleCore;
using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;

namespace XsltApiV2
{
    public class SameInstanceXslTransformTestCase : XsltApiTestCaseBase
    {
        // Variables from init string
        protected string _strPath;					// Path of the data files

        // Other global variables
        public XslCompiledTransform xsltSameInstance;				// Used for same instance testing of XsltArgumentList

        public override int Init(object objParam)
        {
            xsltSameInstance = new XslCompiledTransform();
            _strPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"XsltApiV2\");
            return TEST_PASS;
        }
    }

    [TestCase(Name = "Same instance testing: Transform() - Type & MethodInfo")]
    public class SameTypeCompiledTransform : XsltApiTestCaseBase
    {
        protected string _strPath; // Path of the data files
        protected int counter;

        private Assembly asm;
        private Type type;
        private MethodInfo meth;
        private Byte[] staticData;
        private Type[] ebTypes;

        public override int Init(object objParam)
        {
            _strPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"XsltApiV2\");
            counter = 0;
            InitTestData();
            return TEST_PASS;
        }

        private void InitTestData()
        {
            string filePath = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\Scripting28.dll");
            asm = Assembly.LoadFrom(filePath);
            type = asm.GetType("Scripting28");
            meth = ReflectionTestCaseBase.GetStaticMethod(type, "Execute");
            staticData = (Byte[])type.GetField("staticData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(type);
            ebTypes = (Type[])type.GetField("ebTypes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(type);
        }

        public int LoadAndTransformWithType(Object args)
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(type);
            string filePath = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\Scripting28.xsl");
            xslt.Transform(filePath, String.Format("out{0}.txt", System.Threading.Interlocked.Increment(ref counter)));
            return TEST_PASS;
        }

        public int LoadAndTransformWithMethodInfo(Object args)
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(meth, staticData, ebTypes);
            string filePath = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\Scripting28.xsl");
            xslt.Transform(filePath, String.Format("out{0}.txt", System.Threading.Interlocked.Increment(ref counter)));
            return TEST_PASS;
        }

        ////////////////////////////////////////////////////////////////
        // Same type testing:
        // Multiple Transform() over same XslCompiledTransform object
        ////////////////////////////////////////////////////////////////
        [Variation("Multiple Loads on Type and Transform")]
        public int proc1()
        {
            CThreads rThreads = new CThreads();

            for (int i = 0; i < 1000; i++)
            {
                rThreads.Add(new ThreadFunc(LoadAndTransformWithType), i.ToString());
            }

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Loads with Common MethodInfo, ebTypes and static data and Transform")]
        public int proc2()
        {
            CThreads rThreads = new CThreads();

            for (int i = 0; i < 1000; i++)
            {
                rThreads.Add(new ThreadFunc(LoadAndTransformWithMethodInfo), i.ToString());
            }

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }
    }

    [TestCase(Name = "Same instance testing: Transform() - READER")]
    public class SameInstanceXslTransformReader : SameInstanceXslTransformTestCase
    {
        private XPathDocument xd;			// Loads XML file
        private XmlReader xrData;           // Loads XML File
        //private XmlReader xr;				// Reader output of transform is not supported in XSLT V2

        public void Load(string _strXslFile, string _strXmlFile)
        {
            xrData = XmlReader.Create(_strPath + _strXmlFile);
            xd = new XPathDocument(xrData, XmlSpace.Preserve);
            xrData.Close();

            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ProhibitDtd = false;
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
                xsltSameInstance.Transform(xrData, null, sw);
                CError.WriteLine("Transform: Thread " + args + "\tIteration " + i + "\tDone with READER transform...");
            }
            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - Basic Test")]
        public int proc1()
        {
            Load("xslt_multithreading_test.xsl", "foo.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - QFE 505 Repro")]
        public int proc2()
        {
            Load("QFE505_multith_customer_repro_with_or_expr.xsl", "QFE505_multith_customer_repro_with_or_expr.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - AVTs")]
        public int proc3()
        {
            Load("xslt_multith_AVTs.xsl", "xslt_multith_AVTs.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - xsl:key")]
        public int proc4()
        {
            Load("xslt_multith_keytest.xsl", "xslt_multith_keytest.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - xsl:sort")]
        public int proc5()
        {
            Load("xslt_multith_sorting.xsl", "xslt_multith_sorting.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - Attribute Sets")]
        public int proc6()
        {
            Load("xslt_mutith_attribute_sets.xsl", "xslt_mutith_attribute_sets.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - Boolean Expression AND")]
        public int proc7()
        {
            Load("xslt_mutith_boolean_expr_and.xsl", "xslt_mutith_boolean_expr_and.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - Boolean Expression OR")]
        public int proc8()
        {
            Load("xslt_mutith_boolean_expr_or.xsl", "xslt_mutith_boolean_expr_or.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - FormatNubmer function")]
        public int proc9()
        {
            Load("xslt_mutith_format_number.xsl", "xslt_mutith_format_number.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - Position() function")]
        public int proc10()
        {
            Load("xslt_mutith_position_func.xsl", "xslt_mutith_position_func.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - preserve space")]
        public int proc11()
        {
            Load("xslt_mutith_preserve_space.xsl", "xslt_mutith_preserve_space.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): Reader - Variable nodeset")]
        public int proc12()
        {
            Load("xslt_mutith_variable_nodeset.xsl", "xslt_mutith_variable_nodeset.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }
    }

    [TestCase(Name = "Same instance testing: Transform() - TEXTWRITER")]
    public class SameInstanceXslTransformWriter : SameInstanceXslTransformTestCase
    {
        private XPathDocument xd;		// Loads XML file
        private XmlReader xrData;	    // Loads XML file

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
                    xsltSameInstance.Transform(xrData, null, tw);
                }
            }

            //CError.WriteLine("Transform: Thread " + args + "\tDone with WRITER transform...");
            return TEST_PASS;
        }

        public void Load(string _strXslFile, string _strXmlFile)
        {
            xrData = XmlReader.Create(_strPath + _strXmlFile);
            xd = new XPathDocument(xrData, XmlSpace.Preserve);
            xrData.Close();

            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ProhibitDtd = false;
            XmlReader xrTemp = XmlReader.Create(_strPath + _strXslFile, xrs);
            xsltSameInstance.Load(xrTemp);
        }

        [Variation("Multiple Transform(): TextWriter - Basic Test")]
        public int proc1()
        {
            Load("xslt_multithreading_test.xsl", "foo.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): TextWriter - QFE 505 Repro")]
        public int proc2()
        {
            Load("QFE505_multith_customer_repro_with_or_expr.xsl", "QFE505_multith_customer_repro_with_or_expr.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): TextWriter - AVTs")]
        public int proc3()
        {
            Load("xslt_multith_AVTs.xsl", "xslt_multith_AVTs.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): TextWriter - xsl:key")]
        public int proc4()
        {
            Load("xslt_multith_keytest.xsl", "xslt_multith_keytest.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): TextWriter - xsl:sort")]
        public int proc5()
        {
            Load("xslt_multith_sorting.xsl", "xslt_multith_sorting.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): TextWriter - Attribute Sets")]
        public int proc6()
        {
            Load("xslt_mutith_attribute_sets.xsl", "xslt_mutith_attribute_sets.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): TextWriter - Boolean Expression AND")]
        public int proc7()
        {
            Load("xslt_mutith_boolean_expr_and.xsl", "xslt_mutith_boolean_expr_and.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): TextWriter - Boolean Expression OR")]
        public int proc8()
        {
            Load("xslt_mutith_boolean_expr_or.xsl", "xslt_mutith_boolean_expr_or.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): TextWriter - FormatNubmer function")]
        public int proc9()
        {
            Load("xslt_mutith_format_number.xsl", "xslt_mutith_format_number.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): TextWriter - Position() function")]
        public int proc10()
        {
            Load("xslt_mutith_position_func.xsl", "xslt_mutith_position_func.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): TextWriter - preserve space")]
        public int proc11()
        {
            Load("xslt_mutith_preserve_space.xsl", "xslt_mutith_preserve_space.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }

        [Variation("Multiple Transform(): TextWriter - Variable nodeset")]
        public int proc12()
        {
            Load("xslt_mutith_variable_nodeset.xsl", "xslt_mutith_variable_nodeset.xml");

            CThreads rThreads = new CThreads();
            rThreads.Add(new ThreadFunc(Transform), "1");
            rThreads.Add(new ThreadFunc(Transform), "2");
            rThreads.Add(new ThreadFunc(Transform), "3");
            rThreads.Add(new ThreadFunc(Transform), "4");
            rThreads.Add(new ThreadFunc(Transform), "5");

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return TEST_PASS;
        }
    }
}