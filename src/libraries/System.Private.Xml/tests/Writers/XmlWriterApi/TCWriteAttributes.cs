// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using Xunit;

namespace System.Xml.Tests
{
    //[TestCase(Name = "WriteAttributes(XmlTextReader)", Param = "XMLTEXTREADER")]
    //[TestCase(Name = "WriteAttributes(XmlDocument NodeReader)", Param = "XMLDOCNODEREADER")]
    //[TestCase(Name = "WriteAttributes(XmlDataDocument NodeReader)", Param = "XMLDATADOCNODEREADER")]
    //[TestCase(Name = "WriteAttributes(XsltReader)", Param = "XSLTREADER")]
    //[TestCase(Name = "WriteAttributes(CoreReader)", Param = "COREREADER")]
    //[TestCase(Name = "WriteAttributes(XPathdocument NavigatorReader)", Param = "XPATHDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteAttributes(XmlDocument NavigatorReader)", Param = "XMLDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteAttributes(XmlDataDocument NavigatorReader)", Param = "XMLDATADOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteAttributes(XmlBinaryReader)", Param = "XMLBINARYREADER")]
    public class TCWriteAttributes : ReaderParamTestCase
    {
        //[Variation(id = 1, Desc = "Call WriteAttributes with default DTD attributes = true", Pri = 1)]
        [Fact]
        public void writeAttributes_1()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "name")
                        {
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, true);
                    w.WriteEndElement();
                }
            }
            Assert.True(CompareReader("<Root a=\"b\" FIRST=\"KEVIN\" LAST=\"WHITE\" />"));
        }

        //[Variation(id = 2, Desc = "Call WriteAttributes with default DTD attributes = false", Pri = 1)]
        [Fact]
        public void writeAttributes_2()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "name")
                        {
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }

            if (IsXPathDataModelReader())
                // always sees default attributes
                Assert.True(CompareReader("<Root a=\"b\" FIRST=\"KEVIN\" LAST=\"WHITE\" />"));
            else
                Assert.True(CompareReader("<Root a=\"b\" />"));
        }

        //[Variation(id = 3, Desc = "Call WriteAttributes with XmlReader = null")]
        [Fact]
        public void writeAttributes_3()
        {
            using (XmlWriter w = CreateWriter())
            {
                XmlReader xr = null;
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                }
                catch (ArgumentNullException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        //[Variation(id = 4, Desc = "Call WriteAttributes when reader is located on element", Pri = 1)]
        [Fact]
        public void writeAttributes_4()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "AttributesGeneric")
                        {
                            do
                            { xr.Read(); } while (xr.LocalName != "node");
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Element)
                    {
                        CError.WriteLine("Reader not positioned element");
                        CError.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        Assert.True(false);
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(CompareReader("<Root a=\"b\" c=\"d\" e=\"f\" />"));
        }

        //[Variation(id = 5, Desc = "Call WriteAttributes when reader is located in the middle attribute", Pri = 1)]
        [Fact]
        public void writeAttributes_5()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "AttributesGeneric")
                        {
                            do
                            { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToAttribute(1);
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        CError.WriteLine("Reader not positioned on attribute");
                        CError.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        Assert.True(false);
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(CompareReader("<Root c=\"d\" e=\"f\" />"));
        }

        //[Variation(id = 6, Desc = "Call WriteAttributes when reader is located in the last attribute", Pri = 1)]
        [Fact]
        public void writeAttributes_6()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "AttributesGeneric")
                        {
                            do
                            { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToNextAttribute();
                            xr.MoveToNextAttribute();
                            xr.MoveToNextAttribute();
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        CError.WriteLine("Reader not positioned on attribute");
                        CError.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        Assert.True(false);
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(CompareReader("<Root e=\"f\" />"));
        }

        //[Variation(id = 7, Desc = "Call WriteAttributes when reader is located on an attribute with an entity reference in the value", Pri = 1)]
        [Fact]
        public void writeAttributes_7()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "AttributesEntity")
                        {
                            do
                            { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToNextAttribute();
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        CError.WriteLine("Reader not positioned on attribute");
                        CError.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        Assert.True(false);
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            if (!ReaderExpandsEntityRef())
                Assert.True(CompareString("<Root a=\"&e;\" />"));
            else
                Assert.True(CompareReader("<Root a=\"Test Entity\" />"));
        }

        //[Variation(id = 8, Desc = "Call WriteAttributes with reader on XmlDeclaration", Pri = 1)]
        [Fact]
        public void writeAttributes_8()
        {
            if (IsXPathDataModelReader())
            {
                CError.WriteLine("{0} does not support XmlDecl node", readerType);
                return;
            }
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("Simple.xml"))
                {
                    xr.Read();
                    if (xr.NodeType != XmlNodeType.XmlDeclaration)
                    {
                        CError.WriteLine("Reader not positioned on XmlDeclaration");
                        CError.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        Assert.True(false);
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(CompareReader("<Root version=\"1.0\" standalone=\"yes\" />"));
        }

        //[Variation(id = 9, Desc = "Call WriteAttributes with reader on DocType", Pri = 1, Param = "DocumentType")]
        //[Variation(id = 10, Desc = "Call WriteAttributes with reader on CDATA", Pri = 1, Param = "CDATA")]
        //[Variation(id = 11, Desc = "Call WriteAttributes with reader on Text", Pri = 1, Param = "Text")]
        //[Variation(id = 12, Desc = "Call WriteAttributes with reader on PI", Pri = 1, Param = "ProcessingInstruction")]
        //[Variation(id = 13, Desc = "Call WriteAttributes with reader on Comment", Pri = 1, Param = "Comment")]
        //[Variation(id = 14, Desc = "Call WriteAttributes with reader on EntityRef", Pri = 1, Param = "EntityReference")]
        //[Variation(id = 15, Desc = "Call WriteAttributes with reader on Whitespace", Pri = 1, Param = "Whitespace")]
        //[Variation(id = 16, Desc = "Call WriteAttributes with reader on SignificantWhitespace", Pri = 1, Param = "SignificantWhitespace")]
        [Fact]
        public void writeAttributes_9()
        {
            string strxml = "";
            switch (CurVariation.Param.ToString())
            {
                case "DocumentType":
                    if (IsXPathDataModelReader())
                    {
                        CError.WriteLine("{0} does not support DocumentType node", readerType);
                        return;
                    }
                    strxml = "<!DOCTYPE Root[]><Root/>";
                    break;
                case "CDATA":
                    if (IsXPathDataModelReader())
                    {
                        CError.WriteLine("{0} does not support CDATA node", readerType);
                        return;
                    }
                    strxml = "<root><![CDATA[Test]]></root>";
                    break;
                case "Text":
                    strxml = "<root>Test</root>";
                    break;
                case "ProcessingInstruction":
                    strxml = "<root><?pi test?></root>";
                    break;
                case "Comment":
                    strxml = "<root><!-- comment --></root>";
                    break;
                case "EntityReference":
                    if (!ReaderSupportsEntityRef())
                    {
                        CError.WriteLine("{0} does not support EntityRef node", readerType);
                        return;
                    }
                    strxml = "<!DOCTYPE root[<!ENTITY e \"Test Entity\"> ]><root>&e;</root>";
                    break;
                case "SignificantWhitespace":
                    strxml = "<root xml:space=\"preserve\">			 </root>";
                    break;
                case "Whitespace":
                    if (ReaderStripsWhitespace())
                    {
                        CError.WriteLine("{0} strips whitespace nodes by default", readerType);
                        return;
                    }
                    strxml = "<root>			 </root>";
                    break;
            }

            XmlReader xr;
            xr = CreateReader(new StringReader(strxml));

            do
            { xr.Read(); }
            while ((xr.NodeType.ToString() != CurVariation.Param.ToString()) && (xr.ReadState != ReadState.EndOfFile));

            if (xr.ReadState == ReadState.EndOfFile || xr.NodeType.ToString() != CurVariation.Param.ToString())
            {
                xr.Dispose();
                CError.WriteLine("Reader not positioned on correct node");
                CError.WriteLine("ReadState: {0}", xr.ReadState);
                CError.WriteLine("NodeType: {0}", xr.NodeType);
                Assert.True(false);
            }

            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    if (CurVariation.Param.ToString() != "DocumentType")
                        w.WriteStartElement("root");
                    w.WriteAttributes(xr, false);
                }
                catch (XmlException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    CError.Compare(w.WriteState, (CurVariation.Param.ToString() == "DocumentType") ? WriteState.Start : WriteState.Element, "WriteState should be Element");
                    return;
                }
                finally
                {
                    xr.Dispose();
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        //[Variation(id = 17, Desc = "Call WriteAttribute with double quote char in the value", Pri = 1)]
        [Fact]
        public void writeAttributes_10()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "QuoteChar")
                        {
                            do
                            { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(CompareReader("<Root a=\"b&quot;c\" />"));
        }

        //[Variation(id = 18, Desc = "Call WriteAttribute with single quote char in the value", Pri = 1)]
        [Fact]
        public void writeAttributes_11()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "QuoteChar")
                        {
                            do
                            { xr.Read(); } while (xr.LocalName != "node");
                            do
                            { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(CompareReader("<Root a=\"b'c\" />"));
        }

        //[Variation(id = 19, Desc = "Call WriteAttributes with 100 attributes", Pri = 1)]
        [Fact]
        public void writeAttributes_12()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "OneHundredAttributes")
                        {
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }

                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        CError.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                        xr.Dispose();
                        Assert.True(false);
                    }
                    w.WriteStartElement("OneHundredAttributes");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(CompareBaseline("OneHundredAttributes.xml"));
        }

        //[Variation(id = 20, Desc = "WriteAttributes with different builtin entities in attribute value", Pri = 1)]
        [Fact]
        public void writeAttributes_13()
        {
            string strxml = "<E a=\"&gt;&lt;&quot;&apos;&amp;\" />";
            using (XmlReader xr = CreateReader(new StringReader(strxml)))
            {
                xr.Read();
                xr.MoveToFirstAttribute();

                if (xr.NodeType != XmlNodeType.Attribute)
                {
                    CError.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                    xr.Dispose();
                    Assert.True(false);
                }

                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(CompareReader("<Root a=\"&gt;&lt;&quot;&apos;&amp;\" />"));
        }

        //[Variation(id = 21, Desc = "WriteAttributes tries to duplicate attribute", Pri = 1)]
        [Fact]
        public void writeAttributes_14()
        {
            string strxml = "<root attr='test' />";
            XmlReader xr = CreateReader(new StringReader(strxml));
            xr.Read();
            xr.MoveToFirstAttribute();

            if (xr.NodeType != XmlNodeType.Attribute)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                xr.Dispose();
                Assert.True(false);
            }

            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
                catch (Exception e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
                finally
                {
                    xr.Dispose();
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }
    }
}
