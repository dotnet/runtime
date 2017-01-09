// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.Xsl;
using System.Text;

namespace System.Xml.Tests
{
    //[TestCase(Name = "OutputSettings", Desc = "This testcase tests the OutputSettings on XslCompiledTransform", Param = "Debug")]
    public class COutputSettings : XsltApiTestCaseBase2
    {
        private XslCompiledTransform _xsl = null;
        private string _xmlFile = string.Empty;
        private string _xslFile = string.Empty;

        private ITestOutputHelper _output;
        public COutputSettings(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        private void Init(string xmlFile, string xslFile)
        {
            _xsl = new XslCompiledTransform();

            _xmlFile = FullFilePath(xmlFile);
            _xslFile = FullFilePath(xslFile);

            return;
        }

        private StringWriter Transform()
        {
            StringWriter sw = new StringWriter();
            _xsl.Transform(_xmlFile, null, sw);
            return sw;
        }

        private void VerifyResult(object actual, object expected, string message)
        {
            _output.WriteLine("Expected : {0}", expected);
            _output.WriteLine("Actual : {0}", actual);

            Assert.Equal(actual, expected);
        }

        //[Variation(id = 1, Desc = "Verify the default value of the OutputSettings, expected null", Pri = 0)]
        [InlineData()]
        [Theory]
        public void OS1()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            Assert.True(xslt.OutputSettings == null);
            return;
        }

        //[Variation(id = 2, Desc = "Verify the OutputMethod when output method is not specified, expected AutoDetect", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_Default.xsl" })]
        [InlineData("books.xml", "OutputMethod_Default.xsl", 2)]
        //[Variation(id = 3, Desc = "Verify the OutputMethod when output method is xml, expected Xml", Pri = 0, Params = new object[] { "books.xml", "OutputMethod_Xml.xsl" })]
        [InlineData("books.xml", "OutputMethod_Xml.xsl", 3)]
        //[Variation(id = 4, Desc = "Verify the OutputMethod when output method is html, expected Html", Pri = 0, Params = new object[] { "books.xml", "OutputMethod_Html.xsl" })]
        [InlineData("books.xml", "OutputMethod_Html.xsl", 4)]
        //[Variation(id = 5, Desc = "Verify the OutputMethod when output method is text, expected Text", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_Text.xsl" })]
        [InlineData("books.xml", "OutputMethod_Text.xsl", 5)]
        //[Variation(id = 6, Desc = "Verify the OutputMethod when output method is not specified, first output element is html, expected AutoDetect", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_LiteralHtml.xsl" })]
        [InlineData("books.xml", "OutputMethod_LiteralHtml.xsl", 6)]
        //[Variation(id = 7, Desc = "Verify the OutputMethod when multiple output methods (Xml,Html,Text) are defined, expected Text", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_Multiple1.xsl" })]
        [InlineData("books.xml", "OutputMethod_Multiple1.xsl", 7)]
        //[Variation(id = 8, Desc = "Verify the OutputMethod when multiple output methods (Html,Text,Xml) are defined, expected Xml", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_Multiple2.xsl" })]
        [InlineData("books.xml", "OutputMethod_Multiple2.xsl", 8)]
        //[Variation(id = 9, Desc = "Verify the OutputMethod when multiple output methods (Text,Xml,Html) are defined, expected Html", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_Multiple3.xsl" })]
        [InlineData("books.xml", "OutputMethod_Multiple3.xsl", 9)]
        [Theory]
        public void OS2(object param0, object param1, object param2)
        {
            Init(param0.ToString(), param1.ToString());
            _xsl.Load(_xslFile);
            XmlWriterSettings os = _xsl.OutputSettings;

            switch ((int)param2)
            {
                case 2:
                    Assert.Equal(os.OutputMethod, XmlOutputMethod.AutoDetect);
                    break;

                case 3:
                    Assert.Equal(os.OutputMethod, XmlOutputMethod.Xml);
                    break;

                case 4:
                    Assert.Equal(os.OutputMethod, XmlOutputMethod.Html);
                    break;

                case 5:
                    Assert.Equal(os.OutputMethod, XmlOutputMethod.Text);
                    break;

                case 6:
                    Assert.Equal(os.OutputMethod, XmlOutputMethod.AutoDetect);
                    break;

                case 7:
                    Assert.Equal(os.OutputMethod, XmlOutputMethod.Text);
                    break;

                case 8:
                    Assert.Equal(os.OutputMethod, XmlOutputMethod.Xml);
                    break;

                case 9:
                    Assert.Equal(os.OutputMethod, XmlOutputMethod.Html);
                    break;
            }

            return;
        }

