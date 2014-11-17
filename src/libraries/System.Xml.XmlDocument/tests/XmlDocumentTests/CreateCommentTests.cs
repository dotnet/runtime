// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XmlDocumentTests.XmlDocumentTests
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
