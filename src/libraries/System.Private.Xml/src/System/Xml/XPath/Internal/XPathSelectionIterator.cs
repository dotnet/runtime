// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    // We need this wrapper object to:
    //      1. Calculate position
    //      2. Protect internal query.Current from user who may call MoveNext().
    internal sealed class XPathSelectionIterator : ResettableIterator
    {
        private XPathNavigator _nav;
        private readonly Query _query;
        private int _position;

        internal XPathSelectionIterator(XPathNavigator nav, Query query)
        {
            _nav = nav.Clone();
            _query = query;
        }

        private XPathSelectionIterator(XPathSelectionIterator it)
        {
            _nav = it._nav.Clone();
            _query = (Query)it._query.Clone();
            _position = it._position;
        }

        public override void Reset()
        {
            _query.Reset();
        }

        public override bool MoveNext()
        {
            XPathNavigator? n = _query.Advance();
            if (n != null)
            {
                _position++;
                if (!_nav.MoveTo(n))
                {
                    _nav = n.Clone();
                }
                return true;
            }
            return false;
        }

        public override int Count { get { return _query.Count; } }
        public override XPathNavigator Current { get { return _nav; } }
        public override int CurrentPosition { get { return _position; } }
        public override XPathNodeIterator Clone() { return new XPathSelectionIterator(this); }
    }
}
