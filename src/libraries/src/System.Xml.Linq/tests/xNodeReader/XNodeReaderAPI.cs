// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class XNodeReaderTests : XLinqTestCase
        {
            public partial class XNodeReaderAPI : XLinqTestCase
            {
                // open on different node types, scoping
                // namespaces ...
                // read subtree (scoping)      
                // adjacent text nodes

                private string _xml = "<?xml version='1.0'?>\t<A><?PI?><!--comment1--><B xmlns='x' xmlns:p='nsp'>some_text<C/><?PIX click?><D xmlns='y'/><!--comm2--><p:E/></B></A>";

                //[Variation(Priority = 1, Desc = "Open on node type: XElement (root)", Params = new object[] { XmlNodeType.Element, 0, new string[] { "A", "", "" }, 15 })]
                //[Variation(Priority = 0, Desc = "Open on node type: XElement (in the mIddle)", Params = new object[] { XmlNodeType.Element, 1, new string[] { "B", "", "x" }, 11 })]
                //[Variation(Priority = 0, Desc = "Open on node type: XElement (leaf I.)", Params = new object[] { XmlNodeType.Element, 3, new string[] { "D", "", "y" }, 2 })]
                //[Variation(Priority = 1, Desc = "Open on node type: XElement (leaf II.)", Params = new object[] { XmlNodeType.Element, 4, new string[] { "E", "p", "nsp" }, 1 })]
                //[Variation(Priority = 2, Desc = "Open on node type: PI (root level)", Params = new object[] { XmlNodeType.ProcessingInstruction, 0, new string[] { "PI", "" }, 1 })]
                //[Variation(Priority = 2, Desc = "Open on node type: PI", Params = new object[] { XmlNodeType.ProcessingInstruction, 1, new string[] { "PIX", "click" }, 1 })]
                //[Variation(Priority = 2, Desc = "Open on node type: Comment (root level)", Params = new object[] { XmlNodeType.Comment, 0, new string[] { "comment1" }, 1 })]
                //[Variation(Priority = 2, Desc = "Open on node type: Comment", Params = new object[] { XmlNodeType.Comment, 1, new string[] { "comm2" }, 1 })]
                //[Variation(Priority = 0, Desc = "Open on node type: Text (root level)", Params = new object[] { XmlNodeType.Text, 0, new string[] { "\t" }, 1 })]
                //[Variation(Priority = 1, Desc = "Open on node type: Text", Params = new object[] { XmlNodeType.Text, 1, new string[] { "some_text" }, 1 })]
                public void OpenOnNodeType()
                {
                    XmlNodeType nodeType = (XmlNodeType)Variation.Params[0];
                    int position = (int)Variation.Params[1];
                    string[] verif = (string[])Variation.Params[2];
                    int HowManyReads = (int)Variation.Params[3];

                    XDocument doc = XDocument.Load(new StringReader(_xml), LoadOptions.PreserveWhitespace);

                    // Navigate to the required node
                    int count = 0;
                    XNode node = null;
                    foreach (XNode n in doc.DescendantNodes().Where(x => x.NodeType == nodeType))
                    {
                        if (position == count)
                        {
                            node = n;
                            break;
                        }
                        count++;
                    }

                    using (XmlReader r = node.CreateReader())
                    {
                        TestLog.Compare(r.ReadState, ReadState.Initial, "r.ReadState before Read()");
                        r.Read();
                        TestLog.Compare(r.ReadState, ReadState.Interactive, "r.ReadState after Read()");
                        TestLog.Compare(r.NodeType, (nodeType == XmlNodeType.Text && count == 0) ? XmlNodeType.Whitespace : nodeType, "r.NodeType"); // 
                        switch (nodeType)
                        {
                            case XmlNodeType.Element:
                                TestLog.Compare(r.LocalName, verif[0], "r.LocalName");
                                TestLog.Compare(r.Prefix, verif[1], "r.Prefix");
                                TestLog.Compare(r.NamespaceURI, verif[2], "r.NamespaceURI");
                                break;
                            case XmlNodeType.ProcessingInstruction:
                                TestLog.Compare(r.LocalName, verif[0], "r.LocalName");
                                TestLog.Compare(r.Value, verif[1], "r.Value");
                                break;
                            case XmlNodeType.Comment:
                            case XmlNodeType.Text:
                                TestLog.Compare(r.Value, verif[0], "r.Value");
                                break;
                        }
                        int nodeWalkCount = 0;
                        do
                        {
                            nodeWalkCount++;
                            while (r.MoveToNextAttribute()) nodeWalkCount++;
                        } while (r.Read());
                        TestLog.Compare(r.ReadState, ReadState.EndOfFile, "r.ReadState after reading all");
                    }
                }

                //[Variation(Desc = "Namespaces - root element", Params = new object[] { XmlNodeType.Element, 0, new string[] { "", "" } })]
                //[Variation(Desc = "Namespaces - element", Params = new object[] { XmlNodeType.Element, 1, new string[] { "", "x" }, new string[] { "p", "nsp" } })]
                //[Variation(Desc = "Namespaces - Comment", Params = new object[] { XmlNodeType.Comment, 1, new string[] { "", "x" }, new string[] { "p", "nsp" } })]
                //[Variation(Desc = "Namespaces - element, def. ns redef", Params = new object[] { XmlNodeType.Element, 3, new string[] { "", "y" }, new string[] { "p", "nsp" } })]
                public void Namespaces()
                {
                    XmlNodeType nodeType = (XmlNodeType)Variation.Params[0];
                    int position = (int)Variation.Params[1];
                    string[][] namespaces = new string[Variation.Params.OfType<string[]>().Count()][];
                    for (int i = 0; i < Variation.Params.OfType<string[]>().Count(); i++) namespaces[i] = (string[])Variation.Params[2 + i];

                    XDocument doc = XDocument.Load(new StringReader(_xml), LoadOptions.PreserveWhitespace);

                    // Navigate to the required node
                    int count = 0;
                    XNode node = null;
                    foreach (XNode n in doc.DescendantNodes().Where(x => x.NodeType == nodeType))
                    {
                        if (position == count)
                        {
                            node = n;
                            break;
                        }
                        count++;
                    }

                    using (XmlReader r = node.CreateReader())
                    {
                        TestLog.Compare(r.ReadState, ReadState.Initial, "r.ReadState before Read()");
                        r.Read();
                        foreach (string[] nspair in namespaces)
                            TestLog.Compare(r.LookupNamespace(nspair[0]), nspair[1], "Namespace mismatch " + nspair[0] + ", " + nspair[1]);
                    }
                }

                //[Variation(Priority = 0, Desc = "ReadSubtree (sanity)")]
                public void ReadSubtreeSanity()
                {
                    XDocument doc = XDocument.Load(new StringReader(_xml), LoadOptions.PreserveWhitespace);
                    using (XmlReader r = doc.CreateReader())
                    {
                        r.Read(); // \t
                        r.Read(); // A
                        r.Read(); // PI
                        r.Read(); // comment
                        r.Read(); // B
                        using (XmlReader rSub = r.ReadSubtree())
                        {
                            int counter = 0;
                            while (rSub.Read())
                            {
                                counter++;
                                while (rSub.MoveToNextAttribute()) counter++;
                            }
                            TestLog.Compare(rSub.ReadState, ReadState.EndOfFile, "rSub.ReadState after reading all");
                            TestLog.Compare(11, counter, "Invalid node count on subtreereader");
                        }
                        TestLog.Compare(r.NodeType, XmlNodeType.EndElement, "Nodetype after readsubtree - original");
                        TestLog.Compare(r.LocalName, "B", "Localname after readsubtree - original");
                        r.Read();
                        TestLog.Compare(r.NodeType, XmlNodeType.EndElement, "Nodetype after readsubtree + read - original");
                        TestLog.Compare(r.LocalName, "A", "Localname after readsubtree + read - original");
                        r.Read();
                        TestLog.Compare(r.ReadState, ReadState.EndOfFile, "r.ReadState after reading all");
                    }
                }

                //[Variation(Priority = 0, Desc = "Adjacent text nodes (sanity I.)")]
                public void AdjacentTextNodes1()
                {
                    XElement e = new XElement("A", "start");
                    e.Add(new XText(" cont"));
                    using (XmlReader r = e.CreateReader())
                    {
                        r.Read(); // A
                        r.Read(); // "start"
                        TestLog.Compare(r.NodeType, XmlNodeType.Text, "first text node");
                        TestLog.Compare(r.Value, "start", "first text node value");
                        r.Read(); // "cont"
                        TestLog.Compare(r.NodeType, XmlNodeType.Text, "second text node");
                        TestLog.Compare(r.Value, " cont", "second text node value");
                    }
                }


                //[Variation(Priority = 0, Desc = "Adjacent text nodes (sanity II.) : ReadElementContent")]
                public void AdjacentTextNodes2()
                {
                    XElement e = new XElement("A", new XElement("B", "start"));
                    e.Element("B").Add(new XText(" cont"));
                    using (XmlReader r = e.CreateReader())
                    {
                        r.Read(); // A
                        r.Read(); // B
                        string content = r.ReadElementContentAsString();
                        TestLog.Compare(content, "start cont", "content");
                        TestLog.Compare(r.NodeType, XmlNodeType.EndElement, "nodeType");
                    }
                }

                //[Variation(Priority = 0, Desc = "Adjacent text nodes (sanity IV.) : ReadInnerXml")]
                public void AdjacentTextNodesI()
                {
                    XElement e = new XElement("A", new XElement("B", "start"));
                    e.Element("B").Add(new XText(" cont"));
                    using (XmlReader r = e.CreateReader())
                    {
                        r.Read(); // A
                        r.Read(); // B
                        string content = r.ReadInnerXml();
                        TestLog.Compare(content, "start cont", "content");
                        TestLog.Compare(r.NodeType, XmlNodeType.EndElement, "nodeType");
                    }
                }

                //[Variation(Priority = 0, Desc = "Adjacent text nodes (sanity III.) : ReadContent")]
                public void AdjacentTextNodes3()
                {
                    XElement e = new XElement("A", new XElement("B", "start"));
                    e.Element("B").Add(new XText(" cont"));
                    using (XmlReader r = e.CreateReader())
                    {
                        r.Read(); // A
                        r.Read(); // B
                        r.Read(); // "start"
                        TestLog.Compare(r.Value, "start", "r.Value on first text node");
                        string content = r.ReadContentAsString();
                        TestLog.Compare(content, "start cont", "content");
                        TestLog.Compare(r.NodeType, XmlNodeType.EndElement, "nodeType");
                    }
                }
            }
        }
    }
}
