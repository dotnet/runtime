// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public class TCWriteNode_XmlReader : ReaderParamTestCase
    {
        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader1(XmlWriterUtils utils)
        {
            XmlReader xr = null;
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, false);
                }
                catch (ArgumentNullException)
                {
                    CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader2(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                using (XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "defattr")
                        {
                            xr.Read();
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }

                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        CError.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                        xr.Dispose();
                        w.Dispose();
                        Assert.True(false);
                    }
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(utils.CompareReader("<Root />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader3(XmlWriterUtils utils)
        {
            using (XmlReader xr = CreateReader(new StringReader("<root />")))
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteNode(xr, false);
                }
            }

            Assert.True(utils.CompareReader("<root />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader4(XmlWriterUtils utils)
        {
            using (XmlReader xr = CreateReader(new StringReader("<root />")))
            {
                xr.Read();
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteNode(xr, false);
                }
            }

            Assert.True(utils.CompareReader("<root />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader5(XmlWriterUtils utils)
        {
            using (XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml"))
            {
                while (xr.Read())
                {
                    if (xr.LocalName == "Middle")
                    {
                        xr.Read();
                        break;
                    }
                }
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteNode(xr, false);
                }
                CError.Compare(xr.NodeType, XmlNodeType.Comment, "Error");
                CError.Compare(xr.Value, "WriteComment", "Error");
            }
            Assert.True(utils.CompareReader("<node2>Node Text<node3></node3><?name Instruction?></node2>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader6(XmlWriterUtils utils)
        {
            using (XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml"))
            {
                while (xr.Read())
                { }

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(utils.CompareReader("<Root />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader7(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            { }
            xr.Dispose();

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader8(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "EmptyElement")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            // check reader position
            CError.Compare(xr.NodeType, XmlNodeType.EndElement, "Error");
            CError.Compare(xr.Name, "EmptyElement", "Error");
            xr.Dispose();

            Assert.True(utils.CompareReader("<node1 />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader9(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "OneHundredElements")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            xr.Dispose();
            Assert.True(utils.CompareBaseline("100Nodes.txt"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader10(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "MixedContent")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            // check reader position
            CError.Compare(xr.NodeType, XmlNodeType.EndElement, "Error");
            CError.Compare(xr.Name, "MixedContent", "Error");
            xr.Dispose();

            if (IsXPathDataModelReader())
            {
                Assert.True(utils.CompareReader("<node1><?PI Instruction?><!--Comment-->Textcdata</node1>"));
            }

            Assert.True(utils.CompareReader("<node1><?PI Instruction?><!--Comment-->Text<![CDATA[cdata]]></node1>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader11(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "NamespaceNoPrefix")
                {
                    xr.Read();
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            xr.Dispose();
            Assert.True(utils.CompareReader("<node1 xmlns=\"foo\"></node1>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader12(XmlWriterUtils utils)
        {
            using (XmlReader xr = CreateReaderIgnoreWSFromString("<!DOCTYPE node [ <!ENTITY test \"Test Entity\"> ]><node>&test;</node>"))
            {
                bool sanityCheck = false;
                while (xr.Read())
                {
                    if (xr.NodeType == XmlNodeType.Element && xr.LocalName == "node")
                    {
                        sanityCheck = true;
                        break;
                    }
                }

                Assert.True(sanityCheck, "error in input doc");

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteNode(xr, false);
                }
            }

            Assert.Equal("<node>Test Entity</node>", utils.GetString());
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader14(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "DiffPrefix")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("x", "bar", "foo");
                w.WriteNode(xr, true);
                w.WriteStartElement("blah", "foo");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            xr.Dispose();

            Assert.True(utils.CompareReader("<x:bar xmlns:x=\"foo\"><z:node xmlns:z=\"foo\" /><x:blah /></x:bar>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader15(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "DefaultAttributesTrue")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteNode(xr, true);
                w.WriteEndElement();
            }
            xr.Dispose();
            if (!ReaderParsesDTD())
                Assert.True(utils.CompareReader("<Root><name a='b' /></Root>"));
            else
                Assert.True(utils.CompareReader("<Root><name a='b' FIRST='KEVIN' LAST='WHITE'/></Root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader16(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "DefaultAttributesTrue")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            if (ReaderLoosesDefaultAttrInfo())
                Assert.True(utils.CompareReader("<Root><name a='b' FIRST='KEVIN' LAST='WHITE'/></Root>"));
            else
                Assert.True(utils.CompareReader("<Root><name a='b' /></Root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader17(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "EmptyElementWithAttributes")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            Assert.True(utils.CompareReader("<node1 a='foo' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader18(XmlWriterUtils utils)
        {
            string xml = "<Root a=\"foo\"/>";
            XmlReader xr = CreateReader(new StringReader(xml));
            xr.Read();
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            xr.Dispose();
            Assert.True(utils.CompareReader("<Root a=\"foo\" />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader19(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                string xml = "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>";
                using (XmlReader xr = CreateReader(new StringReader(xml)))
                {
                    while (xr.Read())
                        w.WriteNode(xr, true);
                }
            }
            Assert.True(utils.CompareReader("<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader20(XmlWriterUtils utils)
        {
            string strxml = "<!DOCTYPE ROOT []><ROOT/>";

            string exp = utils.IsIndent() ?
                "<!DOCTYPE ROOT []>" + Environment.NewLine + "<ROOT />" :
                "<!DOCTYPE ROOT []><ROOT />";

            using (XmlReader xr = CreateReader(new StringReader(strxml)))
            using (XmlWriter w = utils.CreateWriter())
            {
                while (xr.NodeType != XmlNodeType.DocumentType)
                    xr.Read();

                w.WriteNode(xr, false);
                w.WriteStartElement("ROOT");
                w.WriteEndElement();
            }

            Assert.Equal(exp, utils.GetString());
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader21(XmlWriterUtils utils)
        {
            string strxml = "<root></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            xr.Dispose();
            Assert.True(utils.CompareReader("<root></root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader21a(XmlWriterUtils utils)
        {
            string strxml = "<a xmlns=\"p1\"><b xmlns=\"p2\"><c xmlns=\"p1\" /></b><d xmlns=\"\"><e xmlns=\"p1\"><f xmlns=\"\" /></d></a>";
            try
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        using (XmlReader xr = CreateReader(new StringReader(strxml)))
                        {
                            w.WriteNode(xr, true);
                            CError.Compare(false, "Failed");
                        }
                    }
                    catch (XmlException xe) { CError.WriteLine(xe.Message); return; }
                }
            }
            catch (ObjectDisposedException e) { CError.WriteLine(e.Message); return; }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader21b(XmlWriterUtils utils)
        {
            string strxml = "<!DOCTYPE doc " +
"[<!ELEMENT doc ANY>" +
"<!ELEMENT test1 (#PCDATA)>" +
"<!ELEMENT test2 ANY>" +
"<!ELEMENT test3 (#PCDATA)>" +
"<!ENTITY e1 \"&e2;\">" +
"<!ENTITY e2 \"xmlns=\"x\"\">" +
"<!ATTLIST test3 a1 CDATA #IMPLIED>" +
"<!ATTLIST test3 a2 CDATA #IMPLIED>" +
"]>" +
"<doc>" +
"    &e2;" +
"    <test1>AA&e2;AA</test1>" +
"    <test2>BB&e1;BB</test2>" +
"    <test3 a1=\"&e2;\" a2=\"&e1;\">World</test3>" +
"</doc>";
            try
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        using (XmlReader xr = CreateReader(new StringReader(strxml)))
                        {
                            w.WriteNode(xr, true);
                            CError.Compare(false, "Failed");
                        }
                    }
                    catch (XmlException xe) { CError.WriteLine(xe.Message); return; }
                }
            }
            catch (ObjectDisposedException e) { CError.WriteLine(e.Message); return; }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader22(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "OneHundredAttributes")
                {
                    break;
                }
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            xr.Dispose();
            Assert.True(utils.CompareBaseline("OneHundredAttributes.xml"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader23(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "Middle")
                {
                    xr.Read();
                    xr.Read();
                    break;
                }
            }
            if (xr.NodeType != XmlNodeType.Text)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType);
                xr.Dispose();
                Assert.True(false);
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            Assert.True(utils.CompareReader("<root>Node Text</root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader24(XmlWriterUtils utils)
        {
            if (IsXPathDataModelReader())
            {
                CError.WriteLine("XPath data model does not have CDATA node type, so {0} can not be positioned on CDATA", readerType);
                return;
            }

            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "CDataNode")
                {
                    xr.Read();
                    break;
                }
            }
            if (xr.NodeType != XmlNodeType.CDATA)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType);
                xr.Dispose();
                Assert.True(false);
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();

            Assert.True(utils.CompareReader("<root><![CDATA[cdata content]]></root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader25(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "PINode")
                {
                    xr.Read();
                    break;
                }
            }
            if (xr.NodeType != XmlNodeType.ProcessingInstruction)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType);
                xr.Dispose();
                Assert.True(false);
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            Assert.True(utils.CompareReader("<root><?PI Text?></root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader26(XmlWriterUtils utils)
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "CommentNode")
                {
                    xr.Read();
                    break;
                }
            }
            if (xr.NodeType != XmlNodeType.Comment)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType);
                xr.Dispose();
                Assert.True(false);
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            Assert.True(utils.CompareReader("<root><!--Comment--></root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader28(XmlWriterUtils utils)
        {
            string strxml = "<?xml version=\"1.0\" standalone=\"yes\"?><Root />";
            XmlReader xr = CreateReader(new StringReader(strxml));

            xr.Read();
            if (xr.NodeType != XmlNodeType.XmlDeclaration)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType);
                xr.Dispose();
                return;
            }

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = false;
            XmlWriter w = utils.CreateWriter(ws);
            w.WriteNode(xr, false);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            xr.Dispose();
            w.Dispose();
            strxml = utils.IsIndent() ? "<?xml version=\"1.0\" standalone=\"yes\"?>" + Environment.NewLine + "<Root />" : strxml;
            Assert.True(utils.CompareString(strxml));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader27(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns:p1='p1'><p2:child xmlns:p2='p2' /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            Assert.True(utils.CompareReader("<p2:child xmlns:p2='p2' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader28b(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns:p1='p1'><p2:child xmlns:p2='p2' xmlns:xml='http://www.w3.org/XML/1998/namespace' /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            string exp = (utils.WriterType == WriterType.UnicodeWriter) ? "<p2:child xmlns:p2=\"p2\" />" : "<p2:child xmlns:p2=\"p2\" xmlns:xml='http://www.w3.org/XML/1998/namespace' />";
            Assert.True(utils.CompareReader(exp));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader29(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns:p1='p1' xmlns:xml='http://www.w3.org/XML/1998/namespace'><p2:child xmlns:p2='p2' /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);

                xr.Dispose();
            }
            Assert.True(utils.CompareReader("<p2:child xmlns:p2=\"p2\" />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader30(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns='p1'><child /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            Assert.True(utils.CompareReader("<child xmlns='p1' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader31(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns:p1='p1'><child xmlns='p2'/></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            Assert.True(utils.CompareReader("<child xmlns='p2' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader32(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns='p1'><child xmlns='p2'/></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);

                xr.Dispose();
            }
            Assert.True(utils.CompareReader("<child xmlns='p2' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader33(XmlWriterUtils utils)
        {
            string strxml = @"<p1:root xmlns:p1='p1'><p1:child xmlns:p1='p2'/></p1:root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            Assert.True(utils.CompareReader("<p1:child xmlns:p1='p2' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XmlReader34(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns:p1='p1'><p1:child /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            Assert.True(utils.CompareReader("<p1:child xmlns:p1='p1' />"));
        }

        [Theory]
        [XmlWriterInlineData(ConformanceLevel.Document)]
        [XmlWriterInlineData(ConformanceLevel.Auto)]
        public void writeNode_XmlReader35(XmlWriterUtils utils, ConformanceLevel conformanceLevel)
        {
            string strxml = @"<?xml version='1.0'?><?pi?><?pi?>  <shouldbeindented><a>text</a></shouldbeindented><?pi?>";
            CError.WriteLine(strxml);
            XmlReader xr = CreateReader(new StringReader(strxml));
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.ConformanceLevel = conformanceLevel;
            ws.Indent = true;
            XmlWriter w = utils.CreateWriter(ws);
            w.WriteNode(xr, false);
            xr.Dispose();
            w.Dispose();
            Assert.True(utils.CompareReader(strxml));
        }

        [Theory]
        [XmlWriterInlineData(true)]
        [XmlWriterInlineData(false)]
        public void writeNode_XmlReader36(XmlWriterUtils utils, bool defattr)
        {
            string strxml = "<Ro\u00F6t \u00F6=\"\u00F6\" />";
            string exp = strxml;

            XmlReader xr = CreateReader(new StringReader(strxml));

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            XmlWriter w = utils.CreateWriter(ws);
            while (!xr.EOF)
            {
                w.WriteNode(xr, defattr);
            }
            xr.Dispose();
            w.Dispose();
            Assert.True(utils.CompareString(exp));
        }

        [Theory]
        [XmlWriterInlineData(true)]
        [XmlWriterInlineData(false)]
        public void writeNode_XmlReader37(XmlWriterUtils utils, bool defattr)
        {
            string strxml = "<!DOCTYPE root PUBLIC \"\" \"#\"><root/>";
            string exp = utils.IsIndent() ?
               "<!DOCTYPE root PUBLIC \"\" \"#\"[]>" + Environment.NewLine + "<root />" :
               "<!DOCTYPE root PUBLIC \"\" \"#\"[]><root />";

            using (XmlReader xr = CreateReader(new StringReader(strxml)))
            using (XmlWriter w = utils.CreateWriter())
            {
                while (!xr.EOF)
                {
                    w.WriteNode(xr, defattr);
                }
            }

            Assert.True(utils.CompareString(exp));
        }

        [Theory]
        [XmlWriterInlineData(true)]
        [XmlWriterInlineData(false)]
        public void writeNode_XmlReader38(XmlWriterUtils utils, bool defattr)
        {
            string strxml = "<!DOCTYPE root SYSTEM \"#\"><root/>";
            try
            {
                using (XmlReader xr = CreateReader(new StringReader(strxml)))
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteNode(xr, defattr);
                    }
                }
            }
            catch (XmlException e) { CError.WriteLine(e); return; }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData(true)]
        [XmlWriterInlineData(false)]
        public void writeNode_XmlReader39(XmlWriterUtils utils, bool defattr)
        {
            string strxml = "<!DOCTYPE root SYSTEM \"\uD812\uDD12\"><root/>";
            string exp = utils.IsIndent() ?
                "<!DOCTYPE root SYSTEM \"\uD812\uDD12\"[]>" + Environment.NewLine + "<root />" :
                "<!DOCTYPE root SYSTEM \"\uD812\uDD12\"[]><root />";

            using (XmlReader xr = CreateReader(new StringReader(strxml)))
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteNode(xr, defattr);
                }
            }

            Assert.Equal(exp, utils.GetString());
        }
    }
}
