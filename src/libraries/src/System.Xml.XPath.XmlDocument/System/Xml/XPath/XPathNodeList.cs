// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;
using System.Collections;
using System.Collections.Generic;

namespace System.Xml
{
    internal class XPathNodeList : XmlNodeList
    {
        List<XmlNode> list;
        XPathNodeIterator nodeIterator;
        bool done;

        public XPathNodeList(XPathNodeIterator nodeIterator)
        {
            this.nodeIterator = nodeIterator;
            this.list = new List<XmlNode>();
            this.done = false;
        }

        public override int Count
        {
            get
            {
                if (!done)
                {
                    ReadUntil(Int32.MaxValue);
                }
                return list.Count;
            }
        }

        private static readonly object[] nullparams = { };

        private XmlNode GetNode(XPathNavigator n)
        {
            return (XmlNode)n.UnderlyingObject;
        }

        internal int ReadUntil(int index)
        {
            int count = list.Count;
            while (!done && count <= index)
            {
                if (nodeIterator.MoveNext())
                {
                    XmlNode n = GetNode(nodeIterator.Current);
                    if (n != null)
                    {
                        list.Add(n);
                        count++;
                    }
                }
                else
                {
                    done = true;
                    break;
                }
            }
            return count;
        }

        public override XmlNode Item(int index)
        {
            if (list.Count <= index)
            {
                ReadUntil(index);
            }
            if (index < 0 || list.Count <= index)
            {
                return null;
            }
            return list[index];
        }

        public override IEnumerator GetEnumerator()
        {
            return new XmlNodeListEnumerator(this);
        }
    }

    internal class XmlNodeListEnumerator : IEnumerator
    {
        XPathNodeList list;
        int index;
        bool valid;

        public XmlNodeListEnumerator(XPathNodeList list)
        {
            this.list = list;
            this.index = -1;
            this.valid = false;
        }

        public void Reset()
        {
            index = -1;
        }

        public bool MoveNext()
        {
            index++;
            int count = list.ReadUntil(index + 1);   // read past for delete-node case
            if (count - 1 < index)
            {
                return false;
            }
            valid = (list[index] != null);
            return valid;
        }

        public object Current
        {
            get
            {
                if (valid)
                {
                    return list[index];
                }
                return null;
            }
        }
    }
}
