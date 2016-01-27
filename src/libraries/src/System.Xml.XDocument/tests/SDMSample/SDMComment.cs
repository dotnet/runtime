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
    public class SDM_Comment
    {
        /// <summary>
        /// Tests the Comment constructor that takes a value.
        /// </summary>
        [Fact]
        public void CreateCommentSimple()
        {
            Assert.Throws<ArgumentNullException>(() => new XComment((string)null));

            XComment c = new XComment("foo");
            Assert.Equal("foo", c.Value);
            Assert.Null(c.Parent);
        }

        /// <summary>
        /// Tests the Comment constructor that operated from an XmlReader.
        /// </summary>
        [Fact]
        public void CreateCommentFromReader()
        {
            TextReader textReader = new StringReader("<x><!-- 12345678 --></x>");
            XmlReader xmlReader = XmlReader.Create(textReader);
            // Advance to the Comment and construct.
            xmlReader.Read();
            xmlReader.Read();
            XComment c = (XComment)XNode.ReadFrom(xmlReader);

            Assert.Equal(" 12345678 ", c.Value);
        }

        /// <summary>
        /// Validates the behavior of the Equals overload on XComment.
        /// </summary>
        [Fact]
        public void CommentEquals()
        {
            XComment c1 = new XComment("xxx");
            XComment c2 = new XComment("xxx");
            XComment c3 = new XComment("yyy");

            Assert.False(c1.Equals(null));
            Assert.False(c1.Equals("foo"));
            Assert.True(c1.Equals(c1));
            Assert.False(c1.Equals(c2));
            Assert.False(c1.Equals(c3));
        }

        /// <summary>
        /// Validates the behavior of the DeepEquals overload on XComment.
        /// </summary>
        [Fact]
        public void CommentDeepEquals()
        {
            XComment c1 = new XComment("xxx");
            XComment c2 = new XComment("xxx");
            XComment c3 = new XComment("yyy");

            Assert.False(XNode.DeepEquals(c1, (XComment)null));
            Assert.True(XNode.DeepEquals(c1, c1));
            Assert.True(XNode.DeepEquals(c1, c2));
            Assert.False(XNode.DeepEquals(c1, c3));

            Assert.Equal(XNode.EqualityComparer.GetHashCode(c1), XNode.EqualityComparer.GetHashCode(c2));
        }


        /// <summary>
        /// Validates the behavior of the Value property on XComment.
        /// </summary>
        [Fact]
        public void CommentValue()
        {
            XComment c = new XComment("xxx");
            Assert.Equal("xxx", c.Value);

            // Null value not allowed.
            Assert.Throws<ArgumentNullException>(() => c.Value = null);

            // Try setting a value.
            c.Value = "abcd";
            Assert.Equal("abcd", c.Value);
        }

        /// <summary>
        /// Tests the WriteTo method on XComment.
        /// </summary>
        [Fact]
        public void CommentWriteTo()
        {
            XComment c = new XComment("abcd ");

            // Null writer not allowed.
            Assert.Throws<ArgumentNullException>(() => c.WriteTo(null));

            // Test.
            StringBuilder stringBuilder = new StringBuilder();
            XmlWriter xmlWriter = XmlWriter.Create(stringBuilder);

            xmlWriter.WriteStartElement("x");
            c.WriteTo(xmlWriter);
            xmlWriter.WriteEndElement();

            xmlWriter.Flush();

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-16\"?><x><!--abcd --></x>", stringBuilder.ToString());
        }
    }
}
