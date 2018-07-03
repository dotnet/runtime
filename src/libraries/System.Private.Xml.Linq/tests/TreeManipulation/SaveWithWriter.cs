// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XmlDiff;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;
using XmlCoreTest.Common;

namespace XLinqTests
{
    public class SaveWithWriter : XLinqTestCase
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+SaveWithWriter
        // Test Case

        #region Constants

        private const string BaseSaveFileName = "baseSave.xml";

        private const string TestSaveFileName = "testSave";

        private const string xml = "<PurchaseOrder><Item price=\"100\">Motor<![CDATA[cdata]]><elem>innertext</elem>text<?pi pi pi?></Item></PurchaseOrder>";

        private const string xmlForXElementWriteContent = "<Item price=\"100\">Motor<![CDATA[cdata]]><elem>innertext</elem>text<?pi pi pi?></Item>";

        #endregion

        #region Fields

        private readonly XmlDiff _diff;

        #endregion

        #region Constructors and Destructors

        public SaveWithWriter()
        {
            _diff = new XmlDiff();
        }

        #endregion

        #region Public Methods and Operators

        public static string[] GetExpectedXml()
        {
            string[] xml = { "text", "", "    ", "<!--comment1 comment1 -->", "<!---->", "<!--     -->", "<?pi1 pi1 pi1 ?>", "<?pi1?>", "<?pi1      ?>", "<![CDATA[cdata cdata ]]>", "<![CDATA[]]>", "<![CDATA[     ]]>", "<elem attr=\"val\">text<!--comm--><?pi hffgg?><![CDATA[jfggr]]></elem>" };
            return xml;
        }

        public static object[] GetObjects()
        {
            object[] objects = { new XText("text"), new XText(""), new XText("    "), new XComment("comment1 comment1 "), new XComment(""), new XComment("     "), new XProcessingInstruction("pi1", "pi1 pi1 "), new XProcessingInstruction("pi1", ""), new XProcessingInstruction("pi1", "     "), new XCData("cdata cdata "), new XCData(""), new XCData("     "), new XElement("elem", new XAttribute("attr", "val"), new XText("text"), new XComment("comm"), new XProcessingInstruction("pi", "hffgg"), new XCData("jfggr")) };
            return objects;
        }

        public override void AddChildren()
        {
            AddChild(new TestVariation(writer_5) { Attribute = new VariationAttribute("Write and valIdate XDocumentType") { Priority = 0 } });
            AddChild(new TestVariation(writer_18) { Attribute = new VariationAttribute("WriteTo after WriteState = Error") { Param = "WriteTo", Priority = 2 } });
            AddChild(new TestVariation(writer_18) { Attribute = new VariationAttribute("Save after WriteState = Error") { Param = "Save", Priority = 2 } });
            AddChild(new TestVariation(writer_20) { Attribute = new VariationAttribute("XDocument: Null parameters for Save") { Priority = 1 } });
            AddChild(new TestVariation(writer_21) { Attribute = new VariationAttribute("XElement: Null parameters for Save") { Priority = 1 } });
            AddChild(new TestVariation(writer_23) { Attribute = new VariationAttribute("XDocument: Null parameters for WriteTo") { Priority = 1 } });
            AddChild(new TestVariation(writer_24) { Attribute = new VariationAttribute("XElement: Null parameters for WriteTo") { Priority = 1 } });
        }

        public Encoding[] GetEncodings()
        {
            Encoding[] encodings = { Encoding.UTF8
                //Encoding.Unicode, 
                //Encoding.BigEndianUnicode,
            };

            return encodings;
        }

