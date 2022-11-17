// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// Iterate over all following-sibling content nodes.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct FollowingSiblingIterator
    {
        private XmlNavigatorFilter _filter;
        private XPathNavigator _navCurrent;

        /// <summary>
        /// Initialize the FollowingSiblingIterator.
        /// </summary>
        public void Create(XPathNavigator context, XmlNavigatorFilter filter)
        {
            _navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
            _filter = filter;
        }

        /// <summary>
        /// Position the iterator on the next following-sibling node.  Return true if such a node exists and
        /// set Current property.  Otherwise, return false (Current property is undefined).
        /// </summary>
        public bool MoveNext()
        {
            return _filter.MoveToFollowingSibling(_navCurrent);
        }

        /// <summary>
        /// Return the current result navigator.  This is only defined after MoveNext() has returned true.
        /// </summary>
        public XPathNavigator Current
        {
            get { return _navCurrent; }
        }
    }


    /// <summary>
    /// Iterate over child following-sibling nodes.  This is a simple variation on the ContentMergeIterator, so use containment
    /// to reuse its code (can't use inheritance with structures).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct FollowingSiblingMergeIterator
    {
        private ContentMergeIterator _wrapped;

        /// <summary>
        /// Initialize the FollowingSiblingMergeIterator.
        /// </summary>
        public void Create(XmlNavigatorFilter filter)
        {
            _wrapped.Create(filter);
        }

        /// <summary>
        /// Position this iterator to the next content or sibling node.  Return IteratorResult.NoMoreNodes if there are
        /// no more content or sibling nodes.  Return IteratorResult.NeedInputNode if the next input node needs to be
        /// fetched first.  Return IteratorResult.HaveCurrent if the Current property is set to the next node in the
        /// iteration.
        /// </summary>
        public IteratorResult MoveNext(XPathNavigator navigator)
        {
            return _wrapped.MoveNext(navigator, false);
        }

        /// <summary>
        /// Return the current result navigator.  This is only defined after MoveNext() has returned IteratorResult.HaveCurrent.
        /// </summary>
        public XPathNavigator Current
        {
            get { return _wrapped.Current; }
        }
    }


    /// <summary>
    /// Iterate over all preceding nodes according to XPath preceding axis rules, returning nodes in reverse
    /// document order.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct PrecedingSiblingIterator
    {
        private XmlNavigatorFilter _filter;
        private XPathNavigator _navCurrent;

        /// <summary>
        /// Initialize the PrecedingSiblingIterator.
        /// </summary>
        public void Create(XPathNavigator context, XmlNavigatorFilter filter)
        {
            _navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
            _filter = filter;
        }

        /// <summary>
        /// Return true if the Current property is set to the next Preceding node in reverse document order.
        /// </summary>
        public bool MoveNext()
        {
            return _filter.MoveToPreviousSibling(_navCurrent);
        }

        /// <summary>
        /// Return the current result navigator.  This is only defined after MoveNext() has returned true.
        /// </summary>
        public XPathNavigator Current
        {
            get { return _navCurrent; }
        }
    }


    /// <summary>
    /// Iterate over all preceding-sibling content nodes in document order.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct PrecedingSiblingDocOrderIterator
    {
        private XmlNavigatorFilter _filter;
        private XPathNavigator _navCurrent, _navEnd;
        private bool _needFirst, _useCompPos;

        /// <summary>
        /// Initialize the PrecedingSiblingDocOrderIterator.
        /// </summary>
        public void Create(XPathNavigator context, XmlNavigatorFilter filter)
        {
            _filter = filter;
            _navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
            _navEnd = XmlQueryRuntime.SyncToNavigator(_navEnd, context);
            _needFirst = true;

            // If the context node will be filtered out, then use ComparePosition to
            // determine when the context node has been passed by.  Otherwise, IsSamePosition
            // is sufficient to determine when the context node has been reached.
            _useCompPos = _filter.IsFiltered(context);
        }

        /// <summary>
        /// Position the iterator on the next preceding-sibling node.  Return true if such a node exists and
        /// set Current property.  Otherwise, return false (Current property is undefined).
        /// </summary>
        public bool MoveNext()
        {
            if (_needFirst)
            {
                // Get first matching preceding-sibling node
                if (!_navCurrent.MoveToParent())
                    return false;

                if (!_filter.MoveToContent(_navCurrent))
                    return false;

                _needFirst = false;
            }
            else
            {
                // Get next matching preceding-sibling node
                if (!_filter.MoveToFollowingSibling(_navCurrent))
                    return false;
            }

            // Accept matching sibling only if it precedes navEnd in document order
            if (_useCompPos)
                return (_navCurrent.ComparePosition(_navEnd) == XmlNodeOrder.Before);

            if (_navCurrent.IsSamePosition(_navEnd))
            {
                // Found the original context node, so iteration is complete.  If MoveNext
                // is called again, use ComparePosition so that false will continue to be
                // returned.
                _useCompPos = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Return the current result navigator.  This is only defined after MoveNext() has returned true.
        /// </summary>
        public XPathNavigator Current
        {
            get { return _navCurrent; }
        }
    }
}
