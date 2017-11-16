// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using Xunit;

namespace System.Xml.Tests
{
    public class TCWriteAttributes : ReaderParamTestCase
    {
        // Call WriteAttributes with default DTD attributes = false
        [Theory]
        [XmlWriterInlineData]
        public void writeAttributes_2(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
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
                Assert.True(utils.CompareReader("<Root a=\"b\" FIRST=\"KEVIN\" LAST=\"WHITE\" />"));
            else
                Assert.True(utils.CompareReader("<Root a=\"b\" />"));
        }

        // Call WriteAttributes with XmlReader = null
        [Theory]
        [XmlWriterInlineData]
        public void writeAttributes_3(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
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

        // Call WriteAttributes when reader is located on element
        [Theory]
        [XmlWriterInlineData]
        public void writeAttributes_4(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
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
            Assert.True(utils.CompareReader("<Root a=\"b\" c=\"d\" e=\"f\" />"));
        }

        // Call WriteAttributes when reader is located in the middle attribute
        [Theory]
        [XmlWriterInlineData]
        public void writeAttributes_5(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
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
            Assert.True(utils.CompareReader("<Root c=\"d\" e=\"f\" />"));
        }

        // Call WriteAttributes when reader is located in the last attribute
        [Theory]
        [XmlWriterInlineData]
        public void writeAttributes_6(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
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
            Assert.True(utils.CompareReader("<Root e=\"f\" />"));
        }

        // Call WriteAttributes with reader on XmlDeclaration
        [Theory]
        [XmlWriterInlineData]
        public void writeAttributes_8(XmlWriterUtils utils)
        {
            if (IsXPathDataModelReader())
            {
                CError.WriteLine("{0} does not support XmlDecl node", readerType);
                return;
            }
            using (XmlWriter w = utils.CreateWriter())
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
            Assert.True(utils.CompareReader("<Root version=\"1.0\" standalone=\"yes\" />"));
        }

        [Theory]
        [XmlWriterInlineData("DocumentType")]
        [XmlWriterInlineData("CDATA")]
        [XmlWriterInlineData("Text")]
        [XmlWriterInlineData("ProcessingInstruction")]
        [XmlWriterInlineData("Comment")]
        [XmlWriterInlineData("EntityReference")]
        [XmlWriterInlineData("Whitespace")]
        [XmlWriterInlineData("SignificantWhitespace")]
        public void writeAttributes_9(XmlWriterUtils utils, string tokenType)
        {
            string strxml = "";
            switch (tokenType)
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
            while ((xr.NodeType.ToString() != tokenType) && (xr.ReadState != ReadState.EndOfFile));

            if (xr.ReadState == ReadState.EndOfFile || xr.NodeType.ToString() != tokenType)
            {
                xr.Dispose();
                CError.WriteLine("Reader not positioned on correct node");
                CError.WriteLine("ReadState: {0}", xr.ReadState);
                CError.WriteLine("NodeType: {0}", xr.NodeType);
                Assert.True(false);
            }

            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    if (tokenType != "DocumentType")
                        w.WriteStartElement("root");
                    w.WriteAttributes(xr, false);
                }
                catch (XmlException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    CError.Compare(w.WriteState, (tokenType == "DocumentType") ? WriteState.Start : WriteState.Element, "WriteState should be Element");
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

        // Call WriteAttribute with double quote char in the value
        [Theory]
        [XmlWriterInlineData]
        public void writeAttributes_10(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
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
            Assert.True(utils.CompareReader("<Root a=\"b&quot;c\" />"));
        }

        // Call WriteAttribute with single quote char in the value
        [Theory]
        [XmlWriterInlineData]
        public void writeAttributes_11(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
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
            Assert.True(utils.CompareReader("<Root a=\"b'c\" />"));
        }

        // Call WriteAttributes with 100 attributes
        [Theory]
        [XmlWriterInlineData]
        public void writeAttributes_12(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
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
            Assert.True(utils.CompareBaseline("OneHundredAttributes.xml"));
        }

        // WriteAttributes with different builtin entities in attribute value
        [Theory]
        [XmlWriterInlineData]
        public void writeAttributes_13(XmlWriterUtils utils)
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

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            Assert.True(utils.CompareReader("<Root a=\"&gt;&lt;&quot;&apos;&amp;\" />"));
        }

        // WriteAttributes tries to duplicate attribute
        [Theory]
        [XmlWriterInlineData]
        public void writeAttributes_14(XmlWriterUtils utils)
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

            using (XmlWriter w = utils.CreateWriter())
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
