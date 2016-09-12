// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class CreateCommentTests
    {
        [Fact]
        public static void CreateEmptyComment()
        {
            var xmlDocument = new XmlDocument();
            var comment = xmlDocument.CreateComment(String.Empty);

            Assert.Equal("<!---->", comment.OuterXml);
            Assert.Equal(String.Empty, comment.InnerText);
            Assert.Equal(comment.NodeType, XmlNodeType.Comment);
        }
    }
}
