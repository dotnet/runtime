// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class FilterQuery : BaseAxisQuery
    {
        private readonly Query _cond;
        private readonly bool _noPosition;

        public FilterQuery(Query qyParent, Query cond, bool noPosition) : base(qyParent)
        {
            _cond = cond;
            _noPosition = noPosition;
        }
        private FilterQuery(FilterQuery other) : base(other)
        {
            _cond = Clone(other._cond);
            _noPosition = other._noPosition;
        }

        public override void Reset()
        {
            _cond.Reset();
            base.Reset();
        }

        public Query Condition { get { return _cond; } }

        public override void SetXsltContext(XsltContext input)
        {
            base.SetXsltContext(input);
            _cond.SetXsltContext(input);
            if (_cond.StaticType != XPathResultType.Number && _cond.StaticType != XPathResultType.Any && _noPosition)
            {
                // BugBug: We can do such trick at Evaluate time only.
                // But to do this FilterQuery should stop inherit from BaseAxisQuery
                ReversePositionQuery? query = qyInput as ReversePositionQuery;
                if (query != null)
                {
                    qyInput = query.input;
                }
            }
        }

        public override XPathNavigator? Advance()
        {
            while ((currentNode = qyInput.Advance()) != null)
            {
                if (EvaluatePredicate())
                {
                    position++;
                    return currentNode;
                }
            }
            return null;
        }

        internal bool EvaluatePredicate()
        {
            object value = _cond.Evaluate(qyInput);
            if (value is XPathNodeIterator) return _cond.Advance() != null;
            if (value is string) return ((string)value).Length != 0;
            if (value is double) return (((double)value) == qyInput.CurrentPosition);
            if (value is bool) return (bool)value;
            Debug.Assert(value is XPathNavigator, "Unknown value type");
            return true;
        }

        public override XPathNavigator? MatchNode(XPathNavigator? current)
        {
            XPathNavigator? context;
            if (current == null)
            {
                return null;
            }
            context = qyInput.MatchNode(current);

            if (context != null)
            {
                // In this switch we process some special case in which we can calculate predicate faster then in generic case
                switch (_cond.StaticType)
                {
                    case XPathResultType.Number:
                        OperandQuery? operand = _cond as OperandQuery;
                        if (operand != null)
                        {
                            double val = (double)operand.val;
                            ChildrenQuery? childrenQuery = qyInput as ChildrenQuery;
                            if (childrenQuery != null)
                            { // foo[2], but not foo[expr][2]
                                XPathNavigator result = current.Clone();
                                result.MoveToParent();
                                int i = 0;
                                result.MoveToFirstChild();
                                do
                                {
                                    if (childrenQuery.matches(result))
                                    {
                                        i++;
                                        if (current.IsSamePosition(result))
                                        {
                                            return val == i ? context : null;
                                        }
                                    }
                                } while (result.MoveToNext());
                                return null;
                            }
                            AttributeQuery? attributeQuery = qyInput as AttributeQuery;
                            if (attributeQuery != null)
                            {// @foo[3], but not @foo[expr][2]
                                XPathNavigator result = current.Clone();
                                result.MoveToParent();
                                int i = 0;
                                result.MoveToFirstAttribute();
                                do
                                {
                                    if (attributeQuery.matches(result))
                                    {
                                        i++;
                                        if (current.IsSamePosition(result))
                                        {
                                            return val == i ? context : null;
                                        }
                                    }
                                } while (result.MoveToNextAttribute());
                                return null;
                            }
                        }
                        break;
                    case XPathResultType.NodeSet:
                        _cond.Evaluate(new XPathSingletonIterator(current, /*moved:*/true));
                        return (_cond.Advance() != null) ? context : null;
                    case XPathResultType.Boolean:
                        if (_noPosition)
                        {
                            return ((bool)_cond.Evaluate(new XPathSingletonIterator(current, /*moved:*/true))) ? context : null;
                        }
                        break;
                    case XPathResultType.String:
                        if (_noPosition)
                        {
                            return (((string)_cond.Evaluate(new XPathSingletonIterator(current, /*moved:*/true))).Length != 0) ? context : null;
                        }
                        break;
                    case XPathResultType_Navigator:
                        return context;
                    default:
                        return null;
                }
                /* Generic case */
                {
                    Evaluate(new XPathSingletonIterator(context, /*moved:*/true));
                    XPathNavigator? result;
                    while ((result = Advance()) != null)
                    {
                        if (result.IsSamePosition(current))
                        {
                            return context;
                        }
                    }
                }
            }
            return null;
        }

        public override QueryProps Properties
        {
            get
            {
                return QueryProps.Position | (qyInput.Properties & (QueryProps.Merge | QueryProps.Reverse));
            }
        }

        public override XPathNodeIterator Clone() { return new FilterQuery(this); }
    }
}
