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
            public partial class DocOrderComparer : XLinqTestCase
            {
                //[Variation(Priority = 0, Desc = "Connected nodes")]
                public void ConnectedNodes()
                {
                    XDocument doc = XDocument.Parse("\n<?PI?><!--comm1--><A>t1\t<B xmlns='nsb'><C/><!--cpmm--><![CDATA[hey]]></B>t2<D>string\tonly</D></A>", LoadOptions.PreserveWhitespace);
                    IEnumerable<XNode> nodes = GetDescendantNodes(doc, true);
                    TestLog.Compare(doc.DescendantNodes().Count() + 1, nodes.Count(), "TEST_FAILED: Selection method failed");
                    foreach (XNode n1 in nodes)
                    {
                        foreach (XNode n2 in nodes)
                        {
                            VerifyOrder(nodes, n1, n2);
                        }
                    }
                }

                private void VerifyOrder(IEnumerable<XNode> nodes, XNode n1, XNode n2)
                {
                    TestLog.Compare(XNode.DocumentOrderComparer.Compare(n1, n2), CompareInEnumeration(nodes, n1, n2), "Comparison XNode");
                    TestLog.Compare(((IComparer)XNode.DocumentOrderComparer).Compare(n1, n2), CompareInEnumeration(nodes, n1, n2), "Comparison interface");
                    TestLog.Compare(XNode.DocumentOrderComparer.Compare(n2, n1), -1 * CompareInEnumeration(nodes, n1, n2), "Comparison XNode (-1*commutative)");
                    TestLog.Compare(((IComparer)XNode.DocumentOrderComparer).Compare(n2, n1), -1 * CompareInEnumeration(nodes, n1, n2), "Comparison interface (-1*commutative)");
                    IsAfterBeforeConsistencyCheck(n1, n2);
                }

                private void IsAfterBeforeConsistencyCheck(XNode n1, XNode n2)
                {
                    TestLog.Compare(n1.IsBefore(n2) == (XNode.DocumentOrderComparer.Compare(n1, n2) < 0), "IsBefore");
                    TestLog.Compare(n1.IsAfter(n2) == (XNode.DocumentOrderComparer.Compare(n1, n2) > 0), "IsAfter");
                }

                private void VerifyOrder(XNode n1, XNode n2, int expected)
                {
                    TestLog.Compare(XNode.DocumentOrderComparer.Compare(n1, n2), expected, "Comparison XNode");
                    TestLog.Compare(((IComparer)XNode.DocumentOrderComparer).Compare(n1, n2), expected, "Comparison interface");
                    TestLog.Compare(XNode.DocumentOrderComparer.Compare(n2, n1), -1 * expected, "Comparison XNode (-1*commutative)");
                    TestLog.Compare(((IComparer)XNode.DocumentOrderComparer).Compare(n2, n1), -1 * expected, "Comparison interface (-1*commutative)");
                    IsAfterBeforeConsistencyCheck(n1, n2);
                }

                private int CompareInEnumeration(IEnumerable<XNode> nodes, XNode n1, XNode n2)
                {
                    return Math.Sign(nodes.IndexOf(n1) - nodes.IndexOf(n2));
                }

                //[Variation(Priority = 3, Desc = "Single standalone node (sanity)")]
                public void StandAloneNodes()
                {
                    XElement e = new XElement("A");
                    VerifyOrder(e, e, 0);
                }

                //[Variation(Priority = 3, Desc = "Adjacent text nodes I. (sanity)")]
                public void AdjacentTextNodes1()
                {
                    XText t1 = new XText("a");
                    XText t2 = new XText("");
                    XElement e = new XElement("root", t1, t2);

                    VerifyOrder(t1, t2, -1);
                }

                //[Variation(Priority = 3, Desc = "Adjacent text nodes II. (sanity)")]
                public void AdjacentTextNodes2()
                {
                    XText t1 = new XText("a");
                    XElement e = new XElement("root", "hello");
                    e.Add(t1);

                    VerifyOrder(e.FirstNode, t1, -1);
                }

                //[Variation(Priority = 2, Desc = "Disconnected nodes")]
                public void DisconnectedNodes1()
                {
                    XElement a = new XElement("A", new XAttribute("id", "a1"),
                                    new XProcessingInstruction("PI", "data"),
                                    new XElement("B", new XElement("C"), new XElement("D")),
                                    new XComment("comment"));

                    XElement b = a.Element("B");
                    XElement c = b.Element("C");
                    XElement d = b.Element("D");

                    XProcessingInstruction pi = a.FirstNode as XProcessingInstruction;
                    XComment comm = a.LastNode as XComment;

                    // sanity tests
                    VerifyOrder(a, b, -1);
                    VerifyOrder(a, pi, -1);
                    VerifyOrder(a, comm, -1);
                    VerifyOrder(pi, b, -1);
                    VerifyOrder(b, comm, -1);
                    VerifyOrder(pi, comm, -1);

                    b.Remove();

                    try
                    {
                        VerifyOrder(a, b, -1);
                        TestLog.Compare(false, "exception expected");
                    }
                    catch (InvalidOperationException) { }

                    try
                    {
                        VerifyOrder(pi, b, -1);
                        TestLog.Compare(false, "exception expected");
                    }
                    catch (InvalidOperationException) { }

                    try
                    {
                        VerifyOrder(b, comm, -1);
                        TestLog.Compare(false, "exception expected");
                    }
                    catch (InvalidOperationException) { }

                    VerifyOrder(a, pi, -1);
                    VerifyOrder(a, comm, -1);
                    VerifyOrder(pi, comm, -1);

                    VerifyOrder(b, c, -1);
                    VerifyOrder(b, d, -1);
                    VerifyOrder(d, c, 1);
                }

                //[Variation(Priority = 2, Desc = "Not XNode")]
                public void NotXNode()
                {
                    int helper = 0;

                    try
                    {
                        helper = ((IComparer)XNode.DocumentOrderComparer).Compare(new XAttribute("a", "A"), new XElement("E"));
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        helper = ((IComparer)XNode.DocumentOrderComparer).Compare(new XDeclaration("1.0", "UFT8", "false"), new XElement("E"));
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        helper = ((IComparer)XNode.DocumentOrderComparer).Compare("", new XElement("E"));
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        helper = ((IComparer)XNode.DocumentOrderComparer).Compare(new XElement("E"), new XAttribute("a", "A"));
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        helper = ((IComparer)XNode.DocumentOrderComparer).Compare(new XElement("E"), new XDeclaration("1.0", "UFT8", "false"));
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        helper = ((IComparer)XNode.DocumentOrderComparer).Compare(new XElement("E"), "");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (ArgumentException) { }
                }

                //[Variation(Priority = 2, Desc = "Nulls")]
                public void Nulls()
                {
                    TestLog.Compare(XNode.DocumentOrderComparer.Compare(null, null), 0, "null, null");
                    TestLog.Compare(XNode.DocumentOrderComparer.Compare(null, new XElement("A")), -1, "null, XElement");
                    TestLog.Compare(XNode.DocumentOrderComparer.Compare(new XElement("A"), null), 1, "XElement, null");
                }

                // Copied from the XLinq sources 
                // This method will return the nodes in the doc order 
                private IEnumerable<XNode> GetDescendantNodes(XContainer source, bool self)
                {
                    if (self) yield return source;
                    XNode n = source;
                    while (true)
                    {
                        XContainer c = n as XContainer;
                        XNode first;
                        if (c != null && (first = c.FirstNode) != null)
                        {
                            n = first;
                        }
                        else
                        {
                            while (n != null && n != source && n == ((n.Parent == null) ? n.Document.LastNode : n.Parent.LastNode)) n = (n.Parent == null) ? (XNode)n.Document : (XNode)n.Parent;
                            if (n == null || n == source) break;
                            n = n.NextNode;
                        }
                        yield return n;
                    }
                }
            }
        }
    }
}

