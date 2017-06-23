// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public class DocOrderComparer
    {
        [Fact]
        public void ConnectedNodes()
        {
            XDocument doc =
                XDocument.Parse(
                    "\n<?PI?><!--comm1--><A>t1\t<B xmlns='nsb'><C/><!--cpmm--><![CDATA[hey]]></B>t2<D>string\tonly</D></A>",
                    LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> nodes = GetDescendantNodes(doc, true);
            Assert.Equal(nodes.Count(), doc.DescendantNodes().Count() + 1);
            foreach (XNode n1 in nodes)
            {
                foreach (XNode n2 in nodes)
                {
                    VerifyOrder(n1, n2, CompareInEnumeration(nodes, n1, n2));
                }
            }
        }

        private void IsAfterBeforeConsistencyCheck(XNode n1, XNode n2)
        {
            Assert.Equal((XNode.DocumentOrderComparer.Compare(n1, n2) < 0), n1.IsBefore(n2));
            Assert.Equal((XNode.DocumentOrderComparer.Compare(n1, n2) > 0), n1.IsAfter(n2));
        }

        private void VerifyOrder(XNode n1, XNode n2, int expected)
        {
            Assert.Equal(expected, XNode.DocumentOrderComparer.Compare(n1, n2)); // Comparison XNode
            Assert.Equal(expected, ((IComparer)XNode.DocumentOrderComparer).Compare(n1, n2)); // Comparison interface
            Assert.Equal(-1 * expected, XNode.DocumentOrderComparer.Compare(n2, n1));
                // Comparison XNode (-1*commutative)
            Assert.Equal(-1 * expected, ((IComparer)XNode.DocumentOrderComparer).Compare(n2, n1));
                // Comparison XNode (-1*commutative)

            IsAfterBeforeConsistencyCheck(n1, n2);
        }

        private static int IndexOf<T>(IEnumerable<T> iter, T node)
        {
            int pos = 0;
            foreach (T n in iter)
            {
                if ((n == null && node == null) || n.Equals(node)) return pos;
                pos++;
            }
            return -1;
        }

        private int CompareInEnumeration(IEnumerable<XNode> nodes, XNode n1, XNode n2)
        {
            return Math.Sign(IndexOf(nodes, n1) - IndexOf(nodes, n2));
        }

        [Fact]
        public void StandAloneNodes()
        {
            XElement e = new XElement("A");
            VerifyOrder(e, e, 0);
        }

        [Fact]
        public void AdjacentTextNodes1()
        {
            XText t1 = new XText("a");
            XText t2 = new XText("");
            XElement e = new XElement("root", t1, t2);

            VerifyOrder(t1, t2, -1);
        }

        [Fact]
        public void AdjacentTextNodes2()
        {
            XText t1 = new XText("a");
            XElement e = new XElement("root", "hello");
            e.Add(t1);

            VerifyOrder(e.FirstNode, t1, -1);
        }

        [Fact]
        public void DisconnectedNodes1()
        {
            XElement a = new XElement(
                "A",
                new XAttribute("id", "a1"),
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

            Assert.Throws<InvalidOperationException>(() => VerifyOrder(a, b, -1));
            Assert.Throws<InvalidOperationException>(() => VerifyOrder(pi, b, -1));
            Assert.Throws<InvalidOperationException>(() => VerifyOrder(b, comm, -1));

            VerifyOrder(a, pi, -1);
            VerifyOrder(a, comm, -1);
            VerifyOrder(pi, comm, -1);

            VerifyOrder(b, c, -1);
            VerifyOrder(b, d, -1);
            VerifyOrder(d, c, 1);
        }

        public static IEnumerable<object[]> GetNotXNodes()
        {
            yield return new object[] { new XAttribute("a", "A"), new XElement("E"), "x" };
            yield return new object[] { new XDeclaration("1.0", "UFT8", "false"), new XElement("E"), "x" };
            yield return new object[] { "", new XElement("E"), "x" };
            yield return new object[] { new XElement("E"), new XAttribute("a", "A"), "y" };
            yield return new object[] { new XElement("E"), new XDeclaration("1.0", "UFT8", "false"), "y" };
            yield return new object[] { new XElement("E"), "", "y" };
        }

        [Theory]
        [MemberData(nameof(GetNotXNodes))]
        public void NotXNode(object x, object y, string paramName)
        {
            AssertExtensions.Throws<ArgumentException>(paramName, () => ((IComparer)XNode.DocumentOrderComparer).Compare(x, y));
        }

        [Fact]
        public void Nulls()
        {
            Assert.Equal(0, XNode.DocumentOrderComparer.Compare(null, null));
            Assert.Equal(-1, XNode.DocumentOrderComparer.Compare(null, new XElement("A")));
            Assert.Equal(1, XNode.DocumentOrderComparer.Compare(new XElement("A"), null));
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
                    while (n != null && n != source
                           && n == ((n.Parent == null) ? n.Document.LastNode : n.Parent.LastNode)) n = (n.Parent == null) ? (XNode)n.Document : (XNode)n.Parent;
                    if (n == null || n == source) break;
                    n = n.NextNode;
                }
                yield return n;
            }
        }
    }
}
