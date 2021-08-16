// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using CoreXml.Test.XLinq;
using Xunit;

namespace System.Xml.Linq.xNodeReader.Tests
{
    public class XNodeReaderAttributeTests
    {
        [Fact]
        public void GetAttributeThrowsOnIndexMinusOne()
        {
            XmlReader dataReader = GetReaderFromXDocumentAndPositionOnElementOne();

            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader.GetAttribute(-1));
        }

        [Fact]
        public void GetAttributeThrowsOnIndexMinusTwo()
        {
            XmlReader dataReader = GetReaderFromXDocumentAndPositionOnElementOne();

            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader.GetAttribute(-2));
        }

        [Fact]
        public void IndexerThrowsOnIndexMinusOne()
        {
            XmlReader dataReader = GetReaderFromXDocumentAndPositionOnElementOne();

            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader[-1]);
        }

        [Fact]
        public void IndexerThrowsOnIndexMinusTwo()
        {
            XmlReader dataReader = GetReaderFromXDocumentAndPositionOnElementOne();

            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader[-2]);
        }

        [Fact]
        public void GetAttributeThrowsOnAttributeCount()
        {
            XmlReader dataReader = GetReaderFromXDocumentAndPositionOnElementZero();

            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader.GetAttribute(dataReader.AttributeCount));
        }

        [Fact]
        public void GetAttributeThrowsOnAttributeCountPlusOne()
        {
            XmlReader dataReader = GetReaderFromXDocumentAndPositionOnElementOne();

            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader.GetAttribute(dataReader.AttributeCount + 1));
        }

        [Fact]
        public void IndexerThrowsOnAttributeCount()
        {
            XmlReader dataReader = GetReaderFromXDocumentAndPositionOnElementZero();

            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader[dataReader.AttributeCount]);
        }

        [Fact]
        public void IndexerThrowsOnAttributeCountPlusOne()
        {
            XmlReader dataReader = GetReaderFromXDocumentAndPositionOnElementOne();

            Assert.Throws<ArgumentOutOfRangeException>(() => dataReader[dataReader.AttributeCount + 1]);
        }

        [Fact]
        public void IndexerThrowsOnNegativeIndicesOnXNodes()
        {
            foreach (XNode n in GetXNodeTypes())
            {
                using XmlReader r = n.CreateReader();

                r.Read();

                Assert.Throws<ArgumentOutOfRangeException>(() => r[-100000]);
                Assert.Throws<ArgumentOutOfRangeException>(() => r[-1]);
            }
        }

        [Fact]
        public void GetAttributeThrowsOnNegativeIndicesOnXNodes()
        {
            foreach (XNode n in GetXNodeTypes())
            {
                using XmlReader r = n.CreateReader();

                r.Read();

                Assert.Throws<ArgumentOutOfRangeException>(() => r.GetAttribute(-100000));
                Assert.Throws<ArgumentOutOfRangeException>(() => r.GetAttribute(-1));
            }
        }

        [Fact]
        public void GetAttributeIntThrowsInNonInteractiveMode()
        {
            foreach (XNode n in GetXNodeTypes())
            {
                using XmlReader r = n.CreateReader();

                Assert.Throws<InvalidOperationException>(() => r.GetAttribute(0));
                Assert.Throws<InvalidOperationException>(() => r.GetAttribute(100000));
            }
        }

        [Fact]
        public void GetAttributeStringReturnsNullInNonInteractiveMode()
        {
            foreach (XNode n in GetXNodeTypes())
            {
                using XmlReader r = n.CreateReader();

                Assert.Null(r.GetAttribute(null));
                Assert.Null(r.GetAttribute(null, null));
                Assert.Null(r.GetAttribute(""));
                Assert.Null(r.GetAttribute("", ""));
            }
        }

        [Fact]
        public void IndexerThrowsOnNonInteractiveMode()
        {
            foreach (XNode n in GetXNodeTypes())
            {
                using XmlReader r = n.CreateReader();

                Assert.Throws<InvalidOperationException>(() => r[0]);
            }
        }

        [Fact]
        public void GetAttributeThrowsOnOutOfRangeUpperBound()
        {
            var xElement = new XElement("element");
            xElement.SetAttributeValue("attr", "val");

            using XmlReader r = xElement.CreateReader();
            r.Read();

            Assert.Throws<ArgumentOutOfRangeException>(() => r.GetAttribute(1));
        }

        [Fact]
        public void IndexerThrowsOnOutOfRangeUpperBound()
        {
            var xElement = new XElement("element");
            xElement.SetAttributeValue("attr", "val");

            using XmlReader r = xElement.CreateReader();
            r.Read();

            Assert.Throws<ArgumentOutOfRangeException>(() => r[1]);
        }

        private static XmlReader GetReaderFromXDocumentAndPositionOnElementOne()
        {
            var bridgeHelpers = new BridgeHelpers();
            var dataReader = bridgeHelpers.GetReader();
            bridgeHelpers.PositionOnElement(dataReader, "ACT1");
            return dataReader;
        }

        private static XmlReader GetReaderFromXDocumentAndPositionOnElementZero()
        {
            var bridgeHelpers = new BridgeHelpers();
            var dataReader = bridgeHelpers.GetReader();
            bridgeHelpers.PositionOnElement(dataReader, "ACT0");
            return dataReader;
        }

        private static IEnumerable<XNode> GetXNodeTypes()
        {
            var xNode = new List<XNode>
            {
                new XDocument(new XDocumentType("root", "", "", "<!ELEMENT root ANY>"), new XElement("root")),
                new XElement("elem1"),
                new XText("text1"),
                new XComment("comment1"),
                new XProcessingInstruction("pi1", "pi1pi1pi1pi1pi1"),
                new XCData("cdata cdata"),
                new XDocumentType("dtd1", "dtd1dtd1dtd1", "dtd1dtd1", "dtd1dtd1dtd1dtd1")
            };

            return xNode;
        }
    }
}
