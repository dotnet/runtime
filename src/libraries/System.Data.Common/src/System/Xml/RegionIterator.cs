// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

#pragma warning disable 618 // ignore obsolete warning about XmlDataDocument

namespace System.Xml
{
    internal abstract class BaseRegionIterator : BaseTreeIterator
    {
        internal BaseRegionIterator(DataSetMapper mapper) : base(mapper) { }
    }

    // Iterates over non-attribute nodes
    internal sealed class RegionIterator : BaseRegionIterator
    {
        private XmlBoundElement _rowElement;
        private XmlNode _currentNode;

        internal RegionIterator(XmlBoundElement rowElement) : base(((XmlDataDocument)(rowElement.OwnerDocument)).Mapper)
        {
            Debug.Assert(rowElement != null && rowElement.Row != null);
            _rowElement = rowElement;
            _currentNode = rowElement;
        }

        internal override XmlNode CurrentNode => _currentNode;

        internal override bool Next()
        {
            XmlNode nextNode;
            ElementState oldState = _rowElement.ElementState;

            // We do not want to cause any foliation w/ this iterator or use this iterator once the region was defoliated
            Debug.Assert(oldState != ElementState.None);

            // Try to move to the first child
            nextNode = _currentNode.FirstChild;

            // No children, try next sibling
            if (nextNode != null)
            {
                _currentNode = nextNode;
                // If we have been defoliated, we should have stayed that way
                Debug.Assert((oldState == ElementState.Defoliated) ? (_rowElement.ElementState == ElementState.Defoliated) : true);
                // Rollback foliation
                _rowElement.ElementState = oldState;
                return true;
            }

            return NextRight();
        }

        internal override bool NextRight()
        {
            // Make sure we do not get past the rowElement if we call NextRight on a just initialized iterator and rowElement has no children
            if (_currentNode == _rowElement)
            {
                _currentNode = null;
                return false;
            }

            ElementState oldState = _rowElement.ElementState;

            // We do not want to cause any foliation w/ this iterator or use this iterator once the region was defoliated
            Debug.Assert(oldState != ElementState.None);

            XmlNode nextNode = _currentNode.NextSibling;

            if (nextNode != null)
            {
                _currentNode = nextNode;
                // If we have been defoliated, we should have stayed that way
                Debug.Assert((oldState == ElementState.Defoliated) ? (_rowElement.ElementState == ElementState.Defoliated) : true);
                // Rollback foliation
                _rowElement.ElementState = oldState;
                return true;
            }

            // No next sibling, try the first sibling of from the parent chain
            nextNode = _currentNode;
            while (nextNode != _rowElement && nextNode.NextSibling == null)
            {
                nextNode = nextNode.ParentNode;
            }

            if (nextNode == _rowElement)
            {
                _currentNode = null;
                // If we have been defoliated, we should have stayed that way
                Debug.Assert((oldState == ElementState.Defoliated) ? (_rowElement.ElementState == ElementState.Defoliated) : true);

                // Rollback foliation
                _rowElement.ElementState = oldState;
                return false;
            }

            _currentNode = nextNode.NextSibling;
            Debug.Assert(_currentNode != null);

            // If we have been defoliated, we should have stayed that way
            Debug.Assert((oldState == ElementState.Defoliated) ? (_rowElement.ElementState == ElementState.Defoliated) : true);

            // Rollback foliation
            _rowElement.ElementState = oldState;
            return true;
        }

        // Get the initial text value for the current node. You should be positioned on the node (element) for
        // which to get the initial text value, not on the text node.
        internal bool NextInitialTextLikeNodes(out string value)
        {
            Debug.Assert(CurrentNode != null);
            Debug.Assert(CurrentNode.NodeType == XmlNodeType.Element);
#if DEBUG
            // It's not OK to try to read the initial text value for sub-regions, because we do not know how to revert their initial state
            if (CurrentNode.NodeType == XmlNodeType.Element && mapper.GetTableSchemaForElement((XmlElement)(CurrentNode)) != null)
            {
                if (CurrentNode != _rowElement)
                {
                    Debug.Assert(false);
                }
            }
#endif

            ElementState oldState = _rowElement.ElementState;

            // We do not want to cause any foliation w/ this iterator or use this iterator once the region was defoliated
            Debug.Assert(oldState != ElementState.None);

            XmlNode n = CurrentNode.FirstChild;
            value = GetInitialTextFromNodes(ref n);
            if (n == null)
            {
                // If we have been defoliated, we should have stayed that way
                Debug.Assert((oldState == ElementState.Defoliated) ? (_rowElement.ElementState == ElementState.Defoliated) : true);

                // Rollback eventual foliation
                _rowElement.ElementState = oldState;
                return NextRight();
            }
            Debug.Assert(!XmlDataDocument.IsTextLikeNode(n));
            _currentNode = n;

            // If we have been defoliated, we should have stayed that way
            Debug.Assert((oldState == ElementState.Defoliated) ? (_rowElement.ElementState == ElementState.Defoliated) : true);

            // Rollback eventual foliation
            _rowElement.ElementState = oldState;
            return true;
        }

        private static string GetInitialTextFromNodes(ref XmlNode n)
        {
            string value = null;

            if (n != null)
            {
                // don't consider whitespace
                while (n.NodeType == XmlNodeType.Whitespace)
                {
                    n = n.NextSibling;
                    if (n == null)
                    {
                        return string.Empty;
                    }
                }

                if (XmlDataDocument.IsTextLikeNode(n) && (n.NextSibling == null || !XmlDataDocument.IsTextLikeNode(n.NextSibling)))
                {
                    // don't use string builder if only one text node exists
                    value = n.Value;
                    n = n.NextSibling;
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    while (n != null && XmlDataDocument.IsTextLikeNode(n))
                    {
                        // Ignore non-significant whitespace nodes
                        if (n.NodeType != XmlNodeType.Whitespace)
                        {
                            sb.Append(n.Value);
                        }
                        n = n.NextSibling;
                    }
                    value = sb.ToString();
                }
            }

            return value ?? string.Empty;
        }
    }
}
