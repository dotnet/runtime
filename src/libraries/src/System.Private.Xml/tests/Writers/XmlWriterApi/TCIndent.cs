// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Text;
using Xunit;

namespace System.Xml.Tests
{
    public class TCIndent
    {
        //[Variation(id=1, Desc="Simple test when false", Pri=0)]
        [Fact]
        public void indent_1()
        {
            if (IsIndent()) return;
            XmlWriter w = CreateWriter();
            CError.Compare(w.Settings.Indent, false, "Mismatch in Indent");
            w.WriteStartElement("Root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root><child /></Root>"));
        }

        //[Variation(id=2, Desc="Simple test when true", Pri=0)]
        [Fact]
        public void indent_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            return CompareString("<Root>" + wSettings.NewLineChars + "  <child />" + wSettings.NewLineChars + "</Root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=3, Desc="Indent = false, element content is empty", Pri=0)]
        [Fact]
        public void indent_3()
        {
            XmlWriter w = CreateWriter();
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root></Root>"));
        }

        //[Variation(id=4, Desc="Indent = true, element content is empty", Pri=0)]
        [Fact]
        public void indent_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.Indent, true, "Mismatch in Indent");
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root></Root>"));
        }

        //[Variation(id=5, Desc="Indent = false, element content is empty, FullEndElement", Pri=0)]
        [Fact]
        public void indent_5()
        {
            XmlWriter w = CreateWriter();
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteFullEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root></Root>"));
        }

        //[Variation(id=6, Desc="Indent = true, element content is empty, FullEndElement", Pri=0)]
        [Fact]
        public void indent_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteString("");
            w.WriteFullEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root></Root>"));
        }

        //[Variation(id=7, Desc="Indent = true, mixed content", Pri=0)]
        [Fact]
        public void indent_7()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteString("test");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root>test<child /></Root>"));
        }

        //[Variation(id=8, Desc="Indent = true, mixed content, FullEndElement", Pri=0)]
        [Fact]
        public void indent_8()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("Root");
            w.WriteString("test");
            w.WriteStartElement("child");
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.Dispose();
            Assert.True(CompareString("<Root>test<child></child></Root>"));
        }

        //[Variation(id = 9, Desc = "Other types of non-text nodes", Priority = 0)]
        [Fact]
        public void indent_9()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteProcessingInstruction("xml", "version=\"1.0\"");
            w.WriteDocType("root", null, null, "foo");
            w.WriteStartElement("root");
            w.WriteProcessingInstruction("pi", "pi");
            w.WriteComment("comment");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<?xml version=\"1.0\"?>" + wSettings.NewLineChars + "<!DOCTYPE root [foo]>" + wSettings.NewLineChars + "<root>" + wSettings.NewLineChars + "  <?pi pi?>" + wSettings.NewLineChars + "  <!--comment-->" + wSettings.NewLineChars + "  <foo />" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        //[Variation(id = 10, Desc = "Mixed content after child", Priority = 0)]
        [Fact]
        public void indent_10()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
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
            CError.Compare(CompareString("<master>" + wSettings.NewLineChars + "  <root>" + wSettings.NewLineChars + "    <foo>text</foo>" + wSettings.NewLineChars + "  </root>text<foo><bar>text2</bar></foo></master>"), "");
            return;
        }

        //[Variation(id = 11, Desc = "Mixed content - CData", Priority = 0)]
        [Fact]
        public void indent_11()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteCData("text");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<root><![CDATA[text]]><foo /></root>"), "");
            return;
        }

        //[Variation(id = 12, Desc = "Mixed content - Whitespace", Priority = 0)]
        [Fact]
        public void indent_12()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteWhitespace("  ");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<root>  <foo /></root>"), "");
            return;
        }

        //[Variation(id = 13, Desc = "Mixed content - Raw", Priority = 0)]
        [Fact]
        public void indent_13()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteRaw("text");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<root>text<foo /></root>"), "");
            return;
        }

        //[Variation(id = 14, Desc = "Mixed content - EntityRef", Priority = 0)]
        [Fact]
        public void indent_14()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteEntityRef("e");
            w.WriteElementString("foo", "");
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<root>&e;<foo /></root>"), "");
            return;
        }

        //[Variation(id = 15, Desc = "Nested Elements - with EndDocument", Priority = 0)]
        [Fact]
        public void indent_15()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("e1");
            w.WriteStartElement("e2");
            w.WriteStartElement("e3");
            w.WriteStartElement("e4");
            w.WriteEndDocument();
            w.Dispose();
            CError.Compare(CompareString("<e1>" + wSettings.NewLineChars + "  <e2>" + wSettings.NewLineChars + "    <e3>" + wSettings.NewLineChars + "      <e4 />" + wSettings.NewLineChars + "    </e3>" + wSettings.NewLineChars + "  </e2>" + wSettings.NewLineChars + "</e1>"), "");
            return;
        }

        //[Variation(id = 16, Desc = "Nested Elements - with EndElement", Priority = 0)]
        [Fact]
        public void indent_16()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("e1");
            w.WriteStartElement("e2");
            w.WriteStartElement("e3");
            w.WriteStartElement("e4");
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();
            CError.Compare(CompareString("<e1>" + wSettings.NewLineChars + "  <e2>" + wSettings.NewLineChars + "    <e3>" + wSettings.NewLineChars + "      <e4 />" + wSettings.NewLineChars + "    </e3>" + wSettings.NewLineChars + "  </e2>" + wSettings.NewLineChars + "</e1>"), "");
            return;
        }

        //[Variation(id = 17, Desc = "Nested Elements - with FullEndElement", Priority = 0)]
        [Fact]
        public void indent_17()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("e1");
            w.WriteStartElement("e2");
            w.WriteStartElement("e3");
            w.WriteStartElement("e4");
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.WriteFullEndElement();
            w.Dispose();
            CError.Compare(CompareString("<e1>" + wSettings.NewLineChars + "  <e2>" + wSettings.NewLineChars + "    <e3>" + wSettings.NewLineChars + "      <e4></e4>" + wSettings.NewLineChars + "    </e3>" + wSettings.NewLineChars + "  </e2>" + wSettings.NewLineChars + "</e1>"), "");
            return;
        }

        //[Variation(id = 18, Desc = "NewLines after root element", Priority = 0)]
        [Fact]
        public void indent_18()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteElementString("root", "");
            w.WriteComment("c");
            w.WriteProcessingInstruction("pi", "pi");
            w.WriteWhitespace("  ");
            w.WriteComment("c");
            w.WriteProcessingInstruction("pi", "pi");
            w.Dispose();
            CError.Compare(CompareString("<root />" + wSettings.NewLineChars + "<!--c-->" + wSettings.NewLineChars + "<?pi pi?>  <!--c--><?pi pi?>"), "");
            return;
        }

        //[Variation(id = 19, Desc = "Elements with attributes", Priority = 0)]
        [Fact]
        public void indent_19()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("a1", "value");
            w.WriteStartElement("foo");
            w.WriteAttributeString("a2", "value");
            w.WriteEndDocument();
            w.Dispose();
            CError.Compare(CompareString("<root a1=\"value\">" + wSettings.NewLineChars + "  <foo a2=\"value\" />" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        //[Variation(id = 20, Desc = "First PI with start document no xmldecl", Priority = 1)]
        [Fact]
        public void indent_20()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartDocument();
            w.WriteProcessingInstruction("pi", "value");
            w.WriteStartElement("root");
            w.Dispose();
            CError.Compare(CompareString("<?pi value?>" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        //[Variation(id = 21, Desc = "First comment with start document no xmldecl", Priority = 1)]
        [Fact]
        public void indent_21()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartDocument();
            w.WriteComment("value");
            w.WriteStartElement("root");
            w.Dispose();
            CError.Compare(CompareString("<!--value-->" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        //[Variation(id = 22, Desc = "PI in mixed content - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 23, Desc = "PI in mixed content - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        [Fact]
        public void indent_22()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteString("text");
                w.WriteStartElement("child");
                w.WriteProcessingInstruction("pi", "value");
            }
            CError.Compare(CompareString("<root>text<child><?pi value?></child></root>"), "");
            return;
        }

        //[Variation(id = 24, Desc = "Comment in mixed content - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 25, Desc = "Comment in mixed content - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        [Fact]
        public void indent_24()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteString("text");
                w.WriteStartElement("child");
                w.WriteComment("value");
            }
            CError.Compare(CompareString("<root>text<child><!--value--></child></root>"), "");
            return;
        }

        //[Variation(id = 26, Desc = "Mixed content after end element - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 27, Desc = "Mixed content after end element - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        [Fact]
        public void indent_26()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteStartElement("child");
                w.WriteStartElement("a");
                w.WriteEndElement();
                w.WriteString("text");
                w.WriteStartElement("a");
            }
            CError.Compare(CompareString("<root>" + wSettings.NewLineChars + "  <child>" + wSettings.NewLineChars + "    <a />text<a /></child>" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        //[Variation(id = 28, Desc = "First element - no decl", Priority = 1)]
        [Fact]
        public void indent_28()
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

                using (XmlWriter w = CreateWriter(wSettings))
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
                CError.Compare(CompareString("<root />"), "");
            }
            return;
        }

        //[Variation(id = 29, Desc = "First element - with decl", Priority = 1, Param = true)]
        [Fact]
        public void indent_29()
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

                XmlWriter w = CreateWriter(wSettings);
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
                CError.Compare(CompareString(expectedResult), "");
            }
            return;
        }

        //[Variation(id = 30, Desc = "Bad indentation of elements with mixed content data - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 31, Desc = "Bad indentation of elements with mixed content data - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        //[Variation(id = 32, Desc = "Bad indentation of elements with mixed content data - Fragment", Priority = 1, Param = ConformanceLevel.Fragment)]
        [Fact]
        public void indent_30()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteStartElement("root");
                w.WriteStartElement("e1");
                w.WriteStartElement("e2");
                w.WriteEndElement();
                w.WriteString("text");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            CError.Compare(CompareString("<root>" + wSettings.NewLineChars + "  <e1>" + wSettings.NewLineChars + "    <e2 />text</e1>" + wSettings.NewLineChars + "</root>"), "");
            return;
        }

        //[Variation(id = 33, Desc = "Indentation error - no new line after PI only if document contains no DocType node - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 34, Desc = "Indentation error - no new line after PI only if document contains no DocType node - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        [Fact]
        public void indent_33()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\"");
                w.WriteProcessingInstruction("piname1", "pitext1");
                w.WriteProcessingInstruction("piname2", "pitext2");
                w.WriteStartElement("root");
                w.WriteEndElement();
            }
            CError.Compare(CompareString("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + wSettings.NewLineChars + "<?piname1 pitext1?>" + wSettings.NewLineChars + "<?piname2 pitext2?>" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        //[Variation(id = 36, Desc = "Indentation error - no new line after PI only if document contains DocType node - Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 37, Desc = "Indentation error - no new line after PI only if document contains DocType node - Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        [Fact]
        public void indent_36()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
            {
                w.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\"");
                w.WriteDocType("name", "publicid", "systemid", "subset");
                w.WriteProcessingInstruction("piname1", "pitext1");
                w.WriteProcessingInstruction("piname2", "pitext2");
                w.WriteStartElement("root");
            }
            CError.Compare(CompareString("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + wSettings.NewLineChars + "<!DOCTYPE name PUBLIC \"publicid\" \"systemid\"[subset]>" + wSettings.NewLineChars + "<?piname1 pitext1?>" + wSettings.NewLineChars + "<?piname2 pitext2?>" + wSettings.NewLineChars + "<root />"), "");
            return;
        }

        //[Variation(id = 39, Desc = "Document", Priority = 1, Param = ConformanceLevel.Document)]
        //[Variation(id = 40, Desc = "Auto", Priority = 1, Param = ConformanceLevel.Auto)]
        //[Variation(id = 41, Desc = "Fragment", Priority = 1, Param = ConformanceLevel.Fragment)]
        [Fact]
        public void indent_39()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.ConformanceLevel = (ConformanceLevel)this.CurVariation.Param;

            using (XmlWriter w = CreateWriter(wSettings))
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
            CError.Compare(CompareString("<?piname1 pitext1?>" + wSettings.NewLineChars + "<!--comment1-->" + wSettings.NewLineChars + "<?piname2 pitext2?>" + wSettings.NewLineChars + "<root>" + wSettings.NewLineChars + "  <e1>" + wSettings.NewLineChars + "    <e2>" + wSettings.NewLineChars + "      <e3>" + wSettings.NewLineChars + "        <e4 />text1<?piname3 pitext3?></e3>" + wSettings.NewLineChars + "      <!--comment2--><![CDATA[cdata1]]>text2<?piname4 pitext4?><![CDATA[cdata2]]><!--comment3--><?piname5 pitext5?></e2>" + wSettings.NewLineChars + "  </e1>" + wSettings.NewLineChars + "</root>"), "");
            return;
        }
    }
}
