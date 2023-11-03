// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml.Tests;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.XslTransformApiTests
{
    public class CSameInstanceXslTransformTestCase : XsltApiTestCaseBase
    {
        // Variables from init string
        protected string _strPath;                    // Path of the data files

        // Other global variables
#pragma warning disable 0618
        public XslTransform xsltSameInstance;         // Used for same instance testing of XsltArgumentList
#pragma warning restore 0618

        protected int threadCount = 5;

        private ITestOutputHelper _output;
        public CSameInstanceXslTransformTestCase(ITestOutputHelper output) : base(output)
        {
            _output = output;
            Init(null);
        }

        private new void Init(object objParam)
        {
#pragma warning disable 0618
            xsltSameInstance = new XslTransform();
#pragma warning restore 0618
            _strPath = Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "XsltApi");
            return;
        }

        protected virtual void Load(string _strXslFile, string _strXmlFile)
        {
        }

        public virtual int Transform(object args)
        {
            return 0;
        }

        //[Variation("Basic Test", Params = new object[] { "xslt_multithreading_test.xsl", "foo.xml" })]
        [InlineData("xslt_multithreading_test.xsl", "foo.xml")]
        //[Variation("AVTs", Params = new object[] { "xslt_multith_AVTs.xsl", "xslt_multith_AVTs.xml" })]
        [InlineData("xslt_multith_AVTs.xsl", "xslt_multith_AVTs.xml")]
        //[Variation("xsl:key", Params = new object[] { "xslt_multith_keytest.xsl", "xslt_multith_keytest.xml" })]
        [InlineData("xslt_multith_keytest.xsl", "xslt_multith_keytest.xml")]
        //[Variation("xsl:sort", Params = new object[] { "xslt_multith_sorting.xsl", "xslt_multith_sorting.xml" })]
        [InlineData("xslt_multith_sorting.xsl", "xslt_multith_sorting.xml")]
        //[Variation("Attribute Sets", Params = new object[] { "xslt_mutith_attribute_sets.xsl", "xslt_mutith_attribute_sets.xml" })]
        [InlineData("xslt_mutith_attribute_sets.xsl", "xslt_mutith_attribute_sets.xml")]
        //[Variation("Boolean Expression AND", Params = new object[] { "xslt_mutith_boolean_expr_and.xsl", "xslt_mutith_boolean_expr_and.xml" })]
        [InlineData("xslt_mutith_boolean_expr_and.xsl", "xslt_mutith_boolean_expr_and.xml")]
        //[Variation("Boolean Expression OR", Params = new object[] { "xslt_mutith_boolean_expr_or.xsl", "xslt_mutith_boolean_expr_or.xml" })]
        [InlineData("xslt_mutith_boolean_expr_or.xsl", "xslt_mutith_boolean_expr_or.xml")]
        //[Variation("FormatNumber function", Params = new object[] { "xslt_mutith_format_number.xsl", "xslt_mutith_format_number.xml" })]
        [InlineData("xslt_mutith_format_number.xsl", "xslt_mutith_format_number.xml")]
        //[Variation("Position() function", Params = new object[] { "xslt_mutith_position_func.xsl", "xslt_mutith_position_func.xml" })]
        [InlineData("xslt_mutith_position_func.xsl", "xslt_mutith_position_func.xml")]
        //[Variation("preserve space", Params = new object[] { "xslt_mutith_preserve_space.xsl", "xslt_mutith_preserve_space.xml" })]
        [InlineData("xslt_mutith_preserve_space.xsl", "xslt_mutith_preserve_space.xml")]
        //[Variation("Variable nodeset", Params = new object[] { "xslt_mutith_variable_nodeset.xsl", "xslt_mutith_variable_nodeset.xml" })]
        [InlineData("xslt_mutith_variable_nodeset.xsl", "xslt_mutith_variable_nodeset.xml")]
        //[Variation("Forward global variable reference", Params = new object[] { "xslt_mutith_variable_global_forward_ref.xsl", "xslt_mutith_variable_nodeset.xml" })]
        [InlineData("xslt_mutith_variable_global_forward_ref.xsl", "xslt_mutith_variable_nodeset.xml")]
        //[Variation("Forward global variable reference deep", Params = new object[] { "xslt_mutith_variable_global_forward_ref_deep.xsl", "xslt_mutith_variable_nodeset.xml" })]
        [InlineData("xslt_mutith_variable_global_forward_ref_deep.xsl", "xslt_mutith_variable_nodeset.xml")]
        //[Variation("Local and global variables", Params = new object[] { "xslt_mutith_variable_local_and_global.xsl", "xslt_mutith_variable_local_and_global.xsl" })]
        [InlineData("xslt_mutith_variable_local_and_global.xsl", "xslt_mutith_variable_local_and_global.xsl")]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Variations(object param0, object param1)
        {
            string xslFile = (string)param0;
            string xmlFile = (string)param1;

            Load(xslFile, xmlFile);

            CThreads rThreads = new CThreads(_output);

            for (int i = 0; i < threadCount; i++)
                rThreads.Add(new ThreadFunc(Transform), i + 1);

            //Wait until they are complete
            rThreads.Start();
            rThreads.Wait();

            return;
        }
    }

    //[TestCase(Name = "Same instance testing: Transform() - READER")]
    public class SameInstanceXslTransformReader : CSameInstanceXslTransformTestCase
    {
        private XPathDocument _xd;            // Loads XML file

        private ITestOutputHelper _output;
        public SameInstanceXslTransformReader(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        protected override void Load(string _strXslFile, string _strXmlFile)
        {
#pragma warning disable 0618
            XmlValidatingReader xrData = new XmlValidatingReader(new XmlTextReader(Path.Combine(_strPath, _strXmlFile)));
#pragma warning restore 0618
            _xd = new XPathDocument(xrData, XmlSpace.Preserve);
            xrData.Dispose();

#pragma warning disable 0618
            XmlValidatingReader xrTemp = new XmlValidatingReader(new XmlTextReader(Path.Combine(_strPath, _strXslFile)));
#pragma warning restore 0618
            xrTemp.ValidationType = ValidationType.None;
            xrTemp.EntityHandling = EntityHandling.ExpandEntities;
            xsltSameInstance.Load(xrTemp);
        }

        public override int Transform(object args)
        {
            for (int i = 1; i <= 100; i++)
            {
                XmlReader xr = null;
                xr = xsltSameInstance.Transform(_xd, null);
            }

            //_output.WriteLine("Transform: Thread " + args + ": Done with READER transform...");
            return 1;
        }
    }

    //[TestCase(Name = "Same instance testing: Transform() - TEXTWRITER")]
    public class SameInstanceXslTransformWriter : CSameInstanceXslTransformTestCase
    {
        private XPathDocument _xd;        // Loads XML file

        private ITestOutputHelper _output;
        public SameInstanceXslTransformWriter(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        protected override void Load(string _strXslFile, string _strXmlFile)
        {
#pragma warning disable 0618
            XmlValidatingReader xrData = new XmlValidatingReader(new XmlTextReader(Path.Combine(_strPath, _strXmlFile)));
#pragma warning restore 0618
            _xd = new XPathDocument(xrData, XmlSpace.Preserve);
            xrData.Dispose();

#pragma warning disable 0618
            XmlValidatingReader xrTemp = new XmlValidatingReader(new XmlTextReader(Path.Combine(_strPath, _strXslFile)));
            xrTemp.ValidationType = ValidationType.None;
            xrTemp.EntityHandling = EntityHandling.ExpandEntities;
            xsltSameInstance.Load(xrTemp);
        }

        public override int Transform(object args)
        {
            for (int i = 1; i <= 100; i++)
            {
                using (XmlTextWriter tw = new XmlTextWriter(System.IO.TextWriter.Null))
                {
                    xsltSameInstance.Transform(_xd, null, tw);
                }
            }

            //_output.WriteLine("Transform: Thread " + args + ": Done with WRITER transform...");
            return 1;
        }
    }
}
