// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Xml
{
    internal sealed class XmlChildNodes : XmlNodeList
    {
        private readonly XmlNode _container;

        public XmlChildNodes(XmlNode container)
        {
            _container = container;
        }

        public override XmlNode? Item(int i)
        {
            // Out of range indexes return a null XmlNode
            if (i < 0)
                return null;

            for (XmlNode? n = _container.FirstChild; n != null; n = n.NextSibling, i--)
            {
                if (i == 0)
                    return n;
            }

            return null;
        }

        public override int Count
        {
            get
            {
                int c = 0;
                for (XmlNode? n = _container.FirstChild; n != null; n = n.NextSibling)
                {
                    c++;
                }

                return c;
            }
        }

        public override IEnumerator GetEnumerator()
        {
            if (_container.FirstChild == null)
            {
                return XmlDocument.EmptyEnumerator;
            }
            else
            {
                return new XmlChildEnumerator(_container);
            }
        }
    }
}
