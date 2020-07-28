// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class XPathDescendantIterator : XPathAxisIterator
    {
        private int _level;

        public XPathDescendantIterator(XPathNavigator nav, XPathNodeType type, bool matchSelf) : base(nav, type, matchSelf) { }
        public XPathDescendantIterator(XPathNavigator nav, string name, string namespaceURI, bool matchSelf) : base(nav, name, namespaceURI, matchSelf) { }

        public XPathDescendantIterator(XPathDescendantIterator it) : base(it)
        {
            _level = it._level;
        }

        public override XPathNodeIterator Clone()
        {
            return new XPathDescendantIterator(this);
        }

        public override bool MoveNext()
        {
            if (_level == -1)
            {
                // We can only get here when we already iterated over all descendants
                return false;
            }

            if (first)
            {
                first = false;
                if (matchSelf && Matches)
                {
                    position = 1;
                    return true;
                }
            }

            while (true)
            {
                if (nav.MoveToFirstChild())
                {
                    _level++;
                }
                else
                {
                    while (true)
                    {
                        if (_level == 0)
                        {
                            // In an attempt to find next descendant we ended up being back in the root node
                            // which means we are done iterating descendants.
                            // If we have called this method again MoveToFirstChild would cause iteration to start over.
                            // We will use _level == -1 to mark that we are already done iterating.
                            _level = -1;
                            return false;
                        }
                        if (nav.MoveToNext())
                        {
                            break;
                        }
                        nav.MoveToParent();
                        _level--;
                    }
                }

                if (Matches)
                {
                    position++;
                    return true;
                }
            }
        }
    }
}
