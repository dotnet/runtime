// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    //[TestCase(Name = "Schematron with XslTransform", Desc = "Test the Schematron Stylesheets with XslTransform", Pri = 2, Param = "v1")]
    //[TestCase(Name = "Schematron with XslCompiledTransform", Desc = "Test the Schematron Stylesheets with XslCompiledTransform", Pri = 2, Param = "v2")]
    public class Schematron //: CTestCase
    {
        /*
        Refer to the page at http://www.xml.com/pub/a/2003/11/12/schematron.html for testing logic.

        Basically, Schematron schema (not XSD) is transformed using one of the Schematron stylesheets (basic) into a
        Validating XSLT which in turn is used to transform the actual XML Instance

        Parameter 1 to the test is Schematron Schema
        Parameter 2 to the test is the XML instance
        Parameter 3 is the Schematron Stylesheet (schematron-basic.xsl, schematron-message.xsl, schematron-report.xsl)
        */

#pragma warning disable 0618
        private XslTransform _xsltV1 = null;
#pragma warning restore 0618
        private XslCompiledTransform _xsltV2 = null;

        private TextWriter _sw = null;
        private string _schemaFile = string.Empty;
        private string _xmlFile = string.Empty;
        private string _xslFile = string.Empty;
        private string _baseline = string.Empty;
        private string _outFile = "out.xml";
        private Utils _utils;

        private ITestOutputHelper _output;
        public Schematron(ITestOutputHelper output)
        {
            _output = output;
        }

        private void Init(string schemaFile, string xmlFile, string xslType, string baseline, string ver)
        {
            _utils = new Utils(_output);

#pragma warning disable 0618
            if (ver == "v1")
                _xsltV1 = new XslTransform();
#pragma warning restore 0618

            else if (ver == "v2")
                _xsltV2 = new XslCompiledTransform();

            _schemaFile = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltScenarios\schematron\" + schemaFile);
            _xmlFile = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltScenarios\schematron\" + xmlFile);
            if (baseline == string.Empty)
                _baseline = string.Empty;
            else
                _baseline = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltScenarios\schematron\" + ver + baseline);

            switch (xslType.ToLower())
            {
                case "basic":
                default:
                    _xslFile = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltScenarios\schematron\" + "schematron-basic.xsl");
                    break;

                case "message":
                    _xslFile = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltScenarios\schematron\" + "schematron-message.xsl");
                    break;

                case "report":
                    _xslFile = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltScenarios\schematron\" + "schematron-report.xsl");
                    break;
            }

            //Delete the output file if it exists
            if (File.Exists(_outFile))
                File.Delete(_outFile);
        }

        private void GenerateValidatingXslt(string ver)
        {
            //Write the output to a string (using TextWriter)
            _sw = new StringWriter();
            _output.WriteLine("Generating Validating XSLT using Schematron schema ...");

            if (ver == "v1")
            {
                _xsltV1.Load(_xslFile, new XmlUrlResolver());
                XPathDocument doc = new XPathDocument(_schemaFile);
                _xsltV1.Transform(doc, null, _sw);
            }
            else if (ver == "v2")
            {
                _xsltV2.Load(_xslFile, new XsltSettings(), new XmlUrlResolver());
                _xsltV2.Transform(_schemaFile, null, _sw);
            }
        }

        private void ValidateInstance(string ver)
        {
            XmlReader xr = XmlReader.Create(new StringReader(_sw.ToString()));
            _output.WriteLine("Validating XML Instance using Validating XSLT ...");

            if (ver == "v1")
            {
                _xsltV1.Load(xr, new XmlUrlResolver());
                _xsltV1.Transform(_xmlFile, _outFile);
            }
            else if (ver == "v2")
            {
                _xsltV2.Load(xr, new XsltSettings(), new XmlUrlResolver());
                _xsltV2.Transform(_xmlFile, _outFile);
            }

            _utils.WriteFile(_outFile);
        }

        //BinCompat TODO: Re-write and re-enable checksum tests
        ////[Variation(id = 1, Desc = "Testing the XML Instance to have Male Gender when Title is Mr, using Basic Schematron Stylesheet", Pri = 2, Params = new object[] { "TestSchema.xml", "Test1.xml", "Basic", "Test1.txt" })]
        //[InlineData("TestSchema.xml", "Test1.xml", "Basic", "Test1.txt", "v1")]
        //[InlineData("TestSchema.xml", "Test1.xml", "Basic", "Test1.txt", "v2")]
        ////[Variation(id = 2, Desc = "Testing the XML Instance to have Male Gender when Title is Mr, using Message Schematron Stylesheet", Pri = 0, Params = new object[] { "TestSchema.xml", "Test1.xml", "Message", "" })]
        //[InlineData("TestSchema.xml", "Test1.xml", "Message", "", "v1")]
        //[InlineData("TestSchema.xml", "Test1.xml", "Message", "", "v2")]
        ////[Variation(id = 4, Desc = "Testing the XML Instance for a mandatory Title attribute, using Basic Schematron Stylesheet", Pri = 2, Params = new object[] { "TestSchema.xml", "Test4.xml", "Basic", "Test4.txt" })]
        //[InlineData("TestSchema.xml", "Test4.xml", "Basic", "Test4.txt", "v1")]
        //[InlineData("TestSchema.xml", "Test4.xml", "Basic", "Test4.txt", "v2")]
        ////[Variation(id = 5, Desc = "Testing the XML Instance for a mandatory Title attribute, using Message Schematron Stylesheet", Pri = 0, Params = new object[] { "TestSchema.xml", "Test4.xml", "Message", "" })]
        //[InlineData("TestSchema.xml", "Test4.xml", "Message", "", "v1")]
        //[InlineData("TestSchema.xml", "Test4.xml", "Message", "", "v2")]
        ////[Variation(id = 7, Desc = "Testing the XML Instance for unexpected children, using Basic Schematron Stylesheet", Pri = 2, Params = new object[] { "TestSchema.xml", "Test7.xml", "Basic", "Test7.txt" })]
        //[InlineData("TestSchema.xml", "Test7.xml", "Basic", "Test7.txt", "v1")]
        //[InlineData("TestSchema.xml", "Test7.xml", "Basic", "Test7.txt", "v2")]
        ////[Variation(id = 8, Desc = "Testing the XML Instance for unexpected children, using Message Schematron Stylesheet", Pri = 0, Params = new object[] { "TestSchema.xml", "Test7.xml", "Message", "" })]
        //[InlineData("TestSchema.xml", "Test7.xml", "Message", "", "v1")]
        //[InlineData("TestSchema.xml", "Test7.xml", "Message", "", "v2")]
        ////[Variation(id = 10, Desc = "Testing the XML Instance for children sequence, using Basic Schematron Stylesheet", Pri = 2, Params = new object[] { "TestSchema.xml", "Test10.xml", "Basic", "Test10.txt" })]
        //[InlineData("TestSchema.xml", "Test10.xml", "Basic", "Test10.txt", "v1")]
        //[InlineData("TestSchema.xml", "Test10.xml", "Basic", "Test10.txt", "v2")]
        ////[Variation(id = 11, Desc = "Testing the XML Instance for children sequence, using Message Schematron Stylesheet", Pri = 0, Params = new object[] { "TestSchema.xml", "Test10.xml", "Message", "" })]
        //[InlineData("TestSchema.xml", "Test10.xml", "Message", "", "v1")]
        //[InlineData("TestSchema.xml", "Test10.xml", "Message", "", "v2")]
        [Theory]
        public void SchematronTest(object param0, object param1, object param2, object param3, object param4)
        {
            Init(param0.ToString(), param1.ToString(), param2.ToString(), param3.ToString(), param4.ToString());

            GenerateValidatingXslt(param4.ToString());
            ValidateInstance(param4.ToString());
            _utils.VerifyChecksum(_outFile, _baseline);
        }
    }
}