        //[Variation(id = 10, Desc = "Verify OmitXmlDeclaration when omit-xml-declared is omitted in XSLT, expected false", Pri = 0, Params = new object[] { "books.xml", "OmitXmlDecl_Default.xsl", false, "Default value for OmitXmlDeclaration is 'no'" })]
        [InlineData("books.xml", "OmitXmlDecl_Default.xsl", false, "Default value for OmitXmlDeclaration is 'no'")]
        //[Variation(id = 11, Desc = "Verify OmitXmlDeclaration when omit-xml-declared is yes in XSLT, expected true", Pri = 0, Params = new object[] { "books.xml", "OmitXmlDecl_Yes.xsl", true, "OmitXmlDeclaration must be 'yes'" })]
        [InlineData("books.xml", "OmitXmlDecl_Yes.xsl", true, "OmitXmlDeclaration must be 'yes'")]
        //[Variation(id = 12, Desc = "Verify OmitXmlDeclaration when omit-xml-declared is no in XSLT, expected false", Pri = 1, Params = new object[] { "books.xml", "OmitXmlDecl_No.xsl", false, "OmitXmlDeclaration must be 'no'" })]
        [InlineData("books.xml", "OmitXmlDecl_No.xsl", false, "OmitXmlDeclaration must be 'no'")]
        [Theory]
        public void OS3(object param0, object param1, object param2, object param3)
        {
            Init(param0.ToString(), param1.ToString());
            _xsl.Load(_xslFile);
            XmlWriterSettings os = _xsl.OutputSettings;
            Assert.Equal(os.OmitXmlDeclaration, (bool)param2);
            return;
        }

        //[Variation(id = 13, Desc = "Verify OutputSettings when omit-xml-declaration has an invalid value, expected null", Pri = 2, Params = new object[] { "books.xml", "OmitXmlDecl_Invalid1.xsl" })]
        [InlineData("books.xml", "OmitXmlDecl_Invalid1.xsl")]
        [Theory]
        public void OS4(object param0, object param1)
        {
            Init(param0.ToString(), param1.ToString());

            try
            {
                _xsl.Load(_xslFile);
            }
            catch (XsltException e)
            {
                _output.WriteLine(e.ToString());
                XmlWriterSettings os = _xsl.OutputSettings;
                Assert.Equal(os, null);
            }

            return;
        }

        //[Variation(id = 14, Desc = "Verify OutputSettings on non well-formed XSLT, expected null", Pri = 2, Params = new object[] { "books.xml", "OmitXmlDecl_Invalid2.xsl" })]
        [InlineData("books.xml", "OmitXmlDecl_Invalid2.xsl")]
        [Theory]
        public void OS5(object param0, object param1)
        {
            Init(param0.ToString(), param1.ToString());

            try
            {
                _xsl.Load(_xslFile);
            }
            catch (XsltException e)
            {
                _output.WriteLine(e.ToString());
                XmlWriterSettings os = _xsl.OutputSettings;
                Assert.Equal(os, null);
            }

            return;
        }

