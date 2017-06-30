// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

#pragma warning disable 0618 // ignore obsolete warning about XmlDataDocument

namespace System.Xml
{
    // Iterates over non-attribute nodes
    internal sealed class TreeIterator : BaseTreeIterator
    {
        private readonly XmlNode _nodeTop;
        private XmlNode _currentNode;

        internal TreeIterator(XmlNode nodeTop) : base(((XmlDataDocument)(nodeTop.OwnerDocument)).Mapper)
        {
            Debug.Assert(nodeTop != null);
            _nodeTop = nodeTop;
            _currentNode = nodeTop;
        }

        internal override XmlNode CurrentNode => _currentNode;

        internal override bool Next()
        {
            XmlNode nextNode;

            // Try to move to the first child
            nextNode = _currentNode.FirstChild;

            // No children, try next sibling
            if (nextNode != null)
            {
                _currentNode = nextNode;
                return true;
            }

            return NextRight();
        }

        internal override bool NextRight()
        {
            // Make sure we do not get past the nodeTop if we call NextRight on a just initialized iterator and nodeTop has no children
            if (_currentNode == _nodeTop)
            {
                _currentNode = null;
                return false;
            }

            XmlNode nextNode = _currentNode.NextSibling;

            if (nextNode != null)
            {
                _currentNode = nextNode;
                return true;
            }

            // No next sibling, try the first sibling of from the parent chain
            nextNode = _currentNode;
            while (nextNode != _nodeTop && nextNode.NextSibling == null)
            {
                nextNode = nextNode.ParentNode;
            }

            if (nextNode == _nodeTop)
            {
                _currentNode = null;
                return false;
            }

            _currentNode = nextNode.NextSibling;
            Debug.Assert(_currentNode != null);
            return true;
        }
    }
}

