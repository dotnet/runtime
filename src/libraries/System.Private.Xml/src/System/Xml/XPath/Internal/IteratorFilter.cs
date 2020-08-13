// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class IteratorFilter : XPathNodeIterator
    {
        private readonly XPathNodeIterator _innerIterator;
        private readonly string _name;
        private int _position;

        internal IteratorFilter(XPathNodeIterator innerIterator, string name)
        {
            _innerIterator = innerIterator;
            _name = name;
        }

        private IteratorFilter(IteratorFilter it)
        {
            _innerIterator = it._innerIterator.Clone();
            _name = it._name;
            _position = it._position;
        }

        public override XPathNodeIterator Clone() { return new IteratorFilter(this); }
        public override XPathNavigator? Current { get { return _innerIterator.Current; } }
        public override int CurrentPosition { get { return _position; } }

        public override bool MoveNext()
        {
            while (_innerIterator.MoveNext())
            {
                Debug.Assert(_innerIterator.Current != null);
                if (_innerIterator.Current.LocalName == _name)
                {
                    _position++;
                    return true;
                }
            }
            return false;
        }
    }
}
