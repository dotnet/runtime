// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml.XPath;
using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.XmlWriterApiTests
{
    public class TCWriteNode_XPathNavigator : ReaderParamTestCase
    {
        private XPathNavigator ToNavigator(XmlReader reader)
        {
            var document = new XPathDocument(reader);
            return document.CreateNavigator();
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator1(XmlWriterUtils utils)
        {
            XPathNavigator xpn = null;
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteNode(xpn, false);
                }
                catch (ArgumentNullException)
                {
                    CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.Fail();
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator2(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                using (XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml"))
                {
                    XPathNavigator xpn = ToNavigator(xr);
                    xpn.MoveToFollowing("defattr", "");
                    xpn.MoveToFirstChild();
                    xpn.MoveToFirstAttribute();

                    CError.Compare(xpn.NodeType, XPathNodeType.Attribute, "Error");

                    w.WriteStartElement("Root");
                    w.WriteNode(xpn, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(utils.CompareReader("<Root />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator3(XmlWriterUtils utils)
        {
            using (XmlReader xr = CreateReader(new StringReader("<root />")))
            {
                XPathNavigator xpn = ToNavigator(xr);
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteNode(xpn, false);
                }
            }

            Assert.True(utils.CompareReader("<root />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator4(XmlWriterUtils utils)
        {
            using (XmlReader xr = CreateReader(new StringReader("<root />")))
            {
                XPathNavigator xpn = ToNavigator(xr);
                xpn.MoveToFirstChild();
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteNode(xpn, false);
                }
            }

            Assert.True(utils.CompareReader("<root />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator5(XmlWriterUtils utils)
        {
            using (XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml"))
            {
                XPathNavigator xpn = ToNavigator(xr);
                xpn.MoveToFollowing("Middle", "");
                xpn.MoveToFirstChild();
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteNode(xpn, false);
                }

                xpn.MoveToNext();
                CError.Compare(xpn.NodeType, XPathNodeType.Comment, "Error");
                CError.Compare(xpn.Value, "WriteComment", "Error");
            }
            Assert.True(utils.CompareReader("<node2>Node Text<node3></node3><?name Instruction?></node2>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator8(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("EmptyElement", "");
            xpn.MoveToFirstChild();
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }

            xpn.MoveToParent();
            CError.Compare(xpn.NodeType, XPathNodeType.Element, "Error");
            CError.Compare(xpn.Name, "EmptyElement", "Error");

            Assert.True(utils.CompareReader("<node1 />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator9(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("OneHundredElements", "");
            xpn.MoveToFirstChild();
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }
            Assert.True(utils.CompareBaseline("100Nodes.txt"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator10(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("MixedContent", "");
            xpn.MoveToFirstChild();
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }

            // check position
            xpn.MoveToParent();
            CError.Compare(xpn.NodeType, XPathNodeType.Element, "Error");
            CError.Compare(xpn.Name, "MixedContent", "Error");

            Assert.True(utils.CompareReader("<node1><?PI Instruction?><!--Comment-->Textcdata</node1>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator11(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("NamespaceNoPrefix", "");
            xpn.MoveToFirstChild();
            xpn.MoveToFirstChild();
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }

            // check position
            CError.Compare(xpn.NodeType, XPathNodeType.Element, "Error");
            CError.Compare(xpn.Name, "node1", "Error");

            Assert.True(utils.CompareReader("<node1 xmlns=\"foo\"></node1>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator12(XmlWriterUtils utils)
        {
            using (XmlReader xr = CreateReaderIgnoreWSFromString("<!DOCTYPE node [ <!ENTITY test \"Test Entity\"> ]><node>&test;</node>"))
            {
                XPathNavigator xpn = ToNavigator(xr);
                xpn.MoveToFollowing("node", "");

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteNode(xpn, false);
                }

                // check position
                CError.Compare(xpn.NodeType, XPathNodeType.Element, "Error");
                CError.Compare(xpn.Name, "node", "Error");
            }

            Assert.Equal("<node>Test Entity</node>", utils.GetString());
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator14(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("DiffPrefix", "");
            xpn.MoveToFirstChild();

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("x", "bar", "foo");
                w.WriteNode(xpn, true);
                w.WriteStartElement("blah", "foo");
                w.WriteEndElement();
                w.WriteEndElement();
            }

            // check position
            xpn.MoveToParent();
            CError.Compare(xpn.NodeType, XPathNodeType.Element, "Error");
            CError.Compare(xpn.Name, "DiffPrefix", "Error");

            Assert.True(utils.CompareReader("<x:bar xmlns:x=\"foo\"><z:node xmlns:z=\"foo\" /><x:blah /></x:bar>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator15(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("DefaultAttributesTrue", "");
            xpn.MoveToFirstChild();
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteNode(xpn, true);
                w.WriteEndElement();
            }

            if (!ReaderParsesDTD())
                Assert.True(utils.CompareReader("<Root><name a='b' /></Root>"));
            else
                Assert.True(utils.CompareReader("<Root><name a='b' FIRST='KEVIN' LAST='WHITE'/></Root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator16(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("DefaultAttributesTrue", "");
            xpn.MoveToFirstChild();
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteNode(xpn, false);
                w.WriteEndElement();
            }

            if (ReaderLoosesDefaultAttrInfo())
                Assert.True(utils.CompareReader("<Root><name a='b' FIRST='KEVIN' LAST='WHITE'/></Root>"));
            else
                Assert.True(utils.CompareReader("<Root><name a='b' /></Root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator17(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("EmptyElementWithAttributes", "");
            xpn.MoveToFirstChild();
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }
            Assert.True(utils.CompareReader("<node1 a='foo' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator18(XmlWriterUtils utils)
        {
            string xml = "<Root a=\"foo\"/>";
            using XmlReader xr = CreateReader(new StringReader(xml));
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFirstChild();
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }

            Assert.True(utils.CompareReader("<Root a=\"foo\" />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator19(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                string xml = "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>";
                using (XmlReader xr = CreateReader(new StringReader(xml)))
                {
                    XPathNavigator xpn = ToNavigator(xr);
                    w.WriteNode(xpn, true);
                }
            }
            Assert.True(utils.CompareReader("<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator21(XmlWriterUtils utils)
        {
            string strxml = "<root></root>";
            using XmlReader xr = CreateReader(new StringReader(strxml));
            XPathNavigator xpn = ToNavigator(xr);
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }

            Assert.True(utils.CompareReader("<root></root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator22(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("OneHundredAttributes", "");
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }
            Assert.True(utils.CompareBaseline("OneHundredAttributes.xml"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator23(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("Middle", "");
            xpn.MoveToFirstChild();
            xpn.MoveToFirstChild();

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xpn, false);
                w.WriteEndElement();
            }

            Assert.True(utils.CompareReader("<root>Node Text</root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator25(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("PINode", "");
            xpn.MoveToFirstChild();

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xpn, false);
                w.WriteEndElement();
            }

            // check position
            xpn.MoveToParent();
            CError.Compare(xpn.NodeType, XPathNodeType.Element, "Error");
            CError.Compare(xpn.Name, "PINode", "Error");

            Assert.True(utils.CompareReader("<root><?PI Text?></root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator26(XmlWriterUtils utils)
        {
            using XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("CommentNode", "");
            xpn.MoveToFirstChild();

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xpn, false);
                w.WriteEndElement();
            }

            // check position
            xpn.MoveToParent();
            CError.Compare(xpn.NodeType, XPathNodeType.Element, "Error");
            CError.Compare(xpn.Name, "CommentNode", "Error");

            Assert.True(utils.CompareReader("<root><!--Comment--></root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator27(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns:p1='p1'><p2:child xmlns:p2='p2' /></root>";
            using XmlReader xr = CreateReader(new StringReader(strxml));
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("child", "p2");
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }
            Assert.True(utils.CompareReader("<p2:child xmlns:p2='p2' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator28b(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns:p1='p1'><p2:child xmlns:p2='p2' xmlns:xml='http://www.w3.org/XML/1998/namespace' /></root>";
            using XmlReader xr = CreateReader(new StringReader(strxml));
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("child", "p2");
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }
            string exp = (utils.WriterType == WriterType.UnicodeWriter) ? "<p2:child xmlns:p2=\"p2\" />" : "<p2:child xmlns:p2=\"p2\" xmlns:xml='http://www.w3.org/XML/1998/namespace' />";
            Assert.True(utils.CompareReader(exp));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator29(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns:p1='p1' xmlns:xml='http://www.w3.org/XML/1998/namespace'><p2:child xmlns:p2='p2' /></root>";
            using XmlReader xr = CreateReader(new StringReader(strxml));
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("child", "p2");
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }
            Assert.True(utils.CompareReader("<p2:child xmlns:p2=\"p2\" />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator30(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns='p1'><child /></root>";
            using XmlReader xr = CreateReader(new StringReader(strxml));
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("child", "p1");
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }
            Assert.True(utils.CompareReader("<child xmlns='p1' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator31(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns:p1='p1'><child xmlns='p2'/></root>";
            using XmlReader xr = CreateReader(new StringReader(strxml));
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("child", "p2");
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }
            Assert.True(utils.CompareReader("<child xmlns='p2' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator32(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns='p1'><child xmlns='p2'/></root>";
            using XmlReader xr = CreateReader(new StringReader(strxml));
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("child", "p2");
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }
            Assert.True(utils.CompareReader("<child xmlns='p2' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator33(XmlWriterUtils utils)
        {
            string strxml = @"<p1:root xmlns:p1='p1'><p1:child xmlns:p1='p2'/></p1:root>";
            using XmlReader xr = CreateReader(new StringReader(strxml));
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("child", "p2");
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }
            Assert.True(utils.CompareReader("<p1:child xmlns:p1='p2' />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void writeNode_XPathNavigator34(XmlWriterUtils utils)
        {
            string strxml = @"<root xmlns:p1='p1'><p1:child /></root>";
            using XmlReader xr = CreateReader(new StringReader(strxml));
            XPathNavigator xpn = ToNavigator(xr);
            xpn.MoveToFollowing("child", "p1");
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(xpn, false);
            }
            Assert.True(utils.CompareReader("<p1:child xmlns:p1='p1' />"));
        }

        [Theory]
        [XmlWriterInlineData(ConformanceLevel.Document)]
        [XmlWriterInlineData(ConformanceLevel.Auto)]
        public void writeNode_XPathNavigator35(XmlWriterUtils utils, ConformanceLevel conformanceLevel)
        {
            string strxml = @"<?xml version='1.0'?><?pi?><?pi?>  <shouldbeindented><a>text</a></shouldbeindented><?pi?>";
            using XmlReader xr = CreateReader(new StringReader(strxml));
            XPathNavigator xpn = ToNavigator(xr);

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.ConformanceLevel = conformanceLevel;
            ws.Indent = true;
            using (XmlWriter w = utils.CreateWriter(ws))
            {
                w.WriteNode(xpn, false);
            }

            Assert.True(utils.CompareReader(strxml));
        }

        [Theory]
        [XmlWriterInlineData(true)]
        [XmlWriterInlineData(false)]
        public void writeNode_XPathNavigator36(XmlWriterUtils utils, bool defattr)
        {
            string strxml = "<Ro\u00F6t \u00F6=\"\u00F6\" />";
            using XmlReader xr = CreateReader(new StringReader(strxml));
            XPathNavigator xpn = ToNavigator(xr);

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (XmlWriter w = utils.CreateWriter(ws))
            {
                w.WriteNode(xpn, defattr);
            }

            Assert.True(utils.CompareString(strxml));
        }
    }
}
