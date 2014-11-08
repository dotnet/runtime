// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class ChildrenQuery : BaseAxisQuery
    {
        XPathNodeIterator iterator = XPathEmptyIterator.Instance;

        public ChildrenQuery(Query qyInput, string name, string prefix, XPathNodeType type) : base(qyInput, name, prefix, type) { }
        protected ChildrenQuery(ChildrenQuery other) : base(other)
        {
            this.iterator = Clone(other.iterator);
        }

        public override void Reset()
        {
            iterator = XPathEmptyIterator.Instance;
            base.Reset();
        }

        public override XPathNavigator Advance()
        {
            while (!iterator.MoveNext())
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
                        iterator = new IteratorFilter(input.SelectChildren(TypeTest), Name);
                    }
                    else
                    {
                        iterator = input.SelectChildren(Name, Namespace);
                    }
                }
                else
                {
                    iterator = input.SelectChildren(TypeTest);
                }
                position = 0;
            }
            position++;
            currentNode = iterator.Current;
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
