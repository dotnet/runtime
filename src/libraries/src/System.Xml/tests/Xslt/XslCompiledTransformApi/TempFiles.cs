using OLEDB.Test.ModuleCore;
using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

namespace XsltApiV2
{
    [TestCase(Name = "TemporaryFiles", Desc = "This testcase tests the Temporary Files property on XslCompiledTransform")]
    public class TempFiles : XsltApiTestCaseBase
    {
        private XslCompiledTransform xsl = null;
        private string XmlFile = string.Empty;
        private string XslFile = string.Empty;

        private int Init(string xmlFile, string xslFile, bool enableDebug)
        {
            if (enableDebug)
                xsl = new XslCompiledTransform(true);
            else
                xsl = new XslCompiledTransform(false);

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

        private int VerifyResult(bool expression, string message)
        {
            if (CError.Compare(expression, message))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation(id = 1, Desc = "Default value of TemporaryFiles before load, expected null", Pri = 0)]
        public int TempFiles1()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            return VerifyResult(xslt.TemporaryFiles, null, "Default value of TemporaryFiles must be null");
        }

        [Variation(id = 2, Desc = "TemporaryFiles after load in Retail Mode with no script block, expected count = 0", Pri = 0, Params = new object[] { "books.xml", "NoScripts.xsl", false })]
        [Variation(id = 3, Desc = "TemporaryFiles after load in Debug Mode with no script block, expected count = 0", Pri = 0, Params = new object[] { "books.xml", "NoScripts.xsl", true })]
        public int TempFiles2()
        {
            if (_isInProc && CurVariation.id == 3)
                return TEST_SKIPPED;

            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString(), (bool)CurVariation.Params[2]);
            xsl.Load(XslFile);

            return VerifyResult(xsl.TemporaryFiles.Count, 0, "There should be no temp files generated, when there are no script block");
        }

        [Variation(id = 4, Desc = "TemporaryFiles after load in Retail Mode with script block and EnableScript", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", false })]
        public int TempFiles3()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString(), (bool)CurVariation.Params[2]);

            if (_isInProc)
                return TEST_SKIPPED;

            xsl.Load(XslFile, new XsltSettings(false, true), new XmlUrlResolver());

            //In retail mode temporary files are not generated if a script block exist
            return VerifyResult(xsl.TemporaryFiles.Count, 0, "TemporaryFiles generated in retail mode");
        }

        [Variation(id = 5, Desc = "TemporaryFiles after load in Debug Mode with script block and EnableScript", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
        public int TempFiles3AndHalf()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString(), (bool)CurVariation.Params[2]);

            if (_isInProc)
                return TEST_SKIPPED;

            xsl.Load(XslFile, new XsltSettings(false, true), new XmlUrlResolver());

            //In debug mode temporary files are generated if a script block exist
            //An extra .pdb info is generated if debugging is enabled
            return VerifyResult(xsl.TemporaryFiles.Count > 0, "TemporaryFiles must be generated when there is a script block");
        }

