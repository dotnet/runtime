// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal abstract class XPathAxisIterator : XPathNodeIterator
    {
        internal XPathNavigator nav;
        internal XPathNodeType type;
        internal string name;
        internal string uri;
        internal int position;
        internal bool matchSelf;
        internal bool first = true;

        public XPathAxisIterator(XPathNavigator nav, bool matchSelf)
        {
            this.nav = nav;
            this.matchSelf = matchSelf;
        }

        public XPathAxisIterator(XPathNavigator nav, XPathNodeType type, bool matchSelf) : this(nav, matchSelf)
        {
            this.type = type;
        }

        public XPathAxisIterator(XPathNavigator nav, string name, string namespaceURI, bool matchSelf) : this(nav, matchSelf)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (namespaceURI == null) throw new ArgumentNullException(nameof(namespaceURI));

            this.name = name;
            this.uri = namespaceURI;
        }

        public XPathAxisIterator(XPathAxisIterator it)
        {
            this.nav = it.nav.Clone();
            this.type = it.type;
            this.name = it.name;
            this.uri = it.uri;
            this.position = it.position;
            this.matchSelf = it.matchSelf;
            this.first = it.first;
        }

        public override XPathNavigator Current
        {
            get { return nav; }
        }

        public override int CurrentPosition
        {
            get { return position; }
        }

        // Nodetype Matching - Given nodetype matches the navigator's nodetype
        //Given nodetype is all . So it matches everything
        //Given nodetype is text - Matches text, WS, Significant WS
        protected virtual bool Matches
        {
            get
            {
                if (name == null)
                {
                    return (
                        type == nav.NodeType ||
                        type == XPathNodeType.All ||
                        type == XPathNodeType.Text && (
                            nav.NodeType == XPathNodeType.Whitespace ||
                            nav.NodeType == XPathNodeType.SignificantWhitespace
                        )
                    );
                }
                else
                {
                    return (
                        nav.NodeType == XPathNodeType.Element &&
                        (name.Length == 0 || name == nav.LocalName) &&
                        (uri == nav.NamespaceURI)
                    );
                }
            }
        }
    }
}
