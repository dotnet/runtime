using OLEDB.Test.ModuleCore;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace XsltApiV2
{
    [TestCase(Name = "XsltSettings-Retail", Desc = "This testcase tests the different settings on XsltSettings and the corresponding behavior in retail mode", Param = "Retail")]
    [TestCase(Name = "XsltSettings-Debug", Desc = "This testcase tests the different settings on XsltSettings and the corresponding behavior in debug mode", Param = "Debug")]
    public class CXsltSettings : XsltApiTestCaseBase
    {
        private XslCompiledTransform xsl = null;
        private string XmlFile = string.Empty;
        private string XslFile = string.Empty;

        private bool _debug = false;

        private int Init(string xmlFile, string xslFile)
        {
            if (Param.ToString() == "Debug")
            {
                xsl = new XslCompiledTransform(true);
                _debug = true;
            }
            else
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

        [Variation(id = 1, Desc = "Test the script block with EnableScript, should work", Pri = 0, Params = new object[] { "XsltSettings.xml", "XsltSettings1.xsl", false, true })]
        [Variation(id = 2, Desc = "Test the script block with Default Settings, should fail", Pri = 0, Params = new object[] { "XsltSettings.xml", "XsltSettings1.xsl", false, false })]
        [Variation(id = 3, Desc = "Test the script block with EnableDocumentFunction, should fail", Pri = 2, Params = new object[] { "XsltSettings.xml", "XsltSettings1.xsl", true, false })]
        [Variation(id = 4, Desc = "Test the script block with TrustedXslt, should work", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings1.xsl", true, true })]
        [Variation(id = 5, Desc = "Test the document function with EnableDocumentFunction, should work", Pri = 0, Params = new object[] { "XsltSettings.xml", "XsltSettings2.xsl", true, false })]
        [Variation(id = 6, Desc = "Test the document function with Default Settings, should fail", Pri = 0, Params = new object[] { "XsltSettings.xml", "XsltSettings2.xsl", false, false })]
        [Variation(id = 7, Desc = "Test the document function with EnableScript, should fail", Pri = 2, Params = new object[] { "XsltSettings.xml", "XsltSettings2.xsl", false, true })]
        [Variation(id = 8, Desc = "Test the document function with TrustedXslt, should work", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings2.xsl", true, true })]
        [Variation(id = 9, Desc = "Test the combination of script and document function with TrustedXslt, should work", Pri = 0, Params = new object[] { "XsltSettings.xml", "XsltSettings3.xsl", true, true })]
        [Variation(id = 10, Desc = "Test the combination of script and document function with Default Settings, should fail", Pri = 0, Params = new object[] { "XsltSettings.xml", "XsltSettings3.xsl", false, false })]
        [Variation(id = 11, Desc = "Test the combination of script and document function with EnableScript, only script should work", Pri = 2, Params = new object[] { "XsltSettings.xml", "XsltSettings3.xsl", false, true })]
        [Variation(id = 12, Desc = "Test the combination of script and document function with EnableDocumentFunction, only document should work", Pri = 2, Params = new object[] { "XsltSettings.xml", "XsltSettings3.xsl", true, false })]
        [Variation(id = 13, Desc = "Test the stylesheet with no script and document function with Default Settings", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings4.xsl", false, false })]
        [Variation(id = 14, Desc = "Test the stylesheet with no script and document function with TrustedXslt", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings4.xsl", true, true })]
        [Variation(id = 15, Desc = "Test 1 with Default settings, should fail", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings1.xsl", false, true, false, false })]
        [Variation(id = 16, Desc = "Test 2 with EnableScript override, should work", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings1.xsl", false, false, false, true })]
        [Variation(id = 17, Desc = "Test 5 with Default settings override, should fail", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings2.xsl", true, false, false, false })]
        [Variation(id = 18, Desc = "Test 6 with EnableDocumentFunction override, should work", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings2.xsl", false, false, true, false })]
        [Variation(id = 19, Desc = "Test 9 with Default settings override, should fail", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings3.xsl", true, true, false, false })]
        [Variation(id = 20, Desc = "Test 10 with TrustedXslt override, should work", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings3.xsl", false, false, true, true })]
        /*
         * enable all disable scripting
         * enable all disable document()
         * enable all disable none
         * enable all disable all

         */
        public int XsltSettings1()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());

            // In Proc Skip
            // XsltSettings - Debug (1-20)
            // XsltSettings - Retail (1,4,9,11,15,16,19,20)
            if (_isInProc)
            {
                if (_debug)
                    return TEST_SKIPPED;
                else
                {
                    switch (CurVariation.id)
                    {
                        case 1:
                        case 4:
                        case 9:
                        case 11:
                        case 15:
                        case 16:
                        case 19:
                        case 20:
                            return TEST_SKIPPED;
                        //                  default:
                        //just continue;
                    }
                }
            }

            XsltSettings xs = new XsltSettings((bool)CurVariation.Params[2], (bool)CurVariation.Params[3]);
            xsl.Load(XslFile, xs, new XmlUrlResolver());

            if (CurVariation.id >= 15 && CurVariation.id <= 20)
            {
                xs.EnableDocumentFunction = (bool)CurVariation.Params[4];
                xs.EnableScript = (bool)CurVariation.Params[5];
                xsl.Load(XslFile, xs, new XmlUrlResolver());
            }

            try
            {
                StringWriter sw = Transform();

                switch (CurVariation.id)
                {
                    case 1:
                    case 4:
                    case 16:
                        return VerifyResult(sw.ToString(), "30", "Unexpected result, expected 30");

                    case 5:
                    case 8:
                    case 18:
                        return VerifyResult(sw.ToString(), "7", "Unexpected result, expected 7");

                    case 9:
                    case 20:
                        return VerifyResult(sw.ToString(), "17", "Unexpected result, expected 17");

                    case 13:
                    case 14:
                        return VerifyResult(sw.ToString(), "PASS", "Unexpected result, expected PASS");

                    default:
                        return TEST_FAIL;
                }
            }
            catch (XsltException ex)
            {
                switch (CurVariation.id)
                {
                    case 2:
                    case 3:
                    case 6:
                    case 7:
                    case 10:
                    case 11:
                    case 12:
                    case 15:
                    case 17:
                    case 19:
                        CError.WriteLineIgnore(ex.ToString());
                        CError.WriteLine(ex.Message);
                        return TEST_PASS;

                    default:
                        return TEST_FAIL;
                }
            }
        }

        [Variation(id = 21, Desc = "Disable Scripting and load a stylesheet which includes another sytlesheet with script block", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings5.xsl", false, false })]
        [Variation(id = 22, Desc = "Disable Scripting and load a stylesheet which imports XSLT which includes another XSLT with script block", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings6.xsl", false, false })]
        [Variation(id = 23, Desc = "Disable Scripting and load a stylesheet which has an entity expanding to a script block", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings7.xsl", false, false })]
        [Variation(id = 24, Desc = "Disable Scripting and load a stylesheet with multiple script blocks with different languages", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings8.xsl", false, false })]
        public int XsltSettings2()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());

            if (_isInProc)
                if (_debug)
                    return TEST_SKIPPED;

            XsltSettings xs = new XsltSettings((bool)CurVariation.Params[2], (bool)CurVariation.Params[3]);
            XPathDocument doc = new XPathDocument(XslFile);
            xsl.Load(doc, xs, new XmlUrlResolver());

            try
            {
                StringWriter sw = Transform();
                CError.WriteLine("Execution of the scripts was allowed even when XsltSettings.EnableScript is false");
                return TEST_FAIL;
            }
            catch (XsltException ex)
            {
                CError.WriteLineIgnore(ex.ToString());
                return TEST_PASS;
            }
        }

        [Variation(id = 25, Desc = "Disable DocumentFunction and Malicious stylesheet has document(url) opening a URL to an external system", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings9.xsl", false, false })]
        [Variation(id = 26, Desc = "Disable DocumentFunction and Malicious stylesheet has document(nodeset) opens union of all URLs referenced", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings10.xsl", false, false })]
        [Variation(id = 27, Desc = "Disable DocumentFunction and Malicious stylesheet has document(url, nodeset) nodeset is a base URL to 1st arg", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings11.xsl", false, false })]
        [Variation(id = 28, Desc = "Disable DocumentFunction and Malicious stylesheet has document(nodeset, nodeset) 2nd arg is a base URL to 1st arg", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings12.xsl", false, false })]
        [Variation(id = 29, Desc = "Disable DocumentFunction and Malicious stylesheet has document(''), no threat but just to verify if its considered", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings13.xsl", false, false })]
        [Variation(id = 30, Desc = "Disable DocumentFunction and Stylesheet includes another stylesheet with document() function", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings14.xsl", false, false })]
        [Variation(id = 31, Desc = "Disable DocumentFunction and Stylesheet has an entity reference to doc(), ENTITY s document('foo.xml')", Pri = 1, Params = new object[] { "XsltSettings.xml", "XsltSettings15.xsl", false, false })]
        public int XsltSettings3()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());

            if (_isInProc)
                if (_debug)
                    return TEST_SKIPPED;

            XsltSettings xs = new XsltSettings((bool)CurVariation.Params[2], (bool)CurVariation.Params[3]);
            XPathDocument doc = new XPathDocument(XslFile);
            xsl.Load(doc, xs, new XmlUrlResolver());

            try
            {
                StringWriter sw = Transform();
                CError.WriteLine("Execution of the document function was allowed even when XsltSettings.EnableDocumentFunction is false");
                return TEST_FAIL;
            }
            catch (XsltException ex)
            {
                CError.WriteLineIgnore(ex.ToString());
                return TEST_PASS;
            }
        }
    }
}