        //[Variation(id = 18, Desc = "Verify Encoding set to windows-1252 explicitly, expected windows-1252", Pri = 1, Params = new object[] { "books.xml", "Encoding4.xsl", "windows-1252", "Encoding must be windows-1252" })]
        [InlineData("books.xml", "Encoding4.xsl", "windows-1252", "Encoding must be windows-1252")]
        [Theory]
        public void OS6_Windows1252Encoding(object param0, object param1, object param2, object param3)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            OS6(param0, param1, param2, param3);
        }

        //[Variation(id = 15, Desc = "Verify default Encoding, expected UTF-8", Pri = 1, Params = new object[] { "books.xml", "Encoding1.xsl", "utf-8", "Default Encoding must be utf-8" })]
        [InlineData("books.xml", "Encoding1.xsl", "utf-8", "Default Encoding must be utf-8")]
        //[Variation(id = 16, Desc = "Verify Encoding set to UTF-8 explicitly, expected UTF-8", Pri = 1, Params = new object[] { "books.xml", "Encoding2.xsl", "utf-8", "Encoding must be utf-8" })]
        [InlineData("books.xml", "Encoding2.xsl", "utf-8", "Encoding must be utf-8")]
        //[Variation(id = 17, Desc = "Verify Encoding set to UTF-16 explicitly, expected UTF-16", Pri = 1, Params = new object[] { "books.xml", "Encoding3.xsl", "utf-16", "Encoding must be utf-16" })]
        [InlineData("books.xml", "Encoding3.xsl", "utf-16", "Encoding must be utf-16")]
        //[Variation(id = 19, Desc = "Verify Encoding when multiple xsl:output tags are present, expected the last set (iso-8859-1)", Pri = 1, Params = new object[] { "books.xml", "Encoding5.xsl", "iso-8859-1", "Encoding must be iso-8859-1" })]
        [InlineData("books.xml", "Encoding5.xsl", "iso-8859-1", "Encoding must be iso-8859-1")]
        [Theory]
        public void OS6(object param0, object param1, object param2, object param3)
        {
            Init(param0.ToString(), param1.ToString());
            _xsl.Load(_xslFile);
            XmlWriterSettings os = _xsl.OutputSettings;

            Assert.Equal(os.Encoding, System.Text.Encoding.GetEncoding((string)param2));
            return;
        }

        //[Variation(id = 20, Desc = "Verify Indent when indent is omitted in XSLT, expected false", Pri = 0, Params = new object[] { "books.xml", "Indent_Default.xsl", false, "Default value for Indent is 'no'" })]
        [InlineData("books.xml", "Indent_Default.xsl", false, "Default value for Indent is 'no'")]
        //[Variation(id = 21, Desc = "Verify Indent when indent is yes in XSLT, expected true", Pri = 0, Params = new object[] { "books.xml", "Indent_Yes.xsl", true, "Indent must be 'yes'" })]
        [InlineData("books.xml", "Indent_Yes.xsl", true, "Indent must be 'yes'")]
        //[Variation(id = 22, Desc = "Verify Indent when indent is no in XSLT, expected false", Pri = 1, Params = new object[] { "books.xml", "Indent_No.xsl", false, "Indent must be 'no'" })]
        [InlineData("books.xml", "Indent_No.xsl", false, "Indent must be 'no'")]
        [Theory]
        public void OS7(object param0, object param1, object param2, object param3)
        {
            Init(param0.ToString(), param1.ToString());
            _xsl.Load(_xslFile);
            XmlWriterSettings os = _xsl.OutputSettings;
            Assert.Equal(os.Indent, (bool)param2);
            return;
        }

        //[Variation(id = 23, Desc = "Verify OutputSettings when Indent has an invalid value, expected null", Pri = 2, Params = new object[] { "books.xml", "Indent_Invalid1.xsl" })]
        [InlineData("books.xml", "Indent_Invalid1.xsl")]
        [Theory]
        public void OS8(object param0, object param1)
        {
            Init(param0.ToString(), param1.ToString());

            try
            {
                _xsl.Load(_xslFile);
            }
            catch (XsltException e)
            {
                _output.WriteLine(e.ToString());
                XmlWriterSettings os = _xsl.OutputSettings;
                Assert.Equal(os, null);
            }

            return;
        }

