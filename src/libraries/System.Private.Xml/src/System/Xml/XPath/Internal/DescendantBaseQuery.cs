// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal abstract class DescendantBaseQuery : BaseAxisQuery
    {
        protected bool matchSelf;
        protected bool abbrAxis;

        public DescendantBaseQuery(Query qyParent, string Name, string Prefix, XPathNodeType Type, bool matchSelf, bool abbrAxis) : base(qyParent, Name, Prefix, Type)
        {
            this.matchSelf = matchSelf;
            this.abbrAxis = abbrAxis;
        }
        public DescendantBaseQuery(DescendantBaseQuery other) : base(other)
        {
            this.matchSelf = other.matchSelf;
            this.abbrAxis = other.abbrAxis;
        }

        public override XPathNavigator MatchNode(XPathNavigator context)
        {
            if (context != null)
            {
                if (!abbrAxis)
                {
                    throw XPathException.Create(SR.Xp_InvalidPattern);
                }
                XPathNavigator result = null;
                if (matches(context))
                {
                    if (matchSelf)
                    {
                        if ((result = qyInput.MatchNode(context)) != null)
                        {
                            return result;
                        }
                    }

                    XPathNavigator anc = context.Clone();
                    while (anc.MoveToParent())
                    {
                        if ((result = qyInput.MatchNode(anc)) != null)
                        {
                            return result;
                        }
                    }
                }
            }
            return null;
        }
    }
}
