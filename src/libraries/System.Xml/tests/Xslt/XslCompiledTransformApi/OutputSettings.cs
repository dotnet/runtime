using OLEDB.Test.ModuleCore;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.XmlDiff;
using System.Xml.Xsl;

namespace XsltApiV2
{
    [TestCase(Name = "OutputSettings", Desc = "This testcase tests the OutputSettings on XslCompiledTransform", Param = "Debug")]
    public class COutputSettings : XsltApiTestCaseBase
    {
        private XslCompiledTransform xsl = null;
        private string XmlFile = string.Empty;
        private string XslFile = string.Empty;

        private int Init(string xmlFile, string xslFile)
        {
            xsl = new XslCompiledTransform();

            XmlFile = FullFilePath(xmlFile);
            XslFile = FullFilePath(xslFile);

            return TEST_PASS;
        }

        private StringWriter Transform()
        {
            StringWriter sw = new StringWriter();
            xsl.Transform(XmlFile, null, sw);
            return sw;
        }

        private int VerifyResult(object actual, object expected, string message)
        {
            CError.WriteLine("Expected : {0}", expected);
            CError.WriteLine("Actual : {0}", actual);

            if (CError.Compare(actual, expected, message))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation(id = 1, Desc = "Verify the default value of the OutputSettings, expected null", Pri = 0)]
        public int OS1()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            CError.Compare(xslt.OutputSettings == null, "Default value of OutputSettings is not null");
            return TEST_PASS;
        }

