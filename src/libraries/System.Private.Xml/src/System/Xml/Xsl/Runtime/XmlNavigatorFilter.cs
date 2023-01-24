// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.XPath;
using System.Diagnostics;
using System.ComponentModel;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// XmlNavigatorFilter provides a flexible filtering abstraction over XPathNavigator.  Callers do
    /// not know what type of filtering will occur; they simply call MoveToContent or MoveToSibling.
    /// The filter implementation invokes appropriate operation(s) on the XPathNavigator in order
    /// to skip over filtered nodes.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class XmlNavigatorFilter
    {
        /// <summary>
        /// Reposition the navigator to the first matching content node (inc. attributes); skip over
        /// filtered nodes.  If there are no matching nodes, then don't move navigator and return false.
        /// </summary>
        public abstract bool MoveToContent(XPathNavigator navigator);

        /// <summary>
        /// Reposition the navigator to the next matching content node (inc. attributes); skip over
        /// filtered nodes.  If there are no matching nodes, then don't move navigator and return false.
        /// </summary>
        public abstract bool MoveToNextContent(XPathNavigator navigator);

        /// <summary>
        /// Reposition the navigator to the next following sibling node (no attributes); skip over
        /// filtered nodes.  If there are no matching nodes, then don't move navigator and return false.
        /// </summary>
        public abstract bool MoveToFollowingSibling(XPathNavigator navigator);

        /// <summary>
        /// Reposition the navigator to the previous sibling node (no attributes); skip over filtered
        /// nodes.  If there are no matching nodes, then don't move navigator and return false.
        /// </summary>
        public abstract bool MoveToPreviousSibling(XPathNavigator navigator);

        /// <summary>
        /// Reposition the navigator to the next following node (inc. descendants); skip over filtered nodes.
        /// If there are no matching nodes, then return false.
        /// </summary>
        public abstract bool MoveToFollowing(XPathNavigator navigator, XPathNavigator? navigatorEnd);

        /// <summary>
        /// Return true if the navigator's current node matches the filter condition.
        /// </summary>
        public abstract bool IsFiltered(XPathNavigator navigator);
    }


    /// <summary>
    /// Filters any non-element and any element with a non-matching local name or namespace uri.
    /// </summary>
    internal sealed class XmlNavNameFilter : XmlNavigatorFilter
    {
        private readonly string _localName;
        private readonly string _namespaceUri;

        /// <summary>
        /// Return an XmlNavigatorFilter that skips over nodes that do not match the specified name.
        /// </summary>
        public static XmlNavigatorFilter Create(string localName, string namespaceUri)
        {
            return new XmlNavNameFilter(localName, namespaceUri);
        }

        /// <summary>
        /// Keep only elements with name = localName, namespaceUri.
        /// </summary>
        private XmlNavNameFilter(string localName, string namespaceUri)
        {
            _localName = localName;
            _namespaceUri = namespaceUri;
        }

        /// <summary>
        /// Reposition the navigator on the first element child with a matching name.
        /// </summary>
        public override bool MoveToContent(XPathNavigator navigator)
        {
            return navigator.MoveToChild(_localName, _namespaceUri);
        }

        /// <summary>
        /// Reposition the navigator on the next element child with a matching name.
        /// </summary>
        public override bool MoveToNextContent(XPathNavigator navigator)
        {
            return navigator.MoveToNext(_localName, _namespaceUri);
        }

        /// <summary>
        /// Reposition the navigator on the next element sibling with a matching name.
        /// </summary>
        public override bool MoveToFollowingSibling(XPathNavigator navigator)
        {
            return navigator.MoveToNext(_localName, _namespaceUri);
        }

        /// <summary>
        /// Reposition the navigator on the previous element sibling with a matching name.
        /// </summary>
        public override bool MoveToPreviousSibling(XPathNavigator navigator)
        {
            return navigator.MoveToPrevious(_localName, _namespaceUri);
        }

        /// <summary>
        /// Reposition the navigator on the next following element with a matching name.
        /// </summary>
        public override bool MoveToFollowing(XPathNavigator navigator, XPathNavigator? navEnd)
        {
            return navigator.MoveToFollowing(_localName, _namespaceUri, navEnd);
        }

        /// <summary>
        /// Return false if the navigator is positioned on an element with a matching name.
        /// </summary>
        public override bool IsFiltered(XPathNavigator navigator)
        {
            return navigator.LocalName != _localName || navigator.NamespaceURI != _namespaceUri;
        }
    }


    /// <summary>
    /// Filters any node not of the specified type (type may not be attribute or namespace).
    /// </summary>
    internal sealed class XmlNavTypeFilter : XmlNavigatorFilter
    {
        private static readonly XmlNavigatorFilter[] s_typeFilters = CreateTypeFilters();
        private readonly XPathNodeType _nodeType;
        private readonly int _mask;

        /// <summary>
        /// There are a limited number of types, so create all possible XmlNavTypeFilter objects just once.
        /// </summary>
        private static XmlNavigatorFilter[] CreateTypeFilters()
        {
            var filters = new XmlNavigatorFilter[(int)XPathNodeType.Comment + 1];
            filters[(int)XPathNodeType.Element] = new XmlNavTypeFilter(XPathNodeType.Element);
            filters[(int)XPathNodeType.Text] = new XmlNavTypeFilter(XPathNodeType.Text);
            filters[(int)XPathNodeType.ProcessingInstruction] = new XmlNavTypeFilter(XPathNodeType.ProcessingInstruction);
            filters[(int)XPathNodeType.Comment] = new XmlNavTypeFilter(XPathNodeType.Comment);
            return filters;
        }

        /// <summary>
        /// Return a previously constructed XmlNavigatorFilter that skips over nodes that do not match the specified type.
        /// </summary>
        public static XmlNavigatorFilter Create(XPathNodeType nodeType)
        {
            Debug.Assert(s_typeFilters[(int)nodeType] != null);
            return s_typeFilters[(int)nodeType];
        }

        /// <summary>
        /// Keep only nodes with XPathNodeType = nodeType, where XPathNodeType.Text selects whitespace as well.
        /// </summary>
        private XmlNavTypeFilter(XPathNodeType nodeType)
        {
            Debug.Assert(nodeType != XPathNodeType.Attribute && nodeType != XPathNodeType.Namespace);
            _nodeType = nodeType;
            _mask = XPathNavigator.GetContentKindMask(nodeType);
        }

        /// <summary>
        /// Reposition the navigator on the first child with a matching type.
        /// </summary>
        public override bool MoveToContent(XPathNavigator navigator)
        {
            return navigator.MoveToChild(_nodeType);
        }

        /// <summary>
        /// Reposition the navigator on the next child with a matching type.
        /// </summary>
        public override bool MoveToNextContent(XPathNavigator navigator)
        {
            return navigator.MoveToNext(_nodeType);
        }

        /// <summary>
        /// Reposition the navigator on the next non-attribute sibling with a matching type.
        /// </summary>
        public override bool MoveToFollowingSibling(XPathNavigator navigator)
        {
            return navigator.MoveToNext(_nodeType);
        }

        /// <summary>
        /// Reposition the navigator on the previous non-attribute sibling with a matching type.
        /// </summary>
        public override bool MoveToPreviousSibling(XPathNavigator navigator)
        {
            return navigator.MoveToPrevious(_nodeType);
        }

        /// <summary>
        /// Reposition the navigator on the next following element with a matching kind.
        /// </summary>
        public override bool MoveToFollowing(XPathNavigator navigator, XPathNavigator? navEnd)
        {
            return navigator.MoveToFollowing(_nodeType, navEnd);
        }

        /// <summary>
        /// Return false if the navigator is positioned on a node with a matching type.
        /// </summary>
        public override bool IsFiltered(XPathNavigator navigator)
        {
            return ((1 << (int)navigator.NodeType) & _mask) == 0;
        }
    }


    /// <summary>
    /// Filters all attribute nodes.
    /// </summary>
    internal sealed class XmlNavAttrFilter : XmlNavigatorFilter
    {
        private static readonly XmlNavigatorFilter s_singleton = new XmlNavAttrFilter();

        /// <summary>
        /// Return a singleton XmlNavigatorFilter that filters all attribute nodes.
        /// </summary>
        public static XmlNavigatorFilter Create()
        {
            return s_singleton;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        private XmlNavAttrFilter()
        {
        }

        /// <summary>
        /// Reposition the navigator on the first non-attribute child.
        /// </summary>
        public override bool MoveToContent(XPathNavigator navigator)
        {
            return navigator.MoveToFirstChild();
        }

        /// <summary>
        /// Reposition the navigator on the next non-attribute sibling.
        /// </summary>
        public override bool MoveToNextContent(XPathNavigator navigator)
        {
            return navigator.MoveToNext();
        }

        /// <summary>
        /// Reposition the navigator on the next non-attribute sibling.
        /// </summary>
        public override bool MoveToFollowingSibling(XPathNavigator navigator)
        {
            return navigator.MoveToNext();
        }

        /// <summary>
        /// Reposition the navigator on the previous non-attribute sibling.
        /// </summary>
        public override bool MoveToPreviousSibling(XPathNavigator navigator)
        {
            return navigator.MoveToPrevious();
        }

        /// <summary>
        /// Reposition the navigator on the next following non-attribute.
        /// </summary>
        public override bool MoveToFollowing(XPathNavigator navigator, XPathNavigator? navEnd)
        {
            return navigator.MoveToFollowing(XPathNodeType.All, navEnd);
        }

        /// <summary>
        /// Return true if the navigator is positioned on an attribute.
        /// </summary>
        public override bool IsFiltered(XPathNavigator navigator)
        {
            return navigator.NodeType == XPathNodeType.Attribute;
        }
    }


    /// <summary>
    /// Never filter nodes.
    /// </summary>
    internal sealed class XmlNavNeverFilter : XmlNavigatorFilter
    {
        private static readonly XmlNavigatorFilter s_singleton = new XmlNavNeverFilter();

        /// <summary>
        /// Return a singleton XmlNavigatorFilter that never filters any nodes.
        /// </summary>
        public static XmlNavigatorFilter Create()
        {
            return s_singleton;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        private XmlNavNeverFilter()
        {
        }

        /// <summary>
        /// Reposition the navigator on the first child (attribute or non-attribute).
        /// </summary>
        public override bool MoveToContent(XPathNavigator navigator)
        {
            return MoveToFirstAttributeContent(navigator);
        }

        /// <summary>
        /// Reposition the navigator on the next child (attribute or non-attribute).
        /// </summary>
        public override bool MoveToNextContent(XPathNavigator navigator)
        {
            return MoveToNextAttributeContent(navigator);
        }

        /// <summary>
        /// Reposition the navigator on the next sibling (no attributes).
        /// </summary>
        public override bool MoveToFollowingSibling(XPathNavigator navigator)
        {
            return navigator.MoveToNext();
        }

        /// <summary>
        /// Reposition the navigator on the previous sibling (no attributes).
        /// </summary>
        public override bool MoveToPreviousSibling(XPathNavigator navigator)
        {
            return navigator.MoveToPrevious();
        }

        /// <summary>
        /// Reposition the navigator on the next following node.
        /// </summary>
        public override bool MoveToFollowing(XPathNavigator navigator, XPathNavigator? navEnd)
        {
            return navigator.MoveToFollowing(XPathNodeType.All, navEnd);
        }

        /// <summary>
        /// Nodes are never filtered so always return false.
        /// </summary>
        public override bool IsFiltered(XPathNavigator navigator)
        {
            return false;
        }

        /// <summary>
        /// Move to navigator's first attribute node.  If no attribute's exist, move to the first content node.
        /// If no content nodes exist, return null.  Otherwise, return navigator.
        /// </summary>
        public static bool MoveToFirstAttributeContent(XPathNavigator navigator)
        {
            if (!navigator.MoveToFirstAttribute())
                return navigator.MoveToFirstChild();
            return true;
        }

        /// <summary>
        /// If navigator is positioned on an attribute, move to the next attribute node.  If there are no more
        /// attributes, move to the first content node.  If navigator is positioned on a content node, move to
        /// the next content node.  If there are no more attributes and content nodes, return null.
        /// Otherwise, return navigator.
        /// </summary>
        public static bool MoveToNextAttributeContent(XPathNavigator navigator)
        {
            if (navigator.NodeType == XPathNodeType.Attribute)
            {
                if (!navigator.MoveToNextAttribute())
                {
                    navigator.MoveToParent();
                    if (!navigator.MoveToFirstChild())
                    {
                        // No children, so reposition on original attribute
                        navigator.MoveToFirstAttribute();
                        while (navigator.MoveToNextAttribute())
                            ;
                        return false;
                    }
                }
                return true;
            }
            return navigator.MoveToNext();
        }
    }
}
