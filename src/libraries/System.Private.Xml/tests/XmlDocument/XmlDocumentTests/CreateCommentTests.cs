// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Xml.Tests
{
    public class CreateCommentTests
    {
        [Fact]
        public static void CreateEmptyComment()
        {
            var xmlDocument = new XmlDocument();
            var comment = xmlDocument.CreateComment(string.Empty);

            Assert.Equal("<!---->", comment.OuterXml);
            Assert.Equal(string.Empty, comment.InnerText);
            Assert.Equal(XmlNodeType.Comment, comment.NodeType);
        }
    }
}
