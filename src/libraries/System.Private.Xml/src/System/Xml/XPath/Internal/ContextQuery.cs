// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class ContextQuery : Query
    {
        protected XPathNavigator? contextNode;

        public ContextQuery()
        {
            this.count = 0;
        }
        protected ContextQuery(ContextQuery other) : base(other)
        {
            this.contextNode = other.contextNode;   // Don't need to clone here
        }
        public override void Reset()
        {
            count = 0;
        }

        public override XPathNavigator? Current { get { return contextNode; } }

        public override object Evaluate(XPathNodeIterator context)
        {
            contextNode = context.Current; // We don't clone here. Because we never move it.
            count = 0;
            return this;
        }

        public override XPathNavigator? Advance()
        {
            if (count == 0)
            {
                count = 1;
                return contextNode;
            }
            return null;
        }

        public override XPathNavigator? MatchNode(XPathNavigator? current)
        {
            return current;
        }

        public override XPathNodeIterator Clone() { return new ContextQuery(this); }

        public override XPathResultType StaticType { get { return XPathResultType.NodeSet; } }
        public override int CurrentPosition { get { return count; } }
        public override int Count { get { return 1; } }
        public override QueryProps Properties { get { return QueryProps.Merge | QueryProps.Cached | QueryProps.Position | QueryProps.Count; } }
    }
}
