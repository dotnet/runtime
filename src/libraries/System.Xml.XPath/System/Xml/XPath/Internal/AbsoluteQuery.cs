// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class AbsoluteQuery : ContextQuery
    {
        public AbsoluteQuery() : base() { }
        private AbsoluteQuery(AbsoluteQuery other) : base(other) { }

        public override object Evaluate(XPathNodeIterator context)
        {
            base.contextNode = context.Current.Clone();
            base.contextNode.MoveToRoot();
            count = 0;
            return this;
        }

        public override XPathNavigator MatchNode(XPathNavigator context)
        {
            if (context != null && context.NodeType == XPathNodeType.Root)
            {
                return context;
            }
            return null;
        }

        public override XPathNodeIterator Clone() { return new AbsoluteQuery(this); }
    }
}
