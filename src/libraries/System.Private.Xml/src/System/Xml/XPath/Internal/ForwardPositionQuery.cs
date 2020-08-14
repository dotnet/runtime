// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class ForwardPositionQuery : CacheOutputQuery
    {
        public ForwardPositionQuery(Query input) : base(input)
        {
            Debug.Assert(input != null);
        }
        protected ForwardPositionQuery(ForwardPositionQuery other) : base(other) { }

        public override object Evaluate(XPathNodeIterator context)
        {
            base.Evaluate(context);

            XPathNavigator? node;
            while ((node = base.input.Advance()) != null)
            {
                outputBuffer.Add(node.Clone());
            }

            return this;
        }

        public override XPathNavigator? MatchNode(XPathNavigator? context)
        {
            return input.MatchNode(context);
        }

        public override XPathNodeIterator Clone() { return new ForwardPositionQuery(this); }
    }
}
