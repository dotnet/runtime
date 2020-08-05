// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Collections.Generic;
using System.Xml.XPath;
using StackNav = MS.Internal.Xml.XPath.ClonableStack<System.Xml.XPath.XPathNavigator>;

namespace MS.Internal.Xml.XPath
{
    internal sealed class FollSiblingQuery : BaseAxisQuery
    {
        private readonly StackNav _elementStk;
        private readonly List<XPathNavigator> _parentStk;
        private XPathNavigator? _nextInput;

        public FollSiblingQuery(Query qyInput, string name, string prefix, XPathNodeType type) : base(qyInput, name, prefix, type)
        {
            _elementStk = new StackNav();
            _parentStk = new List<XPathNavigator>();
        }
        private FollSiblingQuery(FollSiblingQuery other) : base(other)
        {
            _elementStk = other._elementStk.Clone();
            _parentStk = new List<XPathNavigator>(other._parentStk);
            _nextInput = Clone(other._nextInput);
        }

        public override void Reset()
        {
            _elementStk.Clear();
            _parentStk.Clear();
            _nextInput = null;
            base.Reset();
        }

        private bool Visited(XPathNavigator nav)
        {
            XPathNavigator parent = nav.Clone();
            parent.MoveToParent();
            for (int i = 0; i < _parentStk.Count; i++)
            {
                if (parent.IsSamePosition(_parentStk[i]))
                {
                    return true;
                }
            }
            _parentStk.Add(parent);
            return false;
        }

        private XPathNavigator? FetchInput()
        {
            XPathNavigator? input;
            do
            {
                input = qyInput.Advance();
                if (input == null)
                {
                    return null;
                }
            } while (Visited(input));
            return input.Clone();
        }

        public override XPathNavigator? Advance()
        {
            while (true)
            {
                if (currentNode == null)
                {
                    if (_nextInput == null)
                    {
                        _nextInput = FetchInput(); // This can happen at the beginning and at the end
                    }
                    if (_elementStk.Count == 0)
                    {
                        if (_nextInput == null)
                        {
                            return null;
                        }
                        currentNode = _nextInput;
                        _nextInput = FetchInput();
                    }
                    else
                    {
                        currentNode = _elementStk.Pop();
                    }
                }

                while (currentNode.IsDescendant(_nextInput))
                {
                    _elementStk.Push(currentNode);
                    currentNode = _nextInput!;
                    _nextInput = qyInput.Advance();
                    if (_nextInput != null)
                    {
                        _nextInput = _nextInput.Clone();
                    }
                }

                while (currentNode.MoveToNext())
                {
                    if (matches(currentNode))
                    {
                        position++;
                        return currentNode;
                    }
                }
                currentNode = null;
            }
        } // Advance

        public override XPathNodeIterator Clone() { return new FollSiblingQuery(this); }
    }
}
