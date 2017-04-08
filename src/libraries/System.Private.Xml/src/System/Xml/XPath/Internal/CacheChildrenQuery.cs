// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using StackInt = MS.Internal.Xml.XPath.ClonableStack<int>;
using StackNav = MS.Internal.Xml.XPath.ClonableStack<System.Xml.XPath.XPathNavigator>;

namespace MS.Internal.Xml.XPath
{
    // This class implements Children axis on Ancestor & Descendant inputs. (as well as id(), preceding, following)
    // The problem here is that is descendant::*/child::* and ancestor::*/child::* can produce duplicates nodes
    // The algorithm heavily uses the fact that in our implementation of both AncestorQuery and DescendantQuery return nodes in document order. 
    // As result first child is always before or equal of next input. 
    // So we don't need to call DecideNextNode() when needInput == true && stack is empty.
    internal sealed class CacheChildrenQuery : ChildrenQuery
    {
        private XPathNavigator _nextInput = null;
        private StackNav _elementStk;
        private StackInt _positionStk;
        private bool _needInput;
#if DEBUG
        private XPathNavigator _lastNode = null;
#endif

        public CacheChildrenQuery(Query qyInput, string name, string prefix, XPathNodeType type) : base(qyInput, name, prefix, type)
        {
            _elementStk = new StackNav();
            _positionStk = new StackInt();
            _needInput = true;
        }
        private CacheChildrenQuery(CacheChildrenQuery other) : base(other)
        {
            _nextInput = Clone(other._nextInput);
            _elementStk = other._elementStk.Clone();
            _positionStk = other._positionStk.Clone();
            _needInput = other._needInput;
#if DEBUG
            _lastNode = Clone(other._lastNode);
#endif
        }

        public override void Reset()
        {
            _nextInput = null;
            _elementStk.Clear();
            _positionStk.Clear();
            _needInput = true;
            base.Reset();
#if DEBUG
            _lastNode = null;
#endif
        }

        public override XPathNavigator Advance()
        {
            do
            {
                if (_needInput)
                {
                    if (_elementStk.Count == 0)
                    {
                        currentNode = GetNextInput();
                        if (currentNode == null)
                        {
                            return null;
                        }
                        if (!currentNode.MoveToFirstChild())
                        {
                            continue;
                        }
                        position = 0;
                    }
                    else
                    {
                        currentNode = _elementStk.Pop();
                        position = _positionStk.Pop();
                        if (!DecideNextNode())
                        {
                            continue;
                        }
                    }
                    _needInput = false;
                }
                else
                {
                    if (!currentNode.MoveToNext() || !DecideNextNode())
                    {
                        _needInput = true;
                        continue;
                    }
                }
                if (matches(currentNode))
                {
                    position++;
                    return currentNode;
                }
            } while (true);
        } // Advance

        private bool DecideNextNode()
        {
            _nextInput = GetNextInput();
            if (_nextInput != null)
            {
                if (CompareNodes(currentNode, _nextInput) == XmlNodeOrder.After)
                {
                    _elementStk.Push(currentNode);
                    _positionStk.Push(position);
                    currentNode = _nextInput;
                    _nextInput = null;
                    if (!currentNode.MoveToFirstChild())
                    {
                        return false;
                    }
                    position = 0;
                }
            }
            return true;
        }

        private XPathNavigator GetNextInput()
        {
            XPathNavigator result;
            if (_nextInput != null)
            {
                result = _nextInput;
                _nextInput = null;
            }
            else
            {
                result = qyInput.Advance();
                if (result != null)
                {
                    result = result.Clone();
                }
            }
            return result;
        }

        public override XPathNodeIterator Clone() { return new CacheChildrenQuery(this); }
    } // Children Query}
}