        //[Variation(Priority = 2, Desc = "Save after WriteState = Error", Param = "Save")]
        //[Variation(Priority = 2, Desc = "WriteTo after WriteState = Error", Param = "WriteTo")]
        public void writer_18()
        {
            var doc = new XDocument();
            foreach (Encoding encoding in GetEncodings())
            {
                foreach (XmlWriter w in GetXmlWriters(encoding))
                {
                    try
                    {
                        if (Variation.Param.ToString() == "Save")
                        {
                            doc.Save(w);
                        }
                        else
                        {
                            doc.WriteTo(w);
                        }
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (Exception ex)
                    {
                        if (!(ex is InvalidOperationException) && !(ex is ArgumentException))
                        {
                            throw;
                        }

                        TestLog.Equals(w.WriteState, WriteState.Error, "Error in WriteState");
                        try
                        {
                            if (Variation.Param.ToString() == "Save")
                            {
                                doc.Save(w);
                            }
                            else
                            {
                                doc.WriteTo(w);
                            }
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                    finally
                    {
                        w.Dispose();
                    }
                }
            }
        }

        //[Variation(Priority = 1, Desc = "XDocument: Null parameters for Save")]
        public void writer_20()
        {
            var doc = new XDocument(new XElement("PurchaseOrder"));
            //save with TextWriter              
            try
            {
                doc.Save((TextWriter)null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    doc.Save((TextWriter)null);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }
            try
            {
                doc.Save((TextWriter)null, SaveOptions.DisableFormatting);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    doc.Save((TextWriter)null, SaveOptions.DisableFormatting);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }
            try
            {
                doc.Save((TextWriter)null, SaveOptions.None);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    doc.Save((TextWriter)null, SaveOptions.None);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }
            //save with XmlWriter             
            try
            {
                doc.Save((XmlWriter)null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    doc.Save((XmlWriter)null);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }
        }

        //[Variation(Priority = 1, Desc = "XElement: Null parameters for Save")]
        public void writer_21()
        {
            var doc = new XElement(new XElement("PurchaseOrder"));
            //save with TextWriter              
            try
            {
                doc.Save((TextWriter)null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }
            try
            {
                doc.Save((TextWriter)null, SaveOptions.DisableFormatting);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                doc.Save((TextWriter)null, SaveOptions.None);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }

            //save with XmlWriter             
            try
            {
                doc.Save((XmlWriter)null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }
        }

        //[Variation(Priority = 1, Desc = "XDocument: Null parameters for WriteTo")]
        public void writer_23()
        {
            var doc = new XDocument(new XElement("PurchaseOrder"));
            try
            {
                doc.WriteTo(null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }
        }

        //[Variation(Priority = 1, Desc = "XElement: Null parameters for WriteTo")]
        public void writer_24()
        {
            var doc = new XElement(new XElement("PurchaseOrder"));
            try
            {
                doc.WriteTo(null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }
        }

        //[Variation(Priority = 0, Desc = "Write and valIdate XDocumentType")]
        public void writer_5()
        {
            string expectedXml = "<!DOCTYPE root PUBLIC \"\" \"\"[<!ELEMENT root ANY>]>";
            var doc = new XDocumentType("root", "", "", "<!ELEMENT root ANY>");

            TestToStringAndXml(doc, expectedXml);

            var ws = new XmlWriterSettings();
            ws.ConformanceLevel = ConformanceLevel.Document;
            ws.OmitXmlDeclaration = true;
            // Set to true when FileIO is ok
            ws.CloseOutput = false;

            // Use a file to replace this when FileIO is ok
            var m = new MemoryStream();
            using (XmlWriter wr = XmlWriter.Create(m, ws))
            {
                doc.WriteTo(wr);
            }
            m.Position = 0;
            using (TextReader r = new StreamReader(m))
            {
                string actualXml = r.ReadToEnd();
                TestLog.Compare(expectedXml, actualXml, "XDocumentType writeTo method failed");
            }
        }
        #endregion

        //
        // helpers 
        //       

        #region Methods

        private string GenerateTestFileName(int index)
        {
            string filename = string.Format("{0}{1}.xml", TestSaveFileName, index);
            try
            {
                FilePathUtil.getStream(filename);
            }
            catch (Exception)
            {
                FilePathUtil.addStream(filename, new MemoryStream());
            }
            return filename;
        }

        private List<XmlWriter> GetXmlWriters(Encoding encoding)
        {
            return GetXmlWriters(encoding, ConformanceLevel.Document);
        }

        private List<XmlWriter> GetXmlWriters(Encoding encoding, ConformanceLevel conformanceLevel)
        {
            var xmlWriters = new List<XmlWriter>();
            int count = 0;

            var s = new XmlWriterSettings();
            s.CheckCharacters = true;
            s.Encoding = encoding;

            var ws = new XmlWriterSettings();
            ws.CheckCharacters = false;
            // Set it to true when FileIO is ok
            ws.CloseOutput = false;
            ws.Encoding = encoding;

            s.ConformanceLevel = conformanceLevel;
            ws.ConformanceLevel = conformanceLevel;

            TextWriter tw = new StreamWriter(FilePathUtil.getStream(GenerateTestFileName(count++)));
            xmlWriters.Add(new CoreXml.Test.XLinq.CustomWriter(tw, ws)); // CustomWriter                  
            xmlWriters.Add(XmlWriter.Create(FilePathUtil.getStream(GenerateTestFileName(count++)), s)); // Factory XmlWriter       
            xmlWriters.Add(XmlWriter.Create(FilePathUtil.getStream(GenerateTestFileName(count++)), ws)); // Factory Writer   

            return xmlWriters;
        }

        private void SaveBaseline(string xml)
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            sw.Write(xml);
            sw.Flush();
            FilePathUtil.addStream(BaseSaveFileName, ms);
        }

        private void SaveWithFile(object doc)
        {
            string file0 = GenerateTestFileName(0);
            string file1 = GenerateTestFileName(1);
            string file2 = GenerateTestFileName(2);

            foreach (Encoding encoding in GetEncodings())
            {
                if (doc is XDocument)
                {
                    ((XDocument)doc).Save(FilePathUtil.getStream(file0));
                    ((XDocument)doc).Save(FilePathUtil.getStream(file1), SaveOptions.DisableFormatting);
                    ((XDocument)doc).Save(FilePathUtil.getStream(file2), SaveOptions.None);
                }
                else if (doc is XElement)
                {
                    ((XElement)doc).Save(FilePathUtil.getStream(file0));
                    ((XElement)doc).Save(FilePathUtil.getStream(file1), SaveOptions.DisableFormatting);
                    ((XElement)doc).Save(FilePathUtil.getStream(file2), SaveOptions.None);
                }
                else
                {
                    TestLog.Compare(false, "Wrong object");
                }
                TestLog.Compare(_diff.Compare(FilePathUtil.getStream(BaseSaveFileName), FilePathUtil.getStream(file0)), "Save failed:encoding " + encoding);
                TestLog.Compare(_diff.Compare(FilePathUtil.getStream(BaseSaveFileName), FilePathUtil.getStream(file1)), "Save(preserveWhitespace true) failed:encoding " + encoding);
                TestLog.Compare(_diff.Compare(FilePathUtil.getStream(BaseSaveFileName), FilePathUtil.getStream(file2)), "Save(preserveWhitespace false) " + encoding);
            }
        }

        private void SaveWithTextWriter(object doc)
        {
            string file0 = GenerateTestFileName(0);
            string file1 = GenerateTestFileName(1);
            string file2 = GenerateTestFileName(2);

            foreach (Encoding encoding in GetEncodings())
            {
                using (TextWriter w0 = new StreamWriter(FilePathUtil.getStream(file0)))
                using (TextWriter w1 = new StreamWriter(FilePathUtil.getStream(file1)))
                using (TextWriter w2 = new StreamWriter(FilePathUtil.getStream(file2)))
                {
                    if (doc is XDocument)
                    {
                        ((XDocument)doc).Save(w0);
                        ((XDocument)doc).Save(w1, SaveOptions.DisableFormatting);
                        ((XDocument)doc).Save(w2, SaveOptions.None);
                    }
                    else if (doc is XElement)
                    {
                        ((XElement)doc).Save(w0);
                        ((XElement)doc).Save(w1, SaveOptions.DisableFormatting);
                        ((XElement)doc).Save(w2, SaveOptions.None);
                    }
                    else
                    {
                        TestLog.Compare(false, "Wrong object");
                    }

                    w0.Dispose();
                    w1.Dispose();
                    w2.Dispose();

                    TestLog.Compare(_diff.Compare(FilePathUtil.getStream(BaseSaveFileName), FilePathUtil.getStream(file0)), "TextWriter failed:encoding " + encoding);
                    TestLog.Compare(_diff.Compare(FilePathUtil.getStream(BaseSaveFileName), FilePathUtil.getStream(file1)), "TextWriter(preserveWhtsp=true) failed:encoding " + encoding);
                    TestLog.Compare(_diff.Compare(FilePathUtil.getStream(BaseSaveFileName), FilePathUtil.getStream(file2)), "TextWriter(preserveWhtsp=false) failed:encoding " + encoding);
                }
            }
        }

        private void SaveWithXmlWriter(object doc)
        {
            foreach (Encoding encoding in GetEncodings())
            {
                List<XmlWriter> xmlWriters = GetXmlWriters(encoding);
                try
                {
                    for (int i = 0; i < xmlWriters.Count; ++i)
                    {
                        if (doc is XDocument)
                        {
                            ((XDocument)doc).Save(xmlWriters[i]);
                        }
                        else if (doc is XElement)
                        {
                            ((XElement)doc).Save(xmlWriters[i]);
                        }
                        else
                        {
                            TestLog.Compare(false, "Wrong object");
                        }

                        xmlWriters[i].Dispose();
                        if (doc is XDocument)
                        {
                            ((XDocument)doc).Save(FilePathUtil.getStream(GenerateTestFileName(i)));
                        }
                        else if (doc is XElement)
                        {
                            ((XElement)doc).Save(FilePathUtil.getStream(GenerateTestFileName(i)));
                        }
                        else
                        {
                            TestLog.Compare(false, "Wrong object");
                        }

                        TestLog.Skip("ReEnable this when FileIO is OK");
                    }
                }
                finally
                {
                    for (int i = 0; i < xmlWriters.Count; ++i)
                    {
                        xmlWriters[i].Dispose();
                    }
                }
            }
        }

        private void TestToStringAndXml(object doc, string expectedXml)
        {
            string toString = doc.ToString();
            string xml = string.Empty;

            if (doc is XDocument)
            {
                xml = ((XDocument)doc).ToString(SaveOptions.DisableFormatting);
            }
            else if (doc is XElement)
            {
                xml = ((XElement)doc).ToString(SaveOptions.DisableFormatting);
            }
            else if (doc is XText)
            {
                xml = ((XText)doc).ToString(SaveOptions.DisableFormatting);
            }
            else if (doc is XCData)
            {
                xml = ((XCData)doc).ToString(SaveOptions.DisableFormatting);
            }
            else if (doc is XComment)
            {
                xml = ((XComment)doc).ToString(SaveOptions.DisableFormatting);
            }
            else if (doc is XProcessingInstruction)
            {
                xml = ((XProcessingInstruction)doc).ToString(SaveOptions.DisableFormatting);
            }
            else if (doc is XDocumentType)
            {
                xml = ((XDocumentType)doc).ToString(SaveOptions.DisableFormatting);
            }
            else
            {
                TestLog.Compare(false, "Wrong object");
            }

            TestLog.Compare(toString, expectedXml, "Test ToString failed");
            TestLog.Compare(xml, expectedXml, "Test .ToString(SaveOptions.DisableFormatting) failed");
        }
        #endregion
    }
}
