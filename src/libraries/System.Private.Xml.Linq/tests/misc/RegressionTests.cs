// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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

        // When a reader delivers a single logical text value as many small text nodes (as the
        // reader used by DataContractSerializer over a stream does), loading must remain linear
        // and still coalesce the chunks into a single, correct text value rather than
        // concatenating them one at a time.
        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public void LoadCoalescesChunkedTextIntoSingleNode(int chunkSize)
        {
            var original = new XElement("root", string.Join("\n", Enumerable.Repeat(new string('a', 40), 100)));
            string xml = original.ToString(SaveOptions.DisableFormatting);

            XElement loaded = LoadChunked(xml, chunkSize, LoadOptions.None);

            Assert.Equal(original.Value, loaded.Value);
            XText textNode = Assert.IsType<XText>(Assert.Single(loaded.Nodes()));
            Assert.Equal(original.Value, textNode.Value);
        }

        [Fact]
        public void LoadSingleTextNodeReusesReaderValue()
        {
            const string xml = "<root>single text value</root>";
            using var reader = new ChunkingXmlReader(XmlReader.Create(new StringReader(xml)), int.MaxValue);

            XElement loaded = XElement.Load(reader, LoadOptions.None);

            XText textNode = Assert.IsType<XText>(Assert.Single(loaded.Nodes()));
            Assert.Same(reader.LastTextValue, textNode.Value);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void LoadChunkedTextPreservesMixedContent(int chunkSize)
        {
            const string xml = "<root>hello world<child>inner text</child>trailing text</root>";

            XElement loaded = LoadChunked(xml, chunkSize, LoadOptions.None);

            Assert.Equal(xml, loaded.ToString(SaveOptions.DisableFormatting));
            Assert.Equal("hello world", ((XText)loaded.FirstNode).Value);
            Assert.Equal("inner text", loaded.Element("child").Value);
            Assert.Equal("trailing text", ((XText)loaded.LastNode).Value);
        }

        [Fact]
        public void LoadChunkedTextWithBaseUriOptionCoalesces()
        {
            // LoadOptions.SetBaseUri routes through the ContentReader "container" code path.
            var original = new XElement("root", string.Join("\n", Enumerable.Repeat(new string('b', 30), 80)));
            string xml = original.ToString(SaveOptions.DisableFormatting);

            XElement loaded = LoadChunked(xml, chunkSize: 1, LoadOptions.SetBaseUri);

            Assert.Equal(original.Value, loaded.Value);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public async Task LoadAsyncCoalescesChunkedText(int chunkSize)
        {
            var original = new XElement("root", string.Join("\n", Enumerable.Repeat(new string('c', 40), 100)));
            string xml = original.ToString(SaveOptions.DisableFormatting);

            using var reader = new ChunkingXmlReader(XmlReader.Create(new StringReader(xml)), chunkSize);
            XElement loaded = await XElement.LoadAsync(reader, LoadOptions.None, default);

            Assert.Equal(original.Value, loaded.Value);
            Assert.Equal(original.Value, Assert.IsType<XText>(Assert.Single(loaded.Nodes())).Value);
        }

        private static XElement LoadChunked(string xml, int chunkSize, LoadOptions options)
        {
            using var reader = new ChunkingXmlReader(XmlReader.Create(new StringReader(xml)), chunkSize);
            return XElement.Load(reader, options);
        }

        // With buffering, loading a heavily-chunked text value is O(n). Reverting to per-chunk
        // string concatenation makes it O(n^2), which would take minutes for this input instead
        // of well under a second, so the generous time bound only trips on an algorithmic
        // regression, not on normal machine-speed variance.
        [Fact]
        public void LoadChunkedLargeTextRemainsLinear()
        {
            const int length = 750_000;
            string xml = "<root>" + new string('a', length) + "</root>";

            var stopwatch = Stopwatch.StartNew();
            XElement loaded = LoadChunked(xml, chunkSize: 1, LoadOptions.None);
            stopwatch.Stop();

            Assert.Equal(length, loaded.Value.Length);
            Assert.True(
                stopwatch.Elapsed < TimeSpan.FromSeconds(30),
                $"Chunked load took {stopwatch.Elapsed.TotalSeconds:F1}s; expected linear-time completion.");
        }

        // Wraps an XmlReader and reports each text node's value in fixed-size chunks, emulating
        // readers that surface large text content as many small text nodes.
        private sealed class ChunkingXmlReader : XmlReader
        {
            private readonly XmlReader _inner;
            private readonly int _chunkSize;
            private string? _pendingText;
            private int _position;

            public string? LastTextValue { get; private set; }

            public ChunkingXmlReader(XmlReader inner, int chunkSize)
            {
                _inner = inner;
                _chunkSize = chunkSize;
            }

            public override bool Read()
            {
                if (_pendingText != null)
                {
                    _position += _chunkSize;
                    if (_position < _pendingText.Length)
                    {
                        return true;
                    }

                    _pendingText = null;
                    _position = 0;
                }

                if (!_inner.Read())
                {
                    return false;
                }

                if (_inner.NodeType == XmlNodeType.Text && _inner.Value.Length > _chunkSize)
                {
                    _pendingText = _inner.Value;
                    _position = 0;
                }

                return true;
            }

            public override Task<bool> ReadAsync() => Task.FromResult(Read());

            public override Task<string> GetValueAsync() => Task.FromResult(Value);

            public override XmlNodeType NodeType => _pendingText != null ? XmlNodeType.Text : _inner.NodeType;

            public override string Value
            {
                get
                {
                    string value;
                    if (_pendingText != null)
                    {
                        value = _pendingText.Substring(_position, Math.Min(_chunkSize, _pendingText.Length - _position));
                    }
                    else
                    {
                        value = _inner.Value;
                    }

                    if (NodeType == XmlNodeType.Text)
                    {
                        LastTextValue = value;
                    }

                    return value;
                }
            }

            public override int AttributeCount => _inner.AttributeCount;
            public override string BaseURI => _inner.BaseURI;
            public override int Depth => _inner.Depth;
            public override bool EOF => _inner.EOF;
            public override bool IsEmptyElement => _inner.IsEmptyElement;
            public override string LocalName => _inner.LocalName;
            public override string NamespaceURI => _inner.NamespaceURI;
            public override XmlNameTable NameTable => _inner.NameTable;
            public override string Prefix => _inner.Prefix;
            public override ReadState ReadState => _inner.ReadState;

            public override string GetAttribute(int i) => _inner.GetAttribute(i);
            public override string? GetAttribute(string name) => _inner.GetAttribute(name);
            public override string? GetAttribute(string name, string? namespaceURI) => _inner.GetAttribute(name, namespaceURI);
            public override string? LookupNamespace(string prefix) => _inner.LookupNamespace(prefix);
            public override bool MoveToAttribute(string name) => _inner.MoveToAttribute(name);
            public override bool MoveToAttribute(string name, string? ns) => _inner.MoveToAttribute(name, ns);
            public override bool MoveToElement() => _inner.MoveToElement();
            public override bool MoveToFirstAttribute() => _inner.MoveToFirstAttribute();
            public override bool MoveToNextAttribute() => _inner.MoveToNextAttribute();
            public override bool ReadAttributeValue() => _inner.ReadAttributeValue();
            public override void ResolveEntity() => _inner.ResolveEntity();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
