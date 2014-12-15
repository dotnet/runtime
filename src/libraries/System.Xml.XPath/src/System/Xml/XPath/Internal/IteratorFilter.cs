// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class IteratorFilter : XPathNodeIterator
    {
        private XPathNodeIterator _innerIterator;
        private string _name;
        private int _position = 0;

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
        public override XPathNavigator Current { get { return _innerIterator.Current; } }
        public override int CurrentPosition { get { return _position; } }

        public override bool MoveNext()
        {
            while (_innerIterator.MoveNext())
            {
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
