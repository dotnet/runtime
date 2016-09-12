// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Text;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    [TestModule(Name = "ReaderSettings Test", Desc = "ReaderSettings Test")]
    public partial class CReaderTestModule : CGenericTestModule
    {
        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);
            // Create global usage test files
            string strFile = String.Empty;
            // Create reader factory
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.GENERIC);
            ReaderFactory = new ReaderSettingsFactory();

            return ret;
        }

        public override int Terminate(object objParam)
        {
            // Remove global usage test files
            return base.Terminate(objParam);
        }
    }

    internal class ReaderSettingsFactory : ReaderFactory
    {
        public override XmlReader Create(MyDict<string, object> options)
        {
            XmlReaderSettings settings = (XmlReaderSettings)options[ReaderFactory.HT_READERSETTINGS];
            if (settings == null)
                settings = new XmlReaderSettings();

            Stream stream = (Stream)options[ReaderFactory.HT_STREAM];
            string filename = (string)options[ReaderFactory.HT_FILENAME];
            object readerType = options[ReaderFactory.HT_READERTYPE];
            string fragment = (string)options[ReaderFactory.HT_FRAGMENT];

            if (stream != null)
            {
                XmlReader reader = ReaderHelper.Create(stream, settings, filename);
                return reader;
            }

            if (fragment != null)
            {
                StringReader tr = new StringReader(fragment);
                XmlReader reader = ReaderHelper.Create(tr, settings, "someUri");

                return reader;
            }

            if (filename != null)
            {
                XmlReader reader = ReaderHelper.Create(filename, settings);
                return reader;
            }

            throw new CTestFailedException("No Reader Created");
        }
    }

    //[TestCase("ReaderSettings Generic Tests.CoreReader", Param = "CoreReader")]
    //[TestCase("ReaderSettings Generic Tests.CharCheckingReader", Param = "CharCheckingReader")]
    //[TestCase("ReaderSettings Generic Tests.WrappedReader", Param = "WrappedReader")]
    //[TestCase("ReaderSettings Generic Tests.SubtreeReader", Param = "SubtreeReader")]
    //[TestCase("ReaderSettings Generic Tests.CoreValidatingReader", Param = "CoreValidatingReader")]
    //[TestCase("ReaderSettings Generic Tests.XsdValidatingReader", Param = "XsdValidatingReader")]
    public partial class TCReaderSettings : TCXMLReaderBaseGeneral
    {
        public int v1()
        {
            string readerType = (string)this.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader("<root>abc</root>"), false))
            {
                CError.WriteLine(r.GetType().ToString());
                CError.Compare((r.Settings != null), "Settings is null");
            }
            return TEST_PASS;
        }
    }

    //[TestCase("CloseInput.CoreReader", Param = "CoreReader")]
    //[TestCase("CloseInput.CharCheckingReader", Param = "CharCheckingReader")]
    //[TestCase("CloseInput.WrappedReader", Param = "WrappedReader")]
    //[TestCase("CloseInput.SubtreeReader", Param = "SubtreeReader")]
    //[TestCase("CloseInput.CoreValidatingReader", Param = "CoreValidatingReader")]
    //[TestCase("CloseInput.XsdValidatingReader", Param = "XsdValidatingReader")]  
    //[TestCase("CloseInput.XmlTextReader", Param = "XmlTextReader")]
    //[TestCase("CloseInput.XPathNavigatorReader", Param = "XPathNavigatorReader")]
    //[TestCase("CloseInput.XsltReader", Param = "XsltReader")]
    //[TestCase("CloseInput.XmlNodeReader", Param = "XmlNodeReader")]
    //[TestCase("CloseInput.XmlBinaryReader", Param = "XmlBinaryReader")] 
    public partial class TCCloseInput : TCXMLReaderBaseGeneral
    {
        [Variation("Default Values", Priority = 0)]
        public int v1()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            return (settings.CloseInput == true) ? TEST_FAIL : TEST_PASS;
        }
    }

    //[TestCase("ReaderSettings Generic Tests.CoreReader", Param = "CoreReader")]
    //[TestCase("ReaderSettings Generic Tests.CharCheckingReader", Param = "CharCheckingReader")]
    //[TestCase("ReaderSettings Generic Tests.WrappedReader", Param = "WrappedReader")]
    //[TestCase("ReaderSettings Generic Tests.SubtreeReader", Param = "SubtreeReader")]
    //[TestCase("ReaderSettings Generic Tests.CoreValidatingReader", Param = "CoreValidatingReader")]
    //[TestCase("ReaderSettings Generic Tests.XsdValidatingReader", Param = "XsdValidatingReader")]   
    public partial class TCRSGeneric : TCXMLReaderBaseGeneral
    {
        [Variation("ReaderSettings not null", Priority = 0)]
        public int v1()
        {
            string readerType = (string)this.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader("<root>abc</root>"), false))
            {
                CError.WriteLine(r.GetType().ToString());
                CError.Compare((r.Settings != null), "Settings is null");
            }
            return TEST_PASS;
        }

        [Variation("Wrapping scenario")]
        public int WrappingScenario()
        {
            if (AsyncUtil.IsAsyncEnabled)
                return TEST_SKIPPED;
            string readerType = (string)this.Param;
            XmlReaderSettings ReaderSettings = new XmlReaderSettings();
            ReaderSettings.CheckCharacters = true;
            ReaderSettings.IgnoreProcessingInstructions = true;
            ReaderSettings.IgnoreComments = true;
            ReaderSettings.IgnoreWhitespace = true;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader("<root/>"), false, null, ReaderSettings))
            {
                using (XmlReader r2 = ReaderHelper.Create(r, ReaderSettings)) { }
            }
            return TEST_PASS;
        }

        [Variation("Reset", Priority = 0)]
        public int v3()
        {
            string readerType = (string)this.Param;
            XmlReaderSettings rs = new XmlReaderSettings();
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader("<a/>"), false, null, rs))
            {
                bool cc = r.Settings.CheckCharacters;
                bool closeinput = r.Settings.CloseInput;
                DtdProcessing dtd = r.Settings.DtdProcessing;
                bool ignorecomm = r.Settings.IgnoreComments;
                bool ignorepi = r.Settings.IgnoreProcessingInstructions;
                bool ignorewhtsp = r.Settings.IgnoreWhitespace;
                int lineNumberOffset = r.Settings.LineNumberOffset;
                int linePositionOffset = r.Settings.LinePositionOffset;
                long maxcharsindoc = r.Settings.MaxCharactersInDocument;
                XmlNameTable nameTable = r.Settings.NameTable;
                ConformanceLevel cl = r.Settings.ConformanceLevel;
                Type t = r.Settings.GetType();
                rs.Reset();
                CError.Compare(cc, rs.CheckCharacters, "cc");
                CError.Compare(closeinput, rs.CloseInput, "closeinput");
                CError.Compare(dtd, rs.DtdProcessing, "dtd");
                CError.Compare(ignorecomm, rs.IgnoreComments, "ignorecomm");
                CError.Compare(ignorepi, rs.IgnoreProcessingInstructions, "ignorepi");
                CError.Compare(ignorewhtsp, rs.IgnoreWhitespace, "ignorewhtsp");
                CError.Compare(lineNumberOffset, rs.LineNumberOffset, "lineNumberOffset");
                CError.Compare(linePositionOffset, rs.LinePositionOffset, "linePositionOffset");
                CError.Compare(maxcharsindoc, rs.MaxCharactersInDocument, "maxcharsindoc");
                CError.Compare(nameTable, rs.NameTable, "nameTable");
                CError.Compare(cl, rs.ConformanceLevel, "cl");
                CError.Compare(t, rs.GetType(), "t");
                return TEST_PASS;
            }
        }

        [Variation("Clone", Priority = 0)]
        public int v4()
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReaderSettings crs = rs.Clone();
            CError.Compare(rs.CheckCharacters, crs.CheckCharacters, "CheckCharacters");
            CError.Compare(rs.CloseInput, crs.CloseInput, "CloseInput");
            CError.Compare(rs.DtdProcessing, crs.DtdProcessing, "ProhibitDtd");
            CError.Compare(rs.IgnoreComments, crs.IgnoreComments, "IgnoreComments");
            CError.Compare(rs.IgnoreProcessingInstructions, crs.IgnoreProcessingInstructions, "IgnorePI");
            CError.Compare(rs.IgnoreWhitespace, crs.IgnoreWhitespace, "IgnoreWhitespace");
            CError.Compare(rs.LineNumberOffset, crs.LineNumberOffset, "LineNumberOffset");
            CError.Compare(rs.LinePositionOffset, crs.LinePositionOffset, "LinePositionOffset");
            CError.Compare(rs.MaxCharactersInDocument, crs.MaxCharactersInDocument, "maxcharsindoc");
            CError.Compare(rs.NameTable, crs.NameTable, "NameTable");
            CError.Compare(rs.ConformanceLevel, crs.ConformanceLevel, "ConformanceLevel");
            CError.Compare(rs.GetType(), crs.GetType(), "GetType");
            return TEST_PASS;
        }

        [Variation("NameTable", Priority = 0)]
        public int v5()
        {
            XmlNameTable nt = null;
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.NameTable = nt;
            return TEST_PASS;
        }
    }

    //[TestCase("TCDtdProcessingCoreReader.CoreReader", Param = "CoreReader")]
    //[TestCase("TCDtdProcessingCoreReader.CharCheckingReader", Param = "CharCheckingReader")]
    //[TestCase("TCDtdProcessingCoreReader.WrappedReader", Param = "WrappedReader")]
    //[TestCase("TCDtdProcessingCoreReader.SubtreeReader", Param = "SubtreeReader")]
    //[TestCase("TCDtdProcessingCoreReader.CoreValidatingReader", Param = "CoreValidatingReader")]
    //[TestCase("TCDtdProcessingCoreReader.XsdValidatingReader", Param = "XsdValidatingReader")]    
    public partial class TCDtdProcessingCoreReader : TCXMLReaderBaseGeneral
    {
        //[Variation("Read xml without DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Read xml without DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Read xml without DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v0()
        {
            string readerType = (string)this.Param;
            string strXml = "<root><a xmlns:b=\"abc\"><b:c /></a></root>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (StringWriter strWriter = new StringWriter())
            {
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
                {
                    using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                    {
                        w.WriteNode(r, false);
                    }
                    CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                }
                CError.Compare(strWriter.ToString(), strXml, "error");
            }
            return TEST_PASS;
        }

        //[Variation("Wrap with Prohibit, xml w/o DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Prohibit, xml w/o DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Prohibit, xml w/o DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v1a()
        {
            string readerType = (string)this.Param;
            string strXml = "<root><a xmlns:b=\"abc\"><b:c /></a></root>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                XmlReaderSettings s = new XmlReaderSettings();
                s.DtdProcessing = DtdProcessing.Prohibit;

                XmlWriterSettings ws = new XmlWriterSettings();
                ws.OmitXmlDeclaration = true;
                using (StringWriter strWriter = new StringWriter())
                {
                    using (XmlReader wr = ReaderHelper.Create(r, s))
                    {
                        using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                        {
                            w.WriteNode(wr, false);
                        }
                        CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                        CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, "error2");
                    }
                    CError.Compare(strWriter.ToString(), strXml, "error");
                }
            }
            return TEST_PASS;
        }

        //[Variation("Wrap with Ignore, xml w/o DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Ignore, xml w/o DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Ignore, xml w/o DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v1b()
        {
            string readerType = (string)this.Param;
            string strXml = "<root><a xmlns:b=\"abc\"><b:c /></a></root>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                XmlReaderSettings s = new XmlReaderSettings();
                s.DtdProcessing = DtdProcessing.Ignore;

                XmlWriterSettings ws = new XmlWriterSettings();
                ws.OmitXmlDeclaration = true;
                using (StringWriter strWriter = new StringWriter())
                {
                    using (XmlReader wr = ReaderHelper.Create(r, s))
                    {
                        using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                        {
                            w.WriteNode(wr, false);
                        }
                        CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                        CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, DtdProcessing.Ignore, "error2");
                    }
                    CError.Compare(strWriter.ToString(), strXml, "error");
                }
            }
            return TEST_PASS;
        }

        //[Variation("Wrap with Prohibit, change RS, xml w/o DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Prohibit, change RS, xml w/o DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Prohibit, change RS, xml w/o DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v1d()
        {
            string readerType = (string)this.Param;
            string strXml = "<root><a xmlns:b=\"abc\"><b:c /></a></root>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                u.DtdProcessing = DtdProcessing.Prohibit;
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.OmitXmlDeclaration = true;
                using (StringWriter strWriter = new StringWriter())
                {
                    using (XmlReader wr = ReaderHelper.Create(r, u))
                    {
                        using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                        {
                            w.WriteNode(wr, false);
                        }
                        CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                        CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, "error2");
                    }
                    CError.Compare(strWriter.ToString(), strXml, "error");
                }
            }
            return TEST_PASS;
        }

        //[Variation("Wrap with Ignore, change RS, xml w/o DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Ignore, change RS, xml w/o DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Ignore, change RS, xml w/o DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v1e()
        {
            string readerType = (string)this.Param;
            string strXml = "<root><a xmlns:b=\"abc\"><b:c /></a></root>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                u.DtdProcessing = DtdProcessing.Ignore;
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.OmitXmlDeclaration = true;
                using (StringWriter strWriter = new StringWriter())
                {
                    using (XmlReader wr = ReaderHelper.Create(r, u))
                    {
                        using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                        {
                            w.WriteNode(wr, false);
                        }
                        CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                        CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, DtdProcessing.Ignore, "error2");
                    }
                    CError.Compare(strWriter.ToString(), strXml, "error");
                }
            }
            return TEST_PASS;
        }

        //[Variation("Wrap with Prohibit, xml with DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Prohibit, xml with DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Prohibit, xml with DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v2a()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE root [<!ELEMENT root ANY>]><root><a xmlns:b=\"abc\"><b:c /></a></root>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                XmlReaderSettings s = new XmlReaderSettings();
                s.DtdProcessing = DtdProcessing.Prohibit;
                using (XmlReader wr = ReaderHelper.Create(r, s))
                {
                    try
                    {
                        while (wr.Read()) ;
                        CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error0");
                        CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, "error00");
                        return TEST_PASS;
                    }
                    catch (XmlException)
                    {
                        CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                        return TEST_PASS;
                    }
                }
            }
        }

        //[Variation("Wrap with Ignore, xml with DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Ignore, xml with DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Ignore, xml with DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v2b()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE root [<!ELEMENT root ANY>]><root><a xmlns:b=\"abc\"><b:c /></a></root>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                XmlReaderSettings s = new XmlReaderSettings();
                s.DtdProcessing = DtdProcessing.Ignore;
                using (XmlReader wr = ReaderHelper.Create(r, s))
                {
                    try
                    {
                        while (wr.Read()) ;
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLine(e);
                        CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "error1");
                        CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, "error2");
                        return TEST_PASS;
                    }
                    CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, "error3");
                    CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Ignore, "error4");
                }
            }
            return TEST_PASS;
        }

        [Variation("Testing default values.")]
        public int V3()
        {
            string readerType = (string)this.Param;
            string strXml = "<ROOT/>";
            XmlReaderSettings rs = new XmlReaderSettings();
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
            {
                CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "DtdProcessing");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a file with inline DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a file with inline DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a file with inline DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V4()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = "<?xml version='1.0'?>\n<!DOCTYPE ROOT[\n  <!ELEMENT a ANY>\n]> \n<ROOT/>";
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
            {
                try
                {
                    while (r.Read()) ;
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e);
                    CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "error");
                    return TEST_PASS;
                }
                CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, "error2");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a xml with inline inv.DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a xml with inline inv.DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a xml with inline inv.DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V4c()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = @"<?xml version='1.0' encoding='utf-8'?><!DOCTYPE r [<!ATTLIST a b CDATA #FIXED - >]><r></r>";

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
            {
                try
                {
                    while (r.Read()) ;
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e);
                    CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "error");
                    return TEST_PASS;
                }
                CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, "error2");
            }
            return TEST_PASS;
        }

        //[Variation("Read xml with invalid content.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Read xml with invalid content.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Read xml with invalid content.Ignore", Param = DtdProcessing.Ignore)]
        public int V4i()
        {
            string readerType = (string)this.Param;
            string strXml = "<root>&#;</root>";
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
            {
                try
                {
                    while (r.Read()) ;
                    CError.Compare(false, "error");
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e);
                    CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error2");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        //[Variation("Changing DtdProcessing to Prohibit,Ignore.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Changing DtdProcessing to Prohibit,Ignore.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Changing DtdProcessing to Prohibit,Ignore.Ignore", Param = DtdProcessing.Ignore)]
        public int V7a()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE doc [ <!ELEMENT doc ANY >]><doc><![CDATA[< <<]]></doc>";

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            rs.DtdProcessing = DtdProcessing.Prohibit;
            rs.DtdProcessing = DtdProcessing.Ignore;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (StringWriter strWriter = new StringWriter())
            {
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
                {
                    using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                    {
                        w.WriteNode(r, false);
                    }
                    CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, "error1");
                }
                CError.Compare(strWriter.ToString(), "<doc><![CDATA[< <<]]></doc>", "error");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a file with external DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a file with external DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a file with external DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V8()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = "<?xml version='1.0'?>\n<!DOCTYPE ROOT SYSTEM 'some.dtd'>\n<ROOT/>";

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
            {
                try
                {
                    while (r.Read()) ;
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e);
                    CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "error");
                    return TEST_PASS;
                }
                CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, "error2");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a file with invalid inline DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a file with invalid inline DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a file with invalid inline DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V9()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = "<?xml version='1.0'?>\n<!DOCTYPE ROOT[\n  <!ELEMENT a MANY>\n]> \n<ROOT/>"; //Wrong keyword MANY

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
            {
                try
                {
                    while (r.Read()) ;
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e);
                    CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "error");
                    return TEST_PASS;
                }
                CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, "error2");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a valid xml with predefined entities with no DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a valid xml with predefined entities with no DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a valid xml with predefined entities with no DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V11()
        {
            string readerType = (string)this.Param;
            string strXml = "<?xml version='1.0'?>\n<root>&#xD;<a>&#xA;<b>&#xA;<c>&#xA;</c></b></a></root>";

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
            {
                while (r.Read()) ;
                CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a valid xml with entity and DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a valid xml with entity and DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a valid xml with entity and DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V11a()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE doc [  <!ELEMENT doc ANY>  <!ENTITY book \"some\">]><doc>&book;</doc>";
            string exp = "<!DOCTYPE doc [  <!ELEMENT doc ANY>  <!ENTITY book \"some\">]><doc>some</doc>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (StringWriter strWriter = new StringWriter())
            {
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
                {
                    using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                    {
                        try
                        {
                            w.WriteNode(r, false);
                        }
                        catch (XmlException e)
                        {
                            CError.WriteLine(e);
                            CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, DtdProcessing.Prohibit, "error2");
                            return TEST_PASS;
                        }
                    }
                }
                CError.Compare(strWriter.ToString(), exp, "error");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a valid xml with entity in attribute and DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a valid xml with entity in attribute and DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a valid xml with entity in attribute and DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V11b()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE ROOT [<!ENTITY a 'some'>]><ROOT att=\"&a;\"/>";
            string exp = "<!DOCTYPE ROOT [<!ENTITY a 'some'>]><ROOT att=\"some\" />";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (StringWriter strWriter = new StringWriter())
            {
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
                {
                    using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                    {
                        try
                        {
                            w.WriteNode(r, false);
                        }
                        catch (XmlException e)
                        {
                            CError.WriteLine(e);
                            CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, DtdProcessing.Prohibit, "error2");
                            return TEST_PASS;
                        }
                    }
                }
                CError.Compare(strWriter.ToString(), exp, "error");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a invalid xml with entity in attribute and DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a invalid xml with entity in attribute and DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a invalid xml with entity in attribute and DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V11c()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE ROOT [<!ENTITY a '&a;'>]><ROOT att=\"&a;\"/>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (StringWriter strWriter = new StringWriter())
            {
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
                {
                    using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                    {
                        try
                        {
                            w.WriteNode(r, false);
                            CError.Compare(false, "error");
                        }
                        catch (XmlException e)
                        {
                            CError.WriteLine(e);
                            CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error2");
                            return TEST_PASS;
                        }
                    }
                }
            }
            return TEST_FAIL;
        }

        //[Variation("Set value to Reader.Settings.DtdProcessing.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Set value to Reader.Settings.DtdProcessing.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Set value to Reader.Settings.DtdProcessing.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v12()
        {
            string readerType = (string)this.Param;
            string strXml = "<?xml version='1.0'?><test> a </test>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                try
                {
                    r.Settings.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
                    CError.Compare(false, "error");
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e);
                    CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error2");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        [Variation("DtdProcessing - ArgumentOutOfRangeException")]
        public int V14()
        {
            XmlReaderSettings xrs = new XmlReaderSettings();
            try
            {
                xrs.DtdProcessing = (DtdProcessing)777;
                CError.Compare(false, "error");
            }
            catch (ArgumentOutOfRangeException)
            {
                try
                {
                    xrs.DtdProcessing = (DtdProcessing)777;
                    CError.Compare(false, "error2");
                }
                catch (ArgumentOutOfRangeException)
                {
                    CError.Equals(xrs.DtdProcessing, DtdProcessing.Prohibit, "DtdProcessing");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        //[Variation("DtdProcessing - ArgumentOutOfRangeException.Parse", Param = DtdProcessing.Parse)]
        //[Variation("DtdProcessing - ArgumentOutOfRangeException.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("DtdProcessing - ArgumentOutOfRangeException.Ignore", Param = DtdProcessing.Ignore)]
        public int V15()
        {
            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            try
            {
                xrs.DtdProcessing = (DtdProcessing)777;
                CError.Compare(false, "error");
            }
            catch (ArgumentOutOfRangeException)
            {
                try
                {
                    xrs.DtdProcessing = (DtdProcessing)777;
                    CError.Compare(false, "error2");
                }
                catch (ArgumentOutOfRangeException)
                {
                    CError.Equals(xrs.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "DtdProcessing");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        //[Variation("Parse a valid xml DTD and check NodeType.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a valid xml DTD and check NodeType.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a valid xml DTD and check NodeType.Ignore", Param = DtdProcessing.Ignore)]
        public int V16()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE ROOT [<!ENTITY a 'some'>]><ROOT att=\"&a;\"/>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                try
                {
                    while (r.Read())
                    {
                        CError.Compare(r.NodeType, XmlNodeType.DocumentType, "error1");
                        return TEST_PASS;
                    }
                }
                catch (XmlException)
                {
                    CError.Compare(r.NodeType, XmlNodeType.None, XmlNodeType.Element, "error3");
                    CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, DtdProcessing.Ignore, "error4");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        public static string strXml = "<!DOCTYPE doc SYSTEM 'test::rootDtd'><doc></doc>";

        //[Variation("Parse a invalid xml DTD SYSTEM PUBLIC.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a invalid xml DTD SYSTEM PUBLIC.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a invalid xml DTD SYSTEM PUBLIC.Ignore", Param = DtdProcessing.Ignore)]
        public int V18()
        {
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE root SYSTEM 'a.dtd' PUBLIC 'some' []><root/>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                try
                {
                    while (r.Read())
                    {
                        CError.Compare(r.NodeType, XmlNodeType.DocumentType, "error1");
                    }
                }
                catch (XmlException)
                {
                    CError.Compare(r.NodeType, XmlNodeType.None, XmlNodeType.Element, "error3");
                    CError.Compare(r.Settings.DtdProcessing, u.DtdProcessing, "error4");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        //[Variation("1.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 1 })]
        //[Variation("1.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 1 })]
        //[Variation("1.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 1 })]
        //[Variation("2.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 2 })]
        //[Variation("2.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 2 })]
        //[Variation("2.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 2 })]
        //[Variation("3.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 3 })]
        //[Variation("3.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 3 })]
        //[Variation("3.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 3 })]
        //[Variation("4.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 4 })]
        //[Variation("4.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 4 })]
        //[Variation("4.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 4 })]
        //[Variation("5.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 5})]
        //[Variation("5.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 5 })]
        //[Variation("5.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 5 })]
        //[Variation("6.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 6 })]
        //[Variation("6.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 6 })]
        //[Variation("6.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 6 })]
        //[Variation("7.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 7 })]
        //[Variation("7.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 7 })]
        //[Variation("7.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 7 })]
        //[Variation("8.Parsing invalid xml version.Parse", Params = new object[] { DtdProcessing.Parse, 8 })]
        //[Variation("8.Parsing invalid xml version.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 8 })]
        //[Variation("8.PParsing invalid xml version.Ignore", Params = new object[] { DtdProcessing.Ignore, 8 })]
        //[Variation("9.Parsing invalid xml version.Parse", Params = new object[] { DtdProcessing.Parse, 9 })]
        //[Variation("9.Parsing invalid xml version.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 9 })]
        //[Variation("9.Parsing invalid xml version.Ignore", Params = new object[] { DtdProcessing.Ignore, 9 })]
        //[Variation("10.Parsing invalid xml version.Parse", Params = new object[] { DtdProcessing.Parse, 10 })]
        //[Variation("10.Parsing invalid xml version.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 10 })]
        //[Variation("10.Parsing invalid xml version.Ignore", Params = new object[] { DtdProcessing.Ignore, 10 })]
        //[Variation("11.Parsing invalid xml version.Parse", Params = new object[] { DtdProcessing.Parse, 11 })]
        //[Variation("11.Parsing invalid xml version.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 11 })]
        //[Variation("11.Parsing invalid xml version.Ignore", Params = new object[] { DtdProcessing.Ignore, 11 })]
        //[Variation("12.Parsing invalid xml version.Parse", Params = new object[] { DtdProcessing.Parse, 12 })]
        //[Variation("12.Parsing invalid xml version.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 12 })]
        //[Variation("12.Parsing invalid xml version.Ignore", Params = new object[] { DtdProcessing.Ignore, 12 })]
        public int V19()
        {
            string xml = "";
            switch ((int)CurVariation.Params[1])
            {
                case 1: xml = "<!DOCTYPE <"; break;
                case 2: xml = "<!DOCTYPE root SYSTEM"; break;
                case 3: xml = "<!DOCTYPE []<root/>"; break;
                case 4: xml = "<!DOCTYPE root PUBLIC >]>"; break;
                case 5: xml = "<!DOCTYPE "; break;
                case 6: xml = "<!DOCTYPE >"; break;
                case 7: xml = "<!DOCTYPE ["; break;
                case 8: xml = " <?xml version=\"1.0\"     ?>"; break;
                case 9: xml = "<?xml version='1.0'                 ?><!DOCTYPE doc [ <!ELEMENT doc ANY >"; break;
                case 10: xml = "< ?xml version=\"1.0\"     ?>"; break;
                case 11: xml = "<? xml version=\"1.0\"     ?>"; break;
                case 12: xml = "<?xml version      =     \"   1.0       \"     ?>"; break;
            }
            string readerType = (string)this.Param;
            if (readerType == "SubtreeReader") return TEST_SKIPPED;

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Params[0];

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(xml), false, null, u))
            {
                try
                {
                    while (r.Read()) ;
                }
                catch (XmlException)
                {
                    CError.Compare(r.Settings.DtdProcessing, u.DtdProcessing, "error4");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }
    }

    //[TestCase("TCDtdProcessingNonCoreReader.XmlTextReader", Param = "XmlTextReader")]
    //[TestCase("TCDtdProcessingNonCoreReader.XmlValidatingReader", Param = "XmlValidatingReader")]
    //[TestCase("TCDtdProcessingNonCoreReader.XmlNodeReader", Param = "XmlNodeReader")]
    //[TestCase("TCDtdProcessingNonCoreReader.XsltReader", Param = "XsltReader")]
    public partial class TCDtdProcessingNonCoreReader : TCXMLReaderBaseGeneral
    {
        //[Variation("Read xml without DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Read xml without DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Read xml without DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v0()
        {
            string readerType = (string)this.Param;
            string strXml = "<root><a xmlns:b=\"abc\"><b:c /></a></root>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (StringWriter strWriter = new StringWriter())
            {
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
                {
                    using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                    {
                        w.WriteNode(r, false);
                    }
                    if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                }
                CError.Compare(strWriter.ToString(), strXml, "error");
            }
            return TEST_PASS;
        }

        //[Variation("Wrap with Prohibit, xml w/o DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Prohibit, xml w/o DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Prohibit, xml w/o DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v1a()
        {
            string readerType = (string)this.Param;
            if (readerType == "XmlNodeReader" || readerType == "XmlValidatingReader") return TEST_SKIPPED;

            string strXml = "<root><a xmlns:b=\"abc\"><b:c /></a></root>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                XmlReaderSettings s = new XmlReaderSettings();
                s.DtdProcessing = DtdProcessing.Prohibit;

                XmlWriterSettings ws = new XmlWriterSettings();
                ws.OmitXmlDeclaration = true;
                using (StringWriter strWriter = new StringWriter())
                {
                    using (XmlReader wr = ReaderHelper.CreateReader(readerType, r, false, null, s))
                    {
                        using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                        {
                            w.WriteNode(wr, false);
                        }
                        if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                        if (wr.Settings != null) CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, "error2");
                    }
                    CError.Compare(strWriter.ToString(), strXml, "error");
                }
            }
            return TEST_PASS;
        }

        //[Variation("Wrap with Ignore, xml w/o DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Ignore, xml w/o DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Ignore, xml w/o DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v1b()
        {
            string readerType = (string)this.Param;
            if (readerType == "XmlNodeReader" || readerType == "XmlValidatingReader") return TEST_SKIPPED;

            string strXml = "<root><a xmlns:b=\"abc\"><b:c /></a></root>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                XmlReaderSettings s = new XmlReaderSettings();
                s.DtdProcessing = DtdProcessing.Ignore;

                XmlWriterSettings ws = new XmlWriterSettings();
                ws.OmitXmlDeclaration = true;
                using (StringWriter strWriter = new StringWriter())
                {
                    using (XmlReader wr = ReaderHelper.CreateReader(readerType, r, false, null, s))
                    {
                        using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                        {
                            w.WriteNode(wr, false);
                        }
                        if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                        if (wr.Settings != null) CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, DtdProcessing.Ignore, "error2");
                    }
                    CError.Compare(strWriter.ToString(), strXml, "error");
                }
            }
            return TEST_PASS;
        }

        //[Variation("Wrap with Prohibit, change RS, xml w/o DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Prohibit, change RS, xml w/o DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Prohibit, change RS, xml w/o DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v1d()
        {
            string readerType = (string)this.Param;
            if (readerType == "XmlNodeReader" || readerType == "XmlValidatingReader") return TEST_SKIPPED;

            string strXml = "<root><a xmlns:b=\"abc\"><b:c /></a></root>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                u.DtdProcessing = DtdProcessing.Prohibit;
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.OmitXmlDeclaration = true;
                using (StringWriter strWriter = new StringWriter())
                {
                    using (XmlReader wr = ReaderHelper.CreateReader(readerType, r, false, null, u))
                    {
                        using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                        {
                            w.WriteNode(wr, false);
                        }
                        if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                        if (wr.Settings != null) CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, "error2");
                    }
                    CError.Compare(strWriter.ToString(), strXml, "error");
                }
            }
            return TEST_PASS;
        }

        //[Variation("Wrap with Ignore, change RS, xml w/o DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Ignore, change RS, xml w/o DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Ignore, change RS, xml w/o DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v1e()
        {
            string readerType = (string)this.Param;
            if (readerType == "XmlNodeReader" || readerType == "XmlValidatingReader") return TEST_SKIPPED;

            string strXml = "<root><a xmlns:b=\"abc\"><b:c /></a></root>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                u.DtdProcessing = DtdProcessing.Ignore;
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.OmitXmlDeclaration = true;
                using (StringWriter strWriter = new StringWriter())
                {
                    using (XmlReader wr = ReaderHelper.CreateReader(readerType, r, false, null, u))
                    {
                        using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                        {
                            w.WriteNode(wr, false);
                        }
                        if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                        if (wr.Settings != null) CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, DtdProcessing.Ignore, "error2");
                    }
                    CError.Compare(strWriter.ToString(), strXml, "error");
                }
            }
            return TEST_PASS;
        }

        //[Variation("Wrap with Prohibit, xml with DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Prohibit, xml with DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Prohibit, xml with DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v2a()
        {
            string readerType = (string)this.Param;
            if (readerType == "XmlNodeReader" || readerType == "XmlValidatingReader") return TEST_SKIPPED;
            string strXml = "<?xml version='1.0'?>\n<!DOCTYPE ROOT[\n  <!ELEMENT ROOT ANY>\n]> \n<ROOT>abc 123</ROOT>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                XmlReaderSettings s = new XmlReaderSettings();
                s.DtdProcessing = DtdProcessing.Prohibit;
                using (XmlReader wr = ReaderHelper.CreateReader(readerType, r, false, null, s))
                {
                    try
                    {
                        while (wr.Read()) ;
                        if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error0");
                        if (wr.Settings != null) CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, "error00");
                        return TEST_PASS;
                    }
                    catch (XmlException)
                    {
                        if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error1");
                        if (wr.Settings != null) CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, "error2");
                        return TEST_PASS;
                    }
                }
            }
        }

        //[Variation("Wrap with Ignore, xml with DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Wrap with Ignore, xml with DTD.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Wrap with Ignore, xml with DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v2b()
        {
            string readerType = (string)this.Param;
            if (readerType == "XmlNodeReader" || readerType == "XmlValidatingReader") return TEST_SKIPPED;
            string strXml = "<?xml version='1.0'?>\n<!DOCTYPE ROOT[\n  <!ELEMENT ROOT ANY>\n]> \n<ROOT>abc 123</ROOT>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                XmlReaderSettings s = new XmlReaderSettings();
                s.DtdProcessing = DtdProcessing.Ignore;
                using (XmlReader wr = ReaderHelper.CreateReader(readerType, r, false, null, s))
                {
                    try
                    {
                        while (wr.Read()) ;
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLine(e);
                        if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "error1");
                        if (wr.Settings != null) CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Prohibit, "error2");
                        return TEST_PASS;
                    }
                    if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, "error3");
                    if (wr.Settings != null) CError.Compare(wr.Settings.DtdProcessing, DtdProcessing.Ignore, "error4");
                }
            }
            return TEST_PASS;
        }

        [Variation("Testing default values")]
        public int V3()
        {
            string readerType = (string)this.Param;
            string strXml = "<?xml version='1.0'?>\n<!DOCTYPE ROOT[\n  <!ELEMENT a ANY>\n]> \n<ROOT/>";
            XmlReaderSettings rs = new XmlReaderSettings();
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
            {
                if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "DtdProcessing");
            }
            return TEST_PASS;
        }

        //[Variation("Read xml with invalid content.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Read xml with invalid content.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Read xml with invalid content.Ignore", Param = DtdProcessing.Ignore)]
        public int V4i()
        {
            string readerType = (string)this.Param;
            string strXml = "<root>&#;</root>";
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            XmlReader r = null;
            try
            {
                r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs);
                while (r.Read()) ;
                CError.Compare(false, "error");
            }
            catch (XmlException e)
            {
                CError.WriteLine(e);
                if (r != null && r.Settings != null) CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error2");
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        //[Variation("Changing DtdProcessing to Parse, Prohibit.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Changing DtdProcessing to Parse, Prohibit.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Changing DtdProcessing to Parse, Prohibit.Ignore", Param = DtdProcessing.Ignore)]
        public int V5()
        {
            string readerType = (string)this.Param;
            if (readerType == "XsltReader") return TEST_SKIPPED;
            string strXml = "<?xml version='1.0'?>\n<!DOCTYPE ROOT[\n  <!ELEMENT ROOT ANY>\n]> \n<ROOT/>";

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            rs.DtdProcessing = DtdProcessing.Prohibit;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
            {
                try
                {
                    while (r.Read()) ;
                    if (r.Settings != null) CError.Compare(false, "error2");
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e);
                    if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "error");
                    return TEST_PASS;
                }
            }
            return TEST_PASS;
        }

        //[Variation("Changing DtdProcessing to Prohibit,Parse.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Changing DtdProcessing to Prohibit,Parse.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Changing DtdProcessing to Prohibit,Parse.Ignore", Param = DtdProcessing.Ignore)]
        public int V6()
        {
            string readerType = (string)this.Param;
            if (readerType == "XsltReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE doc [  <!ELEMENT doc ANY>  <!ENTITY book ''>  <!ATTLIST doc    JSmith CDATA #FIXED ''     date CDATA #IMPLIED>]><doc JSmith=\"\" date=\"\"> &book; </doc>";
            string exp = "<!DOCTYPE doc [  <!ELEMENT doc ANY>  <!ENTITY book ''>  <!ATTLIST doc    JSmith CDATA #FIXED ''     date CDATA #IMPLIED>]><doc JSmith=\"\" date=\"\">  </doc>";

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            rs.DtdProcessing = DtdProcessing.Prohibit;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (StringWriter strWriter = new StringWriter())
            {
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
                {
                    using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                    {
                        w.WriteNode(r, false);
                    }
                }
                CError.Compare(strWriter.ToString(), (readerType == "XmlTextReader") ? strXml : exp, "error");
            }
            return TEST_PASS;
        }

        //[Variation("Changing DtdProcessing to Parse, Ignore.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Changing DtdProcessing to Parse, Ignore.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Changing DtdProcessing to Parse, Ignore.Ignore", Param = DtdProcessing.Ignore)]
        public int V7()
        {
            string readerType = (string)this.Param;
            string strXml = "<!DOCTYPE doc [  <!ELEMENT doc ANY>  <!ENTITY book ''>  <!ATTLIST doc    JSmith CDATA #FIXED ''     date CDATA #IMPLIED>]><doc JSmith='' date=''> &book; </doc>  ";

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            rs.DtdProcessing = DtdProcessing.Ignore;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (StringWriter strWriter = new StringWriter())
            {
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
                {
                    using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                    {
                        try
                        {
                            w.WriteNode(r, false);
                            if (r.Settings != null) CError.Compare(false, "error");
                        }
                        catch (XmlException e)
                        {
                            CError.WriteLine(e);
                            if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, "error1");
                            return TEST_PASS;
                        }
                    }
                }
            }
            return TEST_PASS;
        }

        //[Variation("Changing DtdProcessing to Prohibit,Ignore.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Changing DtdProcessing to Prohibit,Ignore.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Changing DtdProcessing to Prohibit,Ignore.Ignore", Param = DtdProcessing.Ignore)]
        public int V7a()
        {
            string readerType = (string)this.Param;
            if (readerType == "XsltReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE doc [ <!ELEMENT doc ANY >]><doc><![CDATA[< <<]]></doc>";
            string exp =
                (readerType == "XmlNodeReader" || readerType == "XmlValidatingReader" || readerType == "XmlTextReader") ?
                "<!DOCTYPE doc [ <!ELEMENT doc ANY >]><doc><![CDATA[< <<]]></doc>" : "<doc><![CDATA[< <<]]></doc>";

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            rs.DtdProcessing = DtdProcessing.Prohibit;
            rs.DtdProcessing = DtdProcessing.Ignore;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (StringWriter strWriter = new StringWriter())
            {
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
                {
                    using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                    {
                        w.WriteNode(r, false);
                    }
                    if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, "error1");
                }
                CError.Compare(strWriter.ToString(), exp, "error");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a file with external DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a file with external DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a file with external DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V8()
        {
            string readerType = (string)this.Param;
            string strXml = "<?xml version='1.0'?>\n<!DOCTYPE ROOT SYSTEM 'some.dtd'>\n<ROOT/>";

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            XmlReader r = null;
            try
            {
                using (r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
                {
                    while (r.Read()) ;
                }
            }
            catch (XmlException e)
            {
                CError.WriteLine(e);
                if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "error");
                return TEST_PASS;
            }
            catch (FileNotFoundException e)
            {
                CError.WriteLine(e);
                if (r != null && r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "error");
                return TEST_PASS;
            }
            if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, "error2");
            return TEST_PASS;
        }

        //[Variation("Parse a file with invalid inline DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a file with invalid inline DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a file with invalid inline DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V9()
        {
            string readerType = (string)this.Param;
            string strXml = "<?xml version='1.0'?>\n<!DOCTYPE ROOT[\n  <!ELEMENT a MANY>\n]> \n<ROOT/>"; //Wrong keyword MANY

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            XmlReader r = null;
            try
            {
                r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs);
                while (r.Read()) ;
            }
            catch (XmlException e)
            {
                CError.WriteLine(e);
                if (r != null && r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, "error");
                return TEST_PASS;
            }
            if (r != null && r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, "error2");
            return TEST_PASS;
        }

        //[Variation("Parse a valid xml with predefined entities with no DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a valid xml with predefined entities with no DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a valid xml with predefined entities with no DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V11()
        {
            string readerType = (string)this.Param;
            string strXml = "<?xml version='1.0'?>\n<root>&#xD;<a>&#xA;<b>&#xA;<c>&#xA;</c></b></a></root>";

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, rs))
            {
                while (r.Read()) ;
                if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a valid xml with entity and DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a valid xml with entity and DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a valid xml with entity and DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V11a()
        {
            string readerType = (string)this.Param;
            if (readerType == "XsltReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE doc [  <!ELEMENT doc ANY>  <!ENTITY book \"some\">]><doc>&book;</doc>";
            string exp = "<!DOCTYPE doc [  <!ELEMENT doc ANY>  <!ENTITY book \"some\">]><doc>some</doc>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (StringWriter strWriter = new StringWriter())
            {
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
                {
                    using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                    {
                        try
                        {
                            w.WriteNode(r, false);
                        }
                        catch (XmlException e)
                        {
                            CError.WriteLine(e);
                            if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, DtdProcessing.Prohibit, "error2");
                            return TEST_PASS;
                        }
                    }
                }
                CError.Compare(strWriter.ToString(), (readerType != "XmlTextReader") ? exp : strXml, "error");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a valid xml with entity in attribute and DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a valid xml with entity in attribute and DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a valid xml with entity in attribute and DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V11b()
        {
            string readerType = (string)this.Param;
            if (readerType == "XsltReader" || readerType == "XmlNodeReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE ROOT [<!ELEMENT ROOT ANY><!ATTRIBUTE att ANY><!ENTITY a 'some'>]><ROOT att=\"&a;\" />";
            string exp = "<!DOCTYPE ROOT [<!ELEMENT ROOT ANY><!ATTRIBUTE att ANY><!ENTITY a 'some'>]><ROOT att=\"some\" />";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (StringWriter strWriter = new StringWriter())
            {
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
                {
                    using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                    {
                        try
                        {
                            w.WriteNode(r, false);
                        }
                        catch (XmlException e)
                        {
                            CError.WriteLine(e);
                            if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Ignore, DtdProcessing.Prohibit, "error2");
                            return TEST_PASS;
                        }
                    }
                }
                CError.Compare(strWriter.ToString(), (readerType != "XmlTextReader") ? exp : strXml, "error");
            }
            return TEST_PASS;
        }

        //[Variation("Parse a invalid xml with entity in attribute and DTD.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a invalid xml with entity in attribute and DTD.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a invalid xml with entity in attribute and DTD.Ignore", Param = DtdProcessing.Ignore)]
        public int V11c()
        {
            string readerType = (string)this.Param;
            if (readerType == "XsltReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE ROOT [<!ENTITY a '&a;'>]><ROOT att=\"&a;\"/>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            XmlReader r = null;
            using (StringWriter strWriter = new StringWriter())
            {
                try
                {
                    using (r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
                    {
                        using (XmlWriter w = WriterHelper.Create(strWriter, ws))
                        {
                            w.WriteNode(r, false);
                            if (readerType != "XmlTextReader") CError.Compare(false, "error");
                        }
                    }
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e);
                    if (r != null && r.Settings != null) CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error2");
                    return TEST_PASS;
                }
            }
            return (readerType != "XmlTextReader") ? TEST_FAIL : TEST_PASS;
        }

        //[Variation("Set value to Reader.Settings.DtdProcessing.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Set value to Reader.Settings.DtdProcessing.Ignore", Param = DtdProcessing.Ignore)]
        //[Variation("Set value to Reader.Settings.DtdProcessing.Prohibit", Param = DtdProcessing.Prohibit)]
        public int v12()
        {
            string readerType = (string)this.Param;
            string strXml = "<?xml version='1.0'?><test> a </test>";
            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                try
                {
                    if (r.Settings != null) r.Settings.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
                    if (r.Settings != null) CError.Compare(false, "error");
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e);
                    if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "error2");
                    return TEST_PASS;
                }
            }
            return TEST_PASS;
        }

        [Variation("DtdProcessing - ArgumentOutOfRangeException")]
        public int V14()
        {
            XmlReaderSettings xrs = new XmlReaderSettings();
            try
            {
                xrs.DtdProcessing = (DtdProcessing)777;
                CError.Compare(false, "error");
            }
            catch (ArgumentOutOfRangeException)
            {
                try
                {
                    xrs.DtdProcessing = (DtdProcessing)777;
                    CError.Compare(false, "error2");
                }
                catch (ArgumentOutOfRangeException)
                {
                    CError.Equals(xrs.DtdProcessing, DtdProcessing.Prohibit, "DtdProcessing");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        //[Variation("DtdProcessing - ArgumentOutOfRangeException.Parse", Param = DtdProcessing.Parse)]
        //[Variation("DtdProcessing - ArgumentOutOfRangeException.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("DtdProcessing - ArgumentOutOfRangeException.Ignore", Param = DtdProcessing.Ignore)]
        public int V15()
        {
            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            try
            {
                xrs.DtdProcessing = (DtdProcessing)777;
                CError.Compare(false, "error");
            }
            catch (ArgumentOutOfRangeException)
            {
                try
                {
                    xrs.DtdProcessing = (DtdProcessing)777;
                    CError.Compare(false, "error2");
                }
                catch (ArgumentOutOfRangeException)
                {
                    CError.Equals(xrs.DtdProcessing, (DtdProcessing)this.CurVariation.Param, "DtdProcessing");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        //[Variation("Parse a valid xml DTD and check NodeType.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a valid xml DTD and check NodeType.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a valid xml DTD and check NodeType.Ignore", Param = DtdProcessing.Ignore)]
        public int V16()
        {
            string readerType = (string)this.Param;
            if (readerType == "XsltReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE ROOT [<!ENTITY a 'some'>]><ROOT att=\"&a;\"/>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;

            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u))
            {
                try
                {
                    while (r.Read())
                    {
                        CError.Compare(r.NodeType, XmlNodeType.DocumentType, XmlNodeType.Element, "error1");
                        return TEST_PASS;
                    }
                }
                catch (XmlException)
                {
                    CError.Compare(r.NodeType, XmlNodeType.None, XmlNodeType.Element, "error3");
                    if (r.Settings != null) CError.Compare(r.Settings.DtdProcessing, DtdProcessing.Prohibit, DtdProcessing.Ignore, "error4");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        public static string strXml = "<!DOCTYPE doc SYSTEM 'test::rootDtd'><doc></doc>";

        //[Variation("Parse a invalid xml DTD SYSTEM PUBLIC.Parse", Param = DtdProcessing.Parse)]
        //[Variation("Parse a invalid xml DTD SYSTEM PUBLIC.Prohibit", Param = DtdProcessing.Prohibit)]
        //[Variation("Parse a invalid xml DTD SYSTEM PUBLIC.Ignore", Param = DtdProcessing.Ignore)]
        public int V18()
        {
            string readerType = (string)this.Param;
            if (readerType == "XsltReader" || readerType == "XmlNodeReader") return TEST_SKIPPED;
            string strXml = "<!DOCTYPE root SYSTEM 'a.dtd' PUBLIC 'some' []><root/>";

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Param;
            XmlReader r = null;
            try
            {
                r = ReaderHelper.CreateReader(readerType, new StringReader(strXml), false, null, u);
                while (r.Read())
                {
                    CError.Compare(r.NodeType, XmlNodeType.DocumentType, "error1");
                }
            }
            catch (XmlException)
            {
                if (r != null) CError.Compare(r.NodeType, XmlNodeType.None, XmlNodeType.Element, "error3");
                if (r != null && r.Settings != null) CError.Compare(r.Settings.DtdProcessing, u.DtdProcessing, "error4");
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        //[Variation("1.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 1 })]
        //[Variation("1.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 1 })]
        //[Variation("1.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 1 })]
        //[Variation("2.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 2 })]
        //[Variation("2.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 2 })]
        //[Variation("2.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 2 })]
        //[Variation("3.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 3 })]
        //[Variation("3.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 3 })]
        //[Variation("3.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 3 })]
        //[Variation("4.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 4 })]
        //[Variation("4.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 4 })]
        //[Variation("4.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 4 })]
        //[Variation("5.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 5 })]
        //[Variation("5.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 5 })]
        //[Variation("5.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 5 })]
        //[Variation("6.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 6 })]
        //[Variation("6.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 6 })]
        //[Variation("6.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 6 })]
        //[Variation("7.Parsing invalid DOCTYPE.Parse", Params = new object[] { DtdProcessing.Parse, 7 })]
        //[Variation("7.Parsing invalid DOCTYPE.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 7 })]
        //[Variation("7.Parsing invalid DOCTYPE.Ignore", Params = new object[] { DtdProcessing.Ignore, 7 })]
        //[Variation("8.Parsing invalid xml version.Parse", Params = new object[] { DtdProcessing.Parse, 8 })]
        //[Variation("8.Parsing invalid xml version.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 8 })]
        //[Variation("8.PParsing invalid xml version.Ignore", Params = new object[] { DtdProcessing.Ignore, 8 })]
        //[Variation("9.Parsing invalid xml version.Parse", Params = new object[] { DtdProcessing.Parse, 9 })]
        //[Variation("9.Parsing invalid xml version.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 9 })]
        //[Variation("9.Parsing invalid xml version.Ignore", Params = new object[] { DtdProcessing.Ignore, 9 })]
        //[Variation("10.Parsing invalid xml version.Parse", Params = new object[] { DtdProcessing.Parse, 10 })]
        //[Variation("10.Parsing invalid xml version.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 10 })]
        //[Variation("10.Parsing invalid xml version.Ignore", Params = new object[] { DtdProcessing.Ignore, 10 })]
        //[Variation("11.Parsing invalid xml version.Parse", Params = new object[] { DtdProcessing.Parse, 11 })]
        //[Variation("11.Parsing invalid xml version.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 11 })]
        //[Variation("11.Parsing invalid xml version.Ignore", Params = new object[] { DtdProcessing.Ignore, 11 })]
        //[Variation("12.Parsing invalid xml version.Parse", Params = new object[] { DtdProcessing.Parse, 12 })]
        //[Variation("12.Parsing invalid xml version.Prohibit", Params = new object[] { DtdProcessing.Prohibit, 12 })]
        //[Variation("12.Parsing invalid xml version.Ignore", Params = new object[] { DtdProcessing.Ignore, 12 })]
        public int V19()
        {
            string xml = "";
            switch ((int)CurVariation.Params[1])
            {
                case 1: xml = "<!DOCTYPE <"; break;
                case 2: xml = "<!DOCTYPE root SYSTEM"; break;
                case 3: xml = "<!DOCTYPE []<root/>"; break;
                case 4: xml = "<!DOCTYPE root PUBLIC >]>"; break;
                case 5: xml = "<!DOCTYPE "; break;
                case 6: xml = "<!DOCTYPE >"; break;
                case 7: xml = "<!DOCTYPE ["; break;
                case 8: xml = " <?xml version=\"1.0\"     ?>"; break;
                case 9: xml = "<?xml version='1.0'                 ?><!DOCTYPE doc [ <!ELEMENT doc ANY >"; break;
                case 10: xml = "< ?xml version=\"1.0\"     ?>"; break;
                case 11: xml = "<? xml version=\"1.0\"     ?>"; break;
                case 12: xml = "<?xml version      =     \"   1.0       \"     ?>"; break;
            }
            string readerType = (string)this.Param;

            XmlReaderSettings u = new XmlReaderSettings();
            u.DtdProcessing = (DtdProcessing)this.CurVariation.Params[0];
            XmlReader r = null;
            try
            {
                r = ReaderHelper.CreateReader(readerType, new StringReader(xml), false, null, u);
                while (r.Read()) ;
            }
            catch (XmlException)
            {
                if (r != null && r.Settings != null) CError.Compare(r.Settings.DtdProcessing, u.DtdProcessing, "error4");
                return TEST_PASS;
            }
            return TEST_FAIL;
        }
    }

    //[TestCase("Read xml as one byte stream.CoreReader", Param = "CoreReader")]
    //[TestCase("Read xml as one byte stream.CharCheckingReader", Param = "CharCheckingReader")]
    //[TestCase("Read xml as one byte stream.WrappedReader", Param = "WrappedReader")]
    //[TestCase("Read xml as one byte stream.SubtreeReader", Param = "SubtreeReader")]
    //[TestCase("Read xml as one byte stream.CoreValidatingReader", Param = "CoreValidatingReader")]
    //[TestCase("Read xml as one byte stream.XsdValidatingReader", Param = "XsdValidatingReader")]
    //[TestCase("Read xml as one byte stream.XsltReader", Param = "XsltReader")]
    //[TestCase("Read xml as one byte stream.XmlValidatingReader", Param = "XmlValidatingReader")]
    //[TestCase("Read xml as one byte stream.XmlNodeReader", Param = "XmlNodeReader")]
    //[TestCase("Read xml as one byte stream.XPathNavigatorReader", Param = "XPathNavigatorReader")]
    //[TestCase("Read xml as one byte stream.XmlTextReader", Param = "XmlTextReader")]
    //[TestCase("Read xml as one byte stream.XmlBinaryReader", Param = "XmlBinaryReader")]
    public partial class TCOneByteStream : TCXMLReaderBaseGeneral
    {
        [Variation("445370: Parsing this 'some]' as fragment fails with 'Unexpected EOF' error")]
        public int v0()
        {
            string readerType = (string)this.Param;
            if (AsyncUtil.IsAsyncEnabled)
            {
                if (readerType == "XmlBinaryReader")
                {
                    return TEST_SKIPPED;
                }
            }
            if (readerType == "SubtreeReader" || readerType == "XmlNodeReader") return TEST_SKIPPED;
            OneByteStream sim = new OneByteStream(new byte[] { 0xFE, 0xFF, 0, (byte)'s', 0, (byte)'o',
            0, (byte)'s', 0, (byte)'o', 0, (byte)'s', 0, (byte)'o', 0, (byte)'s', 0, (byte)'o',
            0, (byte)']'});

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ConformanceLevel = ConformanceLevel.Fragment;
            using (XmlReader r = ReaderHelper.CreateReader(readerType, sim, null, false, null, rs, true))
            {
                while (r.Read()) { CError.WriteLine(r.Value); }
            }
            return TEST_PASS;
        }

        [Variation("445370a: Parsing this 'some]' as fragment fails with 'Unexpected EOF' error")]
        public int v0a()
        {
            string readerType = (string)this.Param;
            if (AsyncUtil.IsAsyncEnabled)
            {
                if (readerType == "XmlBinaryReader")
                {
                    return TEST_SKIPPED;
                }
            }
            if (readerType == "SubtreeReader" || readerType == "XmlNodeReader") return TEST_SKIPPED;
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ConformanceLevel = ConformanceLevel.Fragment;

            string[] s = { "sosososo]", "sosososo]]", "sososos]o", "]", "[", "][", "[]", " ]]", "[[", "sosososo[", "sosososo[[", "Last char a square bracket. ]", ". ]" };
            for (int i = 0; i < s.Length; i++)
            {
                CError.WriteLine(s[i]);
                using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(s[i]), false, null, rs, true))
                {
                    r.Read();
                    CError.WriteLine(r.Value);
                }
            }
            return TEST_PASS;
        }

        [Variation("Read as one byte stream xml with surrogate char")]
        public int v1()
        {
            string readerType = (string)this.Param;
            if (AsyncUtil.IsAsyncEnabled)
            {
                if (readerType == "XmlBinaryReader")
                {
                    return TEST_SKIPPED;
                }
            }
            string str = "<abc abc='\uD812\uDD12'>\uD812\uDD12</abc>";
            byte[] bytes = Encoding.Unicode.GetBytes(str);
            XmlReaderSettings rs = new XmlReaderSettings();

            OneByteStream sim = new OneByteStream(bytes);
            using (XmlReader r = ReaderHelper.CreateReader(readerType, sim, null, false, null, rs, false))
            {
                while (r.Read()) { CError.WriteLine(r.Value); }
                return TEST_PASS;
            }
        }

        [Variation("Read as TextReader xml with surrogate char")]
        public int v1a()
        {
            string readerType = (string)this.Param;
            if (AsyncUtil.IsAsyncEnabled)
            {
                if (readerType == "XmlBinaryReader")
                {
                    return TEST_SKIPPED;
                }
            }
            string str = "<abc abc='\uD812\uDD12'>\uD812\uDD12</abc>";
            XmlReaderSettings rs = new XmlReaderSettings();
            using (XmlReader r = ReaderHelper.CreateReader(readerType, new StringReader(str), false, null, rs, false))
            {
                while (r.Read()) { CError.WriteLine(r.Value); }
                return TEST_PASS;
            }
        }

        [Variation("XmlWriter.WriteNode: read as one byte stream xml with surrogate char")]
        public int v2()
        {
            string readerType = (string)this.Param;
            if (AsyncUtil.IsAsyncEnabled)
            {
                if (readerType == "XmlBinaryReader")
                {
                    return TEST_SKIPPED;
                }
            }
            string str = "<abc abc='\uD812\uDD12'>\uD812\uDD12</abc>";
            string exp = "<?xml version=\"1.0\" encoding=\"utf-16\"?><abc abc=\"\U00014912\">\U00014912</abc>";
            exp = (readerType == "XmlBinaryReader") ? "<?xml version=\"1.0\" encoding=\"utf-8\"?><abc abc=\"\U00014912\">\U00014912</abc>" : exp;
            byte[] bytes = Encoding.Unicode.GetBytes(str);
            XmlReaderSettings rs = new XmlReaderSettings();

            OneByteStream sim = new OneByteStream(bytes);
            using (XmlReader r = ReaderHelper.CreateReader(readerType, sim, null, false, null, rs, false))
            {
                using (StringWriter sw = new StringWriter())
                {
                    using (XmlWriter w = WriterHelper.Create(sw))
                    {
                        w.WriteNode(r, false);
                    }
                    CError.Compare(sw.ToString(), exp, "writer output");
                }
            }
            return TEST_PASS;
        }
    }

    internal class OneByteStream : System.IO.Stream
    {
        private byte[] _input;
        private int _pos;

        public OneByteStream(byte[] input)
        {
            _input = input;
            _pos = 0;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public override void Flush()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override long Length
        {
            get { return _input.Length; }
        }

        public override long Position
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int tocopy = count;
            if (tocopy > _input.Length - _pos)
            {
                tocopy = _input.Length - _pos;
            }
            if (tocopy > 4)
            {
                tocopy = 4;
            }

            int i;
            for (i = 0; i < tocopy; i++)
            {
                buffer[offset + i] = _input[_pos + i];
            }
            _pos += i;
            return i;
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void SetLength(long value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
