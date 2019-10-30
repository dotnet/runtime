// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public static class AxisOrderValidation
    {
        [Fact]
        public static void NodesAfterSelfBeforeAndAfter()
        {
            XText aText = new XText("a"), bText = new XText("b");
            XElement a = new XElement("A", aText, bText);
            IEnumerable<XNode> nodes = aText.NodesAfterSelf();
            Assert.Equal(1, nodes.Count());
            bText.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void NodesBeforeSelfBeforeAndAfter()
        {
            XText aText = new XText("a"), bText = new XText("b");
            XElement a = new XElement("A", aText, bText);
            IEnumerable<XNode> nodes = bText.NodesBeforeSelf();
            Assert.Equal(1, nodes.Count());
            aText.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void AncestorsBeforeAndAfter()
        {
            XText aText = new XText("a"), bText = new XText("b");
            XElement a = new XElement("A", aText), b = new XElement("B", bText);
            a.Add(b);
            IEnumerable<XElement> nodes = bText.Ancestors();
            Assert.Equal(2, nodes.Count());
            bText.Remove();
            a.Add(bText);
            Assert.Equal(1, nodes.Count());
        }

        [Fact]
        public static void AncestorsWithXNameBeforeAndAfter()
        {
            XText aText = new XText("a"), bText = new XText("b");
            XElement a = new XElement("A", aText), b = new XElement("B", bText);
            a.Add(b);
            IEnumerable<XElement> nodes = bText.Ancestors("B");
            Assert.Equal(1, nodes.Count());
            bText.Remove(); a.Add(bText);
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void ElementsAfterSelfBeforeAndAfter()
        {
            XText aText = new XText("a"), bText = new XText("b");
            XElement a = new XElement("A", aText), b = new XElement("B", bText);
            a.Add(b);
            IEnumerable<XElement> nodes = aText.ElementsAfterSelf();
            Assert.Equal(1, nodes.Count());
            b.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void ElementsAfterSelfWithXNameBeforeAndAfter()
        {
            XText aText = new XText("a"), bText = new XText("b");
            XElement a = new XElement("A", aText), b = new XElement("B", bText);
            a.Add(b);
            IEnumerable<XElement> nodes = aText.ElementsAfterSelf("B");
            Assert.Equal(1, nodes.Count());
            b.ReplaceWith(a);
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void ElementsBeforeSelfBeforeAndAfter()
        {
            XText aText = new XText("a"), bText = new XText("b");
            XElement a = new XElement("A", aText), b = new XElement("B", bText);
            aText.AddBeforeSelf(b);
            IEnumerable<XElement> nodes = aText.ElementsBeforeSelf();
            Assert.Equal(1, nodes.Count());
            b.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void ElementsBeforeSelfWithXNameBeforeAndAfter()
        {
            XText aText = new XText("a"), bText = new XText("b");
            XElement a = new XElement("A", aText), b = new XElement("B", bText);
            aText.AddBeforeSelf(b);
            IEnumerable<XElement> nodes = aText.ElementsBeforeSelf("B");
            Assert.Equal(1, nodes.Count());
            b.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void NodesOnXDocBeforeAndAfter()
        {
            XText aText = new XText("a"), bText = new XText("b");
            XElement a = new XElement("A", aText, bText);
            XDocument xDoc = new XDocument(a);
            IEnumerable<XNode> nodes = xDoc.Nodes();
            Assert.Equal(1, nodes.Count());
            a.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void DescendantNodesOnXDocBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            XDocument xDoc = new XDocument(a);
            IEnumerable<XNode> nodes = xDoc.DescendantNodes();
            Assert.Equal(4, nodes.Count());
            a.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void ElementsOnXDocBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            XDocument xDoc = new XDocument(a);
            IEnumerable<XElement> nodes = xDoc.Elements();
            Assert.Equal(1, nodes.Count());
            a.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void ElementsWithXNameOnXDocBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            XDocument xDoc = new XDocument(a);
            IEnumerable<XElement> nodes = xDoc.Elements("A");
            Assert.Equal(1, nodes.Count());
            a.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void DescendantsOnXDocBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            XDocument xDoc = new XDocument(a);
            IEnumerable<XElement> nodes = xDoc.Descendants("B");
            Assert.Equal(1, nodes.Count());
            b.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void DescendantsWithXNameOnXDocBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            b.Add(b, b); a.Add(b);
            XDocument xDoc = new XDocument(a);
            IEnumerable<XElement> nodes = xDoc.Descendants("B");
            Assert.Equal(4, nodes.Count());
            b.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void NodesOnXElementBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            IEnumerable<XNode> nodes = a.Nodes();
            Assert.Equal(2, nodes.Count());
            b.Remove();
            Assert.Equal(1, nodes.Count());
        }

        [Fact]
        public static void DescendantNodesOnXElementBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            IEnumerable<XNode> nodes = a.DescendantNodes();
            Assert.Equal(3, nodes.Count());
            a.Add("New Text Node");
            Assert.Equal(4, nodes.Count());
        }

        [Fact]
        public static void ElementsOnXElementBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            IEnumerable<XElement> nodes = a.Elements();
            Assert.Equal(0, nodes.Count());
            a.Add(b, b, b, b);
            Assert.Equal(4, nodes.Count());
        }

        [Fact]
        public static void ElementsWithXNameOnXElementBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            IEnumerable<XElement> nodes = a.Elements("B");
            Assert.Equal(0, nodes.Count());
            a.Add(b, b, b, b);
            Assert.Equal(4, nodes.Count());
        }

        [Fact]
        public static void DescendantsOnXElementBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            IEnumerable<XElement> nodes = a.Descendants();
            Assert.Equal(1, nodes.Count());
            b.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void DescendantsWithXNameOnXElementBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            IEnumerable<XElement> nodes = a.Descendants("B");
            Assert.Equal(1, nodes.Count());
            b.Remove();
            Assert.Equal(0, nodes.Count());
        }

        [Fact]
        public static void DescendantNodesAndSelfBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            IEnumerable<XNode> nodes = a.DescendantNodesAndSelf();
            Assert.Equal(4, nodes.Count());
            a.Add("New Text Node");
            Assert.Equal(5, nodes.Count());
        }

        [Fact]
        public static void DescendantsAndSelfBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            IEnumerable<XElement> nodes = a.DescendantsAndSelf();
            Assert.Equal(2, nodes.Count());
            b.Add(a);
            Assert.Equal(4, nodes.Count());
        }

        [Fact]
        public static void DescendantsAndSelfWithXNameBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            IEnumerable<XElement> nodes = a.DescendantsAndSelf("A");
            Assert.Equal(1, nodes.Count());
            b.ReplaceWith(a);
            Assert.Equal(2, nodes.Count());
        }

        [Fact]
        public static void AncestorsAndSelfBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            IEnumerable<XElement> nodes = b.AncestorsAndSelf();
            Assert.Equal(2, nodes.Count());
            XElement c = new XElement("C", "c", a);
            Assert.Equal(3, nodes.Count());
        }

        [Fact]
        public static void AncestorsAndSelfWithXNameBeforeAndAfter()
        {
            XElement a = new XElement("A", "a"), b = new XElement("B", "b");
            a.Add(b);
            IEnumerable<XElement> nodes = b.AncestorsAndSelf("A");
            Assert.Equal(1, nodes.Count());
            XElement c = new XElement("A", "a", a);
            Assert.Equal(2, nodes.Count());
        }

        [Fact]
        public static void AttributesBeforeAndAfter()
        {
            XElement a = new XElement("A", "a");
            IEnumerable<XAttribute> nodes = a.Attributes();
            Assert.Equal(0, nodes.Count());
            a.Add(new XAttribute("name", "a"), new XAttribute("type", "alphabet"));
            Assert.Equal(2, nodes.Count());
        }

        [Fact]
        public static void AttributeWithXNameBeforeAndAfter()
        {
            XElement a = new XElement("A", "a");
            IEnumerable<XAttribute> nodes = a.Attributes("name");
            Assert.Equal(0, nodes.Count());
            a.Add(new XAttribute("name", "a"), new XAttribute("type", "alphabet"));
            Assert.Equal(1, nodes.Count());
        }

        [Fact]
        public static void IEnumerableInDocumentOrderVariationOne()
        {
            XDocument xDoc = TestData.GetDocumentWithContacts();
            IEnumerable<XNode> xNodes = xDoc.Root.DescendantNodesAndSelf().Reverse();
            Assert.True(xNodes.InDocumentOrder().EqualsAll(xDoc.Root.DescendantNodesAndSelf(), XNode.DeepEquals));
        }

        [Fact]
        public static void IEnumerableInDocumentOrderVariationTwo()
        {
            XDocument xDoc = TestData.GetDocumentWithContacts();
            IEnumerable<XElement> xElement = xDoc.Root.DescendantsAndSelf().Reverse();
            Assert.True(xElement.InDocumentOrder().EqualsAll(xDoc.Root.DescendantsAndSelf(), XNode.DeepEquals));
        }

        [Fact]
        public static void ReorderToDocumentOrder()
        {
            XDocument xDoc = TestData.GetDocumentWithContacts();
            Random rnd = new Random();
            var randomOrderedElements = xDoc.Root.DescendantNodesAndSelf().OrderBy(n => rnd.Next(0, int.MaxValue));
            using (var en = randomOrderedElements.InDocumentOrder().GetEnumerator())
            {
                en.MoveNext();
                var nodeFirst = en.Current;
                while (en.MoveNext())
                {
                    var nodeSecond = en.Current;
                    Assert.True(XNode.DocumentOrderComparer.Compare(nodeFirst, nodeSecond) < 0);
                    nodeFirst = nodeSecond;
                }
            }
        }
    }
}
