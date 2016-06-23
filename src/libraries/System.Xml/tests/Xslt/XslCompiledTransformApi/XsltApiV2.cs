using OLEDB.Test.ModuleCore;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Xml;
using System.Xml.XmlDiff;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;

namespace XsltApiV2
{
    public enum OutputType
    {
        URI, Stream, Writer, TextWriter
    }

    public enum XslInputType
    {
        Reader, URI, Navigator
    }

    public enum XmlInputType
    {
        Reader, URI, Navigator
    }

    public enum NavType
    {
        XmlDocument, DataDocument, XPathDocument, Unknown
    }

    public enum ReaderType
    {
        XmlTextReader, XmlNodeReader, XmlValidatingReader, XsltReader, Unknown
    }

    ////////////////////////////////////////////////////////////////
    // Module declaration for LTM usage
    //
    ////////////////////////////////////////////////////////////////
    [TestModule(Name = "XsltApiV2", Desc = "XslCompiledTransform API Tests", Pri = 1)]
    public class XslCompiledTransformModule : CTestModule
    {
        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);

            // These steps use to be part of javascript files and these were copied and executed as test setup step.
            // I belive this to be a much better way of accomplishing the same task.
            // Logic from CreateApiTestFiles.js
            string sourceFile = Path.Combine(FilePathUtil.GetTestDataPath(), @"XsltApiV2\xmlResolver_document_function.xml");
            string targetFile = @"c:\temp\xmlResolver_document_function.xml";
            if (!Directory.Exists(@"c:\temp"))
                Directory.CreateDirectory(@"c:\temp");
            if (!File.Exists(targetFile))
                File.Copy(sourceFile, targetFile, true);

            // Logic from CreateStrasse.js
            using (XmlWriter writer = XmlWriter.Create("Stra\u00DFe.xml"))
            {
                writer.WriteComment(" ");
            }

