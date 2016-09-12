// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class ChildrenQuery : BaseAxisQuery
    {
        private XPathNodeIterator _iterator = XPathEmptyIterator.Instance;

        public ChildrenQuery(Query qyInput, string name, string prefix, XPathNodeType type) : base(qyInput, name, prefix, type) { }
        protected ChildrenQuery(ChildrenQuery other) : base(other)
        {
            _iterator = Clone(other._iterator);
        }

        public override void Reset()
        {
            _iterator = XPathEmptyIterator.Instance;
            base.Reset();
        }

        public override XPathNavigator Advance()
        {
            while (!_iterator.MoveNext())
            {
                XPathNavigator input = qyInput.Advance();
                if (input == null)
                {
                    return null;
                }
                if (NameTest)
                {
                    if (TypeTest == XPathNodeType.ProcessingInstruction)
                    {
                        _iterator = new IteratorFilter(input.SelectChildren(TypeTest), Name);
                    }
                    else
                    {
                        _iterator = input.SelectChildren(Name, Namespace);
                    }
                }
                else
                {
                    _iterator = input.SelectChildren(TypeTest);
                }
                position = 0;
            }
            position++;
            currentNode = _iterator.Current;
            return currentNode;
        } // Advance

        public sealed override XPathNavigator MatchNode(XPathNavigator context)
        {
            if (context != null)
            {
                if (matches(context))
                {
                    XPathNavigator temp = context.Clone();
                    if (temp.NodeType != XPathNodeType.Attribute && temp.MoveToParent())
                    {
                        return qyInput.MatchNode(temp);
                    }
                    return null;
                }
            }
            return null;
        }

        public override XPathNodeIterator Clone() { return new ChildrenQuery(this); }
    }
}
