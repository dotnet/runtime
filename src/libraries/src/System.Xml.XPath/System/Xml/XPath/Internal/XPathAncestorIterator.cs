// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class XPathAncestorIterator : XPathAxisIterator
    {
        public XPathAncestorIterator(XPathNavigator nav, XPathNodeType type, bool matchSelf) : base(nav, type, matchSelf) { }
        public XPathAncestorIterator(XPathNavigator nav, string name, string namespaceURI, bool matchSelf) : base(nav, name, namespaceURI, matchSelf) { }
        public XPathAncestorIterator(XPathAncestorIterator other) : base(other) { }

        public override bool MoveNext()
        {
            if (first)
            {
                first = false;
                if (matchSelf && Matches)
                {
                    position = 1;
                    return true;
                }
            }

            while (nav.MoveToParent())
            {
                if (Matches)
                {
                    position++;
                    return true;
                }
            }
            return false;
        }

        public override XPathNodeIterator Clone() { return new XPathAncestorIterator(this); }
    }
}

