// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    // Tracks the parents of the input nodes of the following-sibling and preceding-sibling axes,
    // so that the siblings under each parent are only produced once.
    //
    // A list of every parent seen so far has to be scanned for each input node, which is quadratic.
    // When the input nodes arrive in document order (or in reverse document order for preceding-sibling),
    // a parent that was seen before can only be seen again while the input stays within its subtree,
    // so it is enough to keep the chain of parents that are ancestors of the current one and to compare
    // against the innermost of them. If the input turns out not to be ordered, we fall back to the full list.
    internal sealed class SiblingInputParents
    {
        private readonly bool _reverse;
        private readonly List<XPathNavigator> _parents;
        private readonly List<XPathNavigator> _ancestors;
        private XPathNavigator? _lastInput; // kept across Clear() so that it can be reused
        private bool _hasLastInput;
        private bool _unordered;

        public SiblingInputParents(bool reverse)
        {
            _reverse = reverse;
            _parents = new List<XPathNavigator>();
            _ancestors = new List<XPathNavigator>();
        }

        private SiblingInputParents(SiblingInputParents other)
        {
            _reverse = other._reverse;
            _parents = new List<XPathNavigator>(other._parents);
            _ancestors = new List<XPathNavigator>(other._ancestors);
            _hasLastInput = other._hasLastInput;
            _lastInput = other._hasLastInput ? other._lastInput!.Clone() : null;
            _unordered = other._unordered;
        }

        public SiblingInputParents Clone() => new SiblingInputParents(this);

        public void Clear()
        {
            _parents.Clear();
            _ancestors.Clear();
            _hasLastInput = false;
            _unordered = false;
        }

        // True while the input nodes have arrived in order.
        public bool IsInputOrdered => !_unordered;

        // Records the parent of the input node; returns false if it has already been recorded.
        public bool TryAdd(XPathNavigator input)
        {
            XPathNavigator parent = input.Clone();
            parent.MoveToParent();

            if (!_unordered)
            {
                // Most often the input node shares its parent with the previous one
                if (_ancestors.Count > 0 && _ancestors[_ancestors.Count - 1].IsSamePosition(parent))
                {
                    return false;
                }

                if (_hasLastInput && !IsInOrder(_lastInput!, input))
                {
                    _unordered = true;
                }
                else
                {
                    if (_lastInput == null || !_lastInput.MoveTo(input))
                    {
                        _lastInput = input.Clone();
                    }
                    _hasLastInput = true;
                }
            }

            if (_unordered)
            {
                for (int i = 0; i < _parents.Count; i++)
                {
                    if (parent.IsSamePosition(_parents[i]))
                    {
                        return false;
                    }
                }
            }
            else
            {
                // Each parent in _ancestors is an ancestor of the one after it. Drop the ones that are not
                // an ancestor of this parent: the input has left their subtree and cannot come back to it.
                while (_ancestors.Count > 0)
                {
                    XPathNavigator innermost = _ancestors[_ancestors.Count - 1];
                    if (innermost.IsSamePosition(parent))
                    {
                        return false;
                    }
                    if (innermost.IsDescendant(parent))
                    {
                        break;
                    }
                    _ancestors.RemoveAt(_ancestors.Count - 1);
                }
                _ancestors.Add(parent);
            }

            _parents.Add(parent);
            return true;
        }

        private bool IsInOrder(XPathNavigator previous, XPathNavigator current)
        {
            // Compare starting from the node that is expected to come first in document order,
            // since for some navigators the cost grows with the number of nodes scanned past it.
            XmlNodeOrder order = _reverse ? current.ComparePosition(previous) : previous.ComparePosition(current);
            return order == XmlNodeOrder.Before || order == XmlNodeOrder.Same;
        }
    }
}
