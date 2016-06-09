// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Xml.XPath
{
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using System.Diagnostics;

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
                    throw XPathException.Create(Res.Xp_InvalidPattern);
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

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            if (matchSelf)
            {
                w.WriteAttributeString("self", "yes");
            }
            if (NameTest)
            {
                w.WriteAttributeString("name", Prefix.Length != 0 ? Prefix + ':' + Name : Name);
            }
            if (TypeTest != XPathNodeType.Element)
            {
                w.WriteAttributeString("nodeType", TypeTest.ToString());
            }
            qyInput.PrintQuery(w);
            w.WriteEndElement();
        }
    }
}
