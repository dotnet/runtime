// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CoreXml.Test.XLinq;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public class DeepEqualsTests
    {
        // equals => hashcode should be the same

        //  - all "simple" node types
        //  - text vs. CDATA
        //  XDocument:
        //  - Normal mode
        //  - Concatenated text (Whitespace) nodes
        //  - Diffs in XDecl

        //  XElement:
        //  - Normal mode
        //      - same nodes, different order
        //          - comments inside the texts
        //      - same nodes, same order (positive)
        //
        //  - Concatenated text nodes
        //      - string content vs. text node/s
        //          - empty string vs. empty text node
        //      - Multiple text nodes but the same value
        //      - adjacent text & CData
        //  
        //  - IsEmpty
        //  - Attribute order
        //  - Namespace declarations 
        //      - local vs. in-scope
        //      - default redef.

        [InlineData("PI", "click", "PI", "click", true)]        // normal
        [InlineData("PI", "PI", "PI", "PI", true)]              // target=data
        [InlineData("PI", "", "PI", "", true)]                  // data = ''
        [InlineData("PI", "click", "PI", "", false)]            // data1!=data2
        [InlineData("AAAAP", "click", "AAAAQ", "click", false)] // target1!=target2
        [InlineData("AAAAP", "AAAAQ", "AAAAP", "AAAAQ", true)]  // hashconflict I
        [InlineData("PA", "PA", "PI", "PI", false)]             // data=target
        [InlineData("AAAAP", "AAAAQ", "AAAAQ", "AAAAP", false)] // hashconflict II
        [Theory]
        public void ProcessingInstruction(string target1, string data1, string target2, string data2, bool checkHashCode)
        {
            var p1 = new XProcessingInstruction(target1, data1);
            var p2 = new XProcessingInstruction(target2, data2);

            VerifyComparison(checkHashCode, p1, p2);

            XDocument doc = new XDocument(p1);
            XElement e2 = new XElement("p2p", p2);

            VerifyComparison(checkHashCode, p1, p2);
        }

        [InlineData("AAAAP", "AAAAQ", false)] // not equals, hashconflict
        [InlineData("AAAAP", "AAAAP", true)]  // equals
        [InlineData("  ", " ", false)]        // Whitespace (negative)
        [InlineData(" ", " ", true)]          // Whitespace
        [InlineData("", "", true)]            // Empty
        [Theory]
        public void Comment(string value1, string value2, bool checkHashCode)
        {
            XComment c1 = new XComment(value1);
            XComment c2 = new XComment(value2);
            VerifyComparison(checkHashCode, c1, c2);

            XDocument doc = new XDocument(c1);
            XElement e2 = new XElement("p2p", c2);

            VerifyComparison(checkHashCode, c1, c2);
        }

        [InlineData(new[] { "root", "a", "b", "c" }, new[] { "root", "a", "b", "c" }, true)]           // all field
        [InlineData(new[] { "root", null, null, null }, new[] { "root", null, null, null }, true)]     // all nulls
        [InlineData(new[] { "root", null, null, "data" }, new[] { "root", null, null, "data" }, true)] // internal subset only
        [InlineData(new[] { "A", "", "", "" }, new[] { "B", "", "", "" }, false)]                      // (negative) : name diff
        [InlineData(new[] { "A", null, null, "aa" }, new[] { "A", null, null, "bb" }, false)]          // (negative) : subset diff
        [InlineData(new[] { "A", "", "", "" }, new[] { "A", null, null, null }, false)]                // (negative) : null vs. empty
        [Theory]
        public void DocumentType(string[] docType1, string[] docType2, bool checkHashCode)
        {
            var dtd1 = new XDocumentType(docType1[0], docType1[1], docType1[2], docType1[3]);
            var dtd2 = new XDocumentType(docType2[0], docType2[1], docType2[2], docType2[3]);
            VerifyComparison(checkHashCode, dtd1, dtd2);
        }

        [InlineData("same", "different", false)] // different
        [InlineData("same", "same", true)]       // same
        [InlineData("", "", true)]               // Empty
        [InlineData(" ", " ", true)]             // Whitespace
        [InlineData("\n", " ", false)]           // Whitespace (negative)
        [Theory]
        public void Text(string value1, string value2, bool checkHashCode)
        {
            XText t1 = new XText(value1);
            XText t2 = new XText(value2);
            VerifyComparison(checkHashCode, t1, t2);

            XElement e2 = new XElement("p2p", t2);
            e2.Add(t1);
            VerifyComparison(checkHashCode, t1, t2);
        }

        [InlineData("same", "different", false)] // different
        [InlineData("same", "same", true)]       // same
        [InlineData("", "", true)]               // Empty
        [InlineData(" ", " ", true)]             // Whitespace
        [InlineData("\n", " ", false)]           // Whitespace (negative)
        [Theory]
        public void CData(string value1, string value2, bool checkHashCode)
        {
            XCData t1 = new XCData(value1);
            XCData t2 = new XCData(value2);
            VerifyComparison(checkHashCode, t1, t2);

            XElement e2 = new XElement("p2p", t2);
            e2.Add(t1);
            VerifyComparison(checkHashCode, t1, t2);
        }

        [InlineData("same", "same", false)]
        [InlineData("", "", false)]
        [InlineData(" ", " ", false)]
        [Theory]
        public void TextVsCData(string value1, string value2, bool checkHashCode)
        {
            XText t1 = new XCData(value1);
            XText t2 = new XText(value2);
            VerifyComparison(checkHashCode, t1, t2);

            XElement e2 = new XElement("p2p", t2);
            e2.Add(t1);
            VerifyComparison(checkHashCode, t1, t2);
        }

        // do not concatenate inside
        [Fact]
        public void TextWholeVsConcatenate()
        {
            XElement e = new XElement("A", new XText("_start_"), new XText("_end_"));
            XNode[] pieces = new XNode[] { new XText("_start_"), new XText("_end_") };
            XText together = new XText("_start__end_");

            VerifyComparison(true, e.FirstNode, pieces[0]);
            VerifyComparison(true, e.LastNode, pieces[1]);
            VerifyComparison(false, e.FirstNode, together);
            VerifyComparison(false, e.LastNode, together);
        }

        [InlineData("<A/>", "<A></A>", false)]                                            // smoke
        [InlineData("<A/>", "<A Id='a'/>", false)]                                        // attribute missing
        [InlineData("<A Id='a'/>", "<A Id='a'/>", true)]                                  // attributes
        [InlineData("<A at='1' Id='a'/>", "<A Id='a' at='1'/>", false)]                   // attributes (same, different order)
        [InlineData("<A at='1' Id='a'/>", "<A at='1' Id='a'/>", true)]                    // attributes (same, same order)
        [InlineData("<A at='1' Id='a'/>", "<A at='1' Id='ab'/>", false)]                  // attributes (same, same order, different value)
        [InlineData("<A p:at='1' xmlns:p='nsp'/>", "<A p:at='1' xmlns:p='nsp'/>", true)]  // attributes (same, same order, namespace decl)
        [InlineData("<A p:at='1' xmlns:p='nsp'/>", "<A q:at='1' xmlns:q='nsp'/>", false)] // attributes (same, same order, namespace decl, different prefix)
        [InlineData("<A>text</A>", "<A>text</A>", true)]                                  // String content
        [InlineData("<A>text<?PI click?></A>", "<A><?PI click?>text</A>", false)]         // String + PI content (negative)
        [InlineData("<A>text<?PI click?></A>", "<A>text<?PI click?></A>", true)]          // String + PI content
        [Theory]
        public void Element(string text1, string text2, bool checkHashCode)
        {
            XElement e1 = XElement.Parse(text1);
            XElement e2 = XElement.Parse(text2);
            VerifyComparison(checkHashCode, e1, e2);
            // Should always be the same ...
            VerifyComparison(true, e1, e1);
            VerifyComparison(true, e2, e2);
        }

        // String content vs. text node vs. CData
        [Fact]
        public void Element2()
        {
            XElement e1 = new XElement("A", "string_content");
            XElement e2 = new XElement("A", new XText("string_content"));
            XElement e3 = new XElement("A", new XCData("string_content"));

            VerifyComparison(true, e1, e2);
            VerifyComparison(false, e1, e3);
            VerifyComparison(false, e2, e3);
        }

        // XElement - text node concatenations
        [Fact]
        public void Element3()
        {
            XElement e1 = new XElement("A", "string_content");
            XElement e2 = new XElement("A", new XText("string"), new XText("_content"));
            XElement e3 = new XElement("A", new XText("string"), new XComment("comm"), new XText("_content"));
            XElement e4 = new XElement("A", new XText("string"), new XCData("_content"));

            VerifyComparison(true, e1, e2);
            VerifyComparison(false, e1, e3);
            VerifyComparison(false, e2, e3);

            VerifyComparison(false, e1, e4);
            VerifyComparison(false, e2, e4);
            VerifyComparison(false, e3, e4);

            e3.Nodes().First(n => n is XComment).Remove();

            VerifyComparison(true, e1, e3);
            VerifyComparison(true, e2, e3);
        }

        [InlineData(1)] // XElement - text node incarnation - by touching
        [InlineData(2)] // XElement - text node incarnation - by adding new node
        [Theory]
        public void Element6(int param)
        {
            XElement e1 = new XElement("A", "datata");
            XElement e2 = new XElement("A", "datata");
            switch (param)
            {
                case 1:
                    XComment c = new XComment("hele");
                    e2.Add(c);
                    c.Remove();
                    break;
                case 2:
                    break;
            }

            VerifyComparison(true, e1, e2);
        }

        // XElement - text node concatenations (negative)
        [Fact]
        public void Element4()
        {
            XElement e1 = new XElement("A", new XCData("string_content"));
            XElement e2 = new XElement("A", new XCData("string"), new XCData("_content"));
            XElement e3 = new XElement("A", new XCData("string"), "_content");
            VerifyComparison(false, e1, e2);
            VerifyComparison(false, e1, e3);
            VerifyComparison(false, e3, e2);
        }

        // XElement - namespace prefixes
        [Fact]
        public void Element5()
        {
            XElement e1 = XElement.Parse("<A xmlns='nsa'><B><!--comm--><C xmlns=''/></B></A>").Elements().First();
            XElement e2 = XElement.Parse("<A xmlns:p='nsa'><p:B><!--comm--><C xmlns=''/></p:B></A>").Elements().First();
            VerifyComparison(true, e1, e2);
            // Should always be the same ...
            VerifyComparison(true, e1, e1);
            VerifyComparison(true, e2, e2);
        }

        [Fact]
        public void ElementDynamic()
        {
            XElement helper = new XElement("helper", new XText("ko"), new XText("ho"));

            object[] content = new object[]
            {
                "text1", new object[] { new string[] { "t1", null, "t2" }, "t1t2" }, new XProcessingInstruction("PI1", ""),
                new XProcessingInstruction("PI1", ""), new XProcessingInstruction("PI2", "click"),
                new object[] { new XElement("X", new XAttribute("id", "a1"), new XText("hula")), new XElement("X", new XText("hula"), new XAttribute("id", "a1")) },
                new XElement("{nsp}X", new XAttribute("id", "a1"), "hula"),
                new object[] { new XText("koho"), helper.Nodes() },
                new object[] { new XText[] { new XText("hele"), new XText(""), new XCData("youuu") }, new XText[] { new XText("hele"), new XCData("youuu") } },
                new XComment(""), new XComment("comment"),
                new XAttribute("id1", "nono"), new XAttribute("id3", "nono2"), new XAttribute("{nsa}id3", "nono2"),
                new XAttribute("{nsb}id3", "nono2"), new XAttribute("xmlns", "default"),
                new XAttribute(XNamespace.Xmlns + "a", "nsa"), new XAttribute(XNamespace.Xmlns + "p", "nsp"),
                new XElement("{nsa}X", new XAttribute("id", "a1"), "hula", new XAttribute("{nsb}aB", "hele"), new XElement("{nsc}C"))
            };

            foreach (object[] objs in content.NonRecursiveVariations(4))
            {
                XElement e1 = new XElement("E", ExpandAndProtectTextNodes(objs, 0));
                XElement e2 = new XElement("E", ExpandAndProtectTextNodes(objs, 1));
                VerifyComparison(true, e1, e2);
                e1.RemoveAll();
                e2.RemoveAll();
            }
        }

        private IEnumerable<object> ExpandAndProtectTextNodes(IEnumerable<object> source, int position)
        {
            foreach (object o in source)
            {
                object t = (o is object[]) ? (o as object[])[position] : o;
                if (t is XText && !(t is XCData))
                {
                    yield return new XText(t as XText); // clone xtext node
                    continue;
                }
                if (t is XText[])
                {
                    yield return (t as XText[]).Select(x => new XText(x)).ToArray(); // clone XText []
                    continue;
                }
                yield return t;
            }
        }

        [Fact]
        public void Document1()
        {
            object[] content = new object[]
            {
                new object[] { new string[] { " ", null, " " }, "  " },
                new object[] { new string[] { " ", " \t" }, new XText("  \t") },
                new object[] { new XText[] { new XText(" "), new XText("\t") }, new XText(" \t") },
                new XDocumentType("root", "", "", ""), new XProcessingInstruction("PI1", ""), new XText("\n"),
                new XText("\t"), new XText("       "), new XProcessingInstruction("PI1", ""), new XElement("myroot"),
                new XProcessingInstruction("PI2", "click"),
                new object[]
                {
                    new XElement("X", new XAttribute("id", "a1"), new XText("hula")),
                    new XElement("X", new XText("hula"), new XAttribute("id", "a1"))
                },
                new XComment(""),
                new XComment("comment"),
            };

            foreach (object[] objs in content.NonRecursiveVariations(4))
            {
                XDocument doc1 = null;
                XDocument doc2 = null;
                try
                {
                    object[] o1 = ExpandAndProtectTextNodes(objs, 0).ToArray();
                    object[] o2 = ExpandAndProtectTextNodes(objs, 1).ToArray();
                    if (o1.Select(x => new ExpectedValue(false, x)).IsXDocValid()
                        || o2.Select(x => new ExpectedValue(false, x)).IsXDocValid()) continue;
                    doc1 = new XDocument(o1);
                    doc2 = new XDocument(o2);
                    VerifyComparison(true, doc1, doc2);
                }
                catch (InvalidOperationException)
                {
                    // some combination produced from the array are invalid
                    continue;
                }
                finally
                {
                    if (doc1 != null) doc1.RemoveNodes();
                    if (doc2 != null) doc2.RemoveNodes();
                }
            }
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void Document4(bool checkHashCode)
        {
            var doc1 = new XDocument(new object[] { (checkHashCode ? new XDocumentType("root", "", "", "") : null), new XElement("root") });
            var doc2 = new XDocument(new object[] { new XDocumentType("root", "", "", ""), new XElement("root") });
            VerifyComparison(checkHashCode, doc1, doc2);
        }

        private void VerifyComparison(bool expected, XNode n1, XNode n2)
        {
            Assert.Equal(XNode.EqualityComparer.Equals(n1, n2), XNode.EqualityComparer.Equals(n2, n1)); // commutative
            Assert.Equal(((IEqualityComparer)XNode.EqualityComparer).Equals(n1, n2), ((IEqualityComparer)XNode.EqualityComparer).Equals(n2, n1)); // commutative - interface
            Assert.Equal(expected, XNode.EqualityComparer.Equals(n1, n2));
            Assert.Equal(expected, ((IEqualityComparer)XNode.EqualityComparer).Equals(n1, n2));
            if (expected)
            {
                Assert.Equal(XNode.EqualityComparer.GetHashCode(n1), XNode.EqualityComparer.GetHashCode(n2));
                Assert.Equal(((IEqualityComparer)XNode.EqualityComparer).GetHashCode(n1), ((IEqualityComparer)XNode.EqualityComparer).GetHashCode(n2));
            }
        }

        [Fact]
        public void Nulls()
        {
            XElement e = new XElement("A");
            Assert.False(XNode.EqualityComparer.Equals(e, null), "left null");
            Assert.False(XNode.EqualityComparer.Equals(null, e), "right null");
            Assert.True(XNode.EqualityComparer.Equals(null, null), "both null");
            Assert.Equal(0, XNode.EqualityComparer.GetHashCode(null));
        }
    }
}
