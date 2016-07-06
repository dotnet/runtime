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
    public class SDM__PI
    {
        /// <summary>
        /// Tests the ProcessingInstruction constructor that takes a value.
        /// </summary>
        [Fact]
        public void CreateProcessingInstructionSimple()
        {
            Assert.Throws<ArgumentNullException>(() => new XProcessingInstruction(null, "abcd"));
            Assert.Throws<ArgumentNullException>(() => new XProcessingInstruction("abcd", null));

            XProcessingInstruction c = new XProcessingInstruction("foo", "bar");
            Assert.Equal("foo", c.Target);
            Assert.Equal("bar", c.Data);
            Assert.Null(c.Parent);
        }

        /// <summary>
        /// Tests the ProcessingInstruction constructor that operated from an XmlReader.
        /// </summary>
        [Fact]
        public void CreateProcessingInstructionFromReader()
        {
            TextReader textReader = new StringReader("<x><?target data?></x>");
            XmlReader xmlReader = XmlReader.Create(textReader);
            // Advance to the processing instruction and construct.
            xmlReader.Read();
            xmlReader.Read();
            XProcessingInstruction c = (XProcessingInstruction)XNode.ReadFrom(xmlReader);

            Assert.Equal("target", c.Target);
            Assert.Equal("data", c.Data);
        }

        /// <summary>
        /// Validates the behavior of the Equals overload on XProcessingInstruction.
        /// </summary>
        [Fact]
        public void ProcessingInstructionEquals()
        {
            XProcessingInstruction c1 = new XProcessingInstruction("targetx", "datax");
            XProcessingInstruction c2 = new XProcessingInstruction("targetx", "datay");
            XProcessingInstruction c3 = new XProcessingInstruction("targety", "datax");
            XProcessingInstruction c4 = new XProcessingInstruction("targety", "datay");
            XProcessingInstruction c5 = new XProcessingInstruction("targetx", "datax");

            Assert.False(XNode.DeepEquals(c1, (XProcessingInstruction)null));
            Assert.True(XNode.DeepEquals(c1, c1));
            Assert.False(XNode.DeepEquals(c1, c2));
            Assert.False(XNode.DeepEquals(c1, c3));
            Assert.False(XNode.DeepEquals(c1, c4));
            Assert.True(XNode.DeepEquals(c1, c5));

            Assert.Equal(XNode.EqualityComparer.GetHashCode(c1), XNode.EqualityComparer.GetHashCode(c5));
        }

        /// <summary>
        /// Validates the behavior of the Target and Data properties on XProcessingInstruction.
        /// </summary>
        [Fact]
        public void ProcessingInstructionValues()
        {
            XProcessingInstruction c = new XProcessingInstruction("xxx", "yyy");
            Assert.Equal("xxx", c.Target);
            Assert.Equal("yyy", c.Data);

            // Null values not allowed.
            Assert.Throws<ArgumentNullException>(() => c.Target = null);
            Assert.Throws<ArgumentNullException>(() => c.Data = null);

            // Try setting values.
            c.Target = "abcd";
            Assert.Equal("abcd", c.Target);

            c.Data = "efgh";
            Assert.Equal("efgh", c.Data);
            Assert.Equal("abcd", c.Target);
        }

        /// <summary>
        /// Tests the WriteTo method on XComment.
        /// </summary>
        [Fact]
        public void ProcessingInstructionWriteTo()
        {
            XProcessingInstruction c = new XProcessingInstruction("target", "data");

            // Null writer not allowed.
            Assert.Throws<ArgumentNullException>(() => c.WriteTo(null));

            // Test.
            StringBuilder stringBuilder = new StringBuilder();
            XmlWriter xmlWriter = XmlWriter.Create(stringBuilder);

            xmlWriter.WriteStartElement("x");
            c.WriteTo(xmlWriter);
            xmlWriter.WriteEndElement();

            xmlWriter.Flush();

            Assert.Equal(
                "<?xml version=\"1.0\" encoding=\"utf-16\"?><x><?target data?></x>",
                stringBuilder.ToString());
        }
    }
}
