// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Xml.Tests
{
    public class CreateTextNodeTests
    {
        [Fact]
        public static void NodeTypeTest()
        {
            var xmlDocument = new XmlDocument();
            var newNode = xmlDocument.CreateTextNode(String.Empty);

            Assert.Equal(XmlNodeType.Text, newNode.NodeType);
        }
    }
}