        [Variation(id = 2, Desc = "Verify the OutputMethod when output method is not specified, expected AutoDetect", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_Default.xsl" })]
        [Variation(id = 3, Desc = "Verify the OutputMethod when output method is xml, expected Xml", Pri = 0, Params = new object[] { "books.xml", "OutputMethod_Xml.xsl" })]
        [Variation(id = 4, Desc = "Verify the OutputMethod when output method is html, expected Html", Pri = 0, Params = new object[] { "books.xml", "OutputMethod_Html.xsl" })]
        [Variation(id = 5, Desc = "Verify the OutputMethod when output method is text, expected Text", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_Text.xsl" })]
        [Variation(id = 6, Desc = "Verify the OutputMethod when output method is not specified, first output element is html, expected AutoDetect", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_LiteralHtml.xsl" })]
        [Variation(id = 7, Desc = "Verify the OutputMethod when multiple output methods (Xml,Html,Text) are defined, expected Text", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_Multiple1.xsl" })]
        [Variation(id = 8, Desc = "Verify the OutputMethod when multiple output methods (Html,Text,Xml) are defined, expected Xml", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_Multiple2.xsl" })]
        [Variation(id = 9, Desc = "Verify the OutputMethod when multiple output methods (Text,Xml,Html) are defined, expected Html", Pri = 1, Params = new object[] { "books.xml", "OutputMethod_Multiple3.xsl" })]
        public int OS2()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());
            xsl.Load(XslFile);
            XmlWriterSettings os = xsl.OutputSettings;

            switch (CurVariation.id)
            {
                case 2:
                    CError.Compare(os.OutputMethod, XmlOutputMethod.AutoDetect, "Default output method must be Xml");
                    break;

                case 3:
                    CError.Compare(os.OutputMethod, XmlOutputMethod.Xml, "Output method must be Xml");
                    break;

                case 4:
                    CError.Compare(os.OutputMethod, XmlOutputMethod.Html, "Output method must be Html");
                    break;

                case 5:
                    CError.Compare(os.OutputMethod, XmlOutputMethod.Text, "Output method must be Text");
                    break;

                case 6:
                    CError.Compare(os.OutputMethod, XmlOutputMethod.AutoDetect, "Output method must be Html");
                    break;

                case 7:
                    CError.Compare(os.OutputMethod, XmlOutputMethod.Text, "Output method must be Text");
                    break;

                case 8:
                    CError.Compare(os.OutputMethod, XmlOutputMethod.Xml, "Output method must be Xml");
                    break;

                case 9:
                    CError.Compare(os.OutputMethod, XmlOutputMethod.Html, "Output method must be Html");
                    break;
            }

            return TEST_PASS;
        }

        [Variation(id = 10, Desc = "Verify OmitXmlDeclaration when omit-xml-declared is omitted in XSLT, expected false", Pri = 0, Params = new object[] { "books.xml", "OmitXmlDecl_Default.xsl", false, "Default value for OmitXmlDeclaration is 'no'" })]
        [Variation(id = 11, Desc = "Verify OmitXmlDeclaration when omit-xml-declared is yes in XSLT, expected true", Pri = 0, Params = new object[] { "books.xml", "OmitXmlDecl_Yes.xsl", true, "OmitXmlDeclaration must be 'yes'" })]
        [Variation(id = 12, Desc = "Verify OmitXmlDeclaration when omit-xml-declared is no in XSLT, expected false", Pri = 1, Params = new object[] { "books.xml", "OmitXmlDecl_No.xsl", false, "OmitXmlDeclaration must be 'no'" })]
        public int OS3()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());
            xsl.Load(XslFile);
            XmlWriterSettings os = xsl.OutputSettings;
            CError.Compare(os.OmitXmlDeclaration, (bool)CurVariation.Params[2], CurVariation.Params[3].ToString());
            return TEST_PASS;
        }

        [Variation(id = 13, Desc = "Verify OutputSettings when omit-xml-declaration has an invalid value, expected null", Pri = 2, Params = new object[] { "books.xml", "OmitXmlDecl_Invalid1.xsl" })]
        public int OS4()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());

            try
            {
                xsl.Load(XslFile);
            }
            catch (XsltException e)
            {
                CError.WriteLine(e);
                XmlWriterSettings os = xsl.OutputSettings;
                CError.Compare(os, null, "OutputSettings must be null when load is unsuccessful");
            }

            return TEST_PASS;
        }

        [Variation(id = 14, Desc = "Verify OutputSettings on non well-formed XSLT, expected null", Pri = 2, Params = new object[] { "books.xml", "OmitXmlDecl_Invalid2.xsl" })]
        public int OS5()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());

            try
            {
                xsl.Load(XslFile);
            }
            catch (XsltException e)
            {
                CError.WriteLine(e);
                XmlWriterSettings os = xsl.OutputSettings;
                CError.Compare(os, null, "OutputSettings must be null when load is unsuccessful");
            }

            return TEST_PASS;
        }

        [Variation(id = 15, Desc = "Verify default Encoding, expected UTF-8", Pri = 1, Params = new object[] { "books.xml", "Encoding1.xsl", "utf-8", "Default Encoding must be utf-8" })]
        [Variation(id = 16, Desc = "Verify Encoding set to UTF-8 explicitly, expected UTF-8", Pri = 1, Params = new object[] { "books.xml", "Encoding2.xsl", "utf-8", "Encoding must be utf-8" })]
        [Variation(id = 17, Desc = "Verify Encoding set to UTF-16 explicitly, expected UTF-16", Pri = 1, Params = new object[] { "books.xml", "Encoding3.xsl", "utf-16", "Encoding must be utf-16" })]
        [Variation(id = 18, Desc = "Verify Encoding set to windows-1252 explicitly, expected windows-1252", Pri = 1, Params = new object[] { "books.xml", "Encoding4.xsl", "windows-1252", "Encoding must be windows-1252" })]
        [Variation(id = 19, Desc = "Verify Encoding when multiple xsl:output tags are present, expected the last set (iso-8859-1)", Pri = 1, Params = new object[] { "books.xml", "Encoding5.xsl", "iso-8859-1", "Encoding must be iso-8859-1" })]
        public int OS6()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());
            xsl.Load(XslFile);
            XmlWriterSettings os = xsl.OutputSettings;
            string headerName = os.Encoding.HeaderName.ToLower(CultureInfo.InvariantCulture);

            CError.Compare(headerName, CurVariation.Params[2].ToString(), CurVariation.Params[3].ToString());
            return TEST_PASS;
        }

