// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlDocumentTests
{
    public class CreateDocumentFragment
    {
        [Fact]
        public static void CheckNodeType()
        {
            var xmlDocument = new XmlDocument();
            var documentFragment = xmlDocument.CreateDocumentFragment();

            Assert.Equal(XmlNodeType.DocumentFragment, documentFragment.NodeType);
        }
    }
}
