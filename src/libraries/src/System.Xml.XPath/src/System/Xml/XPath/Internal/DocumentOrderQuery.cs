// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class DocumentOrderQuery : CacheOutputQuery
    {
        public DocumentOrderQuery(Query qyParent) : base(qyParent) { }
        private DocumentOrderQuery(DocumentOrderQuery other) : base(other) { }

        public override object Evaluate(XPathNodeIterator context)
        {
            base.Evaluate(context);

            XPathNavigator node;
            while ((node = base.input.Advance()) != null)
            {
                Insert(outputBuffer, node);
            }

            return this;
        }

        public override XPathNavigator MatchNode(XPathNavigator context)
        {
            return input.MatchNode(context);
        }

        public override XPathNodeIterator Clone() { return new DocumentOrderQuery(this); }
    }
}





