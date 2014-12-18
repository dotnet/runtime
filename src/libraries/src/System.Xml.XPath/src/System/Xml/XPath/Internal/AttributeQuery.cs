// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class AttributeQuery : BaseAxisQuery
    {
        private bool _onAttribute = false;

        public AttributeQuery(Query qyParent, string Name, string Prefix, XPathNodeType Type) : base(qyParent, Name, Prefix, Type) { }
        private AttributeQuery(AttributeQuery other) : base(other)
        {
            _onAttribute = other._onAttribute;
        }
        public override void Reset()
        {
            _onAttribute = false;
            base.Reset();
        }

        public override XPathNavigator Advance()
        {
            while (true)
            {
                if (!_onAttribute)
                {
                    currentNode = qyInput.Advance();
                    if (currentNode == null)
                    {
                        return null;
                    }
                    position = 0;
                    currentNode = currentNode.Clone();
                    _onAttribute = currentNode.MoveToFirstAttribute();
                }
                else
                {
                    _onAttribute = currentNode.MoveToNextAttribute();
                }

                if (_onAttribute)
                {
                    Debug.Assert(!currentNode.NamespaceURI.Equals(XmlConst.ReservedNsXmlNs));
                    if (matches(currentNode))
                    {
                        position++;
                        return currentNode;
                    }
                }
            } // while
        }

        public override XPathNavigator MatchNode(XPathNavigator context)
        {
            if (context != null)
            {
                if (context.NodeType == XPathNodeType.Attribute && matches(context))
                {
                    XPathNavigator temp = context.Clone();
                    if (temp.MoveToParent())
                    {
                        return qyInput.MatchNode(temp);
                    }
                }
            }
            return null;
        }

        public override XPathNodeIterator Clone() { return new AttributeQuery(this); }
    }
}
