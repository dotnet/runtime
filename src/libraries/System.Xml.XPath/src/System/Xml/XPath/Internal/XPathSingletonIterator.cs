// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class XPathSingletonIterator : ResetableIterator
    {
        private XPathNavigator nav;
        private int position;

        public XPathSingletonIterator(XPathNavigator nav)
        {
            Debug.Assert(nav != null);
            this.nav = nav;
        }

        public XPathSingletonIterator(XPathNavigator nav, bool moved) : this(nav)
        {
            if (moved)
            {
                position = 1;
            }
        }

        public XPathSingletonIterator(XPathSingletonIterator it)
        {
            this.nav = it.nav.Clone();
            this.position = it.position;
        }

        public override XPathNodeIterator Clone()
        {
            return new XPathSingletonIterator(this);
        }

        public override XPathNavigator Current
        {
            get { return nav; }
        }

        public override int CurrentPosition
        {
            get { return position; }
        }

        public override int Count
        {
            get { return 1; }
        }

        public override bool MoveNext()
        {
            if (position == 0)
            {
                position = 1;
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            position = 0;
        }
    }
}
