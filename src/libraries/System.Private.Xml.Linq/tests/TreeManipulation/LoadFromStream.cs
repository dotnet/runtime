// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;
using XmlCoreTest.Common;

namespace XLinqTests
{
    public class LoadFromStream : XLinqTestCase
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+LoadFromStream
        // Test Case

        #region Constants

        private const string purchaseOrderXml = @"<PurchaseOrder><Item price='100'>Motor<![CDATA[cdata]]><elem>inner text</elem>text<?pi pi pi?></Item></PurchaseOrder>";

        #endregion

        #region Fields

        private readonly XDocument _purchaseOrder = new XDocument(new XElement("PurchaseOrder", new XElement("Item", "Motor", new XAttribute("price", "100"), new XCData("cdata"), new XElement("elem", "inner text"), new XText("text"), new XProcessingInstruction("pi", "pi pi"))));

        #endregion

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(StreamStateAfterLoading) { Attribute = new VariationAttribute("Stream state after loading") { Priority = 0 } });
            AddChild(new TestVariation(XDEncodings) { Attribute = new VariationAttribute("XDocument.Load() Encodings: UTF8, UTF16, UTF16BE") { Priority = 0 } });
            AddChild(new TestVariation(XEEncodings) { Attribute = new VariationAttribute("XElement.Load() Encodings: UTF8, UTF16, UTF16BE") { Priority = 0 } });
            AddChild(new TestVariation(LoadOptionsPWS) { Attribute = new VariationAttribute("XDocument.Load(), Load options, preserveWhitespace, Stream") { Param = "Stream", Priority = 0 } });
            AddChild(new TestVariation(LoadOptionsPWS) { Attribute = new VariationAttribute("XDocument.Load(), Load options, preserveWhitespace, Uri") { Param = "Uri", Priority = 0 } });
            AddChild(new TestVariation(LoadOptionsBU) { Attribute = new VariationAttribute("XDocument.Load(), Load options, BaseUri, Uri") { Param = "Uri", Priority = 0 } });
            AddChild(new TestVariation(LoadOptionsLI) { Attribute = new VariationAttribute("XDocument.Load(), Load options, LineInfo, Uri") { Param = "Uri", Priority = 0 } });
            AddChild(new TestVariation(LoadOptionsLI) { Attribute = new VariationAttribute("XDocument.Load(), Load options, LineInfo, Stream") { Param = "Stream", Priority = 0 } });
            AddChild(new TestVariation(XE_LoadOptionsPWS) { Attribute = new VariationAttribute("XElement.Load(), Load options, preserveWhitespace, Uri") { Param = "Uri", Priority = 0 } });
            AddChild(new TestVariation(XE_LoadOptionsPWS) { Attribute = new VariationAttribute("XElement.Load(), Load options, preserveWhitespace, Stream") { Param = "Stream", Priority = 0 } });
            AddChild(new TestVariation(XE_LoadOptionsBU) { Attribute = new VariationAttribute("XElement.Load(), Load options, BaseUri, Uri") { Param = "Uri", Priority = 0 } });
            AddChild(new TestVariation(XE_LoadOptionsLI) { Attribute = new VariationAttribute("XElement.Load(), Load options, LineInfo, Stream") { Param = "Stream", Priority = 0 } });
            AddChild(new TestVariation(XE_LoadOptionsLI) { Attribute = new VariationAttribute("XElement.Load(), Load options, LineInfo, Uri") { Param = "Uri", Priority = 0 } });
            AddChild(new TestVariation(SaveOptionsTests) { Attribute = new VariationAttribute("XDocument.Save(), SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces") { Param = 3, Priority = 1 } });
            AddChild(new TestVariation(SaveOptionsTests) { Attribute = new VariationAttribute("XDocument.Save(), SaveOptions.OmitDuplicateNamespaces") { Param = 2, Priority = 1 } });
            AddChild(new TestVariation(SaveOptionsTests) { Attribute = new VariationAttribute("XDocument.Save(), SaveOptions.None") { Param = 0, Priority = 1 } });
            AddChild(new TestVariation(SaveOptionsTests) { Attribute = new VariationAttribute("XDocument.Save(), SaveOptions.DisableFormatting ") { Param = 1, Priority = 1 } });
            AddChild(new TestVariation(ElementSaveOptionsTests) { Attribute = new VariationAttribute("XElement.Save(), SaveOptions.OmitDuplicateNamespaces") { Param = 2, Priority = 1 } });
            AddChild(new TestVariation(ElementSaveOptionsTests) { Attribute = new VariationAttribute("XElement.Save(), SaveOptions.None") { Param = 0, Priority = 1 } });
            AddChild(new TestVariation(ElementSaveOptionsTests) { Attribute = new VariationAttribute("XElement.Save(), SaveOptions.DisableFormatting ") { Param = 1, Priority = 1 } });
            AddChild(new TestVariation(ElementSaveOptionsTests) { Attribute = new VariationAttribute("XElement.Save(), SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces") { Param = 3, Priority = 1 } });
            AddChild(new TestVariation(SaveOptionsDefaultTests) { Attribute = new VariationAttribute("XDocument.Save(), SaveOptions - default") { Priority = 1 } });
            AddChild(new TestVariation(ElementSaveOptionsDefaultTests) { Attribute = new VariationAttribute("XElement.Save(), SaveOptions - default") { Priority = 1 } });
            AddChild(new TestVariation(EncodingHints) { Attribute = new VariationAttribute("XDocument.Save(), Encoding hints UTF-8") { Param = "UTF-8", Priority = 1 } });
            AddChild(new TestVariation(EncodingHintsDefault) { Attribute = new VariationAttribute("XDocument.Save(), Encoding hints - No hint, Fallback to UTF8") { Priority = 1 } });
        }

        public void ElementSaveOptionsDefaultTests()
        {
            byte[] expected, actual;

            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement("root", new XProcessingInstruction("PI", ""), new XAttribute("id", "root"), new XAttribute(XNamespace.Xmlns + "p", "ns1"), new XElement("{ns1}A", new XAttribute(XNamespace.Xmlns + "p", "ns1"))));

            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, Encoding.UTF8))
                {
                    doc.Root.Save(sw);
                    sw.Flush();
                    expected = ms.ToArray();
                }
            }

            // write via Stream
            using (var ms = new MemoryStream())
            {
                doc.Root.Save(ms);
                actual = ms.ToArray();
            }

            TestLog.Compare(actual.Length, expected.Length, "Length not the same");
            for (int index = 0; index < actual.Length; index++)
            {
                TestLog.Equals(actual[index], expected[index], "Error on position " + index);
            }
        }

        public void ElementSaveOptionsTests()
        {
            var so = (SaveOptions)CurrentChild.Param;
            byte[] expected, actual;

            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement("root", new XProcessingInstruction("PI", ""), new XAttribute("id", "root"), new XAttribute(XNamespace.Xmlns + "p", "ns1"), new XElement("{ns1}A", new XAttribute(XNamespace.Xmlns + "p", "ns1"))));

            // write via writer
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, Encoding.UTF8))
                {
                    doc.Root.Save(sw, so);
                    sw.Flush();
                    expected = ms.ToArray();
                }
            }

            // write via Stream
            using (var ms = new MemoryStream())
            {
                doc.Root.Save(ms, so);
                actual = ms.ToArray();
            }

            TestLog.Compare(actual.Length, expected.Length, "Length not the same");
            for (int index = 0; index < actual.Length; index++)
            {
                TestLog.Equals(actual[index], expected[index], "Error on position " + index);
            }
        }

        public void EncodingHints()
        {
            Encoding enc = Encoding.GetEncoding(CurrentChild.Param as string);

            var doc = new XDocument(new XDeclaration("1.0", enc.WebName, "yes"), new XElement("root", new XProcessingInstruction("PI", ""), new XAttribute("id", "root"), new XAttribute(XNamespace.Xmlns + "p", "ns1"), new XElement("{ns1}A", new XAttribute(XNamespace.Xmlns + "p", "ns1"))));

            using (var ms = new MemoryStream())
            {
                doc.Save(ms);
                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                {
                    XDocument d1 = XDocument.Load(sr);
                    TestLog.Compare(sr.CurrentEncoding.Equals(enc), "Encoding does not match");
                    TestLog.Compare(d1.Declaration.Encoding, enc.WebName, "declaration does not match");
                }
            }
        }

        //[Variation(Priority = 1, Desc = "XDocument.Save(), Encoding hints - No hint, Fallback to UTF8")]
        public void EncodingHintsDefault()
        {
            var doc = new XDocument(new XElement("root", new XProcessingInstruction("PI", ""), new XAttribute("id", "root"), new XAttribute(XNamespace.Xmlns + "p", "ns1"), new XElement("{ns1}A", new XAttribute(XNamespace.Xmlns + "p", "ns1"))));

            using (var ms = new MemoryStream())
            {
                doc.Save(ms);
                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                {
                    XDocument d1 = XDocument.Load(sr);
                    TestLog.Compare(sr.CurrentEncoding.Equals(Encoding.UTF8), "Encoding does not match");
                    TestLog.Compare(d1.Declaration.Encoding, Encoding.UTF8.WebName, "declaration does not match");
                }
            }
        }

        public void LoadOptionsBU()
        {
            string fileName = @"NoExternals.xml";
            var how = CurrentChild.Param as string;

            XDocument doc = GetXDocument(fileName, LoadOptions.SetBaseUri, how);
            foreach (XObject node in doc.DescendantNodes().OfType<object>().Concat2(doc.Descendants().Attributes().OfType<object>()))
            {
                string baseUri = node.BaseUri;
                // fail when use stream replace file
                if (!String.IsNullOrWhiteSpace(baseUri))
                {
                    TestLog.Compare(new Uri(baseUri), new Uri(GetFullTestPath(fileName)), "base uri failed");
                }
            }

            doc = GetXDocument(fileName, LoadOptions.None, how);
            foreach (XObject node in doc.DescendantNodes().OfType<object>().Concat2(doc.Descendants().Attributes().OfType<object>()))
            {
                string baseUri = node.BaseUri;
                TestLog.Compare(baseUri, "", "base uri failed");
            }
        }

        //[Variation(Priority = 0, Desc = "XDocument.Load(), Load options, LineInfo, Uri", Param = "Uri")]
        //[Variation(Priority = 0, Desc = "XDocument.Load(), Load options, LineInfo, Stream", Param = "Stream")]
        public void LoadOptionsLI()
        {
            string fileName = @"NoExternals.xml";
            var how = CurrentChild.Param as string;

            XDocument doc = GetXDocument(fileName, LoadOptions.SetLineInfo, how);
            foreach (object node in doc.DescendantNodes().OfType<object>().Concat2(doc.Descendants().Attributes().OfType<object>()))
            {
                TestLog.Compare((node as IXmlLineInfo).LineNumber != 0, "LineNumber failed");
                TestLog.Compare((node as IXmlLineInfo).LinePosition != 0, "LinePosition failed");
            }

            doc = GetXDocument(fileName, LoadOptions.None, how);
            foreach (object node in doc.DescendantNodes().OfType<object>().Concat2(doc.Descendants().Attributes().OfType<object>()))
            {
                TestLog.Compare((node as IXmlLineInfo).LineNumber == 0, "LineNumber failed");
                TestLog.Compare((node as IXmlLineInfo).LinePosition == 0, "LinePosition failed");
            }
        }

        public void LoadOptionsPWS()
        {
            string fileName = @"NoExternals.xml";
            var how = CurrentChild.Param as string;

            // PreserveWhitespace = true
            XDocument doc = GetXDocument(fileName, LoadOptions.PreserveWhitespace, how);
            TestLog.Compare(doc.Root.FirstNode.NodeType == XmlNodeType.Text, "First node in root should be whitespace");

            // PreserveWhitespace = false ... default
            doc = GetXDocument(fileName, LoadOptions.None, how);
            TestLog.Compare(doc.Root.FirstNode.NodeType == XmlNodeType.Element, "First node in root should be element(no ws)");
        }

        public void SaveOptionsDefaultTests()
        {
            byte[] expected, actual;

            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement("root", new XProcessingInstruction("PI", ""), new XAttribute("id", "root"), new XAttribute(XNamespace.Xmlns + "p", "ns1"), new XElement("{ns1}A", new XAttribute(XNamespace.Xmlns + "p", "ns1"))));

            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, Encoding.UTF8))
                {
                    doc.Save(sw);
                    sw.Flush();
                    expected = ms.ToArray();
                }
            }

            // write via Stream
            using (var ms = new MemoryStream())
            {
                doc.Save(ms);
                actual = ms.ToArray();
            }

            TestLog.Compare(actual.Length, expected.Length, "Length not the same");
            for (int index = 0; index < actual.Length; index++)
            {
                TestLog.Equals(actual[index], expected[index], "Error on position " + index);
            }
        }

        public void SaveOptionsTests()
        {
            var so = (SaveOptions)CurrentChild.Param;
            byte[] expected, actual;

            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement("root", new XProcessingInstruction("PI", ""), new XAttribute("id", "root"), new XAttribute(XNamespace.Xmlns + "p", "ns1"), new XElement("{ns1}A", new XAttribute(XNamespace.Xmlns + "p", "ns1"))));

            // write via writer
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, Encoding.UTF8))
                {
                    doc.Save(sw, so);
                    sw.Flush();
                    expected = ms.ToArray();
                }
            }

            // write via Stream
            using (var ms = new MemoryStream())
            {
                doc.Save(ms, so);
                actual = ms.ToArray();
            }

            TestLog.Compare(actual.Length, expected.Length, "Length not the same");
            for (int index = 0; index < actual.Length; index++)
            {
                TestLog.Equals(actual[index], expected[index], "Error on position " + index);
            }
        }

        //[Variation(Priority = 0, Desc = "Stream state after loading")]
        public void StreamStateAfterLoading()
        {
            MemoryStream ms = CreateStream(purchaseOrderXml, Encoding.UTF8);

            XDocument.Load(ms);
            // if stream is not closed we should be able to reset it's position and read it again
            ms.Position = 0;
            XDocument.Load(ms);
        }

        //[Variation(Priority = 0, Desc = "XDocument.Load() Encodings: UTF8, UTF16, UTF16BE")]
        public void XDEncodings()
        {
            foreach (Encoding enc in new[] { Encoding.UTF8, Encoding.GetEncoding("UTF-16"), Encoding.GetEncoding("UTF-16BE") })
            {
                MemoryStream ms = CreateStream(purchaseOrderXml, enc);

                XDocument d = XDocument.Load(ms);
                TestLog.Compare(XNode.DeepEquals(d, _purchaseOrder), "Not the same");
            }
        }

        //[Variation(Priority = 0, Desc = "XElement.Load() Encodings: UTF8, UTF16, UTF16BE")]
        public void XEEncodings()
        {
            foreach (Encoding enc in new[] { Encoding.UTF8, Encoding.GetEncoding("UTF-16"), Encoding.GetEncoding("UTF-16BE") })
            {
                MemoryStream ms = CreateStream(purchaseOrderXml, enc);

                XElement e = XElement.Load(ms);
                TestLog.Compare(XNode.DeepEquals(e, _purchaseOrder.Root), "Not the same");
            }
        }

        //[Variation(Priority = 0, Desc = "XDocument.Load(), Load options, preserveWhitespace, Uri", Param = "Uri")]
        //[Variation(Priority = 0, Desc = "XDocument.Load(), Load options, preserveWhitespace, Stream", Param = "Stream")]

        //[Variation(Priority = 0, Desc = "XElement.Load(), Load options, BaseUri, Uri", Param = "Uri")]
        //[Variation(Priority = 0, Desc = "XElement.Load(), Load options, BaseUri, Stream", Param = "Stream")]
        public void XE_LoadOptionsBU()
        {
            string fileName = @"NoExternals.xml";
            var how = CurrentChild.Param as string;

            XElement e = GetXElement(fileName, LoadOptions.SetBaseUri, how);
            foreach (XObject node in e.DescendantNodesAndSelf().OfType<object>().Concat2(e.DescendantsAndSelf().Attributes().OfType<object>()))
            {
                string baseUri = node.BaseUri;
                if (!String.IsNullOrWhiteSpace(baseUri))
                {
                    TestLog.Compare(new Uri(baseUri), new Uri(GetFullTestPath(fileName)), "base uri failed");
                }
            }

            e = GetXElement(fileName, LoadOptions.None, how);
            foreach (XObject node in e.DescendantNodesAndSelf().OfType<object>().Concat2(e.DescendantsAndSelf().Attributes().OfType<object>()))
            {
                string baseUri = node.BaseUri;
                TestLog.Compare(baseUri, "", "base uri failed");
            }
        }

        //[Variation(Priority = 0, Desc = "XElement.Load(), Load options, LineInfo, Uri", Param = "Uri")]
        //[Variation(Priority = 0, Desc = "XElement.Load(), Load options, LineInfo, Stream", Param = "Stream")]
        public void XE_LoadOptionsLI()
        {
            string fileName = @"NoExternals.xml";
            var how = CurrentChild.Param as string;

            XElement e = GetXElement(fileName, LoadOptions.SetLineInfo, how);
            foreach (object node in e.DescendantNodesAndSelf().OfType<object>().Concat2(e.DescendantsAndSelf().Attributes().OfType<object>()))
            {
                TestLog.Compare((node as IXmlLineInfo).LineNumber != 0, "LineNumber failed");
                TestLog.Compare((node as IXmlLineInfo).LinePosition != 0, "LinePosition failed");
            }

            e = GetXElement(fileName, LoadOptions.None, how);
            foreach (object node in e.DescendantNodesAndSelf().OfType<object>().Concat2(e.DescendantsAndSelf().Attributes().OfType<object>()))
            {
                TestLog.Compare((node as IXmlLineInfo).LineNumber == 0, "LineNumber failed");
                TestLog.Compare((node as IXmlLineInfo).LinePosition == 0, "LinePosition failed");
            }
        }

        public void XE_LoadOptionsPWS()
        {
            string fileName = @"NoExternals.xml";
            var how = CurrentChild.Param as string;

            XElement e = GetXElement(fileName, LoadOptions.PreserveWhitespace, how);
            TestLog.Compare(e.FirstNode.NodeType == XmlNodeType.Text, "First node in root should be whitespace");

            // PreserveWhitespace = false ... default
            e = GetXElement(fileName, LoadOptions.None, how);
            TestLog.Compare(e.FirstNode.NodeType == XmlNodeType.Element, "First node in root should be element(no ws)");
        }

        #endregion

        #region Methods

        private static MemoryStream CreateStream(string data, Encoding enc)
        {
            var ms = new MemoryStream();
            // StreamWriter is closing the memorystream when used with using ... so we keep it this way
            var sw = new StreamWriter(ms, enc);
            sw.Write(data);
            sw.Flush();
            ms.Position = 0;
            return ms;
        }

        private static string GetFullTestPath(string fileName)
        {
            return Path.Combine(FilePathUtil.GetTestDataPath() + @"/XLinq", fileName);
        }

        // Save options:
        //[Variation(Priority = 1, Desc = "XDocument.Save(), SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces", Param = SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces)]
        //[Variation(Priority = 1, Desc = "XDocument.Save(), SaveOptions.DisableFormatting ", Param = SaveOptions.DisableFormatting)]
        //[Variation(Priority = 1, Desc = "XDocument.Save(), SaveOptions.OmitDuplicateNamespaces", Param = SaveOptions.OmitDuplicateNamespaces)]
        //[Variation(Priority = 1, Desc = "XDocument.Save(), SaveOptions.None", Param = SaveOptions.None)]

        private static XDocument GetXDocument(string fileName, LoadOptions lo, string how)
        {
            switch (how)
            {
                case "Uri":
                    return XDocument.Load(FilePathUtil.getStream(GetFullTestPath(fileName)), lo);
                case "Stream":
                    using (Stream s = FilePathUtil.getStream(GetFullTestPath(fileName)))
                    {
                        return XDocument.Load(s, lo);
                    }
                default:
                    throw new TestFailedException("TEST FAILED: don't know how to create XDocument");
            }
        }

        private static XElement GetXElement(string fileName, LoadOptions lo, string how)
        {
            switch (how)
            {
                case "Uri":
                    return XElement.Load(FilePathUtil.getStream(GetFullTestPath(fileName)), lo);
                case "Stream":
                    using (Stream s = FilePathUtil.getStream(GetFullTestPath(fileName)))
                    {
                        return XElement.Load(s, lo);
                    }
                default:
                    throw new TestFailedException("TEST FAILED: don't know how to create XElement");
            }
        }
        #endregion
    }
}
