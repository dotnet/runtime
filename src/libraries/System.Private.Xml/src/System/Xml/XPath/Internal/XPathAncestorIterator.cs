// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class XPathAncestorIterator : XPathAxisIterator
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