            return ret;
        }

        public override int Terminate(object o)
        {
            return base.Init(o);
        }
    }

    ////////////////////////////////////////////////////////////////
    // Base class for test cases
    //
    ////////////////////////////////////////////////////////////////
    public class XsltApiTestCaseBase : CTestCase
    {
        // Generic data for all derived test cases
        public String szXslNS = "http://www.w3.org/1999/XSL/Transform";

        public String szDefaultNS = "urn:my-object";
        public String szEmpty = "";
        public String szInvalid = "*?%(){}[]&!@#$";
        public String szLongString = "ThisIsAVeryLongStringToBeStoredAsAVariableToDetermineHowLargeThisBufferForAVariableNameCanBeAndStillFunctionAsExpected";
        public String szLongNS = "http://www.microsoft.com/this/is/a/very/long/namespace/uri/to/do/the/api/testing/for/xslt/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/";
        public String[] szWhiteSpace = { "  ", "\n", "\t", "\r", "\t\n  \r\t" };
        public String szSimple = "myArg";

        // Variables from init string
        private string _strPath;                           // Path of the data files

        private string _httpPath;                          // Http Path of the data files
        private XslInputType _nInputXsl;                         // reader, url, or navigator
        private XmlInputType _nInputXml = XmlInputType.Reader;   // reader, url, or navigator
        private bool _fTrace;                            // Should we write out the results of the transform?
        private OutputType _nOutput;                           // Type of XSL Transform to do
        private NavType _navType;                           // Data document type
        private ReaderType _readerType;                        // Reader type

        // Other global variables
        protected string _strOutFile = "out.xml";        // File to create when using write transforms

        protected XslCompiledTransform xslt;                           // Main XslCompiledTransform instance
        protected XmlReader xrXSLT;                         // for READER transforms
        protected XsltArgumentList m_xsltArg;                      // For XsltArgumentList tests
        public object retObj;

        protected bool _isInProc;                          // Is the current test run in proc or /Host None?
        protected string _standardTests;

        public XslInputType MyXslInputType()
        {
            return _nInputXsl;
        }

        public XmlInputType MyXmlInputType()
        {
            return _nInputXml;
        }

        public OutputType MyOutputType()
        {
            return _nOutput;
        }

        public NavType MyDocType()
        {
            return _navType;
        }

        public ReaderType MyReaderType()
        {
            return _readerType;
        }

        public OutputType GetOutputType(String s)
        {
            if (s.EndsWith(",URI"))
                return OutputType.URI;
            else if (s.EndsWith(",STREAM"))
                return OutputType.Stream;
            else if (s.EndsWith(",WRITER"))
                return OutputType.Writer;
            else if (s.EndsWith(",TEXTWRITER"))
                return OutputType.TextWriter;
            else return OutputType.Stream; //default
        }

        public XmlUrlResolver GetDefaultCredResolver()
        {
            XmlUrlResolver myDefaultCredResolver = new XmlUrlResolver();
            myDefaultCredResolver.Credentials = CredentialCache.DefaultCredentials;

            return myDefaultCredResolver;
        }

        public XslInputType GetXslInputType(String s)
        {
            if (s.StartsWith("READER,"))
                return XslInputType.Reader;
            else if (s.StartsWith("URI,"))
                return XslInputType.URI;
            else if (s.StartsWith("NAVIGATOR,"))
                return XslInputType.Navigator;
            else
                return XslInputType.URI;
        }

        public NavType GetDocType(String s)
        {
            switch (s.ToUpper())
            {
                case "XPATHDOCUMENT":
                    return NavType.XPathDocument;

                case "XMLDOCUMENT":
                    return NavType.XmlDocument;

                case "DATADOCUMENT":
                    return NavType.DataDocument;

                default: // NavType Type not important, using default
                    return NavType.XPathDocument;
            }
        }

        public ReaderType GetReaderType(String s)
        {
            //XmlTextReader, XmlNodeReader, XmlValidatingReader, XsltReader

            switch (s.ToUpper())
            {
                case "XMLTEXTREADER":
                    return ReaderType.XmlTextReader;

                case "XMLNODEREADER":
                    return ReaderType.XmlNodeReader;

                case "XMLVALIDATINGREADER":
                    return ReaderType.XmlValidatingReader;

                case "XSLTREADER":
                    return ReaderType.XsltReader;

                default: // ReaderType Type not important, using default
                    return ReaderType.XmlValidatingReader;
            }
        }

        public String InitStringValue(String str)
        {
            object obj = CModInfo.GetOption(str);

            if (obj == null)
            {
                return String.Empty;
            }
            return obj.ToString();
        }

        public override int Init(object objParam)
        {
            // Get input and transform type from attribute
            string szDesc = GetDescription().ToUpper(CultureInfo.InvariantCulture);
            _nInputXsl = GetXslInputType(szDesc.ToUpper(CultureInfo.InvariantCulture));
            _nOutput = GetOutputType(szDesc.ToUpper(CultureInfo.InvariantCulture));

            // Get parameter info from runtime variables passed to LTM
            _fTrace = (InitStringValue("trace").ToUpper(CultureInfo.InvariantCulture) == "TRUE");
            _navType = GetDocType(InitStringValue("doctype"));
            _readerType = GetReaderType(InitStringValue("readertype"));

            //This is a temporary fix to restore the baselines. Refer to Test bug #
            _strPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"XsltApiV2\");
            _httpPath = FilePathUtil.GetHttpTestDataPath() + @"/XsltApiV2/";
            _standardTests = FilePathUtil.GetHttpStandardPath() + @"/xslt10/Current/";

            // initialize whether this run is in proc or not
            string host = CModInfo.Host;
            if (null != host &&
                 !host.ToUpper().Equals("NONE"))
            {
                _isInProc = true;
            }

            return TEST_PASS;
        }

        public String FullFilePath(String szFile)
        {
            if (szFile == null || szFile == String.Empty)
                return szFile;
            if (szFile.Length > 5)
            {
                if (szFile.Substring(0, 5) != "http:")
                    szFile = _strPath + szFile;
            }
            return szFile;
        }

        public String FullHttpPath(String szFile)
        {
            if (szFile == null || szFile == String.Empty)
                return szFile;
            szFile = _httpPath + szFile;
            return szFile;
        }

        protected string _expectedErrorCode;

        // --------------------------------------------------------------------------------------------------------------
        //  SetExpectedError
        //  -------------------------------------------------------------------------------------------------------------
        public void SetExpectedError(string errorCode)
        {
            _expectedErrorCode = errorCode;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  CheckExpectedError
        //  -------------------------------------------------------------------------------------------------------------
        public int CheckExpectedError(Exception ex, string assembly)
        {
            CExceptionHandler handler = new CExceptionHandler(_strPath + "exceptions.xml", assembly);
            bool result = handler.VerifyException(ex);
            if (handler.res != _expectedErrorCode)
            {
                CError.WriteLine("Expected Exception : {0}", _expectedErrorCode);
                CError.WriteLine("Actual Exception : {0}", handler.res);
                return TEST_FAIL;
            }
            if (!result)
            {
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  CheckExpectedError
        //  -------------------------------------------------------------------------------------------------------------
        public int CheckExpectedError(Exception ex, string assembly, string res, string[] strParams)
        {
            CExceptionHandler handler = new CExceptionHandler(_strPath + "exceptions.xml", assembly);
            if (!handler.VerifyException(ex, res, strParams))
            {
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  LoadXML
        //  -------------------------------------------------------------------------------------------------------------
        public IXPathNavigable LoadXML(String strFileLocation, NavType _navType)
        {
            switch (_navType)
            {
                case NavType.XmlDocument:
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(strFileLocation);
                    return (IXPathNavigable)xmlDocument;

                case NavType.DataDocument:
                    XmlDataDocument xmlDataDocument = new XmlDataDocument();
                    xmlDataDocument.Load(strFileLocation);
                    return (IXPathNavigable)xmlDataDocument;

                case NavType.XPathDocument:
                    XPathDocument xPathDocument = new XPathDocument(strFileLocation);
                    return (IXPathNavigable)xPathDocument;

                default:
                    return null;
            }
        }

        // --------------------------------------------------------------------------------------------------------------
        //  LoadXSL
        //  -------------------------------------------------------------------------------------------------------------
        public int LoadXSL(String _strXslFile)
        {
            return LoadXSL(_strXslFile, _nInputXsl, new XmlUrlResolver());
        }

        public int LoadXSL(String _strXslFile, XmlResolver xr)
        {
            return LoadXSL(_strXslFile, _nInputXsl, xr);
        }

        public int LoadXSL(String _strXslFile, XslInputType xslInputType, XmlResolver xr)
        {
            _strXslFile = FullFilePath(_strXslFile);
            xslt = new XslCompiledTransform();
            XmlReaderSettings xrs = null;

            switch (xslInputType)
            {
                case XslInputType.URI:
                /*                  CError.WriteLineIgnore("Loading style sheet as URI {0}", _strXslFile);
                                    xslt.Load(_strXslFile, XsltSettings.TrustedXslt, xr);
                                    break;
                 */
                case XslInputType.Reader:
                    switch (_readerType)
                    {
                        case ReaderType.XmlTextReader:
                            XmlTextReader trTemp = new XmlTextReader(_strXslFile);
                            try
                            {
                                CError.WriteLineIgnore("Loading style sheet as XmlTextReader " + _strXslFile);
                                xslt.Load(trTemp, XsltSettings.TrustedXslt, xr);
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (trTemp != null)
                                    trTemp.Close();
                            }
                            break;

                        case ReaderType.XmlNodeReader:
                            XmlDocument docTemp = new XmlDocument();
                            docTemp.Load(_strXslFile);
                            XmlNodeReader nrTemp = new XmlNodeReader(docTemp);
                            try
                            {
                                CError.WriteLineIgnore("Loading style sheet as XmlNodeReader " + _strXslFile);
                                xslt.Load(nrTemp);
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (nrTemp != null)
                                    nrTemp.Close();
                            }
                            break;

                        case ReaderType.XmlValidatingReader:
                        default:
                            xrs = new XmlReaderSettings();
                            xrs.ProhibitDtd = false;
                            XmlReader xvr = XmlReader.Create(_strXslFile, xrs);

                            try
                            {
                                CError.WriteLineIgnore("Loading style sheet as XmlValidatingReader " + _strXslFile);
                                xslt.Load(xvr, XsltSettings.TrustedXslt, xr);
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (xvr != null)
                                    xvr.Close();
                            }
                            break;
                    }
                    break;

                case XslInputType.Navigator:
                    xrs = new XmlReaderSettings();
                    xrs.ValidationType = ValidationType.None;
                    xrs.ProhibitDtd = false;
                    XmlReader xrLoad = XmlReader.Create(_strXslFile, xrs);

                    XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
                    xrLoad.Close();
                    CError.WriteLineIgnore("Loading style sheet as Navigator " + _strXslFile);
                    xslt.Load(xdTemp, XsltSettings.TrustedXslt, xr);
                    break;
            }
            return 1;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  LoadXSL_Resolver
        //  -------------------------------------------------------------------------------------------------------------
        public int LoadXSL_Resolver(String _strXslFile, XmlResolver xr)
        {
            _strXslFile = FullFilePath(_strXslFile);
            xslt = new XslCompiledTransform();
            XmlReaderSettings xrs = null;
            switch (_nInputXsl)
            {
                case XslInputType.URI:
                    CError.WriteLineIgnore("Loading style sheet as URI " + _strXslFile);
                    xslt.Load(_strXslFile, XsltSettings.TrustedXslt, xr);
                    break;

                case XslInputType.Reader:
                    switch (_readerType)
                    {
                        case ReaderType.XmlTextReader:
                            XmlTextReader trTemp = new XmlTextReader(_strXslFile);
                            try
                            {
                                CError.WriteLineIgnore("Loading style sheet as XmlTextReader " + _strXslFile);
                                xslt.Load(trTemp, XsltSettings.TrustedXslt, xr);
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (trTemp != null)
                                    trTemp.Close();
                            }
                            break;

                        case ReaderType.XmlNodeReader:
                            XmlDocument docTemp = new XmlDocument();
                            docTemp.Load(_strXslFile);
                            XmlNodeReader nrTemp = new XmlNodeReader(docTemp);
                            try
                            {
                                CError.WriteLineIgnore("Loading style sheet as XmlNodeReader " + _strXslFile);
                                xslt.Load(nrTemp, XsltSettings.TrustedXslt, xr);
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (nrTemp != null)
                                    nrTemp.Close();
                            }
                            break;

                        case ReaderType.XmlValidatingReader:
                        default:
                            xrs = new XmlReaderSettings();
                            xrs.ProhibitDtd = false;
                            XmlReader vrTemp = XmlReader.Create(_strXslFile, xrs);
                            try
                            {
                                CError.WriteLineIgnore("Loading style sheet as XmlValidatingReader " + _strXslFile);
                                xslt.Load(vrTemp, XsltSettings.TrustedXslt, xr);
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (vrTemp != null)
                                    vrTemp.Close();
                            }
                            break;
                    }
                    break;

                case XslInputType.Navigator:
                    xrs = new XmlReaderSettings();
                    xrs.ProhibitDtd = false;
                    XmlReader xrLoad = XmlReader.Create(_strXslFile, xrs);

                    XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
                    xrLoad.Close();
                    CError.WriteLineIgnore("Loading style sheet as Navigator " + _strXslFile);
                    xslt.Load(xdTemp, XsltSettings.TrustedXslt, xr);
                    break;
            }
            return 1;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  LoadXSL_Resolver_Evidence
        //  -------------------------------------------------------------------------------------------------------------
        public int LoadXSL_Resolver_Evidence(String _strXslFile, XmlResolver xr, Evidence e)
        {
            _strXslFile = FullFilePath(_strXslFile);
            xslt = new XslCompiledTransform();

            switch (_nInputXsl)
            {
                case XslInputType.Reader:
                    switch (_readerType)
                    {
                        case ReaderType.XmlTextReader:
                            XmlReader trTemp = XmlReader.Create(_strXslFile);
                            try
                            {
                                CError.WriteLineIgnore("Loading style sheet as XmlTextReader " + _strXslFile);
                                //xslt.Load(trTemp, xr, e); //Evidence is not supported on V2 XSLT Load
                                xslt.Load(trTemp, XsltSettings.TrustedXslt, xr);
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (trTemp != null)
                                    trTemp.Close();
                            }
                            break;

                        case ReaderType.XmlNodeReader:
                            XmlDocument docTemp = new XmlDocument();
                            docTemp.Load(_strXslFile);
                            XmlNodeReader nrTemp = new XmlNodeReader(docTemp);
                            try
                            {
                                CError.WriteLineIgnore("Loading style sheet as XmlNodeReader " + _strXslFile);
                                //xslt.Load(nrTemp, xr, e); Evidence is not supported in V2 XSLT Load
                                xslt.Load(nrTemp, XsltSettings.TrustedXslt, xr);
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (nrTemp != null)
                                    nrTemp.Close();
                            }
                            break;

                        case ReaderType.XmlValidatingReader:
                        default:
                            XmlReaderSettings xrs = new XmlReaderSettings();
                            xrs.ProhibitDtd = false;
                            XmlReader vrTemp = null;
                            try
                            {
                                vrTemp = XmlReader.Create(_strXslFile, xrs);
                                CError.WriteLineIgnore("Loading style sheet as XmlValidatingReader " + _strXslFile);
                                //xslt.Load(vrTemp, xr, e); Evidence is not supported in V2 XSLT Load
                                xslt.Load(vrTemp, XsltSettings.TrustedXslt, xr);
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (vrTemp != null)
                                    vrTemp.Close();
                            }
                            break;
                    }
                    break;

                case XslInputType.Navigator:
                    XmlReader xrLoad = XmlReader.Create(_strXslFile);
                    XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
                    xrLoad.Close();
                    CError.WriteLineIgnore("Loading style sheet as Navigator " + _strXslFile);
                    xslt.Load(xdTemp.CreateNavigator(), XsltSettings.TrustedXslt, xr);
                    break;
            }
            return 1;
        }

        //VerifyResult
        public int VerifyResult(string expectedValue)
        {
            XmlDiff xmldiff = new XmlDiff();
            xmldiff.Option = XmlDiffOption.InfosetComparison | XmlDiffOption.IgnoreEmptyElement;

            StreamReader sr = new StreamReader("out.xml");
            string actualValue = sr.ReadToEnd();
            sr.Close();

            //Output the expected and actual values
            CError.WriteLine("Expected : " + expectedValue);
            CError.WriteLine("Actual : " + actualValue);

            //Load into XmlTextReaders
            XmlTextReader tr1 = new XmlTextReader("out.xml");
            XmlTextReader tr2 = new XmlTextReader(new StringReader(expectedValue));

            bool bResult = xmldiff.Compare(tr1, tr2);

            //Close the readers
            tr1.Close();
            tr2.Close();

            if (bResult)
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        //VerifyResult which compares 2 arguments using XmlDiff.
        public int VerifyResult(string baseline, string outputFile)
        {
            bool bResult = false;
            FileStream fsExpected;

            baseline = FullFilePath(baseline);

            XmlDiff diff = new XmlDiff();
            diff.Option = XmlDiffOption.IgnoreEmptyElement | XmlDiffOption.IgnoreAttributeOrder | XmlDiffOption.InfosetComparison | XmlDiffOption.IgnoreWhitespace;
            XmlParserContext context = new XmlParserContext(new NameTable(), null, "", XmlSpace.None);

            fsExpected = new FileStream(baseline, FileMode.Open, FileAccess.Read, FileShare.Read);
            FileStream fsActual = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.Read);

            CError.WriteLine("Verifying o/p with baseline result {0}...", baseline);
            try
            {
                bResult = diff.Compare(new XmlTextReader(fsActual, XmlNodeType.Element, context), new XmlTextReader(fsExpected, XmlNodeType.Element, context));
            }
            catch (Exception e)
            {
                // TO DO: Write exception msgs in ignore tags
                CError.WriteLine(e);
            }
            finally
            {
                fsExpected.Close();
                fsActual.Close();
            }
            if (!bResult)
            {
                // Write out the actual and expected o/p
                CError.WriteLine("Expected o/p: ");
                using (StreamReader sr = new StreamReader(baseline))
                {
                    string baseLine = sr.ReadToEnd();
                    CError.WriteLine(baseLine);
                }
                CError.WriteLine("Actual o/p: ");
                using (StreamReader sr = new StreamReader(outputFile))
                {
                    string output = sr.ReadToEnd();
                    CError.WriteLine(output);
                }

                using (StreamWriter sw = new StreamWriter("diff.xml"))
                {
                    sw.WriteLine("<?xml-stylesheet href='diff.xsl' type='text/xsl'?>");
                    sw.WriteLine(diff.ToXml());
                }
            }

            if (bResult)
                return TEST_PASS;
            else
            {
                CError.WriteLine("**** Baseline mis-matched ****");
                return TEST_FAIL;
            }
        }

        // --------------------------------------------------------------------------------------------------------------
        //  Transform
        //  -------------------------------------------------------------------------------------------------------------

        public int Transform(String szXmlFile)
        {
            // Default value of errorCase is false
            return (Transform(szXmlFile, false));
        }

        public int Transform(String szXmlFile, bool errorCase)
        {
            szXmlFile = FullFilePath(szXmlFile);

            CError.WriteLineIgnore("Loading XML " + szXmlFile);
            IXPathNavigable xd = LoadXML(szXmlFile, _navType);

            CError.WriteLine("Executing transform");
            xrXSLT = null;
            Stream strmTemp = null;
            switch (_nOutput)
            {
                case OutputType.Stream:
                    try
                    {
                        strmTemp = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite);
                        xslt.Transform(xd, null, strmTemp);
                    }
                    catch (Exception ex)
                    {
                        throw (ex);
                    }
                    finally
                    {
                        if (strmTemp != null)
                            strmTemp.Close();
                    }
                    break;

                case OutputType.Writer:
                    XmlWriterSettings xws = new XmlWriterSettings();
                    xws.Encoding = Encoding.UTF8;
                    XmlWriter xw = null;
                    try
                    {
                        xw = XmlWriter.Create(_strOutFile, xws);
                        xw.WriteStartDocument();
                        xslt.Transform(xd, null, xw);
                    }
                    catch (Exception ex)
                    {
                        throw (ex);
                    }
                    finally
                    {
                        if (xw != null)
                            xw.Close();
                    }
                    break;

                case OutputType.TextWriter:
                    TextWriter tw = null;
                    try
                    {
                        tw = new StreamWriter(_strOutFile, false, Encoding.UTF8);
                        xslt.Transform(xd, null, tw);
                    }
                    catch (Exception ex)
                    {
                        throw (ex);
                    }
                    finally
                    {
                        if (tw != null)
                            tw.Close();
                    }
                    break;
            }
            return TEST_PASS;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  Transform_ArgList
        //  -------------------------------------------------------------------------------------------------------------

        public int Transform_ArgList(String szXmlFile)
        {
            // Default value of errorCase is false
            return (Transform_ArgList(szXmlFile, false));
        }

        public int Transform_ArgList(String szXmlFile, bool errorCase)
        {
            szXmlFile = FullFilePath(szXmlFile);

            CError.WriteLineIgnore("Loading XML " + szXmlFile);
            IXPathNavigable xd = LoadXML(szXmlFile, _navType);

            CError.WriteLine("Executing transform");
            xrXSLT = null;
            Stream strmTemp = null;
            switch (_nOutput)
            {
                case OutputType.Stream:
                    try
                    {
                        strmTemp = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite);
                        xslt.Transform(xd, m_xsltArg, strmTemp);
                    }
                    catch (Exception ex)
                    {
                        throw (ex);
                    }
                    finally
                    {
                        if (strmTemp != null)
                            strmTemp.Close();
                    }
                    break;

                case OutputType.Writer:
                    XmlWriter xw = null;
                    try
                    {
                        xw = new XmlTextWriter(_strOutFile, Encoding.UTF8);
                        xw.WriteStartDocument();
                        xslt.Transform(xd, m_xsltArg, xw);
                    }
                    catch (Exception ex)
                    {
                        throw (ex);
                    }
                    finally
                    {
                        if (xw != null)
                            xw.Close();
                    }
                    break;

                case OutputType.TextWriter:
                    TextWriter tw = null;
                    try
                    {
                        tw = new StreamWriter(_strOutFile, false, Encoding.UTF8);
                        xslt.Transform(xd, m_xsltArg, tw);
                    }
                    catch (Exception ex)
                    {
                        throw (ex);
                    }
                    finally
                    {
                        if (tw != null)
                        {
                            tw.Close();
                        }
                    }
                    break;
            }
            return TEST_PASS;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  TransformResolver
        //  -------------------------------------------------------------------------------------------------------------

        public int TransformResolver(String szXmlFile, XmlResolver xr)
        {
            // Default value of errorCase is false
            return (TransformResolver(szXmlFile, xr, false));
        }

        public int TransformResolver(String szXmlFile, XmlResolver xr, bool errorCase)
        {
            szXmlFile = FullFilePath(szXmlFile);

            CError.WriteLineIgnore("Loading XML " + szXmlFile);
            IXPathNavigable xd = LoadXML(szXmlFile, _navType);

            CError.WriteLine("Executing transform");
            xrXSLT = null;
            Stream strmTemp = null;

            switch (_nOutput)
            {
                case OutputType.Stream:
                    try
                    {
                        strmTemp = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite);
                        xslt.Transform(xd, null, strmTemp);
                    }
                    catch (Exception ex)
                    {
                        throw (ex);
                    }
                    finally
                    {
                        if (strmTemp != null)
                            strmTemp.Close();
                    }
                    break;

                case OutputType.Writer:
                    XmlWriter xw = null;
                    try
                    {
                        xw = new XmlTextWriter(_strOutFile, Encoding.UTF8);
                        xw.WriteStartDocument();
                        xslt.Transform(xd, null, xw);
                    }
                    catch (Exception ex)
                    {
                        throw (ex);
                    }
                    finally
                    {
                        if (xw != null)
                            xw.Close();
                    }
                    break;

                case OutputType.TextWriter:
                    TextWriter tw = null;
                    try
                    {
                        tw = new StreamWriter(_strOutFile, false, Encoding.UTF8);
                        xslt.Transform(xd, null, tw);
                    }
                    catch (Exception ex)
                    {
                        throw (ex);
                    }
                    finally
                    {
                        if (tw != null)
                            tw.Close();
                    }
                    break;
            }
            return TEST_PASS;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  CheckResult
        //  -------------------------------------------------------------------------------------------------------------
        public int CheckResult(double szExpResult)
        {
            double checksumActual;
            CXsltChecksum check = new CXsltChecksum(_fTrace);

            if (_nOutput == OutputType.URI)
                checksumActual = check.Calc(xrXSLT);
            else
                checksumActual = check.Calc(_strOutFile);

            if (szExpResult != checksumActual || _fTrace)
            {
                CError.WriteLine("XML: {0}", check.Xml);
                CError.WriteLine("Actual checksum: {0}, Expected: {1}", checksumActual, szExpResult);
            }
            if (szExpResult != checksumActual)
                return TEST_FAIL;

            return TEST_PASS;
        }

        public int CheckResult(string expResult)
        {
            double actChecksum, expChecksum;
            CXsltChecksum check = new CXsltChecksum(_fTrace);

            // Let's make sure we use the same checksum calculating function for
            // actual and expected so we know we are comparing apples to apples.
            if (_nOutput == OutputType.URI)
            {
                expChecksum = check.Calc(XmlReader.Create(new StringReader(expResult)));
                actChecksum = check.Calc(xrXSLT);
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "expectdchecksum.xml")))
                {
                    sw.Write(expResult);
                }
                expChecksum = check.Calc("expectdchecksum.xml");
                actChecksum = check.Calc(_strOutFile);
            }

            if (expChecksum != actChecksum || _fTrace)
            {
                CError.WriteLine("Act Xml: {0}", check.Xml);
                CError.WriteLine("Exp Xml: {0}", expResult);
                CError.WriteLine("Actual checksum: {0}, Expected: {1}", actChecksum, expChecksum);
            }

            if (expChecksum != actChecksum)
                return TEST_FAIL;

            return TEST_PASS;
        }
    }

    internal class CExceptionHandler
    {
        private XPathDocument doc;
        private XPathNavigator nav;
        public string msg;
        public string res;
        private WebData.BaseLib.ExceptionVerifier exVer;

        public CExceptionHandler(string strXmlFile, string ns)
        {
            exVer = new WebData.BaseLib.ExceptionVerifier(ns, WebData.BaseLib.ExceptionVerificationFlags.IgnoreMultipleDots);

            doc = new XPathDocument(strXmlFile);
            nav = ((IXPathNavigable)doc).CreateNavigator();
        }

        // --------------------------------------------------------------------------------------------------------------
        //  VerifyException
        //  -------------------------------------------------------------------------------------------------------------
        public bool VerifyException(Exception ex)
        {
            Type _type = ex.GetType();
            res = (string)_type.InvokeMember("res", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, ex, null);
            msg = (string)nav.Evaluate("string(/exceptions/exception [@res = '" + res + "']/@message)");
            try
            {
                exVer.IsExceptionOk(ex, res);
                return true;
            }
            catch (Exception exp)
            {
                CError.WriteLine(exp);
                return false;
            }
        }

        public bool VerifyException(Exception ex, string res, string[] strParams)
        {
            //msg = (string)nav.Evaluate("string(/exceptions/exception [@res = '"+res+"']/@message)");
            try
            {
                exVer.IsExceptionOk(ex, res, strParams);
                return true;
            }
            catch (Exception exp)
            {
                CError.WriteLine(exp);
                return false;
            }
        }
    }
}