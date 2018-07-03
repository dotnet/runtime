// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Text;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class CReaderFactory : CFactory
    {
        //Enum defined for different API calls. This is superset of the actual reader types.
        protected enum ReaderOverload
        {
            StreamReader,
            StringReader,
            FileStream,
            MemoryStream,
            CoreReader,
            CustomReader
        };

        public enum ReadThru
        {
            XmlReader,
            TextReader,
            Stream
        };

        //The required objects that will be used during the variation processing.
        private XmlReaderSettings _settings = new XmlReaderSettings();
        private XmlReaderSettings _underlyingSettings = new XmlReaderSettings();

        private Encoding _enc = null;
        private Stream _stream = null;
        private string _baseUri = null;
        private TextReader _textReader = null;
        private ReaderOverload _overload;
        private XmlReader _factoryReader = null;
        private XmlReader _underlyingReader = null;
        protected short numEventHandlers = 0;

        // Parse Optional data specific to reader tests.
        protected override void PreTest()
        {
            SetupSettings();
            Log("--Setup Settings Done");
            SetupReadOverload();
            Log("--Setup ReadOverload Done");
            SetupEncoding();
            Log("--Setup Encoding Done");
            SetupBaseUri();
            Log("--Setup BaseUri Done");
            pstate = TestState.PreTest;
        }

        protected override void PostTest()
        {
            //Cleanup and release files you may hold.
            if (_stream != null)
                _stream.Dispose();

            if (_textReader != null)
                _textReader.Dispose();

            if (_underlyingReader != null)
                _underlyingReader.Dispose();

            if (_factoryReader != null)
                _factoryReader.Dispose();

            pstate = TestState.Complete;
        }

        /// <summary>
        /// This method will test the read based on different settings.
        /// It will call the correct overload and set the state properly.
        /// </summary>
        protected override void Test()
        {
            CError.WriteLine("Testing : " + TestFileName);
            string tempStr = null;

            switch (_overload)
            {
                case ReaderOverload.StreamReader:
                    _textReader = new StreamReader(FilePathUtil.getStream(GetFile(TestFileName)));
                    CreateReader(ReadThru.TextReader);
                    break;

                case ReaderOverload.StringReader:
                    StreamReader sr = new StreamReader(FilePathUtil.getStream(GetFile(TestFileName)));
                    tempStr = sr.ReadToEnd();
                    sr.Dispose();
                    _textReader = new StringReader(tempStr);
                    CreateReader(ReadThru.TextReader);
                    break;

                case ReaderOverload.FileStream:
                    _stream = FilePathUtil.getStream(TestFileName);
                    CreateReader(ReadThru.Stream);
                    break;

                case ReaderOverload.MemoryStream:
                    StreamReader sr1 = new StreamReader(FilePathUtil.getStream(GetFile(TestFileName)));
                    tempStr = sr1.ReadToEnd();
                    sr1.Dispose();
                    byte[] bits = _enc.GetBytes(tempStr);
                    _stream = new MemoryStream(bits);
                    CreateReader(ReadThru.Stream);
                    break;
                case ReaderOverload.CoreReader:
                    _underlyingSettings.DtdProcessing = DtdProcessing.Ignore;
                    _underlyingSettings.ConformanceLevel = _settings.ConformanceLevel;
                    StringReader strr = new StringReader(new StreamReader(FilePathUtil.getStream(GetFile(TestFileName))).ReadToEnd());
                    _underlyingReader = ReaderHelper.CreateReader(_overload.ToString(),
                                                                strr,
                                                                false,
                                                                null,
                                                                _underlyingSettings,
                                                                (_settings.ConformanceLevel == ConformanceLevel.Fragment)); //should this be settings or underlyingSettings?
                    CError.Compare(_underlyingReader != null, "ReaderHelper returned null Reader");
                    CreateReader(ReadThru.XmlReader);
                    break;
                case ReaderOverload.CustomReader:
                    if (AsyncUtil.IsAsyncEnabled)
                    {
                        pstate = TestState.Skip;
                        return;
                    }
                    if (_settings.ConformanceLevel != ConformanceLevel.Fragment)
                        _underlyingReader = new CustomReader(FilePathUtil.getStream(GetFile(TestFileName)), false);
                    else
                        _underlyingReader = new CustomReader(FilePathUtil.getStream(GetFile(TestFileName)), true);

                    CError.Compare(_underlyingReader != null, "ReaderHelper returned null Reader");
                    CreateReader(ReadThru.XmlReader);
                    break;

                default:
                    throw new CTestFailedException("Unknown ReaderOverload: " + _overload);
            }

            if (_underlyingReader != null)
                CError.WriteLineIgnore("Type of Reader : " + _underlyingReader.GetType());

            if (pstate == TestState.Pass) return;
            CError.Compare(pstate, TestState.CreateSuccess, "Invalid State after Create: " + pstate);

            //By this time the factory Reader is already set up correctly. So we must go Consume it now.
            CError.Compare(pstate != TestState.Pass && pstate == TestState.CreateSuccess, "Invalid state before Consuming Reader: " + pstate);

            //Call TestReader to Consume Reader;
            TestReader();

            if (pstate == TestState.Pass) return;
            CError.Compare(pstate != TestState.Pass && pstate == TestState.Consume, "Invalid state after Consuming Reader: " + pstate);
        }

        protected void TestReader()
        {
            pstate = TestState.Consume;
            try
            {
                ConsumeReader(_factoryReader);

                if (!IsVariationValid)
                {
                    //Invalid Case didn't throw exception.
                    pstate = TestState.Error;
                    DumpVariationInfo();
                    throw new CTestFailedException("Invalid Variation didn't throw exception");
                }
                else
                {
                    pstate = TestState.Pass;
                }
            }
            finally
            {
                if (_factoryReader != null)
                    _factoryReader.Dispose();
            }

            //If you are not in PASS state at this point you are in Error.
            if (pstate != TestState.Pass)
                pstate = TestState.Error;
        }

        protected void CompareSettings()
        {
            Log("Comparing ErrorSettings");
            XmlReaderSettings actual = _factoryReader.Settings;
            if (actual == null)
                throw new CTestFailedException("Factory Reader Settings returned null");

            CError.Compare(actual.CheckCharacters, _settings.CheckCharacters, "CheckCharacters");
            CError.Compare(actual.IgnoreComments, _settings.IgnoreComments, "IgnoreComments");
            CError.Compare(actual.IgnoreProcessingInstructions, _settings.IgnoreProcessingInstructions, "IgnorePI");
            CError.Compare(actual.IgnoreWhitespace, _settings.IgnoreWhitespace, "IgnoreWhitespace");
            CError.Compare(actual.LineNumberOffset, _settings.LineNumberOffset, "LinenumberOffset");
            CError.Compare(actual.LinePositionOffset, _settings.LinePositionOffset, "LinePositionOffset");
        }

        protected void ConsumeReader(XmlReader reader)
        {
            while (reader.Read())
            {
                string x = reader.Name + reader.NodeType + reader.Value;
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.HasAttributes)
                    {
                        reader.MoveToFirstAttribute();
                        int index = 0;
                        reader.MoveToAttribute(index);
                        index++;
                        while (reader.MoveToNextAttribute())
                        {
                            string name = reader.Name;
                            string value;

                            value = reader.GetAttribute(index);
                            value = reader.GetAttribute(name);
                            value = reader.GetAttribute(name, null);

                            reader.ReadAttributeValue();
                            reader.MoveToAttribute(index);
                            reader.MoveToAttribute(name, null);
                            index++;
                        }
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    reader.Skip();
                }
            }
        }


        /// <summary>
        /// This method calls the Create Method on the XmlReader and puts the state in CreateSuccess or TestPass. 
        /// It goes in PASS also if the reader threw an expected error. In all other cases it should throw
        /// TestFailedException.
        /// </summary>
        /// <param name="readThru">This param determines which overload to call. 
        /// In future on multiple overloads we can make this param 
        /// an enum which can be set using the spec file data</param>
        protected void CreateReader(ReadThru readThru)
        {
            // Assumption is that the Create method doesn't throw NullReferenceException and
            // it is not the goal of this framework to test if they are thrown anywhere.
            // but if they are thrown that's a problem and they shouldn't be caught but exposed.
            try
            {
                switch (readThru)
                {
                    case ReadThru.TextReader:
                        _factoryReader = ReaderHelper.Create(_textReader, _settings, _baseUri);
                        break;
                    case ReadThru.XmlReader:
                        _factoryReader = ReaderHelper.Create(_underlyingReader, _settings);
                        break;
                    case ReadThru.Stream:
                        _factoryReader = ReaderHelper.Create(_stream, _settings);
                        break;
                    default:
                        throw new CTestFailedException("Unknown ReadThru type: " + readThru);
                }

                pstate = TestState.CreateSuccess;
            }
            catch (ArgumentNullException ane)
            {
                Log(ane.Message);
                Log(ane.StackTrace);
                if (!IsVariationValid)
                {
                    if (!CheckException(ane))
                    {
                        pstate = TestState.Error;
                        DumpVariationInfo();
                        throw new CTestFailedException(
                                "Argument Null Exception Thrown in CreateMethod, is your variation data correct?");
                    }
                    else
                    {
                        //This means that the Exception was checked and everything is fine.
                        pstate = TestState.Pass;
                    }
                }
                else
                {
                    pstate = TestState.Error;
                    DumpVariationInfo();
                    throw new CTestFailedException(
                            "Argument Null Exception Thrown in CreateMethod, is your variation data correct?");
                }
            }
        }

        /// <summary>
        /// Setup Settings basically reads the variation info and populates the info block.
        ///    <ConformanceLevel>Fragment</ConformanceLevel>
        ///    <CheckCharacters>true</CheckCharacters>
        ///    <ReaderType>Dtd</ReaderType>
        ///    <NameTable>new</NameTable>
        ///    <LineNumberOffset>1</LineNumberOffset>
        ///		 <LinePositionOffset>0</LinePositionOffset>
        ///    <IgnoreInlineSchema>false</IgnoreInlineSchema>
        ///    <IgnoreSchemaLocation>true</IgnoreSchemaLocation>
        ///    <IgnoreIdentityConstraints>false</IgnoreIdentityConstraints>
        ///		 <IgnoreValidationWarnings>true</IgnoreValidationWarnings>
        ///    <Schemas>2</Schemas>
        ///    <ValidationEventHandler>0</ValidationEventHandler>
        ///    <ProhibitDtd>true</ProhibitDtd>
        ///    <IgnoreWS>false</IgnoreWS>
        ///    <IgnorePI>true</IgnorePI>
        ///    <IgnoreCS>true</IgnoreCS>
        /// </summary>
        private void SetupSettings()
        {
            _settings = new XmlReaderSettings();
            _callbackWarningCount1 = 0;
            _callbackWarningCount2 = 0;
            _callbackErrorCount1 = 0;
            _callbackErrorCount2 = 0;

            //Conformance Level
            _settings.ConformanceLevel = (ConformanceLevel)Enum.Parse(typeof(ConformanceLevel), ReadFilterCriteria("ConformanceLevel", true));

            //CheckCharacters
            _settings.CheckCharacters = bool.Parse(ReadFilterCriteria("CheckCharacters", true));

            //Reader Type : Parse and then set the Xsd or Dtd validation accordingly.
            string readertype = ReadFilterCriteria("ReaderType", true);
            switch (readertype)
            {
                case "Dtd":
                case "Xsd":
                case "Binary":
                    throw new CTestSkippedException("Skipped: ReaderType " + readertype);
                case "Core":
                    break;
                default:
                    throw new CTestFailedException("Unexpected ReaderType Criteria");
            }

            //Nametable
            string nt = ReadFilterCriteria("NameTable", true);
            switch (nt)
            {
                case "new":
                    _settings.NameTable = new NameTable();
                    break;
                case "null":
                    _settings.NameTable = null;
                    break;
                case "custom":
                    _settings.NameTable = new MyNameTable();
                    break;
                default:
                    throw new CTestFailedException("Unexpected Nametable Criteria : " + nt);
            }

            //Line number
            _settings.LineNumberOffset = int.Parse(ReadFilterCriteria("LineNumberOffset", true));
            //Line position
            _settings.LinePositionOffset = int.Parse(ReadFilterCriteria("LinePositionOffset", true));

            _settings.IgnoreProcessingInstructions = bool.Parse(ReadFilterCriteria("IgnorePI", true));
            _settings.IgnoreComments = bool.Parse(ReadFilterCriteria("IgnoreComments", true));
            _settings.IgnoreWhitespace = bool.Parse(ReadFilterCriteria("IgnoreWhiteSpace", true));
        }//End of SetupSettings

        //Validation Event Handlers and their Counts for Reader to verify
        private int _callbackWarningCount1 = 0;
        private int _callbackWarningCount2 = 0;
        private int _callbackErrorCount1 = 0;
        private int _callbackErrorCount2 = 0;

        public int EventWarningCount1
        {
            get { return _callbackWarningCount1; }
        }

        public int EventWarningCount2
        {
            get { return _callbackWarningCount2; }
        }

        public int EventErrorCount1
        {
            get { return _callbackErrorCount1; }
        }

        public int EventErrorCount2
        {
            get { return _callbackErrorCount2; }
        }

        /// <summary>
        /// Sets up the Correct Read Overload Method to be called.
        /// </summary>
        public void SetupReadOverload()
        {
            string ol = ReadFilterCriteria("Load", true);
            if (ol == "HTTPStream" || ol == "FileName" || ol == "XmlTextReader" || ol == "XmlValidatingReader" || ol == "CoreValidatingReader"
                || ol == "CoreXsdReader" || ol == "XmlBinaryReader" || ol == "XPathNavigatorReader" || ol == "XmlNodeReader" || ol == "XmlNodeReaderDD"
                || ol == "XsltReader")
            {
                throw new CTestSkippedException("Skipped: OverLoad " + ol);
            }
            _overload = (ReaderOverload)Enum.Parse(typeof(ReaderOverload), ol);
        }

        public void SetupBaseUri()
        {
            string bUri = ReadFilterCriteria("BaseUri", true);
            switch (bUri)
            {
                case "valid":
                    _baseUri = GetPath(TestFileName, false);
                    Log("Setting baseuri = " + _baseUri);
                    break;
                case "~null":
                    break;
                default:
                    _baseUri = "";
                    break;
            }
        }

        public void SetupEncoding()
        {
            string strEnc = ReadFilterCriteria("Encoding", true);
            if (strEnc != "~null")
                _enc = Encoding.GetEncoding(strEnc);
        }

        //Custom Nametable
        public class MyNameTable : XmlNameTable
        {
            private NameTable _nt = new NameTable();
            public override string Get(string array)
            {
                return _nt.Get(array);
            }

            public override string Get(char[] array, int offset, int length)
            {
                return _nt.Get(array, offset, length);
            }

            public override string Add(string array)
            {
                return _nt.Add(array);
            }

            public override string Add(char[] array, int offset, int length)
            {
                return _nt.Add(array, offset, length);
            }
        }
    }
}
