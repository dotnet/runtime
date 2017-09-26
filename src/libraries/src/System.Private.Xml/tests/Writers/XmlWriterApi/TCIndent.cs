// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Text;
using Xunit;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public class TCIndent
    {
        [Theory]
        [XmlWriterInlineData(WriterType.AllButIndenting & WriterType.AllButCustom)]
        public void indent_1(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            CError.Compare(w.Settings.Indent, false, "Mismatch in Indent");
            w.WriteStartElement("Root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            Assert.True(utils.CompareString("<Root><child /></Root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_2(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            Assert.True(utils.CompareString("<Root>" + wSettings.NewLineChars + "  <child />" + wSettings.NewLineChars + "</Root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_3(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(utils.CompareString("<Root></Root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void indent_4(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.Indent, true, "Mismatch in Indent");
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(utils.CompareString("<Root></Root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_5(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteFullEndElement();
            w.Dispose();
            Assert.True(utils.CompareString("<Root></Root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_6(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteFullEndElement();
            w.Dispose();
            Assert.True(utils.CompareString("<Root></Root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_7(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteString("test");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            Assert.True(utils.CompareString("<Root>test<child /></Root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_8(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteString("test");
            w.WriteStartElement("child");
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.Dispose();
            Assert.True(utils.CompareString("<Root>test<child></child></Root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_9(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteProcessingInstruction("xml", "version=\"1.0\"");
            w.WriteDocType("root", null, null, "foo");
            w.WriteStartElement("root");
            w.WriteProcessingInstruction("pi", "pi");
            w.WriteComment("comment");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(utils.CompareString("<?xml version=\"1.0\"?>" + wSettings.NewLineChars + "<!DOCTYPE root [foo]>" + wSettings.NewLineChars + "<root>" + wSettings.NewLineChars + "  <?pi pi?>" + wSettings.NewLineChars + "  <!--comment-->" + wSettings.NewLineChars + "  <foo />" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        // Mixed content after child
        [Theory]
        [XmlWriterInlineData]
        public void indent_10(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("master");
            w.WriteStartElement("root");
            w.WriteStartElement("foo");
            w.WriteString("text");
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteString("text");
            w.WriteStartElement("foo");
            w.WriteElementString("bar", "text2");
            w.Dispose();
            CError.Compare(utils.CompareString("<master>" + wSettings.NewLineChars + "  <root>" + wSettings.NewLineChars + "    <foo>text</foo>" + wSettings.NewLineChars + "  </root>text<foo><bar>text2</bar></foo></master>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_11(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteCData("text");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(utils.CompareString("<root><![CDATA[text]]><foo /></root>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_12(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteWhitespace("  ");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(utils.CompareString("<root>  <foo /></root>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_13(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteRaw("text");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(utils.CompareString("<root>text<foo /></root>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_14(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteEntityRef("e");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(utils.CompareString("<root>&e;<foo /></root>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_15(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("e1");
            w.WriteStartElement("e2");
            w.WriteStartElement("e3");
            w.WriteStartElement("e4");
            w.WriteEndDocument();
            w.Dispose();
            CError.Compare(utils.CompareString("<e1>" + wSettings.NewLineChars + "  <e2>" + wSettings.NewLineChars + "    <e3>" + wSettings.NewLineChars + "      <e4 />" + wSettings.NewLineChars + "    </e3>" + wSettings.NewLineChars + "  </e2>" + wSettings.NewLineChars + "</e1>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_16(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("e1");
            w.WriteStartElement("e2");
            w.WriteStartElement("e3");
            w.WriteStartElement("e4");
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(utils.CompareString("<e1>" + wSettings.NewLineChars + "  <e2>" + wSettings.NewLineChars + "    <e3>" + wSettings.NewLineChars + "      <e4 />" + wSettings.NewLineChars + "    </e3>" + wSettings.NewLineChars + "  </e2>" + wSettings.NewLineChars + "</e1>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_17(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("e1");
            w.WriteStartElement("e2");
            w.WriteStartElement("e3");
            w.WriteStartElement("e4");
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.Dispose();
            CError.Compare(utils.CompareString("<e1>" + wSettings.NewLineChars + "  <e2>" + wSettings.NewLineChars + "    <e3>" + wSettings.NewLineChars + "      <e4></e4>" + wSettings.NewLineChars + "    </e3>" + wSettings.NewLineChars + "  </e2>" + wSettings.NewLineChars + "</e1>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_18(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteElementString("root", "");
            w.WriteComment("c");
            w.WriteProcessingInstruction("pi", "pi");
            w.WriteWhitespace("  ");
            w.WriteComment("c");
            w.WriteProcessingInstruction("pi", "pi");
            w.Dispose();
            CError.Compare(utils.CompareString("<root />" + wSettings.NewLineChars + "<!--c-->" + wSettings.NewLineChars + "<?pi pi?>  <!--c--><?pi pi?>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_19(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("a1", "value");
            w.WriteStartElement("foo");
            w.WriteAttributeString("a2", "value");
            w.WriteEndDocument();
            w.Dispose();
            CError.Compare(utils.CompareString("<root a1=\"value\">" + wSettings.NewLineChars + "  <foo a2=\"value\" />" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_20(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartDocument();
            w.WriteProcessingInstruction("pi", "value");
            w.WriteStartElement("root");
            w.Dispose();
            CError.Compare(utils.CompareString("<?pi value?>" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_21(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartDocument();
            w.WriteComment("value");
            w.WriteStartElement("root");
            w.Dispose();
            CError.Compare(utils.CompareString("<!--value-->" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData(ConformanceLevel.Document)]
        [XmlWriterInlineData(ConformanceLevel.Auto)]
        public void indent_22(XmlWriterUtils utils, ConformanceLevel conformanceLevel)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = conformanceLevel;

            using (XmlWriter w = utils.CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteString("text");
                w.WriteStartElement("child");
                w.WriteProcessingInstruction("pi", "value");
            }
            CError.Compare(utils.CompareString("<root>text<child><?pi value?></child></root>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData(ConformanceLevel.Document)]
        [XmlWriterInlineData(ConformanceLevel.Auto)]
        public void indent_24(XmlWriterUtils utils, ConformanceLevel conformanceLevel)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = conformanceLevel;

            using (XmlWriter w = utils.CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteString("text");
                w.WriteStartElement("child");
                w.WriteComment("value");
            }
            CError.Compare(utils.CompareString("<root>text<child><!--value--></child></root>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData(ConformanceLevel.Document)]
        [XmlWriterInlineData(ConformanceLevel.Auto)]
        public void indent_26(XmlWriterUtils utils, ConformanceLevel conformanceLevel)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = conformanceLevel;

            using (XmlWriter w = utils.CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteStartElement("child");
                w.WriteStartElement("a");
                w.WriteEndElement();
                w.WriteString("text");
                w.WriteStartElement("a");
            }
            CError.Compare(utils.CompareString("<root>" + wSettings.NewLineChars + "  <child>" + wSettings.NewLineChars + "    <a />text<a /></child>" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void indent_28(XmlWriterUtils utils)
        {
            // The output should be the same for ConformanceLevel.Document/Auto
            //   and with WriteStartDocument called or not
            int i;
            for (i = 0; i < 4; i++)
            {
                XmlWriterSettings wSettings = new XmlWriterSettings();
                wSettings.OmitXmlDeclaration = true;
                wSettings.Indent = true;
                wSettings.ConformanceLevel = (i % 2) == 0 ? ConformanceLevel.Auto : ConformanceLevel.Document;
                CError.WriteLine("ConformanceLevel: {0}", wSettings.ConformanceLevel.ToString());

                using (XmlWriter w = utils.CreateWriter(wSettings))
                {
                    if (i > 1)
                    {
                        CError.WriteLine("WriteStartDocument called.");
                        w.WriteStartDocument();
                    }
                    else
                    {
                        CError.WriteLine("WriteStartDocument not called.");
                    }
                    w.WriteStartElement("root");
                }
                CError.Compare(utils.CompareString("<root />"), "");
            }
            return;
        }

        // First element - with decl
        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void indent_29(XmlWriterUtils utils)
        {
            // The output should be the same for ConformanceLevel.Document/Auto
            int i;
            for (i = 0; i < 2; i++)
            {
                XmlWriterSettings wSettings = new XmlWriterSettings();
                wSettings.OmitXmlDeclaration = false;
                wSettings.Indent = true;
                wSettings.ConformanceLevel = (i % 2) == 0 ? ConformanceLevel.Auto : ConformanceLevel.Document;
                CError.WriteLine("ConformanceLevel: {0}", wSettings.ConformanceLevel.ToString());

                XmlWriter w = utils.CreateWriter(wSettings);
                Encoding encoding = w.Settings.Encoding;
                if (wSettings.ConformanceLevel == ConformanceLevel.Auto)
                {
                    // Write the decl as PI - since WriteStartDocument would switch to Document mode
                    w.WriteProcessingInstruction("xml", string.Format("version=\"1.0\" encoding=\"{0}\"", encoding.WebName));
                }
                else
                {
                    w.WriteStartDocument();
                }
                w.WriteStartElement("root");
                w.Dispose();
                string expectedResult = string.Format("<?xml version=\"1.0\" encoding=\"{0}\"?>" + wSettings.NewLineChars + "<root />", encoding.WebName);
                CError.Compare(utils.CompareString(expectedResult), "");
            }
        }

        [Theory]
        [XmlWriterInlineData(ConformanceLevel.Document)]
        [XmlWriterInlineData(ConformanceLevel.Auto)]
        [XmlWriterInlineData(ConformanceLevel.Fragment)]
        public void indent_30(XmlWriterUtils utils, ConformanceLevel conformanceLevel)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = conformanceLevel;

            using (XmlWriter w = utils.CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteStartElement("e1");
                w.WriteStartElement("e2");
                w.WriteEndElement();
                w.WriteString("text");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            CError.Compare(utils.CompareString("<root>" + wSettings.NewLineChars + "  <e1>" + wSettings.NewLineChars + "    <e2 />text</e1>" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData(ConformanceLevel.Document)]
        [XmlWriterInlineData(ConformanceLevel.Auto)]
        public void indent_33(XmlWriterUtils utils, ConformanceLevel conformanceLevel)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Indent = true;
            wSettings.ConformanceLevel = conformanceLevel;

            using (XmlWriter w = utils.CreateWriter(wSettings))
            {
                w.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\"");
                w.WriteProcessingInstruction("piname1", "pitext1");
                w.WriteProcessingInstruction("piname2", "pitext2");
                w.WriteStartElement("root");
                w.WriteEndElement();
            }
            CError.Compare(utils.CompareString("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + wSettings.NewLineChars + "<?piname1 pitext1?>" + wSettings.NewLineChars + "<?piname2 pitext2?>" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData(ConformanceLevel.Document)]
        [XmlWriterInlineData(ConformanceLevel.Auto)]
        public void indent_36(XmlWriterUtils utils, ConformanceLevel conformanceLevel)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Indent = true;
            wSettings.ConformanceLevel = conformanceLevel;

            using (XmlWriter w = utils.CreateWriter(wSettings))
            {
                w.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\"");
                w.WriteDocType("name", "publicid", "systemid", "subset");
                w.WriteProcessingInstruction("piname1", "pitext1");
                w.WriteProcessingInstruction("piname2", "pitext2");
                w.WriteStartElement("root");
            }
            CError.Compare(utils.CompareString("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + wSettings.NewLineChars + "<!DOCTYPE name PUBLIC \"publicid\" \"systemid\"[subset]>" + wSettings.NewLineChars + "<?piname1 pitext1?>" + wSettings.NewLineChars + "<?piname2 pitext2?>" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        [Theory]
        [XmlWriterInlineData(ConformanceLevel.Document)]
        [XmlWriterInlineData(ConformanceLevel.Auto)]
        [XmlWriterInlineData(ConformanceLevel.Fragment)]
        public void indent_39(XmlWriterUtils utils, ConformanceLevel conformanceLevel)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = conformanceLevel;

            using (XmlWriter w = utils.CreateWriter(wSettings))
            {
                w.WriteProcessingInstruction("piname1", "pitext1");
                w.WriteComment("comment1");
                w.WriteProcessingInstruction("piname2", "pitext2");
                w.WriteStartElement("root");
                w.WriteStartElement("e1");
                w.WriteStartElement("e2");
                w.WriteStartElement("e3");
                w.WriteStartElement("e4");
                w.WriteEndElement();
                w.WriteString("text1");
                w.WriteProcessingInstruction("piname3", "pitext3");
                w.WriteEndElement();
                w.WriteComment("comment2");
                w.WriteCData("cdata1");
                w.WriteString("text2");
                w.WriteProcessingInstruction("piname4", "pitext4");
                w.WriteCData("cdata2");
                w.WriteComment("comment3");
                w.WriteProcessingInstruction("piname5", "pitext5");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            CError.Compare(utils.CompareString("<?piname1 pitext1?>" + wSettings.NewLineChars + "<!--comment1-->" + wSettings.NewLineChars + "<?piname2 pitext2?>" + wSettings.NewLineChars + "<root>" + wSettings.NewLineChars + "  <e1>" + wSettings.NewLineChars + "    <e2>" + wSettings.NewLineChars + "      <e3>" + wSettings.NewLineChars + "        <e4 />text1<?piname3 pitext3?></e3>" + wSettings.NewLineChars + "      <!--comment2--><![CDATA[cdata1]]>text2<?piname4 pitext4?><![CDATA[cdata2]]><!--comment3--><?piname5 pitext5?></e2>" + wSettings.NewLineChars + "  </e1>" + wSettings.NewLineChars + "</root>"), "");
            return;
        }
    }
}
