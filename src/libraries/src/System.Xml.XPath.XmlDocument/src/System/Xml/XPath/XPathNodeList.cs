// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;
using System.Collections;
using System.Collections.Generic;

namespace System.Xml
{
    internal class XPathNodeList : XmlNodeList
    {
        private List<XmlNode> _list;
        private XPathNodeIterator _nodeIterator;
        private bool _done;

        public XPathNodeList(XPathNodeIterator nodeIterator)
        {
            _nodeIterator = nodeIterator;
            _list = new List<XmlNode>();
            _done = false;
        }

        public override int Count
        {
            get
            {
                if (!_done)
                {
                    ReadUntil(Int32.MaxValue);
                }
                return _list.Count;
            }
        }

        private static readonly object[] _nullparams = { };

        private XmlNode GetNode(XPathNavigator n)
        {
            return (XmlNode)n.UnderlyingObject;
        }

        internal int ReadUntil(int index)
        {
            int count = _list.Count;
            while (!_done && count <= index)
            {
                if (_nodeIterator.MoveNext())
                {
                    XmlNode n = GetNode(_nodeIterator.Current);
                    if (n != null)
                    {
                        _list.Add(n);
                        count++;
                    }
                }
                else
                {
                    _done = true;
                    break;
                }
            }
            return count;
        }

        public override XmlNode Item(int index)
        {
            if (_list.Count <= index)
            {
                ReadUntil(index);
            }
            if (index < 0 || _list.Count <= index)
            {
                return null;
            }
            return _list[index];
        }

        public override IEnumerator GetEnumerator()
        {
            return new XmlNodeListEnumerator(this);
        }
    }

    internal class XmlNodeListEnumerator : IEnumerator
    {
        private XPathNodeList _list;
        private int _index;
        private bool _valid;

        public XmlNodeListEnumerator(XPathNodeList list)
        {
            _list = list;
            _index = -1;
            _valid = false;
        }

        public void Reset()
        {
            _index = -1;
        }

        public bool MoveNext()
        {
            _index++;
            int count = _list.ReadUntil(_index + 1);   // read past for delete-node case
            if (count - 1 < _index)
            {
                return false;
            }
            _valid = (_list[_index] != null);
            return _valid;
        }

        public object Current
        {
            get
            {
                if (_valid)
                {
                    return _list[_index];
                }
                return null;
            }
        }
    }
}
