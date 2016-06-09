// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Text;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    //[TestCase(Name = "XmlWriterSettings: NamespaceHandling")]
    public partial class TCNamespaceHandling : XmlFactoryWriterTestCaseBase
    {
        public override int Init(object o)
        {
            if (!(WriterType == WriterType.CustomWriter || WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent /*|| WriterType == WriterType.DOMWriter || WriterType == WriterType.XmlTextWriter*/))
            {
                int i = base.Init(0);
                return i;
            }
            return TEST_SKIPPED;
        }

        private static NamespaceHandling[] s_nlHandlingMembers = { NamespaceHandling.Default, NamespaceHandling.OmitDuplicates };
        private StringWriter _strWriter = null;

        private XmlWriter CreateMemWriter(XmlWriterSettings settings)
        {
            XmlWriterSettings wSettings = settings.Clone();
            wSettings.CloseOutput = false;
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = false;
            XmlWriter w = null;

            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    w = WriterHelper.Create(_strWriter, wSettings);
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    w = WriterHelper.Create(_strWriter, wSettings);
                    break;
                case WriterType.WrappedWriter:
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    XmlWriter ww = WriterHelper.Create(_strWriter, wSettings);
                    w = WriterHelper.Create(ww, wSettings);
                    break;
                case WriterType.CharCheckingWriter:
                    if (_strWriter != null) _strWriter.Dispose();
                    _strWriter = new StringWriter();
                    XmlWriter cw = WriterHelper.Create(_strWriter, wSettings);
                    XmlWriterSettings cws = settings.Clone();
                    cws.CheckCharacters = true;
                    w = WriterHelper.Create(cw, cws);
                    break;
                default:
                    throw new Exception("Unknown writer type");
            }
            return w;
        }

        private void VerifyOutput(string expected)
        {
            string actual = _strWriter.ToString();

            if (actual != expected)
            {
                CError.WriteLineIgnore("Expected: " + expected);
                CError.WriteLineIgnore("Actual: " + actual);
                CError.Compare(false, "Expected and actual output differ!");
            }
        }

        //[Variation(Desc = "NamespaceHandling Default value - NamespaceHandling.Default")]
        public int NS_Handling_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Compare(wSettings.NamespaceHandling, NamespaceHandling.Default, "Incorect default value for XmlWriterSettings.NamespaceHandling");

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                CError.Compare(w.Settings.NamespaceHandling, NamespaceHandling.Default, "Incorect default value for XmlWriter.Settings.NamespaceHandling");
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "XmlWriter creation with NamespaceHandling.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "XmlWriter creation with NamespaceHandling.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                CError.Compare(w != null, "XmlWriter creation failed");
                CError.Compare(w.Settings.NamespaceHandling, (NamespaceHandling)this.CurVariation.Param, "Invalid NamespaceHandling assignment");
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "NamespaceHandling = NamespaceHandling.Default | NamespaceHandling.OmitDuplicates")]
        public int NS_Handling_2a()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = NamespaceHandling.Default | NamespaceHandling.OmitDuplicates;

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                CError.Compare(w.Settings.NamespaceHandling, NamespaceHandling.OmitDuplicates, "Invalid NamespaceHandling assignment");
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "NamespaceHandling override with Default.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "NamespaceHandling override with Default.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                wSettings.NamespaceHandling = NamespaceHandling.Default;
                CError.Compare(w != null, "XmlWriter creation failed");
                CError.Compare(w.Settings.NamespaceHandling, (NamespaceHandling)this.CurVariation.Param, "Invalid NamespaceHandling assignment");
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "NamespaceHandling override with negativeVal.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "NamespaceHandling override with negativeVal.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_3a()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            try
            {
                wSettings.NamespaceHandling = (NamespaceHandling)(-1);
                CError.Compare(false, "Failed");
            }
            catch (ArgumentOutOfRangeException)
            {
                try
                {
                    wSettings.NamespaceHandling = (NamespaceHandling)(999);
                    CError.Compare(false, "Failed2");
                }
                catch (ArgumentOutOfRangeException) { }
            }

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                CError.Compare(w != null, "XmlWriter creation failed");
                CError.Compare(w.Settings.NamespaceHandling, (NamespaceHandling)this.CurVariation.Param, "Invalid NamespaceHandling assignment");
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "1.NamespaceHandling.Default", Params = new object[] { NamespaceHandling.Default, "<root><p:foo xmlns:p=\"uri\"><a xmlns:p=\"uri\" /></p:foo></root>",null })]
        //[Variation(Desc = "1.NamespaceHandling.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<root><p:foo xmlns:p=\"uri\"><a xmlns:p=\"uri\" /></p:foo></root>","<root><p:foo xmlns:p=\"uri\"><a /></p:foo></root>" })]
        //[Variation(Desc = "2.NamespaceHandling.Default", Params = new object[] { NamespaceHandling.Default, "<root><foo xmlns=\"uri\"><a xmlns=\"uri\" /></foo></root>", null })]
        //[Variation(Desc = "2.NamespaceHandling.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<root><foo xmlns=\"uri\"><a xmlns=\"uri\" /></foo></root>","<root><foo xmlns=\"uri\"><a /></foo></root>" })]
        //[Variation(Desc = "3.NamespaceHandling.Default", Params = new object[] { NamespaceHandling.Default, "<root><p:foo xmlns:p=\"uri\"><a xmlns:p=\"uriOther\" /></p:foo></root>",null })]
        //[Variation(Desc = "3.NamespaceHandling.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<root><p:foo xmlns:p=\"uri\"><a xmlns:p=\"uriOther\" /></p:foo></root>",null })]
        //[Variation(Desc = "4.NamespaceHandling.Default", Params = new object[] { NamespaceHandling.Default, "<root><p:foo xmlns:p=\"uri\"><a xmlns:pOther=\"uri\" /></p:foo></root>",null })]
        //[Variation(Desc = "4.NamespaceHandling.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<root><p:foo xmlns:p=\"uri\"><a xmlns:pOther=\"uri\" /></p:foo></root>",null })]
        //[Variation(Desc = "5.NamespaceHandling.Default", Params = new object[] { NamespaceHandling.Default, "<root xmlns:p=\"uri\"><p:foo><p:a xmlns:p=\"uri\" /></p:foo></root>", null })]
        //[Variation(Desc = "5.NamespaceHandling.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<root xmlns:p=\"uri\"><p:foo><p:a xmlns:p=\"uri\" /></p:foo></root>", "<root xmlns:p=\"uri\"><p:foo><p:a /></p:foo></root>" })]
        //[Variation(Desc = "6.NamespaceHandling.Default", Params = new object[] { NamespaceHandling.Default, "<root xmlns:p=\"uri\"><p:foo><a xmlns:p=\"uri\" /></p:foo></root>", null })]
        //[Variation(Desc = "6.NamespaceHandling.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<root xmlns:p=\"uri\"><p:foo><a xmlns:p=\"uri\" /></p:foo></root>","<root xmlns:p=\"uri\"><p:foo><a /></p:foo></root>" })]
        //[Variation(Desc = "7.NamespaceHandling.Default", Params = new object[] { NamespaceHandling.Default, "<root><p xmlns:xml=\"http://www.w3.org/XML/1998/namespace\" /></root>", null })]
        //[Variation(Desc = "7.NamespaceHandling.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<root><p xmlns:xml=\"http://www.w3.org/XML/1998/namespace\" /></root>","<root><p /></root>" })]
        //[Variation(Desc = "8.NamespaceHandling.Default NS Redefinition.Default", Params = new object[] { NamespaceHandling.Default, "<a xmlns=\"p1\"><b xmlns=\"p2\"><c xmlns=\"p1\" /></b><d xmlns=\"\"><e xmlns=\"p1\"><f xmlns=\"\" /></e></d></a>", null })]
        //[Variation(Desc = "8.NamespaceHandling.Default NS Redefinition.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<a xmlns=\"p1\"><b xmlns=\"p2\"><c xmlns=\"p1\" /></b><d xmlns=\"\"><e xmlns=\"p1\"><f xmlns=\"\" /></e></d></a>", null })]
        //[Variation(Desc = "9.Unused namespace declarations.Default", Params = new object[] { NamespaceHandling.Default, "<root> <elem1 xmlns=\"urn:namespace\" att1=\"foo\" /></root>", null })]
        //[Variation(Desc = "9.Unused namespace declarations.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<root> <elem1 xmlns=\"urn:namespace\" att1=\"foo\" /></root>", null })]
        //[Variation(Desc = "10.NamespaceHandling.Same NS.Diff prefix.Default", Params = new object[] { NamespaceHandling.Default, "<a xmlns:p=\"p1\"><b xmlns:a=\"p1\"><c xmlns:b=\"p1\" /></b></a>", null })]
        //[Variation(Desc = "10.NamespaceHandling.Same NS.Diff prefix.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<a xmlns:p=\"p1\"><b xmlns:a=\"p1\"><c xmlns:b=\"p1\" /></b></a>", null })]
        //[Variation(Desc = "11.NamespaceHandling.Diff NS.Same prefix.Default", Params = new object[] { NamespaceHandling.Default, "<a xmlns:p=\"p1\"><b xmlns:p=\"p2\"><c xmlns:p=\"p3\" /></b></a>", null })]
        //[Variation(Desc = "11.NamespaceHandling.Diff NS.Same prefix.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<a xmlns:p=\"p1\"><b xmlns:p=\"p2\"><c xmlns:p=\"p3\" /></b></a>", null })]
        //[Variation(Desc = "12.NamespaceHandling.NS Redefinition.Unused NS.Default", Params = new object[] { NamespaceHandling.Default, "<a xmlns:p=\"p1\"><b xmlns:p=\"p2\"><c xmlns:p=\"p1\" /></b></a>", null })]
        //[Variation(Desc = "12.NamespaceHandling.NS Redefinition.Unused NS.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "<a xmlns:p=\"p1\"><b xmlns:p=\"p2\"><c xmlns:p=\"p1\" /></b></a>", null })]
        public int NS_Handling_3b()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            string xml = (string)this.CurVariation.Params[1];
            string exp = (string)this.CurVariation.Params[2];

            using (XmlReader r = ReaderHelper.Create(new StringReader(xml)))
            {
                using (XmlWriter w = CreateMemWriter(wSettings))
                {
                    w.WriteNode(r, false);
                    w.Dispose();
                    VerifyOutput(exp == null ? xml : exp);
                }
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "NamespaceHandling wrap with Default.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "NamespaceHandling wrap with Default.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_4a()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            using (XmlWriter ww = CreateMemWriter(wSettings))
            {
                XmlWriterSettings ws = wSettings.Clone();
                ws.NamespaceHandling = NamespaceHandling.Default;
                ws.CheckCharacters = true;
                using (XmlWriter w = WriterHelper.Create(ww, ws))
                {
                    CError.Compare(w != null, "XmlWriter creation failed");
                    CError.Compare(w.Settings.NamespaceHandling, (NamespaceHandling)this.CurVariation.Param, "Invalid NamespaceHandling assignment");
                }
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "NamespaceHandling wrap with OmitDuplicates.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "NamespaceHandling wrap with OmitDuplicates.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_4b()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            using (XmlWriter ww = CreateMemWriter(wSettings))
            {
                XmlWriterSettings ws = wSettings.Clone();
                ws.NamespaceHandling = NamespaceHandling.OmitDuplicates;
                ws.CheckCharacters = true;
                using (XmlWriter w = WriterHelper.Create(ww, ws))
                {
                    CError.Compare(w != null, "XmlWriter creation failed");
                    CError.Compare(w.Settings.NamespaceHandling, (NamespaceHandling)this.CurVariation.Param, "Invalid NamespaceHandling assignment");
                }
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "XmlWriter.WriteStartElement() should inspect attributes before emitting the element tag.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "XmlWriter.WriteStartElement() should inspect attributes before emitting the element tag.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_5()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("root", "uri");
                w.WriteStartAttribute("xmlns", "p", "http://www.w3.org/2000/xmlns/");
                w.WriteString("uri");
                w.WriteEndElement();
            }
            VerifyOutput("<root xmlns:p=\"uri\" xmlns=\"uri\" />");
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteStartElement,AttrString with null prefix.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteStartElement,AttrString with null prefix.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement(null, "e", "ns");
                w.WriteAttributeString(null, "attr", "ns", "val");
                w.WriteElementString(null, "el", "ns", "val");
            }
            VerifyOutput("<e p1:attr=\"val\" xmlns:p1=\"ns\" xmlns=\"ns\"><p1:el>val</p1:el></e>");
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteStartElement,AttrString with empty prefix.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteStartElement,AttrString with empty prefix.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_7()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement(String.Empty, "e", "ns");
                w.WriteAttributeString(String.Empty, "attr", "ns", "val");
                w.WriteElementString(String.Empty, "el", "ns", "val");
            }
            VerifyOutput("<e p1:attr=\"val\" xmlns:p1=\"ns\" xmlns=\"ns\"><el>val</el></e>");
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteStartElement,AttrString with not null prefix and namespace.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteStartElement,AttrString with not null prefix and namespace.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_8()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("a", "e", "ns");
                w.WriteAttributeString("a", "attr", "ns", "val");
                w.WriteElementString("a", "el", "ns", "val");
            }
            VerifyOutput("<a:e a:attr=\"val\" xmlns:a=\"ns\"><a:el>val</a:el></a:e>");
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteStartElement,AttrString without prefix.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteStartElement,AttrString without prefix.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_9()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("e", "ns");
                w.WriteAttributeString("attr", "ns", "val");
                w.WriteElementString("el", "ns", "val");
            }
            VerifyOutput("<e p1:attr=\"val\" xmlns:p1=\"ns\" xmlns=\"ns\"><p1:el>val</p1:el></e>");
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteStartElement,AttrString with null namespace,prefix.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteStartElement,AttrString with null namespace,prefix.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_10()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement(null, "e", null);
                w.WriteAttributeString(null, "attr", null, "val");
                w.WriteElementString(null, "el", null, "val");
            }
            VerifyOutput("<e attr=\"val\"><el>val</el></e>");
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteStartElement,AttrString with empty namespace,prefix.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteStartElement,AttrString with empty namespace,prefix.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_11()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement(String.Empty, "e", String.Empty);
                w.WriteAttributeString(String.Empty, "attr", String.Empty, "val");
                w.WriteElementString(String.Empty, "el", String.Empty, "val");
            }
            VerifyOutput("<e attr=\"val\"><el>val</el></e>");
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteStartElement,AttrString without namespace,prefix.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteStartElement,AttrString without namespace,prefix.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_12()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("attr", "val");
                w.WriteElementString("el", "val");
            }
            VerifyOutput("<e attr=\"val\"><el>val</el></e>");
            return TEST_PASS;
        }

        //[Variation(Desc = "LookupPrefix.Default", Param = NamespaceHandling.Default)] 
        //[Variation(Desc = "LookupPrefix.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]         
        public int NS_Handling_16()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("a", "foo", "b");
                CError.Compare(w.LookupPrefix("foo"), null, "FailedEl");
                w.WriteAttributeString("a", "foo", "b");
                CError.Compare(w.LookupPrefix("foo"), "p1", "FailedAttr");
                w.WriteElementString("e", "foo", "b");
                CError.Compare(w.LookupPrefix("foo"), "p1", "FailedEl");
            }
            VerifyOutput("<a:foo p1:a=\"b\" xmlns:p1=\"foo\" xmlns:a=\"b\"><p1:e>b</p1:e></a:foo>");
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteAttributeString with dup.namespace,w/o prefix.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteAttributeString with dup.namespace,w/o prefix.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_17()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteDocType("a", null, null, "<!ATTLIST Root a CDATA #IMPLIED>");
                w.WriteStartElement("Root");
                for (int i = 0; i < 1000; i++)
                {
                    w.WriteAttributeString("a", "n" + i, "val");
                }
                try
                {
                    w.WriteAttributeString("a", "n" + 999, "val");
                    CError.Compare(false, "Failed");
                }
                catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "WriteAttributeString with prefix bind to the same ns.Default", Params = new object[] { NamespaceHandling.Default, true })]
        //[Variation(Desc = "WriteAttributeString with prefix bind to the same ns.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, true })]
        //[Variation(Desc = "WriteElementString with prefix bind to the same ns.Default", Params = new object[] { NamespaceHandling.Default, false })]
        //[Variation(Desc = "WriteElementString with prefix bind to the same ns.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, false })]
        public int NS_Handling_17a()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            bool isAttr = (bool)this.CurVariation.Params[1];

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteDocType("a", null, null, "<!ATTLIST Root a CDATA #IMPLIED>");
                w.WriteStartElement("Root");
                for (int i = 0; i < 10; i++)
                {
                    if (isAttr)
                        w.WriteAttributeString("p", "a" + i, "n", "val");
                    else
                        w.WriteElementString("p", "a" + i, "n", "val");
                }
            }
            string exp = isAttr ?
                "<!DOCTYPE a [<!ATTLIST Root a CDATA #IMPLIED>]><Root p:a0=\"val\" p:a1=\"val\" p:a2=\"val\" p:a3=\"val\" p:a4=\"val\" p:a5=\"val\" p:a6=\"val\" p:a7=\"val\" p:a8=\"val\" p:a9=\"val\" xmlns:p=\"n\" />" :
                "<!DOCTYPE a [<!ATTLIST Root a CDATA #IMPLIED>]><Root><p:a0 xmlns:p=\"n\">val</p:a0><p:a1 xmlns:p=\"n\">val</p:a1><p:a2 xmlns:p=\"n\">val</p:a2><p:a3 xmlns:p=\"n\">val</p:a3><p:a4 xmlns:p=\"n\">val</p:a4><p:a5 xmlns:p=\"n\">val</p:a5><p:a6 xmlns:p=\"n\">val</p:a6><p:a7 xmlns:p=\"n\">val</p:a7><p:a8 xmlns:p=\"n\">val</p:a8><p:a9 xmlns:p=\"n\">val</p:a9></Root>";
            VerifyOutput(exp);
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteAttributeString with prefix bind to default ns.Default", Params = new object[] { NamespaceHandling.Default, true })]
        //[Variation(Desc = "WriteAttributeString with prefix bind to default ns.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, true })]
        //[Variation(Desc = "WriteElementString with prefix bind to default ns.Default", Params = new object[] { NamespaceHandling.Default, false })]
        //[Variation(Desc = "WriteElementString with prefix bind to default ns.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, false })]
        public int NS_Handling_17b()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            bool isAttr = (bool)this.CurVariation.Params[1];

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("Root");
                for (int i = 0; i < 5; i++)
                {
                    if (isAttr)
                        w.WriteAttributeString("p", "a" + i, "xmlns", "val");
                    else
                        w.WriteElementString("p", "a" + i, "xmlns", "val");
                }
            }
            string exp = isAttr ?
                "<Root p:a0=\"val\" p:a1=\"val\" p:a2=\"val\" p:a3=\"val\" p:a4=\"val\" xmlns:p=\"xmlns\" />" :
                "<Root><p:a0 xmlns:p=\"xmlns\">val</p:a0><p:a1 xmlns:p=\"xmlns\">val</p:a1><p:a2 xmlns:p=\"xmlns\">val</p:a2><p:a3 xmlns:p=\"xmlns\">val</p:a3><p:a4 xmlns:p=\"xmlns\">val</p:a4></Root>";
            VerifyOutput(exp);
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteAttributeString with prefix bind to non-default ns.dup.namespace.Default", Params = new object[] { NamespaceHandling.Default, true })]
        //[Variation(Desc = "WriteAttributeString with prefix bind to non-default ns.dup.namespace.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, true })]
        //[Variation(Desc = "WriteElementString with prefix bind to non-default ns.dup.namespace.Default", Params = new object[] { NamespaceHandling.Default, false })]
        //[Variation(Desc = "WriteElementString with prefix bind to non-default ns.dup.namespace.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, false })]
        public int NS_Handling_17c()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            bool isAttr = (bool)this.CurVariation.Params[1];

            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteStartElement("Root");
            for (int i = 0; i < 5; i++)
            {
                if (isAttr)
                    w.WriteAttributeString("p" + i, "a", "n" + i, "val" + i);
                else
                    w.WriteElementString("p" + i, "a", "n" + i, "val" + i);
            }
            try
            {
                if (isAttr)
                {
                    w.WriteAttributeString("p", "a", "n" + 4, "val");
                    CError.Compare(false, "Failed");
                }
                else
                    w.WriteElementString("p", "a", "n" + 4, "val");
            }
            catch (XmlException) { }
            finally
            {
                w.Dispose();
                string exp = isAttr ?
                    "<Root p0:a=\"val0\" p1:a=\"val1\" p2:a=\"val2\" p3:a=\"val3\" p4:a=\"val4\"" :
                    "<Root><p0:a xmlns:p0=\"n0\">val0</p0:a><p1:a xmlns:p1=\"n1\">val1</p1:a><p2:a xmlns:p2=\"n2\">val2</p2:a><p3:a xmlns:p3=\"n3\">val3</p3:a><p4:a xmlns:p4=\"n4\">val4</p4:a><p:a xmlns:p=\"n4\">val</p:a></Root>";
                VerifyOutput(exp);
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteAttributeString with prefix bind to ns uri.Default", Params = new object[] { NamespaceHandling.Default, true })]
        //[Variation(Desc = "WriteAttributeString with prefix bind to ns uri.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, true })]
        //[Variation(Desc = "WriteElementString with prefix bind to ns uri.Default", Params = new object[] { NamespaceHandling.Default, false })]
        //[Variation(Desc = "WriteElementString with prefix bind to ns uri.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, false })]
        public int NS_Handling_17d()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            bool isAttr = (bool)this.CurVariation.Params[1];

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("Root");
                for (int i = 0; i < 5; i++)
                {
                    if (isAttr)
                    {
                        w.WriteAttributeString("xml", "a" + i, "http://www.w3.org/XML/1998/namespace", "val");
                        w.WriteAttributeString("xmlns", "a" + i, "http://www.w3.org/2000/xmlns/", "val");
                    }
                    else
                    {
                        w.WriteElementString("xml", "a" + i, "http://www.w3.org/XML/1998/namespace", "val");
                    }
                }
            }
            string exp = isAttr ?
                "<Root xml:a0=\"val\" xmlns:a0=\"val\" xml:a1=\"val\" xmlns:a1=\"val\" xml:a2=\"val\" xmlns:a2=\"val\" xml:a3=\"val\" xmlns:a3=\"val\" xml:a4=\"val\" xmlns:a4=\"val\" />" :
                "<Root><xml:a0>val</xml:a0><xml:a1>val</xml:a1><xml:a2>val</xml:a2><xml:a3>val</xml:a3><xml:a4>val</xml:a4></Root>";
            VerifyOutput(exp); return TEST_PASS;
        }

        //[Variation(Desc = "WriteAttributeString with ns uri.Default", Params = new object[] { NamespaceHandling.Default, true })]
        //[Variation(Desc = "WriteAttributeString with ns uri.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, true })]
        //[Variation(Desc = "WriteElementString with ns uri.Default", Params = new object[] { NamespaceHandling.Default, false })]
        //[Variation(Desc = "WriteElementString with ns uri.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, false })]
        public int NS_Handling_17e()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            bool isAttr = (bool)this.CurVariation.Params[1];

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("Root");
                for (int i = 0; i < 5; i++)
                {
                    if (isAttr)
                        w.WriteAttributeString("a" + i, "http://www.w3.org/XML/1998/namespace", "val");
                    else
                        w.WriteElementString("a" + i, "http://www.w3.org/XML/1998/namespace", "val");
                }
            }
            string exp = isAttr ?
                "<Root xml:a0=\"val\" xml:a1=\"val\" xml:a2=\"val\" xml:a3=\"val\" xml:a4=\"val\" />" :
                "<Root><xml:a0>val</xml:a0><xml:a1>val</xml:a1><xml:a2>val</xml:a2><xml:a3>val</xml:a3><xml:a4>val</xml:a4></Root>";
            VerifyOutput(exp); return TEST_PASS;
        }

        //[Variation(Desc = "WriteAttribute/ElemString with prefixes.namespace.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteAttribute/ElemString with prefixes.namespace.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_18()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("test");
                w.WriteAttributeString("p", "a1", "ns1", "v");
                w.WriteStartElement("base");
                w.WriteAttributeString("a2", "ns1", "v");
                w.WriteAttributeString("p", "a3", "ns2", "v");
                w.WriteElementString("p", "e", "ns2", "v");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            string exp = "<test p:a1=\"v\" xmlns:p=\"ns1\"><base p:a2=\"v\" p4:a3=\"v\" xmlns:p4=\"ns2\"><p:e xmlns:p=\"ns2\">v</p:e></base></test>";
            VerifyOutput(exp); return TEST_PASS;
        }

        //[Variation(Desc = "WriteAttribute/ElemString with xmlns:xml,space,lang.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteAttribute/ElemString with xmlns:xml,space,lang.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_19()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("xmlns", "xml", null, "http://www.w3.org/XML/1998/namespace");
                w.WriteAttributeString("xmlns", "space", null, "preserve");
                w.WriteAttributeString("xmlns", "lang", null, "chs");
                w.WriteElementString("xml", "lang", null, "jpn");
                w.WriteElementString("xml", "space", null, "default");
                w.WriteElementString("xml", "xml", null, "http://www.w3.org/XML/1998/namespace");
                w.WriteEndElement();
            }
            string exp = ((NamespaceHandling)this.CurVariation.Param == NamespaceHandling.OmitDuplicates) ?
                "<Root xmlns:space=\"preserve\" xmlns:lang=\"chs\"><xml:lang>jpn</xml:lang><xml:space>default</xml:space><xml:xml>http://www.w3.org/XML/1998/namespace</xml:xml></Root>" :
                "<Root xmlns:xml=\"http://www.w3.org/XML/1998/namespace\" xmlns:space=\"preserve\" xmlns:lang=\"chs\"><xml:lang>jpn</xml:lang><xml:space>default</xml:space><xml:xml>http://www.w3.org/XML/1998/namespace</xml:xml></Root>";
            VerifyOutput(exp); return TEST_PASS;
        }

        //[Variation(Desc = "WriteAttributeString with null val,ns.attr=xmlns:xml.Default", Params = new object[] { NamespaceHandling.Default, "xmlns", "xml", true })]
        //[Variation(Desc = "WriteAttributeString with null val,ns.attr=xmlns:xml.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "xmlns", "xml", true })]
        //[Variation(Desc = "WriteAttributeString with null val,ns.elem=xmlns:xml.Default", Params = new object[] { NamespaceHandling.Default, "xmlns", "xml", false })]
        //[Variation(Desc = "WriteAttributeString with null val,ns.elem=xmlns:xml.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "xmlns", "xml", false })]

        //[Variation(Desc = "WriteAttributeString with null val,ns.attr=xml:space.Default", Params = new object[] { NamespaceHandling.Default, "xml", "space", true })]
        //[Variation(Desc = "WriteAttributeString with null val,ns.attr=xml:space.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "xml", "space", true })]
        //[Variation(Desc = "WriteAttributeString with null val,ns.elem=xmlns:space.Default", Params = new object[] { NamespaceHandling.Default, "xmlns", "space", false })]
        //[Variation(Desc = "WriteAttributeString with null val,ns.elem=xmlns:space.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "xmlns", "space", false })]

        //[Variation(Desc = "WriteAttributeString with null val,ns.attr=xmlns:lang.Default", Params = new object[] { NamespaceHandling.Default, "xmlns", "lang", true })]
        //[Variation(Desc = "WriteAttributeString with null val,ns.attr=xmlns:lang.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "xmlns", "lang", true })]
        //[Variation(Desc = "WriteAttributeString with null val,ns.elem=xmlns:lang.Default", Params = new object[] { NamespaceHandling.Default, "xmlns", "lang", false })]
        //[Variation(Desc = "WriteAttributeString with null val,ns.elem=xmlns:lang.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, "xmlns", "lang", false })]
        public int NS_Handling_19a()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            string prefix = (string)this.CurVariation.Params[1];
            string name = (string)this.CurVariation.Params[2];
            bool isAttr = (bool)this.CurVariation.Params[3];
            XmlWriter w = CreateMemWriter(wSettings);
            w.WriteStartElement("Root");
            try
            {
                if (isAttr)
                    w.WriteAttributeString(prefix, name, null, null);
                else
                    w.WriteElementString(prefix, name, null, null);
                CError.Compare(false, "error");
            }
            catch (ArgumentException e) { CError.WriteLine(e); CError.Compare(w.WriteState, WriteState.Error, "state"); }
            finally
            {
                w.Dispose();
                CError.Compare(w.WriteState, WriteState.Closed, "state");
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteAttributeString in error state.Default", Params = new object[] { NamespaceHandling.Default, true })]
        //[Variation(Desc = "WriteAttributeString in error state.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, true })]
        //[Variation(Desc = "WriteElementString in error state.Default", Params = new object[] { NamespaceHandling.Default, false })]
        //[Variation(Desc = "WriteElementString in error state.OmitDuplicates", Params = new object[] { NamespaceHandling.OmitDuplicates, false })]
        public int NS_Handling_19b()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            bool isAttr = (bool)this.CurVariation.Params[1];

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("Root");
                try
                {
                    if (isAttr)
                        w.WriteAttributeString("xmlns", "xml", null, null);
                    else
                        w.WriteElementString("xmlns", "xml", null, null);
                }
                catch (ArgumentException e) { CError.WriteLine(e.Message); }
            }
            string exp = isAttr ? "<Root" : "<Root><xmlns:xml";
            VerifyOutput(exp); return TEST_PASS;
        }

        //[Variation(Desc = "WriteAttributeString,StartElement with hello:world.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteAttributeString,StartElement with hello:worldl.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_20()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("d", "Data", "http://example.org/data");
                w.WriteStartElement("g", "GoodStuff", "http://example.org/data/good");
                w.WriteAttributeString("hello", "world");
                w.WriteEndElement();
                w.WriteStartElement("BadStuff", "http://example.org/data/bad");
                w.WriteAttributeString("hello", "world");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            VerifyOutput("<d:Data xmlns:d=\"http://example.org/data\"><g:GoodStuff hello=\"world\" xmlns:g=\"http://example.org/data/good\" /><BadStuff hello=\"world\" xmlns=\"http://example.org/data/bad\" /></d:Data>");
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteStartAttribute(xml:lang),WriteRaw(0,0).Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteStartAttribute(xml:lang),WriteRaw(0,0).OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_21()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            string strraw = "abc";
            char[] buffer = strraw.ToCharArray();

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteStartAttribute("xml", "lang", null);
                w.WriteRaw(buffer, 0, 0);
                w.WriteRaw(buffer, 1, 1);
                w.WriteRaw(buffer, 0, 2);
                w.WriteEndElement();
            }
            VerifyOutput("<root xml:lang=\"bab\" />"); return TEST_PASS;
        }

        //[Variation(Desc = "WriteStartAttribute(xml:lang),WriteBinHex(0,0).Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteStartAttribute(xml:lang),WriteBinHex(0,0).OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_22()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            byte[] buffer = new byte[] { (byte)'a', (byte)'b', (byte)'c' };

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteStartAttribute("xml", "lang", null);
                w.WriteBinHex(buffer, 0, 0);
                w.WriteBinHex(buffer, 1, 1);
                w.WriteBinHex(buffer, 0, 2);
                w.WriteEndElement();
            }
            VerifyOutput("<root xml:lang=\"626162\" />"); return TEST_PASS;
        }

        //[Variation(Desc = "WriteStartAttribute(xml:lang),WriteBase64(0,0).Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "WriteStartAttribute(xml:lang),WriteBase64(0,0).OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_23()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            byte[] buffer = new byte[] { (byte)'a', (byte)'b', (byte)'c' };

            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteStartAttribute("a", "b", null);
                w.WriteBase64(buffer, 0, 0);
                w.WriteBase64(buffer, 1, 1);
                w.WriteBase64(buffer, 0, 2);
                w.WriteEndElement();
            }
            VerifyOutput("<root b=\"YmFi\" />"); return TEST_PASS;
        }

        //[Variation(Desc = "Duplicate attribute conflict when the namespace decl. is being omitted.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "Duplicate attribute conflict when the namespace decl. is being omitted.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_24()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            XmlWriter w = CreateMemWriter(wSettings);
            byte[] buffer = new byte[] { (byte)'a', (byte)'b', (byte)'c' };

            w.WriteStartElement("A");
            w.WriteAttributeString("xmlns", "p", null, "ns1");
            w.WriteStartElement("B");
            w.WriteAttributeString("xmlns", "p", null, "ns1");  // will be omitted
            try
            {
                w.WriteAttributeString("xmlns", "p", null, "ns1");
                CError.Compare(false, "error");
            }
            catch (XmlException e) { CError.WriteLine(e); }
            finally
            {
                w.Dispose();
                VerifyOutput((NamespaceHandling)this.CurVariation.Param == NamespaceHandling.OmitDuplicates ? "<A xmlns:p=\"ns1\"><B" : "<A xmlns:p=\"ns1\"><B xmlns:p=\"ns1\"");
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "1.Namespace redefinition.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "1.Namespace redefinition.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_25()
        {
            string xml = "<employees xmlns:email=\"http://www.w3c.org/some-spec-3.2\">" +
    "<employee><name>Bob Worker</name><address xmlns=\"http://postal.ie/spec-1.0\"><street>Nassau Street</street>" +
            "<city>Dublin 3</city><country>Ireland</country></address><email:address>bob.worker@hisjob.ie</email:address>" +
    "</employee></employees>";

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            using (XmlReader r = ReaderHelper.Create(new StringReader(xml)))
            {
                using (XmlWriter w = CreateMemWriter(wSettings))
                {
                    w.WriteNode(r, false);
                }
            }
            VerifyOutput(xml); return TEST_PASS;
        }

        //[Variation(Desc = "2.Default Namespace redefinition.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "2.Default Namespace redefinition.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_25a()
        {
            string xml = "<root><elem1 xmlns=\"urn:URN1\" xmlns:ns1=\"urn:URN2\"><ns1:childElem1><grandChild1 /></ns1:childElem1><childElem2><grandChild2 /></childElem2></elem1></root>";

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            using (XmlReader r = ReaderHelper.Create(new StringReader(xml)))
            {
                using (XmlWriter w = CreateMemWriter(wSettings))
                {
                    w.WriteNode(r, false);
                }
            }
            VerifyOutput(xml); return TEST_PASS;
        }

        //[Variation(Desc = "Namespaces with the same prefix different NS value.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "Namespaces with the same prefix different NS value.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_26()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("p", "e", "uri1");
                w.WriteAttributeString("p", "e", "uri1", "val");
                w.WriteAttributeString("p", "e", "uri2", "val");
                w.WriteElementString("p", "e", "uri1", "val");
                w.WriteElementString("p", "e", "uri2", "val");
            }
            VerifyOutput("<p:e p:e=\"val\" p1:e=\"val\" xmlns:p1=\"uri2\" xmlns:p=\"uri1\"><p:e>val</p:e><p:e xmlns:p=\"uri2\">val</p:e></p:e>");
            return TEST_PASS;
        }

        //[Variation(Desc = "Namespaces with the same NS value but different prefixes.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "Namespaces with the same NS value but different prefixes.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_27()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("p1", "e", "uri");
                w.WriteAttributeString("p1", "e", "uri", "val");
                w.WriteAttributeString("p2", "e2", "uri", "val");
                w.WriteElementString("p1", "e", "uri", "val");
                w.WriteElementString("p2", "e", "uri", "val");
            }
            VerifyOutput("<p1:e p1:e=\"val\" p2:e2=\"val\" xmlns:p2=\"uri\" xmlns:p1=\"uri\"><p1:e>val</p1:e><p2:e>val</p2:e></p1:e>");
            return TEST_PASS;
        }

        //[Variation(Desc = "Reader got the namespaces as a fixed value from DTD.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "Reader got the namespaces as a fixed value from DTD.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_29()
        {
            string xml = "<!DOCTYPE root [ <!ELEMENT root ANY > <!ELEMENT ns1:elem1 ANY >" +
"<!ATTLIST ns1:elem1 xmlns CDATA #FIXED \"urn:URN2\">  <!ATTLIST ns1:elem1 xmlns:ns1 CDATA #FIXED \"urn:URN1\">" +
"<!ELEMENT childElem1 ANY >  <!ATTLIST childElem1 childElem1Att1 CDATA #FIXED \"attributeValue\">]>" +
"<root>  <ns1:elem1 xmlns:ns1=\"urn:URN1\" xmlns=\"urn:URN2\">    text node in elem1    <![CDATA[<doc> content </doc>]]>" +
"<childElem1 childElem1Att1=\"attributeValue\">      <?PI in childElem1 ?>    </childElem1>    <!-- Comment in elem1 -->    &amp;  </ns1:elem1></root>";

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            XmlReaderSettings rs = new XmlReaderSettings();
            using (XmlReader r = ReaderHelper.Create(new StringReader(xml), rs))
            {
                using (XmlWriter w = CreateMemWriter(wSettings))
                {
                    w.WriteNode(r, false);
                }
            }
            VerifyOutput(xml); return TEST_PASS;
        }

        //[Variation(Desc = "Reader got the namespaces as default attribute from DTD.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "Reader got the namespaces as default attribute from DTD.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_30()
        {
            string xml = "<!DOCTYPE doc " +
"[<!ELEMENT doc ANY>" +
"<!ELEMENT test1 (#PCDATA)>" +
"<!ELEMENT test2 ANY>" +
"<!ELEMENT test3 (#PCDATA)>" +
"<!ENTITY e1 \"&e2;\">" +
"<!ENTITY e2 \"xmlns:p='x'\">" +
"<!ATTLIST test3 a1 CDATA #IMPLIED>" +
"<!ATTLIST test3 a2 CDATA #IMPLIED>" +
"]>" +
"<doc xmlns:p='&e2;'>" +
"    &e2;" +
"    <test1 xmlns:p='&e2;'>AA&e2;AA</test1>" +
"    <test2 xmlns:p='&e1;'>BB&e1;BB</test2>" +
"    <test3 a1=\"&e2;\" a2=\"&e1;\">World</test3>" +
"</doc>";
            string exp = ((NamespaceHandling)this.CurVariation.Param == NamespaceHandling.OmitDuplicates) ?
                "<!DOCTYPE doc [<!ELEMENT doc ANY><!ELEMENT test1 (#PCDATA)><!ELEMENT test2 ANY><!ELEMENT test3 (#PCDATA)><!ENTITY e1 \"&e2;\"><!ENTITY e2 \"xmlns:p='x'\"><!ATTLIST test3 a1 CDATA #IMPLIED><!ATTLIST test3 a2 CDATA #IMPLIED>]><doc xmlns:p=\"xmlns:p='x'\">    xmlns:p='x'    <test1>AAxmlns:p='x'AA</test1>    <test2>BBxmlns:p='x'BB</test2>    <test3 a1=\"xmlns:p='x'\" a2=\"xmlns:p='x'\">World</test3></doc>" :
                "<!DOCTYPE doc [<!ELEMENT doc ANY><!ELEMENT test1 (#PCDATA)><!ELEMENT test2 ANY><!ELEMENT test3 (#PCDATA)><!ENTITY e1 \"&e2;\"><!ENTITY e2 \"xmlns:p='x'\"><!ATTLIST test3 a1 CDATA #IMPLIED><!ATTLIST test3 a2 CDATA #IMPLIED>]><doc xmlns:p=\"xmlns:p='x'\">    xmlns:p='x'    <test1 xmlns:p=\"xmlns:p='x'\">AAxmlns:p='x'AA</test1>    <test2 xmlns:p=\"xmlns:p='x'\">BBxmlns:p='x'BB</test2>    <test3 a1=\"xmlns:p='x'\" a2=\"xmlns:p='x'\">World</test3></doc>";

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            XmlReaderSettings rs = new XmlReaderSettings();
            using (XmlReader r = ReaderHelper.Create(new StringReader(xml), rs))
            {
                using (XmlWriter w = CreateMemWriter(wSettings))
                {
                    w.WriteNode(r, false);
                }
            }
            VerifyOutput(exp); return TEST_PASS;
        }

        //[Variation(Desc = "Reader got the default namespaces as default attribute from DTD.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "Reader got the default namespaces as default attribute from DTD.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_30a()
        {
            string xml = "<!DOCTYPE doc " +
"[<!ELEMENT doc ANY>" +
"<!ELEMENT test1 (#PCDATA)>" +
"<!ELEMENT test2 ANY>" +
"<!ELEMENT test3 (#PCDATA)>" +
"<!ENTITY e1 \"&e2;\">" +
"<!ENTITY e2 \"xmlns='x'\">" +
"<!ATTLIST test3 a1 CDATA #IMPLIED>" +
"<!ATTLIST test3 a2 CDATA #IMPLIED>" +
"]>" +
"<doc xmlns:p='&e2;'>" +
"    &e2;" +
"    <test1 xmlns:p='&e2;'>AA&e2;AA</test1>" +
"    <test2 xmlns:p='&e1;'>BB&e1;BB</test2>" +
"    <test3 a1=\"&e2;\" a2=\"&e1;\">World</test3>" +
"</doc>";
            string exp = ((NamespaceHandling)this.CurVariation.Param == NamespaceHandling.OmitDuplicates) ?
                "<!DOCTYPE doc [<!ELEMENT doc ANY><!ELEMENT test1 (#PCDATA)><!ELEMENT test2 ANY><!ELEMENT test3 (#PCDATA)><!ENTITY e1 \"&e2;\"><!ENTITY e2 \"xmlns='x'\"><!ATTLIST test3 a1 CDATA #IMPLIED><!ATTLIST test3 a2 CDATA #IMPLIED>]><doc xmlns:p=\"xmlns='x'\">    xmlns='x'    <test1>AAxmlns='x'AA</test1>    <test2>BBxmlns='x'BB</test2>    <test3 a1=\"xmlns='x'\" a2=\"xmlns='x'\">World</test3></doc>" :
                "<!DOCTYPE doc [<!ELEMENT doc ANY><!ELEMENT test1 (#PCDATA)><!ELEMENT test2 ANY><!ELEMENT test3 (#PCDATA)><!ENTITY e1 \"&e2;\"><!ENTITY e2 \"xmlns='x'\"><!ATTLIST test3 a1 CDATA #IMPLIED><!ATTLIST test3 a2 CDATA #IMPLIED>]><doc xmlns:p=\"xmlns='x'\">    xmlns='x'    <test1 xmlns:p=\"xmlns='x'\">AAxmlns='x'AA</test1>    <test2 xmlns:p=\"xmlns='x'\">BBxmlns='x'BB</test2>    <test3 a1=\"xmlns='x'\" a2=\"xmlns='x'\">World</test3></doc>";

            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Params[0];
            XmlReaderSettings rs = new XmlReaderSettings();
            using (XmlReader r = ReaderHelper.Create(new StringReader(xml), rs))
            {
                using (XmlWriter w = CreateMemWriter(wSettings))
                {
                    w.WriteNode(r, false);
                }
            }
            VerifyOutput(exp); return TEST_PASS;
        }

        //[Variation(Desc = "XmlTextWriter : Wrong prefix management if the same prefix is used at inner level.Default", Param = NamespaceHandling.Default)]
        //[Variation(Desc = "XmlTextWriter : Wrong prefix management if the same prefix is used at inner level.OmitDuplicates", Param = NamespaceHandling.OmitDuplicates)]
        public int NS_Handling_31()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NamespaceHandling = (NamespaceHandling)this.CurVariation.Param;
            using (XmlWriter w = CreateMemWriter(wSettings))
            {
                w.WriteStartElement("test");
                w.WriteAttributeString("p", "a1", "ns1", "v");
                w.WriteStartElement("base");
                w.WriteAttributeString("a2", "ns1", "v");
                w.WriteAttributeString("p", "a3", "ns2", "v");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            VerifyOutput("<test p:a1=\"v\" xmlns:p=\"ns1\"><base p:a2=\"v\" p4:a3=\"v\" xmlns:p4=\"ns2\" /></test>");
            return TEST_PASS;
        }
    }
}

