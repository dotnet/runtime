// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Linq;

namespace System.Xml.XPath
{
    public static class XDocumentExtensions
    {
        private class XDocumentNavigable : IXPathNavigable
        {
            private XNode _node;
            public XDocumentNavigable(XNode n)
            {
                _node = n;
            }
            public XPathNavigator CreateNavigator()
            {
                return _node.CreateNavigator();
            }
        }
        public static IXPathNavigable ToXPathNavigable(this XNode node)
        {
            return new XDocumentNavigable(node);
        }
    }
}