        [Variation(id = 20, Desc = "Verify Indent when indent is omitted in XSLT, expected false", Pri = 0, Params = new object[] { "books.xml", "Indent_Default.xsl", false, "Default value for Indent is 'no'" })]
        [Variation(id = 21, Desc = "Verify Indent when indent is yes in XSLT, expected true", Pri = 0, Params = new object[] { "books.xml", "Indent_Yes.xsl", true, "Indent must be 'yes'" })]
        [Variation(id = 22, Desc = "Verify Indent when indent is no in XSLT, expected false", Pri = 1, Params = new object[] { "books.xml", "Indent_No.xsl", false, "Indent must be 'no'" })]
        public int OS7()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());
            xsl.Load(XslFile);
            XmlWriterSettings os = xsl.OutputSettings;
            CError.Compare(os.Indent, (bool)CurVariation.Params[2], CurVariation.Params[3].ToString());
            return TEST_PASS;
        }

        [Variation(id = 23, Desc = "Verify OutputSettings when Indent has an invalid value, expected null", Pri = 2, Params = new object[] { "books.xml", "Indent_Invalid1.xsl" })]
        public int OS8()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());

            try
            {
                xsl.Load(XslFile);
            }
            catch (XsltException e)
            {
                CError.WriteLine(e);
                XmlWriterSettings os = xsl.OutputSettings;
                CError.Compare(os, null, "OutputSettings must be null when load is unsuccessful");
            }

            return TEST_PASS;
        }

        [Variation(id = 24, Desc = "Verify OutputSettings on non well-formed XSLT, expected null", Pri = 2, Params = new object[] { "books.xml", "Indent_Invalid2.xsl" })]
        public int OS9()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());

            try
            {
                xsl.Load(XslFile);
            }
            catch (XsltException e)
            {
                CError.WriteLine(e);
                XmlWriterSettings os = xsl.OutputSettings;
                CError.Compare(os, null, "OutputSettings must be null when load is unsuccessful");
            }

            return TEST_PASS;
        }

        [Variation(id = 25, Desc = "Verify OutputSettings with all attributes on xsl:output", Pri = 0, Params = new object[] { "books.xml", "OutputSettings.xsl" })]
        public int OS10()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());
            xsl.Load(XslFile);
            XmlWriterSettings os = xsl.OutputSettings;
            CError.WriteLine("OmitXmlDeclaration : {0}", os.OmitXmlDeclaration);
            CError.Compare(os.OmitXmlDeclaration, true, "OmitXmlDeclaration must be true");
            CError.WriteLine("Indent : {0}", os.Indent);
            CError.Compare(os.Indent, true, "Indent must be true");
            CError.WriteLine("OutputMethod : {0}", os.OutputMethod);
            CError.Compare(os.OutputMethod, XmlOutputMethod.Xml, "OutputMethod must be Xml");
            CError.WriteLine("Encoding : {0}", os.Encoding.HeaderName);
            CError.Compare(os.Encoding.HeaderName.ToLower(CultureInfo.InvariantCulture), "utf-8", "OmitXmlDeclaration must be true");

            return TEST_PASS;
        }

        [Variation(id = 26, Desc = "Compare Stream Output with XmlWriter over Stream with OutputSettings", Pri = 0, Params = new object[] { "books.xml", "OutputSettings1.xsl" })]
        public int OS11()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());
            xsl.Load(XslFile);

            //Transform to Stream
            Stream stm1 = new FileStream("out1.xml", FileMode.Create, FileAccess.ReadWrite);
            CError.WriteLine("Transforming to Stream1 - 'out1.xml'");
            xsl.Transform(XmlFile, null, stm1);

            //Create an XmlWriter using OutputSettings
            Stream stm2 = new FileStream("out2.xml", FileMode.Create, FileAccess.ReadWrite);

            XmlWriterSettings os = xsl.OutputSettings;
            XmlWriter xw = XmlWriter.Create(stm2, os);

            //Transform to XmlWriter
            CError.WriteLine("Transforming to XmlWriter over Stream2 with XSLT	OutputSettings - 'out2.xml'");
            xsl.Transform(XmlFile, null, xw);

            //Close the streams
            stm1.Close();
            stm2.Close();

            //XmlDiff the 2 Outputs.
            XmlDiff diff = new XmlDiff();
            XmlReader xr1 = XmlReader.Create("out1.xml");
            XmlReader xr2 = XmlReader.Create("out2.xml");

            //XmlDiff
            CError.WriteLine("Comparing the Stream Output and XmlWriter Output");
            CError.Compare(diff.Compare(xr1, xr2), "Stream and XmlWriter Outputs are different");

            //Delete the temp files
            xr1.Close();
            xr2.Close();
            File.Delete("out1.xml");
            File.Delete("out2.xml");

            return TEST_PASS;
        }
    }
}