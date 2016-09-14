// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Xml.XPath {
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;
    using System.Collections;

    internal class XmlIteratorQuery: Query {
        ResetableIterator it;

        public XmlIteratorQuery(XPathNodeIterator it) {
            this.it = it as ResetableIterator;
            if (this.it == null) {
                this.it = new XPathArrayIterator(it);
            }
        }
        protected XmlIteratorQuery(XmlIteratorQuery other) : base(other) {
            this.it = (ResetableIterator)other.it.Clone();
        }

        public override XPathNavigator Current { get { return it.Current; } }

        public override XPathNavigator Advance() {
            if (it.MoveNext()) {
                return it.Current;
            }
            return null;
        }

        public override XPathResultType StaticType { get { return XPathResultType.NodeSet; } }

        public override void Reset() {
            it.Reset();
        }

        public override XPathNodeIterator Clone() { return new XmlIteratorQuery(this); }

        public override int CurrentPosition { get { return it.CurrentPosition; } }

        public override object Evaluate(XPathNodeIterator nodeIterator) {
            return it;
        }
    }
}
