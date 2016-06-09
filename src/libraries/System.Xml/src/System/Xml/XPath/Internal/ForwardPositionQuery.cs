// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Xml.XPath
{
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using System.Diagnostics;

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

            XPathNavigator node;
            while ((node = base.input.Advance()) != null)
            {
                outputBuffer.Add(node.Clone());
            }

            return this;
        }

        public override XPathNavigator MatchNode(XPathNavigator context)
        {
            return input.MatchNode(context);
        }

        public override XPathNodeIterator Clone() { return new ForwardPositionQuery(this); }
    }
}