        //[Variation(id = 24, Desc = "Verify OutputSettings on non well-formed XSLT, expected null", Pri = 2, Params = new object[] { "books.xml", "Indent_Invalid2.xsl" })]
        [InlineData("books.xml", "Indent_Invalid2.xsl")]
        [Theory]
        public void OS9(object param0, object param1)
        {
            Init(param0.ToString(), param1.ToString());

            try
            {
                _xsl.Load(_xslFile);
            }
            catch (XsltException e)
            {
                _output.WriteLine(e.ToString());
                XmlWriterSettings os = _xsl.OutputSettings;
                Assert.Equal(os, null);
            }

            return;
        }

        //[Variation(id = 25, Desc = "Verify OutputSettings with all attributes on xsl:output", Pri = 0, Params = new object[] { "books.xml", "OutputSettings.xsl" })]
        [InlineData("books.xml", "OutputSettings.xsl")]
        [Theory]
        public void OS10(object param0, object param1)
        {
            Init(param0.ToString(), param1.ToString());
            _xsl.Load(_xslFile);
            XmlWriterSettings os = _xsl.OutputSettings;
            _output.WriteLine("OmitXmlDeclaration : {0}", os.OmitXmlDeclaration);
            Assert.Equal(os.OmitXmlDeclaration, true);
            _output.WriteLine("Indent : {0}", os.Indent);
            Assert.Equal(os.Indent, true);
            _output.WriteLine("OutputMethod : {0}", os.OutputMethod);
            Assert.Equal(os.OutputMethod, XmlOutputMethod.Xml);
            _output.WriteLine("Encoding : {0}", os.Encoding.ToString());
            Assert.Equal(os.Encoding, System.Text.Encoding.GetEncoding("utf-8"));

            return;
        }

        //[Variation(id = 26, Desc = "Compare Stream Output with XmlWriter over Stream with OutputSettings", Pri = 0, Params = new object[] { "books.xml", "OutputSettings1.xsl" })]
        [InlineData("books.xml", "OutputSettings1.xsl")]
        [Theory]
        public void OS11(object param0, object param1)
        {
            Init(param0.ToString(), param1.ToString());
            _xsl.Load(_xslFile);

            //Transform to Stream
            Stream stm1 = new FileStream("out1.xml", FileMode.Create, FileAccess.ReadWrite);
            _output.WriteLine("Transforming to Stream1 - 'out1.xml'");
            _xsl.Transform(_xmlFile, null, stm1);

            //Create an XmlWriter using OutputSettings
            Stream stm2 = new FileStream("out2.xml", FileMode.Create, FileAccess.ReadWrite);

            XmlWriterSettings os = _xsl.OutputSettings;
            XmlWriter xw = XmlWriter.Create(stm2, os);

            //Transform to XmlWriter
            _output.WriteLine("Transforming to XmlWriter over Stream2 with XSLT	OutputSettings - 'out2.xml'");
            _xsl.Transform(_xmlFile, null, xw);

            //Close the streams
            stm1.Dispose();
            stm2.Dispose();

            //XmlDiff the 2 Outputs.
            XmlDiff.XmlDiff diff = new XmlDiff.XmlDiff();
            XmlReader xr1 = XmlReader.Create("out1.xml");
            XmlReader xr2 = XmlReader.Create("out2.xml");

            //XmlDiff
            _output.WriteLine("Comparing the Stream Output and XmlWriter Output");
            Assert.True(diff.Compare(xr1, xr2));

            //Delete the temp files
            xr1.Dispose();
            xr2.Dispose();
            File.Delete("out1.xml");
            File.Delete("out2.xml");

            return;
        }
    }
}