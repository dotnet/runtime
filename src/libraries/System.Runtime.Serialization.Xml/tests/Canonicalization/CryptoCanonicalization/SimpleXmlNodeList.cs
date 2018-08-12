// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace System.Runtime.Serialization.Xml.Canonicalization.Tests
{
    internal class SimpleXmlNodeList : XmlNodeList
    {
        private readonly List<XmlNode> _list = new List<XmlNode>();

        public override int Count
        {
            get { return _list.Count; }
        }

        public void Add(XmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            _list.Add(node);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(XmlNode node)
        {
            return _list.Contains(node);
        }

        public override IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public override XmlNode Item(int index)
        {
            return _list[index];
        }

        public void Remove(XmlNode node)
        {
            _list.Remove(node);
        }
    }
}

