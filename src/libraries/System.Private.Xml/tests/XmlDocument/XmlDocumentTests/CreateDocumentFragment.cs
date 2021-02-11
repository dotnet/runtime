// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Xml.Tests
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
