// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class XPathSelfQuery : BaseAxisQuery
    {
        public XPathSelfQuery(Query qyInput, string Name, string Prefix, XPathNodeType Type) : base(qyInput, Name, Prefix, Type) { }
        private XPathSelfQuery(XPathSelfQuery other) : base(other) { }

        public override XPathNavigator Advance()
        {
            while ((currentNode = qyInput.Advance()) != null)
            {
                if (matches(currentNode))
                {
                    position = 1;
                    return currentNode;
                }
            }
            return null;
        }

        public override XPathNodeIterator Clone() { return new XPathSelfQuery(this); }
    }
}
