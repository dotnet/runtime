// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Xunit;

namespace XDocumentTests.SDMSample
{
    public class SDM_CDATA
    {
        /// <summary>
        /// Tests the XCData constructor that takes a value.
        /// </summary>
        [Fact]
        public void CreateTextSimple()
        {
            Assert.Throws<ArgumentNullException>(() => new XCData((string)null));

            XCData c = new XCData("foo");
            Assert.Equal("foo", c.Value);
            Assert.Null(c.Parent);
        }

        /// <summary>
        /// Tests the XText constructor that operated from an XmlReader.
        /// </summary>
        [Fact]
        public void CreateTextFromReader()
        {
            TextReader textReader = new StringReader("<x><![CDATA[12345678]]></x>");
            XmlReader xmlReader = XmlReader.Create(textReader);
            // Advance to the CData and construct.
            xmlReader.Read();
            xmlReader.Read();
            XCData c = (XCData)XNode.ReadFrom(xmlReader);

            Assert.Equal("12345678", c.Value);
        }

        /// <summary>
        /// Validates the behavior of the Equals overload on XText.
        /// </summary>
        [Fact]
        public void TextEquals()
        {
            XCData c1 = new XCData("xxx");
            XCData c2 = new XCData("xxx");
            XCData c3 = new XCData("yyy");

            Assert.False(c1.Equals(null));
            Assert.False(c1.Equals("foo"));
            Assert.True(c1.Equals(c1));
            Assert.False(c1.Equals(c2));
            Assert.False(c1.Equals(c3));
        }

        /// <summary>
        /// Validates the behavior of the DeepEquals overload on XText.
        /// </summary>
        [Fact]
        public void DeepEquals()
        {
            XCData c1 = new XCData("xxx");
            XCData c2 = new XCData("xxx");
            XCData c3 = new XCData("yyy");

            Assert.False(XNode.DeepEquals(c1, (XText)null));
            Assert.True(XNode.DeepEquals(c1, c1));
            Assert.True(XNode.DeepEquals(c1, c2));
            Assert.False(XNode.DeepEquals(c1, c3));

            Assert.Equal(XNode.EqualityComparer.GetHashCode(c1), XNode.EqualityComparer.GetHashCode(c2));
        }

        /// <summary>
        /// Validates the behavior of the Value property on XText.
        /// </summary>
        [Fact]
        public void TextValue()
        {
            XCData c = new XCData("xxx");
            Assert.Equal("xxx", c.Value);

            // Null value not allowed.
            Assert.Throws<ArgumentNullException>(() => c.Value = null);

            // Try setting a value.
            c.Value = "abcd";
            Assert.Equal("abcd", c.Value);
        }

        /// <summary>
        /// Tests the WriteTo method on XTest.
        /// </summary>
        [Fact]
        public void TextWriteTo()
        {
            XCData c = new XCData("abcd");

            // Null writer not allowed.
            Assert.Throws<ArgumentNullException>(() => c.WriteTo(null));

            // Test.
            StringBuilder stringBuilder = new StringBuilder();
            XmlWriter xmlWriter = XmlWriter.Create(stringBuilder);

            xmlWriter.WriteStartElement("x");
            c.WriteTo(xmlWriter);
            xmlWriter.WriteEndElement();

            xmlWriter.Flush();

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-16\"?><x><![CDATA[abcd]]></x>", stringBuilder.ToString());
        }
    }
}
