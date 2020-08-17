// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class FollowingQuery : BaseAxisQuery
    {
        private XPathNavigator? _input;
        private XPathNodeIterator? _iterator;

        public FollowingQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest) : base(qyInput, name, prefix, typeTest) { }
        private FollowingQuery(FollowingQuery other) : base(other)
        {
            _input = Clone(other._input);
            _iterator = Clone(other._iterator);
        }

        public override void Reset()
        {
            _iterator = null;
            base.Reset();
        }

        public override XPathNavigator? Advance()
        {
            if (_iterator == null)
            {
                _input = qyInput.Advance();
                if (_input == null)
                {
                    return null;
                }

                XPathNavigator prev;
                do
                {
                    prev = _input.Clone();
                    _input = qyInput.Advance();
                } while (prev.IsDescendant(_input));
                _input = prev;

                _iterator = XPathEmptyIterator.Instance;
            }

            while (!_iterator.MoveNext())
            {
                bool matchSelf;
                if (_input!.NodeType == XPathNodeType.Attribute || _input.NodeType == XPathNodeType.Namespace)
                {
                    _input.MoveToParent();
                    matchSelf = false;
                }
                else
                {
                    while (!_input.MoveToNext())
                    {
                        if (!_input.MoveToParent())
                        {
                            return null;
                        }
                    }
                    matchSelf = true;
                }
                if (NameTest)
                {
                    _iterator = _input.SelectDescendants(Name, Namespace!, matchSelf);
                }
                else
                {
                    _iterator = _input.SelectDescendants(TypeTest, matchSelf);
                }
            }
            position++;
            currentNode = _iterator.Current;
            return currentNode;
        }

        public override XPathNodeIterator Clone() { return new FollowingQuery(this); }
    }
}
