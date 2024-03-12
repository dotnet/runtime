// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml.Tests;
using System.Xml.XmlDiff;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.XslCompiledTransformApiTests
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

    ////////////////////////////////////////////////////////////////
    // Base class for test cases
    //
    ////////////////////////////////////////////////////////////////
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    public class XsltApiTestCaseBase2
    {
        // Generic data for all derived test cases
        public string szXslNS = "http://www.w3.org/1999/XSL/Transform";

        public string szDefaultNS = "urn:my-object";
        public string szEmpty = "";
        public string szInvalid = "*?%(){}\0[]&!@#$";
        public string szLongString = "ThisIsAVeryLongStringToBeStoredAsAVariableToDetermineHowLargeThisBufferForAVariableNameCanBeAndStillFunctionAsExpected";
        public string szLongNS = "http://www.microsoft.com/this/is/a/very/long/namespace/uri/to/do/the/api/testing/for/xslt/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/";
        public string[] szWhiteSpace = { "  ", "\n", "\t", "\r", "\t\n  \r\t" };
        public string szSimple = "myArg";

        // Variables from init string
        private string _strPath;                           // Path of the data files

        private string _httpPath;                          // Http Path of the data files

        // Other global variables
        protected string _strOutFile = "out.xml";        // File to create when using write transforms

        protected XslCompiledTransform xslt;                           // Main XslCompiledTransform instance
        protected XmlReader xrXSLT;                         // for READER transforms
        protected XsltArgumentList m_xsltArg;                      // For XsltArgumentList tests
        public object retObj;

        protected string _standardTests;

        private ITestOutputHelper _output;
        public XsltApiTestCaseBase2(ITestOutputHelper output)
        {
            AppContext.SetSwitch("TestSwitch.LocalAppContext.DisableCaching", true);
            _output = output;
            this.Init(null);
        }

        static XsltApiTestCaseBase2()
        {
            // Replace absolute URI in xmlResolver_document_function.xml based on the environment
            string targetFile = Path.Combine(Path.GetTempPath(), typeof(XsltApiTestCaseBase2) + "_" + Path.GetRandomFileName());
            string xslFile = Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "XsltApiV2", "xmlResolver_document_function_absolute_uri.xsl");
            XmlDocument doc = new XmlDocument();
            doc.Load(xslFile);
            string xslString = doc.OuterXml.Replace("ABSOLUTE_URI", targetFile);
            doc.LoadXml(xslString);
            doc.Save(xslFile);
        }

        public OutputType GetOutputType(string s)
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

            return myDefaultCredResolver;
        }

        public XslInputType GetXslInputType(string s)
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

        public NavType GetDocType(string s)
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

        public ReaderType GetReaderType(string s)
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

        public void Init(object objParam)
        {
            //This is a temporary fix to restore the baselines. Refer to Test bug #
            _strPath = Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "XsltApiV2");
            _httpPath = Path.Combine(FilePathUtil.GetHttpTestDataPath(), "XsltApiV2");
            _standardTests = Path.Combine("TestFiles", FilePathUtil.GetHttpStandardPath(), "xslt10","Current");

            return;
        }

        public string FullFilePath(string szFile)
        {
            if (szFile == null || szFile == string.Empty)
                return szFile;
            if (szFile.Length > 5)
            {
                if (szFile.Substring(0, 5) != "http:")
                    szFile = Path.Combine(_strPath, szFile);
            }
            return szFile;
        }

        public string FullHttpPath(string szFile)
        {
            if (szFile == null || szFile == string.Empty)
                return szFile;
            szFile = Path.Combine(_httpPath, szFile);
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
        public void CheckExpectedError(Exception ex, string assembly)
        {
            CExceptionHandler handler = new CExceptionHandler(Path.Combine(_strPath, "Exceptions.xml"), assembly, _output);
            bool result = handler.VerifyException(ex);
            if (handler.res != _expectedErrorCode)
            {
                _output.WriteLine("Expected Exception : {0}", _expectedErrorCode);
                _output.WriteLine("Actual Exception : {0}", handler.res);
                Assert.Fail();
            }
            if (!result)
            {
                Assert.Fail();
            }
            return;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  CheckExpectedError
        //  -------------------------------------------------------------------------------------------------------------
        public void CheckExpectedError(Exception ex, string assembly, string res, string[] strParams)
        {
            CExceptionHandler handler = new CExceptionHandler(Path.Combine(_strPath, "Exceptions.xml"), assembly, _output);
            if (!handler.VerifyException(ex, res, strParams))
            {
                Assert.Fail();
            }
            return;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  LoadXML
        //  -------------------------------------------------------------------------------------------------------------
        public IXPathNavigable LoadXML(string strFileLocation, NavType _navType)
        {
            switch (_navType)
            {
                case NavType.XmlDocument:
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(strFileLocation);
                    return (IXPathNavigable)xmlDocument;

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
        public int LoadXSL(string _strXslFile, XslInputType xslInputType, ReaderType readerType)
        {
            return LoadXSL(_strXslFile, xslInputType, readerType, new XmlUrlResolver());
        }

        public int LoadXSL(string _strXslFile, XslInputType xslInputType, ReaderType readerType, XmlResolver xr)
        {
            _strXslFile = FullFilePath(_strXslFile);
            xslt = new XslCompiledTransform();
            XmlReaderSettings xrs = null;

            switch (xslInputType)
            {
                case XslInputType.URI:
                    _output.WriteLine("Loading style sheet as URI {0}", _strXslFile);
                    xrs = new XmlReaderSettings() { DtdProcessing = DtdProcessing.Parse};
                    xslt.Load(XmlReader.Create(_strXslFile, xrs), XsltSettings.TrustedXslt, xr);
                    break;

                case XslInputType.Reader:
                    switch (readerType)
                    {
                        case ReaderType.XmlTextReader:
                            XmlTextReader trTemp = new XmlTextReader(_strXslFile);
                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlTextReader " + _strXslFile);
                                xslt.Load(trTemp, XsltSettings.TrustedXslt, xr);
                            }
                            finally
                            {
                                if (trTemp != null)
                                    trTemp.Dispose();
                            }
                            break;

                        case ReaderType.XmlNodeReader:
                            XmlDocument docTemp = new XmlDocument();
                            docTemp.Load(_strXslFile);
                            XmlNodeReader nrTemp = new XmlNodeReader(docTemp);
                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlNodeReader " + _strXslFile);
                                xslt.Load(nrTemp);
                            }
                            finally
                            {
                                if (nrTemp != null)
                                    nrTemp.Dispose();
                            }
                            break;

                        case ReaderType.XmlValidatingReader:
                        default:
                            xrs = new XmlReaderSettings();
#pragma warning disable 0618
                            xrs.ProhibitDtd = false;
#pragma warning restore 0618
                            XmlReader xvr = XmlReader.Create(_strXslFile, xrs);

                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlValidatingReader " + _strXslFile);
                                xslt.Load(xvr, XsltSettings.TrustedXslt, xr);
                            }
                            finally
                            {
                                if (xvr != null)
                                    xvr.Dispose();
                            }
                            break;
                    }
                    break;

                case XslInputType.Navigator:
                    xrs = new XmlReaderSettings();
                    xrs.ValidationType = ValidationType.None;
#pragma warning disable 0618
                    xrs.ProhibitDtd = false;
#pragma warning restore 0618
                    XmlReader xrLoad = XmlReader.Create(_strXslFile, xrs);

                    XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
                    xrLoad.Dispose();
                    _output.WriteLine("Loading style sheet as Navigator " + _strXslFile);
                    xslt.Load(xdTemp, XsltSettings.TrustedXslt, xr);
                    break;
            }
            return 1;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  LoadXSL_Resolver
        //  -------------------------------------------------------------------------------------------------------------
        public int LoadXSL_Resolver(string _strXslFile, XslInputType xslInputType, ReaderType readerType, XmlResolver xr)
        {
            _strXslFile = FullFilePath(_strXslFile);
            xslt = new XslCompiledTransform();
            XmlReaderSettings xrs = null;
            switch (xslInputType)
            {
                case XslInputType.URI:
                    _output.WriteLine("Loading style sheet as URI " + _strXslFile);
                    xslt.Load(_strXslFile, XsltSettings.TrustedXslt, xr);
                    break;

                case XslInputType.Reader:
                    switch (readerType)
                    {
                        case ReaderType.XmlTextReader:
                            XmlTextReader trTemp = new XmlTextReader(_strXslFile);
                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlTextReader " + _strXslFile);
                                xslt.Load(trTemp, XsltSettings.TrustedXslt, xr);
                            }
                            finally
                            {
                                if (trTemp != null)
                                    trTemp.Dispose();
                            }
                            break;

                        case ReaderType.XmlNodeReader:
                            XmlDocument docTemp = new XmlDocument();
                            docTemp.Load(_strXslFile);
                            XmlNodeReader nrTemp = new XmlNodeReader(docTemp);
                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlNodeReader " + _strXslFile);
                                xslt.Load(nrTemp, XsltSettings.TrustedXslt, xr);
                            }
                            finally
                            {
                                if (nrTemp != null)
                                    nrTemp.Dispose();
                            }
                            break;

                        case ReaderType.XmlValidatingReader:
                        default:
                            xrs = new XmlReaderSettings();
#pragma warning disable 0618
                            xrs.ProhibitDtd = false;
#pragma warning restore 0618
                            XmlReader vrTemp = XmlReader.Create(_strXslFile, xrs);
                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlValidatingReader " + _strXslFile);
                                xslt.Load(vrTemp, XsltSettings.TrustedXslt, xr);
                            }
                            finally
                            {
                                if (vrTemp != null)
                                    vrTemp.Dispose();
                            }
                            break;
                    }
                    break;

                case XslInputType.Navigator:
                    xrs = new XmlReaderSettings();
#pragma warning disable 0618
                    xrs.ProhibitDtd = false;
#pragma warning restore 0618
                    XmlReader xrLoad = XmlReader.Create(_strXslFile, xrs);

                    XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
                    xrLoad.Dispose();
                    _output.WriteLine("Loading style sheet as Navigator " + _strXslFile);
                    xslt.Load(xdTemp, XsltSettings.TrustedXslt, xr);
                    break;
            }
            return 1;
        }

        //VerifyResult
        public void VerifyResult(string expectedValue)
        {
            XmlDiff.XmlDiff xmldiff = new XmlDiff.XmlDiff();
            xmldiff.Option = XmlDiffOption.InfosetComparison | XmlDiffOption.IgnoreEmptyElement | XmlDiffOption.NormalizeNewline;

            StreamReader sr = new StreamReader(new FileStream("out.xml", FileMode.Open, FileAccess.Read));
            string actualValue = sr.ReadToEnd();
            sr.Dispose();

            //Output the expected and actual values
            _output.WriteLine("Expected : " + expectedValue);
            _output.WriteLine("Actual : " + actualValue);

            //Load into XmlTextReaders
            XmlTextReader tr1 = new XmlTextReader("out.xml");
            XmlTextReader tr2 = new XmlTextReader(new StringReader(expectedValue));

            bool result = xmldiff.Compare(tr1, tr2);

            //Close the readers
            tr1.Dispose();
            tr2.Dispose();

             Assert.True(result);
        }

        //VerifyResult which compares 2 arguments using XmlDiff.
        public void VerifyResult(string baseline, string outputFile)
        {
            bool result = false;
            FileStream fsExpected;

            baseline = FullFilePath(baseline);

            XmlDiff.XmlDiff diff = new XmlDiff.XmlDiff();
            diff.Option = XmlDiffOption.IgnoreEmptyElement | XmlDiffOption.IgnoreAttributeOrder | XmlDiffOption.InfosetComparison | XmlDiffOption.IgnoreWhitespace | XmlDiffOption.NormalizeNewline;
            XmlParserContext context = new XmlParserContext(new NameTable(), null, "", XmlSpace.None);

            fsExpected = new FileStream(baseline, FileMode.Open, FileAccess.Read, FileShare.Read);
            FileStream fsActual = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.Read);

            _output.WriteLine("Verifying o/p with baseline result {0}...", baseline);
            try
            {
                result = diff.Compare(new XmlTextReader(fsActual, XmlNodeType.Element, context), new XmlTextReader(fsExpected, XmlNodeType.Element, context));
            }
            catch (Exception e)
            {
                // TO DO: Write exception msgs in ignore tags
                _output.WriteLine(e.ToString());
            }
            finally
            {
                fsExpected.Dispose();
                fsActual.Dispose();
            }
            if (!result)
            {
                // Write out the actual and expected o/p
                _output.WriteLine("Expected o/p: ");
                using (StreamReader sr = new StreamReader(new FileStream(baseline, FileMode.Open, FileAccess.Read)))
                {
                    string baseLine = sr.ReadToEnd();
                    _output.WriteLine(baseLine);
                }
                _output.WriteLine("Actual o/p: ");
                using (StreamReader sr = new StreamReader(new FileStream(outputFile, FileMode.Open, FileAccess.Read)))
                {
                    string output = sr.ReadToEnd();
                    _output.WriteLine(output);
                }

                using (StreamWriter sw = new StreamWriter(new FileStream("diff.xml", FileMode.Open, FileAccess.Read)))
                {
                    sw.WriteLine("<?xml-stylesheet href='diff.xsl' type='text/xsl'?>");
                    sw.WriteLine(diff.ToXml());
                }
            }

            Assert.True(result, "**** Baseline mis-matched ****");
        }

        // --------------------------------------------------------------------------------------------------------------
        //  Transform
        //  -------------------------------------------------------------------------------------------------------------

        public int Transform(string szXmlFile, OutputType outputType, NavType navType)
        {
            // Default value of errorCase is false
            return (Transform(szXmlFile, outputType, navType, false));
        }

        public int Transform(string szXmlFile, OutputType outputType, NavType navType, bool errorCase)
        {
            szXmlFile = FullFilePath(szXmlFile);

            _output.WriteLine("Loading XML " + szXmlFile);
            IXPathNavigable xd = LoadXML(szXmlFile, navType);

            _output.WriteLine("Executing transform");
            xrXSLT = null;
            Stream strmTemp = null;
            switch (outputType)
            {
                case OutputType.Stream:
                    try
                    {
                        strmTemp = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite);
                        xslt.Transform(xd, null, strmTemp);
                    }
                    finally
                    {
                        if (strmTemp != null)
                            strmTemp.Dispose();
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
                    finally
                    {
                        if (xw != null)
                            xw.Dispose();
                    }
                    break;

                case OutputType.TextWriter:
                    TextWriter tw = null;
                    try
                    {
                        tw = new StreamWriter(new FileStream(_strOutFile, FileMode.Create, FileAccess.Write), Encoding.UTF8);
                        xslt.Transform(xd, null, tw);
                    }
                    finally
                    {
                        if (tw != null)
                            tw.Dispose();
                    }
                    break;
            }
            return 1;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  Transform_ArgList
        //  -------------------------------------------------------------------------------------------------------------

        public int Transform_ArgList(string szXmlFile, OutputType outputType, NavType navType)
        {
            // Default value of errorCase is false
            return (Transform_ArgList(szXmlFile, outputType, navType, false));
        }

        public int Transform_ArgList(string szXmlFile, OutputType outputType, NavType navType, bool errorCase)
        {
            szXmlFile = FullFilePath(szXmlFile);

            _output.WriteLine("Loading XML " + szXmlFile);
            IXPathNavigable xd = LoadXML(szXmlFile, navType);

            _output.WriteLine("Executing transform");
            xrXSLT = null;
            Stream strmTemp = null;
            switch (outputType)
            {
                case OutputType.Stream:
                    try
                    {
                        strmTemp = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite);
                        xslt.Transform(xd, m_xsltArg, strmTemp);
                    }
                    finally
                    {
                        if (strmTemp != null)
                            strmTemp.Dispose();
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
                    finally
                    {
                        if (xw != null)
                            xw.Dispose();
                    }
                    break;

                case OutputType.TextWriter:
                    TextWriter tw = null;
                    try
                    {
                        tw = new StreamWriter(new FileStream(_strOutFile, FileMode.Create, FileAccess.Write), Encoding.UTF8);
                        xslt.Transform(xd, m_xsltArg, tw);
                    }
                    finally
                    {
                        if (tw != null)
                        {
                            tw.Dispose();
                        }
                    }
                    break;
            }
            return 1;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  TransformResolver
        //  -------------------------------------------------------------------------------------------------------------

        public int TransformResolver(string szXmlFile, OutputType outputType, NavType navType, XmlResolver xr)
        {
            // Default value of errorCase is false
            return (TransformResolver(szXmlFile, outputType, navType, xr, false));
        }

        public int TransformResolver(string szXmlFile, OutputType outputType, NavType navType, XmlResolver xr, bool errorCase)
        {
            szXmlFile = FullFilePath(szXmlFile);

            _output.WriteLine("Loading XML " + szXmlFile);
            IXPathNavigable xd = LoadXML(szXmlFile, navType);

            _output.WriteLine("Executing transform");
            xrXSLT = null;
            Stream strmTemp = null;

            switch (outputType)
            {
                case OutputType.Stream:
                    try
                    {
                        strmTemp = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite);
                        xslt.Transform(xd, null, strmTemp);
                    }
                    finally
                    {
                        if (strmTemp != null)
                            strmTemp.Dispose();
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
                    finally
                    {
                        if (xw != null)
                            xw.Dispose();
                    }
                    break;

                case OutputType.TextWriter:
                    TextWriter tw = null;
                    try
                    {
                        tw = new StreamWriter(new FileStream(_strOutFile, FileMode.Create, FileAccess.Write), Encoding.UTF8);
                        xslt.Transform(xd, null, tw);
                    }
                    finally
                    {
                        if (tw != null)
                            tw.Dispose();
                    }
                    break;
            }
            return 1;
        }
    }

    internal class CExceptionHandler
    {
        private XPathDocument _doc;
        private XPathNavigator _nav;
        public string msg;
        public string res;
        private ExceptionVerifier _exVer;

        private ITestOutputHelper _output;

        public CExceptionHandler(string strXmlFile, string ns, ITestOutputHelper output)
        {
            _exVer = new ExceptionVerifier(ns, ExceptionVerificationFlags.IgnoreMultipleDots, output);

            _doc = new XPathDocument(strXmlFile);
            _nav = ((IXPathNavigable)_doc).CreateNavigator();

            _output = output;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  VerifyException
        //  -------------------------------------------------------------------------------------------------------------
        public bool VerifyException(Exception ex)
        {
            Type _type = ex.GetType();
            res = string.Empty;
            msg = (string)_nav.Evaluate("string(/exceptions/exception [@res = '" + res + "']/@message)");
            try
            {
                _exVer.IsExceptionOk(ex, res);
                return true;
            }
            catch (Exception exp)
            {
                _output.WriteLine(exp.Message);
                return false;
            }
        }

        public bool VerifyException(Exception ex, string res, string[] strParams)
        {
            try
            {
                _exVer.IsExceptionOk(ex, res, strParams);
                return true;
            }
            catch (Exception exp)
            {
                _output.WriteLine(exp.Message);
                return false;
            }
        }
    }
}