        [Variation(id = 6, Desc = "TemporaryFiles after load in retail mode with script block and default settings", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", false })]
        [Variation(id = 7, Desc = "TemporaryFiles after load in debug mode with script block and default settings", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
        public int TempFiles4()
        {
            if (_isInProc && CurVariation.id == 7)
                return TEST_SKIPPED;

            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString(), (bool)CurVariation.Params[2]);
            xsl.Load(XslFile, XsltSettings.Default, new XmlUrlResolver());

            return VerifyResult(xsl.TemporaryFiles.Count, 0, "TemporaryFiles must not be generated, when XsltSettings is None");
        }

        [Variation(id = 8, Desc = "TemporaryFiles after load in retail mode with script block and EnableDocumentFunction", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", false })]
        [Variation(id = 9, Desc = "TemporaryFiles after load in debug mode with script block and EnableDocumentFunction", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
        public int TempFiles5()
        {
            if (_isInProc && CurVariation.id == 9)
                return TEST_SKIPPED;

            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString(), (bool)CurVariation.Params[2]);
            xsl.Load(XslFile, new XsltSettings(true, false), new XmlUrlResolver());

            return VerifyResult(xsl.TemporaryFiles.Count, 0, "TemporaryFiles must not be generated, when XsltSettings is EnableDocumentFunction alone");
        }

        [Variation(id = 10, Desc = "Verify the existence of TemporaryFiles after load in debug mode with script block", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
        public int TempFiles6()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString(), (bool)CurVariation.Params[2]);

            if (_isInProc)
                return TEST_SKIPPED;

            xsl.Load(XslFile, new XsltSettings(false, true), new XmlUrlResolver());

            foreach (string filename in xsl.TemporaryFiles)
            {
                CError.WriteLineIgnore(filename);
                CError.Compare(File.Exists(filename), "Temporary file '" + filename + "' doesn't exist");
            }
            return TEST_PASS;
        }

        [Variation(id = 11, Desc = "Verify if the user can delete the TemporaryFiles after load in debug mode with script block", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
        public int TempFiles7()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString(), (bool)CurVariation.Params[2]);

            if (_isInProc)
                return TEST_SKIPPED;

            xsl.Load(XslFile, new XsltSettings(false, true), new XmlUrlResolver());

            TempFileCollection tempFiles = xsl.TemporaryFiles;

            int fileCount = tempFiles.Count;
            tempFiles.Delete();
            fileCount = tempFiles.Count;

            return VerifyResult(fileCount, 0, "Temp files could not be deleted");
        }

        [Variation(id = 12, Desc = "Verify if the user can rename the TemporaryFiles after load in debug mode with script block", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
        public int TempFiles8()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString(), (bool)CurVariation.Params[2]);

            if (_isInProc)
                return TEST_SKIPPED;

            xsl.Load(XslFile, new XsltSettings(false, true), new XmlUrlResolver());

            TempFileCollection tempFiles = xsl.TemporaryFiles;

            string newfilename = string.Empty;
            foreach (string filename in tempFiles)
            {
                string tempdir = filename.Substring(0, filename.LastIndexOf("\\") + 1);
                newfilename = tempdir + "new" + filename.Substring(filename.LastIndexOf("\\") + 1);
                File.Move(filename, newfilename);
            }

            return TEST_PASS;
        }

        [Variation(id = 13, Desc = "Verify if the necessary files are generated after load in debug mode with script block", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
        public int TempFiles9()
        {
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString(), (bool)CurVariation.Params[2]);

            if (_isInProc)
                return TEST_SKIPPED;

            xsl.Load(XslFile, new XsltSettings(false, true), new XmlUrlResolver());

            TempFileCollection tempFiles = xsl.TemporaryFiles;
            string filelist = string.Empty;

            foreach (string filename in tempFiles)
            {
                filelist += filename.Substring(filename.LastIndexOf("\\") + 1) + ";";
            }

            CError.WriteLine("Verifying the existence of .dll, .pdb, .err, .out, .cmdline, .cs and .tmp");
            CError.Compare(filelist.IndexOf(".dll") > 0, "Script DLL tempfile is not generated");
            CError.Compare(filelist.IndexOf(".pdb") > 0, "Debug info tempfile is not generated");
            CError.Compare(filelist.IndexOf(".err") > 0, "Error tempfile is not generated");
            CError.Compare(filelist.IndexOf(".out") > 0, "Output tempfile is not generated");
            CError.Compare(filelist.IndexOf(".cmdline") > 0, "Command Line tempfile is not generated");
            CError.Compare(filelist.IndexOf(".cs") > 0, "CSharp tempfile is not generated");
            CError.Compare(filelist.IndexOf(".tmp") > 0, "Tempfile is not generated");

            return TEST_PASS;
        }

        [Variation(id = 14, Desc = "TemporaryFiles after unsuccessful load of an invalid stylesheet in debug mode with script block", Pri = 2, Params = new object[] { "books.xml", "Invalid.xsl", true })]
        public int TempFiles10()
        {
            TempFileCollection tempFiles = null;
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString(), (bool)CurVariation.Params[2]);

            try
            {
                xsl.Load(XslFile, new XsltSettings(false, true), new XmlUrlResolver());
            }
            catch (XsltException e)
            {
                CError.WriteLine(e);
                tempFiles = xsl.TemporaryFiles;
            }

            return VerifyResult(tempFiles, null, "Temporary files must not be generated");
        }

        [Variation(id = 15, Desc = "TemporaryFiles after unsuccessful load of a valid stylesheet in debug mode with a missing function in the script block", Pri = 2, Params = new object[] { "books.xml", "InvalidFn.xsl", true })]
        public int TempFiles11()
        {
            TempFileCollection tempFiles = null;
            Init(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString(), (bool)CurVariation.Params[2]);

            if (_isInProc)
                return TEST_SKIPPED;

            try
            {
                xsl.Load(XslFile, new XsltSettings(false, true), new XmlUrlResolver());
            }
            catch (XsltException e)
            {
                CError.WriteLine(e);
                tempFiles = xsl.TemporaryFiles;
            }

            return VerifyResult(tempFiles != null, "Temporary files should have been generated even when Load() is unsuccessful");
        }

        [Variation(Desc = "Load File from a drive c:", Pri = 2)]
        public int TempFiles12()
        {
            if (_isInProc)
                return TEST_SKIPPED;

            string childFile = Path.Combine(Directory.GetCurrentDirectory(), "child.xsl");

            string parentString = "<?xml version=\"1.0\"?>"
                + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
                + "<xsl:import href=\"" + childFile + "\"/>"
                + "<xsl:output method=\"xml\" omit-xml-declaration=\"yes\" indent=\"yes\"/>"
                + "<xsl:template match=\"book[@style='autobiography']\">"
                + "<SPAN style=\"color=blue\">From B<xsl:value-of select=\"name()\"/> : <xsl:value-of select=\"title\"/>"
                + "</SPAN><br/>"
                + "<xsl:apply-templates />"
                + "</xsl:template>"
                + "<xsl:template match=\"text()\" >"
                + "</xsl:template>"
                + "</xsl:stylesheet>";

            string childString = "<?xml version=\"1.0\"?>"
                + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
                + "<xsl:output method=\"xml\" omit-xml-declaration=\"yes\" indent=\"yes\"/>"
                + "<xsl:template match=\"book[@style='autobiography']\">"
                + "<SPAN style=\"color=blue\">From B<xsl:value-of select=\"name()\"/> : <xsl:value-of select=\"title\"/>"
                + "</SPAN><br/>"
                + "<xsl:apply-templates />"
                + "</xsl:template>"
                + "<xsl:template match=\"text()\" >"
                + "</xsl:template>"
                + "</xsl:stylesheet>";

            try
            {
                // create a xsl file in current directory on some drive, this is included in XSL above
                StreamWriter file = new StreamWriter(childFile);
                file.WriteLine(childString);
                file.Close();
                StreamWriter parentFile = new StreamWriter("parent.xsl");
                parentFile.WriteLine(parentString);
                parentFile.Close();
            }
            catch (Exception e)
            {
                CError.WriteLine(e);
                return TEST_FAIL;
            }

            try
            {
                // Now let's load the parent xsl file
                xsl.Load("parent.xsl", new XsltSettings(false, true), new XmlUrlResolver());
            }
            catch (XsltException e)
            {
                CError.WriteLine(e);
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation(Desc = "Bug 469775 - XSLT V2 : Exception thrown if xsl:preserve-space/xsl:strip-space is used and input document contains entities", Pri = 2)]
        public int TempFiles13()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("bug469775.xsl"));
                xslt.Transform(new XmlTextReader(FullFilePath("bug469775.xml")), (XsltArgumentList)null, CError.Out);
            }
            catch (System.Xml.XmlException)
            {
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation(Desc = "Bug 469770 - XslCompiledTransform failed to load embedded stylesheets when prefixes are defined outside of xsl:stylesheet element", Pri = 2)]
        public int TempFiles14()
        {
            try
            {
                string xsl = "<root xmlns:ns=\"testing\">"
                    + "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\">"
                    + "<xsl:template match=\"/\">"
                    + "<xsl:value-of select=\"ns:test\" />"
                    + "</xsl:template>"
                    + "</xsl:stylesheet>"
                    + "</root>";

                XmlReader r = XmlReader.Create(new StringReader(xsl));
                while (r.NodeType != XmlNodeType.Element || r.LocalName != "stylesheet")
                {
                    if (!r.Read())
                    {
                        CError.WriteLine("There is no 'stylesheet' element in the file");
                        return TEST_FAIL;
                    }
                }

                XslCompiledTransform t = new XslCompiledTransform();
                t.Load(r);
            }
            catch (XsltException exception)
            {
                CError.WriteLine("The following exception should not have been thrown");
                CError.WriteLine(exception.ToString());
                return TEST_FAIL;
            }

            return TEST_PASS;
        }

        [Variation(Desc = "Bug 482971 - XslCompiledTransform cannot output numeric character reference after long output", Pri = 2)]
        public int TempFiles15()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("bug482971.xsl"));
                xslt.Transform(FullFilePath("bug482971.xml"), "out.txt");
            }
            catch (Exception exception)
            {
                CError.WriteLine("No exception should not have been thrown");
                CError.WriteLine(exception.ToString());
                return TEST_FAIL;
            }
            return TEST_PASS;
        }
    }
}