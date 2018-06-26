// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Text;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    /// <summary>
    /// Summary description for WriterFactory.
    /// </summary>
    public class CWriterFactory : CFactory
    {
        protected enum WriterType
        {
            UTF8Writer,
            UnicodeWriter
        };

        protected enum WriteThru
        {
            Stream,
            StringBuilder,
            TextWriter,
            XmlWriter
        };

        protected enum WriterOverload
        {
            StringBuilder,
            StringWriter,
            StreamWriter,
            MemoryStream,
            TextWriter,
            UTF8Writer,
            UnicodeWriter
        };

        private XmlWriter _factoryWriter = null;
        private XmlWriter _underlyingWriter = null;
        private Stream _stream = null;
        private StringBuilder _stringBuilder = null;
        private TextWriter _textWriter = null;
        private XmlWriterSettings _settings = new XmlWriterSettings();
        private XmlWriterSettings _underlyingSettings = new XmlWriterSettings();

        private WriterOverload _overload;

        protected override void PreTest()
        {
            Log("In Pretest...");
            SetupSettings();
            Log("SetupSettings Done");
            SetupWriterOverload();
            Log("SetupWriterOverload Done");

            pstate = TestState.PreTest;
        }

        protected override void Test()
        {
            Log("Testing : " + TestFileName);
            Log("Overload : " + _overload);
            switch (_overload)
            {
                case WriterOverload.MemoryStream:
                    _stream = new MemoryStream();
                    CreateWriter(WriteThru.Stream);
                    break;

                case WriterOverload.StreamWriter:
                    _textWriter = new StreamWriter(new MemoryStream());
                    CreateWriter(WriteThru.TextWriter);
                    break;

                case WriterOverload.StringBuilder:
                    _stringBuilder = new StringBuilder();
                    CreateWriter(WriteThru.StringBuilder);
                    break;

                case WriterOverload.StringWriter:
                    _textWriter = new StringWriter();
                    CreateWriter(WriteThru.TextWriter);
                    break;

                case WriterOverload.UnicodeWriter:
                    _underlyingSettings = new XmlWriterSettings();
                    _underlyingSettings.Encoding = Encoding.Unicode;
                    _underlyingWriter = WriterHelper.Create(TestFileName, _underlyingSettings);
                    CreateWriter(WriteThru.XmlWriter);
                    break;

                case WriterOverload.UTF8Writer:
                    _underlyingSettings = new XmlWriterSettings();
                    _underlyingSettings.Encoding = Encoding.UTF8;
                    _underlyingWriter = WriterHelper.Create(TestFileName, _underlyingSettings);
                    CreateWriter(WriteThru.XmlWriter);
                    break;

                default:
                    throw new CTestFailedException("Unknown WriterOverload: " + _overload);
            }

            if (pstate == TestState.Pass)
                return;

            CError.Compare(pstate, TestState.CreateSuccess, "Invalid State after Create: " + pstate);

            //By this time the factory Reader is already set up correctly. So we must go Consume it now.
            CError.Compare(pstate != TestState.Pass && pstate == TestState.CreateSuccess, "Invalid state before Consuming Reader: " + pstate);

            //Call TestWriter to Consume Reader;
            TestWriter();
            if (pstate == TestState.Pass) return;
            CError.Compare(pstate != TestState.Pass && pstate == TestState.Consume, "Invalid state after Consuming Reader: " + pstate);
        }

        protected void TestWriter()
        {
            pstate = TestState.Consume;

            try
            {
                WriteNodes();
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
            catch (XmlException writerException)
            {
                Log(writerException.Message);
                Log(writerException.StackTrace);
                if (!IsVariationValid)
                {
                    if (!CheckException(writerException))
                    {
                        pstate = TestState.Error;
                        DumpVariationInfo();
                        throw new CTestFailedException("Invalid Exception Type thrown");
                    }
                    else
                    {
                        pstate = TestState.Pass;
                    }
                }
                else //Variation was valid
                {
                    pstate = TestState.Error;
                    DumpVariationInfo();
                    throw new CTestFailedException("Valid Variation throws Unspecified Exception");
                }
            }
            finally
            {
                if (_factoryWriter != null && _factoryWriter.WriteState != WriteState.Closed && _factoryWriter.WriteState != WriteState.Error)
                {
                    if (_textWriter == null)
                    {
                        CError.WriteLineIgnore(_factoryWriter.WriteState.ToString());
                        _factoryWriter.Flush();
                        _factoryWriter.Dispose();
                    }
                    else
                    {
                        _textWriter.Flush();
                        _textWriter.Dispose();
                    }
                }
                if (_underlyingWriter != null && _underlyingWriter.WriteState != WriteState.Closed && _underlyingWriter.WriteState != WriteState.Error)
                {
                    _underlyingWriter.Flush();
                    _underlyingWriter.Dispose();
                }
            }

            //If you are not in PASS state at this point you are in Error.
            if (pstate != TestState.Pass)
                pstate = TestState.Error;
        }

        protected void CompareSettings()
        {
            XmlWriterSettings actual = _factoryWriter.Settings;

            CError.Compare(actual.CheckCharacters, _settings.CheckCharacters, "CheckCharacters");

            //This actually checks Conformance Level DCR to some extent.
            if (_settings.ConformanceLevel != ConformanceLevel.Auto)
                CError.Compare(actual.ConformanceLevel, _settings.ConformanceLevel, "ConformanceLevel");

            CError.Compare(actual.Encoding, _settings.Encoding, "Encoding");
            CError.Compare(actual.Indent, _settings.Indent, "Indent");
            CError.Compare(actual.IndentChars, _settings.IndentChars, "IndentChars");
            CError.Compare(actual.NewLineChars, _settings.NewLineChars, "NewLineChars");
            CError.Compare(actual.NewLineOnAttributes, _settings.NewLineOnAttributes, "NewLineOnAttributes");
            CError.Compare(actual.NewLineHandling, _settings.NewLineHandling, "NormalizeNewLines");
            CError.Compare(actual.OmitXmlDeclaration, _settings.OmitXmlDeclaration, "OmitXmlDeclaration");
        }

        protected void CreateWriter(WriteThru writeThru)
        {
            // Assumption is that the Create method doesn't throw NullReferenceException and
            // it is not the goal of this framework to test if they are thrown anywhere.
            // but if they are thrown that's a problem and they shouldn't be caught but exposed.

            Log("Writing thru : " + writeThru);

            try
            {
                switch (writeThru)
                {
                    case WriteThru.Stream:
                        _factoryWriter = WriterHelper.Create(_stream, _settings);
                        break;
                    case WriteThru.StringBuilder:
                        _factoryWriter = WriterHelper.Create(_stringBuilder, _settings);
                        break;
                    case WriteThru.TextWriter:
                        _factoryWriter = WriterHelper.Create(_textWriter, _settings);
                        break;
                    case WriteThru.XmlWriter:
                        _factoryWriter = WriterHelper.Create(_underlyingWriter, _settings);
                        break;
                }

                pstate = TestState.CreateSuccess;
            }
            catch (Exception ane)
            {
                Log(ane.ToString());
                if (!IsVariationValid)
                {
                    if (!CheckException(ane))
                    {
                        pstate = TestState.Error;
                        DumpVariationInfo();
                        throw new CTestFailedException(
                                "Exception Thrown in CreateMethod, is your variation data correct?");
                    }
                    else
                    {
                        //This means that the Exception was checked and everything is fine.
                        pstate = TestState.Pass;
                    }
                }//Else valid variation threw exception
                else
                {
                    pstate = TestState.Error;
                    DumpVariationInfo();
                    throw new CTestFailedException(
                            "Exception Thrown in CreateMethod, is your variation data correct?");
                }
            }
        }

        protected override void PostTest()
        {
            pstate = TestState.Complete;
        }

        protected void SetupSettings()
        {
            _settings.ConformanceLevel = (ConformanceLevel)Enum.Parse(typeof(ConformanceLevel), ReadFilterCriteria("ConformanceLevel", true));
            _settings.CheckCharacters = bool.Parse(ReadFilterCriteria("CheckCharacters", true));
            _settings.CloseOutput = false;

            _settings.Indent = bool.Parse(ReadFilterCriteria("Indent", true));
            _settings.IndentChars = new string(Convert.ToChar(int.Parse(ReadFilterCriteria("IndentChars", true))), 1);
            _settings.NewLineChars = new string(Convert.ToChar(int.Parse(ReadFilterCriteria("NewLineChars", true))), 1);
            _settings.NewLineOnAttributes = bool.Parse(ReadFilterCriteria("NewLineOnAttributes", true));
            if (bool.Parse(ReadFilterCriteria("NormalizeNewlines", true)))
                _settings.NewLineHandling = NewLineHandling.Replace;
            else
                _settings.NewLineHandling = NewLineHandling.None;

            _settings.OmitXmlDeclaration = bool.Parse(ReadFilterCriteria("OmitXmlDeclaration", true));

            //Reading Writer Type to determine encoding and if the writer type is binary writer.
            string wt = ReadFilterCriteria("WriterType", true);
            if (wt == "TextWriter" || wt == "XmlDocumentWriter")
            {
                throw new CTestSkippedException("Skipped: WriterType " + wt);
            }
            WriterType writerType = (WriterType)Enum.Parse(typeof(WriterType), wt);
            switch (writerType)
            {
                case WriterType.UnicodeWriter:
                    _settings.Encoding = Encoding.Unicode;
                    break;
                default:
                    break;
            }
        }

        protected void SetupWriterOverload()
        {
            string ol = ReadFilterCriteria("Load", true);
            if (ol == "FileName" || ol == "XmlDocumentWriter" || ol == "InvalidUri")
            {
                throw new CTestSkippedException("Skipped: OverLoad " + ol);
            }
            _overload = (WriterOverload)Enum.Parse(typeof(WriterOverload), ol); //ReadFilterCriteria("Load", true));
        }


        /// <summary>
        /// This function writes the test nodes on the factoryWriter. 
        /// This will be called from Test(). If successful it will just return,
        /// else it will throw an appropriate XmlException. This function can use
        /// the knowledge of the current writertype to write appropriate data if 
        /// really needed.
        /// </summary>
        protected void WriteNodes()
        {
            _factoryWriter.WriteStartElement("a", "b", "c");
            _factoryWriter.WriteStartElement("d", "e");
            _factoryWriter.WriteStartElement("f");
            _factoryWriter.WriteStartAttribute("g", "h", "i");
            _factoryWriter.WriteStartAttribute("j", "k");
            _factoryWriter.WriteStartAttribute("l");
            _factoryWriter.WriteString("Some String");
            _factoryWriter.WriteEndElement();
            _factoryWriter.WriteRaw("<thisisraw/>");
            _factoryWriter.WriteProcessingInstruction("somepiname", "somepitext");
            _factoryWriter.WriteValue(1000);
            _factoryWriter.WriteComment("SomeComment");
            _factoryWriter.WriteEndElement();
            _factoryWriter.WriteCData("< is not a valid thing");
            _factoryWriter.WriteCharEntity('a');
            _factoryWriter.WriteWhitespace(" ");
            _factoryWriter.WriteEndElement();
        }
    }
}
