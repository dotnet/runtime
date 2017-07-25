// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    //[TestCase(Name = "WriteNode(XmlTextReader)", Param = "XMLTEXTREADER")]
    //[TestCase(Name = "WriteNode(XmlDocument NodeReader)", Param = "XMLDOCNODEREADER")]
    //[TestCase(Name = "WriteNode(XmlDataDocument NodeReader)", Param = "XMLDATADOCNODEREADER")]
    //[TestCase(Name = "WriteNode(XsltReader)", Param = "XSLTREADER")]
    //[TestCase(Name = "WriteNode(CoreReader)", Param = "COREREADER")]
    //[TestCase(Name = "WriteNode(XPathdocument NavigatorReader)", Param = "XPATHDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteNode(XmlDocument NavigatorReader)", Param = "XMLDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteNode(XmlDataDocument NavigatorReader)", Param = "XMLDATADOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteNode(XmlBinaryReader)", Param = "XMLBINARYREADER")]
    public class TCWriteNode_XmlReader : ReaderParamTestCase
    {
        //[Variation(id = 1, Desc = "WriteNode with null reader", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader1()
        {
            XmlReader xr = null;
            using (XmlWriter w = CreateWriter())
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

        //[Variation(id = 2, Desc = "WriteNode with reader positioned on attribute, no operation", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader2()
        {
            using (XmlWriter w = CreateWriter())
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
            Assert.True(CompareReader("<Root />"));
        }

        //[Variation(id = 3, Desc = "WriteNode before reader.Read()", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader3()
        {
            using (XmlReader xr = CreateReader(new StringReader("<root />")))
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteNode(xr, false);
                }
            }

            Assert.True(CompareReader("<root />"));
        }

        //[Variation(id = 4, Desc = "WriteNode after first reader.Read()", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader4()
        {
            using (XmlReader xr = CreateReader(new StringReader("<root />")))
            {
                xr.Read();
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteNode(xr, false);
                }
            }

            Assert.True(CompareReader("<root />"));
        }

        //[Variation(id = 5, Desc = "WriteNode when reader is positioned on middle of an element node", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader5()
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
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteNode(xr, false);
                }
                CError.Compare(xr.NodeType, XmlNodeType.Comment, "Error");
                CError.Compare(xr.Value, "WriteComment", "Error");
            }
            Assert.True(CompareReader("<node2>Node Text<node3></node3><?name Instruction?></node2>"));
        }

        //[Variation(id = 6, Desc = "WriteNode when reader state is EOF", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader6()
        {
            using (XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml"))
            {
                while (xr.Read())
                { }

                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(CompareReader("<Root />"));
        }

        //[Variation(id = 7, Desc = "WriteNode when reader state is Closed", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader7()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            { }
            xr.Dispose();

            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            Assert.True(CompareReader("<Root />"));
        }

        //[Variation(id = 8, Desc = "WriteNode with reader on empty element node", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader8()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            // check reader position
            CError.Compare(xr.NodeType, XmlNodeType.EndElement, "Error");
            CError.Compare(xr.Name, "EmptyElement", "Error");
            xr.Dispose();

            Assert.True(CompareReader("<node1 />"));
        }

        //[Variation(id = 9, Desc = "WriteNode with reader on 100 Nodes", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader9()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            xr.Dispose();
            Assert.True(CompareBaseline("100Nodes.txt"));
        }

        //[Variation(id = 10, Desc = "WriteNode with reader on node with mixed content", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader10()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            // check reader position
            CError.Compare(xr.NodeType, XmlNodeType.EndElement, "Error");
            CError.Compare(xr.Name, "MixedContent", "Error");
            xr.Dispose();

            if (IsXPathDataModelReader())
            {
                Assert.True(CompareReader("<node1><?PI Instruction?><!--Comment-->Textcdata</node1>"));
            }

            Assert.True(CompareReader("<node1><?PI Instruction?><!--Comment-->Text<![CDATA[cdata]]></node1>"));
        }

        //[Variation(id = 11, Desc = "WriteNode with reader on node with declared namespace in parent", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader11()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            xr.Dispose();
            Assert.True(CompareReader("<node1 xmlns=\"foo\"></node1>"));
        }

        //[Variation(id = 12, Desc = "WriteNode with reader on node with entity reference included in element", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader12()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "EntityRef")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            xr.Dispose();
            if (!ReaderExpandsEntityRef())
                Assert.True(CompareString("<node>&e;</node>"));
            else
                Assert.True(CompareReader("<node>Test Entity</node>"));
        }

        //[Variation(id = 14, Desc = "WriteNode with element that has different prefix", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader14()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("x", "bar", "foo");
                w.WriteNode(xr, true);
                w.WriteStartElement("blah", "foo");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            xr.Dispose();

            Assert.True(CompareReader("<x:bar xmlns:x=\"foo\"><z:node xmlns:z=\"foo\" /><x:blah /></x:bar>"));
        }

        //[Variation(id = 15, Desc = "Call WriteNode with default attributes = true and DTD", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader15()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteNode(xr, true);
                w.WriteEndElement();
            }
            xr.Dispose();
            if (!ReaderParsesDTD())
                Assert.True(CompareReader("<Root><name a='b' /></Root>"));
            else
                Assert.True(CompareReader("<Root><name a='b' FIRST='KEVIN' LAST='WHITE'/></Root>"));
        }

        //[Variation(id = 16, Desc = "Call WriteNode with default attributes = false and DTD", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader16()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            if (ReaderLoosesDefaultAttrInfo())
                Assert.True(CompareReader("<Root><name a='b' FIRST='KEVIN' LAST='WHITE'/></Root>"));
            else
                Assert.True(CompareReader("<Root><name a='b' /></Root>"));
        }

        //[Variation(id = 17, Desc = "WriteNode with reader on empty element with attributes", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader17()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            Assert.True(CompareReader("<node1 a='foo' />"));
        }

        //[Variation(id = 18, Desc = "WriteNode with document containing just empty element with attributes", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader18()
        {
            string xml = "<Root a=\"foo\"/>";
            XmlReader xr = CreateReader(new StringReader(xml));
            xr.Read();
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            xr.Dispose();
            Assert.True(CompareReader("<Root a=\"foo\" />"));
        }

        //[Variation(id = 19, Desc = "Call WriteNode with special entity references as attribute value", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader19()
        {
            using (XmlWriter w = CreateWriter())
            {
                string xml = "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>";
                using (XmlReader xr = CreateReader(new StringReader(xml)))
                {
                    while (xr.Read())
                        w.WriteNode(xr, true);
                }
            }
            Assert.True(CompareReader("<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>"));
        }

        //[Variation(id = 20, Desc = "Call WriteNode with reader on doctype", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader20()
        {
            string strxml = "<!DOCTYPE ROOT []><ROOT/>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            using (XmlWriter w = CreateWriter())
            {
                while (xr.NodeType != XmlNodeType.DocumentType)
                    xr.Read();

                w.WriteNode(xr, false);
                w.WriteStartElement("ROOT");
                w.WriteEndElement();
            }
            xr.Dispose();

            Assert.True(CompareReader("<!DOCTYPE ROOT[]><ROOT />"));
        }

        //[Variation(id = 21, Desc = "Call WriteNode with full end element", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader21()
        {
            string strxml = "<root></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            xr.Dispose();
            Assert.True(CompareReader("<root></root>"));
        }

        //[Variation(Desc = "Call WriteNode with tag mismatch")]
        [Fact]
        public void writeNode_XmlReader21a()
        {
            string strxml = "<a xmlns=\"p1\"><b xmlns=\"p2\"><c xmlns=\"p1\" /></b><d xmlns=\"\"><e xmlns=\"p1\"><f xmlns=\"\" /></d></a>";
            try
            {
                using (XmlWriter w = CreateWriter())
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

        //[Variation(Desc = "Call WriteNode with default NS from DTD.UnexpToken")]
        [Fact]
        public void writeNode_XmlReader21b()
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
                using (XmlWriter w = CreateWriter())
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

        //[Variation(id = 22, Desc = "Call WriteNode with reader on element with 100 attributes", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader22()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "OneHundredAttributes")
                {
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            xr.Dispose();
            Assert.True(CompareBaseline("OneHundredAttributes.xml"));
        }

        //[Variation(id = 23, Desc = "Call WriteNode with reader on text node", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader23()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            Assert.True(CompareReader("<root>Node Text</root>"));
        }

        //[Variation(id = 24, Desc = "Call WriteNode with reader on CDATA node", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader24()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();

            Assert.True(CompareReader("<root><![CDATA[cdata content]]></root>"));
        }

        //[Variation(id = 25, Desc = "Call WriteNode with reader on PI node", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader25()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            Assert.True(CompareReader("<root><?PI Text?></root>"));
        }

        //[Variation(id = 26, Desc = "Call WriteNode with reader on Comment node", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader26()
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
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            Assert.True(CompareReader("<root><!--Comment--></root>"));
        }

        //[Variation(Desc = "Call WriteNode with reader on XmlDecl (OmitXmlDecl false)", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader28()
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
            XmlWriter w = CreateWriter(ws);
            w.WriteNode(xr, false);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            xr.Dispose();
            w.Dispose();
            strxml = IsIndent() ? "<?xml version=\"1.0\" standalone=\"yes\"?>" + Environment.NewLine + "<Root />" : strxml;
            Assert.True(CompareString(strxml));
        }

        //[Variation(id = 27, Desc = "WriteNode should only write required namespaces", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader27()
        {
            string strxml = @"<root xmlns:p1='p1'><p2:child xmlns:p2='p2' /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            Assert.True(CompareReader("<p2:child xmlns:p2='p2' />"));
        }

        //[Variation(id = 28, Desc = "Reader.WriteNode should only write required namespaces, include xmlns:xml", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader28b()
        {
            string strxml = @"<root xmlns:p1='p1'><p2:child xmlns:p2='p2' xmlns:xml='http://www.w3.org/XML/1998/namespace' /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            string exp = (WriterType == WriterType.UnicodeWriter) ? "<p2:child xmlns:p2=\"p2\" />" : "<p2:child xmlns:p2=\"p2\" xmlns:xml='http://www.w3.org/XML/1998/namespace' />";
            Assert.True(CompareReader(exp));
        }

        //[Variation(id = 29, Desc = "WriteNode should only write required namespaces, exclude xmlns:xml", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader29()
        {
            string strxml = @"<root xmlns:p1='p1' xmlns:xml='http://www.w3.org/XML/1998/namespace'><p2:child xmlns:p2='p2' /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);

                xr.Dispose();
            }
            Assert.True(CompareReader("<p2:child xmlns:p2=\"p2\" />"));
        }

        //[Variation(id = 30, Desc = "WriteNode should only write required namespaces, change default ns at top level", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader30()
        {
            string strxml = @"<root xmlns='p1'><child /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            Assert.True(CompareReader("<child xmlns='p1' />"));
        }

        //[Variation(id = 31, Desc = "WriteNode should only write required namespaces, change default ns at same level", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader31()
        {
            string strxml = @"<root xmlns:p1='p1'><child xmlns='p2'/></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            Assert.True(CompareReader("<child xmlns='p2' />"));
        }

        //[Variation(id = 32, Desc = "WriteNode should only write required namespaces, change default ns at both levels", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader32()
        {
            string strxml = @"<root xmlns='p1'><child xmlns='p2'/></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);

                xr.Dispose();
            }
            Assert.True(CompareReader("<child xmlns='p2' />"));
        }

        //[Variation(id = 33, Desc = "WriteNode should only write required namespaces, change ns uri for same prefix", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader33()
        {
            string strxml = @"<p1:root xmlns:p1='p1'><p1:child xmlns:p1='p2'/></p1:root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            Assert.True(CompareReader("<p1:child xmlns:p1='p2' />"));
        }

        //[Variation(id = 34, Desc = "WriteNode should only write required namespaces, reuse prefix from top level", Pri = 1)]
        [Fact]
        public void writeNode_XmlReader34()
        {
            string strxml = @"<root xmlns:p1='p1'><p1:child /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            Assert.True(CompareReader("<p1:child xmlns:p1='p1' />"));
        }

        //[Variation(Desc = "1. XDocument does not format content while Saving", Param = @"<?xml version='1.0'?><?pi?><?pi?>  <shouldbeindented><a>text</a></shouldbeindented><?pi?>")]
        //[Variation(Desc = "2. XDocument does not format content while Saving", Param = @"<?xml version='1.0'?><?pi?><?pi?>  <shouldbeindented><a>text</a></shouldbeindented><?pi?>")]
        [Fact]
        public void writeNode_XmlReader35()
        {
            string strxml = (string)CurVariation.Param;
            CError.WriteLine(strxml);
            XmlReader xr = CreateReader(new StringReader(strxml));
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.ConformanceLevel = (CurVariation.Desc.Contains("1.")) ? ConformanceLevel.Document : ConformanceLevel.Auto;
            ws.Indent = true;
            XmlWriter w = CreateWriter(ws);
            w.WriteNode(xr, false);
            xr.Dispose();
            w.Dispose();
            Assert.True(CompareReader(strxml));
        }

        //[Variation(Desc = "1.WriteNode with ascii encoding", Param = true)]
        //[Variation(Desc = "2.WriteNode with ascii encoding", Param = false)]
        [Fact]
        public void writeNode_XmlReader36()
        {
            string strxml = "<Ro\u00F6t \u00F6=\"\u00F6\" />";
            string exp = strxml;

            XmlReader xr = CreateReader(new StringReader(strxml));

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            XmlWriter w = CreateWriter(ws);
            while (!xr.EOF)
            {
                w.WriteNode(xr, (bool)CurVariation.Param);
            }
            xr.Dispose();
            w.Dispose();
            Assert.True((CompareString(exp)));
        }

        //[Variation(Desc = "WriteNode DTD PUBLIC with identifier", Param = true)]
        //[Variation(Desc = "WriteNode DTD PUBLIC with identifier", Param = false)]
        [Fact]
        public void writeNode_XmlReader37()
        {
            string strxml = "<!DOCTYPE root PUBLIC \"\" \"#\"><root/>";
            string exp = (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent) ?
               "<!DOCTYPE root PUBLIC \"\" \"#\"[]>" + nl + "<root />" :
               "<!DOCTYPE root PUBLIC \"\" \"#\"[]><root />";
            try
            {
                XmlReader xr = CreateReader(new StringReader(strxml));
                using (XmlWriter w = CreateWriter())
                {
                    while (!xr.EOF)
                    {
                        w.WriteNode(xr, (bool)CurVariation.Param);
                    }
                    xr.Dispose();
                }
            }
            catch (FileNotFoundException e) { CError.WriteLine(e); return; }
            catch (XmlException e) { CError.WriteLine(e); return; }
            Assert.True(false);
        }

        //[Variation(Desc = "WriteNode DTD SYSTEM with identifier", Param = true)]
        //[Variation(Desc = "WriteNode DTD SYSTEM with identifier", Param = false)]
        [Fact]
        public void writeNode_XmlReader38()
        {
            string strxml = "<!DOCTYPE root SYSTEM \"#\"><root/>";
            try
            {
                using (XmlReader xr = CreateReader(new StringReader(strxml)))
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteNode(xr, (bool)CurVariation.Param);
                    }
                }
            }
            catch (XmlException e) { CError.WriteLine(e); return; }
            Assert.True(false);
        }

        //[Variation(Desc = "WriteNode DTD SYSTEM with valid surrogate pair", Param = true)]
        //[Variation(Desc = "WriteNode DTD SYSTEM with valid surrogate pair", Param = false)]
        [Fact]
        public void writeNode_XmlReader39()
        {
            string strxml = "<!DOCTYPE root SYSTEM \"\uD812\uDD12\"><root/>";
            string exp = "<!DOCTYPE root SYSTEM \"\uD812\uDD12\"[]><root />";
            try
            {
                using (XmlReader xr = CreateReader(new StringReader(strxml)))
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteNode(xr, (bool)CurVariation.Param);
                    }
                }
            }
            catch (XmlException e) { CError.WriteLine(e); return; }
            catch (FileNotFoundException e) { CError.WriteLine(e); return; }
            if (WriterType == WriterType.CharCheckingWriter)
            {
                Assert.True((CompareString(exp)));
            }
            Assert.True(false);
        }
    }
}
