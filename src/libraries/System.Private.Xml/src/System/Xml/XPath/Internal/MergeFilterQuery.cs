// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class MergeFilterQuery : CacheOutputQuery
    {
        private Query _child;

        public MergeFilterQuery(Query input, Query child) : base(input)
        {
            _child = child;
        }
        private MergeFilterQuery(MergeFilterQuery other) : base(other)
        {
            _child = Clone(other._child);
        }

        public override void SetXsltContext(XsltContext xsltContext)
        {
            base.SetXsltContext(xsltContext);
            _child.SetXsltContext(xsltContext);
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            base.Evaluate(nodeIterator);

            while (input.Advance() != null)
            {
                _child.Evaluate(input);
                XPathNavigator node;
                while ((node = _child.Advance()) != null)
                {
                    Insert(outputBuffer, node);
                }
            }
            return this;
        }

        public override XPathNavigator MatchNode(XPathNavigator current)
        {
            XPathNavigator context = _child.MatchNode(current);
            if (context == null)
            {
                return null;
            }
            context = input.MatchNode(context);
            if (context == null)
            {
                return null;
            }
            Evaluate(new XPathSingletonIterator(context.Clone(), /*moved:*/true));
            XPathNavigator result = Advance();
            while (result != null)
            {
                if (result.IsSamePosition(current))
                {
                    return context;
                }
                result = Advance();
            }
            return null;
        }

        public override XPathNodeIterator Clone() { return new MergeFilterQuery(this); }
    }
}
