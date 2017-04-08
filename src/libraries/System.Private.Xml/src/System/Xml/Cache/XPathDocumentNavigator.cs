// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache
{
    /// <summary>
    /// This is the default XPath/XQuery data model cache implementation.  It will be used whenever
    /// the user does not supply his own XPathNavigator implementation.
    /// </summary>
    internal sealed class XPathDocumentNavigator : XPathNavigator, IXmlLineInfo
    {
        private XPathNode[] _pageCurrent;
        private XPathNode[] _pageParent;
        private int _idxCurrent;
        private int _idxParent;
        private string _atomizedLocalName;


        //-----------------------------------------------
        // Constructors
        //-----------------------------------------------

        /// <summary>
        /// Create a new navigator positioned on the specified current node.  If the current node is a namespace or a collapsed
        /// text node, then the parent is a virtualized parent (may be different than .Parent on the current node).
        /// </summary>
        public XPathDocumentNavigator(XPathNode[] pageCurrent, int idxCurrent, XPathNode[] pageParent, int idxParent)
        {
            Debug.Assert(pageCurrent != null && idxCurrent != 0);
            Debug.Assert((pageParent == null) == (idxParent == 0));
            _pageCurrent = pageCurrent;
            _pageParent = pageParent;
            _idxCurrent = idxCurrent;
            _idxParent = idxParent;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public XPathDocumentNavigator(XPathDocumentNavigator nav) : this(nav._pageCurrent, nav._idxCurrent, nav._pageParent, nav._idxParent)
        {
            _atomizedLocalName = nav._atomizedLocalName;
        }


        //-----------------------------------------------
        // XPathItem
        //-----------------------------------------------

        /// <summary>
        /// Get the string value of the current node, computed using data model dm:string-value rules.
        /// If the node has a typed value, return the string representation of the value.  If the node
        /// is not a parent type (comment, text, pi, etc.), get its simple text value.  Otherwise,
        /// concatenate all text node descendants of the current node.
        /// </summary>
        public override string Value
        {
            get
            {
                string value;
                XPathNode[] page, pageEnd;
                int idx, idxEnd;

                // Try to get the pre-computed string value of the node
                value = _pageCurrent[_idxCurrent].Value;
                if (value != null)
                    return value;

#if DEBUG
                switch (_pageCurrent[_idxCurrent].NodeType)
                {
                    case XPathNodeType.Namespace:
                    case XPathNodeType.Attribute:
                    case XPathNodeType.Comment:
                    case XPathNodeType.ProcessingInstruction:
                        Debug.Fail("ReadStringValue() should have taken care of these node types.");
                        break;

                    case XPathNodeType.Text:
                        Debug.Assert(_idxParent != 0 && _pageParent[_idxParent].HasCollapsedText,
                                     "ReadStringValue() should have taken care of anything but collapsed text.");
                        break;
                }
#endif

                // If current node is collapsed text, then parent element has a simple text value
                if (_idxParent != 0)
                {
                    Debug.Assert(_pageCurrent[_idxCurrent].NodeType == XPathNodeType.Text);
                    return _pageParent[_idxParent].Value;
                }

                // Must be node with complex content, so concatenate the string values of all text descendants
                string s = string.Empty;
                StringBuilder bldr = null;

                // Get all text nodes which follow the current node in document order, but which are still descendants
                page = pageEnd = _pageCurrent;
                idx = idxEnd = _idxCurrent;
                if (!XPathNodeHelper.GetNonDescendant(ref pageEnd, ref idxEnd))
                {
                    pageEnd = null;
                    idxEnd = 0;
                }

                while (XPathNodeHelper.GetTextFollowing(ref page, ref idx, pageEnd, idxEnd))
                {
                    Debug.Assert(page[idx].NodeType == XPathNodeType.Element || page[idx].IsText);

                    if (s.Length == 0)
                    {
                        s = page[idx].Value;
                    }
                    else
                    {
                        if (bldr == null)
                        {
                            bldr = new StringBuilder();
                            bldr.Append(s);
                        }
                        bldr.Append(page[idx].Value);
                    }
                }

                return (bldr != null) ? bldr.ToString() : s;
            }
        }


        //-----------------------------------------------
        // XPathNavigator
        //-----------------------------------------------

        /// <summary>
        /// Create a copy of this navigator, positioned to the same node in the tree.
        /// </summary>
        public override XPathNavigator Clone()
        {
            return new XPathDocumentNavigator(_pageCurrent, _idxCurrent, _pageParent, _idxParent);
        }

        /// <summary>
        /// Get the XPath node type of the current node.
        /// </summary>
        public override XPathNodeType NodeType
        {
            get { return _pageCurrent[_idxCurrent].NodeType; }
        }

        /// <summary>
        /// Get the local name portion of the current node's name.
        /// </summary>
        public override string LocalName
        {
            get { return _pageCurrent[_idxCurrent].LocalName; }
        }

        /// <summary>
        /// Get the namespace portion of the current node's name.
        /// </summary>
        public override string NamespaceURI
        {
            get { return _pageCurrent[_idxCurrent].NamespaceUri; }
        }

        /// <summary>
        /// Get the name of the current node.
        /// </summary>
        public override string Name
        {
            get { return _pageCurrent[_idxCurrent].Name; }
        }

        /// <summary>
        /// Get the prefix portion of the current node's name.
        /// </summary>
        public override string Prefix
        {
            get { return _pageCurrent[_idxCurrent].Prefix; }
        }

        /// <summary>
        /// Get the base URI of the current node.
        /// </summary>
        public override string BaseURI
        {
            get
            {
                XPathNode[] page;
                int idx;

                if (_idxParent != 0)
                {
                    // Get BaseUri of parent for attribute, namespace, and collapsed text nodes
                    page = _pageParent;
                    idx = _idxParent;
                }
                else
                {
                    page = _pageCurrent;
                    idx = _idxCurrent;
                }

                do
                {
                    switch (page[idx].NodeType)
                    {
                        case XPathNodeType.Element:
                        case XPathNodeType.Root:
                        case XPathNodeType.ProcessingInstruction:
                            // BaseUri is always stored with Elements, Roots, and PIs
                            return page[idx].BaseUri;
                    }

                    // Get BaseUri of parent
                    idx = page[idx].GetParent(out page);
                }
                while (idx != 0);

                return string.Empty;
            }
        }

        /// <summary>
        /// Return true if this is an element which used a shortcut tag in its Xml 1.0 serialized form.
        /// </summary>
        public override bool IsEmptyElement
        {
            get { return _pageCurrent[_idxCurrent].AllowShortcutTag; }
        }

        /// <summary>
        /// Return the xml name table which was used to atomize all prefixes, local-names, and
        /// namespace uris in the document.
        /// </summary>
        public override XmlNameTable NameTable
        {
            get { return _pageCurrent[_idxCurrent].Document.NameTable; }
        }

        /// <summary>
        /// Position the navigator on the first attribute of the current node and return true.  If no attributes
        /// can be found, return false.
        /// </summary>
        public override bool MoveToFirstAttribute()
        {
            XPathNode[] page = _pageCurrent;
            int idx = _idxCurrent;

            if (XPathNodeHelper.GetFirstAttribute(ref _pageCurrent, ref _idxCurrent))
            {
                // Save element parent in order to make node-order comparison simpler
                _pageParent = page;
                _idxParent = idx;
                return true;
            }

            return false;
        }

        /// <summary>
        /// If positioned on an attribute, move to its next sibling attribute.  If no attributes can be found,
        /// return false.
        /// </summary>
        public override bool MoveToNextAttribute()
        {
            return XPathNodeHelper.GetNextAttribute(ref _pageCurrent, ref _idxCurrent);
        }

        /// <summary>
        /// True if the current node has one or more attributes.
        /// </summary>
        public override bool HasAttributes
        {
            get { return _pageCurrent[_idxCurrent].HasAttribute; }
        }

        /// <summary>
        /// Position the navigator on the attribute with the specified name and return true.  If no matching
        /// attribute can be found, return false.  Don't assume the name parts are atomized with respect
        /// to this document.
        /// </summary>
        public override bool MoveToAttribute(string localName, string namespaceURI)
        {
            XPathNode[] page = _pageCurrent;
            int idx = _idxCurrent;

            if ((object)localName != (object)_atomizedLocalName)
                _atomizedLocalName = (localName != null) ? NameTable.Get(localName) : null;

            if (XPathNodeHelper.GetAttribute(ref _pageCurrent, ref _idxCurrent, _atomizedLocalName, namespaceURI))
            {
                // Save element parent in order to make node-order comparison simpler
                _pageParent = page;
                _idxParent = idx;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Position the navigator on the namespace within the specified scope.  If no matching namespace
        /// can be found, return false.
        /// </summary>
        public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
        {
            XPathNode[] page;
            int idx;

            if (namespaceScope == XPathNamespaceScope.Local)
            {
                // Get local namespaces only
                idx = XPathNodeHelper.GetLocalNamespaces(_pageCurrent, _idxCurrent, out page);
            }
            else
            {
                // Get all in-scope namespaces
                idx = XPathNodeHelper.GetInScopeNamespaces(_pageCurrent, _idxCurrent, out page);
            }

            while (idx != 0)
            {
                // Don't include the xmlns:xml namespace node if scope is ExcludeXml
                if (namespaceScope != XPathNamespaceScope.ExcludeXml || !page[idx].IsXmlNamespaceNode)
                {
                    _pageParent = _pageCurrent;
                    _idxParent = _idxCurrent;
                    _pageCurrent = page;
                    _idxCurrent = idx;
                    return true;
                }

                // Skip past xmlns:xml
                idx = page[idx].GetSibling(out page);
            }

            return false;
        }

        /// <summary>
        /// Position the navigator on the next namespace within the specified scope.  If no matching namespace
        /// can be found, return false.
        /// </summary>
        public override bool MoveToNextNamespace(XPathNamespaceScope scope)
        {
            XPathNode[] page = _pageCurrent, pageParent;
            int idx = _idxCurrent, idxParent;

            // If current node is not a namespace node, return false
            if (page[idx].NodeType != XPathNodeType.Namespace)
                return false;

            while (true)
            {
                // Get next namespace sibling
                idx = page[idx].GetSibling(out page);

                // If there are no more nodes, return false
                if (idx == 0)
                    return false;

                switch (scope)
                {
                    case XPathNamespaceScope.Local:
                        // Once parent changes, there are no longer any local namespaces
                        idxParent = page[idx].GetParent(out pageParent);
                        if (idxParent != _idxParent || (object)pageParent != (object)_pageParent)
                            return false;
                        break;

                    case XPathNamespaceScope.ExcludeXml:
                        // If node is xmlns:xml, then skip it
                        if (page[idx].IsXmlNamespaceNode)
                            continue;
                        break;
                }

                // Found a matching next namespace node, so return it
                break;
            }

            _pageCurrent = page;
            _idxCurrent = idx;
            return true;
        }

        /// <summary>
        /// If the current node is an attribute or namespace (not content), return false.  Otherwise,
        /// move to the next content node.  Return false if there are no more content nodes.
        /// </summary>
        public override bool MoveToNext()
        {
            return XPathNodeHelper.GetContentSibling(ref _pageCurrent, ref _idxCurrent);
        }

        /// <summary>
        /// If the current node is an attribute or namespace (not content), return false.  Otherwise,
        /// move to the previous (sibling) content node.  Return false if there are no previous content nodes.
        /// </summary>
        public override bool MoveToPrevious()
        {
            // If parent exists, then this is a namespace, an attribute, or a collapsed text node, all of which do
            // not have previous siblings.
            if (_idxParent != 0)
                return false;

            return XPathNodeHelper.GetPreviousContentSibling(ref _pageCurrent, ref _idxCurrent);
        }

        /// <summary>
        /// Move to the first content-typed child of the current node.  Return false if the current
        /// node has no content children.
        /// </summary>
        public override bool MoveToFirstChild()
        {
            if (_pageCurrent[_idxCurrent].HasCollapsedText)
            {
                // Virtualize collapsed text nodes
                _pageParent = _pageCurrent;
                _idxParent = _idxCurrent;
                _idxCurrent = _pageCurrent[_idxCurrent].Document.GetCollapsedTextNode(out _pageCurrent);
                return true;
            }

            return XPathNodeHelper.GetContentChild(ref _pageCurrent, ref _idxCurrent);
        }

        /// <summary>
        /// Position the navigator on the parent of the current node.  If the current node has no parent,
        /// return false.
        /// </summary>
        public override bool MoveToParent()
        {
            if (_idxParent != 0)
            {
                // 1. For attribute nodes, element parent is always stored in order to make node-order
                //    comparison simpler.
                // 2. For namespace nodes, parent is always stored in navigator in order to virtualize
                //    XPath 1.0 namespaces.
                // 3. For collapsed text nodes, element parent is always stored in navigator.
                Debug.Assert(_pageParent != null);
                _pageCurrent = _pageParent;
                _idxCurrent = _idxParent;
                _pageParent = null;
                _idxParent = 0;
                return true;
            }

            return XPathNodeHelper.GetParent(ref _pageCurrent, ref _idxCurrent);
        }

        /// <summary>
        /// Position this navigator to the same position as the "other" navigator.  If the "other" navigator
        /// is not of the same type as this navigator, then return false.
        /// </summary>
        public override bool MoveTo(XPathNavigator other)
        {
            XPathDocumentNavigator that = other as XPathDocumentNavigator;
            if (that != null)
            {
                _pageCurrent = that._pageCurrent;
                _idxCurrent = that._idxCurrent;
                _pageParent = that._pageParent;
                _idxParent = that._idxParent;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Position to the navigator to the element whose id is equal to the specified "id" string.
        /// </summary>
        public override bool MoveToId(string id)
        {
            XPathNode[] page;
            int idx;

            idx = _pageCurrent[_idxCurrent].Document.LookupIdElement(id, out page);
            if (idx != 0)
            {
                // Move to ID element and clear parent state
                Debug.Assert(page[idx].NodeType == XPathNodeType.Element);
                _pageCurrent = page;
                _idxCurrent = idx;
                _pageParent = null;
                _idxParent = 0;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if this navigator is positioned to the same node as the "other" navigator.  Returns false
        /// if not, or if the "other" navigator is not the same type as this navigator.
        /// </summary>
        public override bool IsSamePosition(XPathNavigator other)
        {
            XPathDocumentNavigator that = other as XPathDocumentNavigator;
            if (that != null)
            {
                return _idxCurrent == that._idxCurrent && _pageCurrent == that._pageCurrent &&
                       _idxParent == that._idxParent && _pageParent == that._pageParent;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the current node has children.
        /// </summary>
        public override bool HasChildren
        {
            get { return _pageCurrent[_idxCurrent].HasContentChild; }
        }

        /// <summary>
        /// Position the navigator on the root node of the current document.
        /// </summary>
        public override void MoveToRoot()
        {
            if (_idxParent != 0)
            {
                // Clear parent state
                _pageParent = null;
                _idxParent = 0;
            }
            _idxCurrent = _pageCurrent[_idxCurrent].GetRoot(out _pageCurrent);
        }

        /// <summary>
        /// Move to the first element child of the current node with the specified name.  Return false
        /// if the current node has no matching element children.
        /// </summary>
        public override bool MoveToChild(string localName, string namespaceURI)
        {
            if ((object)localName != (object)_atomizedLocalName)
                _atomizedLocalName = (localName != null) ? NameTable.Get(localName) : null;

            return XPathNodeHelper.GetElementChild(ref _pageCurrent, ref _idxCurrent, _atomizedLocalName, namespaceURI);
        }

        /// <summary>
        /// Move to the first element sibling of the current node with the specified name.  Return false
        /// if the current node has no matching element siblings.
        /// </summary>
        public override bool MoveToNext(string localName, string namespaceURI)
        {
            if ((object)localName != (object)_atomizedLocalName)
                _atomizedLocalName = (localName != null) ? NameTable.Get(localName) : null;

            return XPathNodeHelper.GetElementSibling(ref _pageCurrent, ref _idxCurrent, _atomizedLocalName, namespaceURI);
        }

        /// <summary>
        /// Move to the first content child of the current node with the specified type.  Return false
        /// if the current node has no matching children.
        /// </summary>
        public override bool MoveToChild(XPathNodeType type)
        {
            if (_pageCurrent[_idxCurrent].HasCollapsedText)
            {
                // Only XPathNodeType.Text and XPathNodeType.All matches collapsed text node
                if (type != XPathNodeType.Text && type != XPathNodeType.All)
                    return false;

                // Virtualize collapsed text nodes
                _pageParent = _pageCurrent;
                _idxParent = _idxCurrent;
                _idxCurrent = _pageCurrent[_idxCurrent].Document.GetCollapsedTextNode(out _pageCurrent);
                return true;
            }

            return XPathNodeHelper.GetContentChild(ref _pageCurrent, ref _idxCurrent, type);
        }

        /// <summary>
        /// Move to the first content sibling of the current node with the specified type.  Return false
        /// if the current node has no matching siblings.
        /// </summary>
        public override bool MoveToNext(XPathNodeType type)
        {
            return XPathNodeHelper.GetContentSibling(ref _pageCurrent, ref _idxCurrent, type);
        }

        /// <summary>
        /// Move to the next element that:
        ///   1. Follows the current node in document order (includes descendants, unlike XPath following axis)
        ///   2. Precedes "end" in document order (if end is null, then all following nodes in the document are considered)
        ///   3. Has the specified QName
        /// Return false if the current node has no matching following elements.
        /// </summary>
        public override bool MoveToFollowing(string localName, string namespaceURI, XPathNavigator end)
        {
            XPathNode[] pageEnd;
            int idxEnd;

            if ((object)localName != (object)_atomizedLocalName)
                _atomizedLocalName = (localName != null) ? NameTable.Get(localName) : null;

            // Get node on which scan ends (null if rest of document should be scanned)
            idxEnd = GetFollowingEnd(end as XPathDocumentNavigator, false, out pageEnd);

            // If this navigator is positioned on a virtual node, then compute following of parent
            if (_idxParent != 0)
            {
                if (!XPathNodeHelper.GetElementFollowing(ref _pageParent, ref _idxParent, pageEnd, idxEnd, _atomizedLocalName, namespaceURI))
                    return false;

                _pageCurrent = _pageParent;
                _idxCurrent = _idxParent;
                _pageParent = null;
                _idxParent = 0;
                return true;
            }

            return XPathNodeHelper.GetElementFollowing(ref _pageCurrent, ref _idxCurrent, pageEnd, idxEnd, _atomizedLocalName, namespaceURI);
        }

        /// <summary>
        /// Move to the next node that:
        ///   1. Follows the current node in document order (includes descendants, unlike XPath following axis)
        ///   2. Precedes "end" in document order (if end is null, then all following nodes in the document are considered)
        ///   3. Has the specified XPathNodeType
        /// Return false if the current node has no matching following nodes.
        /// </summary>
        public override bool MoveToFollowing(XPathNodeType type, XPathNavigator end)
        {
            XPathDocumentNavigator endTiny = end as XPathDocumentNavigator;
            XPathNode[] page, pageEnd;
            int idx, idxEnd;

            // If searching for text, make sure to handle collapsed text nodes correctly
            if (type == XPathNodeType.Text || type == XPathNodeType.All)
            {
                if (_pageCurrent[_idxCurrent].HasCollapsedText)
                {
                    // Positioned on an element with collapsed text, so return the virtual text node, assuming it's before "end"
                    if (endTiny != null && _idxCurrent == endTiny._idxParent && _pageCurrent == endTiny._pageParent)
                    {
                        // "end" is positioned to a virtual attribute, namespace, or text node
                        return false;
                    }

                    _pageParent = _pageCurrent;
                    _idxParent = _idxCurrent;
                    _idxCurrent = _pageCurrent[_idxCurrent].Document.GetCollapsedTextNode(out _pageCurrent);
                    return true;
                }

                if (type == XPathNodeType.Text)
                {
                    // Get node on which scan ends (null if rest of document should be scanned, parent if positioned on virtual node)
                    idxEnd = GetFollowingEnd(endTiny, true, out pageEnd);

                    // If this navigator is positioned on a virtual node, then compute following of parent
                    if (_idxParent != 0)
                    {
                        page = _pageParent;
                        idx = _idxParent;
                    }
                    else
                    {
                        page = _pageCurrent;
                        idx = _idxCurrent;
                    }

                    // If ending node is a virtual node, and current node is its parent, then we're done
                    if (endTiny != null && endTiny._idxParent != 0 && idx == idxEnd && page == pageEnd)
                        return false;

                    // Get all virtual (collapsed) and physical text nodes which follow the current node
                    if (!XPathNodeHelper.GetTextFollowing(ref page, ref idx, pageEnd, idxEnd))
                        return false;

                    if (page[idx].NodeType == XPathNodeType.Element)
                    {
                        // Virtualize collapsed text nodes
                        Debug.Assert(page[idx].HasCollapsedText);
                        _idxCurrent = page[idx].Document.GetCollapsedTextNode(out _pageCurrent);
                        _pageParent = page;
                        _idxParent = idx;
                    }
                    else
                    {
                        // Physical text node
                        Debug.Assert(page[idx].IsText);
                        _pageCurrent = page;
                        _idxCurrent = idx;
                        _pageParent = null;
                        _idxParent = 0;
                    }
                    return true;
                }
            }

            // Get node on which scan ends (null if rest of document should be scanned, parent + 1 if positioned on virtual node)
            idxEnd = GetFollowingEnd(endTiny, false, out pageEnd);

            // If this navigator is positioned on a virtual node, then compute following of parent
            if (_idxParent != 0)
            {
                if (!XPathNodeHelper.GetContentFollowing(ref _pageParent, ref _idxParent, pageEnd, idxEnd, type))
                    return false;

                _pageCurrent = _pageParent;
                _idxCurrent = _idxParent;
                _pageParent = null;
                _idxParent = 0;
                return true;
            }

            return XPathNodeHelper.GetContentFollowing(ref _pageCurrent, ref _idxCurrent, pageEnd, idxEnd, type);
        }

        /// <summary>
        /// Return an iterator that ranges over all children of the current node that match the specified XPathNodeType.
        /// </summary>
        public override XPathNodeIterator SelectChildren(XPathNodeType type)
        {
            return new XPathDocumentKindChildIterator(this, type);
        }

        /// <summary>
        /// Return an iterator that ranges over all children of the current node that match the specified QName.
        /// </summary>
        public override XPathNodeIterator SelectChildren(string name, string namespaceURI)
        {
            // If local name is wildcard, then call XPathNavigator.SelectChildren
            if (name == null || name.Length == 0)
                return base.SelectChildren(name, namespaceURI);

            return new XPathDocumentElementChildIterator(this, name, namespaceURI);
        }

        /// <summary>
        /// Return an iterator that ranges over all descendants of the current node that match the specified
        /// XPathNodeType.  If matchSelf is true, then also perform the match on the current node.
        /// </summary>
        public override XPathNodeIterator SelectDescendants(XPathNodeType type, bool matchSelf)
        {
            return new XPathDocumentKindDescendantIterator(this, type, matchSelf);
        }

        /// <summary>
        /// Return an iterator that ranges over all descendants of the current node that match the specified
        /// QName.  If matchSelf is true, then also perform the match on the current node.
        /// </summary>
        public override XPathNodeIterator SelectDescendants(string name, string namespaceURI, bool matchSelf)
        {
            // If local name is wildcard, then call XPathNavigator.SelectDescendants
            if (name == null || name.Length == 0)
                return base.SelectDescendants(name, namespaceURI, matchSelf);

            return new XPathDocumentElementDescendantIterator(this, name, namespaceURI, matchSelf);
        }

        /// <summary>
        /// Returns:
        ///     XmlNodeOrder.Unknown -- This navigator and the "other" navigator are not of the same type, or the
        ///                             navigator's are not positioned on nodes in the same document.
        ///     XmlNodeOrder.Before -- This navigator's current node is before the "other" navigator's current node
        ///                            in document order.
        ///     XmlNodeOrder.After -- This navigator's current node is after the "other" navigator's current node
        ///                           in document order.
        ///     XmlNodeOrder.Same -- This navigator is positioned on the same node as the "other" navigator.
        /// </summary>
        public override XmlNodeOrder ComparePosition(XPathNavigator other)
        {
            XPathDocumentNavigator that = other as XPathDocumentNavigator;
            if (that != null)
            {
                XPathDocument thisDoc = _pageCurrent[_idxCurrent].Document;
                XPathDocument thatDoc = that._pageCurrent[that._idxCurrent].Document;
                if ((object)thisDoc == (object)thatDoc)
                {
                    int locThis = GetPrimaryLocation();
                    int locThat = that.GetPrimaryLocation();

                    if (locThis == locThat)
                    {
                        locThis = GetSecondaryLocation();
                        locThat = that.GetSecondaryLocation();

                        if (locThis == locThat)
                            return XmlNodeOrder.Same;
                    }
                    return (locThis < locThat) ? XmlNodeOrder.Before : XmlNodeOrder.After;
                }
            }
            return XmlNodeOrder.Unknown;
        }

        /// <summary>
        /// Return true if the "other" navigator's current node is a descendant of this navigator's current node.
        /// </summary>
        public override bool IsDescendant(XPathNavigator other)
        {
            XPathDocumentNavigator that = other as XPathDocumentNavigator;
            if (that != null)
            {
                XPathNode[] pageThat;
                int idxThat;

                // If that current node's parent is virtualized, then start with the virtual parent
                if (that._idxParent != 0)
                {
                    pageThat = that._pageParent;
                    idxThat = that._idxParent;
                }
                else
                {
                    idxThat = that._pageCurrent[that._idxCurrent].GetParent(out pageThat);
                }

                while (idxThat != 0)
                {
                    if (idxThat == _idxCurrent && pageThat == _pageCurrent)
                        return true;
                    idxThat = pageThat[idxThat].GetParent(out pageThat);
                }
            }
            return false;
        }

        /// <summary>
        /// Construct a primary location for this navigator.  The location is an integer that can be
        /// easily compared with other locations in the same document in order to determine the relative
        /// document order of two nodes.  If two locations compare equal, then secondary locations should
        /// be compared.
        /// </summary>
        private int GetPrimaryLocation()
        {
            // Is the current node virtualized?
            if (_idxParent == 0)
            {
                // No, so primary location should be derived from current node
                return XPathNodeHelper.GetLocation(_pageCurrent, _idxCurrent);
            }

            // Yes, so primary location should be derived from parent node
            return XPathNodeHelper.GetLocation(_pageParent, _idxParent);
        }

        /// <summary>
        /// Construct a secondary location for this navigator.  This location should only be used if
        /// primary locations previously compared equal.
        /// </summary>
        private int GetSecondaryLocation()
        {
            // Is the current node virtualized?
            if (_idxParent == 0)
            {
                // No, so secondary location is int.MinValue (always first)
                return int.MinValue;
            }

            // Yes, so secondary location should be derived from current node
            // This happens with attributes nodes, namespace nodes, collapsed text nodes, and atomic values
            switch (_pageCurrent[_idxCurrent].NodeType)
            {
                case XPathNodeType.Namespace:
                    // Namespace nodes come first (make location negative, but greater than int.MinValue)
                    return int.MinValue + 1 + XPathNodeHelper.GetLocation(_pageCurrent, _idxCurrent);

                case XPathNodeType.Attribute:
                    // Attribute nodes come next (location is always positive)
                    return XPathNodeHelper.GetLocation(_pageCurrent, _idxCurrent);

                default:
                    // Collapsed text nodes are always last
                    return int.MaxValue;
            }
        }

        /// <summary>
        /// Create a unique id for the current node.  This is used by the generate-id() function.
        /// </summary>
        internal override string UniqueId
        {
            get
            {
                // 32-bit integer is split into 5-bit groups, the maximum number of groups is 7
                char[] buf = new char[1 + 7 + 1 + 7];
                int idx = 0;
                int loc;

                // Ensure distinguishing attributes, namespaces and child nodes
                buf[idx++] = NodeTypeLetter[(int)_pageCurrent[_idxCurrent].NodeType];

                // If the current node is virtualized, code its parent
                if (_idxParent != 0)
                {
                    loc = (_pageParent[0].PageInfo.PageNumber - 1) << 16 | (_idxParent - 1);
                    do
                    {
                        buf[idx++] = UniqueIdTbl[loc & 0x1f];
                        loc >>= 5;
                    } while (loc != 0);
                    buf[idx++] = '0';
                }

                // Code the node itself
                loc = (_pageCurrent[0].PageInfo.PageNumber - 1) << 16 | (_idxCurrent - 1);
                do
                {
                    buf[idx++] = UniqueIdTbl[loc & 0x1f];
                    loc >>= 5;
                } while (loc != 0);

                return new string(buf, 0, idx);
            }
        }

        public override object UnderlyingObject
        {
            get
            {
                // Since we don't have any underlying PUBLIC object
                //   the best one we can return is a clone of the navigator.
                // Note that it should be a clone as the user might Move the returned navigator
                //   around and thus cause unexpected behavior of the caller of this class (For example the validator)
                return this.Clone();
            }
        }

        //-----------------------------------------------
        // IXmlLineInfo
        //-----------------------------------------------

        /// <summary>
        /// Return true if line number information is recorded in the cache.
        /// </summary>
        public bool HasLineInfo()
        {
            return _pageCurrent[_idxCurrent].Document.HasLineInfo;
        }

        /// <summary>
        /// Return the source line number of the current node.
        /// </summary>
        public int LineNumber
        {
            get
            {
                // If the current node is a collapsed text node, then return parent element's line number
                if (_idxParent != 0 && NodeType == XPathNodeType.Text)
                    return _pageParent[_idxParent].LineNumber;

                return _pageCurrent[_idxCurrent].LineNumber;
            }
        }

        /// <summary>
        /// Return the source line position of the current node.
        /// </summary>
        public int LinePosition
        {
            get
            {
                // If the current node is a collapsed text node, then get position from parent element
                if (_idxParent != 0 && NodeType == XPathNodeType.Text)
                    return _pageParent[_idxParent].CollapsedLinePosition;

                return _pageCurrent[_idxCurrent].LinePosition;
            }
        }


        //-----------------------------------------------
        // Helper methods
        //-----------------------------------------------

        /// <summary>
        /// Get hashcode based on current position of the navigator.
        /// </summary>
        public int GetPositionHashCode()
        {
            return _idxCurrent ^ _idxParent;
        }

        /// <summary>
        /// Return true if navigator is positioned to an element having the specified name.
        /// </summary>
        public bool IsElementMatch(string localName, string namespaceURI)
        {
            if ((object)localName != (object)_atomizedLocalName)
                _atomizedLocalName = (localName != null) ? NameTable.Get(localName) : null;

            // Cannot be an element if parent is stored
            if (_idxParent != 0)
                return false;

            return _pageCurrent[_idxCurrent].ElementMatch(_atomizedLocalName, namespaceURI);
        }

        /// <summary>
        /// Return true if navigator is positioned to a node of the specified kind.  Whitespace/SignficantWhitespace/Text are
        /// all treated the same (i.e. they all match each other).
        /// </summary>
        public bool IsKindMatch(XPathNodeType typ)
        {
            return (((1 << (int)_pageCurrent[_idxCurrent].NodeType) & GetKindMask(typ)) != 0);
        }

        /// <summary>
        /// "end" is positioned on a node which terminates a following scan.  Return the page and index of "end" if it
        /// is positioned to a non-virtual node.  If "end" is positioned to a virtual node:
        ///    1. If useParentOfVirtual is true, then return the page and index of the virtual node's parent
        ///    2. If useParentOfVirtual is false, then return the page and index of the virtual node's parent + 1.
        /// </summary>
        private int GetFollowingEnd(XPathDocumentNavigator end, bool useParentOfVirtual, out XPathNode[] pageEnd)
        {
            // If ending navigator is positioned to a node in another document, then return null
            if (end != null && _pageCurrent[_idxCurrent].Document == end._pageCurrent[end._idxCurrent].Document)
            {
                // If the ending navigator is not positioned on a virtual node, then return its current node
                if (end._idxParent == 0)
                {
                    pageEnd = end._pageCurrent;
                    return end._idxCurrent;
                }

                // If the ending navigator is positioned on an attribute, namespace, or virtual text node, then use the
                // next physical node instead, as the results will be the same.
                pageEnd = end._pageParent;
                return (useParentOfVirtual) ? end._idxParent : end._idxParent + 1;
            }

            // No following, so set pageEnd to null and return an index of 0
            pageEnd = null;
            return 0;
        }
    }
}
