// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class XPathSingletonIterator : ResetableIterator
    {
        private readonly XPathNavigator _nav;
        private int _position;

        public XPathSingletonIterator(XPathNavigator nav)
        {
            Debug.Assert(nav != null);
            _nav = nav;
        }

        public XPathSingletonIterator(XPathNavigator nav, bool moved) : this(nav)
        {
            if (moved)
            {
                _position = 1;
            }
        }

        public XPathSingletonIterator(XPathSingletonIterator it)
        {
            _nav = it._nav.Clone();
            _position = it._position;
        }

        public override XPathNodeIterator Clone()
        {
            return new XPathSingletonIterator(this);
        }

        public override XPathNavigator Current
        {
            get { return _nav; }
        }

        public override int CurrentPosition
        {
            get { return _position; }
        }

        public override int Count
        {
            get { return 1; }
        }

        public override bool MoveNext()
        {
            if (_position == 0)
            {
                _position = 1;
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            _position = 0;
        }
    }
}
