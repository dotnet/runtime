// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.XmlDiff;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public enum TransformType
    {
        Reader, Stream, Writer, TextWriter
    }

    public enum InputType
    {
        Reader, URI, Navigator
    }

    public enum DocType
    {
        XmlDocument, XPathDocument, Unknown
    }

    public enum ReaderType
    {
        XmlTextReader, XmlNodeReader, XmlValidatingReader, XsltReader, Unknown
    }

    ////////////////////////////////////////////////////////////////
    // Module declaration for LTM usage
    //
    ////////////////////////////////////////////////////////////////
    //[TestModule(Name = "XSLTransform API Tests", Desc = "XSLTransform API Tests", Pri = 1)]
    public class XSLTransformModule
    {
    }

    ////////////////////////////////////////////////////////////////
    // Base class for test cases
    //
    ////////////////////////////////////////////////////////////////
    public class XsltApiTestCaseBase
    {
        // Generic data for all derived test cases
        public String szXslNS = "http://www.w3.org/1999/XSL/Transform";

        public String szDefaultNS = "urn:my-object";
        public String szEmpty = "";
        public String szInvalid = "*?%(){}[]&!@#$";
        public String szLongString = "ThisIsAVeryLongStringToBeStoredAsAVariableToDetermineHowLargeThisBufferForAVariableNameCanBeAndStillFunctionAsExpected";
        public String szLongNS = "http://www.miocrosoft.com/this/is/a/very/long/namespace/uri/to/do/the/api/testing/for/xslt/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/";
        public String[] szWhiteSpace = { "  ", "\n", "\t", "\r", "\t\n  \r\t" };
        public String szSimple = "myArg";

        // Variables from init string
        private string _strPath;           // Path of the data files

        private string _httpPath;          // HTTP Path of the data files
        private InputType _nInput;            // reader, url, or navigator
        private bool _fTrace;            // Should we write out the results of the transform?
        private TransformType _nTransform;        // Type of XSL Transform to do
        private DocType _docType;          // Data document type
        private ReaderType _readerType;       // Reader type

        // Other global variables
        protected string _strOutFile = "out.xml";        // File to create when using write transforms

#pragma warning disable 0618
        protected XslTransform xslt;                           // Main XslTransform instance
#pragma warning restore 0618
        protected XmlReader xrXSLT;                         // for READER transforms
        protected XsltArgumentList m_xsltArg;                   // For XsltArgumentList tests
        public object retObj;

        protected bool _isInProc;          // Is the current test run in proc or /Host None?

        private ITestOutputHelper _output;
        public XsltApiTestCaseBase(ITestOutputHelper output)
        {
            _output = output;
            this.Init(null);
        }

        public InputType MyInputType()
        {
            return _nInput;
        }

        public TransformType MyTransformType()
        {
            return _nTransform;
        }

        public DocType MyDocType()
        {
            return _docType;
        }

        public ReaderType MyReaderType()
        {
            return _readerType;
        }

        public TransformType GetTransformType(String s)
        {
            if (s.EndsWith(",READER"))
                return TransformType.Reader;

            if (s.EndsWith(",STREAM"))
                return TransformType.Stream;

            if (s.EndsWith(",WRITER"))
                return TransformType.Writer;

            if (s.EndsWith(",TEXTWRITER"))
                return TransformType.TextWriter;

            // TransformType not necessary, using default
            return TransformType.Stream;
        }

        public XmlUrlResolver GetDefaultCredResolver()
        {
            XmlUrlResolver myDefaultCredResolver = new XmlUrlResolver();
            //myDefaultCredResolver.Credentials = CredentialCache.DefaultCredentials;

            return myDefaultCredResolver;
        }

        public InputType GetInputType(String s)
        {
            if (s.StartsWith("READER,"))
                return InputType.Reader;
            if (s.StartsWith("URI,"))
                return InputType.URI;
            if (s.StartsWith("NAVIGATOR,"))
                return InputType.Navigator;

            // Input Type not necessary, using default
            return InputType.URI;
        }

        public DocType GetDocType(String s)
        {
            switch (s.ToUpper())
            {
                case "XPATHDOCUMENT":
                    return DocType.XPathDocument;

                case "XMLDOCUMENT":
                    return DocType.XmlDocument;

                default: // DocType Type not important, using default
                    return DocType.XPathDocument;
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
            _nInput = GetInputType(String.Empty);
            _nTransform = GetTransformType(String.Empty);

            // Get parameter info from runtime variables passed to LTM
            _fTrace = (InitStringValue("trace").ToUpper() == "TRUE");
            _docType = GetDocType(InitStringValue("doctype"));
            _readerType = GetReaderType(InitStringValue("readertype"));

            // FullFilePath and FullHttpPath attempt to normalize file paths, however
            // as an extra step we can normalize them here, when they are first read
            // from the LTM file.
            _strPath = Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "XsltApi");
            _httpPath = FilePathUtil.GetHttpTestDataPath() + @"/XsltApi/";

            return;
        }

        // Returns the full path of a file, based on LTM parameters
        public String FullFilePath(String szFile)
        {
            return FullFilePath(szFile, false);
        }

        // Returns the full, lower-cased path of a file, based on LTM parameters
        public String FullFilePath(String szFile, bool normalizeToLower)
        {
            if (szFile == null || szFile == String.Empty)
                return szFile;
            // why is this check here?
            if (szFile.Length > 5)
            {
                if (szFile.Substring(0, 5) != "http:")
                    szFile = Path.Combine(_strPath, szFile);
            }
            if (normalizeToLower)
                return szFile.ToLower();
            else
                return szFile;
        }

        // Returns the full, lower-cased http path of a file, based on LTM parameters
        public String FullHttpPath(String szFile)
        {
            if (szFile == null || szFile == String.Empty)
                return szFile;

            szFile = _httpPath + szFile;

            return szFile.ToLower();
        }

        // --------------------------------------------------------------------------------------------------------------
        //  CheckExpectedError
        //  -------------------------------------------------------------------------------------------------------------
        public void CheckExpectedError(Exception ex, string assembly, string res, string[] strParams)
        {
            CExceptionHandler handler = new CExceptionHandler(Path.Combine(_strPath, "Exceptions.xml"), assembly, _output);
            if (!handler.VerifyException(ex, res, strParams))
            {
                Assert.True(false);
            }
            return;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  CheckExpectedError
        //  -------------------------------------------------------------------------------------------------------------
        public void CheckExpectedError(Exception ex, string assembly, string res, string[] strParams, LineInfo lInfo)
        {
            CExceptionHandler handler = new CExceptionHandler(Path.Combine(_strPath, "Exceptions.xml"), assembly, _output);
            if (!handler.VerifyException(ex, res, strParams, lInfo))
            {
                Assert.True(false);
            }
            return;
        }

        // --------------------------------------------------------------------------------------------------------------
        //  LoadXML
        //  -------------------------------------------------------------------------------------------------------------
        public IXPathNavigable LoadXML(String strFileLocation, DocType _docType)
        {
            switch (_docType)
            {
                case DocType.XmlDocument:
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(strFileLocation);
                    return (IXPathNavigable)xmlDocument;

                case DocType.XPathDocument:
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
            return LoadXSL(_strXslFile, _nInput);
        }

        public int LoadXSL(String _strXslFile, InputType inputType)
        {
            _strXslFile = FullFilePath(_strXslFile);
#pragma warning disable 0618
            xslt = new XslTransform();
#pragma warning restore 0618

            switch (inputType)
            {
                case InputType.URI:
                    _output.WriteLine("Loading style sheet as URI {0}", _strXslFile);
                    xslt.Load(_strXslFile);
                    break;

                case InputType.Reader:
                    switch (_readerType)
                    {
                        case ReaderType.XmlTextReader:
                            XmlTextReader trTemp = new XmlTextReader(_strXslFile);
                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlTextReader {0}", _strXslFile);
                                xslt.Load(trTemp);
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
                                _output.WriteLine("Loading style sheet as XmlNodeReader {0}", _strXslFile);
                                xslt.Load(nrTemp);
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
#pragma warning disable 0618
                            XmlValidatingReader vrTemp = new XmlValidatingReader(new XmlTextReader(_strXslFile));
#pragma warning restore 0618
                            vrTemp.ValidationType = ValidationType.None;
                            vrTemp.EntityHandling = EntityHandling.ExpandEntities;
                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlValidatingReader {0}", _strXslFile);
                                xslt.Load(vrTemp);
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

                case InputType.Navigator:
#pragma warning disable 0618
                    XmlValidatingReader xrLoad = new XmlValidatingReader(new XmlTextReader(_strXslFile));
#pragma warning restore 0618
                    xrLoad.ValidationType = ValidationType.None;
                    XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
                    xrLoad.Dispose();
                    _output.WriteLine("Loading style sheet as Navigator {0}", _strXslFile);
                    xslt.Load(xdTemp);
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
#pragma warning disable 0618
            xslt = new XslTransform();
#pragma warning restore 0618

            switch (_nInput)
            {
                case InputType.URI:
                    _output.WriteLine("Loading style sheet as URI {0}", _strXslFile);
                    xslt.Load(_strXslFile, xr);
                    break;

                case InputType.Reader:
                    switch (_readerType)
                    {
                        case ReaderType.XmlTextReader:
                            XmlTextReader trTemp = new XmlTextReader(_strXslFile);
                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlTextReader {0}", _strXslFile);
                                xslt.Load(trTemp, xr);
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
                                _output.WriteLine("Loading style sheet as XmlNodeReader {0}", _strXslFile);
                                xslt.Load(nrTemp, xr);
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
#pragma warning disable 0618
                            XmlValidatingReader vrTemp = new XmlValidatingReader(new XmlTextReader(_strXslFile));
#pragma warning restore 0618
                            vrTemp.ValidationType = ValidationType.None;
                            vrTemp.EntityHandling = EntityHandling.ExpandEntities;
                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlValidatingReader {0}", _strXslFile);
                                xslt.Load(vrTemp, xr);
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

                case InputType.Navigator:
#pragma warning disable 0618
                    XmlValidatingReader xrLoad = new XmlValidatingReader(new XmlTextReader(_strXslFile));
#pragma warning restore 0618
                    XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
                    xrLoad.Dispose();
                    _output.WriteLine("Loading style sheet as Navigator {0}", _strXslFile);
                    xslt.Load(xdTemp, xr);
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
#pragma warning disable 0618
            xslt = new XslTransform();
#pragma warning restore 0618

            switch (_nInput)
            {
                case InputType.Reader:
                    switch (_readerType)
                    {
                        case ReaderType.XmlTextReader:
                            XmlTextReader trTemp = new XmlTextReader(_strXslFile);
                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlTextReader {0}", _strXslFile);
                                xslt.Load(trTemp, xr, e);
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
                                _output.WriteLine("Loading style sheet as XmlNodeReader {0}", _strXslFile);
                                xslt.Load(nrTemp, xr, e);
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
#pragma warning disable 0618
                            XmlValidatingReader vrTemp = new XmlValidatingReader(new XmlTextReader(_strXslFile));
#pragma warning restore 0618
                            vrTemp.ValidationType = ValidationType.None;
                            vrTemp.EntityHandling = EntityHandling.ExpandEntities;
                            try
                            {
                                _output.WriteLine("Loading style sheet as XmlValidatingReader {0}", _strXslFile);
                                xslt.Load(vrTemp, xr, e);
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

                case InputType.Navigator:
#pragma warning disable 0618
                    XmlValidatingReader xrLoad = new XmlValidatingReader(new XmlTextReader(_strXslFile));
#pragma warning restore 0618
                    XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
                    xrLoad.Dispose();
                    _output.WriteLine("Loading style sheet as Navigator {0}", _strXslFile);
                    xslt.Load(xdTemp.CreateNavigator(), xr, e);
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

        // --------------------------------------------------------------------------------------------------------------
        //  Transform
        //  -------------------------------------------------------------------------------------------------------------

        private static readonly object s_outFileMemoryLock = new object();

#pragma warning disable 0618
        public void CallTransform(XslTransform xslt, String szFullFilename, String _strOutFile)
        {
            lock (s_outFileMemoryLock)
            {
                xslt.Transform(szFullFilename, _strOutFile);
            }
        }

        public void CallTransform(XslTransform xslt, String szFullFilename, String _strOutFile, XmlResolver resolver)
        {
            lock (s_outFileMemoryLock)
            {
                xslt.Transform(szFullFilename, _strOutFile, resolver);
            }
        }
#pragma warning restore 0618

        public int Transform(String szXmlFile)
        {
            // Default value of errorCase is false
            return Transform(szXmlFile, false);
        }

        public int Transform(String szXmlFile, bool errorCase)
        {
            lock (s_outFileMemoryLock)
            {
                szXmlFile = FullFilePath(szXmlFile);

                _output.WriteLine("Loading XML {0}", szXmlFile);
                IXPathNavigable xd = LoadXML(szXmlFile, _docType);

                _output.WriteLine("Executing transform");
                xrXSLT = null;
                Stream strmTemp = null;
                switch (_nTransform)
                {
                    case TransformType.Reader:
                        xrXSLT = xslt.Transform(xd, null);
                        if (errorCase)
                        {
                            try
                            {
                                while (xrXSLT.Read()) { }
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (xrXSLT != null)
                                    xrXSLT.Dispose();
                            }
                        }
                        break;

                    case TransformType.Stream:
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
                                strmTemp.Dispose();
                        }
                        break;

                    case TransformType.Writer:
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
                                xw.Dispose();
                        }
                        break;

                    case TransformType.TextWriter:
                        TextWriter tw = null;
                        try
                        {
                            tw = new StreamWriter(new FileStream(_strOutFile, FileMode.Create, FileAccess.Write), Encoding.UTF8);
                            xslt.Transform(xd, null, tw);
                        }
                        catch (Exception ex)
                        {
                            throw (ex);
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

        // --------------------------------------------------------------------------------------------------------------
        //  Transform_ArgList
        //  -------------------------------------------------------------------------------------------------------------

        public int Transform_ArgList(String szXmlFile)
        {
            // Default value of errorCase is false
            return Transform_ArgList(szXmlFile, false);
        }

        public int Transform_ArgList(String szXmlFile, bool errorCase)
        {
            lock (s_outFileMemoryLock)
            {
                szXmlFile = FullFilePath(szXmlFile);

                _output.WriteLine("Loading XML {0}", szXmlFile);
                IXPathNavigable xd = LoadXML(szXmlFile, _docType);

                _output.WriteLine("Executing transform");
                xrXSLT = null;
                Stream strmTemp = null;
                switch (_nTransform)
                {
                    case TransformType.Reader:
                        xrXSLT = xslt.Transform(xd, m_xsltArg);
                        if (errorCase)
                        {
                            try
                            {
                                while (xrXSLT.Read()) { }
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (xrXSLT != null)
                                    xrXSLT.Dispose();
                            }
                        }
                        break;

                    case TransformType.Stream:
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
                                strmTemp.Dispose();
                        }
                        break;

                    case TransformType.Writer:
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
                                xw.Dispose();
                        }
                        break;

                    case TransformType.TextWriter:
                        TextWriter tw = null;
                        try
                        {
                            tw = new StreamWriter(new FileStream(_strOutFile, FileMode.Create, FileAccess.Write), Encoding.UTF8);
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
                                tw.Dispose();
                            }
                        }
                        break;
                }
                return 1;
            }
        }

        // --------------------------------------------------------------------------------------------------------------
        //  TransformResolver
        //  -------------------------------------------------------------------------------------------------------------

        public int TransformResolver(String szXmlFile, XmlResolver xr)
        {
            // Default value of errorCase is false
            return TransformResolver(szXmlFile, xr, false);
        }

        public int TransformResolver(String szXmlFile, XmlResolver xr, bool errorCase)
        {
            lock (s_outFileMemoryLock)
            {
                szXmlFile = FullFilePath(szXmlFile);

                _output.WriteLine("Loading XML {0}", szXmlFile);
                IXPathNavigable xd = LoadXML(szXmlFile, _docType);

                _output.WriteLine("Executing transform");
                xrXSLT = null;
                Stream strmTemp = null;

                switch (_nTransform)
                {
                    case TransformType.Reader:
                        xrXSLT = xslt.Transform(xd, null, xr);
                        if (errorCase)
                        {
                            try
                            {
                                while (xrXSLT.Read()) { }
                            }
                            catch (Exception ex)
                            {
                                throw (ex);
                            }
                            finally
                            {
                                if (xrXSLT != null)
                                    xrXSLT.Dispose();
                            }
                        }
                        break;

                    case TransformType.Stream:
                        try
                        {
                            strmTemp = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite);
                            xslt.Transform(xd, null, strmTemp, xr);
                        }
                        catch (Exception ex)
                        {
                            throw (ex);
                        }
                        finally
                        {
                            if (strmTemp != null)
                                strmTemp.Dispose();
                        }
                        break;

                    case TransformType.Writer:
                        XmlWriter xw = null;
                        try
                        {
                            xw = new XmlTextWriter(_strOutFile, Encoding.UTF8);
                            xw.WriteStartDocument();
                            xslt.Transform(xd, null, xw, xr);
                        }
                        catch (Exception ex)
                        {
                            throw (ex);
                        }
                        finally
                        {
                            if (xw != null)
                                xw.Dispose();
                        }
                        break;

                    case TransformType.TextWriter:
                        TextWriter tw = null;
                        try
                        {
                            tw = new StreamWriter(new FileStream(_strOutFile, FileMode.Create, FileAccess.Write), Encoding.UTF8);
                            xslt.Transform(xd, null, tw, xr);
                        }
                        catch (Exception ex)
                        {
                            throw (ex);
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

        // --------------------------------------------------------------------------------------------------------------
        //  CheckResult
        //  -------------------------------------------------------------------------------------------------------------
        public int CheckResult(double szExpResult)
        {
            lock (s_outFileMemoryLock)
            {
                double checksumActual;
                CXsltChecksum check = new CXsltChecksum(_fTrace, _output);

                if (_nTransform == TransformType.Reader)
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
        }
    }

    internal class CExceptionHandler
    {
        private XPathDocument _doc;
        private XPathNavigator _nav;
        private ExceptionVerifier _exVer;

        private ITestOutputHelper _output;

        public CExceptionHandler(string strXmlFile, string ns, ITestOutputHelper output)
        {
            _output = output;

            _exVer = new ExceptionVerifier(ns, ExceptionVerificationFlags.IgnoreMultipleDots, _output);

            _doc = new XPathDocument(strXmlFile);
            _nav = ((IXPathNavigable)_doc).CreateNavigator();
        }

        // --------------------------------------------------------------------------------------------------------------
        //  VerifyException
        //  -------------------------------------------------------------------------------------------------------------
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

        // --------------------------------------------------------------------------------------------------------------
        //  VerifyException
        //  -------------------------------------------------------------------------------------------------------------
        public bool VerifyException(Exception ex, string res, string[] strParams, LineInfo lInfo)
        {
            try
            {
                _exVer.IsExceptionOk(ex, res, strParams, lInfo);
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
