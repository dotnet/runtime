// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class UnionExpr : Query
    {
        internal Query qy1, qy2;
        private bool _advance1, _advance2;
        private XPathNavigator? _currentNode;
        private XPathNavigator? _nextNode;

        public UnionExpr(Query query1, Query query2)
        {
            this.qy1 = query1;
            this.qy2 = query2;
            _advance1 = true;
            _advance2 = true;
        }
        private UnionExpr(UnionExpr other) : base(other)
        {
            this.qy1 = Clone(other.qy1);
            this.qy2 = Clone(other.qy2);
            _advance1 = other._advance1;
            _advance2 = other._advance2;
            _currentNode = Clone(other._currentNode);
            _nextNode = Clone(other._nextNode);
        }

        public override void Reset()
        {
            qy1.Reset();
            qy2.Reset();
            _advance1 = true;
            _advance2 = true;
            _nextNode = null;
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
            _advance1 = true;
            _advance2 = true;
            _nextNode = null;
            base.ResetCount();
            return this;
        }

        private XPathNavigator ProcessSamePosition(XPathNavigator result)
        {
            _currentNode = result;
            _advance1 = _advance2 = true;
            return result;
        }

        private XPathNavigator ProcessBeforePosition(XPathNavigator res1, XPathNavigator res2)
        {
            _nextNode = res2;
            _advance2 = false;
            _advance1 = true;
            _currentNode = res1;
            return res1;
        }

        private XPathNavigator ProcessAfterPosition(XPathNavigator res1, XPathNavigator res2)
        {
            _nextNode = res1;
            _advance1 = false;
            _advance2 = true;
            _currentNode = res2;
            return res2;
        }

        public override XPathNavigator? Advance()
        {
            XPathNavigator? res1, res2;
            XmlNodeOrder order = 0;
            if (_advance1)
            {
                res1 = qy1.Advance();
            }
            else
            {
                res1 = _nextNode;
            }
            if (_advance2)
            {
                res2 = qy2.Advance();
            }
            else
            {
                res2 = _nextNode;
            }
            if (res1 != null && res2 != null)
            {
                order = CompareNodes(res1, res2);
            }
            else if (res2 == null)
            {
                _advance1 = true;
                _advance2 = false;
                _currentNode = res1;
                _nextNode = null;
                return res1;
            }
            else
            {
                _advance1 = false;
                _advance2 = true;
                _currentNode = res2;
                _nextNode = null;
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

        public override XPathNavigator? MatchNode(XPathNavigator? xsltContext)
        {
            if (xsltContext != null)
            {
                XPathNavigator? result = qy1.MatchNode(xsltContext);
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

        public override XPathNavigator? Current { get { return _currentNode; } }
        public override int CurrentPosition { get { throw new InvalidOperationException(); } }
    }
}
