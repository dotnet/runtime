// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.Test.ModuleCore;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public class RegressionTests
    {
        [Fact]
        public void XPIEmptyStringShouldNotBeAllowed()
        {
            var pi = new XProcessingInstruction("PI", "data");
            Assert.Throws<ArgumentException>(() => pi.Target = string.Empty);
        }

        [Fact]
        public void RemovingMixedContent()
        {
            XElement a = XElement.Parse(@"<A>t1<B/>t2</A>");
            a.Nodes().Skip(1).Remove();
            Assert.Equal("<A>t1</A>", a.ToString(SaveOptions.DisableFormatting));
        }

        [Fact]
        public void CannotParseDTD()
        {
            string xml = "<!DOCTYPE x []><x/>";
            XElement e = XElement.Parse(xml);
            Assert.Equal("<x />", e.ToString(SaveOptions.DisableFormatting));
        }

        [Fact]
        public void ReplaceContent()
        {
            XElement a = XElement.Parse("<A><B><C/></B></A>");
            a.Element("B").ReplaceNodes(a.Nodes());
            XElement x = a;
            foreach (string s in (new string[] { "A", "B", "B" }))
            {
                Assert.Equal(x.Name.LocalName, s);
                x = x.FirstNode as XElement;
            }
        }

        [Fact]
        public void DuplicateNamespaceDeclarationIsAllowed()
        {
            XElement element = XElement.Parse("<A xmlns:p='ns'/>");
            Assert.Throws<InvalidOperationException>(() => element.Add(new XAttribute(XNamespace.Xmlns + "p", "ns")));
        }

        [Fact]
        public void ManuallyDeclaredPrefixNamespacePairIsNotReflectedInTheXElementSerialization()
        {
            var element = XElement.Parse("<A/>");
            element.Add(new XAttribute(XNamespace.Xmlns + "p", "ns"));
            element.Add(new XElement("{ns}B", null));
            MemoryStream sourceStream = new MemoryStream();
            element.Save(sourceStream);
            sourceStream.Position = 0;
            // creating the following element with expected output so we can compare
            XElement target = XElement.Parse("<A xmlns:p=\"ns\"><p:B /></A>");
            MemoryStream targetStream = new MemoryStream();
            target.Save(targetStream);
            targetStream.Position = 0;
            XmlDiff.XmlDiff diff = new XmlDiff.XmlDiff();
            Assert.True(diff.Compare(sourceStream, targetStream));
        }

        [Fact]
        public void XNameGetDoesThrowWhenPassingNulls1()
        {
            Assert.Throws<ArgumentNullException>(() => XName.Get(null, null));
        }

        [Fact]
        public void XNameGetDoesThrowWhenPassingNulls2()
        {
            Assert.Throws<ArgumentNullException>(() => XName.Get(null, "MyName"));
        }

        [Fact]
        public void HashingNamePartsShouldBeSameAsHashingExpandedNameWhenUsingNamespaces()
        {
            // shouldn't throw
            XElement element1 = new XElement(
                XName.Get("e1", "ns1"),
                "e1 should be in \"ns1\"",
                new XElement(
                    XName.Get("e2", "ns-default1"),
                    "e2 should be in ns-default1",
                    new XElement(
                        XName.Get("e3", "ns-default2"),
                        "e3 should be in ns-default2",
                        new XElement(XName.Get("e4", "ns2"), "e4 should be in ns2"))));
        }

        [Fact]
        public void CreatingNewXElementsPassingNullReaderAndOrNullXNameShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => new XElement((XName)null));
            Assert.Throws<ArgumentNullException>(() => (XElement)XNode.ReadFrom((XmlReader)null));
        }

        [Fact]
        public void XNodeAddBeforeSelfPrependingTextNodeToTextNodeDoesDisconnectTheOriginalNode()
        {
            XElement e = new XElement("e1", new XElement("e2"), "text1", new XElement("e3"));
            XNode t = e.FirstNode.NextNode;
            t.AddBeforeSelf("text2");
            t.AddBeforeSelf("text3");
            Assert.Equal("text2text3text1", e.Value);
        }

        [Fact]
        public void ReadSubtreeOnXReaderThrows()
        {
            XElement xe = new XElement(
                "root",
                new XElement("A", new XElement("B", "data")),
                new XProcessingInstruction("PI", "joke"));

            using (XmlReader r = xe.CreateReader())
            {
                r.Read();
                r.Read();
                using (XmlReader subR = r.ReadSubtree())
                {
                    subR.Read();
                }
            }
        }

        [Fact]
        public void StackOverflowForDeepNesting()
        {
            StringBuilder sb = new StringBuilder();

            for (long l = 0; l < 6600; l++) sb.Append("<A>");
            sb.Append("<A/>");
            for (long l = 0; l < 6600; l++) sb.Append("</A>");
            XElement e = XElement.Parse(sb.ToString());
        }

        [Fact]
        public void EmptyCDataTextNodeIsNotPreservedInTheTree()
        {
            // The Empty CData text node is not preserved in the tree
            XDocument d = XDocument.Parse("<root><![CDATA[]]></root>");
            Assert.Equal(1, d.Element("root").Nodes().Count());
            Assert.IsType<XCData>(d.Root.FirstNode);
            Assert.Equal(string.Empty, (d.Root.FirstNode as XCData).Value);
        }

        [Fact]
        public void XDocumentToStringThrowsForXDocumentContainingOnlyWhitespaceNodes()
        {
            // XDocument.ToString() throw exception for the XDocument containing whitespace node only
            XDocument d = new XDocument();
            d.Add(" ");
            string s = d.ToString();
        }

        [Fact]
        public void NametableReturnsIncorrectXNamespace()
        {
            XNamespace ns = XNamespace.Get("h");
            Assert.NotSame(XNamespace.Xml, ns);
        }

        [Fact]
        public void XmlNamespaceSerialization()
        {
            // shouldn't throw
            XElement e = new XElement(
                "a",
                new XAttribute(XNamespace.Xmlns.GetName("ns"), "def"),
                new XElement(
                    "b",
                    new XAttribute(XNamespace.Xmlns.GetName("ns1"), "def"),
                    new XElement("{def}c", new XAttribute(XNamespace.Xmlns.GetName("ns1"), "abc"))));
        }

        [Theory]
        [MemberData(nameof(GetObjects))]
        public void CreatingXElementsFromNewDev10Types(object t, Type type)
        {
            XElement e = new XElement("e1", new XElement("e2"), "text1", new XElement("e3"), t);
            e.Add(t);
            e.FirstNode.ReplaceWith(t);

            XNode n = e.FirstNode.NextNode;
            n.AddBeforeSelf(t);
            n.AddAnnotation(t);
            n.ReplaceWith(t);

            e.FirstNode.AddAfterSelf(t);
            e.AddFirst(t);
            e.Annotation(type);
            e.Annotations(type);
            e.RemoveAnnotations(type);
            e.ReplaceAll(t);
            e.ReplaceAttributes(t);
            e.ReplaceNodes(t);
            e.SetAttributeValue("a", t);
            e.SetElementValue("e2", t);
            e.SetValue(t);

            XAttribute a = new XAttribute("a", t);
            XStreamingElement se = new XStreamingElement("se", t);
            se.Add(t);

            AssertExtensions.Throws<ArgumentException>(null, () => new XDocument(t));
            AssertExtensions.Throws<ArgumentException>(null, () => new XDocument(t));
        }

        public static IEnumerable<object[]> GetObjects()
        {
            var d = new Dictionary<int, string>();
            d.Add(7, "a");

            yield return new object[] { Tuple.Create(1, "Melitta", 7.5), typeof(Tuple) };
            yield return new object[] { new Guid(), typeof(Guid) };
            yield return new object[] { d, typeof(Dictionary<int, string>) };
        }
    }
}
