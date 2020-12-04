// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Text;
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
    public class XsltApiTestCaseBase : FileCleanupTestBase
    {
        private const string XmlResolverDocumentName = "xmlResolver_document_function.xml";
        private static readonly string s_temporaryResolverDocumentFullName = Path.Combine(Path.GetTempPath(), typeof(XsltApiTestCaseBase) + "_" + Path.GetRandomFileName());
        private static readonly object s_temporaryResolverDocumentLock = new object();

        // Generic data for all derived test cases
        public string szXslNS = "http://www.w3.org/1999/XSL/Transform";

        public string szDefaultNS = "urn:my-object";
        public string szEmpty = "";
        public string szInvalid = "*?%()\0{}[]&!@#$";
        public string szLongString = "ThisIsAVeryLongStringToBeStoredAsAVariableToDetermineHowLargeThisBufferForAVariableNameCanBeAndStillFunctionAsExpected";
        public string szLongNS = "http://www.miocrosoft.com/this/is/a/very/long/namespace/uri/to/do/the/api/testing/for/xslt/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/";
        public string[] szWhiteSpace = { "  ", "\n", "\t", "\r", "\t\n  \r\t" };
        public string szSimple = "myArg";

        // Variables from init string
        private string _strPath;           // Path of the data files

        private string _httpPath;          // HTTP Path of the data files

        // Other global variables
        protected string _strOutFile;        // File to create when using write transforms

#pragma warning disable 0618
        protected XslTransform xslt;                           // Main XslTransform instance
#pragma warning restore 0618
        protected XmlReader xrXSLT;                         // for READER transforms
        protected XsltArgumentList m_xsltArg;                   // For XsltArgumentList tests
        public object retObj;

        protected bool _isInProc;          // Is the current test run in proc or /Host None?

        private ITestOutputHelper _output;

        static XsltApiTestCaseBase()
        {
            // In AppContainer access is denied to full path and the code below and related tests cannot run
            if (!PlatformDetection.IsInAppContainer)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(s_temporaryResolverDocumentFullName));

                // Replace absolute URI in xmlResolver_document_function.xml based on the environment, and store it under a different name
                string xslFile = Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "XsltApi", "xmlResolver_document_function_absolute_uri.xsl");
                string replacedXslFile = Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "XsltApi", "xmlResolver_document_function_absolute_uri_replaced.xsl");
                File.Copy(xslFile, replacedXslFile, true);
                XmlDocument doc = new XmlDocument();
                doc.Load(replacedXslFile);
                string xslString = doc.OuterXml.Replace("ABSOLUTE_URI", s_temporaryResolverDocumentFullName);
                doc.LoadXml(xslString);
                doc.Save(replacedXslFile);
            }
        }

        public XsltApiTestCaseBase(ITestOutputHelper output)
        {
            // Make sure that we don't cache the value of the switch to enable testing
            AppContext.SetSwitch("TestSwitch.LocalAppContext.DisableCaching", true);
            _output = output;
            _strOutFile = GetTestFilePath();
            this.Init(null);
        }

        public void TestUsingTemporaryCopyOfResolverDocument(Action testAction)
        {
            lock (s_temporaryResolverDocumentLock)
            {
                try
                {
                    File.Copy(FullFilePath(XmlResolverDocumentName), s_temporaryResolverDocumentFullName, overwrite: true);
                    testAction();
                }
                finally
                {
                    if (File.Exists(s_temporaryResolverDocumentFullName))
                        File.Delete(s_temporaryResolverDocumentFullName);
                }
            }
        }

        public TransformType GetTransformType(string s)
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

            return myDefaultCredResolver;
        }

        public InputType GetInputType(string s)
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

        public DocType GetDocType(string s)
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

        protected void Init(object objParam)
        {
            // FullFilePath and FullHttpPath attempt to normalize file paths, however
            // as an extra step we can normalize them here, when they are first read
            // from the LTM file.
            _strPath = Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "XsltApi");
            _httpPath = FilePathUtil.GetHttpTestDataPath() + @"/XsltApi/";

            return;
        }

        // Returns the full path of a file, based on LTM parameters
        public string FullFilePath(string szFile)
        {
            return FullFilePath(szFile, false);
        }

        // Returns the full, lower-cased path of a file, based on LTM parameters
        public string FullFilePath(string szFile, bool normalizeToLower)
        {
            if (szFile == null || szFile == string.Empty)
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
        public string FullHttpPath(string szFile)
        {
            if (szFile == null || szFile == string.Empty)
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
        public IXPathNavigable LoadXML(string strFileLocation, DocType _docType)
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
        public int LoadXSL(string _strXslFile, InputType inputType, ReaderType readerType)
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
                    switch (readerType)
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
        public int LoadXSL_Resolver(string _strXslFile, XmlResolver xr, InputType inputType, ReaderType readerType)
        {
            _strXslFile = FullFilePath(_strXslFile);
#pragma warning disable 0618
            xslt = new XslTransform();
#pragma warning restore 0618

            switch (inputType)
            {
                case InputType.URI:
                    _output.WriteLine("Loading style sheet as URI {0}", _strXslFile);
                    xslt.Load(_strXslFile, xr);
                    break;

                case InputType.Reader:
                    switch (readerType)
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

        //VerifyResult
        public void VerifyResult(string expectedValue)
        {
            lock(s_outFileMemoryLock)
            {
                XmlDiff.XmlDiff xmldiff = new XmlDiff.XmlDiff();
                xmldiff.Option = XmlDiffOption.InfosetComparison | XmlDiffOption.IgnoreEmptyElement | XmlDiffOption.NormalizeNewline;

                string actualValue = File.ReadAllText(_strOutFile);

                //Output the expected and actual values
                _output.WriteLine("Expected : " + expectedValue);
                _output.WriteLine("Actual : " + actualValue);

                bool result;

                //Load into XmlTextReaders
                using (XmlTextReader tr1 = new XmlTextReader(_strOutFile))
                using (XmlTextReader tr2 = new XmlTextReader(new StringReader(expectedValue)))
                {
                    result = xmldiff.Compare(tr1, tr2);
                }

                Assert.True(result);
            }
        }

        // --------------------------------------------------------------------------------------------------------------
        //  Transform
        //  -------------------------------------------------------------------------------------------------------------

        private static readonly object s_outFileMemoryLock = new object();

#pragma warning disable 0618
        public void CallTransform(XslTransform xslt, string szFullFilename, string _strOutFile)
        {
            lock (s_outFileMemoryLock)
            {
                xslt.Transform(szFullFilename, _strOutFile);
            }
        }

        public void CallTransform(XslTransform xslt, string szFullFilename, string _strOutFile, XmlResolver resolver)
        {
            lock (s_outFileMemoryLock)
            {
                xslt.Transform(szFullFilename, _strOutFile, resolver);
            }
        }
#pragma warning restore 0618

        public int Transform(string szXmlFile, TransformType transformType, DocType docType)
        {
            // Default value of errorCase is false
            return Transform(szXmlFile, false, transformType, docType);
        }

        public int Transform(string szXmlFile, bool errorCase, TransformType transformType, DocType docType)
        {
            lock (s_outFileMemoryLock)
            {
                szXmlFile = FullFilePath(szXmlFile);

                _output.WriteLine("Loading XML {0}", szXmlFile);
                IXPathNavigable xd = LoadXML(szXmlFile, docType);

                _output.WriteLine("Executing transform");
                xrXSLT = null;
                Stream strmTemp = null;
                switch (transformType)
                {
                    case TransformType.Reader:
                        xrXSLT = xslt.Transform(xd, null);

                        using (FileStream outFile = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite))
                        using (XmlWriter writer = XmlWriter.Create(outFile))
                        {
                            writer.WriteNode(xrXSLT, true);
                        }

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
                            using (FileStream outFile = new FileStream(_strOutFile, FileMode.Create, FileAccess.Write))
                            {
                                tw = new StreamWriter(outFile, Encoding.UTF8);
                                xslt.Transform(xd, null, tw);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw (ex);
                        }
                        break;
                }
                return 1;
            }
        }

        // --------------------------------------------------------------------------------------------------------------
        //  Transform_ArgList
        //  -------------------------------------------------------------------------------------------------------------

        public int Transform_ArgList(string szXmlFile, TransformType transformType, DocType docType)
        {
            // Default value of errorCase is false
            return Transform_ArgList(szXmlFile, false, transformType, docType);
        }

        public int Transform_ArgList(string szXmlFile, bool errorCase, TransformType transformType, DocType docType)
        {
            lock (s_outFileMemoryLock)
            {
                szXmlFile = FullFilePath(szXmlFile);

                _output.WriteLine("Loading XML {0}", szXmlFile);
                IXPathNavigable xd = LoadXML(szXmlFile, docType);

                _output.WriteLine("Executing transform");
                xrXSLT = null;
                Stream strmTemp = null;
                switch (transformType)
                {
                    case TransformType.Reader:
                        xrXSLT = xslt.Transform(xd, m_xsltArg);

                        using (FileStream outFile = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite))
                        using (XmlWriter writer = XmlWriter.Create(outFile))
                        {
                            writer.WriteNode(xrXSLT, true);
                        }

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
                            using (FileStream outFile = new FileStream(_strOutFile, FileMode.Create, FileAccess.Write))
                            {
                                tw = new StreamWriter(outFile, Encoding.UTF8);
                                xslt.Transform(xd, m_xsltArg, tw);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw (ex);
                        }
                        break;
                }
                return 1;
            }
        }

        // --------------------------------------------------------------------------------------------------------------
        //  TransformResolver
        //  -------------------------------------------------------------------------------------------------------------

        public int TransformResolver(string szXmlFile, TransformType transformType, DocType docType, XmlResolver xr)
        {
            // Default value of errorCase is false
            return TransformResolver(szXmlFile, xr, false, transformType, docType);
        }

        public int TransformResolver(string szXmlFile, XmlResolver xr, bool errorCase, TransformType transformType, DocType docType)
        {
            lock (s_outFileMemoryLock)
            {
                szXmlFile = FullFilePath(szXmlFile);

                _output.WriteLine("Loading XML {0}", szXmlFile);
                IXPathNavigable xd = LoadXML(szXmlFile, docType);

                _output.WriteLine("Executing transform");
                xrXSLT = null;
                Stream strmTemp = null;

                switch (transformType)
                {
                    case TransformType.Reader:
                        xrXSLT = xslt.Transform(xd, null, xr);

                        using (FileStream outFile = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite))
                        using (XmlWriter writer = XmlWriter.Create(outFile))
                        {
                            writer.WriteNode(xrXSLT, true);
                        }

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
                            using (FileStream outFile = new FileStream(_strOutFile, FileMode.Create, FileAccess.Write))
                            {
                                tw = new StreamWriter(outFile, Encoding.UTF8);
                                xslt.Transform(xd, null, tw, xr);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw (ex);
                        }
                        break;
                }
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
