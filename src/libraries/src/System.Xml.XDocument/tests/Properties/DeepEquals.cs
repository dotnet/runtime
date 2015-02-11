// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class PropertiesTests : XLinqTestCase
        {
            public partial class DeepEqualsTests : XLinqTestCase
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

                //[Variation(Priority = 0, Desc = "PI normal", Params = new object[] { "PI", "click", "PI", "click", true })]
                //[Variation(Priority = 0, Desc = "PI target=data", Params = new object[] { "PI", "PI", "PI", "PI", true })]
                //[Variation(Priority = 0, Desc = "PI data = ''", Params = new object[] { "PI", "", "PI", "", true })]
                //[Variation(Priority = 1, Desc = "PI data1!=data2", Params = new object[] { "PI", "click", "PI", "", false })]
                //[Variation(Priority = 2, Desc = "PI target1!=target2!", Params = new object[] { "AAAAP", "click", "AAAAQ", "click", false })]
                //[Variation(Priority = 2, Desc = "PI hashconflict I.", Params = new object[] { "AAAAP", "AAAAQ", "AAAAP", "AAAAQ", true })]
                //[Variation(Priority = 2, Desc = "PI data=target, not the same", Params = new object[] { "PA", "PA", "PI", "PI", false })]
                //[Variation(Priority = 2, Desc = "PI hashconflict II.", Params = new object[] { "AAAAP", "AAAAQ", "AAAAQ", "AAAAP", false })]
                public void PI()
                {
                    bool expected = (bool)Variation.Params[4];
                    XProcessingInstruction p1 = new XProcessingInstruction(Variation.Params[0] as string, Variation.Params[1] as string);
                    XProcessingInstruction p2 = new XProcessingInstruction(Variation.Params[2] as string, Variation.Params[3] as string);

                    VerifyComparison(expected, p1, p2);

                    XDocument doc = new XDocument(p1);
                    XElement e2 = new XElement("p2p", p2);

                    VerifyComparison(expected, p1, p2);
                }

                //[Variation(Priority = 0, Desc = "XComment - not equals, hashconflict", Params = new object[] { "AAAAP", "AAAAQ", false })]
                //[Variation(Priority = 0, Desc = "XComment - equals", Params = new object[] { "AAAAP", "AAAAP", true })]
                //[Variation(Priority = 3, Desc = "XComment - Whitespaces (negative)", Params = new object[] { "  ", " ", false })]
                //[Variation(Priority = 3, Desc = "XComment - Whitespaces", Params = new object[] { " ", " ", true })]
                //[Variation(Priority = 1, Desc = "XComment - Empty", Params = new object[] { "", "", true })]
                public void Comment()
                {
                    bool expected = (bool)Variation.Params[2];
                    XComment c1 = new XComment(Variation.Params[0] as string);
                    XComment c2 = new XComment(Variation.Params[1] as string);
                    VerifyComparison(expected, c1, c2);

                    XDocument doc = new XDocument(c1);
                    XElement e2 = new XElement("p2p", c2);

                    VerifyComparison(expected, c1, c2);
                }

                //[Variation(Priority = 0, Desc = "DTD : all field", Params = new object[] { new string[] { "root", "a", "b", "c" }, new string[] { "root", "a", "b", "c" }, true })]
                //[Variation(Priority = 1, Desc = "DTD : all nulls", Params = new object[] { new string[] { "root", null, null, null }, new string[] { "root", null, null, null }, true })]
                //[Variation(Priority = 2, Desc = "DTD : internal subset only", Params = new object[] { new string[] { "root", null, null, "data" }, new string[] { "root", null, null, "data" }, true })]
                //[Variation(Priority = 0, Desc = "DTD (negative) : name diff", Params = new object[] { new string[] { "A", "", "", "" }, new string[] { "B", "", "", "" }, false })]
                //[Variation(Priority = 1, Desc = "DTD (negative) : subset diff", Params = new object[] { new string[] { "A", null, null, "aa" }, new string[] { "A", null, null, "bb" }, false })]
                //[Variation(Priority = 2, Desc = "DTD (negative) : null vs. \"\"", Params = new object[] { new string[] { "A", "", "", "" }, new string[] { "A", null, null, null }, false })]
                public void DTD()
                {
                    bool expected = (bool)Variation.Params[2];
                    var data0 = Variation.Params[0] as string[];
                    var data1 = Variation.Params[1] as string[];
                    XDocumentType dtd1 = new XDocumentType(data0[0], data0[1], data0[2], data0[3]);
                    XDocumentType dtd2 = new XDocumentType(data1[0], data1[1], data1[2], data1[3]);
                    VerifyComparison(expected, dtd1, dtd2);
                }

                //[Variation(Priority = 0, Desc = "DTD (null vs empty)", Params = new object[] { "", "", "", "" })]

                //[Variation(Priority = 0, Desc = "XText - different", Params = new object[] { "same", "different", false })]
                //[Variation(Priority = 0, Desc = "XText - same", Params = new object[] { "same", "same", true })]
                //[Variation(Priority = 2, Desc = "XText - Empty", Params = new object[] { "", "", true })]
                //[Variation(Priority = 2, Desc = "XText - Whitespaces ()", Params = new object[] { " ", " ", true })]
                //[Variation(Priority = 2, Desc = "XText - Whitespaces (negative)", Params = new object[] { "\n", " ", false })]
                public void Text1()
                {
                    bool expected = (bool)Variation.Params[2];
                    XText t1 = new XText(Variation.Params[0] as string);
                    XText t2 = new XText(Variation.Params[1] as string);
                    VerifyComparison(expected, t1, t2);

                    XElement e2 = new XElement("p2p", t2);
                    e2.Add(t1);
                    VerifyComparison(expected, t1, t2);
                }

                //[Variation(Priority = 0, Desc = "XCData - different", Params = new object[] { "same", "different", false })]
                //[Variation(Priority = 0, Desc = "XCData - same", Params = new object[] { "same", "same", true })]
                //[Variation(Priority = 2, Desc = "XCData - Empty", Params = new object[] { "", "", true })]
                //[Variation(Priority = 2, Desc = "XCData - Whitespaces ()", Params = new object[] { " ", " ", true })]
                //[Variation(Priority = 2, Desc = "XCData - Whitespaces (negative)", Params = new object[] { "\n", " ", false })]
                public void CData()
                {
                    bool expected = (bool)Variation.Params[2];
                    XCData t1 = new XCData(Variation.Params[0] as string);
                    XCData t2 = new XCData(Variation.Params[1] as string);
                    VerifyComparison(expected, t1, t2);

                    XElement e2 = new XElement("p2p", t2);
                    e2.Add(t1);
                    VerifyComparison(expected, t1, t2);
                }

                //[Variation(Priority = 0, Desc = "Xtext vs. XCData - same", Params = new object[] { "same", "same", false })]
                //[Variation(Priority = 2, Desc = "Xtext vs. XCData - Empty", Params = new object[] { "", "", false })]
                //[Variation(Priority = 2, Desc = "Xtext vs. XCData - Whitespaces", Params = new object[] { " ", " ", false })]
                public void TextVsCData()
                {
                    bool expected = (bool)Variation.Params[2];
                    XText t1 = new XCData(Variation.Params[0] as string);
                    XText t2 = new XText(Variation.Params[1] as string);
                    VerifyComparison(expected, t1, t2);

                    XElement e2 = new XElement("p2p", t2);
                    e2.Add(t1);
                    VerifyComparison(expected, t1, t2);
                }

                //[Variation(Priority = 2, Desc = "XText do not concatenate inside")]
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

                //[Variation(Priority = 0, Desc = "XElement - smoke", Params = new object[] { "<A/>", "<A></A>", false })]
                //[Variation(Priority = 0, Desc = "XElement - atribute missing", Params = new object[] { "<A/>", "<A Id='a'/>", false })]
                //[Variation(Priority = 0, Desc = "XElement - atributes", Params = new object[] { "<A Id='a'/>", "<A Id='a'/>", true })]
                //[Variation(Priority = 1, Desc = "XElement - atributes (same, different order)", Params = new object[] { "<A at='1' Id='a'/>", "<A Id='a' at='1'/>", false })]
                //[Variation(Priority = 0, Desc = "XElement - atributes (same, same order)", Params = new object[] { "<A at='1' Id='a'/>", "<A at='1' Id='a'/>", true })]
                //[Variation(Priority = 1, Desc = "XElement - atributes (same, same order, different value)", Params = new object[] { "<A at='1' Id='a'/>", "<A at='1' Id='ab'/>", false })]
                //[Variation(Priority = 0, Desc = "XElement - atributes (same, same order, namespace decl)", Params = new object[] { "<A p:at='1' xmlns:p='nsp'/>", "<A p:at='1' xmlns:p='nsp'/>", true })]
                //[Variation(Priority = 0, Desc = "XElement - atributes (same, same order, namespace decl, different prefix)", Params = new object[] { "<A p:at='1' xmlns:p='nsp'/>", "<A q:at='1' xmlns:q='nsp'/>", false })]
                //[Variation(Priority = 0, Desc = "XElement - String content", Params = new object[] { "<A>text</A>", "<A>text</A>", true })]
                //[Variation(Priority = 0, Desc = "XElement - String + PI content (negative)", Params = new object[] { "<A>text<?PI click?></A>", "<A><?PI click?>text</A>", false })]
                //[Variation(Priority = 1, Desc = "XElement - String + PI content", Params = new object[] { "<A>text<?PI click?></A>", "<A>text<?PI click?></A>", true })]
                public void Element()
                {
                    bool expected = (bool)Variation.Params[2];
                    XElement e1 = XElement.Parse(Variation.Params[0] as string);
                    XElement e2 = XElement.Parse(Variation.Params[1] as string);
                    VerifyComparison(expected, e1, e2);
                    // Should always be the same ...
                    VerifyComparison(true, e1, e1);
                    VerifyComparison(true, e2, e2);
                }

                //[Variation(Priority = 0, Desc = "XElement - String content vs. text node vs. CData")]
                public void Element2()
                {
                    XElement e1 = new XElement("A", "string_content");
                    XElement e2 = new XElement("A", new XText("string_content"));
                    XElement e3 = new XElement("A", new XCData("string_content"));

                    VerifyComparison(true, e1, e2);
                    VerifyComparison(false, e1, e3);
                    VerifyComparison(false, e2, e3);
                }

                //[Variation(Priority = 0, Desc = "XElement - text node concatenations")]
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

                //[Variation(Priority = 0, Desc = "XElement - text node incarnation - by touching", Param = 1)]
                //[Variation(Priority = 0, Desc = "XElement - text node incarnation - by adding new node", Param = 2)]
                public void Element6()
                {
                    XElement e1 = new XElement("A", "datata");
                    XElement e2 = new XElement("A", "datata");
                    switch ((int)Variation.Param)
                    {
                        case 1:
                            XComment c = new XComment("hele");
                            e2.Add(c);
                            c.Remove();
                            break;
                        case 2:
                            break;
                        default:
                            TestLog.Compare(false, "Unexpected value - test failed");
                            break;
                    }

                    VerifyComparison(true, e1, e2);
                }

                //[Variation(Priority = 2, Desc = "XElement - text node concatenations (negative)")]
                public void Element4()
                {
                    XElement e1 = new XElement("A", new XCData("string_content"));
                    XElement e2 = new XElement("A", new XCData("string"), new XCData("_content"));
                    XElement e3 = new XElement("A", new XCData("string"), "_content");
                    VerifyComparison(false, e1, e2);
                    VerifyComparison(false, e1, e3);
                    VerifyComparison(false, e3, e2);
                }

                //[Variation(Priority = 0, Desc = "XElement - namespace prefixes", Params = new object[] { "<A xmlns='nsa'><B><!--comm--><C xmlns=''/></B></A>", "<A xmlns:p='nsa'><p:B><!--comm--><C xmlns=''/></p:B></A>", true })]
                public void Element5()
                {
                    bool expected = (bool)Variation.Params[2];
                    XElement e1 = XElement.Parse(Variation.Params[0] as string).Elements().First();
                    XElement e2 = XElement.Parse(Variation.Params[1] as string).Elements().First();
                    VerifyComparison(expected, e1, e2);
                    // Should always be the same ...
                    VerifyComparison(true, e1, e1);
                    VerifyComparison(true, e2, e2);
                }

                //[Variation(Priority = 0, Desc = "XElement - dynamic")]
                public void ElementDynamic()
                {
                    XElement helper = new XElement("helper", new XText("ko"), new XText("ho"));

                    object[] content = new object[] {
                            "text1",
                            new object [] {new string [] {"t1", null, "t2"}, "t1t2"},
                            new XProcessingInstruction ("PI1",""),
                            new XProcessingInstruction ("PI1",""),
                            new XProcessingInstruction ("PI2","click"),
                            new object [] {new XElement ("X", new XAttribute ("id","a1"), new XText ("hula")), new XElement ("X", new XText ("hula"), new XAttribute ("id","a1"))},
                            new XElement ("{nsp}X", new XAttribute ("id","a1"), "hula"),
                            new object [] {new XText ("koho"),  helper.Nodes()},
                            new object [] {new XText [] {new XText ("hele"), new XText (""), new XCData ("youuu")}, new XText [] {new XText ("hele"), new XCData ("youuu")}},
                            new XComment (""),
                            new XComment ("comment"),
                            new XAttribute ("id1", "nono"),
                            new XAttribute ("id3", "nono2"),
                            new XAttribute ("{nsa}id3", "nono2"),
                            new XAttribute ("{nsb}id3", "nono2"),
                            new XAttribute ("xmlns", "default"),
                            new XAttribute (XNamespace.Xmlns+"a", "nsa"),
                            new XAttribute (XNamespace.Xmlns+"p", "nsp"),
                            new XElement ("{nsa}X", new XAttribute ("id","a1"), "hula", new XAttribute("{nsb}aB", "hele"), new XElement ("{nsc}C"))
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
                            yield return new XText(t as XText);   // clone xtext node
                            continue;
                        }
                        if (t is XText[])
                        {
                            yield return (t as XText[]).Select(x => new XText(x)).ToArray();  // clone XText []
                            continue;
                        }
                        yield return t;
                    }
                }

                //[Variation(Priority = 0, Desc = "XDocument : dynamic")]
                public void Document1()
                {
                    object[] content = new object[] {
                            new object [] {new string [] {" ", null, " "}, "  "},
                            new object [] {new string [] {" "," \t"}, new XText("  \t")},
                            new object [] {new XText [] {new XText(" "), new XText("\t")}, new XText(" \t")},
                            new XDocumentType ("root", "", "", ""),
                            new XProcessingInstruction ("PI1",""),
                            new XText("\n"),
                            new XText("\t"),
                            new XText("       "),
                            new XProcessingInstruction ("PI1",""),
                            new XElement ("myroot"),
                            new XProcessingInstruction ("PI2","click"),
                            new object [] {new XElement ("X", new XAttribute ("id","a1"), new XText ("hula")), new XElement ("X", new XText ("hula"), new XAttribute ("id","a1"))},
                            new XComment (""),
                            new XComment ("comment"),
                        };

                    foreach (object[] objs in content.NonRecursiveVariations(4))
                    {
                        XDocument doc1 = null;
                        XDocument doc2 = null;
                        try
                        {
                            object[] o1 = ExpandAndProtectTextNodes(objs, 0).ToArray();
                            object[] o2 = ExpandAndProtectTextNodes(objs, 1).ToArray();
                            if (o1.Select(x => new ExpectedValue(false, x)).IsXDocValid() || o2.Select(x => new ExpectedValue(false, x)).IsXDocValid()) continue;
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

                //[Variation(Priority = 2, Desc = "XDocument : DTD", Param = true)]
                //[Variation(Priority = 2, Desc = "XDocument : DTD (negative)", Param = false)]
                public void Document4()
                {
                    bool expected = (bool)Variation.Param;
                    XDocument doc1 = new XDocument(new object[] { (expected ? new XDocumentType("root", "", "", "") : null), new XElement("root") });
                    XDocument doc2 = new XDocument(new object[] { new XDocumentType("root", "", "", ""), new XElement("root") });
                    VerifyComparison(expected, doc1, doc2);
                }

                private void VerifyComparison(bool expected, XNode n1, XNode n2)
                {
                    TestLog.Compare(XNode.EqualityComparer.Equals(n1, n2), XNode.EqualityComparer.Equals(n2, n1), "commutative");
                    TestLog.Compare(((IEqualityComparer)XNode.EqualityComparer).Equals(n1, n2), ((IEqualityComparer)XNode.EqualityComparer).Equals(n2, n1), "commutative - interface");
                    TestLog.Compare(expected, XNode.EqualityComparer.Equals(n1, n2), "Equals");
                    TestLog.Compare(expected, ((IEqualityComparer)XNode.EqualityComparer).Equals(n1, n2), "Equals - interface");
                    if (expected)
                    {
                        TestLog.Compare(XNode.EqualityComparer.GetHashCode(n1), XNode.EqualityComparer.GetHashCode(n2), "GetHashCode");
                        TestLog.Compare(((IEqualityComparer)XNode.EqualityComparer).GetHashCode(n1), ((IEqualityComparer)XNode.EqualityComparer).GetHashCode(n2), "GetHashCode - interface");
                    }
                }

                //[Variation(Priority = 2, Desc = "Nulls", Param = true)]
                public void Nulls()
                {
                    XElement e = new XElement("A");
                    TestLog.Compare(!XNode.EqualityComparer.Equals(e, null), "left null");
                    TestLog.Compare(!XNode.EqualityComparer.Equals(null, e), "right null");
                    TestLog.Compare(XNode.EqualityComparer.Equals(null, null), "both null");
                    TestLog.Compare(XNode.EqualityComparer.GetHashCode(null), 0, "GetHashCode (null)");
                }
            }
        }
    }
}
