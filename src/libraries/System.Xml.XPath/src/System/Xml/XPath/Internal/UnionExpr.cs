// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class UnionExpr : Query
    {
        internal Query qy1, qy2;
        private bool advance1, advance2;
        private XPathNavigator currentNode;
        private XPathNavigator nextNode;

        public UnionExpr(Query query1, Query query2)
        {
            this.qy1 = query1;
            this.qy2 = query2;
            this.advance1 = true;
            this.advance2 = true;
        }
        private UnionExpr(UnionExpr other) : base(other)
        {
            this.qy1 = Clone(other.qy1);
            this.qy2 = Clone(other.qy2);
            this.advance1 = other.advance1;
            this.advance2 = other.advance2;
            this.currentNode = Clone(other.currentNode);
            this.nextNode = Clone(other.nextNode);
        }

        public override void Reset()
        {
            qy1.Reset();
            qy2.Reset();
            advance1 = true;
            advance2 = true;
            nextNode = null;
        }

        public override void SetXsltContext(XsltContext xsltContext)
        {
            qy1.SetXsltContext(xsltContext);
            qy2.SetXsltContext(xsltContext);
        }

        public override object Evaluate(XPathNodeIterator context)
        {
            qy1.Evaluate(context);
            qy2.Evaluate(context);
            advance1 = true;
            advance2 = true;
            nextNode = null;
            base.ResetCount();
            return this;
        }

        private XPathNavigator ProcessSamePosition(XPathNavigator result)
        {
            currentNode = result;
            advance1 = advance2 = true;
            return result;
        }

        private XPathNavigator ProcessBeforePosition(XPathNavigator res1, XPathNavigator res2)
        {
            nextNode = res2;
            advance2 = false;
            advance1 = true;
            currentNode = res1;
            return res1;
        }

        private XPathNavigator ProcessAfterPosition(XPathNavigator res1, XPathNavigator res2)
        {
            nextNode = res1;
            advance1 = false;
            advance2 = true;
            currentNode = res2;
            return res2;
        }

        public override XPathNavigator Advance()
        {
            XPathNavigator res1, res2;
            XmlNodeOrder order = 0;
            if (advance1)
            {
                res1 = qy1.Advance();
            }
            else
            {
                res1 = nextNode;
            }
            if (advance2)
            {
                res2 = qy2.Advance();
            }
            else
            {
                res2 = nextNode;
            }
            if (res1 != null && res2 != null)
            {
                order = CompareNodes(res1, res2);
            }
            else if (res2 == null)
            {
                advance1 = true;
                advance2 = false;
                currentNode = res1;
                nextNode = null;
                return res1;
            }
            else
            {
                advance1 = false;
                advance2 = true;
                currentNode = res2;
                nextNode = null;
                return res2;
            }

            if (order == XmlNodeOrder.Before)
            {
                return ProcessBeforePosition(res1, res2);
            }
            else if (order == XmlNodeOrder.After)
            {
                return ProcessAfterPosition(res1, res2);
            }
            else
            {
                // BugBug. In case of Unknown we sorting as the same.
                return ProcessSamePosition(res1);
            }
        }

        public override XPathNavigator MatchNode(XPathNavigator xsltContext)
        {
            if (xsltContext != null)
            {
                XPathNavigator result = qy1.MatchNode(xsltContext);
                if (result != null)
                {
                    return result;
                }
                return qy2.MatchNode(xsltContext);
            }
            return null;
        }

        public override XPathResultType StaticType { get { return XPathResultType.NodeSet; } }

        public override XPathNodeIterator Clone() { return new UnionExpr(this); }

        public override XPathNavigator Current { get { return currentNode; } }
        public override int CurrentPosition { get { throw new InvalidOperationException(); } }

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            if (qy1 != null)
            {
                qy1.PrintQuery(w);
            }
            if (qy2 != null)
            {
                qy2.PrintQuery(w);
            }
            w.WriteEndElement();
        }
    }
}
