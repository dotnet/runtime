// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
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

            XPathNavigator? node;
            while ((node = base.input.Advance()) != null)
            {
                Insert(outputBuffer, node);
            }

            return this;
        }

        public override XPathNavigator? MatchNode(XPathNavigator? context)
        {
            return input.MatchNode(context);
        }

        public override XPathNodeIterator Clone() { return new DocumentOrderQuery(this); }
    }
}
