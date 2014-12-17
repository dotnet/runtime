// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace System.Xml.XPath
{
    public static class XDocumentExtensions
    {
        private class XDocumentNavigable : IXPathNavigable
        {
            private XNode node;
            public XDocumentNavigable(XNode n)
            {
                this.node = n;
            }
            public XPathNavigator CreateNavigator()
            {
                return node.CreateNavigator();
            }
        }
        public static IXPathNavigable ToXPathNavigable(this XNode node)
        {
            return new XDocumentNavigable(node);
        }
    }
}
