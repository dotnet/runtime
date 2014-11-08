// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class IteratorFilter : XPathNodeIterator
    {
        private XPathNodeIterator innerIterator;
        private string name;
        private int position = 0;

        internal IteratorFilter(XPathNodeIterator innerIterator, string name)
        {
            this.innerIterator = innerIterator;
            this.name = name;
        }

        private IteratorFilter(IteratorFilter it)
        {
            this.innerIterator = it.innerIterator.Clone();
            this.name = it.name;
            this.position = it.position;
        }

        public override XPathNodeIterator Clone() { return new IteratorFilter(this); }
        public override XPathNavigator Current { get { return innerIterator.Current; } }
        public override int CurrentPosition { get { return this.position; } }

        public override bool MoveNext()
        {
            while (innerIterator.MoveNext())
            {
                if (innerIterator.Current.LocalName == this.name)
                {
                    this.position++;
                    return true;
                }
            }
            return false;
        }
    }
}
