// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.XmlDiff;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;
using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
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
    // Module declaration for LTM usage
    //
    ////////////////////////////////////////////////////////////////
    //[TestModule(Name = "XsltApiV2", Desc = "XslCompiledTransform API Tests", Pri = 1)]
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
    }

    ////////////////////////////////////////////////////////////////
    // Base class for test cases
    //
    ////////////////////////////////////////////////////////////////
    public class XsltApiTestCaseBase2 //: CTestCase
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

        private ITestOutputHelper _output;
        public XsltApiTestCaseBase2(ITestOutputHelper output)
        {
            _output = output;
            this.Init(null);
        }

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
            //myDefaultCredResolver.Credentials = CredentialCache.DefaultCredentials;

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
            return String.Empty;
        }

        public void Init(object objParam)
        {
            // Get input and transform type from attribute
            _nInputXsl = GetXslInputType(String.Empty);
            _nOutput = GetOutputType(String.Empty);

            // Get parameter info from runtime variables passed to LTM
            _fTrace = false;
            _navType = GetDocType(InitStringValue("doctype"));
            _readerType = GetReaderType(InitStringValue("readertype"));

            //This is a temporary fix to restore the baselines. Refer to Test bug #
            _strPath = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltApiV2\");
            _httpPath = FilePathUtil.GetHttpTestDataPath() + @"/XsltApiV2/";
            _standardTests = Path.Combine(@"TestFiles\", FilePathUtil.GetHttpStandardPath() + @"/xslt10/Current/");

            return;
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
        public void CheckExpectedError(Exception ex, string assembly)
        {
            CExceptionHandler handler = new CExceptionHandler(_strPath + "exceptions.xml", assembly, _output);
            bool result = handler.VerifyException(ex);
            if (handler.res != _expectedErrorCode)
            {
                _output.WriteLine("Expected Exception : {0}", _expectedErrorCode);
                _output.WriteLine("Actual Exception : {0}", handler.res);
                Assert.True(false);
            }
            if (!result)
            {
                Assert.True(false);
            }
            return;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  CheckExpectedError
        //  -------------------------------------------------------------------------------------------------------------
        public void CheckExpectedError(Exception ex, string assembly, string res, string[] strParams)
        {
            CExceptionHandler handler = new CExceptionHandler(_strPath + "exceptions.xml", assembly, _output);
            if (!handler.VerifyException(ex, res, strParams))
            {
                Assert.True(false);
            }
            return;
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
                /*                  _output.WriteLine("Loading style sheet as URI {0}", _strXslFile);
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
        public int LoadXSL_Resolver(String _strXslFile, XmlResolver xr)
        {
            _strXslFile = FullFilePath(_strXslFile);
            xslt = new XslCompiledTransform();
            XmlReaderSettings xrs = null;
            switch (_nInputXsl)
            {
                case XslInputType.URI:
                    _output.WriteLine("Loading style sheet as URI " + _strXslFile);
                    xslt.Load(_strXslFile, XsltSettings.TrustedXslt, xr);
                    break;

                case XslInputType.Reader:
                    switch (_readerType)
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

        // --------------------------------------------------------------------------------------------------------------
        //  LoadXSL_Resolver_Evidence
        //  -------------------------------------------------------------------------------------------------------------
        /*public int LoadXSL_Resolver_Evidence(String _strXslFile, XmlResolver xr, Evidence e)
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
                                _output.WriteLine("Loading style sheet as XmlTextReader " + _strXslFile);
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
                                    nrTemp.Dispose();
                            }
                            break;

                        case ReaderType.XmlValidatingReader:
                        default:
                            XmlReaderSettings xrs = new XmlReaderSettings();
#pragma warning disable 0618
                            xrs.ProhibitDtd = false;
#pragma warning restore 0618
                            XmlReader vrTemp = null;
                            try
                            {
                                vrTemp = XmlReader.Create(_strXslFile, xrs);
                                _output.WriteLine("Loading style sheet as XmlValidatingReader " + _strXslFile);
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
                                    vrTemp.Dispose();
                            }
                            break;
                    }
                    break;

                case XslInputType.Navigator:
                    XmlReader xrLoad = XmlReader.Create(_strXslFile);
                    XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
                    xrLoad.Dispose();
                    _output.WriteLine("Loading style sheet as Navigator " + _strXslFile);
                    xslt.Load(xdTemp.CreateNavigator(), XsltSettings.TrustedXslt, xr);
                    break;
            }
            return 1;
        }*/

        //VerifyResult
        public void VerifyResult(string expectedValue)
        {
            XmlDiff.XmlDiff xmldiff = new XmlDiff.XmlDiff();
            xmldiff.Option = XmlDiffOption.InfosetComparison | XmlDiffOption.IgnoreEmptyElement;

            StreamReader sr = new StreamReader(new FileStream("out.xml", FileMode.Open, FileAccess.Read));
            string actualValue = sr.ReadToEnd();
            sr.Dispose();

            //Output the expected and actual values
            _output.WriteLine("Expected : " + expectedValue);
            _output.WriteLine("Actual : " + actualValue);

            //Load into XmlTextReaders
            XmlTextReader tr1 = new XmlTextReader("out.xml");
            XmlTextReader tr2 = new XmlTextReader(new StringReader(expectedValue));

            bool bResult = xmldiff.Compare(tr1, tr2);

            //Close the readers
            tr1.Dispose();
            tr2.Dispose();

            if (bResult)
                return;
            else
                Assert.True(false);
        }

        //VerifyResult which compares 2 arguments using XmlDiff.
        public void VerifyResult(string baseline, string outputFile)
        {
            bool bResult = false;
            FileStream fsExpected;

            baseline = FullFilePath(baseline);

            XmlDiff.XmlDiff diff = new XmlDiff.XmlDiff();
            diff.Option = XmlDiffOption.IgnoreEmptyElement | XmlDiffOption.IgnoreAttributeOrder | XmlDiffOption.InfosetComparison | XmlDiffOption.IgnoreWhitespace;
            XmlParserContext context = new XmlParserContext(new NameTable(), null, "", XmlSpace.None);

            fsExpected = new FileStream(baseline, FileMode.Open, FileAccess.Read, FileShare.Read);
            FileStream fsActual = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.Read);

            _output.WriteLine("Verifying o/p with baseline result {0}...", baseline);
            try
            {
                bResult = diff.Compare(new XmlTextReader(fsActual, XmlNodeType.Element, context), new XmlTextReader(fsExpected, XmlNodeType.Element, context));
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
            if (!bResult)
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

            if (bResult)
                return;
            else
            {
                _output.WriteLine("**** Baseline mis-matched ****");
                Assert.True(false);
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

            _output.WriteLine("Loading XML " + szXmlFile);
            IXPathNavigable xd = LoadXML(szXmlFile, _navType);

            _output.WriteLine("Executing transform");
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

        public int Transform_ArgList(String szXmlFile)
        {
            // Default value of errorCase is false
            return (Transform_ArgList(szXmlFile, false));
        }

        public int Transform_ArgList(String szXmlFile, bool errorCase)
        {
            szXmlFile = FullFilePath(szXmlFile);

            _output.WriteLine("Loading XML " + szXmlFile);
            IXPathNavigable xd = LoadXML(szXmlFile, _navType);

            _output.WriteLine("Executing transform");
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

        public int TransformResolver(String szXmlFile, XmlResolver xr)
        {
            // Default value of errorCase is false
            return (TransformResolver(szXmlFile, xr, false));
        }

        public int TransformResolver(String szXmlFile, XmlResolver xr, bool errorCase)
        {
            szXmlFile = FullFilePath(szXmlFile);

            _output.WriteLine("Loading XML " + szXmlFile);
            IXPathNavigable xd = LoadXML(szXmlFile, _navType);

            _output.WriteLine("Executing transform");
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

        // --------------------------------------------------------------------------------------------------------------
        //  CheckResult
        //  -------------------------------------------------------------------------------------------------------------
        public int CheckResult(double szExpResult)
        {
            double checksumActual;
            CXsltChecksum check = new CXsltChecksum(_fTrace, _output);

            if (_nOutput == OutputType.URI)
                checksumActual = check.Calc(xrXSLT);
            else
                checksumActual = check.Calc(_strOutFile);

            if (szExpResult != checksumActual || _fTrace)
            {
                _output.WriteLine("XML: {0}", check.Xml);
                _output.WriteLine("Actual checksum: {0}, Expected: {1}", checksumActual, szExpResult);
            }
            if (szExpResult != checksumActual)
                return 0;

            return 1;
        }

        public int CheckResult(string expResult)
        {
            double actChecksum, expChecksum;
            CXsltChecksum check = new CXsltChecksum(_fTrace, _output);

            // Let's make sure we use the same checksum calculating function for
            // actual and expected so we know we are comparing apples to apples.
            if (_nOutput == OutputType.URI)
            {
                expChecksum = check.Calc(XmlReader.Create(new StringReader(expResult)));
                actChecksum = check.Calc(xrXSLT);
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "expectdchecksum.xml"), FileMode.Create, FileAccess.Write)))
                {
                    sw.Write(expResult);
                }
                expChecksum = check.Calc("expectdchecksum.xml");
                actChecksum = check.Calc(_strOutFile);
            }

            if (expChecksum != actChecksum || _fTrace)
            {
                _output.WriteLine("Act Xml: {0}", check.Xml);
                _output.WriteLine("Exp Xml: {0}", expResult);
                _output.WriteLine("Actual checksum: {0}, Expected: {1}", actChecksum, expChecksum);
            }

            if (expChecksum != actChecksum)
                return 0;

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
            _exVer = new ExceptionVerifier(ns, ExceptionVerificationFlags.IgnoreMultipleDots, _output);

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
            res = String.Empty;//(string)_type.InvokeMember("res", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, ex, null);
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
            //msg = (string)nav.Evaluate("string(/exceptions/exception [@res = '"+res+"']/@message)");
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